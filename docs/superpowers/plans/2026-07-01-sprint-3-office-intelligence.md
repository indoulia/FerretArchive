# Sprint 3 — Office Intelligence (Word + Excel) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship end-to-end DOCX and XLSX indexing — a new `Ferret.Parsers.Office` package (OpenXml: `WordParser` DOM, `ExcelParser` streaming SAX), composed into the existing `ParserPackModule` (now 7 parsers), surfaced through `ferret doctor`, and proven by `ferret index` → `ferret search` finding Word prose and a Jira-export-style Excel cell value.

**Architecture:** Word and Excel share the OpenXml dependency and ship together in one sibling package, `Ferret.Parsers.Office` (splitting them would be dependency-isolation theater). `WordParser` uses the DOM (Word docs are small); `ExcelParser` uses the streaming `OpenXmlReader` (SAX) because enterprise exports run to 100k+ rows. Sprint 2 already created the `Ferret.Parsers` composition project and wired the CLI to `ParserPackModule`; this sprint only **adds** the Office package to that composition (one ProjectReference + one `ConfigureServices` call) — no new CLI callsite. A `ferret doctor` introspection check reports all installed parsers and the supported-extension count. Sprint 1 already reclassified `.docx`/`.xlsx` → their OpenXML media types (`BinaryParseable`; XLSX → `Data`), so no resolver change is needed.

**Tech Stack:** .NET 9, C#, xUnit, Microsoft.Extensions.DependencyInjection, DocumentFormat.OpenXml.

**Milestone spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md`
**Parent plan (source of reused code):** `docs/superpowers/plans/2026-07-01-parser-pack-1.md` (Task 4 = Office package; Task 5 = composition delta; Task 6 = doctor)
**Predecessors:** Sprint 1 (`ParserOptions`/`ExtractionLimiter`/`DocumentMetadata`, `.docx`/`.xlsx` mappings) and Sprint 2 (`Ferret.Parsers` project + `ParserPackModule` + CLI wiring) must be implemented first.

> **Forward link to Sprint 4/6:** the Word and Excel parsers must extract metadata into the same `DocumentMetadata.*` keys with the same conventions as PDF (Sprint 2). This consistency is what lets Sprint 4's corpus renderers emit metadata deterministically and Sprint 6's validation compare extracted metadata against the `corpus.json` manifest. Keep the key set and formatting uniform across all three parsers — the metadata round-trip tests in Task 1b lock this in.

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props` — do NOT set `<TargetFramework>` in any csproj.
- **Central Package Management:** every NuGet version lives in `Directory.Packages.props`; `<PackageReference>` carries **no** `Version` attribute.
- **Parser package isolation:** `Ferret.ParserPlatform` MUST NOT reference `Ferret.Parsers.Office`, and `Ferret.Parsers.Office` MUST NOT reference `Ferret.Parsers.Pdf`. The OpenXml dependency lives only in the Office package.
- **Parser responsibility (hard rule):** extract text + lightweight metadata only. NO chunking, tokenization, embedding, summarization, AI processing, **spreadsheet calculation, or formula evaluation.** `ExcelParser` reads **cached** cell values (`CellValue`) and NEVER reads `CellFormula` — the formula expression is never emitted.
- **Excel reads streaming:** `ExcelParser` uses `OpenXmlReader` (SAX) for worksheets, not the DOM. Word stays DOM.
- **Parsers MUST be `sealed`.** `CanParse` is pure: no I/O, never throws, deterministic.
- **Extracted-text limit (uniform):** both parsers take `ParserOptions` and apply the shared `ExtractionLimiter.ApplyCharacterLimit` (default `null` = unlimited); when exceeded, truncate `PlainText` and set `Metadata[DocumentMetadata.Truncated]="true"`.
- **Metadata keys are `DocumentMetadata.*` constants**, never raw strings.
- **Stream ownership:** parsers MUST NOT dispose/close the content stream.
- **Failure signaling:** malformed/non-OOXML input throws (`Open` fails) → dispatcher returns `Failed`. Legacy `.doc`/`.xls` are unsupported (stay `BinaryOpaque`). `OperationCanceledException` must propagate.
- **DocumentKind:** DOCX → `Prose`; XLSX → `Data`.
- **Pinned dependency:** `DocumentFormat.OpenXml` `3.1.0`. Bumping is a separate maintenance task.
- **No new CLI callsite:** the CLI already calls `ParserPackModule` (Sprint 2). This sprint edits only `ParserPackModule.cs` and its csproj.
- **New projects** must be added to `src/Ferret.sln` via `dotnet sln src/Ferret.sln add <path>`.
- **Backward compatibility:** existing text/markdown/JSON/CSV/PDF indexing unchanged.
- **StyleCop:** public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | `Ferret.Parsers.Office` package (Word + Excel + module + unit tests) | `Ferret.Parsers.Office` (new) |
| 1b | Expanded Office coverage (typed cells, multi-sheet, cached formula, metadata round-trip) | `Ferret.Parsers.Office.Tests` |
| 2 | Compose Office into `ParserPackModule` (5 → 7 parsers) | `Ferret.Parsers` |
| 3 | `ferret doctor` installed-parsers introspection | `Ferret.Cli`, `Ferret.ParserPlatform` |
| 4 | End-to-end DOCX + XLSX indexing validation | `Ferret.E2E.Tests` |

