# Parser Pack 1 — Design

**Date:** 2026-07-01
**Status:** Approved for implementation
**Author:** Captured from product session

---

## Problem Statement

Ferret today indexes text-based files only. Three content parsers exist
(`PlainTextParser`, `MarkdownParser`, `JsonParser`), all inside
`Ferret.ParserPlatform`. Binary formats — PDF and Microsoft Office documents —
are not extracted: `MimeTypeResolver` maps `.pdf`/`.docx`/`.xlsx`/`.pptx` to
`application/octet-stream`, no parser claims that media type, and the pipeline
emits a `DocumentSkippedEvent`. As a result, enterprise documentation never
enters the index.

Parser Pack 1 makes Ferret able to index both **software repositories** and
**enterprise documentation**, so that benchmarking (the next roadmap phase)
measures Ferret against the document types real users work with — not only
source code and plain text.

**Roadmap placement:** `v0.15.x → Parser Pack 1 (this) → Benchmarking →
Dogfooding (DOGFOOD-001) → GA`. Pack 1 lands before dogfooding so evaluation
reflects realistic enterprise usage.

---

## Goals

Parser Pack 1 ships:

1. **Expanded text/code/config MIME mappings** with improved `DocumentKind`
   classification.
2. **An expanded binary denylist** preventing opaque binary artifacts from
   entering the text index.
3. **`Ferret.Parsers.Pdf`** — `PdfParser` using UglyToad.PdfPig.
4. **`Ferret.Parsers.Office`** — `WordParser` with **DOCX support only** this
   milestone.
5. **Additive `MimeTypeResolver` changes** so PDF and DOCX resolve to dedicated
   media types that dispatch to their parsers, while preserving the existing
   parser contracts.
6. **A distinct content model for parseable binaries** (PDF/DOCX) vs opaque
   binaries, so future binary-skip logic does not exclude parseable formats.
7. **A deterministic, multi-format benchmark corpus generator** (abstract corpus
   model + format-specific renderers) producing realistic enterprise documents
   and mixed code repositories at configurable sizes. Assets are **not**
   committed.

### Non-goals (deferred, YAGNI)

- `.xlsx` / `.pptx` parsing (Office package ships DOCX only).
- DOCX comments extraction.
- OCR for scanned / image-only PDFs.
- Legacy `.doc` (pre-2007 binary Word).
- Extraction of the built-in text parsers into a separate `Ferret.Parsers.Text`
  package — deferred until post-GA, when there is a clear need for parser
  modularization or a plugin ecosystem.

---

## Parser Design Principle

> **Parsers are responsible only for extracting text and lightweight metadata
> from a stream.** They MUST NOT perform chunking, tokenization, embedding,
> summarization, or any AI processing. Those concerns belong to the indexing,
> context-assembly, and AI layers downstream. A parser's single job is:
> `Stream → Document { text, DocumentKind, lightweight metadata }`.

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

| Key          | Source              | Notes                          |
| ------------ | ------------------- | ------------------------------ |
| `Author`     | PDF / DOCX core     |                                |
| `Subject`    | PDF / DOCX core     |                                |
| `Keywords`   | PDF / DOCX core     |                                |
| `PageCount`  | PDF                 | page count                     |
| `Created`    | PDF / DOCX core     | ISO-8601                       |
| `Modified`   | PDF / DOCX core     | ISO-8601                       |
| `Company`    | DOCX extended props | Word-specific                  |
| `Category`   | DOCX extended props | Word-specific                  |

`Title` maps to `Document.Title`. Missing values are simply omitted (no empty
keys). Ferret need not consume every key today — capturing it now is free and
lets downstream Ai/ranking use it later without re-parsing.

### DocumentKind evolution

PDF and DOCX are classified `Prose` this milestone. Future content types
(API reference, requirements, meeting notes, policies, architecture) are also
broadly "prose"; **future releases may refine `DocumentKind` without changing
parser contracts** — `DocumentKind` is an output value, not part of the
`IContentParser` signature.

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
src/Ferret.Parsers.Pdf/         PdfParser            dep: UglyToad.PdfPig
src/Ferret.Parsers.Office/      WordParser           dep: DocumentFormat.OpenXml
                                 (future: ExcelParser, PowerPointParser)
src/Ferret.Parsers/             ParserPackModule (composition only)
                                 refs: ParserPlatform + Pdf + Office
