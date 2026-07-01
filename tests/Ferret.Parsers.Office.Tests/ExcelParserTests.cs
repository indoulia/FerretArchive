using System.Globalization;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;

namespace Ferret.Parsers.Office.Tests;

public sealed class ExcelParserTests
{
    private static AssetDescriptor Asset() => new()
    {
        Id = AssetId.From(new Uri("filesystem:///bugs.xlsx")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///bugs.xlsx"),
        DisplayName = "bugs.xlsx",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = OfficeMediaTypes.Xlsx,
    };

    // Builds a single-sheet .xlsx using the shared-string table, exercising the SharedString path.
    private static MemoryStream MakeXlsx(string sheetName, string[][] rows)
    {
        var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, autoSave: true))
        {
            var wbPart = doc.AddWorkbookPart();
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

            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            foreach (var row in rows)
            {
                var r = new Row();
                foreach (var cellText in row)
                {
                    r.Append(new Cell
                    {
                        DataType = CellValues.SharedString,
                        CellValue = new CellValue(Intern(cellText).ToString(CultureInfo.InvariantCulture)),
                    });
                }

                sheetData.Append(r);
            }

            wsPart.Worksheet = new Worksheet(sheetData);
            sstPart.SharedStringTable = sst;

            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = sheetName });
        }

        ms.Position = 0;
        return ms;
    }

    // Builds a single-sheet workbook with typed cells to exercise the non-shared-string value path.
    private static MemoryStream BuildTypedXlsx(object?[][] rows, string sheetName = "Data")
    {
        var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, autoSave: true))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();

            var sstPart = wbPart.AddNewPart<SharedStringTablePart>();
            var sst = new SharedStringTable();
            var idx = new Dictionary<string, int>(StringComparer.Ordinal);
            int Intern(string s)
            {
                if (idx.TryGetValue(s, out var i))
                {
                    return i;
                }

                i = idx.Count;
                idx[s] = i;
                sst.Append(new SharedStringItem(new Text(s)));
                return i;
            }

            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            foreach (var row in rows)
            {
                var r = new Row();
                foreach (var value in row)
                {
                    switch (value)
                    {
                        case null: continue; // empty cell — omitted, exercises the parser's skip logic
                        case string s: r.Append(new Cell { DataType = CellValues.SharedString, CellValue = new CellValue(Intern(s).ToString(CultureInfo.InvariantCulture)) }); break;
                        case bool b: r.Append(new Cell { DataType = CellValues.Boolean, CellValue = new CellValue(b ? "1" : "0") }); break;
                        case int n: r.Append(new Cell { CellValue = new CellValue(n.ToString(CultureInfo.InvariantCulture)) }); break;
                        case double d: r.Append(new Cell { CellValue = new CellValue(d.ToString(CultureInfo.InvariantCulture)) }); break;
                        case DateOnly dt: r.Append(new Cell { CellValue = new CellValue(dt.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture)) }); break; // Excel date serial
                        case Formula f: r.Append(new Cell { CellFormula = new CellFormula(f.Expression), CellValue = new CellValue(f.CachedValue) }); break;
                        default: throw new ArgumentException($"Unsupported cell value: {value}");
                    }
                }

                sheetData.Append(r);
            }

            wsPart.Worksheet = new Worksheet(sheetData);
            sstPart.SharedStringTable = sst;
            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = sheetName });
        }

        ms.Position = 0;
        return ms;
    }

    // Builds a multi-sheet workbook, one shared-string cell per sheet. Uses a single workbook-level
    // shared-string table (OpenXml 3.1.0 rejects a SharedStringTablePart attached to a worksheet part),
    // which is also what the parser's ReadSharedStrings reads.
    private static MemoryStream BuildMultiSheetXlsx((string Name, string Cell)[] sheets)
    {
        var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, autoSave: true))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();

            var sstPart = wbPart.AddNewPart<SharedStringTablePart>();
            var sst = new SharedStringTable();
            var sheetsElement = wbPart.Workbook.AppendChild(new Sheets());
            uint id = 1;
            foreach (var (name, cellText) in sheets)
            {
                var stringIndex = (int)(id - 1);
                sst.Append(new SharedStringItem(new Text(cellText)));

                var wsPart = wbPart.AddNewPart<WorksheetPart>();
                wsPart.Worksheet = new Worksheet(new SheetData(new Row(new Cell
                {
                    DataType = CellValues.SharedString,
                    CellValue = new CellValue(stringIndex.ToString(CultureInfo.InvariantCulture)),
                })));
                sheetsElement.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = id, Name = name });
                id++;
            }

            sstPart.SharedStringTable = sst;
        }

        ms.Position = 0;
        return ms;
    }

    // Builds a workbook whose package properties carry known metadata.
    private static MemoryStream BuildXlsxWithProps(string author, string category)
    {
        var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, autoSave: true))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            wsPart.Worksheet = new Worksheet(new SheetData());
            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Data" });
            doc.PackageProperties.Creator = author;
            doc.PackageProperties.Category = category;
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void CanParse_True_For_Xlsx_Only()
    {
        var parser = new ExcelParser(new ParserOptions());
        Assert.True(parser.CanParse(OfficeMediaTypes.Xlsx));
        Assert.False(parser.CanParse(OfficeMediaTypes.Docx));
        Assert.False(parser.CanParse("application/vnd.ms-excel")); // legacy .xls unsupported
    }

    [Fact]
    public async Task ParseAsync_Extracts_Sheet_Header_And_Rows_As_Data()
    {
        var parser = new ExcelParser(new ParserOptions());
        using var stream = MakeXlsx(
            "Bugs",
            [
                ["Key", "Summary", "Severity"],
                ["BUG-1", "Login fails on SSO", "High"],
            ]);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Bugs", doc.PlainText, StringComparison.Ordinal);        // sheet name
        Assert.Contains("Severity", doc.PlainText, StringComparison.Ordinal);    // header token
        Assert.Contains("Login fails on SSO", doc.PlainText, StringComparison.Ordinal); // cell value
        Assert.Equal(DocumentKind.Data, doc.Kind);
        Assert.Equal(OfficeMediaTypes.Xlsx, doc.MediaType);
        Assert.Equal("1", doc.Metadata[DocumentMetadata.SheetCount]);
    }

    [Fact]
    public async Task ParseAsync_Honors_Configured_Extraction_Limit()
    {
        var parser = new ExcelParser(new ParserOptions { MaxExtractedCharacters = 5 });
        using var stream = MakeXlsx("Sheet1", [["alpha", "beta", "gamma", "delta"]]);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.True(doc.PlainText.Length <= 5);
        Assert.Equal("true", doc.Metadata[DocumentMetadata.Truncated]);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Numeric_And_Boolean_Cells_And_Skips_Empty()
    {
        var parser = new ExcelParser(new ParserOptions());

        // Header (shared strings), then a data row with a number, a boolean, and an empty cell.
        using var stream = BuildTypedXlsx([["Amount", "Active", "Note"], [987654, true, null]]);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Amount", doc.PlainText, StringComparison.Ordinal);  // shared-string header
        Assert.Contains("987654", doc.PlainText, StringComparison.Ordinal);  // numeric via value path
        Assert.Contains("1", doc.PlainText, StringComparison.Ordinal);       // boolean true -> stored "1"
    }

    [Fact]
    public async Task ParseAsync_Indexes_Cached_Formula_Value_Not_Formula_Expression()
    {
        var parser = new ExcelParser(new ParserOptions());
        using var stream = BuildTypedXlsx([["Sum"], [new Formula("A1+B1", "42")]]);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("42", doc.PlainText, StringComparison.Ordinal);        // cached value indexed
        Assert.DoesNotContain("A1+B1", doc.PlainText, StringComparison.Ordinal); // formula expression never emitted
    }

    [Fact]
    public async Task ParseAsync_Extracts_Mixed_Cell_Types_In_One_Worksheet()
    {
        var parser = new ExcelParser(new ParserOptions());

        // One realistic row exercising every supported cell type together — catches ordering/interaction
        // bugs that isolated single-type tests miss.
        using var stream = BuildTypedXlsx(
            [
                ["Name", "Age", "Active", "JoinDate", "Salary"],
                ["Alice", 28, true, new DateOnly(2026, 1, 1), 65000],
            ]);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Alice", doc.PlainText, StringComparison.Ordinal);   // text (shared string)
        Assert.Contains("28", doc.PlainText, StringComparison.Ordinal);      // integer
        Assert.Contains("65000", doc.PlainText, StringComparison.Ordinal);   // integer
        Assert.Contains("1", doc.PlainText, StringComparison.Ordinal);       // boolean true -> stored "1"

        // Dates are stored as serial numbers; the ISO form is never emitted (documented v1 limitation).
        Assert.DoesNotContain("2026-01-01", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_MultiSheet_Emits_All_Sheet_Names_And_SheetCount()
    {
        var parser = new ExcelParser(new ParserOptions());
        using var stream = BuildMultiSheetXlsx([("Alpha", "apple"), ("Beta", "banana"), ("Gamma", "cherry")]);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Alpha", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Beta", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Gamma", doc.PlainText, StringComparison.Ordinal);
        Assert.Equal("3", doc.Metadata[DocumentMetadata.SheetCount]);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Package_Metadata()
    {
        var parser = new ExcelParser(new ParserOptions());
        using var stream = BuildXlsxWithProps(author: "Alice", category: "Data");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Equal("Alice", doc.Metadata[DocumentMetadata.Author]);
        Assert.Equal("Data", doc.Metadata[DocumentMetadata.Category]);
    }

    // A formula cell: its expression plus the cached (stored) result the parser must index.
    private sealed record Formula(string Expression, string CachedValue);
}