Task 1 stands alone (needs Sprint 1 only). Task 1b hardens Task 1's parsers with the coverage the review flagged as highest-value (it consumes only Task 1). Task 2 depends on Task 1. Task 3 depends on Task 2 (it composes the full pack to count parsers). Task 4 depends on Task 2 (the published CLI binary must include the Office parsers).

> **Scope note (row-count realism):** unit tests here stay small and fast. Multi-thousand-row workbook realism is deliberately *not* tested here — it is covered by Sprint 4's deterministically-varied archetype row counts (75–4500) and Sprint 5's dedicated 50k-row large-workbook benchmark. Do not add large-row workbooks to these unit tests.

---

### Task 1: Ferret.Parsers.Office — WordParser (DOCX) + ExcelParser (XLSX)

**Files:**
- Modify: `Directory.Packages.props` (add `DocumentFormat.OpenXml`)
- Create: `src/Ferret.Parsers.Office/Ferret.Parsers.Office.csproj`
- Create: `src/Ferret.Parsers.Office/OfficeMediaTypes.cs`
- Create: `src/Ferret.Parsers.Office/WordParser.cs`
- Create: `src/Ferret.Parsers.Office/ExcelParser.cs`
- Create: `src/Ferret.Parsers.Office/OfficeParserModule.cs`
- Create: `tests/Ferret.Parsers.Office.Tests/Ferret.Parsers.Office.Tests.csproj`
- Create: `tests/Ferret.Parsers.Office.Tests/WordParserTests.cs`
- Create: `tests/Ferret.Parsers.Office.Tests/ExcelParserTests.cs`

**Interfaces:**
- Consumes: `ParserOptions`, `ExtractionLimiter`, `DocumentMetadata` (Sprint 1); `IContentParser`, `ParserDescriptor`, `ParserId`, `ParseContext`, `Document`, `DocumentId`, `DocumentKind`, `ParserCapabilities` (`Ferret.Core`).
- Produces: `public static class OfficeMediaTypes { public const string Docx; public const string Xlsx; }`; `public sealed class WordParser : IContentParser` (ctor `ParserOptions`); `public sealed class ExcelParser : IContentParser` (ctor `ParserOptions`); `public static class OfficeParserModule { static void ConfigureServices(IServiceCollection); }` — registers **both** parsers + a default `ParserOptions`.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`:

```xml
<ItemGroup Label="Office (OpenXML) parsing">
  <PackageVersion Include="DocumentFormat.OpenXml" Version="3.1.0" />
</ItemGroup>
```

- [ ] **Step 2: Create the project file**

```xml
<!-- src/Ferret.Parsers.Office/Ferret.Parsers.Office.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Ferret.Parsers.Office</RootNamespace>
    <AssemblyName>Ferret.Parsers.Office</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="DocumentFormat.OpenXml" />
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write the failing Word tests**

```csharp
// tests/Ferret.Parsers.Office.Tests/WordParserTests.cs
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;

namespace Ferret.Parsers.Office.Tests;

public sealed class WordParserTests
{
    private static AssetDescriptor Asset() => new()
    {
        Id = AssetId.From(new Uri("filesystem:///doc.docx")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///doc.docx"),
        DisplayName = "doc.docx",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = OfficeMediaTypes.Docx,
    };

    // Builds a minimal .docx with a body paragraph and a one-cell table.
    private static Stream MakeDocx(string paragraphText, string cellText)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            body.Append(new Paragraph(new Run(new Text(paragraphText))));
            var table = new Table(new TableRow(new TableCell(new Paragraph(new Run(new Text(cellText))))));
            body.Append(table);
            main.Document = new Document(body);
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void CanParse_True_For_Docx_Only()
    {
        var parser = new WordParser(new ParserOptions());
        Assert.True(parser.CanParse(OfficeMediaTypes.Docx));
        Assert.False(parser.CanParse("application/pdf"));
        Assert.False(parser.CanParse("application/msword")); // legacy .doc unsupported
    }

    [Fact]
    public async Task ParseAsync_Extracts_Paragraph_And_Table_Text()
    {
        var parser = new WordParser(new ParserOptions());
        using var stream = MakeDocx("Quarterly objectives", "Revenue target");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Quarterly objectives", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Revenue target", doc.PlainText, StringComparison.Ordinal);
        Assert.Equal(DocumentKind.Prose, doc.Kind);
        Assert.Equal(OfficeMediaTypes.Docx, doc.MediaType);
    }
}
```

