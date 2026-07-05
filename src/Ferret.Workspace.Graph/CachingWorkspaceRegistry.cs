using System.Collections.Concurrent;

namespace Ferret.Workspace.Graph;

/// <summary>
/// In-process, read-through cache over another <see cref="IWorkspaceRegistry"/> (WIP-032). The
/// federated query path (<c>Ferret.Knowledge.Federation.FederatedKnowledgeStore</c>'s source
/// resolution) calls <see cref="ResolveAsync"/> once per member repo and once per direct reference
/// on every query — a file-open + JSON-parse each, even when nothing has changed since the last
/// query in this process (<c>20-Phase-3-Priority-Assessment.md</c> §1/§2). This decorator caches the
/// resolved entry (or its absence) per <see cref="Guid"/> and keeps it correct by writing through on
/// <see cref="SaveAsync"/> — the registry's only mutation path, which every <c>workspaces</c> CLI
/// command (<c>add-repo</c>, <c>add-reference</c>, <c>pin-reference</c>, etc.) already funnels
/// through for the one workspace entry it modifies.
/// </summary>
/// <remarks>
/// Never a source of truth: an exception from the wrapped registry (e.g.
/// <see cref="WorkspaceRegistryCorruptException"/>) is never cached — it propagates on every call,
/// so a corrupt manifest keeps failing exactly as it did before this cache existed.
/// <see cref="ListAsync"/> passes straight through, uncached; it is not on the federated query hot
/// path and its own directory scan already dominates its cost. In-memory only — a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> instance field, lost on process exit, never
/// persisted or shared across processes.
/// </remarks>
public sealed class CachingWorkspaceRegistry : IWorkspaceRegistry
{
    private readonly IWorkspaceRegistry _inner;
    private readonly ConcurrentDictionary<Guid, WorkspaceRegistryEntry?> _cache = new();

    /// <summary>Initializes a new instance of the <see cref="CachingWorkspaceRegistry"/> class.</summary>
    /// <param name="inner">The registry read through to on a cache miss and written through to on every save.</param>
    public CachingWorkspaceRegistry(IWorkspaceRegistry inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc/>
    public async Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(workspaceId, out var cached))
        {
            return cached;
        }

        var entry = await _inner.ResolveAsync(workspaceId, ct).ConfigureAwait(false);
        _cache[workspaceId] = entry;
        return entry;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
        _inner.ListAsync(ct);

    /// <inheritdoc/>
    public async Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _inner.SaveAsync(entry, ct).ConfigureAwait(false);

        // Refresh only an already-cached entry, so a cache warmed solely by this save (never yet
        // resolved in this process) still costs exactly one ResolveAsync read-through, same as any
        // other never-resolved workspace — this class caches read history, not write history.
        if (_cache.ContainsKey(entry.WorkspaceId))
        {
            _cache[entry.WorkspaceId] = entry;
        }
    }
}
