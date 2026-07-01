# Enterprise Content Pack 1 — Design

**Date:** 2026-07-01
**Status:** Approved for implementation (rev 2 — Excel added, milestone renamed)
**Author:** Captured from product session

> **Naming:** this milestone was renamed from "Parser Pack 1" to **Enterprise Content Pack 1**
> to reflect its value story — indexing the documents enterprises actually run on
> (PDF, Word, Excel). **Technical asset names are unchanged**: the packages remain
> `Ferret.Parsers.Pdf` / `Ferret.Parsers.Office` / `Ferret.Parsers`, and the
> composition entry point remains `ParserPackModule`.

---

## Problem Statement

Ferret today indexes text-based files only. Three content parsers exist
(`PlainTextParser`, `MarkdownParser`, `JsonParser`), all inside
`Ferret.ParserPlatform`. Binary formats — PDF and Microsoft Office documents —
are not extracted: `MimeTypeResolver` maps `.pdf`/`.docx`/`.xlsx`/`.pptx` to
`application/octet-stream`, no parser claims that media type, and the pipeline
emits a `DocumentSkippedEvent`. As a result, enterprise documentation never
enters the index.

Enterprise Content Pack 1 makes Ferret able to index both **software
repositories** and **enterprise documentation**, so that benchmarking (the next
roadmap phase) measures Ferret against the document types real users work with —
not only source code and plain text.

**Why Excel matters:** although native Jira integration comes later, nearly every
enterprise exports Jira, Azure DevOps, test-case matrices, requirement
traceability matrices, bug reports, sprint backlogs, risk registers, asset
inventories, and compliance reports to `.xlsx`. For knowledge retrieval these
tabular artifacts are frequently *more* valuable than presentations — which is
why Excel is in scope for this milestone and PowerPoint is not.

**Roadmap placement:** `v0.15.x → Enterprise Content Pack 1 (this) →
Benchmarking → Dogfooding (DOGFOOD-001) → GA`. This pack lands before dogfooding
so evaluation reflects realistic enterprise usage.

---

## Goals

Enterprise Content Pack 1 ships:

1. **Expanded text/code/config MIME mappings** with improved `DocumentKind`
   classification.
2. **An expanded binary denylist** preventing opaque binary artifacts from
   entering the text index.
3. **`Ferret.Parsers.Pdf`** — `PdfParser` using UglyToad.PdfPig.
4. **`Ferret.Parsers.Office`** — `WordParser` (**DOCX**) and `ExcelParser`
   (**XLSX**), both on DocumentFormat.OpenXml.
5. **Additive `MimeTypeResolver` changes** so PDF, DOCX, and XLSX resolve to
   dedicated media types that dispatch to their parsers, while preserving the
   existing parser contracts.
6. **A distinct content model for parseable binaries** (PDF/DOCX/XLSX) vs opaque
   binaries, so future binary-skip logic does not exclude parseable formats.
7. **A deterministic, multi-format corpus generator** (abstract model +
   format-specific renderers, including a **tabular `CorpusTable`** primitive and
   Excel-specific enterprise datasets) producing realistic documents and mixed
   code repositories at configurable sizes. Assets are **not** committed.
8. **A configurable extracted-text limit** (default **unlimited**) so large
   workbooks index completely unless an administrator caps them.

### Non-goals (deferred, YAGNI)

- **`.pptx` (PowerPoint).** Technically cheap once OpenXml is in the Office
  package (slide text extracts much like Word), but lower retrieval value than
  spreadsheets. Deferred as a **fast-follow** — a small increment, not a new
  project.
- Spreadsheet **calculation, formula evaluation, or editing** (see the parser
  principle). Excel extraction reads cached values only.
- DOCX comments extraction.
- OCR for scanned / image-only PDFs.
- Legacy `.doc` / `.xls` (pre-2007 binary Office formats).
- Extraction of the built-in text parsers into a separate `Ferret.Parsers.Text`
  package — deferred until post-GA.

---

## Parser Design Principle

> **Parsers are responsible only for extracting text and lightweight metadata
> from a stream.** They MUST NOT perform chunking, tokenization, embedding,
> summarization, AI processing, **spreadsheet calculation, or formula
> evaluation.** Those concerns belong to downstream layers. A parser's single
> job is: `Stream → Document { text, DocumentKind, lightweight metadata }`.