- [ ] **Step 4: Run Word tests to verify they fail**

Run: `dotnet test tests/Ferret.Parsers.Office.Tests --filter WordParserTests`
Expected: FAIL — `WordParser`/`OfficeMediaTypes` do not exist.

- [ ] **Step 5: Create the test project file**

```xml
<!-- tests/Ferret.Parsers.Office.Tests/Ferret.Parsers.Office.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.Parsers.Office.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="DocumentFormat.OpenXml" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Parsers.Office\Ferret.Parsers.Office.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6: Implement `OfficeMediaTypes` and `WordParser`**

```csharp
// src/Ferret.Parsers.Office/OfficeMediaTypes.cs
namespace Ferret.Parsers.Office;

/// <summary>Well-known OpenXML media type constants.</summary>
public static class OfficeMediaTypes
{
    /// <summary>The OpenXML WordprocessingML (.docx) media type.</summary>
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>The OpenXML SpreadsheetML (.xlsx) media type.</summary>
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
```

```csharp
// src/Ferret.Parsers.Office/WordParser.cs
using System.Text;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

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
        foreach (var text in root.Descendants<Text>())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(text.Text);
        }
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(WordprocessingDocument word, bool truncated)
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
```

> Malformed/non-OOXML input makes `WordprocessingDocument.Open` throw → dispatcher returns `Failed`. The "Company" extended property lives in `ExtendedFilePropertiesPart` and is deferred (YAGNI); `PackageProperties` covers the core metadata.

- [ ] **Step 7: Write the failing Excel tests**

```csharp
// tests/Ferret.Parsers.Office.Tests/ExcelParserTests.cs
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
    private static Stream MakeXlsx(string sheetName, string[][] rows)
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
                if (index.TryGetValue(s, out var i)) return i;
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
                        CellValue = new CellValue(Intern(cellText).ToString(System.Globalization.CultureInfo.InvariantCulture)),
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
        using var stream = MakeXlsx("Bugs",
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
}
```

- [ ] **Step 8: Run Excel tests to verify they fail**

Run: `dotnet test tests/Ferret.Parsers.Office.Tests --filter ExcelParserTests`
Expected: FAIL — `ExcelParser` does not exist.

- [ ] **Step 9: Implement `ExcelParser` (streaming reader, shared strings, cached values, configurable limit)**

```csharp
// src/Ferret.Parsers.Office/ExcelParser.cs
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

    private static IReadOnlyDictionary<string, string> BuildMetadata(
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
```

> `.xls` (legacy binary) is unsupported and stays `BinaryOpaque`; malformed/non-OOXML input makes `SpreadsheetDocument.Open` throw → dispatcher returns `Failed`. Dates stored as serial numbers may surface as serials (documented limitation).

- [ ] **Step 10: Implement the DI module**

```csharp
// src/Ferret.Parsers.Office/OfficeParserModule.cs
using Ferret.Core.Documents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Parsers.Office;

/// <summary>DI registration for the Office parser package: Word (.docx) and Excel (.xlsx).</summary>
public static class OfficeParserModule
{
    /// <summary>Registers <see cref="WordParser"/> and <see cref="ExcelParser"/> as <see cref="IContentParser"/>s.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Default (unlimited) options unless a host has already registered a configured instance.
        services.TryAddSingleton(new ParserOptions());

        services.AddSingleton<IContentParser, WordParser>();
        services.AddSingleton<IContentParser, ExcelParser>();
    }
}
```

> `TryAddSingleton(new ParserOptions())` is idempotent across `PdfParserModule` and `OfficeParserModule` (first registration wins), so composing them in `ParserPackModule` keeps the option uniform across all parsers.

- [ ] **Step 11: Add projects to the solution, build, and run tests**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Parsers.Office/Ferret.Parsers.Office.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Parsers.Office.Tests/Ferret.Parsers.Office.Tests.csproj
dotnet test tests/Ferret.Parsers.Office.Tests
```

Expected: PASS (Word: 2 tests, Excel: 3 tests).