```

### Composition: `ParserPackModule`

A single composition project, **`Ferret.Parsers`**, references the platform and
both parser packages and exposes one entry point:

```csharp
public static class ParserPackModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        ParserPlatformModule.ConfigureServices(services); // registry, dispatcher, MimeTypeResolver, built-ins
        PdfParserModule.ConfigureServices(services);      // AddSingleton<IContentParser, PdfParser>
        OfficeParserModule.ConfigureServices(services);   // AddSingleton<IContentParser, WordParser>
    }
}
```

`ParserPackModule` exists so hosts wire parsers **once**, rather than calling
multiple modules scattered through the application. Hosts that currently call
`ParserPlatformModule.ConfigureServices` switch to `ParserPackModule`. Because
`ParserPlatform` must not depend on the heavyweight packages, `ParserPackModule`
cannot live in `ParserPlatform` — it lives in the dedicated `Ferret.Parsers`
composition project, which also future-proofs adding HTML/XML/OCR packs.

**Callsite migration:** every site that currently calls
`ParserPlatformModule.ConfigureServices` must switch to
`ParserPackModule.ConfigureServices`. Known callsite: `IndexCliModule.cs:68`.
Implementation will grep for all callsites (index, serve/MCP, any test host) and
migrate each.

### Why the registry needs no changes

`IParserRegistry` is built from `sp.GetServices<IContentParser>()`
(`ParserPlatformModule.cs:23-24`) — it aggregates **every** registered
`IContentParser` in the container. New parsers are picked up automatically once
registered; the registry, dispatcher, and `IContentParser` contract are
untouched. This matches the established `McpModule` / `AiModule` /
`ModelPlatformModule` composition pattern.

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
    BinaryParseable, // binary, but a parser can extract text (PDF, DOCX)
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

**Future binary-skip logic** keys off `Category == BinaryOpaque`, so PDF/DOCX
(`BinaryParseable`) are never dropped. This is preferred over gating skip on
"does the registry have a parser," which would couple the resolver's static
intent to runtime DI registration.

### New parseable-binary mappings

| Extension | Media type                                                              | Category          | DocumentKind |
| --------- | ----------------------------------------------------------------------- | ----------------- | ------------ |
| `.pdf`    | `application/pdf`                                                        | `BinaryParseable` | `Prose`      |
| `.docx`   | `application/vnd.openxmlformats-officedocument.wordprocessingml.document`| `BinaryParseable` | `Prose`      |

`.xlsx` / `.pptx` remain `BinaryOpaque` this milestone (no parser yet).

### Expanded text/code/config mappings

The resolver already maps ~40 extensions. **Note:** unmapped extensions already
fall through to a `text/plain` default (`MimeTypeResolver.cs:86-92,110`), so
unmapped source files are *already indexed* — explicit mappings improve
`DocumentKind` accuracy (Code/Config/Prose/Data vs `Unknown`) and confidence
(1.0 vs 0.5), which feed ranking and context assembly. Gaps to fill (illustrative,
finalized during implementation):

`.scss .less .php .scala .clj .cljs .edn .ex .exs .erl .dart .lua .r .pl
.groovy .gradle .bat .cmd .psm1 .psd1 .ini .cfg .conf .env .properties
.dockerfile .makefile .cmake .rst .adoc .tex .vb .fs .fsx .csproj .vbproj
.fsproj .props .targets .resx .xaml .gitignore .editorconfig`

### Expanded binary denylist

Map opaque artifacts to `BinaryOpaque` so they stop leaking into the text index
via the `text/plain` fallback. Several are **already** mapped
(`MimeTypeResolver.cs:55-83`: `.dll .exe .pdb .obj .zip .gz .tar .7z .rar .png
.jpg .jpeg .gif .bmp .ico .svg .mp3 .mp4 .avi .mov .ttf .woff .woff2 .eot`); the
work adds the gaps:

`.so .dylib .a .o .lib .class .pyc .pyo .wasm .node .nupkg .snk .pfx .jar .war
.ear .db .sqlite .parquet .dat .keystore .psd .ai .otf`

(Directory-level skipping of `bin`/`obj`/`node_modules` already exists in the
filesystem connector, so this targets loose binaries sitting inside source
trees.)

---

## PdfParser (`Ferret.Parsers.Pdf`)

- **Library:** UglyToad.PdfPig (pure managed, no native binaries, MIT).
- **Extraction:** open from the provided `Stream` (do **not** dispose — contract),
  iterate pages in order, extract page text, join with newlines.
- **Output:** `DocumentKind.Prose`, `MediaType = "application/pdf"`.
- **`CanParse`:** matches `application/pdf` only.
- **Error handling (never throw past the boundary):**
  - Encrypted / password-protected / corrupt PDFs → `Failed` `ParseResult` with a
    diagnostic message; the pipeline emits `DocumentParsingFailedEvent`.
  - Image-only / zero extractable text → `Failed` with a clear message
    (e.g. "no extractable text (likely scanned); OCR not supported"). OCR is out
    of scope.

---

## WordParser (`Ferret.Parsers.Office`, DOCX only)

- **Library:** DocumentFormat.OpenXml (Microsoft-supported, pure managed, no
  Office install required).
- **Open:** `WordprocessingDocument.Open(stream, isEditable: false)`.
- **Extraction (document order):** body paragraphs, table cell text, headers,
  footers. Concatenate to plain text with structural newlines. **Comments
  deferred.**
- **Output:** `DocumentKind.Prose`, `MediaType =` the OpenXML wordprocessing media
  type.
- **`CanParse`:** matches the OpenXML wordprocessing media type only.
- **Error handling:** legacy `.doc` (binary) is not supported and stays
  `BinaryOpaque`; malformed / non-OOXML input → `Failed` `ParseResult`.

`Ferret.Parsers.Office` is **not** a "Word-only" package — `ExcelParser` and
`PowerPointParser` are planned for a later milestone and will register the same
way (additional `IContentParser` registrations in `OfficeParserModule`).

---

## Synthetic Enterprise Corpus Generator

Named for its broader reach: although it is the first real implementation of the
corpus generator the approved **Benchmark Suite Spec**
(`docs/superpowers/specs/2026-06-30-benchmark-suite-spec.md`) calls for, the same
deterministic generator is reusable beyond benchmarking — `ferret demo`,
CI/regression fixtures, and training/sample data can all consume it. (Those
consumers are out of scope for Parser Pack 1; the rename and clean abstraction
just avoid foreclosing them.)

It lives in `tests/Ferret.Benchmarks` (alongside the existing BenchmarkDotNet
harness), **not** a parallel `tools/` project, and replaces the current inline
10k-`.cs`-file generation in `IndexPipelineBenchmark.cs`.

### Abstract corpus model + format renderers

Separate *what* a document is from *how* it is emitted, so the same logical
document can be rendered to multiple formats and new formats added without
touching corpus logic:

```csharp
// Logical, format-agnostic document.
sealed record CorpusDocument(string Title, IReadOnlyList<CorpusBlock> Blocks, ...);

