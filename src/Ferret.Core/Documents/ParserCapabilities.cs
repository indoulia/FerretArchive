namespace Ferret.Core.Documents;

/// <summary>Well-known parser capabilities as immutable singletons.
/// Use these instead of constructing new <see cref="ParserCapability"/> instances.</summary>
public static class ParserCapabilities
{
    /// <summary>Parser extracts full plain text for keyword indexing. All built-in parsers provide this.</summary>
    public static readonly ParserCapability PlainTextExtraction =
        new(
            "plain-text",
            "Plain Text Extraction",
            "1.0",
            "Extracts the full plain-text representation of the document for keyword indexing.");

    /// <summary>Parser extracts structural sections (e.g. Markdown headings, notebook cells).</summary>
    public static readonly ParserCapability SectionExtraction =
        new(
            "section-extraction",
            "Section Extraction",
            "1.0",
            "Extracts logically distinct sections as DocumentSection entries.");

    /// <summary>Parser extracts structured metadata (e.g. JSON properties, YAML front matter).</summary>
    public static readonly ParserCapability MetadataExtraction =
        new(
            "metadata-extraction",
            "Metadata Extraction",
            "1.0",
            "Extracts key-value metadata into Document.Metadata.");

    /// <summary>Parser extracts hyperlinks or cross-references. Reserved for future sprints.</summary>
    public static readonly ParserCapability LinkExtraction =
        new(
            "link-extraction",
            "Link Extraction",
            "1.0",
            "Extracts hyperlinks and cross-references from content.");

    /// <summary>Reserved: parser produces richer structured extraction (tables, slides, mail parts).
    /// Unused this milestone — declared so future parsers (OCR, PowerPoint, Outlook) can advertise it
    /// without a contract change.</summary>
    public static readonly ParserCapability StructuredExtraction =
        new(
            "structured-extraction",
            "Structured Extraction",
            "1.0",
            "Extracts structured content (tables, slides, message parts) beyond flat text.");

    /// <summary>Gets all well-known capabilities in definition order.</summary>
    public static IReadOnlyList<ParserCapability> All { get; } = [
        PlainTextExtraction, SectionExtraction, MetadataExtraction, LinkExtraction,
    ];
}
