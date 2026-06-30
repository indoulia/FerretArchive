namespace Ferret.Core.Documents;

/// <summary>
/// A content parser that transforms a raw content stream into a <see cref="Document"/>.
/// Implementations MUST be sealed. CanParse is pure — no I/O, no exceptions, no side effects.
/// Parsers are responsible for assigning DocumentKind — never infer it from MediaType alone.
/// </summary>
public interface IContentParser
{
    /// <summary>Gets the static descriptor for this parser.</summary>
    ParserDescriptor Descriptor { get; }

    /// <summary>Returns true if this parser can handle the given MIME type.
    /// Pure — no I/O, never throws, always returns the same result for the same input.</summary>
    /// <param name="mediaType">The MIME type to check (e.g. "text/markdown").</param>
    /// <returns><see langword="true"/> if this parser handles the given MIME type; otherwise <see langword="false"/>.</returns>
    bool CanParse(string mediaType);

    /// <summary>Parses the content stream and produces a Document.
    /// The stream is positioned at the beginning. Do not close or dispose it.</summary>
    /// <param name="content">The raw content stream to parse.</param>
    /// <param name="context">Contextual information including the source AssetDescriptor.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The parsed <see cref="Document"/>.</returns>
    ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct = default);
}
