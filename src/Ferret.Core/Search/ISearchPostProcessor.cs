namespace Ferret.Core.Search;

/// <summary>
/// A post-processing stage applied to search results after provider execution.
/// Sprint 10 ships zero implementations; the interface is registered for future use.
/// Examples: AI reranker, deduplicator, permission filter, knowledge boost.
/// </summary>
public interface ISearchPostProcessor
{
    /// <summary>
    /// Processes the hit list and returns a (possibly filtered/reordered) result.
    /// Must not throw for expected conditions.
    /// </summary>
    /// <param name="hits">The ranked hits from the provider.</param>
    /// <param name="query">The parsed query that drove the search.</param>
    /// <param name="options">The search options for this request.</param>
    /// <returns>A (possibly filtered/reordered) list of hits.</returns>
    Task<IReadOnlyList<SearchHit>> ProcessAsync(
        IReadOnlyList<SearchHit> hits,
        SearchQuery query,
        SearchOptions options);
}
