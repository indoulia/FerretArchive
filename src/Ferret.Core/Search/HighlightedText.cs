namespace Ferret.Core.Search;

/// <summary>
/// A sequence of <see cref="TextSpan"/> values representing snippet text with semantic highlight markers.
/// Produced by the provider's internal <c>HighlightEngine</c>; consumed by renderers.
/// No renderer knows the backend markup format (e.g. FTS5 snippet syntax) that produced this model.
/// </summary>
public sealed class HighlightedText
{
    /// <summary>Gets a shared empty instance with no spans.</summary>
    public static HighlightedText Empty { get; } = new() { Spans = [] };

    /// <summary>Gets the ordered spans that compose this highlighted text.</summary>
    public required IReadOnlyList<TextSpan> Spans { get; init; }

    /// <summary>Creates a <see cref="HighlightedText"/> from a single plain (un-highlighted) string.</summary>
    /// <param name="text">The plain text content.</param>
    /// <returns>A <see cref="HighlightedText"/> with a single normal span containing the text.</returns>
    public static HighlightedText Plain(string text) =>
        new() { Spans = [new TextSpan(text, TextSpanKind.Normal)] };
}