Ferret is a search and context platform, not an Excel engine. The Excel parser
extracts *searchable enterprise knowledge* (cell values, sheet structure), never
recomputing formulas — it reads the value already stored in the cell.

This keeps parser packages dependency-light, deterministic, independently
testable, and safe to run in any host. It also preserves the existing
`IContentParser` contract (`CanParse` is pure; the parser assigns
`DocumentKind`; the stream is not disposed by the parser).

### Lightweight metadata

"Lightweight metadata" means values cheap to read from the source's own headers —
**no contract change required**. `Document` already exposes `Title`, a
`Metadata` string→string dictionary, and `Sections` (`Document.cs:44-53`).
Parsers populate `Document.Title` and these standard `Metadata` keys when the
format provides them:

| Key          | Source                    | Notes                          |
| ------------ | ------------------------- | ------------------------------ |
| `Author`     | PDF / DOCX / XLSX core    |                                |
| `Subject`    | PDF / DOCX core           |                                |
| `Keywords`   | PDF / DOCX core           |                                |
| `PageCount`  | PDF                       | page count                     |
| `SheetCount` | XLSX                      | number of worksheets           |
| `Created`    | PDF / DOCX / XLSX core    | ISO-8601                       |
| `Modified`   | PDF / DOCX / XLSX core    | ISO-8601                       |
| `Category`   | DOCX / XLSX core props    |                                |

`Title` maps to `Document.Title`. Missing values are simply omitted (no empty
keys). Ferret need not consume every key today — capturing it now is free and
lets downstream ranking/AI use it later without re-parsing.

**Metadata keys are constants, not raw strings.** All keys are defined once on a
`DocumentMetadata` static class (`Ferret.Core.Documents`) — `DocumentMetadata.Author`,
`.Subject`, `.Keywords`, `.PageCount`, `.SheetCount`, `.Created`, `.Modified`,
`.Category`, `.Truncated`. Every parser references those constants so keys never
drift (`PageCount` vs `Pagecount` vs `Page Count`) as future parsers are added.

### DocumentKind

- **PDF, DOCX → `DocumentKind.Prose`** (documents).
- **XLSX → `DocumentKind.Data`** — a spreadsheet (Jira export, RTM, risk
  register) is structured data, not prose; ranking/context should treat it as
  such.

Future content types may refine `DocumentKind` **without changing parser
contracts** — `DocumentKind` is an output value, not part of the `IContentParser`
signature.

### Reserved extension point

Every parser returns a `Document` today. Future formats (OCR, PowerPoint,
Outlook, OneNote) may want richer, structured extraction. To reserve the seam
now at zero cost, add a **`ParserCapabilities.StructuredExtraction`** capability
constant — **unused this milestone**, declared so parsers can advertise it later
without a contract change. No behavior is attached to it yet.

---

## Architecture

### Package layout (Option 2 — platform stays intact)

`Ferret.ParserPlatform` is **unchanged in responsibility**: it continues to own
the parser registry, dispatcher, `MimeTypeResolver`, and the three built-in
parsers. Heavyweight-format parsers live in new sibling packages so their
dependencies (PdfPig, OpenXml) never leak into the platform or the text parsers.

```
src/Ferret.ParserPlatform/      (unchanged: registry, dispatcher,
                                 MimeTypeResolver, PlainText/Markdown/Json)
src/Ferret.Parsers.Pdf/         PdfParser              dep: UglyToad.PdfPig
src/Ferret.Parsers.Office/      WordParser, ExcelParser  dep: DocumentFormat.OpenXml
                                 (future fast-follow: PowerPointParser)
src/Ferret.Parsers/             ParserPackModule (composition only)
                                 refs: ParserPlatform + Pdf + Office
```

**Why Office stays one package:** Word and Excel share the OpenXml dependency and
are cohesive. Splitting into `Parsers.Word` / `Parsers.Excel` would be
dependency-isolation theater with no payoff. PDF stays separate because it has a
different dependency (PdfPig).

### Composition: `ParserPackModule`

A single composition project, **`Ferret.Parsers`**, references the platform and
both parser packages and exposes one entry point:

