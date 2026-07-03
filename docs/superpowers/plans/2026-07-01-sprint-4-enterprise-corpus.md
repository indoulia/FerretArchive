# Sprint 4 — Enterprise Corpus Generator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic, multi-format synthetic enterprise corpus generator — an abstract `CorpusDocument` model (with metadata and typed table cells) plus per-format renderers (Markdown, HTML, C#, JSON, PDF, DOCX, XLSX), realistic tabular enterprise archetypes, a realistic enterprise folder hierarchy, and a `corpus.json` manifest — so later benchmarking and validation run against representative PDF/DOCX/XLSX/code documents at configurable sizes.

**Architecture:** Separate *what* a document is (`CorpusDocument`: title + `DocumentMetadata`-keyed metadata + prose `CorpusBlock`s + tabular `CorpusTable`s of typed `CorpusCell`s) from *how* it is emitted (`IDocumentRenderer`). The same logical document renders to Markdown (pipe tables), HTML, C#, JSON, a real PDF (PdfPig writer), a real Word table (OpenXml), and a real Excel sheet (OpenXml; text→shared strings, numbers/booleans/dates→typed cells). Excel is the format that *forces* the model to carry tabular content — hence `CorpusTable`; typed cells exist so the generator exercises `ExcelParser`'s non-shared-string value path, not to model spreadsheet semantics. All randomness derives from a single seeded `Random`; same seed + size ⇒ identical output. Documents are laid out under a realistic enterprise tree (Engineering / Operations / Quality / Management) so path names are themselves searchable. A `corpus.json` manifest records seed, size, generator version, and counts. Everything lives in the existing `tests/Ferret.Benchmarks` project (a BenchmarkDotNet console app); unit tests live in a new sibling xUnit project `tests/Ferret.Benchmarks.Tests`. Generated corpora go to temp directories and are **never committed**.

**Tech Stack:** .NET 9, C#, xUnit, UglyToad.PdfPig (writer), DocumentFormat.OpenXml.

**Milestone spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md` (§ Synthetic Enterprise Corpus Generator)
**Benchmark Suite Spec:** `docs/superpowers/specs/2026-06-30-benchmark-suite-spec.md` (corpus size tiers)
**Parent plan (source of reused code):** `docs/superpowers/plans/2026-07-01-parser-pack-1.md` (Task 7)
**Predecessors:** Sprint 2 (PdfPig package version) and Sprint 3 (`Ferret.Parsers.Office` + OpenXml) must be implemented first — the renderers, and the determinism test's extraction step, reference both parser packages.

## Global Constraints

- **Target framework:** `net9.0`. `Ferret.Benchmarks` already pins `net9.0` explicitly; the new test project inherits from `Directory.Build.props` — do NOT set `<TargetFramework>` there.
- **Central Package Management:** every NuGet version lives in `Directory.Packages.props` (PdfPig `1.7.0-custom-5` — see Sprint 2's version-deviation note — and OpenXml 3.1.0 were added in Sprints 2/3); `<PackageReference>` carries **no** `Version` attribute.
- **Determinism (hard rule):** all randomness derives from a fixed seed. NO unseeded `Random`, NO wall-clock timestamps, NO `Guid.NewGuid()` in document *content* or in the manifest. DOCX/XLSX metadata timestamps are pinned to a fixed value. (Test scaffolding may use `Guid`/temp paths for *directory* names — never for content.)
- **Determinism acceptance contract (primary):**
  - **Text formats** (`.md`, `.html`, `.cs`, `.json`) and **`corpus.json`** → **byte-identical** across same-seed runs.
  - **Binary formats** (`.pdf`, `.docx`, `.xlsx`) → **identical extracted text + identical extracted metadata** (excluding writer-stamped `Created`/`Modified`, which PDF writers set from the wall clock). Byte equality is NOT required for binaries — OOXML ZIP entry ordering and package internals make it fragile.
- **Metadata via constants:** logical document metadata uses `Ferret.Core.Documents.DocumentMetadata.*` keys (never raw strings), so renderers and parsers exercise the same metadata schema end-to-end.
- **Typed cells are minimal:** `CorpusCell` has exactly five kinds — `Text`, `Number`, `Boolean`, `Date`, `Empty`. Purpose is parser coverage (numeric/boolean/date cells hit `ExcelParser`'s non-shared-string branch); it is NOT a spreadsheet type system. Search still indexes extracted text.
- **No dependency injection in the benchmark tool.** Renderers are plain classes; the `IDocumentRenderer` interface is the extension seam. Do not add a DI container.
- **Test placement:** `Ferret.Benchmarks` is a console app, not a test project. Generator unit tests live in `tests/Ferret.Benchmarks.Tests` (xUnit), referencing `Ferret.Benchmarks` and the parser packages.
- **Not committed:** generated output goes to `Path.GetTempPath()` subdirectories and is deleted by the tests.
- **Reusability:** the generator is standalone (seed + size + output root); it takes no dependency on benchmark harness internals, so demo-data/CI-fixture consumers can reuse it.
- **New projects** must be added to `src/Ferret.sln` via `dotnet sln src/Ferret.sln add <path>`.
- **StyleCop:** public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | Logical model (metadata + typed cells) + renderer contract + size tiers | `Ferret.Benchmarks` |
| 2 | Text-family renderers (Markdown, HTML, C#, JSON) with tables + metadata | `Ferret.Benchmarks` |
| 3 | Binary renderers (PDF, DOCX, XLSX) with typed cells + metadata | `Ferret.Benchmarks` |
| 4 | Enterprise tabular archetypes (typed cells, varied row counts) | `Ferret.Benchmarks` |
| 5 | Seeded generator (hierarchy + manifest) + tests | `Ferret.Benchmarks`, `Ferret.Benchmarks.Tests` (new) |

Tasks 1–4 build components (each independently testable). Task 5 assembles them into the generator (realistic tree + `corpus.json`) and adds determinism, per-renderer validation, and cross-format equivalence tests. Task 3 must follow Sprints 2/3 (needs PdfPig + OpenXml).

---

### Task 1: Logical model (metadata + typed cells) + renderer contract + size tiers

**Files:**
- Create: `tests/Ferret.Benchmarks/Corpus/CorpusDocument.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/CorpusCell.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/IDocumentRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/CorpusSize.cs`

**Interfaces:**
- Produces: `enum CorpusBlockKind { Heading, Paragraph, CodeLine, KeyValue }`; `sealed record CorpusBlock(CorpusBlockKind Kind, string Text)`; `enum CorpusCellKind { Text, Number, Boolean, Date, Empty }`; `sealed record CorpusCell(CorpusCellKind Kind, string Value)` with static factories; `sealed record CorpusTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CorpusCell>> Rows)`; `sealed record CorpusDocument(string Title, IReadOnlyDictionary<string,string> Metadata, IReadOnlyList<CorpusBlock> Blocks, IReadOnlyList<CorpusTable> Tables)`; `interface IDocumentRenderer { string Extension { get; } void Render(CorpusDocument doc, Stream output); }`; `enum CorpusSize { Small, Medium, Enterprise }`.

- [ ] **Step 1: Create the typed cell**

```csharp
// tests/Ferret.Benchmarks/Corpus/CorpusCell.cs
using System.Globalization;

