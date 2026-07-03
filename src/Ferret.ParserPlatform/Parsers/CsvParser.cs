using System.Globalization;
using System.Text;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Structure-aware parser for CSV and TSV (<c>text/csv</c>, <c>text/tab-separated-values</c>).
/// Dependency-free; lives in the platform beside JSON/Markdown. Emits header + data rows so column
/// tokens are searchable, and lightweight metadata (row/column counts, has-header). Read-only;
/// no chunking, embedding, or AI processing.
/// </summary>
public sealed class CsvParser : IContentParser
{
    private const string CsvMediaType = "text/csv";
    private const string TsvMediaType = "text/tab-separated-values";

    private static readonly ParserDescriptor CsvDescriptor = new()
    {
        Id = new ParserId("text/csv"),
        Name = "CSV Parser",
        Version = "1.0",
        SupportedMediaTypes = [CsvMediaType, TsvMediaType],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.MetadataExtraction],
        Priority = 200, // beats PlainTextParser (100) for text/csv and text/tab-separated-values
    };

    private readonly ParserOptions _options;

    /// <summary>Initializes a new instance of the <see cref="CsvParser"/> class.</summary>
    /// <param name="options">Host-configurable parser options (extraction limit).</param>
    public CsvParser(ParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => CsvDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.Equals(CsvMediaType, StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals(TsvMediaType, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        var mediaType = context.Asset.MediaType ?? CsvMediaType;
        var delimiter = mediaType.Equals(TsvMediaType, StringComparison.OrdinalIgnoreCase) ? '\t' : ',';

        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var sb = new StringBuilder();
        IReadOnlyList<string>? header = null;
        var dataRowCount = 0;

        foreach (var record in CsvRecordReader.ReadRecords(raw, delimiter))
        {
            ct.ThrowIfCancellationRequested();

            if (header is null)
            {
                header = record; // first record is treated as the header
            }
            else
            {
                dataRowCount++;
            }

            var joined = string.Join('\t', record.Where(f => !string.IsNullOrEmpty(f)));
            if (joined.Length > 0)
            {
                sb.AppendLine(joined);
            }
        }

        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit(sb.ToString().Trim(), _options);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DocumentMetadata.HasHeader] = header is not null ? "true" : "false",
            [DocumentMetadata.RowCount] = dataRowCount.ToString(CultureInfo.InvariantCulture),
            [DocumentMetadata.ColumnCount] = (header?.Count ?? 0).ToString(CultureInfo.InvariantCulture),
        };

        if (truncated)
        {
            metadata[DocumentMetadata.Truncated] = "true";
        }

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = mediaType,
            Kind = DocumentKind.Data,
            PlainText = text,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
            Metadata = metadata,
        };
    }
}