```csharp
public static class ParserPackModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        ParserPlatformModule.ConfigureServices(services); // registry, dispatcher, resolver, built-ins
        PdfParserModule.ConfigureServices(services);       // AddSingleton<IContentParser, PdfParser>
        OfficeParserModule.ConfigureServices(services);    // Word + Excel parsers
    }
}
```

Hosts that currently call `ParserPlatformModule.ConfigureServices` switch to
`ParserPackModule`. Because `ParserPlatform` must not depend on the heavyweight
packages, `ParserPackModule` lives in the dedicated `Ferret.Parsers` composition
project, which also future-proofs adding PowerPoint/HTML/XML packs.

**Callsite migration:** every site that currently calls
`ParserPlatformModule.ConfigureServices` switches to
`ParserPackModule.ConfigureServices`. Known callsite: `IndexCliModule.cs:68`.
Implementation greps for all callsites (index, serve/MCP, any test host).

### Why the registry needs no changes

`IParserRegistry` is built from `sp.GetServices<IContentParser>()`
(`ParserPlatformModule.cs:23-24`) — it aggregates **every** registered
`IContentParser`. New parsers are picked up automatically once registered; the
registry, dispatcher, and `IContentParser` contract are untouched. This matches
the established `McpModule` / `AiModule` / `ModelPlatformModule` pattern.

### Data flow (unchanged except the resolver mapping)

```
Filesystem Connector → MimeTypeResolver → IParserRegistry → IContentParser → Index Engine
```

Only `MimeTypeResolver`'s extension→media-type table and the `MediaTypeInfo`
content model change. The dispatch mechanism, registry, and parser contract are
unchanged.

---

## MimeTypeResolver & Content Model Changes (additive, in ParserPlatform)

### Content category enum

Replace the two booleans on `MediaTypeInfo` (`Ferret.Core.Documents`) with a
small enum that expresses intent and distinguishes the three cases:

```csharp
public enum MediaCategory
{
    Text,            // extractable as-is by a text/* parser
    BinaryParseable, // binary, but a parser can extract text (PDF, DOCX, XLSX)
    BinaryOpaque,    // binary with no text content (images, executables, fonts)
}
```

`MediaTypeInfo` gains `MediaCategory Category { get; init; }`. To avoid touching
the (few) existing readers of `IsText` / `IsBinary`, those remain as **computed
properties** derived from `Category`:

- `IsText => Category == MediaCategory.Text`
- `IsBinary => Category != MediaCategory.Text`

Resolver factories:

- `Text(mediaType, kind)` → `Category = Text`
- `ParseableBinary(mediaType, kind)` → `Category = BinaryParseable`
- `Binary()` → `Category = BinaryOpaque`

**Future binary-skip logic** keys off `Category == BinaryOpaque`, so
PDF/DOCX/XLSX (`BinaryParseable`) are never dropped. (This is the design decision
Excel *validated*: the enum absorbs a third parseable format with zero change.)

### New parseable-binary mappings