namespace Ferret.Benchmarks.Corpus;

/// <summary>The value kind of a table cell. Minimal by design — parser coverage, not spreadsheet semantics.</summary>
public enum CorpusCellKind
{
    Text,
    Number,
    Boolean,
    Date,
    Empty,
}

/// <summary>A single typed table cell. <see cref="Value"/> is the canonical text form; renderers that
/// support types (Excel) emit typed cells, text renderers emit <see cref="Value"/> verbatim.</summary>
public sealed record CorpusCell(CorpusCellKind Kind, string Value)
{
    /// <summary>An empty cell.</summary>
    public static readonly CorpusCell Empty = new(CorpusCellKind.Empty, string.Empty);

    /// <summary>Creates a text cell.</summary>
    public static CorpusCell Text(string value) => new(CorpusCellKind.Text, value);

    /// <summary>Creates a numeric cell.</summary>
    public static CorpusCell Number(double value) =>
        new(CorpusCellKind.Number, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Creates a boolean cell.</summary>
    public static CorpusCell Boolean(bool value) => new(CorpusCellKind.Boolean, value ? "true" : "false");

    /// <summary>Creates a date cell (ISO yyyy-MM-dd canonical form).</summary>
    public static CorpusCell Date(DateOnly value) =>
        new(CorpusCellKind.Date, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
```

- [ ] **Step 2: Create the document model (with metadata)**

```csharp
// tests/Ferret.Benchmarks/Corpus/CorpusDocument.cs
namespace Ferret.Benchmarks.Corpus;

/// <summary>The semantic role of a block within a logical corpus document.</summary>
public enum CorpusBlockKind
{
    Heading,
    Paragraph,
    CodeLine,
    KeyValue,
}

/// <summary>A single format-agnostic content block.</summary>
public sealed record CorpusBlock(CorpusBlockKind Kind, string Text);

/// <summary>A format-agnostic table: a header row plus typed data rows. Rendered as a Markdown pipe
/// table, an HTML table, a Word table, or an Excel sheet by the respective renderer.</summary>
public sealed record CorpusTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CorpusCell>> Rows);

/// <summary>A logical, format-agnostic document. Renderers turn it into concrete file bytes.
/// <see cref="Metadata"/> uses <c>Ferret.Core.Documents.DocumentMetadata</c> keys so every renderer
/// and parser exercises the same metadata schema. A document may carry prose blocks, tables, or both.</summary>
public sealed record CorpusDocument(
    string Title,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<CorpusBlock> Blocks,
    IReadOnlyList<CorpusTable> Tables);
```

- [ ] **Step 3: Create the renderer contract**

```csharp
// tests/Ferret.Benchmarks/Corpus/IDocumentRenderer.cs
namespace Ferret.Benchmarks.Corpus;

/// <summary>Renders a logical <see cref="CorpusDocument"/> into a concrete file format.
/// This interface is the sole extension seam for new formats (XML, YAML, PPTX, CSV, logs, …):
/// add a renderer, no change to the model or generator core.</summary>
public interface IDocumentRenderer
{
    /// <summary>Gets the file extension this renderer produces, including the leading dot.</summary>
    string Extension { get; }

    /// <summary>Renders the document to the output stream. Must be deterministic for a given input.</summary>
    /// <param name="doc">The logical document.</param>
    /// <param name="output">The destination stream.</param>
    void Render(CorpusDocument doc, Stream output);
}
```

- [ ] **Step 4: Create the size tiers**

```csharp
// tests/Ferret.Benchmarks/Corpus/CorpusSize.cs
namespace Ferret.Benchmarks.Corpus;

/// <summary>Benchmark corpus size tiers, aligned with the Benchmark Suite Spec.</summary>
public enum CorpusSize
{
    /// <summary>~200 files.</summary>
    Small,

    /// <summary>~2,000 files.</summary>
    Medium,

    /// <summary>~15,000 files.</summary>
    Enterprise,
}
```

- [ ] **Step 5: Build the benchmark project to confirm the model compiles**

Run: `dotnet build tests/Ferret.Benchmarks`
Expected: build succeeds (types added; no consumers yet).

- [ ] **Step 6: Commit**

```bash
git add tests/Ferret.Benchmarks/Corpus/CorpusDocument.cs tests/Ferret.Benchmarks/Corpus/CorpusCell.cs tests/Ferret.Benchmarks/Corpus/IDocumentRenderer.cs tests/Ferret.Benchmarks/Corpus/CorpusSize.cs
git commit -m "feat(bench): add logical corpus model with metadata and typed cells, renderer contract, size tiers"
```

---

### Task 2: Text-family renderers (Markdown, HTML, C#, JSON)

**Files:**
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/MarkdownRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/HtmlRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/CSharpRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/JsonRenderer.cs`

**Interfaces:**
- Consumes: `IDocumentRenderer`, `CorpusDocument`, `CorpusBlock`, `CorpusBlockKind`, `CorpusTable`, `CorpusCell` (Task 1); `DocumentMetadata` (`Ferret.Core`).
- Produces: `MarkdownRenderer` (`.md`), `HtmlRenderer` (`.html`), `CSharpRenderer` (`.cs`), `JsonRenderer` (`.json`), all `public sealed class : IDocumentRenderer`. Markdown/HTML render tables (pipe/`<table>`); all emit metadata in a format-appropriate way.

- [ ] **Step 1: Implement the Markdown renderer (metadata front line + pipe tables)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/MarkdownRenderer.cs
using System.Text;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as Markdown, including an author line and pipe tables.</summary>
public sealed class MarkdownRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".md";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(doc.Title);
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            sb.Append("> Author: ").AppendLine(author);
        }

        foreach (var block in doc.Blocks)
        {
            switch (block.Kind)
            {
                case CorpusBlockKind.Heading: sb.Append("## ").AppendLine(block.Text); break;
                case CorpusBlockKind.CodeLine: sb.Append("    ").AppendLine(block.Text); break;
                case CorpusBlockKind.KeyValue: sb.Append("- ").AppendLine(block.Text); break;
                default: sb.AppendLine(block.Text); break;
            }
        }