// One renderer per output format; same CorpusDocument → different bytes.
interface IDocumentRenderer
{
    string Extension { get; }          // ".md", ".pdf", ".docx", ".html", ...
    void Render(CorpusDocument doc, Stream output);
}
```

Renderers for Parser Pack 1: **Markdown, PDF, DOCX (OpenXml), HTML**, plus
code/JSON file emitters for the mixed-repo portion. Future formats
(e.g. `.pptx`, `.xlsx`, RTF) add a renderer only.

**PDF generation is implementation-defined.** PdfPig is excellent at *reading*
but is not intended as a PDF *writer*. If it cannot generate sufficiently
realistic PDFs, the PDF renderer uses a dedicated lightweight PDF writer **solely
inside the benchmark renderer**. This never affects the production `PdfParser`,
which only reads.

### Determinism

- All randomness derives from a **fixed seed** (CLI/parameter input); same seed +
  size ⇒ byte-identical corpus.
- No unseeded `Random`, no wall-clock timestamps, no `Guid.NewGuid()` in content.

### Sizes (aligned to the Benchmark Suite Spec)

Reuse the approved tiers **Small / Medium / Enterprise** (the product session's
"Large" maps to "Enterprise"):

| Corpus     | Files  | Approx LOC |
| ---------- | ------ | ---------- |
| Small      | 200    | 20K        |
| Medium     | 2,000  | 250K       |
| Enterprise | 15,000 | 2M         |

Each tier emits a realistic mix: C# source, Markdown, JSON, **PDF**, **DOCX**,
and a "Mixed" repo tree. Output is generated to a temp/`.gitignore`d directory
and **never committed**.

---

## Testing & Deliverables

### Unit tests (per parser)

- `PdfParser`: happy path; empty PDF; multi-page ordering; encrypted/corrupt →
  `Failed`; image-only → `Failed`.
- `WordParser`: happy path; paragraphs + tables + headers + footers extracted;
  empty doc; malformed/non-OOXML → `Failed`; `.doc` unsupported.
- Fixtures generated via the corpus renderers (no committed binaries) or tiny
  inline byte arrays.

### End-to-end integration test

Generate a Small corpus → `ferret index` → assert:
- PDF and DOCX content is searchable;
- `.cs` / `.md` / `.json` still index correctly;
- opaque binaries (`.so`, `.class`, `.nupkg`, …) are **not** indexed.

### Performance report

Extend the benchmark harness to record, per corpus tier: documents/sec, MB/sec,
time-by-document-type, **parser time vs index time**, and search latency. Output
to a versioned report under `docs/benchmarks/<release>/` and the historical trend
table, per the Benchmark Suite Spec report format.

### Search quality (not just performance)

Performance alone does not show whether search got *better*. Quality metrics are
already defined in the Benchmark Suite Spec **Category 3** (Precision@k,
Recall@k, MRR, nDCG@10, Success@1/@5/@10) — Parser Pack 1 does **not** redefine
them. Its contribution is to make those metrics meaningful for documents: once
PDF/DOCX are indexed, the eval dataset gains document-type Q&A pairs (find a fact
that lives only in a PDF / DOCX), so retrieval quality is measured across code
*and* documents. Concretely, the integration test asserts a baseline
`Top-1 / Top-5 correct?` for at least one PDF-only and one DOCX-only fact.

### Parser introspection (`ferret doctor`)

Surface installed parsers and supported extensions at runtime — useful for
support and as a tangible signal of the pack's value:

```
Installed Parsers
  ✓ Plain Text   ✓ Markdown   ✓ JSON   ✓ PDF   ✓ DOCX