- [ ] **Step 12: Commit**

```bash
git add Directory.Packages.props src/Ferret.Parsers.Office tests/Ferret.Parsers.Office.Tests src/Ferret.sln
git commit -m "feat(parsers): add Office package with Word (docx) and Excel (xlsx) parsers"
```

---

### Task 1b: Expanded Office parser coverage

These are characterization/coverage tests over the Task-1 parsers (typed cells, multi-sheet, cached formulas, metadata round-trip). They should **pass immediately** against the Task-1 implementation — if one fails, it has found a real gap to fix in the parser, not the test. All exercise `ExcelParser.ResolveCell`'s non-shared-string value path and the `PackageProperties`→`DocumentMetadata` mapping that Sprint 4's corpus and Sprint 6's validation rely on.

**Files:**
- Modify: `tests/Ferret.Parsers.Office.Tests/ExcelParserTests.cs` (add typed-cell builder + 4 tests)
- Modify: `tests/Ferret.Parsers.Office.Tests/WordParserTests.cs` (add props builder + 1 test)

**Interfaces:**
- Consumes: `ExcelParser`, `WordParser`, `OfficeMediaTypes` (Task 1); `DocumentMetadata` (`Ferret.Core`).

- [ ] **Step 1: Add a typed-cell workbook builder + tests to `ExcelParserTests`**

Add these members to the existing `ExcelParserTests` class. The `object?`-based builder maps `string`→shared string, `int`/`double`→numeric cell, `bool`→boolean cell, `null`→empty (skipped), and the `Formula` record→a formula cell carrying a cached value.

```csharp
using System.Globalization;

// A formula cell: its expression plus the cached (stored) result the parser must index.
private sealed record Formula(string Expression, string CachedValue);

// Builds a single-sheet workbook with typed cells to exercise the non-shared-string value path.
private static Stream BuildTypedXlsx(object?[][] rows, string sheetName = "Data")
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
            if (idx.TryGetValue(s, out var i)) return i;
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

// Builds a multi-sheet workbook, one shared-string cell per sheet.
private static Stream BuildMultiSheetXlsx((string Name, string Cell)[] sheets)
{
    var ms = new MemoryStream();
    using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, autoSave: true))
    {
        var wbPart = doc.AddWorkbookPart();
        wbPart.Workbook = new Workbook();
        var sheetsElement = wbPart.Workbook.AppendChild(new Sheets());
        uint id = 1;
        foreach (var (name, cellText) in sheets)
        {
            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sstPart = wsPart.AddNewPart<SharedStringTablePart>();
            sstPart.SharedStringTable = new SharedStringTable(new SharedStringItem(new Text(cellText)));
            wsPart.Worksheet = new Worksheet(new SheetData(new Row(new Cell
            {
                DataType = CellValues.SharedString,
                CellValue = new CellValue("0"),
            })));
            sheetsElement.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = id, Name = name });
            id++;
        }
    }

    ms.Position = 0;
    return ms;
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
```

> The multi-sheet builder attaches a `SharedStringTablePart` per worksheet part for brevity. If the target OpenXml version requires a single workbook-level shared-string table, move the interning to `wbPart` as in `MakeXlsx`; the parser reads whichever the package presents. Keep the assertion on sheet names + `SheetCount`.

- [ ] **Step 2: Add a metadata round-trip test to `ExcelParserTests`**

```csharp
// Builds a workbook whose package properties carry known metadata.
private static Stream BuildXlsxWithProps(string author, string category)
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
public async Task ParseAsync_Extracts_Package_Metadata()
{
    var parser = new ExcelParser(new ParserOptions());
    using var stream = BuildXlsxWithProps(author: "Alice", category: "Data");

    var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

    Assert.Equal("Alice", doc.Metadata[DocumentMetadata.Author]);
    Assert.Equal("Data", doc.Metadata[DocumentMetadata.Category]);
}
```

- [ ] **Step 3: Add a metadata round-trip test to `WordParserTests`**

Add to the existing `WordParserTests` class:

