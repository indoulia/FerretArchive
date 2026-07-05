using System.Diagnostics;

using Ferret.Core.Search;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// Fans a query out across a workspace's own member repos and every workspace it references
/// (03-Cross-Workspace-References.md §2), merges the hits, and tags each with its source workspace.
/// Never copies or re-indexes a referenced workspace's content (ADR-0027) — it only holds a live
/// handle (via <see cref="IRepoSearchServiceFactory"/>) to each repo's existing, unmodified index.
/// A source that fails or returns no index is skipped rather than failing the whole query — one
/// unavailable referenced repo must not corrupt results from the rest (ADR-0027 Consequences) — and
/// every skipped source is recorded in the merged result's <see cref="SearchServiceResult.Diagnostics"/>
/// so a caller can tell a complete answer from a partial one (Stabilization Sprint 1).
/// </summary>
public sealed partial class FederatedKnowledgeStore : IFederatedKnowledgeStore
{
    private readonly IWorkspaceRegistry _registry;
    private readonly IRepoSearchServiceFactory _repoSearchServiceFactory;
    private readonly Guid _workspaceId;
    private readonly IWorkspaceStateFingerprintProvider _fingerprintProvider;
    private readonly ILogger<FederatedKnowledgeStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederatedKnowledgeStore"/> class.</summary>
    /// <param name="registry">The workspace registry, used to resolve member repos and references.</param>
    /// <param name="repoSearchServiceFactory">Builds a per-repo search service.</param>
    /// <param name="workspaceId">The workspace being queried.</param>
    /// <param name="fingerprintProvider">Computes the Workspace State Fingerprint used to verify a pinned reference (ADR-0027 Amendment).</param>
    /// <param name="logger">Structured logger for per-query duration and per-source skip events (WIP-040). Defaults to a no-op logger.</param>
    public FederatedKnowledgeStore(
        IWorkspaceRegistry registry,
        IRepoSearchServiceFactory repoSearchServiceFactory,
        Guid workspaceId,
        IWorkspaceStateFingerprintProvider fingerprintProvider,
        ILogger<FederatedKnowledgeStore>? logger = null)
    {
        _registry = registry;
        _repoSearchServiceFactory = repoSearchServiceFactory;
        _workspaceId = workspaceId;
        _fingerprintProvider = fingerprintProvider;
        _logger = logger ?? NullLogger<FederatedKnowledgeStore>.Instance;
    }

