namespace Ferret.Core.Search;

/// <summary>
/// Controls which search provider(s) the search service uses for a request.
/// Sprint 10 always uses <see cref="Keyword"/>. Future values are reserved.
/// </summary>
public enum SearchExecutionMode
{
    /// <summary>Reserved: automatically select the best available provider based on query and capabilities. Sprint 11+.</summary>
    Auto = 0,

    /// <summary>Use the BM25/FTS5 keyword search provider.</summary>
    Keyword = 1,

    /// <summary>Reserved: use the semantic (embedding) search provider. Sprint 11+.</summary>
    Semantic = 2,

    /// <summary>Reserved: use both keyword and semantic providers, fused by a post-processor. Sprint 12+.</summary>
    Hybrid = 3,
}