```csharp
// Builds a .docx whose package properties carry known metadata.
private static Stream MakeDocxWithProps(string author, string subject)
{
    var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document(new Body(new Paragraph(new Run(new Text("body")))));
        doc.PackageProperties.Creator = author;
        doc.PackageProperties.Subject = subject;
    }

    ms.Position = 0;
    return ms;
}

[Fact]
public async Task ParseAsync_Extracts_Package_Metadata()
{
    var parser = new WordParser(new ParserOptions());
    using var stream = MakeDocxWithProps(author: "Bob", subject: "Design Proposal");

    var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

    Assert.Equal("Bob", doc.Metadata[DocumentMetadata.Author]);
    Assert.Equal("Design Proposal", doc.Metadata[DocumentMetadata.Subject]);
}

// Builds a .docx with a header, a body paragraph, and a footer, wired via section properties.
private static Stream MakeDocxWithHeaderFooter(string headerText, string bodyText, string footerText)
{
    var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document(new Body());

        var headerPart = main.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(new Paragraph(new Run(new Text(headerText))));
        var headerId = main.GetIdOfPart(headerPart);

        var footerPart = main.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(new Paragraph(new Run(new Text(footerText))));
        var footerId = main.GetIdOfPart(footerPart);

        var body = main.Document.Body!;
        body.Append(new Paragraph(new Run(new Text(bodyText))));
        body.Append(new SectionProperties(
            new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
            new FooterReference { Type = HeaderFooterValues.Default, Id = footerId }));
    }

    ms.Position = 0;
    return ms;
}

[Fact]
public async Task ParseAsync_Extracts_Header_And_Footer_Text()
{
    var parser = new WordParser(new ParserOptions());
    using var stream = MakeDocxWithHeaderFooter("Confidential header", "Body content", "Page footer note");

    var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

    Assert.Contains("Confidential header", doc.PlainText, StringComparison.Ordinal);
    Assert.Contains("Body content", doc.PlainText, StringComparison.Ordinal);
    Assert.Contains("Page footer note", doc.PlainText, StringComparison.Ordinal);
}
```

**Coverage matrix (this task + Task 1).** Excel: text, number, boolean, date, empty, mixed-type worksheet, multiple worksheets, cached formula, workbook metadata. Word: paragraphs + tables (Task 1), headers, footers, document-properties round-trip. This is Sprint 3's definition of correctness — the parser implementations satisfy it before composition/wiring begins.

- [ ] **Step 4: Run the expanded Office suite**

Run: `dotnet test tests/Ferret.Parsers.Office.Tests`
Expected: PASS — Word (4 tests) + Excel (8 tests). These validate the Task-1 parsers; no parser change is expected. If the cached-formula test fails (formula expression leaks) or a typed cell is missing, fix `ExcelParser.ResolveCell` — that is a real defect, not a test issue.

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.Parsers.Office.Tests/ExcelParserTests.cs tests/Ferret.Parsers.Office.Tests/WordParserTests.cs
git commit -m "test(parsers): cover typed Excel cells, multi-sheet, cached formulas, and Office metadata round-trip"
```

---

### Task 2: Compose Office into ParserPackModule (5 → 7 parsers)

**Files:**
- Modify: `src/Ferret.Parsers/Ferret.Parsers.csproj` (add Office ProjectReference)
- Modify: `src/Ferret.Parsers/ParserPackModule.cs` (call `OfficeParserModule.ConfigureServices`)
- Modify: `tests/Ferret.Parsers.Tests/ParserPackModuleTests.cs` (5 → 7)

**Interfaces:**
- Consumes: `OfficeParserModule.ConfigureServices` (Task 1).
- Produces: `ParserPackModule` now registers 7 `IContentParser`s.

- [ ] **Step 1: Add the Office ProjectReference to the composition project**

In `src/Ferret.Parsers/Ferret.Parsers.csproj`, add inside the existing `<ItemGroup>`:

```xml
<ProjectReference Include="..\Ferret.Parsers.Office\Ferret.Parsers.Office.csproj" />
```

- [ ] **Step 2: Update the parser-count assertion to fail (red)**

In `tests/Ferret.Parsers.Tests/ParserPackModuleTests.cs`, change the `Registers_All_Five_Parsers` test to:

```csharp
    [Fact]
    public void Registers_All_Seven_Parsers()
    {
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var parsers = provider.GetServices<IContentParser>().ToList();
        Assert.Equal(7, parsers.Count); // PlainText, Markdown, Json, Csv, Pdf, Word, Excel
    }
```

Run: `dotnet test tests/Ferret.Parsers.Tests --filter Registers_All_Seven_Parsers`
Expected: FAIL — only 5 parsers registered (Office not composed yet).

- [ ] **Step 3: Add the Office registration to `ParserPackModule`**

In `src/Ferret.Parsers/ParserPackModule.cs`, add the using and the call:

```csharp
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers;