        foreach (var t in doc.Tables)
        {
            sb.Append("| ").Append(string.Join(" | ", t.Headers)).AppendLine(" |");
            sb.Append("| ").Append(string.Join(" | ", t.Headers.Select(_ => "---"))).AppendLine(" |");
            foreach (var row in t.Rows)
            {
                sb.Append("| ").Append(string.Join(" | ", row.Select(c => c.Value))).AppendLine(" |");
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }
}
```

- [ ] **Step 2: Implement the HTML renderer (meta tags + tables)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/HtmlRenderer.cs
using System.Text;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a minimal HTML document with meta tags and tables.</summary>
public sealed class HtmlRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".html";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var sb = new StringBuilder();
        sb.Append("<html><head><title>").Append(doc.Title).Append("</title>");
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            sb.Append("<meta name=\"author\" content=\"").Append(author).Append("\">");
        }

        sb.Append("</head><body>");
        sb.Append("<h1>").Append(doc.Title).Append("</h1>");
        foreach (var block in doc.Blocks)
        {
            sb.Append("<p>").Append(block.Text).Append("</p>");
        }

        foreach (var t in doc.Tables)
        {
            sb.Append("<table><tr>");
            foreach (var h in t.Headers) sb.Append("<th>").Append(h).Append("</th>");
            sb.Append("</tr>");
            foreach (var row in t.Rows)
            {
                sb.Append("<tr>");
                foreach (var c in row) sb.Append("<td>").Append(c.Value).Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
        }

        sb.Append("</body></html>");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }
}
```

- [ ] **Step 3: Implement the C# renderer**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/CSharpRenderer.cs
using System.Text;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a plausible C# source file.</summary>
public sealed class CSharpRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".cs";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var sb = new StringBuilder();
        sb.AppendLine("namespace Generated;").AppendLine();
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            sb.Append("// Author: ").AppendLine(author);
        }

        sb.Append("/// <summary>").Append(doc.Title).AppendLine("</summary>");
        sb.Append("public sealed class ").Append(Sanitize(doc.Title)).AppendLine();
        sb.AppendLine("{");
        foreach (var block in doc.Blocks)
        {
            sb.Append("    // ").AppendLine(block.Text);
        }

        sb.AppendLine("}");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }

    private static string Sanitize(string title)
    {
        var chars = title.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "Doc" : new string(chars);
    }
}
```

- [ ] **Step 4: Implement the JSON renderer (metadata object)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/JsonRenderer.cs
using System.Text.Json;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a JSON object with a metadata map.</summary>
public sealed class JsonRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".json";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        // Deterministic: fixed property order; metadata keys emitted in ordinal-sorted order.
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartObject();
        writer.WriteString("title", doc.Title);

        writer.WriteStartObject("metadata");
        foreach (var kv in doc.Metadata.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            writer.WriteString(kv.Key, kv.Value);
        }

        writer.WriteEndObject();

        writer.WriteStartArray("blocks");
        foreach (var block in doc.Blocks)
        {
            writer.WriteStringValue(block.Text);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }
}
```

- [ ] **Step 5: Build to confirm the text renderers compile**

Run: `dotnet build tests/Ferret.Benchmarks`
Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
git add tests/Ferret.Benchmarks/Corpus/Renderers/MarkdownRenderer.cs tests/Ferret.Benchmarks/Corpus/Renderers/HtmlRenderer.cs tests/Ferret.Benchmarks/Corpus/Renderers/CSharpRenderer.cs tests/Ferret.Benchmarks/Corpus/Renderers/JsonRenderer.cs
git commit -m "feat(bench): add text-family renderers with table and metadata support"
```

---

### Task 3: Binary renderers (PDF, DOCX, XLSX)

**Files:**
- Modify: `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` (add `UglyToad.PdfPig`, `DocumentFormat.OpenXml`, `Ferret.Parsers.Office`)
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/PdfRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/DocxRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/XlsxRenderer.cs`

**Interfaces:**
- Consumes: `IDocumentRenderer`, `CorpusDocument`, `CorpusTable`, `CorpusCell`, `CorpusCellKind` (Task 1); `DocumentMetadata` (`Ferret.Core`).
- Produces: `PdfRenderer` (`.pdf`), `DocxRenderer` (`.docx`), `XlsxRenderer` (`.xlsx`), all `public sealed class : IDocumentRenderer`, emitting metadata; `XlsxRenderer` emits typed cells.

- [ ] **Step 1: Add the renderer dependencies to the benchmark project**

In `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj`, add a new `ItemGroup`:

```xml
<ItemGroup>
  <PackageReference Include="UglyToad.PdfPig" />
  <PackageReference Include="DocumentFormat.OpenXml" />
  <ProjectReference Include="..\..\src\Ferret.Parsers.Office\Ferret.Parsers.Office.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Implement the DOCX renderer (metadata + tables, pinned timestamps)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/DocxRenderer.cs
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a real .docx using OpenXml.</summary>
public sealed class DocxRenderer : IDocumentRenderer
{
    private static readonly DateTime FixedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc/>
    public string Extension => ".docx";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        using var word = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document, autoSave: true);
        var main = word.AddMainDocumentPart();
        var body = new Body();
        body.Append(new Paragraph(new Run(new Text(doc.Title))));
        foreach (var block in doc.Blocks)
        {
            body.Append(new Paragraph(new Run(new Text(block.Text) { Space = SpaceProcessingModeValues.Preserve })));
        }

        foreach (var t in doc.Tables)
        {
            var table = new Table();
            table.Append(RowOf(t.Headers));
            foreach (var row in t.Rows)
            {
                table.Append(RowOf(row.Select(c => c.Value).ToList()));
            }

            body.Append(table);
        }

        main.Document = new Document(body);

        var props = word.PackageProperties;
        props.Title = doc.Title;
        props.Creator = Meta(doc, DocumentMetadata.Author) ?? "Synthetic Corpus Generator";
        props.Subject = Meta(doc, DocumentMetadata.Subject);
        props.Keywords = Meta(doc, DocumentMetadata.Keywords);
        props.Category = Meta(doc, DocumentMetadata.Category);
        props.Created = FixedTimestamp;   // pinned for determinism
        props.Modified = FixedTimestamp;
    }

    private static string? Meta(CorpusDocument doc, string key) =>
        doc.Metadata.TryGetValue(key, out var v) ? v : null;

    private static TableRow RowOf(IReadOnlyList<string> cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
        {
            row.Append(new TableCell(new Paragraph(new Run(new Text(cell) { Space = SpaceProcessingModeValues.Preserve }))));
        }

        return row;
    }
}
```

