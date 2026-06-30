namespace Ferret.Cli.Search;

/// <summary>
/// Implements <see cref="ITextStyler"/> using ANSI terminal escape sequences.
/// Bold (<c>ESC[1m</c>) for matches; dim (<c>ESC[2m</c>) for metadata; reset (<c>ESC[0m</c>) after each span.
/// </summary>
public sealed class AnsiTextStyler : ITextStyler
{
    private const string Bold = "\x1B[1m";
    private const string Dim = "\x1B[2m";
    private const string Reset = "\x1B[0m";

    /// <inheritdoc/>
    public string Match(string text) => $"{Bold}{text}{Reset}";

    /// <inheritdoc/>
    public string Muted(string text) => $"{Dim}{text}{Reset}";

    /// <inheritdoc/>
    public string Normal(string text) => text;
}
