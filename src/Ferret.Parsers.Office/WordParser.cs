using System.Text;

using DocumentFormat.OpenXml.Packaging;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

// Alias only the one Wordprocessing type used here; importing the whole namespace would make the
// bare name `Document` ambiguous with Ferret.Core.Documents.Document (the parser's return type).
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Ferret.Parsers.Office;

/// <summary>
/// Content parser for OpenXML Word documents (.docx) using DocumentFormat.OpenXml.
/// Extracts body paragraphs, table cell text, headers, and footers, plus lightweight metadata.
/// Read-only; performs no chunking, embedding, or AI processing. Legacy binary .doc is not supported.
/// </summary>
public sealed class WordParser : IContentParser
{
    private static readonly ParserDescriptor WordDescriptor = new()
    {
        Id = new ParserId(OfficeMediaTypes.Docx),
        Name = "Word (DOCX) Parser",
        Version = "1.0",
        SupportedMediaTypes = [OfficeMediaTypes.Docx],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.MetadataExtraction],
        Priority = 200,
    };

    private readonly ParserOptions _options;

    /// <summary>Initializes a new instance of the <see cref="WordParser"/> class.</summary>
    /// <param name="options">Host-configurable parser options (extraction limit).</param>
    public WordParser(ParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => WordDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.Equals(OfficeMediaTypes.Docx, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        using var word = WordprocessingDocument.Open(content, isEditable: false);
        var main = word.MainDocumentPart;

        var sb = new StringBuilder();

        // Headers (document order is not guaranteed across parts; emit headers, body, footers).
        if (main is not null)
        {
            foreach (var headerPart in main.HeaderParts)
            {
                AppendText(headerPart.Header, sb, ct);
            }

            AppendText(main.Document?.Body, sb, ct);

            foreach (var footerPart in main.FooterParts)
            {
                AppendText(footerPart.Footer, sb, ct);
            }
        }

        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit(sb.ToString().Trim(), _options);
        var metadata = BuildMetadata(word, truncated);
        var props = word.PackageProperties;

        var document = new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = OfficeMediaTypes.Docx,
            Kind = DocumentKind.Prose,
            PlainText = text,
            Title = string.IsNullOrWhiteSpace(props.Title) ? null : props.Title,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
            Metadata = metadata,
        };

        return ValueTask.FromResult(document);
    }

    private static void AppendText(DocumentFormat.OpenXml.OpenXmlElement? root, StringBuilder sb, CancellationToken ct)
    {
        if (root is null)
        {
            return;
        }

        // Text elements appear in document order within a part, covering paragraphs and table cells.
        foreach (var text in root.Descendants<WordText>())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(text.Text);
        }
    }

    private static Dictionary<string, string> BuildMetadata(WordprocessingDocument word, bool truncated)
    {
        var props = word.PackageProperties;
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (truncated)
        {
            map[DocumentMetadata.Truncated] = "true";
        }

        Add(map, DocumentMetadata.Author, props.Creator);
        Add(map, DocumentMetadata.Subject, props.Subject);
        Add(map, DocumentMetadata.Keywords, props.Keywords);
        Add(map, DocumentMetadata.Category, props.Category);
        Add(map, DocumentMetadata.Created, props.Created?.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
        Add(map, DocumentMetadata.Modified, props.Modified?.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
        return map;
    }

    private static void Add(Dictionary<string, string> map, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            map[key] = value;
        }
    }
}
