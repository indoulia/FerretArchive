using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Ferret.Core.Search;
using Ferret.Workspace.Graph;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// Process-local, read-through cache in front of an <see cref="IFederatedKnowledgeStore"/> (WIP-030 +
/// WIP-031, merged per <c>20-Phase-3-Priority-Assessment.md</c> §1: pull-based invalidation has no
/// standalone value until there is a federated query cache to invalidate). A repeat query against an
/// unchanged set of participating workspaces skips the entire fan-out/merge pipeline; anything else
/// — a different query, different options, a changed reference set, changed content, or a workspace
/// that cannot currently be verified — always falls through to the real, authoritative pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache key:</b> the query identity (raw text or parsed <see cref="SearchQuery.OriginalText"/>),
/// the parts of <see cref="SearchOptions"/> that can affect output, the queried workspace's ID, that
/// workspace's own current Workspace State Fingerprint (<see cref="IWorkspaceStateFingerprintProvider"/>,
/// ADR-0027 Amendment), and — for every one of its <see cref="WorkspaceRegistryEntry.References"/> —
/// the reference's mode, pinned-state hash, and a per-reference change signal: a <i>pinned</i>
/// reference's current Workspace State Fingerprint (same drift-check the real, uncached pipeline
/// already performs), or a <i>floating</i> reference's cheap
/// <see cref="IWorkspaceStateFingerprintProvider.ComputeIndexChangeSignalAsync"/> signal (P3-002 —
/// see that method's remarks for why this is correct for a floating reference specifically, and
/// <c>26-P3-002-Query-Cache-Regression.md</c> for the measurement that motivated it). No new metadata
/// is introduced; every input already exists for another reason (the registry, the fingerprint
/// provider, the on-disk keyword index, the reference record itself).
/// </para>
/// <para>
/// <b>Cache value:</b> the derived <see cref="SearchServiceResult"/> exactly as the inner store
/// produced it — hits, citations, and diagnostics included. This cache is never a source of truth;
/// the inner <see cref="IFederatedKnowledgeStore"/> remains authoritative, and every cached entry is
/// byte-for-byte a value that pipeline actually returned for that exact key.
/// </para>
/// <para>
/// <b>Invalidation:</b> pull-based, like the rest of this codebase's caching (<c>21-P3-001-Fingerprint-Optimization.md</c>,
/// <c>CachingWorkspaceRegistry</c>). There is no push/event path — every call recomputes the key from
/// current state and a state change (content, reference added/removed, pin drift) naturally produces
/// a different key, so the old entry is simply never looked up again. If the key cannot be built at
/// all — the queried workspace isn't found, a reference's registry entry is corrupt, a reference no
/// longer resolves, or any participant's fingerprint or change signal can't be computed (unreachable
/// local checkout, no index built yet) — the cache is bypassed entirely for that call, in both
/// directions: no lookup, no write. This is
/// what guarantees the cache can never mask corruption or an unreachable workspace: those cases are
/// simply never cached, so every call runs the real pipeline and reports the real diagnostic.
/// </para>
/// </remarks>
public sealed class CachingFederatedKnowledgeStore : IFederatedKnowledgeStore
{
    private readonly IFederatedKnowledgeStore _inner;
    private readonly IWorkspaceRegistry _registry;
    private readonly IWorkspaceStateFingerprintProvider _fingerprintProvider;
    private readonly Guid _workspaceId;
    private readonly FederatedQueryCache _cache;