| Extension | Media type                                                                | Category          | DocumentKind |
| --------- | ------------------------------------------------------------------------- | ----------------- | ------------ |
| `.pdf`    | `application/pdf`                                                          | `BinaryParseable` | `Prose`      |
| `.docx`   | `application/vnd.openxmlformats-officedocument.wordprocessingml.document`  | `BinaryParseable` | `Prose`      |
| `.xlsx`   | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`        | `BinaryParseable` | `Data`       |

`.pptx` remains `BinaryOpaque` this milestone (deferred fast-follow).

### Expanded text/code/config mappings

The resolver already maps ~40 extensions. **Note:** unmapped extensions already
fall through to a `text/plain` default (`MimeTypeResolver.cs:86-92,110`), so
unmapped source files are *already indexed* — explicit mappings improve
`DocumentKind` accuracy and confidence (1.0 vs 0.5). Gaps to fill (illustrative):

`.scss .less .php .scala .clj .cljs .edn .ex .exs .erl .dart .lua .r .pl
.groovy .gradle .bat .cmd .psm1 .psd1 .ini .cfg .conf .env .properties
.dockerfile .makefile .cmake .rst .adoc .tex .vb .fs .fsx .csproj .vbproj
.fsproj .props .targets .resx .xaml .gitignore .editorconfig`

### Expanded binary denylist

Map opaque artifacts to `BinaryOpaque` so they stop leaking into the text index.
Several are **already** mapped (`MimeTypeResolver.cs:55-83`); the work adds:

`.so .dylib .a .o .lib .class .pyc .pyo .wasm .node .nupkg .snk .pfx .jar .war
.ear .db .sqlite .parquet .dat .keystore .psd .ai .otf`

---

## Configurable extracted-text limit

Large workbooks (a 100k-row Jira export) produce very large `PlainText`. Rather
than a mandatory cap, extraction is bounded by a **configurable** limit:

- A `ParserOptions` record (`Ferret.Core.Documents`) with
  `long? MaxExtractedCharacters { get; init; }`, **default `null` = unlimited**.
- Bound from configuration (e.g. `Ferret:Parsers:MaxExtractedCharacters`) and
  injected into **all three** heavyweight parsers — **PDF, Word, and Excel treat
  the limit identically**. A million-character PDF or a 3,000-page DOCX is as
  real as a huge workbook, so behavior is uniform, not Excel-only.
- **One shared implementation, no duplication:** a single
  `ExtractionLimiter.ApplyCharacterLimit(text, options)` helper
  (`Ferret.Core.Documents`) returns `(string text, bool truncated)`. Every parser
  calls it — there is no per-parser truncation logic.
- When set and exceeded, the parser truncates `PlainText` to the limit and sets
  `DocumentMetadata.Truncated = "true"` (observable, never silent).
- **Default behavior is unchanged:** unlimited ⇒ documents index completely.
  Only an administrator opting in changes this.

The text parsers (plain/markdown/json) are unaffected (small files).

---

## PdfParser (`Ferret.Parsers.Pdf`)

- **Library:** UglyToad.PdfPig (pure managed, MIT).
- **Extraction:** open from the provided `Stream` (do **not** dispose), iterate
  pages in order, extract page text, join with newlines.
- **Output:** `DocumentKind.Prose`, `MediaType = "application/pdf"`. Metadata via
  `DocumentMetadata` constants (`Author`, `Subject`, `Keywords`, `PageCount`,
  `Created`, `Modified`).
- **Extraction limit:** takes `ParserOptions`; applies the shared
  `ExtractionLimiter` (a large PDF can hold millions of characters).
- **`CanParse`:** `application/pdf` only.
- **Error handling:** encrypted/corrupt PDFs throw → dispatcher returns `Failed`.
  Image-only/zero-text yields empty `PlainText` → dispatcher returns `Empty` (no
  garbage indexed). OCR is out of scope.

---

## WordParser (`Ferret.Parsers.Office`, DOCX)

- **Library:** DocumentFormat.OpenXml.
- **Open:** `WordprocessingDocument.Open(stream, isEditable: false)`.
- **Extraction:** body paragraphs, table cell text, headers, footers. Concatenate
  with structural newlines. Comments deferred.
- **Output:** `DocumentKind.Prose`, `MediaType =` the OpenXML wordprocessing type.
  Metadata via `DocumentMetadata` constants.
- **Extraction limit:** takes `ParserOptions`; applies the shared
  `ExtractionLimiter` (a 3,000-page DOCX is possible) — identical to PDF/Excel.
- **Error handling:** legacy `.doc` unsupported (stays `BinaryOpaque`);
  malformed/non-OOXML throws → `Failed`.

---

## ExcelParser (`Ferret.Parsers.Office`, XLSX)

The Excel parser extracts **searchable enterprise knowledge** from tabular data —
not a spreadsheet engine.

- **Library:** DocumentFormat.OpenXml.
- **Open:** `SpreadsheetDocument.Open(stream, isEditable: false)`.
- **Streaming (performance-critical):** worksheets are read with the **streaming
  `OpenXmlReader` (SAX)**, not the DOM. Enterprise exports can be 10k–100k+ rows;
  a DOM load would hold the entire sheet in memory. This is a deliberate
  per-format divergence from Word (which stays DOM — Word docs are small).
- **Shared strings:** resolve the `SharedStringTablePart` once into an array;
  cells with `DataType == SharedString` index into it. Other cells use
  `CellValue.InnerText`.
- **Cached values, never recompute:** for a formula cell, index its **cached
  value** (`CellValue`, the stored result). If the cache is absent, **skip** the
  cell. **Never emit the formula expression itself** — indexing `=SUM(A1:A50)`
  has no search value. The parser reads `CellValue` and never touches
  `CellFormula`. No calculation engine.
- **Flattening strategy (for keyword search + context):**
  - Iterate sheets in workbook order.
  - Emit each sheet name as a heading line for context.
  - For each non-empty row, join non-empty cell values with a tab, one row per
    line. Row 1 (typically headers, e.g. Jira/ADO columns) is emitted naturally
    as the first line so header tokens are searchable alongside data.
  - Skip empty cells and fully empty rows.
- **Output:** `DocumentKind.Data`, `MediaType =` the OpenXML spreadsheet type.
  Metadata via `DocumentMetadata` constants (`SheetCount`, `Author`, `Created`,
  `Modified`, `Category`).
- **`CanParse`:** the OpenXML spreadsheet media type only.
- **Error handling:** legacy `.xls` unsupported (stays `BinaryOpaque`);
  malformed/non-OOXML throws → `Failed`.
- **Known limitation:** dates/numbers are extracted as their **stored string**;
  Excel stores dates as serial numbers, so some dates may surface as serials
  (e.g. `45658`). Acceptable for v1 keyword search; a numFmt-aware enhancement is
  future work.
- **Extracted-text limit** applies via the shared `ExtractionLimiter` — the
  primary defense for very large workbooks (identical mechanism to PDF/Word).

---

## Synthetic Enterprise Corpus Generator

The first real implementation of the corpus generator the approved **Benchmark
Suite Spec** (`docs/superpowers/specs/2026-06-30-benchmark-suite-spec.md`) calls
for. It lives in `tests/Ferret.Benchmarks`, not a parallel `tools/` project, and
replaces the inline 10k-`.cs`-file generation in `IndexPipelineBenchmark.cs`. The
same deterministic generator is reusable beyond benchmarking (demo data, CI
fixtures); those consumers are out of scope here.

### Abstract corpus model + format renderers

Separate *what* a document is from *how* it is emitted. Excel forces the model to
carry **tabular** content, so `CorpusDocument` gains a `CorpusTable` primitive
alongside prose blocks:

```csharp
sealed record CorpusBlock(CorpusBlockKind Kind, string Text);        // prose blocks
sealed record CorpusTable(IReadOnlyList<string> Headers,
                          IReadOnlyList<IReadOnlyList<string>> Rows); // tabular
