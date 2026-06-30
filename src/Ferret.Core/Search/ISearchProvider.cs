namespace Ferret.Core.Search;

/// <summary>
/// A search provider that executes queries against a backing store and returns ranked hits.
/// Implementations: BM25SearchProvider (Sprint 10), SemanticSearchProvider (Sprint 11+), HybridSearchProvider (Sprint 12+).
/// All providers receive the SearchQuery AST — no provider knows the raw query string.
/// </summary>
public interface ISearchProvider
{
    /// <summary>Gets the static descriptor for this provider, including metadata and capabilities.</summary>
    SearchProviderDescriptor Descriptor { get; }

    /// <summary>Gets the capabilities this provider supports.</summary>
    SearchCapabilities Capabilities { get; }

    /// <summary>
    /// Executes the query against the backing store and returns a provider result.
    /// The provider translates the SearchQuery AST to backend-specific syntax internally.
    /// Returns SearchProviderResult.Failure for expected conditions; never throws for expected cases.
    /// </summary>
    /// <param name="query">The parsed query AST.</param>
    /// <param name="options">Execution options including limits, highlighting, and mode.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="SearchProviderResult"/> with hits on success or a failure status.</returns>
    Task<SearchProviderResult> SearchAsync(SearchQuery query, SearchOptions options, CancellationToken ct = default);
}