- [ ] **Step 3: Implement the XLSX renderer (typed cells, shared strings, pinned timestamps)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/XlsxRenderer.cs
using System.Globalization;

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
            if (index.TryGetValue(s, out var i)) return i;
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
            foreach (var h in t.Headers) headerRow.Append(SharedCell(h));
            sheetData.Append(headerRow);

            foreach (var row in t.Rows)
            {
                var r = new Row();
                foreach (var c in row)
                {
                    if (c.Kind == CorpusCellKind.Empty) continue; // skip empties (exercises parser skip logic)
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
```

- [ ] **Step 4: Implement the PDF renderer (metadata via DocumentInformation)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/PdfRenderer.cs
using Ferret.Core.Documents;

using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a real PDF using PdfPig's writer (benchmark-only use).</summary>
public sealed class PdfRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".pdf";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var builder = new PdfDocumentBuilder { ArchiveStandard = PdfAStandard.None };
        builder.DocumentInformation.Title = doc.Title;
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author)) builder.DocumentInformation.Author = author;
        if (doc.Metadata.TryGetValue(DocumentMetadata.Subject, out var subject)) builder.DocumentInformation.Subject = subject;
        if (doc.Metadata.TryGetValue(DocumentMetadata.Keywords, out var keywords)) builder.DocumentInformation.Keywords = keywords;

        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(595, 842);

        var y = 800;
        page.AddText(doc.Title, 14, new PdfPoint(25, y), font);
        foreach (var block in doc.Blocks)
        {
            y -= 16;
            if (y < 40) { page = builder.AddPage(595, 842); y = 800; }
            page.AddText(Truncate(block.Text), 10, new PdfPoint(25, y), font);
        }

        var bytes = builder.Build();
        output.Write(bytes, 0, bytes.Length);
    }

    // 100-char per-line cap keeps generated text on one line. Cross-format equivalence tests must
    // therefore use blocks < 100 chars so PDF text is not truncated relative to MD/DOCX.
    private static string Truncate(string text) => text.Length <= 100 ? text : text[..100];
}
```

> **Backlog (not this milestone):** richer PDF layout — headings, page breaks, wrapped paragraphs, real tables — would improve realism but is deferred. If PdfPig's writer cannot meet a requirement, replace this renderer's body with a minimal hand-rolled emitter. Benchmark-only; the production `PdfParser` is never affected. PDF `Created`/`Modified` are writer-stamped and therefore excluded from the binary determinism comparison (see Task 5).

- [ ] **Step 5: Build to confirm the binary renderers compile**

Run: `dotnet build tests/Ferret.Benchmarks`
Expected: build succeeds. **Note (PdfPig `1.7.0-custom-5`):** `PdfDocumentBuilder` is `IDisposable` in this build — dispose it (`using var builder = ...`) in `PdfRenderer.Render`, mirroring the adaptation already made in Sprint 2's `PdfParserTests`/`WorkspaceFixture`. If `PdfDocumentBuilder.DocumentInformation` member names differ, set the equivalent Title/Author/Subject/Keywords properties it exposes (the writer supports document information); adjust to match.

- [ ] **Step 6: Commit**

```bash
git add tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj tests/Ferret.Benchmarks/Corpus/Renderers/PdfRenderer.cs tests/Ferret.Benchmarks/Corpus/Renderers/DocxRenderer.cs tests/Ferret.Benchmarks/Corpus/Renderers/XlsxRenderer.cs
git commit -m "feat(bench): add PDF/DOCX/XLSX renderers with metadata and typed Excel cells"
```

---

### Task 4: Enterprise tabular archetypes

**Files:**
- Create: `tests/Ferret.Benchmarks/Corpus/EnterpriseArchetypes.cs`

**Interfaces:**
- Consumes: `CorpusDocument`, `CorpusTable`, `CorpusCell` (Task 1); `DocumentMetadata` (`Ferret.Core`).
- Produces: `public static class EnterpriseArchetypes { static IReadOnlyList<CorpusDocument> Build(Random rng); }` — 11 tabular documents (RTM, bug report, sprint backlog, risk register, test execution, release checklist, deployment plan, incident, security findings, **database schema**, **API endpoint inventory**) with typed cells, deterministically varied row counts, and metadata.

- [ ] **Step 1: Implement the archetypes**

```csharp
// tests/Ferret.Benchmarks/Corpus/EnterpriseArchetypes.cs
using System.Globalization;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Builds realistic enterprise tabular documents — the artifacts that motivated Excel support.
/// Cells are typed (text/number/boolean/date) and row counts vary deterministically to give the
/// Excel parser realistic, non-uniform workloads. Deterministic given the RNG.
/// </summary>
public static class EnterpriseArchetypes
{
    // Non-uniform but deterministic row counts (index into this from the seeded RNG).
    private static readonly int[] RowCounts = [75, 120, 200, 350, 900, 1800, 4500];

    /// <summary>Builds one document per archetype, each carrying a single typed <see cref="CorpusTable"/>.</summary>
    /// <param name="rng">Seeded RNG for row content and row-count selection.</param>
    /// <returns>The archetype documents.</returns>
    public static IReadOnlyList<CorpusDocument> Build(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return
        [
            Doc(rng, "Requirement Traceability Matrix",
                ["ID", "Requirement", "Priority", "Status", "Coverage", "Owner"],
                i => [CorpusCell.Text($"REQ-{i:D3}"), CorpusCell.Text(Phrase(rng)), CorpusCell.Text(Pick(rng, "High", "Medium", "Low")), CorpusCell.Text(Pick(rng, "Open", "Done")), CorpusCell.Number(rng.Next(0, 101)), CorpusCell.Text(Pick(rng, "Alice", "Bob", "Chandra"))]),
            Doc(rng, "Bug Report Export",
                ["Key", "Summary", "Severity", "Resolved", "Assignee", "Created"],
                i => [CorpusCell.Text($"BUG-{i:D3}"), CorpusCell.Text(Phrase(rng)), CorpusCell.Text(Pick(rng, "Blocker", "Major", "Minor")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Text(Pick(rng, "Alice", "Bob")), CorpusCell.Date(new DateOnly(2026, 1, 1 + (i % 27)))]),
            Doc(rng, "Sprint Backlog",
                ["Story", "Points", "Sprint", "State", "Epic"],
                i => [CorpusCell.Text($"STORY-{i:D3}"), CorpusCell.Number(Pick(rng, 1, 2, 3, 5, 8)), CorpusCell.Text(Pick(rng, "S-12", "S-13")), CorpusCell.Text(Pick(rng, "To Do", "Doing", "Done")), CorpusCell.Text(Pick(rng, "Search", "Indexing"))]),
            Doc(rng, "Risk Register",
                ["Risk", "Likelihood", "Impact", "Mitigation", "Owner"],
                i => [CorpusCell.Text($"RISK-{i:D3}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "Low", "Medium", "High")), CorpusCell.Text(Pick(rng, "Low", "Medium", "High")), CorpusCell.Text(Phrase(rng)), CorpusCell.Text(Pick(rng, "Alice", "Bob"))]),
            Doc(rng, "Test Execution Report",
                ["Test", "Passed", "Duration", "Build", "Tester"],
                i => [CorpusCell.Text($"TC-{i:D3}"), CorpusCell.Boolean(rng.Next(3) != 0), CorpusCell.Number(rng.Next(1, 900)), CorpusCell.Text($"build-{rng.Next(100, 999)}"), CorpusCell.Text(Pick(rng, "Alice", "Bob"))]),
            Doc(rng, "Release Checklist",
                ["Item", "Owner", "Status", "Due", "Notes"],
                i => [CorpusCell.Text($"Item {i}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "Alice", "Bob", "Chandra")), CorpusCell.Text(Pick(rng, "Pending", "Done", "Blocked")), CorpusCell.Date(new DateOnly(2026, 2, 1 + (i % 27))), CorpusCell.Text(Phrase(rng))]),
            Doc(rng, "Deployment Plan",
                ["Step", "Environment", "Owner", "Rollback", "Status"],
                i => [CorpusCell.Text($"Step {i}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "Dev", "Staging", "Prod")), CorpusCell.Text(Pick(rng, "Alice", "Bob")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Text(Pick(rng, "Planned", "Complete"))]),
            Doc(rng, "Production Incident",
                ["Incident", "Severity", "Detected", "Resolved", "Root Cause"],
                i => [CorpusCell.Text($"INC-{i:D3}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "SEV1", "SEV2", "SEV3")), CorpusCell.Date(new DateOnly(2026, 1, 15)), CorpusCell.Boolean(true), CorpusCell.Text(Phrase(rng))]),
            Doc(rng, "Security Findings",
                ["Finding", "CVSS", "Component", "Fixed", "Remediation"],
                i => [CorpusCell.Text($"SEC-{i:D3}: {Phrase(rng)}"), CorpusCell.Number(PickD(rng, 3.1, 5.4, 7.8, 9.1)), CorpusCell.Text(Pick(rng, "auth", "index", "api")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Text(Phrase(rng))]),
            Doc(rng, "Database Schema",
                ["Table", "Column", "Type", "Nullable", "Indexed"],
                i => [CorpusCell.Text(Pick(rng, "documents", "assets", "chunks")), CorpusCell.Text($"col_{i}"), CorpusCell.Text(Pick(rng, "text", "integer", "boolean", "timestamp")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Boolean(rng.Next(3) == 0)]),
            Doc(rng, "API Endpoint Inventory",
                ["Path", "Method", "Auth", "Deprecated", "Owner"],
                i => [CorpusCell.Text($"/api/v1/resource{i}"), CorpusCell.Text(Pick(rng, "GET", "POST", "PUT", "DELETE")), CorpusCell.Boolean(rng.Next(4) != 0), CorpusCell.Boolean(rng.Next(5) == 0), CorpusCell.Text(Pick(rng, "Alice", "Bob"))]),
        ];
    }

    private static CorpusDocument Doc(
        Random rng, string title, string[] headers, Func<int, IReadOnlyList<CorpusCell>> row)
    {
        var rows = RowCounts[rng.Next(RowCounts.Length)];
        var data = new List<IReadOnlyList<CorpusCell>>(rows);
        for (var i = 1; i <= rows; i++) data.Add(row(i));
        var metadata = Metadata(rng, title, "Data");
        return new CorpusDocument(title, metadata, [], [new CorpusTable(headers, data)]);
    }

    private static IReadOnlyDictionary<string, string> Metadata(Random rng, string subject, string category) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DocumentMetadata.Author] = Pick(rng, "Alice", "Bob", "Chandra"),
            [DocumentMetadata.Subject] = subject,
            [DocumentMetadata.Category] = category,
        };

    private static readonly string[] Terms =
        ["login", "export", "index", "search", "auth", "cache", "report", "sync", "upload", "filter"];

    private static string Phrase(Random rng) =>
        string.Create(CultureInfo.InvariantCulture, $"{Terms[rng.Next(Terms.Length)]} {Terms[rng.Next(Terms.Length)]}");

    private static string Pick(Random rng, params string[] options) => options[rng.Next(options.Length)];

    private static int Pick(Random rng, params int[] options) => options[rng.Next(options.Length)];

    private static double PickD(Random rng, params double[] options) => options[rng.Next(options.Length)];
}
```

- [ ] **Step 2: Build to confirm it compiles**

Run: `dotnet build tests/Ferret.Benchmarks`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add tests/Ferret.Benchmarks/Corpus/EnterpriseArchetypes.cs
git commit -m "feat(bench): add 11 enterprise tabular archetypes with typed cells and varied row counts"
```

---

### Task 5: Seeded generator (hierarchy + manifest) + tests

**Files:**
- Create: `tests/Ferret.Benchmarks/Corpus/CorpusManifest.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs`
- Create: `tests/Ferret.Benchmarks.Tests/Ferret.Benchmarks.Tests.csproj`
- Create: `tests/Ferret.Benchmarks.Tests/Corpus/CorpusGeneratorTests.cs`
- Create: `tests/Ferret.Benchmarks.Tests/Corpus/RendererTests.cs`

**Interfaces:**
- Consumes: all renderers (Tasks 2–3), `EnterpriseArchetypes` (Task 4), `CorpusSize`, `CorpusDocument`, `CorpusCell` (Task 1); `DocumentMetadata` (`Ferret.Core`).
- Produces: `sealed record CorpusManifest(...)`; `public sealed class SyntheticEnterpriseCorpusGenerator { const string GeneratorVersion; SyntheticEnterpriseCorpusGenerator(int seed); void Generate(CorpusSize size, string outputRoot); }` — emits a realistic enterprise tree plus `corpus.json` at the root.

- [ ] **Step 1: Write the failing determinism + coverage tests**

```csharp
// tests/Ferret.Benchmarks.Tests/Corpus/CorpusGeneratorTests.cs
using System.Text.Json;

using Ferret.Benchmarks.Corpus;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;

namespace Ferret.Benchmarks.Tests.Corpus;

public sealed class CorpusGeneratorTests
{
    // Text formats + the manifest must be byte-identical; binary formats compare extracted text + metadata.
    private static readonly string[] TextExtensions = [".md", ".html", ".cs", ".json"];

    [Fact]
    public void Same_Seed_Text_Formats_And_Manifest_Are_Byte_Identical()
    {
        Run((dirA, dirB) =>
        {
            foreach (var ext in TextExtensions.Append(".manifest"))
            {
                var pattern = ext == ".manifest" ? "corpus.json" : "*" + ext;
                var filesA = Directory.GetFiles(dirA, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirA)).ToList();
                var filesB = Directory.GetFiles(dirB, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirB)).ToList();
                Assert.Equal(filesA.Count, filesB.Count);
                for (var i = 0; i < filesA.Count; i++)
                {
                    Assert.Equal(File.ReadAllBytes(filesA[i]), File.ReadAllBytes(filesB[i]));
                }
            }
        });
    }

    [Fact]
    public void Same_Seed_Binary_Formats_Have_Identical_Extracted_Text_And_Metadata()
    {
        Run(async (dirA, dirB) =>
        {
            await AssertBinaryEquivalent(dirA, dirB, "*.pdf", (s, a) => new PdfParser(new ParserOptions()).ParseAsync(s, a));
            await AssertBinaryEquivalent(dirA, dirB, "*.docx", (s, a) => new WordParser(new ParserOptions()).ParseAsync(s, a));
            await AssertBinaryEquivalent(dirA, dirB, "*.xlsx", (s, a) => new ExcelParser(new ParserOptions()).ParseAsync(s, a));
        }).GetAwaiter().GetResult();
    }

    [Fact]
    public void Small_Corpus_Emits_Enterprise_Hierarchy_And_Manifest()
    {
        var dir = NewDir();
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 1).Generate(CorpusSize.Small, dir);

            Assert.True(File.Exists(Path.Join(dir, "corpus.json")));
            Assert.True(Directory.Exists(Path.Join(dir, "Engineering")));
            Assert.True(Directory.Exists(Path.Join(dir, "Operations")));
            Assert.True(Directory.Exists(Path.Join(dir, "Quality")));
            Assert.True(Directory.Exists(Path.Join(dir, "Management")));

            Assert.NotEmpty(Directory.GetFiles(dir, "*.pdf", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.docx", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.xlsx", SearchOption.AllDirectories));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories));

            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Join(dir, "corpus.json")));
            Assert.Equal(1, manifest.RootElement.GetProperty("seed").GetInt32());
            Assert.Equal("Small", manifest.RootElement.GetProperty("size").GetString());
            Assert.True(manifest.RootElement.GetProperty("documentCount").GetInt32() > 0);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    private static void Run(Action<string, string> assert)
    {
        var dirA = NewDir();
        var dirB = NewDir();
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirA);
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirB);
            assert(dirA, dirB);
        }
        finally
        {
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
        }
    }

    private static async Task Run(Func<string, string, Task> assert)
    {
        var dirA = NewDir();
        var dirB = NewDir();
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirA);
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirB);
            await assert(dirA, dirB);
        }
        finally
        {
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
        }
    }

    private static async Task AssertBinaryEquivalent(
        string dirA, string dirB, string pattern, Func<Stream, ParseContext, ValueTask<Document>> parse)
    {
        var filesA = Directory.GetFiles(dirA, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirA)).ToList();
        var filesB = Directory.GetFiles(dirB, pattern, SearchOption.AllDirectories).OrderBy(RelPath(dirB)).ToList();
        Assert.Equal(filesA.Count, filesB.Count);
        for (var i = 0; i < filesA.Count; i++)
        {
            var a = await ParseFile(filesA[i], parse);
            var b = await ParseFile(filesB[i], parse);
            Assert.Equal(a.PlainText, b.PlainText);
            Assert.Equal(Stable(a.Metadata), Stable(b.Metadata)); // exclude writer-stamped timestamps
        }
    }

    private static async Task<Document> ParseFile(string path, Func<Stream, ParseContext, ValueTask<Document>> parse)
    {
        await using var fs = File.OpenRead(path);
        var uri = new Uri("filesystem:///" + Path.GetFileName(path));
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("bench"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = Path.GetFileName(path),
            LastModified = DateTimeOffset.UnixEpoch,
            MediaType = "application/octet-stream",
        };
        return await parse(fs, ParseContext.For(asset));
    }

    // PDF writers stamp Created/Modified from the wall clock; exclude those keys from the comparison.
    private static IEnumerable<KeyValuePair<string, string>> Stable(IReadOnlyDictionary<string, string> m) =>
        m.Where(kv => kv.Key != DocumentMetadata.Created && kv.Key != DocumentMetadata.Modified)
         .OrderBy(kv => kv.Key, StringComparer.Ordinal);

    private static Func<string, string> RelPath(string root) => p => Path.GetRelativePath(root, p);

    private static string NewDir() => Path.Join(Path.GetTempPath(), "corpus-" + Guid.NewGuid().ToString("N"));
}
```

- [ ] **Step 2: Create the test project and add it to the solution**

```xml
<!-- tests/Ferret.Benchmarks.Tests/Ferret.Benchmarks.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.Benchmarks.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Ferret.Benchmarks\Ferret.Benchmarks.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Parsers.Pdf\Ferret.Parsers.Pdf.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Parsers.Office\Ferret.Parsers.Office.csproj" />
  </ItemGroup>