    /// <inheritdoc/>
    public async Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return await RunAsync(options, service => service.SearchAsync(rawQuery, options)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);
        return await RunAsync(options, service => service.SearchAsync(query, options)).ConfigureAwait(false);
    }

    private static void AddRepos(List<(Guid WorkspaceId, string RepoPath)> sources, WorkspaceRegistryEntry entry)
    {
        foreach (var repo in entry.Members.Repos)
        {
            if (repo.LocalPath is not null)
            {
                sources.Add((entry.WorkspaceId, repo.LocalPath));
            }
        }
    }

    private static SearchServiceResult Merge(
        (Guid WorkspaceId, SearchServiceResult Result)[] perSourceResults,
        IReadOnlyList<SearchDiagnostic> resolutionDiagnostics,
        SearchOptions options)
    {
        var diagnostics = new List<SearchDiagnostic>(resolutionDiagnostics);
        foreach (var (workspaceId, result) in perSourceResults)
        {
            if (result.IsSuccess)
            {
                continue;
            }

            // RunSourceAsync attaches a specific diagnostic when it caught an exception; fall back to a
            // generic one for a plain status-code failure (e.g. IndexNotFound) that carries none of its own.
            diagnostics.AddRange(result.Diagnostics.Count > 0
                ? result.Diagnostics
                : [new SearchDiagnostic(SearchDiagnosticSeverity.Warning, $"Workspace '{workspaceId}' skipped: {result.Status}")]);
        }

        var successful = perSourceResults.Where(p => p.Result.IsSuccess).ToList();
        if (successful.Count == 0)
        {
            var status = perSourceResults.Length > 0 ? perSourceResults[0].Result.Status : SearchServiceStatus.IndexNotFound;
            return SearchServiceResult.Failure(EmptyQuery(), status, diagnostics);
        }

        var taggedHits = successful
            .SelectMany(p => p.Result.Hits.Select(hit => hit with { SourceWorkspaceId = p.WorkspaceId }))
            .OrderByDescending(hit => hit.Score)
            .Take(options.MaxResults)
            .ToList();

        var mergedResult = new SearchResult
        {
            Hits = taggedHits,
            TotalHits = successful.Sum(p => p.Result.Result!.TotalHits),
            ReturnedHits = taggedHits.Count,
        };

        var executionInfo = new SearchExecutionInfo
        {
            SessionId = Guid.NewGuid(),
            ProviderName = "federated",
            Duration = TimeSpan.FromTicks(successful.Sum(p => p.Result.ExecutionInfo!.Duration.Ticks)),
            DocumentsScanned = successful.Sum(p => p.Result.ExecutionInfo!.DocumentsScanned),
            IndexVersion = $"federated:{successful.Count}-sources",
        };

        return SearchServiceResult.Success(successful[0].Result.Query, mergedResult, executionInfo) with
        {
            Diagnostics = diagnostics,
        };
    }

    private static SearchQuery EmptyQuery() =>
        new() { OriginalText = string.Empty, Root = new KeywordExpression(string.Empty) };

    [LoggerMessage(Level = LogLevel.Information, Message = "Federated query on workspace {WorkspaceId} completed in {DurationMs:F1}ms with {HitCount} hits")]
    private static partial void LogQueryCompleted(ILogger logger, Guid workspaceId, double durationMs, int hitCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Federated query on workspace {WorkspaceId} skipped a source: {Reason}")]
    private static partial void LogSourceSkipped(ILogger logger, Guid workspaceId, string reason);

    private async Task<SearchServiceResult> RunAsync(SearchOptions options, Func<ISearchService, Task<SearchServiceResult>> run)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await RunCoreAsync(options, run).ConfigureAwait(false);
        stopwatch.Stop();

        LogQueryCompleted(_logger, _workspaceId, stopwatch.Elapsed.TotalMilliseconds, result.Hits.Count);
        foreach (var diagnostic in result.Diagnostics)
        {
            LogSourceSkipped(_logger, _workspaceId, diagnostic.Message);
        }

        return result;
    }

    private async Task<SearchServiceResult> RunCoreAsync(SearchOptions options, Func<ISearchService, Task<SearchServiceResult>> run)
    {
        var entry = await _registry.ResolveAsync(_workspaceId, options.Token).ConfigureAwait(false);
        if (entry is null)
        {
            return SearchServiceResult.Failure(EmptyQuery(), SearchServiceStatus.WorkspaceNotFound, []);
        }

        var (sources, resolutionDiagnostics) = await ResolveSourcesAsync(entry, options.Token).ConfigureAwait(false);
        if (sources.Count == 0)
        {
            return SearchServiceResult.Failure(EmptyQuery(), SearchServiceStatus.IndexNotFound, resolutionDiagnostics);
        }

        var perSourceResults = await Task.WhenAll(
            sources.Select(source => RunSourceAsync(source, run))).ConfigureAwait(false);

        return Merge(perSourceResults, resolutionDiagnostics, options);
    }

    private async Task<(Guid WorkspaceId, SearchServiceResult Result)> RunSourceAsync(
        (Guid WorkspaceId, string RepoPath) source, Func<ISearchService, Task<SearchServiceResult>> run)
    {
        try
        {
            var service = _repoSearchServiceFactory.CreateForRepo(source.RepoPath);
            var result = await run(service).ConfigureAwait(false);
            return (source.WorkspaceId, result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        // Fan-out boundary: one source's I/O failure (e.g. permission denied) must degrade only that
        // source, never the whole federated query (ADR-0027 Consequences; Stabilization Sprint 1).
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        {
            return (source.WorkspaceId, SearchServiceResult.Failure(
                EmptyQuery(),
                SearchServiceStatus.IndexNotFound,
                [new SearchDiagnostic(SearchDiagnosticSeverity.Warning, $"Workspace '{source.WorkspaceId}' failed: {ex.Message}")]));
        }
#pragma warning restore CA1031
    }

    private async Task<(List<(Guid WorkspaceId, string RepoPath)> Sources, List<SearchDiagnostic> Diagnostics)> ResolveSourcesAsync(
        WorkspaceRegistryEntry entry, CancellationToken ct)
    {
        var sources = new List<(Guid, string)>();
        var diagnostics = new List<SearchDiagnostic>();
        AddRepos(sources, entry);

        foreach (var reference in entry.References)
        {
            WorkspaceRegistryEntry? referenced;
            try
            {
                referenced = await _registry.ResolveAsync(reference.WorkspaceId, ct).ConfigureAwait(false);
            }
            catch (WorkspaceRegistryCorruptException ex)
            {
                // A corrupt referenced manifest degrades only that reference, per ADR-0027 —
                // it must not corrupt the importing workspace's own query.
                diagnostics.Add(new SearchDiagnostic(
                    SearchDiagnosticSeverity.Warning,
                    $"Referenced workspace '{reference.WorkspaceId}' registry entry is corrupt: {ex.Message}"));
                continue;
            }

            if (referenced is null)
            {
                diagnostics.Add(new SearchDiagnostic(
                    SearchDiagnosticSeverity.Warning,
                    $"Referenced workspace '{reference.WorkspaceId}' not found"));
                continue;
            }

            if (reference.PinnedStateHash is not null)
            {
                var currentFingerprint = await _fingerprintProvider.ComputeFingerprintAsync(referenced, ct).ConfigureAwait(false);
                if (!string.Equals(currentFingerprint, reference.PinnedStateHash, StringComparison.Ordinal))
                {
                    // Fail closed (ADR-0027 Amendment): never serve stale or unverifiable content from
                    // a pinned reference. This degrades only this one reference, same as the other
                    // per-source failure modes above — it never fails the whole federated query.
                    var message = currentFingerprint is null
                        ? $"Referenced workspace '{reference.WorkspaceId}' is pinned but its current state could not be verified — treated as out of date"
                        : $"Referenced workspace '{reference.WorkspaceId}' is pinned but out of date (fingerprint mismatch)";
                    diagnostics.Add(new SearchDiagnostic(SearchDiagnosticSeverity.Error, message));
                    continue;
                }
            }

            AddRepos(sources, referenced);
        }

        return (sources, diagnostics);
    }
}
