using System.Collections.Concurrent;

using Ferret.Core.Search;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// The process-local store backing <see cref="CachingFederatedKnowledgeStore"/> (WIP-030/031). A
/// thin, injectable wrapper around a <see cref="ConcurrentDictionary{TKey,TValue}"/> so the cache
/// can be registered once as a singleton (surviving across the per-query <see
/// cref="CachingFederatedKnowledgeStore"/> instances a long-lived host constructs) while remaining
/// unit-testable without touching dependency injection.
/// </summary>
/// <remarks>
/// In-memory only, unbounded, never persisted — lost on process exit, exactly like <see
/// cref="Ferret.Workspace.Graph.CachingWorkspaceRegistry"/> (WIP-032). A cache-key collision here
/// only ever happens for a genuinely identical query against a genuinely unchanged set of
/// participating workspaces (see <see cref="CachingFederatedKnowledgeStore"/> for key
/// construction), so no eviction or TTL is needed for correctness — a stale entry is unreachable
/// by construction because a state change always produces a different key.
/// </remarks>
public sealed class FederatedQueryCache
{
    private readonly ConcurrentDictionary<string, SearchServiceResult> _cache = new(StringComparer.Ordinal);

    /// <summary>Attempts to retrieve a previously cached result for the given key.</summary>
    /// <param name="key">The cache key, as built by <see cref="CachingFederatedKnowledgeStore"/>.</param>
    /// <param name="result">The cached result, if found.</param>
    /// <returns><see langword="true"/> if a cached result exists for <paramref name="key"/>.</returns>
    public bool TryGet(string key, out SearchServiceResult result) => _cache.TryGetValue(key, out result!);

    /// <summary>Stores a result under the given key, overwriting any previous entry.</summary>
    /// <param name="key">The cache key, as built by <see cref="CachingFederatedKnowledgeStore"/>.</param>
    /// <param name="result">The derived query result to cache. Never a source of truth — always reproducible by re-running the query.</param>
    public void Set(string key, SearchServiceResult result) => _cache[key] = result;
}