</Project>
```

```bash
dotnet sln src/Ferret.sln add tests/Ferret.Benchmarks.Tests/Ferret.Benchmarks.Tests.csproj
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/Ferret.Benchmarks.Tests`
Expected: FAIL — `SyntheticEnterpriseCorpusGenerator` / `CorpusManifest` do not exist.

- [ ] **Step 4: Implement the manifest record**

```csharp
// tests/Ferret.Benchmarks/Corpus/CorpusManifest.cs
namespace Ferret.Benchmarks.Corpus;

/// <summary>Deterministic description of a generated corpus, serialized to corpus.json for
/// reproducibility and benchmark diagnostics. Contains no timestamps or random identifiers.</summary>
public sealed record CorpusManifest(
    string GeneratorVersion,
    int Seed,
    string Size,
    int DocumentCount,
    IReadOnlyDictionary<string, int> FormatCounts,
    IReadOnlyDictionary<string, int> ArchetypeCounts);
```

- [ ] **Step 5: Implement the generator (enterprise hierarchy + manifest)**

```csharp
// tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs
using System.Globalization;
using System.Text.Json;

using Ferret.Benchmarks.Corpus.Renderers;
using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Generates a deterministic, multi-format synthetic enterprise corpus laid out under a realistic
/// enterprise folder tree (Engineering / Operations / Quality / Management), plus a corpus.json
/// manifest. Same seed + size produces identical output (byte-identical text; identical extracted
/// text/metadata for binaries). Reusable beyond benchmarks; lives in the benchmark project.
/// </summary>
public sealed class SyntheticEnterpriseCorpusGenerator
{
    /// <summary>Manifest schema/generator version. Bump when the layout or content changes.</summary>
    public const string GeneratorVersion = "1.0";

