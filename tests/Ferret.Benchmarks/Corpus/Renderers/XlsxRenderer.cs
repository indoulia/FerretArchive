using System.Globalization;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument's tables as a real .xlsx (one worksheet per table).
/// Text cells use the shared-string table; Number/Boolean/Date cells use typed inline values so the
/// ExcelParser's non-shared-string value path is exercised.</summary>
public sealed class XlsxRenderer : IDocumentRenderer
{
    private static readonly DateTime FixedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc/>
    public string Extension => ".xlsx";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        using var spreadsheet = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook, autoSave: true);
        var wbPart = spreadsheet.AddWorkbookPart();
        wbPart.Workbook = new Workbook();

        var sstPart = wbPart.AddNewPart<SharedStringTablePart>();
        var sst = new SharedStringTable();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);

        int Intern(string s)
        {
            if (index.TryGetValue(s, out var i))
            {
                return i;
            }

            i = index.Count;
            index[s] = i;
            sst.Append(new SharedStringItem(new Text(s)));
            return i;
        }

        Cell SharedCell(string s) => new()
        {
            DataType = CellValues.SharedString,
            CellValue = new CellValue(Intern(s).ToString(CultureInfo.InvariantCulture)),
        };

        Cell TypedCell(CorpusCell c) => c.Kind switch
        {
            CorpusCellKind.Number => new Cell { CellValue = new CellValue(c.Value) }, // numeric default
            CorpusCellKind.Boolean => new Cell { DataType = CellValues.Boolean, CellValue = new CellValue(c.Value == "true" ? "1" : "0") },
            CorpusCellKind.Date => new Cell { CellValue = new CellValue(ToSerial(c.Value)) }, // Excel date serial
            _ => SharedCell(c.Value), // Text (Empty is filtered out before this)
        };

        var sheets = wbPart.Workbook.AppendChild(new Sheets());
        uint sheetId = 1;

        // Fall back to a single sheet built from the title when the doc has no tables.
        var tables = doc.Tables.Count > 0
            ? doc.Tables
            : [new CorpusTable(["Title"], [[CorpusCell.Text(doc.Title)]])];

        foreach (var t in tables)
        {
            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            var headerRow = new Row();
            foreach (var h in t.Headers)
            {
                headerRow.Append(SharedCell(h));
            }

            sheetData.Append(headerRow);

            foreach (var row in t.Rows)
            {
                var r = new Row();
                foreach (var c in row)
                {
                    if (c.Kind == CorpusCellKind.Empty)
                    {
                        continue; // skip empties (exercises parser skip logic)
                    }

                    r.Append(TypedCell(c));
                }

                sheetData.Append(r);
            }

            wsPart.Worksheet = new Worksheet(sheetData);
            sheets.Append(new Sheet
            {
                Id = wbPart.GetIdOfPart(wsPart),
                SheetId = sheetId,
                Name = string.Create(CultureInfo.InvariantCulture, $"Sheet{sheetId}"),
            });
            sheetId++;
        }

        sstPart.SharedStringTable = sst;

        var props = spreadsheet.PackageProperties;
        props.Title = doc.Title;
        props.Creator = doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author) ? author : "Synthetic Corpus Generator";
        props.Category = doc.Metadata.TryGetValue(DocumentMetadata.Category, out var cat) ? cat : null;
        props.Created = FixedTimestamp;   // pinned for determinism
        props.Modified = FixedTimestamp;
    }

    // Excel stores dates as a serial number (days since 1899-12-30). The ExcelParser surfaces the
    // serial as text — the documented v1 limitation — so this deliberately exercises that path.
    private static string ToSerial(string iso)
    {
        var date = DateOnly.ParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var serial = date.ToDateTime(TimeOnly.MinValue).ToOADate();
        return serial.ToString(CultureInfo.InvariantCulture);
    }
}
