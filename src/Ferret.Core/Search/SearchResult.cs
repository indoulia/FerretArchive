namespace Ferret.Core.Search;

/// <summary>
/// The raw output of a single provider execution — hits ranked by score.
/// Wrapped in SearchServiceResult by SearchService before returning to callers.
/// </summary>
public sealed record SearchResult
{
    /// <summary>Gets the ranked hits returned by the provider.</summary>
    public required IReadOnlyList<SearchHit> Hits { get; init; }

    /// <summary>Gets the total number of matching documents in the index (may exceed <see cref="ReturnedHits"/>).</summary>
    public required int TotalHits { get; init; }

    /// <summary>Gets the number of hits actually returned (capped by <see cref="SearchOptions.MaxResults"/>).</summary>
    public required int ReturnedHits { get; init; }

    /// <summary>Gets a shared empty result — zero hits, used for no-match responses.</summary>
    public static SearchResult Empty { get; } = new() { Hits = [], TotalHits = 0, ReturnedHits = 0 };
}