    // Prose title families per role, keeping generated documents recognizably enterprise-like.
    private static readonly string[] AdrTitles = ["Architecture Decision {0}", "Design Proposal {0}", "RFC {0}"];
    private static readonly string[] DocTitles = ["Design Specification {0}", "Knowledge Base Article {0}", "Configuration Guide {0}"];
    private static readonly string[] RunbookTitles = ["Runbook {0}", "Operations Guide {0}"];
    private static readonly string[] IncidentTitles = ["Incident Report {0}", "Postmortem {0}"];
    private static readonly string[] SpecTitles = ["Technical Specification {0}", "Interface Design {0}"];
    private static readonly string[] PlanningTitles = ["Sprint {0} Planning", "Quarterly Review {0}", "Release Notes {0}"];
    private static readonly string[] SourceTitles = ["Service {0}", "Repository {0}", "Controller {0}"];
    private static readonly string[] MixedTitles = ["Meeting Minutes {0}", "Status Update {0}"];

    private static readonly string[] Names = ["Alice", "Bob", "Chandra", "Dana", "Omar", "Priya"];

    // Deterministic sentence templates — natural prose without unseeded randomness.
    private static readonly string[] SentenceTemplates =
    [
        "The indexing pipeline stores extracted content in the workspace.",
        "The connector periodically synchronizes remote repositories.",
        "Search latency improved after introducing compression.",
        "The deployment failed because authentication tokens expired.",
        "Retrieval quality is measured across code and documents.",
        "The parser extracts text and lightweight metadata from each stream.",
        "Context assembly ranks candidates before returning the top results.",
        "Throughput scales with the number of connector instances.",
    ];

