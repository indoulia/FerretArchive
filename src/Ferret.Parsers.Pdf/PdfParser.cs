using System.Globalization;
using System.Text;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

using UglyToad.PdfPig;

namespace Ferret.Parsers.Pdf;

/// <summary>
/// Content parser for <c>application/pdf</c> using UglyToad.PdfPig. Extracts page text in order
/// plus lightweight document metadata. Read-only; performs no chunking, embedding, or AI processing.
/// </summary>
public sealed class PdfParser : IContentParser
{
    /// <summary>The media type this parser handles.</summary>
    public const string PdfMediaType = "application/pdf";

    private static readonly ParserDescriptor PdfDescriptor = new()
    {
        Id = new ParserId(PdfMediaType),
        Name = "PDF Parser",
        Version = "1.0",
        SupportedMediaTypes = [PdfMediaType],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.MetadataExtraction],
        Priority = 200,
    };

    private readonly ParserOptions _options;

    /// <summary>Initializes a new instance of the <see cref="PdfParser"/> class.</summary>
    /// <param name="options">Host-configurable parser options (extraction limit).</param>
    public PdfParser(ParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => PdfDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.Equals(PdfMediaType, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        // PdfPig's Open is synchronous and cannot be cancelled mid-open; honor an already-cancelled
        // token before paying the (potentially large) open cost. Per-page cancellation follows below.
        ct.ThrowIfCancellationRequested();

        // PdfPig is synchronous and reads the whole stream; wrap the result in a completed ValueTask.
        using var pdf = PdfDocument.Open(content);

        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }

        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit(sb.ToString().Trim(), _options);
        var metadata = BuildMetadata(pdf, truncated);

        var document = new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = PdfMediaType,
            Kind = DocumentKind.Prose,
            PlainText = text,
            Title = string.IsNullOrWhiteSpace(pdf.Information.Title) ? null : pdf.Information.Title,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
            Metadata = metadata,
        };

        return ValueTask.FromResult(document);
    }

    private static Dictionary<string, string> BuildMetadata(PdfDocument pdf, bool truncated)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DocumentMetadata.PageCount] = pdf.NumberOfPages.ToString(CultureInfo.InvariantCulture),
        };

        if (truncated)
        {
            map[DocumentMetadata.Truncated] = "true";
        }

        Add(map, DocumentMetadata.Author, pdf.Information.Author);
        Add(map, DocumentMetadata.Subject, pdf.Information.Subject);
        Add(map, DocumentMetadata.Keywords, pdf.Information.Keywords);
        Add(map, DocumentMetadata.Created, pdf.Information.CreationDate);
        Add(map, DocumentMetadata.Modified, pdf.Information.ModifiedDate);
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
