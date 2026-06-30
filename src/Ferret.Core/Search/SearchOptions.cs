namespace Ferret.Core.Search;

/// <summary>
/// Controls how a search request executes. Separate from <see cref="SearchQuery"/> (what the user wants)
/// so the same query can be executed differently by CLI, MCP, REST, and programmatic callers.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>Gets a shared default instance with all defaults applied.</summary>
    public static SearchOptions Default { get; } = new();

    /// <summary>Gets the maximum number of hits to return. Default: 10.</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>Gets a value indicating whether to return passage-level hits instead of file-level hits.</summary>
    public bool IncludePassages { get; init; }

    /// <summary>Gets a value indicating whether to apply ANSI/HTML highlight markers to snippets. Default: true.</summary>
    public bool HighlightEnabled { get; init; } = true;

    /// <summary>Gets the maximum character length of each snippet. Default: 160.</summary>
    public int SnippetLength { get; init; } = 160;

    /// <summary>Gets the execution mode controlling provider selection. Default: <see cref="SearchExecutionMode.Keyword"/>.</summary>
    public SearchExecutionMode Mode { get; init; } = SearchExecutionMode.Keyword;

    /// <summary>Gets the cancellation token for this request.</summary>
    public CancellationToken Token { get; init; } = CancellationToken.None;
}