    private readonly int _seed;

    /// <summary>Initializes a new generator with a fixed RNG seed for reproducibility.</summary>
    /// <param name="seed">The RNG seed.</param>
    public SyntheticEnterpriseCorpusGenerator(int seed) => _seed = seed;

    private sealed record Entry(string RelativePath, IDocumentRenderer Renderer, int Count, string[]? ProseTitles);

    /// <summary>Generates the corpus into <paramref name="outputRoot"/> and writes corpus.json.</summary>
    /// <param name="size">The corpus size tier.</param>
    /// <param name="outputRoot">The destination directory (created if missing).</param>
    public void Generate(CorpusSize size, string outputRoot)
    {
        ArgumentNullException.ThrowIfNull(outputRoot);
        var rng = new Random(_seed); // single seeded RNG drives all content => deterministic
        var layout = LayoutFor(size);

        var formatCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var archetypeCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var documentCount = 0;

        foreach (var entry in layout)
        {
            var dir = Path.Join(new[] { outputRoot }.Concat(entry.RelativePath.Split('/')).ToArray());
            Directory.CreateDirectory(dir);

            for (var i = 0; i < entry.Count; i++)
            {
                CorpusDocument doc;
                if (entry.ProseTitles is null)
                {
                    // Tabular role: cycle the archetypes (rebuilt per doc so RNG advances deterministically).
                    var archetypes = EnterpriseArchetypes.Build(rng);
                    doc = archetypes[i % archetypes.Count];
                    archetypeCounts[doc.Title] = archetypeCounts.GetValueOrDefault(doc.Title) + 1;
                }
                else
                {
                    doc = BuildProse(rng, i, entry.ProseTitles);
                }

                var fileName = string.Create(CultureInfo.InvariantCulture, $"doc{i:D5}{entry.Renderer.Extension}");
                using (var fs = File.Create(Path.Join(dir, fileName)))
                {
                    entry.Renderer.Render(doc, fs);
                }

                formatCounts[entry.Renderer.Extension] = formatCounts.GetValueOrDefault(entry.Renderer.Extension) + 1;
                documentCount++;
            }
        }

        WriteManifest(outputRoot, size, documentCount, formatCounts, archetypeCounts);
    }

    private CorpusDocument BuildProse(Random rng, int index, string[] titleTemplates)
    {
        var blocks = new List<CorpusBlock>();
        var paraCount = 3 + rng.Next(5);
        for (var p = 0; p < paraCount; p++)
        {
            blocks.Add(new CorpusBlock(CorpusBlockKind.Paragraph, SentenceTemplates[rng.Next(SentenceTemplates.Length)]));
        }

        var template = titleTemplates[rng.Next(titleTemplates.Length)];
        var title = string.Format(CultureInfo.InvariantCulture, template, index);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DocumentMetadata.Author] = Names[rng.Next(Names.Length)],
            [DocumentMetadata.Subject] = title,
            [DocumentMetadata.Category] = "Prose",
        };
        return new CorpusDocument(title, metadata, blocks, Tables: []);
    }

    private void WriteManifest(
        string outputRoot, CorpusSize size, int documentCount,
        IReadOnlyDictionary<string, int> formatCounts, IReadOnlyDictionary<string, int> archetypeCounts)
    {
        var manifest = new CorpusManifest(
            GeneratorVersion, _seed, size.ToString(), documentCount, formatCounts, archetypeCounts);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Join(outputRoot, "corpus.json"), JsonSerializer.Serialize(manifest, options));
    }

    // Realistic enterprise tree; every binary format (.pdf/.docx/.xlsx) and code/text appears.
    private static IReadOnlyList<Entry> LayoutFor(CorpusSize size)
    {
        var c = CountsFor(size);
        return
        [
            new("Engineering/Source", new CSharpRenderer(), c.Code, SourceTitles),
            new("Engineering/Docs", new MarkdownRenderer(), c.Docs, DocTitles),
            new("Engineering/ADR", new MarkdownRenderer(), c.Adr, AdrTitles),
            new("Engineering/Specs", new DocxRenderer(), c.Word, SpecTitles),
            new("Operations/Runbooks", new MarkdownRenderer(), c.Runbooks, RunbookTitles),
            new("Operations/Incidents", new PdfRenderer(), c.Pdf, IncidentTitles),
            new("Quality/Matrices", new XlsxRenderer(), c.Excel, ProseTitles: null), // tabular archetypes
            new("Management/Planning", new PdfRenderer(), c.Planning, PlanningTitles),
            new("Management/Notes", new JsonRenderer(), c.Json, MixedTitles),
            new("Management/Portal", new HtmlRenderer(), c.Html, MixedTitles),
        ];
    }

    private static (int Code, int Docs, int Adr, int Word, int Runbooks, int Pdf, int Excel, int Planning, int Json, int Html) CountsFor(CorpusSize size) => size switch
    {
        CorpusSize.Small => (60, 20, 15, 20, 15, 20, 20, 10, 8, 6),
        CorpusSize.Medium => (700, 200, 150, 200, 120, 200, 200, 100, 80, 50),
        CorpusSize.Enterprise => (6000, 1500, 1000, 1500, 800, 1500, 1500, 600, 400, 200),
        _ => (60, 20, 15, 20, 15, 20, 20, 10, 8, 6),
    };
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Ferret.Benchmarks.Tests`
Expected: PASS. The two byte-identical text/manifest assertions and the binary extracted-text+metadata assertions all hold. If a binary extracted-text comparison ever fails, it indicates a real non-determinism source in a renderer (unseeded value) — fix the renderer, do not weaken the assertion.

- [ ] **Step 7: Write the per-renderer + cross-format equivalence tests**

```csharp
// tests/Ferret.Benchmarks.Tests/Corpus/RendererTests.cs
using System.Text;

