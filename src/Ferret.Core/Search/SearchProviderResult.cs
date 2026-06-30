namespace Ferret.Core.Search;

/// <summary>
/// The outcome of a single search provider execution.
/// Wraps provider-specific metadata. The search service converts this
/// into a <see cref="SearchServiceResult"/> for callers.
/// </summary>
public sealed class SearchProviderResult
{
    private SearchProviderResult()
    {
    }

    /// <summary>Gets a value indicating whether the provider executed successfully.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Gets the status when not successful.</summary>
    public SearchServiceStatus Status { get; private init; }

    /// <summary>Gets the ranked hits returned by the provider. Empty when not successful.</summary>
    public IReadOnlyList<SearchHit> Hits { get; private init; } = [];

    /// <summary>Gets the number of index documents scanned by the provider.</summary>
    public int DocumentsScanned { get; private init; }

    /// <summary>Gets the version string of the index that was queried.</summary>
    public string IndexVersion { get; private init; } = string.Empty;

    /// <summary>Provider executed successfully and returned ranked hits.</summary>
    /// <param name="hits">The ranked hits returned by the provider.</param>
    /// <param name="documentsScanned">The number of index documents scanned.</param>
    /// <param name="indexVersion">The version string of the index that was queried.</param>
    /// <returns>A successful <see cref="SearchProviderResult"/>.</returns>
    public static SearchProviderResult Success(
        IReadOnlyList<SearchHit> hits, int documentsScanned, string indexVersion) =>
        new() { IsSuccess = true, Hits = hits, DocumentsScanned = documentsScanned, IndexVersion = indexVersion };

    /// <summary>Provider failed with the given status.</summary>
    /// <param name="status">The failure status code.</param>
    /// <returns>A failed <see cref="SearchProviderResult"/>.</returns>
    public static SearchProviderResult Failure(SearchServiceStatus status) =>
        new() { IsSuccess = false, Status = status };
}
