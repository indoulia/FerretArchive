namespace Ferret.Core.Search;

/// <summary>
/// An immutable segment of text within a <see cref="HighlightedText"/>, tagged with a display kind.
/// The provider assigns the kind; the renderer applies formatting.
/// </summary>
/// <param name="Text">The text content of this span.</param>
/// <param name="Kind">The display classification of this span.</param>
public sealed record TextSpan(string Text, TextSpanKind Kind);
