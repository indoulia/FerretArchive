namespace Ferret.Cli.Search;

/// <summary>Parsed arguments for the <c>ferret search &lt;query&gt;</c> command.</summary>
public sealed record SearchCommandArgs
{
    /// <summary>Gets the raw query string typed by the user.</summary>
    public required string Query { get; init; }

    /// <summary>Gets the maximum number of results to return.</summary>
    public int Limit { get; init; } = 20;

    /// <summary>Gets a value indicating whether to return passage-level results instead of file-level.</summary>
    public bool Passages { get; init; }

    /// <summary>Gets a value indicating whether to strip ANSI highlighting from output.</summary>
    public bool NoHighlight { get; init; }

    /// <summary>Gets the output format: text (default) or json.</summary>
    public SearchOutputFormat Format { get; init; } = SearchOutputFormat.Text;
}