/// <summary>
/// Single composition entry point for the full parser pack: the platform (registry, dispatcher,
/// MimeTypeResolver, built-in text/CSV parsers) plus the PDF and Office parser packages.
/// Hosts call this once instead of wiring each parser module individually.
/// </summary>
public static class ParserPackModule
{
    /// <summary>Registers the parser platform and all bundled format parsers.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ParserPlatformModule.ConfigureServices(services);
        PdfParserModule.ConfigureServices(services);
        OfficeParserModule.ConfigureServices(services);
    }
}
```

- [ ] **Step 4: Run the composition tests to verify they pass**

Run: `dotnet test tests/Ferret.Parsers.Tests`
Expected: PASS (7 parsers; dispatcher-routing test unchanged). Because the CLI was wired to `ParserPackModule` in Sprint 2, DOCX/XLSX go live at `ferret index` with no CLI edit.

- [ ] **Step 5: Commit**

```bash
git add src/Ferret.Parsers/Ferret.Parsers.csproj src/Ferret.Parsers/ParserPackModule.cs tests/Ferret.Parsers.Tests/ParserPackModuleTests.cs
git commit -m "feat(parsers): compose Office parsers into ParserPackModule (7 parsers total)"
```

---

### Task 3: InstalledParsersCheck (ferret doctor introspection)

**Files:**
- Create: `src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs`
- Modify: `src/Ferret.Cli/Commands/CoreCliModule.cs` (`BuildChecks`)
- Modify: `src/Ferret.ParserPlatform/MimeTypeResolver.cs` (expose `KnownExtensionCount`)
- Test: `tests/Ferret.Cli.Tests/Diagnostics/InstalledParsersCheckTests.cs`

**Interfaces:**
- Consumes: `IDiagnosticCheck`, `DiagnosticCheckResult`, `IFerretContext`, `IEnumerable<IContentParser>`, `MimeTypeResolver`, `ParserPackModule`.
- Produces: `internal sealed class InstalledParsersCheck : IDiagnosticCheck`; `MimeTypeResolver.KnownExtensionCount` (static).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Ferret.Cli.Tests/Diagnostics/InstalledParsersCheckTests.cs
using Ferret.Cli.Diagnostics.Checks;
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class InstalledParsersCheckTests
{
    [Fact]
    public async Task Passes_When_Parsers_Registered()
    {
        IReadOnlyList<IContentParser> parsers = [new PlainTextParser(), new MarkdownParser(), new JsonParser()];
        var check = new InstalledParsersCheck(parsers, parserCount: 3, supportedExtensionCount: 60);

        var result = await check.RunAsync(context: null!, CancellationToken.None);

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Warns_When_No_Parsers_Registered()
    {
        var check = new InstalledParsersCheck([], parserCount: 0, supportedExtensionCount: 0);

        var result = await check.RunAsync(context: null!, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.True(result.IsWarning);
    }
}
```

> If `PlainTextParser`/`MarkdownParser`/`JsonParser` require constructor arguments in the current codebase, mirror the construction used in the existing `ParserPlatform` tests. The check's contract only depends on `IContentParser.Descriptor.Name`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Ferret.Cli.Tests --filter InstalledParsersCheckTests`
Expected: FAIL — `InstalledParsersCheck` does not exist.

- [ ] **Step 3: Implement the check**

```csharp
// src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs
using System.Globalization;

using Ferret.Cli.Cli;
using Ferret.Core.Documents;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Reports the content parsers registered in the host and the number of supported file extensions.</summary>
internal sealed class InstalledParsersCheck : IDiagnosticCheck
{
    private readonly IReadOnlyList<string> _parserNames;
    private readonly int _parserCount;
    private readonly int _supportedExtensionCount;

    internal InstalledParsersCheck(
        IReadOnlyList<IContentParser> parsers,
        int parserCount,
        int supportedExtensionCount)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        _parserNames = parsers.Select(p => p.Descriptor.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        _parserCount = parserCount;
        _supportedExtensionCount = supportedExtensionCount;
    }

    /// <inheritdoc/>
    public string Name => string.Create(
        CultureInfo.InvariantCulture,
        $"Content parsers: {_parserCount} installed, {_supportedExtensionCount} extensions ({string.Join(", ", _parserNames)})");

    /// <inheritdoc/>
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        var result = _parserCount > 0
            ? DiagnosticCheckResult.Pass()
            : DiagnosticCheckResult.Warn("No content parsers are registered; indexing will skip all files.");
        return Task.FromResult(result);
    }
}
```

> Match the exact `IDiagnosticCheck` shape (member names, `DiagnosticCheckResult.Pass()`/`.Warn(...)`, `Passed`/`IsWarning`) against an existing check in `src/Ferret.Cli/Diagnostics/Checks/` before implementing; adjust names if the codebase differs.

- [ ] **Step 4: Expose the supported-extension count on `MimeTypeResolver`**

