using System.Globalization;
using System.Text;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.Parsers.Office;

/// <summary>
/// Content parser for OpenXML spreadsheets (.xlsx). Extracts searchable enterprise knowledge —
/// sheet names, header rows, and cell values — using the streaming OpenXmlReader (SAX) to bound
/// memory on large exports. Reads cached cell values only: no formula evaluation, no calculation.
/// </summary>
public sealed class ExcelParser : IContentParser
{
    private static readonly ParserDescriptor XlsxDescriptor = new()
    {
        Id = new ParserId(OfficeMediaTypes.Xlsx),
        Name = "Excel (XLSX) Parser",
        Version = "1.0",
        SupportedMediaTypes = [OfficeMediaTypes.Xlsx],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.MetadataExtraction],
        Priority = 200,
    };

    private readonly ParserOptions _options;

    /// <summary>Initializes a new instance of the <see cref="ExcelParser"/> class.</summary>
    /// <param name="options">Host-configurable parser options (extraction limit).</param>
    public ExcelParser(ParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => XlsxDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.Equals(OfficeMediaTypes.Xlsx, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        using var spreadsheet = SpreadsheetDocument.Open(content, isEditable: false);
        var wbPart = spreadsheet.WorkbookPart;
        var shared = ReadSharedStrings(wbPart);

        var sb = new StringBuilder();
        var sheetCount = 0;
        var limit = _options.MaxExtractedCharacters;

        var sheets = wbPart?.Workbook?.Sheets?.Elements<Sheet>() ?? [];
        foreach (var sheet in sheets)
        {
            ct.ThrowIfCancellationRequested();
            sheetCount++;
            sb.Append("# ").AppendLine(sheet.Name);

            if (sheet.Id?.Value is null || wbPart!.GetPartById(sheet.Id!.Value!) is not WorksheetPart wsPart)
            {
                continue;
            }

            using var reader = OpenXmlReader.Create(wsPart);
            while (reader.Read())
            {
                if (reader.ElementType != typeof(Row))
                {
                    continue;
                }

                var cells = new List<string>();
                if (reader.ReadFirstChild())
                {
                    do
                    {
                        if (reader.ElementType == typeof(Cell) && reader.LoadCurrentElement() is Cell cell)
                        {
                            var value = ResolveCell(cell, shared);
                            if (!string.IsNullOrEmpty(value))
                            {
                                cells.Add(value);
                            }
                        }
                    }
                    while (reader.ReadNextSibling());
                }

                if (cells.Count > 0)
                {
                    sb.AppendLine(string.Join('\t', cells));
                }

                if (limit is long max && sb.Length >= max)
                {
                    break; // stop early once the configured limit is reached
                }
            }

            if (limit is long capped && sb.Length >= capped)
            {
                break;
            }
        }

        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit(sb.ToString().Trim(), _options);
        var metadata = BuildMetadata(spreadsheet, sheetCount, truncated);

        var document = new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = OfficeMediaTypes.Xlsx,
            Kind = DocumentKind.Data,
            PlainText = text,
            Title = string.IsNullOrWhiteSpace(spreadsheet.PackageProperties.Title) ? null : spreadsheet.PackageProperties.Title,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
            Metadata = metadata,
        };

        return ValueTask.FromResult(document);
    }

    private static string ResolveCell(Cell cell, string[] shared)
    {
        // Read the cell's stored/cached value only. A formula cell (CellFormula) exposes its cached
        // result in CellValue; we index that. We never read CellFormula, so the formula expression
        // (e.g. "=SUM(A1:A50)") is never emitted. If the cache is absent, the value is empty and skipped.
        var raw = cell.CellValue?.InnerText;
        if (string.IsNullOrEmpty(raw))
        {
            return cell.InlineString?.Text?.Text ?? string.Empty;
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
                && idx >= 0 && idx < shared.Length
                ? shared[idx]
                : string.Empty;
        }

        // Number, boolean, or cached formula result — emitted as stored (no recomputation).
        return raw;
    }

    private static string[] ReadSharedStrings(WorkbookPart? wbPart)
    {
        var table = wbPart?.SharedStringTablePart?.SharedStringTable;
        return table is null
            ? []
            : table.Elements<SharedStringItem>().Select(item => item.InnerText).ToArray();
    }

    private static Dictionary<string, string> BuildMetadata(
        SpreadsheetDocument spreadsheet, int sheetCount, bool truncated)
    {
        var props = spreadsheet.PackageProperties;
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DocumentMetadata.SheetCount] = sheetCount.ToString(CultureInfo.InvariantCulture),
        };

        if (truncated)
        {
            map[DocumentMetadata.Truncated] = "true";
        }

        Add(map, DocumentMetadata.Author, props.Creator);
        Add(map, DocumentMetadata.Category, props.Category);
        Add(map, DocumentMetadata.Created, props.Created?.ToString("o", CultureInfo.InvariantCulture));
        Add(map, DocumentMetadata.Modified, props.Modified?.ToString("o", CultureInfo.InvariantCulture));
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