    /// <summary>Initializes a new instance of the <see cref="CachingFederatedKnowledgeStore"/> class.</summary>
    /// <param name="inner">The real federated store this decorates. Always authoritative on a cache miss.</param>
    /// <param name="registry">The workspace registry, used only to probe current reference topology for the cache key.</param>
    /// <param name="fingerprintProvider">Computes the Workspace State Fingerprint used to detect content changes for the cache key.</param>
    /// <param name="workspaceId">The workspace being queried.</param>
    /// <param name="cache">The shared, process-local cache store. Must be a singleton to be effective across queries.</param>
    public CachingFederatedKnowledgeStore(
        IFederatedKnowledgeStore inner,
        IWorkspaceRegistry registry,
        IWorkspaceStateFingerprintProvider fingerprintProvider,
        Guid workspaceId,
        FederatedQueryCache cache)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(fingerprintProvider);
        ArgumentNullException.ThrowIfNull(cache);
        _inner = inner;
        _registry = registry;
        _fingerprintProvider = fingerprintProvider;
        _workspaceId = workspaceId;
        _cache = cache;
    }

    /// <inheritdoc/>
    public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ExecuteAsync($"raw:{rawQuery}", () => _inner.SearchAsync(rawQuery, options), options);
    }

    /// <inheritdoc/>
    public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);
        return ExecuteAsync($"parsed:{query.OriginalText}", () => _inner.SearchAsync(query, options), options);
    }

    private static string Sha256Hex(string input) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    private async Task<SearchServiceResult> ExecuteAsync(string queryIdentity, Func<Task<SearchServiceResult>> run, SearchOptions options)
    {
        var key = await TryBuildCacheKeyAsync(queryIdentity, options, options.Token).ConfigureAwait(false);
        if (key is not null && _cache.TryGet(key, out var cached))
        {
            return cached;
        }

        var result = await run().ConfigureAwait(false);

        if (key is not null)
        {
            _cache.Set(key, result);
        }

        return result;
    }

    /// <summary>
    /// Builds the cache key from the current, live state of the queried workspace and everything it
    /// references, or returns <see langword="null"/> when that state cannot be safely verified.
    /// </summary>
    private async Task<string?> TryBuildCacheKeyAsync(string queryIdentity, SearchOptions options, CancellationToken ct)
    {
        WorkspaceRegistryEntry? entry;

        // Cache-safety boundary: any failure while probing workspace state for the cache key must
        // never prevent the real query from running -- bypass the cache (return null), not the query.
        // The inner store performs this same resolution for real immediately after, and reports
        // whatever the actual outcome is (including throwing, for a corrupt queried workspace).
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            entry = await _registry.ResolveAsync(_workspaceId, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }

        if (entry is null)
        {
            return null;
        }

        string? ownFingerprint;
        try
        {
            ownFingerprint = await _fingerprintProvider.ComputeFingerprintAsync(entry, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }

        if (ownFingerprint is null)
        {
            return null;
        }

        var participants = new List<string> { $"{entry.WorkspaceId}:{ownFingerprint}" };

        foreach (var reference in entry.References)
        {
            WorkspaceRegistryEntry? referenced;
            try
            {
                referenced = await _registry.ResolveAsync(reference.WorkspaceId, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }

            if (referenced is null)
            {
                return null;
            }

            // P3-002: floating-reference cache keys track the state of the search index, because a
            // cached query result is derived exclusively from indexed content -- ComputeIndexChangeSignalAsync
            // is correct and cheap here. Pinned-reference validation keeps using the Workspace State
            // Fingerprint (ADR-0027), because it must detect content drift independently of indexing --
            // that is the whole point of pinning. These two branches must NOT be collapsed into one
            // mechanism; see IWorkspaceStateFingerprintProvider's remarks for why each exists.
            string? changeSignal;
            try
            {
                changeSignal = reference.PinnedStateHash is not null
                    ? await _fingerprintProvider.ComputeFingerprintAsync(referenced, ct).ConfigureAwait(false)
                    : await _fingerprintProvider.ComputeIndexChangeSignalAsync(referenced, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }

            if (changeSignal is null)
            {
                return null;
            }

            participants.Add($"{reference.WorkspaceId}:{reference.Mode}:{reference.PinnedStateHash}:{changeSignal}");
        }
#pragma warning restore CA1031

        var keyMaterial = string.Join(
            '|',
            [
                queryIdentity,
                options.MaxResults.ToString(CultureInfo.InvariantCulture),
                options.IncludePassages.ToString(CultureInfo.InvariantCulture),
                options.HighlightEnabled.ToString(CultureInfo.InvariantCulture),
                options.SnippetLength.ToString(CultureInfo.InvariantCulture),
                options.Mode.ToString(),
                _workspaceId.ToString(),
                .. participants,
            ]);

        return Sha256Hex(keyMaterial);
    }
}
