namespace Ferret.Core.Search;

/// <summary>
/// Classifies the granularity of a search result.
/// Sprint 10: <see cref="File"/> (default) and <see cref="Passage"/> (<c>--passages</c>).
/// <see cref="Segment"/> is reserved for Sprint 11 semantic search.
/// </summary>
public enum SearchHitKind
{
    /// <summary>Result represents an entire file — the best-matching snippet is surfaced.</summary>
    File = 0,

    /// <summary>Result represents a human-readable passage (heading + body block).</summary>
    Passage = 1,

    /// <summary>Reserved: AI processing unit (embedding chunk, notebook cell, AST node). Sprint 11+.</summary>
    Segment = 2,
}
