namespace Ferret.Core.Documents;

/// <summary>
/// A logically distinct section within a Document, extracted by the parser.
/// Sprint 9: H1 and H2 Markdown headings. Future parsers may extract any structural heading.
/// </summary>
/// <param name="Title">The section title extracted by the parser (e.g. a Markdown heading). May be null.</param>
/// <param name="Content">The plain-text content of this section.</param>
/// <param name="StartLine">The 1-based source line number where this section begins.</param>
/// <param name="EndLine">The 1-based source line number where this section ends (inclusive).</param>
public sealed record DocumentSection(string? Title, string Content, int StartLine, int EndLine);
