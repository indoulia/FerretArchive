namespace Ferret.Core.Search;

/// <summary>A passage-level search hit — one result per matching passage within a file.
/// Returned when <see cref="SearchOptions.IncludePassages"/> is true (<c>--passages</c>).</summary>
public sealed record PassageSearchHit : SearchHit
{
    /// <summary>Gets the heading of the passage, if extracted by the parser. May be null.</summary>
    public string? Heading { get; init; }

    /// <summary>Gets the character offset where this passage begins within the document plain text.</summary>
    public int StartOffset { get; init; }

    /// <summary>Gets the character offset where this passage ends within the document plain text.</summary>
    public int EndOffset { get; init; }
}