using Ferret.Benchmarks.Corpus;
using Ferret.Benchmarks.Corpus.Renderers;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;

namespace Ferret.Benchmarks.Tests.Corpus;

public sealed class RendererTests
{
    // Blocks kept < 100 chars so the PDF renderer does not truncate relative to Markdown/DOCX.
    private static CorpusDocument SampleProse() => new(
        "Design Proposal 7",
        new Dictionary<string, string>(StringComparer.Ordinal) { [DocumentMetadata.Author] = "Alice" },
        [
            new CorpusBlock(CorpusBlockKind.Paragraph, "The indexing pipeline stores content."),
            new CorpusBlock(CorpusBlockKind.Paragraph, "Search latency improved after compression."),
        ],
        Tables: []);

    [Theory]
    [InlineData(".md")]
    [InlineData(".html")]
    [InlineData(".cs")]
    [InlineData(".json")]
    public void Text_Renderer_Emits_NonEmpty_File_Containing_Title(string ext)
    {
        IDocumentRenderer renderer = ext switch
        {
            ".md" => new MarkdownRenderer(),
            ".html" => new HtmlRenderer(),
            ".cs" => new CSharpRenderer(),
            _ => new JsonRenderer(),
        };
        using var ms = new MemoryStream();

        renderer.Render(SampleProse(), ms);

        var text = Encoding.UTF8.GetString(ms.ToArray());
        Assert.NotEmpty(text);
        // C# sanitizes the title into a type name; assert a title token survives in that form.
        var expected = ext == ".cs" ? "DesignProposal7" : "Design Proposal 7";
        Assert.Contains(expected, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cross_Format_Renderers_Preserve_Content_Tokens()
    {
        var doc = SampleProse();
        var expected = new[] { "indexing", "pipeline", "content", "latency", "compression" };

        var mdText = ExtractedFrom(new MarkdownRenderer(), doc, null);
        var docxText = ExtractedFrom(new DocxRenderer(), doc, (s, a) => new WordParser(new ParserOptions()).ParseAsync(s, a));
        var pdfText = ExtractedFrom(new PdfRenderer(), doc, (s, a) => new PdfParser(new ParserOptions()).ParseAsync(s, a));

        foreach (var token in expected)
        {
            Assert.Contains(token, Normalize(mdText), StringComparison.Ordinal);
            Assert.Contains(token, Normalize(docxText), StringComparison.Ordinal);
            Assert.Contains(token, Normalize(pdfText), StringComparison.Ordinal);
        }
    }

    // For MD (no parser), read the rendered bytes directly; for binaries, parse and take PlainText.
    private static string ExtractedFrom(
        IDocumentRenderer renderer, CorpusDocument doc, Func<Stream, ParseContext, ValueTask<Document>>? parse)
    {
        using var ms = new MemoryStream();
        renderer.Render(doc, ms);
        ms.Position = 0;
        if (parse is null)
        {
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        var uri = new Uri("filesystem:///sample" + renderer.Extension);
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("bench"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = "sample" + renderer.Extension,
            LastModified = DateTimeOffset.UnixEpoch,
            MediaType = "application/octet-stream",
        };
        return parse(ms, ParseContext.For(asset)).GetAwaiter().GetResult().PlainText;
    }

    // Normalized semantic comparison: collapse whitespace to single spaces, lower-case.
    private static string Normalize(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
```

- [ ] **Step 8: Run the renderer tests to verify they pass**

Run: `dotnet test tests/Ferret.Benchmarks.Tests --filter RendererTests`
Expected: PASS. The cross-format test asserts **normalized token containment** (not exact string equality) because PDF/DOCX/MD extraction differ in whitespace and line breaks. If a token is missing from the PDF path, confirm the block text is under the 100-char PDF line cap.

- [ ] **Step 9: Commit**

```bash
git add tests/Ferret.Benchmarks/Corpus/CorpusManifest.cs tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs tests/Ferret.Benchmarks.Tests src/Ferret.sln
git commit -m "feat(bench): add corpus generator with enterprise hierarchy, manifest, and determinism/equivalence tests"
```

---

## Final verification

- [ ] **Full solution build + test**

Run: `dotnet build src/Ferret.sln && dotnet test src/Ferret.sln`
Expected: build clean, all tests green.

- [ ] **Acceptance criteria check**

Confirm each: same seed + size ⇒ byte-identical text formats + `corpus.json`, and identical extracted text + metadata (excluding writer timestamps) for `.pdf`/`.docx`/`.xlsx` · a Small corpus emits the Engineering/Operations/Quality/Management tree with non-empty `.pdf`/`.docx`/`.xlsx`/`.cs` · the 11 tabular archetypes render as real Excel sheets with typed cells (number/boolean/date exercise the parser's non-shared-string path) and deterministically varied row counts · `CorpusDocument.Metadata` uses `DocumentMetadata` constants and flows through every renderer/parser · `corpus.json` records seed/size/version/documentCount/formatCounts/archetypeCounts deterministically · per-renderer validation + normalized cross-format equivalence pass · no DI in the benchmark tool · no unseeded `Random`/wall-clock/`Guid` in content or manifest · generated output lives only under temp and is deleted · no `Version` attributes on `<PackageReference>`.
