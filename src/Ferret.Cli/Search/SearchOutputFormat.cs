namespace Ferret.Cli.Search;

/// <summary>Output format for <c>ferret search</c> results.</summary>
public enum SearchOutputFormat
{
    /// <summary>Human-readable text with ANSI highlighting (default).</summary>
    Text,

    /// <summary>Machine-readable JSON array of hits.</summary>
    Json,
}