sealed record CorpusDocument(string Title,
                             IReadOnlyList<CorpusBlock> Blocks,
                             IReadOnlyList<CorpusTable> Tables);

interface IDocumentRenderer
{
    string Extension { get; }          // ".md", ".pdf", ".docx", ".xlsx", ".html", ...
    void Render(CorpusDocument doc, Stream output);
}
```

The **same logical document** now renders to Markdown (tables as pipe tables),
DOCX (real Word tables), HTML, and **XLSX (real sheets)** — which is exactly the
promise the abstraction makes, and the first time a format *requires* it. Text
renderers that don't do tables render them as delimited lines.

Renderers for this milestone: **Markdown, PDF, DOCX, XLSX, HTML**, plus code/JSON
emitters for the mixed-repo portion. PowerPoint/RTF add a renderer only.

**PDF generation is implementation-defined.** If PdfPig's writer is insufficient,
use a minimal lightweight PDF writer **solely inside the benchmark renderer** —
never affecting the production `PdfParser`.

### Excel-specific enterprise datasets

The generator emits realistic tabular archetypes (deterministic, seeded), so
benchmarks reflect the artifacts that motivated Excel support:

- **Requirement traceability matrix** — ID, Requirement, Priority, Status,
  Linked Test, Owner.
- **Bug report export** — Key, Summary, Severity, Status, Assignee, Created.
- **Sprint backlog** — Story, Points, Sprint, State, Epic.
- **Risk register** — Risk, Likelihood, Impact, Mitigation, Owner.
- **Test execution report** — Test, Result, Duration, Build, Tester.
- **Release checklist** — Item, Owner, Status, Due, Notes.
- **Deployment plan** — Step, Environment, Owner, Rollback, Status.
- **Production incident** — Incident, Severity, Detected, Resolved, Root Cause.
- **Security findings** — Finding, CVSS, Component, Status, Remediation.

Each becomes a `CorpusDocument` with a `CorpusTable`, renderable to `.xlsx` (and,
for cross-format tests, to `.md`/`.docx`).

### Enterprise-like document titles

Prose/document titles resemble real enterprise artifacts rather than
`Document 42 platform` — e.g. *Sprint 14 Planning*, *Architecture Decision*,
*Bug Investigation*, *Quarterly Review*, *Security Assessment*, *Incident
Report*, *Design Proposal*. Realistic titles materially improve search-quality
evaluation (titles are strong ranking signals).

### Determinism

- All randomness derives from a **fixed seed**; same seed + size ⇒ identical
  output. No unseeded `Random`, no wall-clock timestamps, no `Guid.NewGuid()` in
  content. DOCX/XLSX/PDF metadata timestamps are pinned to a fixed value; if
  byte-identical OOXML proves impractical, the determinism test compares
  extracted text instead of bytes for those formats.

### Sizes (aligned to the Benchmark Suite Spec)

Tiers **Small / Medium / Enterprise**:

| Corpus     | Files  | Approx LOC |
| ---------- | ------ | ---------- |
| Small      | 200    | 20K        |
| Medium     | 2,000  | 250K       |
| Enterprise | 15,000 | 2M         |

Each tier emits a realistic mix: C# source, Markdown, JSON, **PDF**, **DOCX**,
**XLSX** (including the enterprise tabular archetypes), and a "Mixed" repo tree.
Output goes to a temp/`.gitignore`d directory and is **never committed**.

---

## Testing & Deliverables

### Unit tests (per parser)

- `PdfParser`: happy path; empty; multi-page ordering; encrypted/corrupt →
  `Failed`; image-only → empty text.
- `WordParser`: paragraphs + tables + headers + footers; empty; malformed →
  `Failed`; `.doc` unsupported.
- `ExcelParser`: shared-string resolution; multi-sheet (sheet names emitted);
  numeric + cached-formula cells; formula-without-cache skipped and the formula
  expression never emitted; empty cells/rows skipped; header row + data rows
  present in text; malformed → `Failed`; `.xls` unsupported; extracted-text limit
  truncates + sets `Truncated` when configured.
- **Dispatch-level test:** assert `IParserDispatcher.DispatchAsync` (the public
  API) routes a PDF/DOCX/XLSX stream to the correct parser and returns `Success`.
  The registry lookup is an implementation detail; the dispatcher is what
  production uses.

### End-to-end integration test

Generate a Small corpus → `ferret index` → assert:
- PDF, DOCX, and **XLSX** content is searchable;
- a value in a **Jira-export-style `.xlsx` cell** is retrievable by searching
  that value (Top-5) — the stated product-value assertion;
- `.cs` / `.md` / `.json` still index correctly;
- opaque binaries (`.so`, `.class`, `.nupkg`, …) are **not** indexed.

### Acceptance criteria

1. A `.pdf`, `.docx`, and `.xlsx` produce non-empty indexed content and are
   searchable.
2. Searching a cell value from a generated requirement-traceability / bug-report
   `.xlsx` returns that file in the top results.
3. Opaque binaries never enter the index.
4. `MediaCategory` classifies `.pdf`/`.docx`/`.xlsx` as `BinaryParseable`; `.xlsx`
   documents are `DocumentKind.Data`.
5. With the default (unlimited) extraction limit, a large generated workbook
   indexes completely; with a configured limit, `PlainText` is truncated and
   `Truncated=true` is set.
6. `ferret doctor` lists the 6 installed parsers and the supported-extension
   count.
7. All existing parser/index/search tests still pass; the parser registry is
   unchanged.

### Performance report

Extend the benchmark harness to record, per corpus tier: documents/sec, MB/sec,
time-by-document-type (**including a large multi-thousand-row XLSX case**),
parser time vs index time, and search latency. Output to a versioned report under
`docs/benchmarks/<release>/` per the Benchmark Suite Spec format.

**Memory, per parser.** Enterprise users care more about "500 MB vs 5 GB" than
milliseconds. Record `[MemoryDiagnoser]`'s **Allocated** (managed bytes) per
parser, and — because that column is *allocations*, not resident set — also
capture **peak working set** (`Process.PeakWorkingSet64`) for the large-workbook
XLSX case, which is where the streaming reader's memory profile matters most.

### Search quality (not just performance)

Quality metrics are already defined in the Benchmark Suite Spec **Category 3**
(Precision@k, Recall@k, MRR, nDCG@10, Success@1/@5/@10) — this milestone does not
redefine them. Its contribution: once PDF/DOCX/XLSX are indexed, the eval dataset
gains document-type Q&A pairs (find a fact that lives only in a PDF / DOCX /
spreadsheet cell), so retrieval quality is measured across code *and* documents.

### Parser introspection (`ferret doctor`)

Surface installed parsers and supported extensions at runtime:

```
Installed Parsers
  ✓ Plain Text   ✓ Markdown   ✓ JSON   ✓ PDF   ✓ DOCX   ✓ XLSX