In `src/Ferret.ParserPlatform/MimeTypeResolver.cs`, add (additive, next to the `Map` dictionary):

```csharp
/// <summary>Gets the number of mapped extensions that resolve to text or parseable-binary content.</summary>
public static int KnownExtensionCount => Map.Count(kv => kv.Value.Category != MediaCategory.BinaryOpaque);
```

- [ ] **Step 5: Register the check in `CoreCliModule.BuildChecks`**

In `src/Ferret.Cli/Commands/CoreCliModule.cs`, add near the other `yield return` checks in `BuildChecks(...)`:

```csharp
{
    var parserServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    Ferret.Parsers.ParserPackModule.ConfigureServices(parserServices);
    using var parserProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
        .CreateScope(parserServices.BuildServiceProvider());
    var parsers = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
        .GetServices<Ferret.Core.Documents.IContentParser>(parserProvider.ServiceProvider).ToList();
    yield return new Checks.InstalledParsersCheck(
        parsers, parsers.Count, Ferret.ParserPlatform.MimeTypeResolver.KnownExtensionCount);
}
```

(Fully-qualified names are used to avoid touching the file's `using` block; simplify to short names if those usings already exist.)

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.Cli.Tests --filter InstalledParsersCheckTests`
Expected: PASS (2 tests).

- [ ] **Step 7: Manually verify the doctor output**

Run: `dotnet run --project src/Ferret.Cli -- doctor`
Expected: output includes a line naming the 7 installed parsers (Plain Text, Markdown, JSON, CSV, PDF, Word (DOCX), Excel (XLSX)) and the supported-extension count.

- [ ] **Step 8: Commit**

```bash
git add src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs src/Ferret.Cli/Commands/CoreCliModule.cs src/Ferret.ParserPlatform/MimeTypeResolver.cs tests/Ferret.Cli.Tests/Diagnostics/InstalledParsersCheckTests.cs
git commit -m "feat(cli): add installed-parsers diagnostic check to ferret doctor"
```

---

### Task 4: End-to-end DOCX + XLSX indexing validation (Ferret.E2E.Tests)

**Files:**
- Modify: `tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj` (add `DocumentFormat.OpenXml` for writing test files)
- Modify: `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs` (add `WriteSampleOfficeFilesAsync`)
- Create: `tests/Ferret.E2E.Tests/Tests/OfficeIndexE2ETests.cs`

**Interfaces:**
- Consumes: `WorkspaceFixture.InitializeAsync()`, `WorkspaceFixture.RunAsync(...)`, `WorkspaceFixture.WorkspaceDir`, `WorkspaceFixture.DisposeAsync()` (existing).
- Produces: `WorkspaceFixture.WriteSampleOfficeFilesAsync()` writing a real `.docx` (prose) and a Jira-export-style `.xlsx` into `WorkspaceDir` via OpenXml.

> The E2E project drives the **published `ferret` binary**, so this validates that the Task-1/Task-2 Office parsers reach a shipped build through `ParserPackModule`.

- [ ] **Step 1: Add the OpenXml writer dependency to the E2E project**

In `tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj`, add to the existing `<ItemGroup>`:

```xml
<PackageReference Include="DocumentFormat.OpenXml" />
```

(Version resolves from `Directory.Packages.props`, added in Task 1. Used only to author binary test fixtures.)

- [ ] **Step 2: Add real DOCX/XLSX fixture writers to `WorkspaceFixture`**

Add these to `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs`, plus the usings at the top: `using DocumentFormat.OpenXml;`, `using DocumentFormat.OpenXml.Packaging;`, `using DocumentFormat.OpenXml.Spreadsheet;`, and `using Word = DocumentFormat.OpenXml.Wordprocessing;` (aliased to avoid the `Text`/`Document` name clash with the spreadsheet namespace).

```csharp
/// <summary>Writes a real .docx (prose) and a Jira-export-style .xlsx into the workspace.</summary>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public async Task WriteSampleOfficeFilesAsync()
{
    WriteDocx(
        Path.Join(WorkspaceDir, "design-proposal.docx"),
        "Design Proposal: adopt a columnar cache to accelerate retrieval.",
        "Approved");

    WriteXlsx(
        Path.Join(WorkspaceDir, "bug-export.xlsx"),
        "Bugs",
        [
            ["Key", "Summary", "Severity", "Assignee"],
            ["BUG-1", "Checkout latency regression", "High", "Dana"],
            ["BUG-2", "Timeout on export", "Critical", "Rahul"],
        ]);

    await Task.CompletedTask.ConfigureAwait(false);
}

private static void WriteDocx(string path, string paragraph, string cell)
{
    using var fs = File.Create(path);
    using var doc = WordprocessingDocument.Create(fs, WordprocessingDocumentType.Document, autoSave: true);
    var main = doc.AddMainDocumentPart();
    var body = new Word.Body();
    body.Append(new Word.Paragraph(new Word.Run(new Word.Text(paragraph))));
    var table = new Word.Table(new Word.TableRow(new Word.TableCell(
        new Word.Paragraph(new Word.Run(new Word.Text(cell))))));
    body.Append(table);
    main.Document = new Word.Document(body);
}

private static void WriteXlsx(string path, string sheetName, string[][] rows)
{
    using var fs = File.Create(path);
    using var doc = SpreadsheetDocument.Create(fs, SpreadsheetDocumentType.Workbook, autoSave: true);
    var wbPart = doc.AddWorkbookPart();
    wbPart.Workbook = new Workbook();

    var sstPart = wbPart.AddNewPart<SharedStringTablePart>();
    var sst = new SharedStringTable();
    var index = new Dictionary<string, int>(StringComparer.Ordinal);
    int Intern(string s)
    {
        if (index.TryGetValue(s, out var i)) return i;
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
                CellValue = new CellValue(Intern(cellText).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            });
        }

        sheetData.Append(r);
    }

    wsPart.Worksheet = new Worksheet(sheetData);
    sstPart.SharedStringTable = sst;

    var sheets = wbPart.Workbook.AppendChild(new Sheets());
    sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = sheetName });
}
```

> The spreadsheet `Text`/`SharedStringItem`/`Cell` types come from `DocumentFormat.OpenXml.Spreadsheet`; the Word types are reached through the `Word.` alias. This mirrors the unit-test fixtures in Task 1.

- [ ] **Step 3: Write the failing E2E tests**

```csharp
// tests/Ferret.E2E.Tests/Tests/OfficeIndexE2ETests.cs
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E: index real DOCX + XLSX through the published binary, then prove they are searchable.</summary>
public sealed class OfficeIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSampleOfficeFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search after indexing Office files returns exit code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_AfterOfficeIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search columnar");

        Assert.Equal(0, exitCode);
    }

    /// <summary>A word from the DOCX body is searchable and points at the source document.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_DocxBodyWord_ReturnsDesignProposal()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search columnar");

        Assert.Contains("design-proposal.docx", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A Jira-export cell value (the stated product-value assertion) is searchable and points at the .xlsx.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_XlsxCellValue_ReturnsBugExport()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search regression");

        Assert.Contains("bug-export.xlsx", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>An XLSX header token is searchable and points at the .xlsx.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_XlsxAssignee_ReturnsBugExport()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search Rahul");

        Assert.Contains("bug-export.xlsx", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 4: Run the E2E tests to verify they pass**

Run: `dotnet test tests/Ferret.E2E.Tests --filter OfficeIndexE2ETests`
Expected: PASS (4 tests). If a multi-word cell fails to match, assert on a single distinctive lowercase token guaranteed present (`columnar`, `regression`, `Rahul`).

- [ ] **Step 5: Run the full E2E suite to confirm no regression**

Run: `dotnet test tests/Ferret.E2E.Tests`
Expected: PASS (existing tests, plus Sprint 2's `PdfIndexE2ETests`, unaffected — the new fixture method and test class are additive).

- [ ] **Step 6: Commit**

```bash
git add tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs tests/Ferret.E2E.Tests/Tests/OfficeIndexE2ETests.cs
git commit -m "test(e2e): validate DOCX and XLSX (Jira-export cell) index and search end-to-end"
```

---

## Final verification

- [ ] **Full solution build + test**

Run: `dotnet build src/Ferret.sln && dotnet test src/Ferret.sln`
Expected: build clean, all tests green.

- [ ] **Acceptance criteria check**

Confirm each: `Ferret.Parsers.Office` builds with no OpenXml leak into the platform or the PDF package · `WordParser` extracts paragraphs + table cells (Prose) · `ExcelParser` uses streaming SAX, resolves shared strings, emits sheet name + header + rows (Data), never emits a formula expression, honors the configured limit · `ParserPackModule` composes 7 parsers · a real DOCX and a Jira-export-style XLSX cell are searchable e2e · `ferret doctor` lists the 7 parsers + extension count · legacy `.doc`/`.xls` unsupported (unit-level) · existing PDF/text/markdown/JSON/CSV indexing unchanged · no `Version` attributes on `<PackageReference>` · OpenXml pinned to 3.1.0 · all existing tests still green.