Supported Extensions: 87
```

Implementation is small: enumerate `GetServices<IContentParser>()` for the parser
list and count `MimeTypeResolver`'s non-opaque extensions. Surfaced via a
`ferret doctor` command (or by extending the existing `ferret status`,
`CoreCliModule.cs:37` — implementation picks one). Scoped minimal: a listing, no
health-check framework.

### Documentation

Update README / docs to list supported file types and the new parser packages
(`Ferret.Parsers.Pdf`, `Ferret.Parsers.Office`, composed via `Ferret.Parsers`).

---

## Milestone: Content Parser Pack 1 — Deliverables Summary

- `Ferret.Parsers.Pdf` (PdfParser, PdfPig).
- `Ferret.Parsers.Office` (WordParser, OpenXml, DOCX only).
- `Ferret.Parsers` composition project (`ParserPackModule`) + host callsite
  migration.
- `MimeTypeResolver` + `MediaTypeInfo` changes: `MediaCategory` enum, PDF/DOCX
  parseable-binary mappings, expanded text/config mappings, expanded binary
  denylist.
- Deterministic Synthetic Enterprise Corpus Generator (abstract model +
  renderers) in `tests/Ferret.Benchmarks`.
- Parser introspection (`ferret doctor` / `status`) listing installed parsers and
  supported-extension count.
- Unit tests, end-to-end integration test, performance report, documentation
  updates.

---

## Reserved: Parser Pack 2 (future, not this milestone)

Once the `GetServices<IContentParser>()` extension pattern is proven by Pack 1,
the natural follow-up is high-coverage, low-effort formats:

`HTML, XML, RTF, CSV, YAML, TOML, INI`

**Honest scoping note:** CSV/YAML/TOML/INI/XML/HTML *already index today* as
plain text via the `text/plain` fallback. Parser Pack 2's value for those is
**structure-aware** extraction (e.g. CSV → rows/columns, HTML → text without
markup, XML → element text), not merely "making them searchable." RTF is the one
that is genuinely unindexed today. This reservation sets direction; scope and
priority are decided when Pack 2 is specced.

---

## Risks & Open Items

- **PdfPig text-write capability:** if PdfPig's writer is insufficient for
  realistic benchmark PDFs, fall back to a minimal hand-rolled PDF emitter in the
  renderer. Reading (the product path) is unaffected — PdfPig reads robustly.
- **`MediaTypeInfo` consumers:** confirm during implementation that no external
  reader depends on `IsText`/`IsBinary` being settable; they become computed.
- **Callsite coverage:** the `ParserPlatformModule → ParserPackModule` migration
  must cover every host (index, serve/MCP, test hosts); verified by grep before
  freezing implementation.