Supported Extensions: 88
```

Implemented as a new `IDiagnosticCheck` (`ferret doctor` already has the check
framework) — enumerate `GetServices<IContentParser>()` and count
`MimeTypeResolver`'s non-opaque extensions. No new command, no new framework.

### Documentation

Update README / docs to list supported file types (PDF, Word, Excel) and the new
parser packages (`Ferret.Parsers.Pdf`, `Ferret.Parsers.Office`, composed via
`Ferret.Parsers`), plus the configurable extraction-limit setting.

---

## Milestone: Enterprise Content Pack 1 — Deliverables Summary

- `Ferret.Parsers.Pdf` (PdfParser, PdfPig).
- `Ferret.Parsers.Office` (**WordParser + ExcelParser**, OpenXml).
- `Ferret.Parsers` composition project (`ParserPackModule`) + host callsite
  migration.
- `MimeTypeResolver` + `MediaTypeInfo` changes: `MediaCategory` enum;
  PDF/DOCX/XLSX parseable-binary mappings (XLSX → `Data`); expanded text/config
  mappings; expanded binary denylist.
- Core additions (`Ferret.Core.Documents`): `ParserOptions` (configurable
  extracted-text limit, default unlimited); shared `ExtractionLimiter`
  (single truncation implementation, no per-parser duplication); `DocumentMetadata`
  key constants (no string drift); reserved `ParserCapabilities.StructuredExtraction`
  (unused, future-proof). PDF/Word/Excel all take `ParserOptions` and apply the
  limit uniformly.
- Deterministic Synthetic Enterprise Corpus Generator (abstract model +
  renderers, **`CorpusTable`** + **XLSX renderer** + enterprise tabular
  archetypes + enterprise-like titles) in `tests/Ferret.Benchmarks`.
- Parser introspection diagnostic check for `ferret doctor` (6 parsers).
- Unit tests, end-to-end integration test (incl. XLSX cell-search), performance
  report (incl. large-workbook case), documentation updates.

---

## Reserved: Enterprise Content Pack 2 (future, not this milestone)

Once the `GetServices<IContentParser>()` extension pattern is proven, the natural
follow-up is high-coverage formats plus the deferred **PowerPoint** fast-follow:

`PPTX, HTML, XML, RTF, CSV, YAML, TOML, INI`

**Honest scoping note:** CSV/YAML/TOML/INI/XML/HTML *already index today* as plain
text via the fallback. Pack 2's value for those is **structure-aware** extraction
(CSV → rows/columns, HTML → text without markup). PPTX and RTF are genuinely
unindexed today. This reservation sets direction; scope is decided when Pack 2 is
specced.

---

## Risks & Open Items

- **Excel date/number formatting:** dates stored as serial numbers may surface as
  serials in extracted text. Mitigation: extract stored string for v1; numFmt-
  aware formatting is future work. Documented limitation, not a blocker.
- **Large-workbook memory:** mitigated by the streaming `OpenXmlReader` for Excel
  and the configurable extracted-text limit.
- **Shared-string edge cases** (rich-text runs, inline vs shared strings): covered
  by unit tests.
- **OOXML determinism** (DOCX/XLSX embed timestamps): pin to a fixed value, or
  compare extracted text instead of bytes in the determinism test.
- **PdfPig write capability:** if insufficient for benchmark PDFs, use a minimal
  hand-rolled emitter in the renderer only. Reading (product path) unaffected.
- **`MediaTypeInfo` consumers:** confirm no external reader depends on
  `IsText`/`IsBinary` being settable; they become computed.
- **Callsite coverage:** `ParserPlatformModule → ParserPackModule` migration must
  cover every host; verified by grep during implementation.
