namespace Ferret.Cli.Search;

/// <summary>
/// Applies visual emphasis to text for terminal output.
/// Sprint 10 implementation: ANSI escape codes (<see cref="AnsiTextStyler"/>).
/// No-op implementation: <see cref="NullTextStyler"/> — used for <c>--no-highlight</c>.
/// Reserved: SpectreConsoleStyler (future dedicated CLI UX sprint).
/// </summary>
public interface ITextStyler
{
    /// <summary>
    /// Applies match/highlight emphasis (bold in ANSI).
    /// </summary>
    /// <param name="text">The text to emphasize.</param>
    /// <returns>The text with match styling applied.</returns>
    string Match(string text);

    /// <summary>
    /// Applies muted/dim emphasis for metadata (path, score, timing).
    /// </summary>
    /// <param name="text">The text to mute.</param>
    /// <returns>The text with muted styling applied.</returns>
    string Muted(string text);

    /// <summary>
    /// Returns text without modification.
    /// </summary>
    /// <param name="text">The text to return as-is.</param>
    /// <returns>The unmodified text.</returns>
    string Normal(string text);
}
