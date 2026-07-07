# Enterprise Content Pack 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Naming:** milestone = "Enterprise Content Pack 1"; technical asset names (`Ferret.Parsers.*`, `ParserPackModule`) are unchanged.

**Goal:** Add PDF, DOCX, and XLSX content parsers (plus expanded MIME mappings and a deterministic multi-format corpus generator) so Ferret indexes enterprise documents — including the Excel exports (Jira/ADO, RTMs, bug reports, risk registers) enterprises run on — not just code/text.

**Architecture:** `Ferret.ParserPlatform` stays intact (registry, dispatcher, `MimeTypeResolver`, 3 built-in parsers). Two new sibling packages hold heavyweight-format parsers — `Ferret.Parsers.Pdf` (PdfPig) and `Ferret.Parsers.Office` (OpenXml; `WordParser` + `ExcelParser`). A thin `Ferret.Parsers` project composes all three via `ParserPackModule`. The registry auto-aggregates every `IContentParser` via `GetServices<IContentParser>()`, so the dispatcher/registry/contract are untouched. Only `MimeTypeResolver` + `MediaTypeInfo` change (additively).

**Tech Stack:** .NET 9, C#, xUnit, Microsoft.Extensions.DependencyInjection, UglyToad.PdfPig, DocumentFormat.OpenXml, BenchmarkDotNet.

**Spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md`

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props` — do NOT set `<TargetFramework>` in new csproj files.
- **Central Package Management:** every NuGet version lives in `Directory.Packages.props`; `<PackageReference>` in csproj carries **no** `Version` attribute (STD-005 §11.2).
- **Parsers MUST be `sealed`.** `CanParse` is pure: no I/O, never throws, deterministic for a given input.
- **Parser responsibility (hard rule):** extract text + lightweight metadata from the stream only. NO chunking, tokenization, embedding, summarization, AI processing, **spreadsheet calculation, or formula evaluation**. The Excel parser extracts cached cell values — it never recomputes.
- **Excel reads streaming:** `ExcelParser` uses the OpenXml **`OpenXmlReader` (SAX)** for worksheets, not the DOM — enterprise exports can be 100k+ rows. Word stays DOM.
- **Extracted-text limit (uniform across PDF/Word/Excel):** all three heavyweight parsers take `ParserOptions` and apply the **shared** `ExtractionLimiter.ApplyCharacterLimit` (default `null` = unlimited). When exceeded, the parser truncates `PlainText` and sets `Metadata[DocumentMetadata.Truncated]="true"` (observable, never silent). No per-parser truncation logic; no logging dependency.
- **Metadata keys are `DocumentMetadata.*` constants**, never raw strings, to prevent key drift across parsers.
- **Stream ownership:** parsers MUST NOT dispose or close the content stream (use `leaveOpen: true` on any reader).
- **Failure signaling:** a parser signals failure by **throwing** with a clear message — `ParserDispatcher` catches all non-cancellation exceptions and converts to `ParseResult<Document>.Failed(ex.Message)`. Empty/whitespace `PlainText` becomes `Empty`. `OperationCanceledException` must propagate.
- **Parser package isolation:** `Ferret.Parsers.Pdf` and `Ferret.Parsers.Office` must NOT reference each other, and `Ferret.ParserPlatform` must NOT reference either (no heavyweight deps in the platform).
- **StyleCop:** analyzers apply to all projects; public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.
- **New projects** must be added to `src/Ferret.sln` via `dotnet sln src/Ferret.sln add <path>`.

---

## Sprint Map & Parallel Execution

The nine tasks group into a **foundation → fan-out → integration → benchmark →
RC** shape. The foundation is a hard barrier; once it lands, the heavy parser
work and the corpus/docs work are largely independent and run **concurrently as
subagent workstreams**; then everything re-converges at integration.

### Dependency graph

```text
            Phase 1: Foundation (SEQUENTIAL — blocks everything)
              Task 1 Core primitives  +  Task 2 MimeTypeResolver
                                   │
        ┌──────────────┬───────────┼───────────┬──────────────┐
        ▼              ▼           ▼           ▼              ▼
   Phase 2: PARALLEL workstreams (independent; run as separate subagents)
   [A] PDF        [B] Word     [C] Excel   [D] CSV        [E] Corpus     [F] Docs
   Task 3         Task 4a      Task 4b     Task 2b        Task 7         (README/
   Pdf pkg        WordParser   ExcelParser CsvParser      generator       manual)
        └──────────────┴───────────┼───────────┴──────────────┘
                                   ▼
        Phase 3: Integration (SEQUENTIAL — single owner, merge point)
          Task 5 ParserPackModule + CLI wiring · Task 6 doctor · Task 8 integration tests
                                   │
                                   ▼
        Phase 4: Benchmarks (SEQUENTIAL — needs everything)
          Task 9 throughput · large workbook · memory · report
                                   │
                                   ▼
        Phase 5: RC (SEQUENTIAL) — package · validate · publish
```

### Phase → task → deliverable

| Phase | Parallel? | Tasks | Deliverable |
| ----- | --------- | ----- | ----------- |
| **1 Foundation** | ❌ sequential | Task 1, Task 2 | Parser platform primitives + resolver ready; no behavior change |
| **2 Fan-out** | ✅ parallel | Task 3 (PDF), Task 4 (Word+Excel), Task 2b (CSV), Task 7 (corpus), docs | Each parser unit+dispatch tested in isolation; corpus + docs progress |
| **3 Integration** | ❌ sequential | Task 5, Task 6, Task 8 | `ParserPackModule` wired once; 7 parsers live via `ferret index`; e2e green |
| **4 Benchmark** | ❌ sequential | Task 9 | Throughput / memory / search-latency baseline |
| **5 RC** | ❌ sequential | (release runbook) | Packaged, validated release candidate |

### Recommended subagent workstreams (Phase 2)

Run in isolated git worktrees so parallel file writes don't collide:

| Subagent | Scope | Touches |
| -------- | ----- | ------- |
| **PDF** | Task 3 | `src/Ferret.Parsers.Pdf/**`, its test project |
| **Office** | Task 4 (Word + Excel) | `src/Ferret.Parsers.Office/**`, its test project |
| **CSV** | Task 2b | `src/Ferret.ParserPlatform/Parsers/Csv*`, `ParserPlatformModule.cs` |
| **Corpus** | Task 7 | `tests/Ferret.Benchmarks/Corpus/**` |
| **Docs** | README/manual | docs only |

Office **may** be split into two subagents (Word, Excel) for six streams if
time/tokens allow — but they share `OfficeParserModule.cs` and the `.csproj`, so
give those two files a **single owner** to merge, or let one agent scaffold the
package/module first and the other add only its parser file.

**Claude's role is integration architect:** spawn the Phase-2 subagents, review
each branch, then personally own Phase 3 (composition + wiring + integration
tests) where the streams merge.

### Wiring is a single integration step (not per-parser)

`ParserPackModule` (Task 5) references the Pdf and Office packages, so it only
compiles once both exist — and more importantly, **CLI wiring must not be edited
by the parallel parser streams** (three subagents editing `IndexCliModule.cs`
would collide). Therefore:

- Phase-2 parsers are verified **only** by their own unit + dispatch tests, not
  by `ferret index`.
- `IndexCliModule` is touched **once**, in Task 5, swapping
  `ParserPlatformModule.ConfigureServices` for `ParserPackModule.ConfigureServices`.
- CSV is the exception that needs no wiring at all — it registers through the
  already-wired `ParserPlatformModule`, so `.csv`/`.tsv` are searchable as soon
  as Task 2b merges.

### Do NOT parallelize

Foundation (Task 1/2), composition/wiring (Task 5), integration tests (Task 8),
benchmarks (Task 9), and RC packaging are **merge points** — single-owner,
sequential. Parallelism buys nothing there and invites conflicts.

---

### Task 1: Content model + shared parser primitives (Ferret.Core)

**Files:**
- Create: `src/Ferret.Core/Documents/MediaCategory.cs`
- Modify: `src/Ferret.Core/Documents/MediaTypeInfo.cs`
- Create: `src/Ferret.Core/Documents/DocumentMetadata.cs` (metadata key constants)
- Create: `src/Ferret.Core/Documents/ParserOptions.cs` (configurable extraction limit)
- Create: `src/Ferret.Core/Documents/ExtractionLimiter.cs` (shared truncation helper)
- Modify: `src/Ferret.Core/Documents/ParserCapabilities.cs` (reserve `StructuredExtraction`)
- Test: `tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs`
- Test: `tests/Ferret.Core.Tests/Documents/ExtractionLimiterTests.cs`

**Interfaces:**
- Produces: `enum MediaCategory { Text, BinaryParseable, BinaryOpaque }`; `MediaTypeInfo.Category` (required init); computed `IsText`/`IsBinary`.
- Produces: `static class DocumentMetadata { const string Author, Subject, Keywords, PageCount, SheetCount, Created, Modified, Category, Truncated; }`.
- Produces: `sealed record ParserOptions { long? MaxExtractedCharacters { get; init; } }`.
- Produces: `static class ExtractionLimiter { (string Text, bool Truncated) ApplyCharacterLimit(string text, ParserOptions options); }` — the single truncation implementation shared by PDF/Word/Excel.
- Produces: `ParserCapabilities.StructuredExtraction` (reserved, unused this milestone).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs
using Ferret.Core.Documents;

namespace Ferret.Core.Tests.Documents;

public sealed class MediaTypeInfoTests
{
    [Fact]
    public void Text_Category_IsText_True_IsBinary_False()
    {
        var info = new MediaTypeInfo { MediaType = "text/plain", Category = MediaCategory.Text };
        Assert.True(info.IsText);
        Assert.False(info.IsBinary);
    }

    [Fact]
    public void BinaryParseable_IsText_False_IsBinary_True()
    {
        var info = new MediaTypeInfo { MediaType = "application/pdf", Category = MediaCategory.BinaryParseable };
        Assert.False(info.IsText);
        Assert.True(info.IsBinary);
    }

    [Fact]
    public void BinaryOpaque_IsText_False_IsBinary_True()
    {
        var info = new MediaTypeInfo { MediaType = "application/octet-stream", Category = MediaCategory.BinaryOpaque };
        Assert.False(info.IsText);
        Assert.True(info.IsBinary);
    }

    [Fact]
    public void Unknown_Is_BinaryOpaque()
    {
        Assert.Equal(MediaCategory.BinaryOpaque, MediaTypeInfo.Unknown.Category);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Ferret.Core.Tests --filter MediaTypeInfoTests`
Expected: FAIL — `MediaCategory` does not exist / `Category` not a member.

- [ ] **Step 3: Create the enum**

```csharp
// src/Ferret.Core/Documents/MediaCategory.cs
namespace Ferret.Core.Documents;

/// <summary>Classifies how a media type's content can be consumed by the parser platform.</summary>
public enum MediaCategory
{
    /// <summary>Human-readable text, consumable directly by a text/* parser.</summary>
    Text = 0,

    /// <summary>Binary, but a registered parser can extract text from it (e.g. PDF, DOCX).</summary>
    BinaryParseable = 1,

    /// <summary>Binary with no extractable text (images, executables, fonts, archives).</summary>
    BinaryOpaque = 2,
}
```

- [ ] **Step 4: Refactor MediaTypeInfo to derive the booleans from Category**

```csharp
// src/Ferret.Core/Documents/MediaTypeInfo.cs
namespace Ferret.Core.Documents;

/// <summary>
/// Richer MIME type resolution result. Returned by IMimeTypeResolver in place of a raw string
/// so callers have enough context to make decisions (binary skip, kind suggestion, confidence)
/// without re-examining the file name. Immutable.
/// </summary>
public sealed record MediaTypeInfo
{
    /// <summary>Gets the resolved MIME type string (e.g. "text/markdown").</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the content category for this media type.</summary>
    public required MediaCategory Category { get; init; }

    /// <summary>Gets a value indicating whether the content is human-readable text. Derived from <see cref="Category"/>.</summary>
    public bool IsText => Category == MediaCategory.Text;

    /// <summary>Gets a value indicating whether the content is binary. Derived from <see cref="Category"/>.</summary>
    public bool IsBinary => Category != MediaCategory.Text;

    /// <summary>Gets an optional suggested DocumentKind hint for the parser.</summary>
    public DocumentKind? SuggestedKind { get; init; }

    /// <summary>Gets the resolver's confidence in this classification (0.0–1.0).</summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>Gets a <see cref="MediaTypeInfo"/> representing an unrecognized binary file.</summary>
    public static MediaTypeInfo Unknown => new()
    {
        MediaType = "application/octet-stream",
        Category = MediaCategory.BinaryOpaque,
        Confidence = 0.5,
    };
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Ferret.Core.Tests --filter MediaTypeInfoTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Build the solution to surface any consumers of removed `required` setters**

Run: `dotnet build src/Ferret.sln`
Expected: build succeeds. If any code set `IsText`/`IsBinary` directly, it will now fail to compile — fix by setting `Category` instead. (Known setters live only in `MimeTypeResolver.cs`, addressed in Task 2; if the build fails there, continue to Task 2 before committing.)

- [ ] **Step 7: Add `DocumentMetadata` key constants**

```csharp
// src/Ferret.Core/Documents/DocumentMetadata.cs
namespace Ferret.Core.Documents;

/// <summary>Canonical keys for <see cref="Document.Metadata"/>. Parsers MUST use these constants
/// (never raw strings) so keys never drift (PageCount vs Pagecount vs "Page Count") across parsers.</summary>
public static class DocumentMetadata
{
    /// <summary>Document author / creator.</summary>
    public const string Author = "Author";

    /// <summary>Document subject.</summary>
    public const string Subject = "Subject";

    /// <summary>Document keywords.</summary>
    public const string Keywords = "Keywords";

    /// <summary>Page count (PDF).</summary>
    public const string PageCount = "PageCount";

    /// <summary>Worksheet count (XLSX).</summary>
    public const string SheetCount = "SheetCount";

    /// <summary>Creation timestamp (ISO-8601).</summary>
    public const string Created = "Created";

    /// <summary>Last-modified timestamp (ISO-8601).</summary>
    public const string Modified = "Modified";

    /// <summary>Document category.</summary>
    public const string Category = "Category";

    /// <summary>Set to "true" when extracted text was truncated by the configured limit.</summary>
    public const string Truncated = "Truncated";
}
```

- [ ] **Step 8: Add `ParserOptions`**

```csharp
// src/Ferret.Core/Documents/ParserOptions.cs
namespace Ferret.Core.Documents;

/// <summary>Host-configurable options for content parsers.</summary>
public sealed record ParserOptions
{
    /// <summary>Maximum characters of extracted text to keep per document.
    /// Null (default) means unlimited — documents index completely unless an administrator caps them.</summary>
    public long? MaxExtractedCharacters { get; init; }
}
```

- [ ] **Step 9: Write the failing `ExtractionLimiter` test, then implement it**

```csharp
// tests/Ferret.Core.Tests/Documents/ExtractionLimiterTests.cs
using Ferret.Core.Documents;

namespace Ferret.Core.Tests.Documents;

public sealed class ExtractionLimiterTests
{
    [Fact]
    public void Unlimited_By_Default_Returns_Text_Unchanged()
    {
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hello world", new ParserOptions());
        Assert.Equal("hello world", text);
        Assert.False(truncated);
    }

    [Fact]
    public void Truncates_When_Over_Limit()
    {
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hello world", new ParserOptions { MaxExtractedCharacters = 5 });
        Assert.Equal("hello", text);
        Assert.True(truncated);
    }

    [Fact]
    public void No_Truncation_When_Under_Limit()
    {
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hi", new ParserOptions { MaxExtractedCharacters = 5 });
        Assert.Equal("hi", text);
        Assert.False(truncated);
    }
}
```

Run: `dotnet test tests/Ferret.Core.Tests --filter ExtractionLimiterTests` → FAIL, then implement:

```csharp
// src/Ferret.Core/Documents/ExtractionLimiter.cs
namespace Ferret.Core.Documents;

/// <summary>The single shared implementation of the configurable extracted-text limit.
/// Every heavyweight parser (PDF, Word, Excel) calls this — no per-parser truncation logic.</summary>
public static class ExtractionLimiter
{
    /// <summary>Applies <see cref="ParserOptions.MaxExtractedCharacters"/> to <paramref name="text"/>.</summary>
    /// <param name="text">The extracted text.</param>
    /// <param name="options">Parser options carrying the optional limit.</param>
    /// <returns>The (possibly truncated) text and whether truncation occurred.</returns>
    public static (string Text, bool Truncated) ApplyCharacterLimit(string text, ParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxExtractedCharacters is long max && text.Length > max)
        {
            return (text[..(int)max], true);
        }

        return (text, false);
    }
}
```

Run again → PASS.

- [ ] **Step 10: Reserve the `StructuredExtraction` capability**

In `src/Ferret.Core/Documents/ParserCapabilities.cs`, add a new capability alongside the existing ones (leave `All` and existing members intact):

```csharp
/// <summary>Reserved: parser produces richer structured extraction (tables, slides, mail parts).
/// Unused this milestone — declared so future parsers (OCR, PowerPoint, Outlook) can advertise it
/// without a contract change.</summary>
public static readonly ParserCapability StructuredExtraction =
    new(
        "structured-extraction",
        "Structured Extraction",
        "1.0",
        "Extracts structured content (tables, slides, message parts) beyond flat text.");
```

(Do not add it to any parser's `Capabilities` list this milestone — it is a reserved extension point only.)

- [ ] **Step 11: Commit**

```bash
git add src/Ferret.Core/Documents/MediaCategory.cs src/Ferret.Core/Documents/MediaTypeInfo.cs src/Ferret.Core/Documents/DocumentMetadata.cs src/Ferret.Core/Documents/ParserOptions.cs src/Ferret.Core/Documents/ExtractionLimiter.cs src/Ferret.Core/Documents/ParserCapabilities.cs tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs tests/Ferret.Core.Tests/Documents/ExtractionLimiterTests.cs
git commit -m "feat(core): add MediaCategory, DocumentMetadata, ParserOptions, ExtractionLimiter, reserved StructuredExtraction"
```

---

### Task 2: MimeTypeResolver — parseable-binary mappings, expanded text/code/config map, expanded binary denylist (ParserPlatform)

**Files:**
- Modify: `src/Ferret.ParserPlatform/MimeTypeResolver.cs`
- Test: `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs` (create if absent)

**Interfaces:**
- Consumes: `MediaCategory` (Task 1).
- Produces: resolver emits `application/pdf` (`BinaryParseable`, `Prose`) for `.pdf`; the OpenXML wordprocessing media type (`BinaryParseable`, `Prose`) for `.docx`; new text/code/config mappings; expanded `BinaryOpaque` denylist. Public DOCX media-type constant is defined in Task 4 (`OfficeMediaTypes.Docx`); for Task 2 use the literal string.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs
using Ferret.Core.Documents;
using Ferret.ParserPlatform;

namespace Ferret.ParserPlatform.Tests;

public sealed class MimeTypeResolverTests
{
    private static readonly MimeTypeResolver Resolver = new();

    [Fact]
    public void Pdf_Resolves_To_ApplicationPdf_ParseableBinary()
    {
        var info = Resolver.Resolve("report.pdf");
        Assert.Equal("application/pdf", info.MediaType);
        Assert.Equal(MediaCategory.BinaryParseable, info.Category);
        Assert.Equal(DocumentKind.Prose, info.SuggestedKind);
    }

    [Fact]
    public void Docx_Resolves_To_Wordprocessing_ParseableBinary()
    {
        var info = Resolver.Resolve("spec.docx");
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", info.MediaType);
        Assert.Equal(MediaCategory.BinaryParseable, info.Category);
    }

    [Fact]
    public void Xlsx_Resolves_To_Spreadsheet_ParseableBinary_Data()
    {
        var info = Resolver.Resolve("jira-export.xlsx");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", info.MediaType);
        Assert.Equal(MediaCategory.BinaryParseable, info.Category);
        Assert.Equal(DocumentKind.Data, info.SuggestedKind);
    }

    [Theory]
    [InlineData("a.so")]
    [InlineData("a.class")]
    [InlineData("a.pyc")]
    [InlineData("a.nupkg")]
    [InlineData("a.psd")]
    public void Opaque_Binaries_Are_BinaryOpaque(string fileName)
    {
        Assert.Equal(MediaCategory.BinaryOpaque, Resolver.Resolve(fileName).Category);
    }

    [Theory]
    [InlineData("a.php", "text/x-php", DocumentKind.Code)]
    [InlineData("a.scala", "text/x-scala", DocumentKind.Code)]
    [InlineData("a.ini", "text/x-ini", DocumentKind.Config)]
    public void New_Text_Mappings_Have_Correct_Kind(string fileName, string mediaType, DocumentKind kind)
    {
        var info = Resolver.Resolve(fileName);
        Assert.Equal(mediaType, info.MediaType);
        Assert.Equal(MediaCategory.Text, info.Category);
        Assert.Equal(kind, info.SuggestedKind);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter MimeTypeResolverTests`
Expected: FAIL — `.pdf` currently resolves to `application/octet-stream`.

- [ ] **Step 3: Update the factory helpers and map in `MimeTypeResolver.cs`**

Replace the `Text`/`Binary` helpers and add `ParseableBinary`; update map entries. Change every existing `Text(...)` call's resulting category implicitly (the helper now sets `Category = Text`). Concretely:

```csharp
// Replace the Text(...) helper:
private static MediaTypeInfo Text(string mediaType, DocumentKind kind) => new()
{
    MediaType = mediaType,
    Category = MediaCategory.Text,
    SuggestedKind = kind,
    Confidence = 1.0,
};

// Replace the Binary() helper (now explicitly opaque):
private static MediaTypeInfo Binary() => new()
{
    MediaType = "application/octet-stream",
    Category = MediaCategory.BinaryOpaque,
    Confidence = 1.0,
};

// Add a new helper for parseable binaries:
private static MediaTypeInfo ParseableBinary(string mediaType, DocumentKind kind) => new()
{
    MediaType = mediaType,
    Category = MediaCategory.BinaryParseable,
    SuggestedKind = kind,
    Confidence = 1.0,
};
```

Update `UnknownText` to use the enum:

```csharp
private static readonly MediaTypeInfo UnknownText = new()
{
    MediaType = "text/plain",
    Category = MediaCategory.Text,
    Confidence = 0.5,
};
```

- [ ] **Step 4: Replace the `.pdf`/`.docx` entries and add the new mappings in the `Map` dictionary**

Change these two existing entries:

```csharp
[".pdf"] = ParseableBinary("application/pdf", DocumentKind.Prose),
[".docx"] = ParseableBinary("application/vnd.openxmlformats-officedocument.wordprocessingml.document", DocumentKind.Prose),
[".xlsx"] = ParseableBinary("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", DocumentKind.Data),
```

`.pptx` stays `Binary()` (opaque — deferred fast-follow this milestone). Note `.xlsx` is classified `DocumentKind.Data`, not `Prose`. Add the expanded text/code/config entries:

```csharp
[".scss"] = Text("text/x-scss", DocumentKind.Code),
[".less"] = Text("text/x-less", DocumentKind.Code),
[".php"] = Text("text/x-php", DocumentKind.Code),
[".scala"] = Text("text/x-scala", DocumentKind.Code),
[".clj"] = Text("text/x-clojure", DocumentKind.Code),
[".cljs"] = Text("text/x-clojure", DocumentKind.Code),
[".dart"] = Text("text/x-dart", DocumentKind.Code),
[".lua"] = Text("text/x-lua", DocumentKind.Code),
[".r"] = Text("text/x-r", DocumentKind.Code),
[".pl"] = Text("text/x-perl", DocumentKind.Code),
[".groovy"] = Text("text/x-groovy", DocumentKind.Code),
[".gradle"] = Text("text/x-groovy", DocumentKind.Config),
[".bat"] = Text("text/x-bat", DocumentKind.Code),
[".cmd"] = Text("text/x-bat", DocumentKind.Code),
[".psm1"] = Text("text/x-powershell", DocumentKind.Code),
[".psd1"] = Text("text/x-powershell", DocumentKind.Config),
[".vb"] = Text("text/x-vb", DocumentKind.Code),
[".fs"] = Text("text/x-fsharp", DocumentKind.Code),
[".fsx"] = Text("text/x-fsharp", DocumentKind.Code),
[".ini"] = Text("text/x-ini", DocumentKind.Config),
[".cfg"] = Text("text/x-ini", DocumentKind.Config),
[".conf"] = Text("text/x-ini", DocumentKind.Config),
[".env"] = Text("text/x-dotenv", DocumentKind.Config),
[".properties"] = Text("text/x-properties", DocumentKind.Config),
[".csproj"] = Text("text/xml", DocumentKind.Config),
[".vbproj"] = Text("text/xml", DocumentKind.Config),
[".fsproj"] = Text("text/xml", DocumentKind.Config),
[".props"] = Text("text/xml", DocumentKind.Config),
[".targets"] = Text("text/xml", DocumentKind.Config),
[".resx"] = Text("text/xml", DocumentKind.Data),
[".xaml"] = Text("text/xml", DocumentKind.Code),
[".rst"] = Text("text/x-rst", DocumentKind.Prose),
[".adoc"] = Text("text/x-asciidoc", DocumentKind.Prose),
[".tex"] = Text("text/x-tex", DocumentKind.Prose),
[".gitignore"] = Text("text/plain", DocumentKind.Config),
[".editorconfig"] = Text("text/plain", DocumentKind.Config),
```

Add the expanded binary denylist (these are NOT already mapped):

```csharp
[".so"] = Binary(),
[".dylib"] = Binary(),
[".a"] = Binary(),
[".o"] = Binary(),
[".lib"] = Binary(),
[".class"] = Binary(),
[".pyc"] = Binary(),
[".pyo"] = Binary(),
[".wasm"] = Binary(),
[".node"] = Binary(),
[".nupkg"] = Binary(),
[".snk"] = Binary(),
[".pfx"] = Binary(),
[".jar"] = Binary(),
[".war"] = Binary(),
[".ear"] = Binary(),
[".db"] = Binary(),
[".sqlite"] = Binary(),
[".parquet"] = Binary(),
[".dat"] = Binary(),
[".keystore"] = Binary(),
[".psd"] = Binary(),
[".ai"] = Binary(),
[".otf"] = Binary(),
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter MimeTypeResolverTests`
Expected: PASS.

- [ ] **Step 6: Run the full ParserPlatform + Core test suites to confirm no regressions**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests && dotnet test tests/Ferret.Core.Tests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Ferret.ParserPlatform/MimeTypeResolver.cs tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs
git commit -m "feat(parsers): map PDF/DOCX/XLSX to parseable-binary media types, expand text and binary maps"
```

---

### Task 2b: CsvParser (Ferret.ParserPlatform) — Sprint 1

**Files:**
- Create: `src/Ferret.ParserPlatform/Parsers/CsvParser.cs`
- Create: `src/Ferret.ParserPlatform/Parsers/CsvRecordReader.cs`
- Modify: `src/Ferret.ParserPlatform/ParserPlatformModule.cs` (register `CsvParser` + default `ParserOptions`)
- Test: `tests/Ferret.ParserPlatform.Tests/Parsers/CsvParserTests.cs`

**Interfaces:**
- Consumes: `ParserOptions`, `ExtractionLimiter`, `DocumentMetadata` (Task 1); `IContentParser`, `ParserDescriptor`, `ParseContext`, `Document` (Ferret.Core).
- Produces: `public sealed class CsvParser : IContentParser` (ctor takes `ParserOptions`; `CanParse` matches `text/csv` + `text/tab-separated-values`; priority 200); `internal static class CsvRecordReader`.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.ParserPlatform.Tests/Parsers/CsvParserTests.cs
using System.Text;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class CsvParserTests
{
    private static AssetDescriptor Asset(string mediaType) => new()
    {
        Id = AssetId.From(new Uri("filesystem:///export.csv")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///export.csv"),
        DisplayName = "export.csv",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = mediaType,
    };

    private static Stream MakeStream(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

    [Fact]
    public void CanParse_Csv_And_Tsv_Only()
    {
        var parser = new CsvParser(new ParserOptions());
        Assert.True(parser.CanParse("text/csv"));
        Assert.True(parser.CanParse("text/tab-separated-values"));
        Assert.False(parser.CanParse("text/plain"));
    }

    [Fact]
    public void Priority_Is_200_To_Beat_PlainText()
    {
        Assert.Equal(200, new CsvParser(new ParserOptions()).Descriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Header_And_Rows_As_Data()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key,Summary,Severity\nBUG-1,Login fails,High\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains("Severity", doc.PlainText, StringComparison.Ordinal); // header
        Assert.Contains("Login fails", doc.PlainText, StringComparison.Ordinal); // cell
        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Handles_Quoted_Field_With_Embedded_Comma()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key,Summary\nBUG-2,\"Fails on login, then crashes\"\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains("Fails on login, then crashes", doc.PlainText, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter CsvParserTests`
Expected: FAIL — `CsvParser` does not exist.

- [ ] **Step 3: Implement the quote-aware record reader**

```csharp
// src/Ferret.ParserPlatform/Parsers/CsvRecordReader.cs
using System.Text;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>Minimal RFC-4180 reader: yields records of fields. Fields may be quoted with double
/// quotes and contain the delimiter or newlines; a doubled quote ("") is an escaped quote.</summary>
internal static class CsvRecordReader
{
    public static IEnumerable<IReadOnlyList<string>> ReadRecords(string text, char delimiter)
    {
        var field = new StringBuilder();
        var record = new List<string>();
        var inQuotes = false;
        var pending = false; // true once any char/field seen on the current record

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else { inQuotes = false; }
                }
                else { field.Append(c); }

                continue;
            }

            switch (c)
            {
                case '"': inQuotes = true; pending = true; break;
                case '\r': break; // ignore; handled by \n
                case var d when d == delimiter: record.Add(field.ToString()); field.Clear(); pending = true; break;
                case '\n':
                    record.Add(field.ToString()); field.Clear();
                    yield return record;
                    record = new List<string>(); pending = false; break;
                default: field.Append(c); pending = true; break;
            }
        }

        if (pending || field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            yield return record;
        }
    }
}
```

- [ ] **Step 4: Implement `CsvParser`**

```csharp
// src/Ferret.ParserPlatform/Parsers/CsvParser.cs
using System.Text;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Structure-aware parser for CSV and TSV (<c>text/csv</c>, <c>text/tab-separated-values</c>).
/// Dependency-free; lives in the platform beside JSON/Markdown. Emits header + data rows so column
/// tokens are searchable. Read-only; no chunking, embedding, or AI processing.
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
        foreach (var record in CsvRecordReader.ReadRecords(raw, delimiter))
        {
            ct.ThrowIfCancellationRequested();
            var joined = string.Join('\t', record.Where(f => !string.IsNullOrEmpty(f)));
            if (joined.Length > 0)
            {
                sb.AppendLine(joined);
            }
        }

        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit(sb.ToString().Trim(), _options);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
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
```

- [ ] **Step 5: Register `CsvParser` in `ParserPlatformModule`**

In `src/Ferret.ParserPlatform/ParserPlatformModule.cs`, add the `using` and register the parser + a default `ParserOptions` (so `CsvParser`'s constructor resolves). Add alongside the existing built-in registrations:

```csharp
using Microsoft.Extensions.DependencyInjection.Extensions; // for TryAddSingleton
// ...
services.TryAddSingleton(new ParserOptions()); // unlimited default; host may override before wiring
services.AddSingleton<IContentParser, CsvParser>();
```

(The registry factory already aggregates via `GetServices<IContentParser>()`, so no registry change.)

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter CsvParserTests`
Expected: PASS (4 tests). CSV/TSV are now searchable through the already-wired `ParserPlatformModule` — no CLI change needed for Sprint 1.

- [ ] **Step 7: Commit**

```bash
git add src/Ferret.ParserPlatform/Parsers/CsvParser.cs src/Ferret.ParserPlatform/Parsers/CsvRecordReader.cs src/Ferret.ParserPlatform/ParserPlatformModule.cs tests/Ferret.ParserPlatform.Tests/Parsers/CsvParserTests.cs
git commit -m "feat(parsers): add structure-aware CsvParser for CSV/TSV enterprise exports"
```

---

### Task 3: Ferret.Parsers.Pdf — PdfParser (PdfPig)

**Files:**
- Modify: `Directory.Packages.props` (add `UglyToad.PdfPig`)
- Create: `src/Ferret.Parsers.Pdf/Ferret.Parsers.Pdf.csproj`
- Create: `src/Ferret.Parsers.Pdf/PdfParser.cs`
- Create: `src/Ferret.Parsers.Pdf/PdfParserModule.cs`
- Create: `tests/Ferret.Parsers.Pdf.Tests/Ferret.Parsers.Pdf.Tests.csproj`
- Create: `tests/Ferret.Parsers.Pdf.Tests/PdfParserTests.cs`

**Interfaces:**
- Consumes: `IContentParser`, `ParserDescriptor`, `ParseContext`, `Document`, `DocumentKind`, `ParserCapabilities`, `DocumentId` (all `Ferret.Core`).
- Produces: `public sealed class PdfParser : IContentParser` (`CanParse("application/pdf")`); `public static class PdfParserModule { static void ConfigureServices(IServiceCollection); }`.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`, add a new `ItemGroup`:

```xml
<ItemGroup Label="PDF parsing">
  <PackageVersion Include="UglyToad.PdfPig" Version="1.7.0-custom-5" />
</ItemGroup>
```

Version **pinned** to `1.7.0-custom-5` (implementation deviation from the originally-planned `0.1.9`, which is not obtainable from the configured NuGet feeds; see Sprint 2's version-deviation note). One adapted API difference: `PdfDocumentBuilder` is `IDisposable` in this build. Bumping it later is a separate maintenance task, not part of this implementation.

- [ ] **Interfaces update:** `PdfParser`'s constructor takes `ParserOptions` (Task 1); `PdfParserModule` registers a default `ParserOptions` via `TryAddSingleton` and the parser.

- [ ] **Step 2: Create the project file**

```xml
<!-- src/Ferret.Parsers.Pdf/Ferret.Parsers.Pdf.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Ferret.Parsers.Pdf</RootNamespace>
    <AssemblyName>Ferret.Parsers.Pdf</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="UglyToad.PdfPig" />
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Add projects to the solution**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Parsers.Pdf/Ferret.Parsers.Pdf.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Parsers.Pdf.Tests/Ferret.Parsers.Pdf.Tests.csproj
```

(The test csproj is created in Step 6; running this after Step 6 is fine — or run both `add` commands together then.)

- [ ] **Step 4: Write the failing tests**

```csharp
// tests/Ferret.Parsers.Pdf.Tests/PdfParserTests.cs
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Pdf;

using UglyToad.PdfPig.Writer;

namespace Ferret.Parsers.Pdf.Tests;

public sealed class PdfParserTests
{
    private static AssetDescriptor Asset(string name) => new()
    {
        Id = AssetId.From(new Uri($"filesystem:///{name}")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri($"filesystem:///{name}"),
        DisplayName = name,
        LastModified = DateTimeOffset.UtcNow,
        MediaType = "application/pdf",
    };

    // Builds a one-page PDF containing the given text using PdfPig's writer.
    private static Stream MakePdf(string text)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        page.AddText(text, 12, new UglyToad.PdfPig.Core.PdfPoint(25, 800), font);
        return new MemoryStream(builder.Build());
    }

    [Fact]
    public void CanParse_True_For_ApplicationPdf_Only()
    {
        var parser = new PdfParser(new ParserOptions());
        Assert.True(parser.CanParse("application/pdf"));
        Assert.False(parser.CanParse("text/plain"));
        Assert.False(parser.CanParse("application/octet-stream"));
    }

    [Fact]
    public async Task ParseAsync_Extracts_Text()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("Hello enterprise document");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.Contains("Hello", doc.PlainText, StringComparison.Ordinal);
        Assert.Equal(DocumentKind.Prose, doc.Kind);
        Assert.Equal("application/pdf", doc.MediaType);
    }

    [Fact]
    public async Task ParseAsync_Sets_PageCount_Metadata()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("page one text");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.Equal("1", doc.Metadata[DocumentMetadata.PageCount]);
    }

    [Fact]
    public async Task ParseAsync_Does_Not_Dispose_Stream()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("x");

        await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.True(stream.CanRead); // not disposed
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.Parsers.Pdf.Tests`
Expected: FAIL — `PdfParser` does not exist.

- [ ] **Step 6: Create the test project file**

```xml
<!-- tests/Ferret.Parsers.Pdf.Tests/Ferret.Parsers.Pdf.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.Parsers.Pdf.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="UglyToad.PdfPig" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Parsers.Pdf\Ferret.Parsers.Pdf.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Implement `PdfParser`**

```csharp
// src/Ferret.Parsers.Pdf/PdfParser.cs
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

    private static IReadOnlyDictionary<string, string> BuildMetadata(PdfDocument pdf, bool truncated)
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
```

> Note: image-only/scanned PDFs yield empty `page.Text`, so `PlainText` is empty → the dispatcher returns `Empty` (no garbage indexed). Password-protected/corrupt PDFs make `PdfDocument.Open` throw → the dispatcher returns `Failed` with the exception message. Both behaviors satisfy the spec without special-casing.

- [ ] **Step 8: Implement the DI module**

```csharp
// src/Ferret.Parsers.Pdf/PdfParserModule.cs
using Ferret.Core.Documents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Parsers.Pdf;

/// <summary>DI registration for the PDF parser package.</summary>
public static class PdfParserModule
{
    /// <summary>Registers <see cref="PdfParser"/> as an <see cref="IContentParser"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(new ParserOptions()); // unlimited default unless a host configured one
        services.AddSingleton<IContentParser, PdfParser>();
    }
}
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.Parsers.Pdf.Tests`
Expected: PASS (5 tests).

- [ ] **Step 10: Commit**

```bash
git add Directory.Packages.props src/Ferret.Parsers.Pdf tests/Ferret.Parsers.Pdf.Tests src/Ferret.sln
git commit -m "feat(parsers): add Ferret.Parsers.Pdf with PdfPig-based PdfParser"
```

---

### Task 4: Ferret.Parsers.Office — WordParser (DOCX) + ExcelParser (XLSX)

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
- Consumes: `ParserOptions`, `ExtractionLimiter`, `DocumentMetadata` (all from Task 1).
- Produces: `public static class OfficeMediaTypes { public const string Docx = "..."; public const string Xlsx = "..."; }`; `public sealed class WordParser : IContentParser` (ctor takes `ParserOptions`); `public sealed class ExcelParser : IContentParser` (ctor takes `ParserOptions`); `public static class OfficeParserModule { static void ConfigureServices(IServiceCollection); }` — registers **both** Word and Excel, both honoring the extraction limit.

> Word steps (1–7) are unchanged from the DOCX-only design except that `WordParser` now takes `ParserOptions` and applies the shared `ExtractionLimiter`; the Excel additions follow as Steps E1–E4, then solution-add and commit cover both parsers.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`:

```xml
<ItemGroup Label="Office (OpenXML) parsing">
  <PackageVersion Include="DocumentFormat.OpenXml" Version="3.1.0" />
</ItemGroup>
```

Version **pinned** to `3.1.0`. Bumping it later is a separate maintenance task.

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

- [ ] **Step 3: Write the failing tests**

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

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.Parsers.Office.Tests`
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

> Note: malformed/non-OOXML input makes `WordprocessingDocument.Open` throw → dispatcher returns `Failed`. The "Company" extended property lives in `ExtendedFilePropertiesPart` and is deferred (YAGNI); `PackageProperties` covers the core metadata.

- [ ] **Step 7: Implement the DI module**

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

> Both `WordParser` and `ExcelParser` take `ParserOptions` via DI. `TryAddSingleton(new ParserOptions())` supplies an unlimited default; a host that binds `Ferret:Parsers:MaxExtractedCharacters` from config registers its own `ParserOptions` **before** calling `ParserPackModule`, and `TryAdd` leaves it intact. (`PdfParserModule` registers the same default, so the option is uniform across all three parsers.)

#### Excel (XLSX) additions

> `ParserOptions`, `ExtractionLimiter`, and `DocumentMetadata` were already added to `Ferret.Core` in Task 1 — Excel consumes them directly.

- [ ] **Step E2: Write the failing Excel tests**

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

- [ ] **Step E3: Run Excel tests to verify they fail**

Run: `dotnet test tests/Ferret.Parsers.Office.Tests --filter ExcelParserTests`
Expected: FAIL — `ExcelParser` does not exist.

- [ ] **Step E4: Implement `ExcelParser` (streaming reader, shared strings, cached values, configurable limit)**

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

> Note: `.xls` (legacy binary) is unsupported and stays `BinaryOpaque`; malformed/non-OOXML input makes `SpreadsheetDocument.Open` throw → dispatcher returns `Failed`. Dates stored as serial numbers may surface as serials (documented limitation).

- [ ] **Step 8: Add projects to the solution, build, and run tests**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Parsers.Office/Ferret.Parsers.Office.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Parsers.Office.Tests/Ferret.Parsers.Office.Tests.csproj
dotnet test tests/Ferret.Parsers.Office.Tests
```

Expected: PASS (Word: 2 tests, Excel: 3 tests).

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props src/Ferret.Parsers.Office tests/Ferret.Parsers.Office.Tests src/Ferret.sln
git commit -m "feat(parsers): add Office package with Word (docx) and Excel (xlsx) parsers"
```

---

### Task 5: Ferret.Parsers composition project + host wiring

**Files:**
- Create: `src/Ferret.Parsers/Ferret.Parsers.csproj`
- Create: `src/Ferret.Parsers/ParserPackModule.cs`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj` (add ProjectReference to `Ferret.Parsers`)
- Modify: `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs:68`
- Create: `tests/Ferret.Parsers.Tests/Ferret.Parsers.Tests.csproj`
- Create: `tests/Ferret.Parsers.Tests/ParserPackModuleTests.cs`

**Interfaces:**
- Consumes: `ParserPlatformModule.ConfigureServices`, `PdfParserModule.ConfigureServices`, `OfficeParserModule.ConfigureServices`, `IParserDispatcher`, `IParserRegistry`.
- Produces: `public static class ParserPackModule { static void ConfigureServices(IServiceCollection); }`.

- [ ] **Step 1: Create the composition project**

```xml
<!-- src/Ferret.Parsers/Ferret.Parsers.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Ferret.Parsers</RootNamespace>
    <AssemblyName>Ferret.Parsers</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <ProjectReference Include="..\Ferret.ParserPlatform\Ferret.ParserPlatform.csproj" />
    <ProjectReference Include="..\Ferret.Parsers.Pdf\Ferret.Parsers.Pdf.csproj" />
    <ProjectReference Include="..\Ferret.Parsers.Office\Ferret.Parsers.Office.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Write the failing test**

```csharp
// tests/Ferret.Parsers.Tests/ParserPackModuleTests.cs
using System.Text;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers.Tests;

public sealed class ParserPackModuleTests
{
    [Fact]
    public void Registers_All_Seven_Parsers()
    {
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var parsers = provider.GetServices<IContentParser>().ToList();
        Assert.Equal(7, parsers.Count); // PlainText, Markdown, Json, Csv, Pdf, Word, Excel
    }

    [Fact]
    public async Task Dispatcher_Routes_A_Stream_To_The_Correct_Parser()
    {
        // The dispatcher is the public API; the registry is an implementation detail.
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IParserDispatcher>();

        var asset = new AssetDescriptor
        {
            Id = AssetId.From(new Uri("filesystem:///Greeter.cs")),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///Greeter.cs"),
            DisplayName = "Greeter.cs",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/x-csharp",
        };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("public class Greeter { }"));

        var result = await dispatcher.DispatchAsync(stream, asset);

        Assert.Equal(ParseResultKind.Success, result.Kind);
        Assert.Contains("Greeter", result.Value!.PlainText, StringComparison.Ordinal);
    }
}
```

> PDF/DOCX/XLSX dispatch-routing (the same public API with real binary files) is asserted end-to-end in Task 8. This test covers the composed dispatcher wiring cheaply with an in-memory text stream.

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/Ferret.Parsers.Tests`
Expected: FAIL — `ParserPackModule` does not exist.

- [ ] **Step 4: Create the test project file**

```xml
<!-- tests/Ferret.Parsers.Tests/Ferret.Parsers.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.Parsers.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Parsers\Ferret.Parsers.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Implement `ParserPackModule`**

```csharp
// src/Ferret.Parsers/ParserPackModule.cs
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers;

/// <summary>
/// Single composition entry point for the full parser pack: the platform (registry, dispatcher,
/// MimeTypeResolver, built-in text parsers) plus the PDF and Office parser packages.
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

- [ ] **Step 6: Add projects to the solution**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Parsers/Ferret.Parsers.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Parsers.Tests/Ferret.Parsers.Tests.csproj
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test tests/Ferret.Parsers.Tests`
Expected: PASS.

- [ ] **Step 8: Wire the composition module into the CLI**

Add a ProjectReference in `src/Ferret.Cli/Ferret.Cli.csproj` (inside the existing `<ItemGroup>` that holds ProjectReferences):

```xml
<ProjectReference Include="..\Ferret.Parsers\Ferret.Parsers.csproj" />
```

In `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs`, change line 68 from:

```csharp
        // Parser platform — resolves IParserDispatcher required by IIndexPipeline.
        ParserPlatformModule.ConfigureServices(services);
```

to:

```csharp
        // Parser pack — platform + PDF + Office parsers; resolves IParserDispatcher required by IIndexPipeline.
        Ferret.Parsers.ParserPackModule.ConfigureServices(services);
```

(Remove the now-unused `using Ferret.ParserPlatform;` only if no other symbol from it is referenced in the file.)

- [ ] **Step 9: Build and run the CLI test suite**

Run: `dotnet build src/Ferret.sln && dotnet test tests/Ferret.Cli.Tests`
Expected: build + tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/Ferret.Parsers tests/Ferret.Parsers.Tests src/Ferret.Cli/Ferret.Cli.csproj src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs src/Ferret.sln
git commit -m "feat(parsers): add ParserPackModule composition and wire it into the index command"
```

---

### Task 6: InstalledParsersCheck (ferret doctor introspection)

**Files:**
- Create: `src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs`
- Modify: `src/Ferret.Cli/Commands/CoreCliModule.cs` (`BuildChecks`)
- Test: `tests/Ferret.Cli.Tests/Diagnostics/InstalledParsersCheckTests.cs`

**Interfaces:**
- Consumes: `IDiagnosticCheck`, `DiagnosticCheckResult`, `IFerretContext`, `IEnumerable<IContentParser>`, `IMimeTypeResolver`.
- Produces: `internal sealed class InstalledParsersCheck : IDiagnosticCheck`.

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

        Assert.False(result.Passed || !result.IsWarning); // warning, not pass
        Assert.True(result.IsWarning);
    }
}
```

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

- [ ] **Step 4: Register the check in `CoreCliModule.BuildChecks`**

In `src/Ferret.Cli/Commands/CoreCliModule.cs`, the `BuildChecks` method yields checks. Add the parser check. Because `BuildChecks` is static and does not have DI access, compose the parser list via `ParserPackModule` into a throwaway provider:

```csharp
// Add near the other `yield return` checks in BuildChecks(...):
{
    var parserServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    Ferret.Parsers.ParserPackModule.ConfigureServices(parserServices);
    using var parserProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
        .CreateScope(parserServices.BuildServiceProvider());
    var parsers = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
        .GetServices<Ferret.Core.Documents.IContentParser>(parserProvider.ServiceProvider).ToList();
    var resolver = new Ferret.ParserPlatform.MimeTypeResolver();
    yield return new Checks.InstalledParsersCheck(parsers, parsers.Count, MimeTypeResolver.KnownExtensionCount);
}
```

This requires a small additive helper on `MimeTypeResolver` to expose the count of non-opaque (parseable/text) extensions. Add to `src/Ferret.ParserPlatform/MimeTypeResolver.cs`:

```csharp
/// <summary>Gets the number of mapped extensions that resolve to text or parseable-binary content.</summary>
public static int KnownExtensionCount => Map.Count(kv => kv.Value.Category != MediaCategory.BinaryOpaque);
```

(If `using Ferret.Parsers;` / `using Ferret.ParserPlatform;` are not already present, prefer the fully-qualified names shown above to avoid touching the file's using block.)

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.Cli.Tests --filter InstalledParsersCheckTests`
Expected: PASS.

- [ ] **Step 6: Manually verify the doctor output**

Run: `dotnet run --project src/Ferret.Cli -- doctor`
Expected: output includes a line naming the 7 installed parsers (Plain Text, Markdown, JSON, CSV, PDF, Word/DOCX, Excel/XLSX) and the supported-extension count.

- [ ] **Step 7: Commit**

```bash
git add src/Ferret.Cli/Diagnostics/Checks/InstalledParsersCheck.cs src/Ferret.Cli/Commands/CoreCliModule.cs src/Ferret.ParserPlatform/MimeTypeResolver.cs tests/Ferret.Cli.Tests/Diagnostics/InstalledParsersCheckTests.cs
git commit -m "feat(cli): add installed-parsers diagnostic check to ferret doctor"
```

---

### Task 7: Synthetic Enterprise Corpus Generator (abstract model + renderers)

**Files:**
- Modify: `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` (reference `Ferret.Parsers.Office`, `UglyToad.PdfPig`)
- Create: `tests/Ferret.Benchmarks/Corpus/CorpusDocument.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/IDocumentRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/CorpusSize.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/MarkdownRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/HtmlRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/CSharpRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/JsonRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/PdfRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/DocxRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/Renderers/XlsxRenderer.cs`
- Create: `tests/Ferret.Benchmarks/Corpus/EnterpriseArchetypes.cs` (tabular Jira/RTM/bug/risk documents)
- Create: `tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs`
- Create: `tests/Ferret.Benchmarks.Tests/Corpus/CorpusGeneratorTests.cs` (new test project, see Step 8)

> **Note on test placement:** `Ferret.Benchmarks` is a console (BenchmarkDotNet) project, not a test project. Put the generator's unit tests in a sibling `tests/Ferret.Benchmarks.Tests` xUnit project that references `Ferret.Benchmarks`.

**Interfaces:**
- Produces:
  - `sealed record CorpusBlock(CorpusBlockKind Kind, string Text)` and `enum CorpusBlockKind { Heading, Paragraph, CodeLine, KeyValue }`
  - `sealed record CorpusTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows)`
  - `sealed record CorpusDocument(string Title, IReadOnlyList<CorpusBlock> Blocks, IReadOnlyList<CorpusTable> Tables)`
  - `interface IDocumentRenderer { string Extension { get; } void Render(CorpusDocument doc, Stream output); }`
  - `enum CorpusSize { Small, Medium, Enterprise }`
  - `static class EnterpriseArchetypes { IReadOnlyList<CorpusDocument> Build(Random rng); }` — Jira/RTM/bug/risk tabular docs
  - `sealed class SyntheticEnterpriseCorpusGenerator { SyntheticEnterpriseCorpusGenerator(int seed); void Generate(CorpusSize size, string outputRoot); }`

- [ ] **Step 1: Write the failing determinism test**

```csharp
// tests/Ferret.Benchmarks.Tests/Corpus/CorpusGeneratorTests.cs
using Ferret.Benchmarks.Corpus;

namespace Ferret.Benchmarks.Tests.Corpus;

public sealed class CorpusGeneratorTests
{
    [Fact]
    public void Same_Seed_Produces_Identical_Bytes()
    {
        var dirA = Path.Join(Path.GetTempPath(), "corpus-a-" + Guid.NewGuid().ToString("N"));
        var dirB = Path.Join(Path.GetTempPath(), "corpus-b-" + Guid.NewGuid().ToString("N"));
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirA);
            new SyntheticEnterpriseCorpusGenerator(seed: 42).Generate(CorpusSize.Small, dirB);

            var filesA = Directory.GetFiles(dirA, "*", SearchOption.AllDirectories).OrderBy(p => p).ToList();
            var filesB = Directory.GetFiles(dirB, "*", SearchOption.AllDirectories).OrderBy(p => p).ToList();

            Assert.Equal(filesA.Count, filesB.Count);
            for (var i = 0; i < filesA.Count; i++)
            {
                Assert.Equal(Path.GetFileName(filesA[i]), Path.GetFileName(filesB[i]));
                Assert.Equal(File.ReadAllBytes(filesA[i]), File.ReadAllBytes(filesB[i]));
            }
        }
        finally
        {
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
        }
    }

    [Fact]
    public void Small_Corpus_Emits_All_Format_Subdirs()
    {
        var dir = Path.Join(Path.GetTempPath(), "corpus-" + Guid.NewGuid().ToString("N"));
        try
        {
            new SyntheticEnterpriseCorpusGenerator(seed: 1).Generate(CorpusSize.Small, dir);
            Assert.True(Directory.Exists(Path.Join(dir, "SourceCode")));
            Assert.True(Directory.Exists(Path.Join(dir, "Documentation")));
            Assert.True(Directory.Exists(Path.Join(dir, "PDF")));
            Assert.True(Directory.Exists(Path.Join(dir, "Word")));
            Assert.True(Directory.Exists(Path.Join(dir, "Excel")));
            Assert.True(Directory.Exists(Path.Join(dir, "Mixed")));
            Assert.NotEmpty(Directory.GetFiles(Path.Join(dir, "PDF"), "*.pdf"));
            Assert.NotEmpty(Directory.GetFiles(Path.Join(dir, "Word"), "*.docx"));
            Assert.NotEmpty(Directory.GetFiles(Path.Join(dir, "Excel"), "*.xlsx"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Ferret.Benchmarks.Tests`
Expected: FAIL — generator types do not exist.

- [ ] **Step 3: Create the logical model**

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

/// <summary>A format-agnostic table: a header row plus data rows. Rendered as a Markdown pipe table,
/// a Word table, or an Excel sheet by the respective renderer.</summary>
public sealed record CorpusTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>A logical, format-agnostic document. Renderers turn it into concrete file bytes.
/// A document may carry prose blocks, tables, or both.</summary>
public sealed record CorpusDocument(
    string Title,
    IReadOnlyList<CorpusBlock> Blocks,
    IReadOnlyList<CorpusTable> Tables);
```

```csharp
// tests/Ferret.Benchmarks/Corpus/IDocumentRenderer.cs
namespace Ferret.Benchmarks.Corpus;

/// <summary>Renders a logical <see cref="CorpusDocument"/> into a concrete file format.</summary>
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

- [ ] **Step 4: Implement the text-family renderers (Markdown, HTML, C#, JSON)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/MarkdownRenderer.cs
using System.Text;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as Markdown.</summary>
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

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }
}
```

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/HtmlRenderer.cs
using System.Text;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a minimal HTML document.</summary>
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
        sb.Append("<html><head><title>").Append(doc.Title).Append("</title></head><body>");
        sb.Append("<h1>").Append(doc.Title).Append("</h1>");
        foreach (var block in doc.Blocks)
        {
            sb.Append("<p>").Append(block.Text).Append("</p>");
        }

        sb.Append("</body></html>");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }
}
```

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/CSharpRenderer.cs
using System.Text;

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

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/JsonRenderer.cs
using System.Text;
using System.Text.Json;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a JSON object.</summary>
public sealed class JsonRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".json";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        // Deterministic: no indentation randomness, ordinal property order from the block list.
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartObject();
        writer.WriteString("title", doc.Title);
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

- [ ] **Step 5: Implement the DOCX renderer (OpenXml)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/DocxRenderer.cs
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a real .docx using OpenXml.</summary>
public sealed class DocxRenderer : IDocumentRenderer
{
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

        // Tables (e.g. enterprise archetypes) render as real Word tables.
        foreach (var t in doc.Tables)
        {
            var table = new Table();
            table.Append(RowOf(t.Headers));
            foreach (var row in t.Rows)
            {
                table.Append(RowOf(row));
            }

            body.Append(table);
        }

        main.Document = new Document(body);
        word.PackageProperties.Title = doc.Title;
        word.PackageProperties.Creator = "Synthetic Corpus Generator";
    }

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

> **Determinism caveat:** OpenXml packages (`.docx` and `.xlsx`) embed creation timestamps by default. To keep bytes identical across runs, pin package timestamps to a fixed value in the OOXML renderers. If byte-identical OOXML proves impractical, the determinism test compares identical **extracted text** for `.docx`/`.xlsx` instead of identical bytes — note this in the test if you take that path.

- [ ] **Step 6: Implement the PDF renderer (PdfPig writer; fallback allowed)**

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/PdfRenderer.cs
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

    private static string Truncate(string text) => text.Length <= 100 ? text : text[..100];
}
```

> If PdfPig's writer cannot meet a determinism or content requirement, replace this renderer's body with a minimal hand-rolled single-page PDF emitter. This is benchmark-only and never touches the production `PdfParser`.

- [ ] **Step 6a: Implement the XLSX renderer (OpenXml)**

Renders a `CorpusDocument`'s tables as real worksheets (one sheet per table), using shared strings so the `ExcelParser`'s SharedString path is exercised end-to-end.

```csharp
// tests/Ferret.Benchmarks/Corpus/Renderers/XlsxRenderer.cs
using System.Globalization;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument's tables as a real .xlsx (one worksheet per table).</summary>
public sealed class XlsxRenderer : IDocumentRenderer
{
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

        var sheets = wbPart.Workbook.AppendChild(new Sheets());
        uint sheetId = 1;

        // Fall back to a single sheet built from the title when the doc has no tables.
        var tables = doc.Tables.Count > 0
            ? doc.Tables
            : [new CorpusTable(["Title"], [[doc.Title]])];

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
                foreach (var cell in row) r.Append(SharedCell(cell));
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
    }
}
```

- [ ] **Step 6b: Implement the enterprise tabular archetypes**

```csharp
// tests/Ferret.Benchmarks/Corpus/EnterpriseArchetypes.cs
using System.Globalization;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Builds realistic enterprise tabular documents — the artifacts that motivated Excel support
/// (requirement traceability, bug reports, sprint backlog, risk register). Deterministic given the RNG.
/// </summary>
public static class EnterpriseArchetypes
{
    /// <summary>Builds one document per archetype, each carrying a single <see cref="CorpusTable"/>.</summary>
    /// <param name="rng">Seeded RNG for row content.</param>
    /// <returns>The archetype documents.</returns>
    public static IReadOnlyList<CorpusDocument> Build(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return
        [
            Doc("Requirement Traceability Matrix",
                ["ID", "Requirement", "Priority", "Status", "Linked Test", "Owner"],
                rng, 25, i => [$"REQ-{i:D3}", Phrase(rng), Pick(rng, "High", "Medium", "Low"), Pick(rng, "Open", "Done"), $"TC-{i:D3}", Pick(rng, "Alice", "Bob", "Chandra")]),
            Doc("Bug Report Export",
                ["Key", "Summary", "Severity", "Status", "Assignee", "Created"],
                rng, 25, i => [$"BUG-{i:D3}", Phrase(rng), Pick(rng, "Blocker", "Major", "Minor"), Pick(rng, "Open", "In Progress", "Closed"), Pick(rng, "Alice", "Bob"), "2026-01-01"]),
            Doc("Sprint Backlog",
                ["Story", "Points", "Sprint", "State", "Epic"],
                rng, 25, i => [$"STORY-{i:D3}", Pick(rng, "1", "2", "3", "5", "8"), Pick(rng, "S-12", "S-13"), Pick(rng, "To Do", "Doing", "Done"), Pick(rng, "Search", "Indexing")]),
            Doc("Risk Register",
                ["Risk", "Likelihood", "Impact", "Mitigation", "Owner"],
                rng, 25, i => [$"RISK-{i:D3}: {Phrase(rng)}", Pick(rng, "Low", "Medium", "High"), Pick(rng, "Low", "Medium", "High"), Phrase(rng), Pick(rng, "Alice", "Bob")]),
            Doc("Test Execution Report",
                ["Test", "Result", "Duration", "Build", "Tester"],
                rng, 25, i => [$"TC-{i:D3}", Pick(rng, "Pass", "Fail", "Skipped"), $"{rng.Next(1, 900)}ms", $"build-{rng.Next(100, 999)}", Pick(rng, "Alice", "Bob")]),
            Doc("Release Checklist",
                ["Item", "Owner", "Status", "Due", "Notes"],
                rng, 25, i => [$"Item {i}: {Phrase(rng)}", Pick(rng, "Alice", "Bob", "Chandra"), Pick(rng, "Pending", "Done", "Blocked"), "2026-02-01", Phrase(rng)]),
            Doc("Deployment Plan",
                ["Step", "Environment", "Owner", "Rollback", "Status"],
                rng, 25, i => [$"Step {i}: {Phrase(rng)}", Pick(rng, "Dev", "Staging", "Prod"), Pick(rng, "Alice", "Bob"), Pick(rng, "Yes", "No"), Pick(rng, "Planned", "Complete")]),
            Doc("Production Incident",
                ["Incident", "Severity", "Detected", "Resolved", "Root Cause"],
                rng, 25, i => [$"INC-{i:D3}: {Phrase(rng)}", Pick(rng, "SEV1", "SEV2", "SEV3"), "2026-01-15", "2026-01-15", Phrase(rng)]),
            Doc("Security Findings",
                ["Finding", "CVSS", "Component", "Status", "Remediation"],
                rng, 25, i => [$"SEC-{i:D3}: {Phrase(rng)}", Pick(rng, "3.1", "5.4", "7.8", "9.1"), Pick(rng, "auth", "index", "api"), Pick(rng, "Open", "Fixed"), Phrase(rng)]),
        ];
    }

    private static CorpusDocument Doc(
        string title, string[] headers, Random rng, int rows, Func<int, IReadOnlyList<string>> row)
    {
        var data = new List<IReadOnlyList<string>>();
        for (var i = 1; i <= rows; i++) data.Add(row(i));
        return new CorpusDocument(title, [], [new CorpusTable(headers, data)]);
    }

    private static readonly string[] Terms =
        ["login", "export", "index", "search", "auth", "cache", "report", "sync", "upload", "filter"];

    private static string Phrase(Random rng) =>
        string.Create(CultureInfo.InvariantCulture, $"{Terms[rng.Next(Terms.Length)]} {Terms[rng.Next(Terms.Length)]}");

    private static string Pick(Random rng, params string[] options) => options[rng.Next(options.Length)];
}
```

- [ ] **Step 7: Implement the generator (seeded, deterministic)**

```csharp
// tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs
using System.Globalization;

using Ferret.Benchmarks.Corpus.Renderers;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Generates a deterministic, multi-format synthetic enterprise corpus: source code, documentation,
/// PDFs, Word documents, Excel workbooks (enterprise tabular archetypes), and a mixed repo tree.
/// Same seed + size produces identical output.
/// Reusable beyond benchmarks (demo data, CI fixtures). Lives in the benchmark project; not committed output.
/// </summary>
public sealed class SyntheticEnterpriseCorpusGenerator
{
    private static readonly DateTime FixedTimestamp =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); // fixed so DOCX/PDF metadata stays deterministic

    private static readonly string[] Words =
    [
        "platform", "indexing", "context", "retrieval", "search", "document", "parser", "pipeline",
        "connector", "workspace", "enterprise", "knowledge", "throughput", "latency", "compression",
    ];

    private readonly int _seed;

    /// <summary>Initializes a new generator with a fixed RNG seed for reproducibility.</summary>
    /// <param name="seed">The RNG seed.</param>
    public SyntheticEnterpriseCorpusGenerator(int seed) => _seed = seed;

    /// <summary>Generates the corpus into <paramref name="outputRoot"/>.</summary>
    /// <param name="size">The corpus size tier.</param>
    /// <param name="outputRoot">The destination directory (created if missing).</param>
    public void Generate(CorpusSize size, string outputRoot)
    {
        ArgumentNullException.ThrowIfNull(outputRoot);
        var counts = CountsFor(size);
        var rng = new Random(_seed); // single seeded RNG drives all content => deterministic

        Emit(rng, Path.Join(outputRoot, "SourceCode"), counts.Code, new CSharpRenderer());
        Emit(rng, Path.Join(outputRoot, "Documentation"), counts.Docs, new MarkdownRenderer());
        Emit(rng, Path.Join(outputRoot, "PDF"), counts.Pdf, new PdfRenderer());
        Emit(rng, Path.Join(outputRoot, "Word"), counts.Word, new DocxRenderer());

        // Excel: enterprise tabular archetypes (Jira/RTM/bug/risk), cycled to fill the count.
        var excelDir = Path.Join(outputRoot, "Excel");
        Directory.CreateDirectory(excelDir);
        var xlsx = new XlsxRenderer();
        for (var i = 0; i < counts.Excel; i++)
        {
            var archetypes = EnterpriseArchetypes.Build(rng); // rebuilt per doc so RNG advances deterministically
            var doc = archetypes[i % archetypes.Count];
            var fileName = string.Create(CultureInfo.InvariantCulture, $"sheet{i:D5}.xlsx");
            using var fs = File.Create(Path.Join(excelDir, fileName));
            xlsx.Render(doc, fs);
        }

        // Mixed: alternate renderers deterministically by index.
        IDocumentRenderer[] mixed = [new CSharpRenderer(), new MarkdownRenderer(), new JsonRenderer(), new HtmlRenderer()];
        Directory.CreateDirectory(Path.Join(outputRoot, "Mixed"));
        for (var i = 0; i < counts.Mixed; i++)
        {
            EmitOne(rng, Path.Join(outputRoot, "Mixed"), i, mixed[i % mixed.Length]);
        }
    }

    private void Emit(Random rng, string dir, int count, IDocumentRenderer renderer)
    {
        Directory.CreateDirectory(dir);
        for (var i = 0; i < count; i++)
        {
            EmitOne(rng, dir, i, renderer);
        }
    }

    private void EmitOne(Random rng, string dir, int index, IDocumentRenderer renderer)
    {
        var doc = BuildDocument(rng, index);
        var fileName = string.Create(CultureInfo.InvariantCulture, $"doc{index:D5}{renderer.Extension}");
        using var fs = File.Create(Path.Join(dir, fileName));
        renderer.Render(doc, fs);
    }

    // Enterprise-like titles improve search-quality evaluation (titles are strong ranking signals).
    private static readonly string[] TitleTemplates =
    [
        "Sprint {0} Planning", "Architecture Decision {0}", "Bug Investigation {0}",
        "Quarterly Review {0}", "Security Assessment {0}", "Incident Report {0}",
        "Design Proposal {0}", "Release Notes {0}", "Runbook {0}", "Postmortem {0}",
    ];

    private CorpusDocument BuildDocument(Random rng, int index)
    {
        var blocks = new List<CorpusBlock>();
        var paraCount = 3 + rng.Next(5);
        for (var p = 0; p < paraCount; p++)
        {
            blocks.Add(new CorpusBlock(CorpusBlockKind.Paragraph, Sentence(rng)));
        }

        var template = TitleTemplates[rng.Next(TitleTemplates.Length)];
        var title = string.Format(CultureInfo.InvariantCulture, template, index);
        return new CorpusDocument(title, blocks, Tables: []);
    }

    private string Sentence(Random rng)
    {
        var len = 6 + rng.Next(10);
        var parts = new string[len];
        for (var i = 0; i < len; i++)
        {
            parts[i] = Words[rng.Next(Words.Length)];
        }

        return string.Join(' ', parts) + ".";
    }

    private static (int Code, int Docs, int Pdf, int Word, int Excel, int Mixed) CountsFor(CorpusSize size) => size switch
    {
        CorpusSize.Small => (90, 30, 30, 20, 10, 20),
        CorpusSize.Medium => (1000, 300, 300, 200, 100, 100),
        CorpusSize.Enterprise => (9000, 2000, 2000, 1000, 1000, 1000),
        _ => (90, 30, 30, 20, 10, 20),
    };
}
```

> Implementation note for determinism: if DOCX/XLSX/PDF embed wall-clock timestamps, pin them to `FixedTimestamp` inside the respective renderers (e.g. `word.PackageProperties.Created = FixedTimestamp;`, and the spreadsheet's `PackageProperties.Created`). Verify the determinism test passes; if byte-identical OOXML is impractical, switch that test to compare extracted text (documented in Step 1's test file).

- [ ] **Step 8: Create the benchmark-tests project and wire references**

Add to `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` (new ItemGroup):

```xml
<ItemGroup>
  <PackageReference Include="UglyToad.PdfPig" />
  <PackageReference Include="DocumentFormat.OpenXml" />
  <ProjectReference Include="..\..\src\Ferret.Parsers.Office\Ferret.Parsers.Office.csproj" />
</ItemGroup>
```

Create the test project:

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
  </ItemGroup>
</Project>
```

```bash
dotnet sln src/Ferret.sln add tests/Ferret.Benchmarks.Tests/Ferret.Benchmarks.Tests.csproj
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.Benchmarks.Tests`
Expected: PASS (2 tests). If the determinism test fails only on `.docx`/`.pdf` bytes, apply the fixed-timestamp note or switch to text-equality as documented, then re-run.

- [ ] **Step 10: Commit**

```bash
git add tests/Ferret.Benchmarks tests/Ferret.Benchmarks.Tests src/Ferret.sln
git commit -m "feat(bench): add deterministic synthetic enterprise corpus generator with format renderers"
```

---

### Task 8: End-to-end integration test (index PDF + DOCX + XLSX + CSV, exclude opaque binaries)

**Files:**
- Modify: `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj` (reference `Ferret.Parsers`, `Ferret.Benchmarks`)
- Create: `tests/Ferret.Integration.Tests/ParserPackIndexingTests.cs`

**Interfaces:**
- Consumes: `SyntheticEnterpriseCorpusGenerator`, `ParserPackModule`, `IParserDispatcher`, `IIndexEngine`/`SqliteKeywordIndexEngine`, `IndexPipeline`, `FilesystemConnector`, the search service. Reuse the wiring pattern from `IndexPipelineBenchmark.cs:74-106`.

- [ ] **Step 1: Add project references**

In `tests/Ferret.Integration.Tests/Ferret.Integration.Tests.csproj`, add:

```xml
<ProjectReference Include="..\..\src\Ferret.Parsers\Ferret.Parsers.csproj" />
<ProjectReference Include="..\Ferret.Benchmarks\Ferret.Benchmarks.csproj" />
```

(Plus references to `Ferret.Indexing`, `Ferret.Connectors.Filesystem`, `Ferret.Search` if not already present — mirror `IndexPipelineBenchmark.cs` usings.)

- [ ] **Step 2: Write the integration test**

```csharp
// tests/Ferret.Integration.Tests/ParserPackIndexingTests.cs
using Ferret.Benchmarks.Corpus;
using Ferret.Core.Documents;
using Ferret.Parsers;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Integration.Tests;

public sealed class ParserPackIndexingTests
{
    [Fact]
    public async Task Pdf_Docx_Xlsx_Parsed_And_Opaque_Binaries_Excluded()
    {
        // 1. Generate a Small corpus.
        var root = Path.Join(Path.GetTempPath(), "pp-int-" + Guid.NewGuid().ToString("N"));
        new SyntheticEnterpriseCorpusGenerator(seed: 7).Generate(CorpusSize.Small, root);

        // 2. Drop a loose opaque binary into the tree (must NOT be indexed).
        await File.WriteAllBytesAsync(Path.Join(root, "SourceCode", "native.so"), [0x7F, 0x45, 0x4C, 0x46, 0x00, 0x01]);

        // 3. Resolve the full parser pack dispatcher.
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IParserDispatcher>();
        var resolver = (IMimeTypeResolver)new MimeTypeResolver();

        // Drop a CSV export into the tree (structure-aware CsvParser in the platform).
        var csvPath = Path.Join(root, "Mixed", "jira-export.csv");
        await File.WriteAllTextAsync(csvPath, "Key,Summary,Severity\nBUG-1,SSO login fails,High\n");

        // 4. Parse one PDF, DOCX, XLSX, and CSV directly through the dispatcher (full resolve path).
        var pdfPath = Directory.GetFiles(Path.Join(root, "PDF"), "*.pdf").OrderBy(p => p).First();
        var docxPath = Directory.GetFiles(Path.Join(root, "Word"), "*.docx").OrderBy(p => p).First();
        var xlsxPath = Directory.GetFiles(Path.Join(root, "Excel"), "*.xlsx").OrderBy(p => p).First();

        var pdfResult = await DispatchFile(dispatcher, resolver, pdfPath);
        var docxResult = await DispatchFile(dispatcher, resolver, docxPath);
        var xlsxResult = await DispatchFile(dispatcher, resolver, xlsxPath);
        var csvResult = await DispatchFile(dispatcher, resolver, csvPath);
        var soResult = await DispatchFile(dispatcher, resolver, Path.Join(root, "SourceCode", "native.so"));

        Assert.Equal(ParseResultKind.Success, pdfResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(pdfResult.Value!.PlainText));
        Assert.Equal(ParseResultKind.Success, docxResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(docxResult.Value!.PlainText));

        // XLSX: parsed as Data, and a header token from the enterprise archetype is searchable.
        Assert.Equal(ParseResultKind.Success, xlsxResult.Kind);
        Assert.Equal(DocumentKind.Data, xlsxResult.Value!.Kind);
        Assert.Contains("Priority", xlsxResult.Value!.PlainText, StringComparison.Ordinal);

        // CSV: structure-aware, Data kind, cell value searchable (CsvParser beats PlainTextParser).
        Assert.Equal(ParseResultKind.Success, csvResult.Kind);
        Assert.Equal(DocumentKind.Data, csvResult.Value!.Kind);
        Assert.Contains("SSO login fails", csvResult.Value!.PlainText, StringComparison.Ordinal);

        // Opaque binary: resolver yields application/octet-stream, dispatcher finds no parser.
        Assert.Equal(ParseResultKind.Unsupported, soResult.Kind);

        Directory.Delete(root, true);
    }

    private static async Task<ParseResult<Document>> DispatchFile(
        IParserDispatcher dispatcher, IMimeTypeResolver resolver, string path)
    {
        var name = Path.GetFileName(path);
        var mediaType = resolver.Resolve(name).MediaType;
        var asset = TestAsset.For(path, mediaType); // small helper building an AssetDescriptor from a path
        await using var fs = File.OpenRead(path);
        return await dispatcher.DispatchAsync(fs, asset);
    }
}
```

> Add a tiny `TestAsset.For(path, mediaType)` helper in the test project that builds an `AssetDescriptor` (mirror the fixture in `JsonParserTests`/`PdfParserTests`, using a `filesystem:///` URI from the file name). If a full `ferret index` end-to-end (pipeline + SQLite + search) is preferred over direct dispatch, mirror the wiring in `IndexPipelineBenchmark.cs:74-106`, swapping the single-parser registry for `ParserPackModule`, then assert a `search` for a known corpus word returns a `.pdf` and a `.docx` hit (Top-5). Keep whichever is green; the direct-dispatch version above is the minimal reliable assertion.

- [ ] **Step 3: Run the test**

Run: `dotnet test tests/Ferret.Integration.Tests --filter ParserPackIndexingTests`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add tests/Ferret.Integration.Tests
git commit -m "test(parsers): end-to-end PDF/DOCX/XLSX parsing and opaque-binary exclusion"
```

---

### Task 9: Performance metrics + documentation

**Files:**
- Create: `tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs`
- Modify: `tests/Ferret.Benchmarks/Benchmarks/IndexPipelineBenchmark.cs` (optionally source files from the generator)
- Create: `docs/benchmarks/parser-pack-1/README.md` (report skeleton)
- Modify: `README.md` (supported file types + parser packages)

**Interfaces:**
- Consumes: `SyntheticEnterpriseCorpusGenerator`, `ParserPackModule`, `IParserDispatcher`.

- [ ] **Step 1: Add a parser-throughput benchmark**

```csharp
// tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs
using BenchmarkDotNet.Attributes;

using Ferret.Benchmarks.Corpus;
using Ferret.Core.Documents;
using Ferret.Parsers;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>Measures parse throughput per document type (PDF, DOCX, XLSX, code, markdown) over a Small corpus.</summary>
[MemoryDiagnoser]
public class ParserThroughputBenchmark
{
    private string _root = string.Empty;
    private IParserDispatcher _dispatcher = null!;
    private IMimeTypeResolver _resolver = null!;

    [GlobalSetup]
    public void Setup()
    {
        _root = Path.Join(Path.GetTempPath(), "pp-bench-" + Guid.NewGuid().ToString("N"));
        new SyntheticEnterpriseCorpusGenerator(seed: 99).Generate(CorpusSize.Small, _root);

        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        _dispatcher = provider.GetRequiredService<IParserDispatcher>();
        _resolver = new MimeTypeResolver();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Benchmark]
    public async Task ParseAllPdfs()
    {
        foreach (var path in Directory.GetFiles(Path.Join(_root, "PDF"), "*.pdf"))
        {
            await ParseOne(path);
        }
    }

    [Benchmark]
    public async Task ParseAllDocx()
    {
        foreach (var path in Directory.GetFiles(Path.Join(_root, "Word"), "*.docx"))
        {
            await ParseOne(path);
        }
    }

    [Benchmark]
    public async Task ParseAllXlsx()
    {
        foreach (var path in Directory.GetFiles(Path.Join(_root, "Excel"), "*.xlsx"))
        {
            await ParseOne(path);
        }
    }

    private async Task ParseOne(string path)
    {
        var mediaType = _resolver.Resolve(Path.GetFileName(path)).MediaType;
        var asset = TestAsset.For(path, mediaType);
        await using var fs = File.OpenRead(path);
        await _dispatcher.DispatchAsync(fs, asset);
    }
}
```

> Reuse/define the same `TestAsset.For` helper used in Task 8 (place a shared copy in the benchmark project). The report records documents/sec and MB/sec per type, and parser-time vs index-time when run through the full pipeline benchmark. `[MemoryDiagnoser]` reports **Allocated** managed bytes per benchmark.

- [ ] **Step 1a: Add a large-workbook case with peak-memory capture**

Generate (or hand-build) a single `.xlsx` with a multi-thousand-row sheet and benchmark it separately to characterize the streaming reader on realistic enterprise export sizes. Because `[MemoryDiagnoser]` reports *allocations*, not resident memory, capture **peak working set** explicitly around the run:

```csharp
[Benchmark]
public async Task ParseLargeWorkbook()
{
    var proc = System.Diagnostics.Process.GetCurrentProcess();
    await ParseOne(_largeXlsxPath); // built in [GlobalSetup]: one sheet, ~50k rows
    proc.Refresh();
    _peakWorkingSetBytes = proc.PeakWorkingSet64; // recorded into the report's "Peak WS" column
}
```

Note in the report that `PeakWorkingSet64` is process-wide (not per-call); run the large-workbook benchmark in isolation for a clean reading.

- [ ] **Step 2: Create the report skeleton**

```markdown
<!-- docs/benchmarks/parser-pack-1/README.md -->
# Enterprise Content Pack 1 — Performance Report

## Objective
Measure indexing throughput and parse cost for PDF, DOCX, and XLSX vs text/code,
including a large-workbook (multi-thousand-row) XLSX case.

## Environment
(CPU, RAM, .NET version, corpus size)

## Methodology
Deterministic Small/Medium corpus via SyntheticEnterpriseCorpusGenerator (seed pinned).
Run: `dotnet run -c Release --project tests/Ferret.Benchmarks`

## Raw Measurements
| Type          | Docs/sec | MB/sec | Allocated | Peak WS | Parser time | Index time |
| ------------- | -------- | ------ | --------- | ------- | ----------- | ---------- |
| PDF           |          |        |           |         |             |            |
| DOCX          |          |        |           |         |             |            |
| XLSX          |          |        |           |         |             |            |
| XLSX (large)  |          |        |           |         |             |            |
| Code          |          |        |           |         |             |            |

> **Allocated** = `[MemoryDiagnoser]` managed bytes. **Peak WS** =
> `Process.PeakWorkingSet64` captured around the large-workbook run (see Step 1a) —
> this is resident memory, which is what "500 MB vs 5 GB" refers to; the diagnoser
> does not report it.

## Observations

## Future Optimization Opportunities
```

- [ ] **Step 3: Update README supported file types**

In `README.md`, add a "Supported file types" section listing: source code & text/config (via PlainText/Markdown/JSON), **PDF** (`Ferret.Parsers.Pdf`), **Word .docx** and **Excel .xlsx** (`Ferret.Parsers.Office`), composed via `Ferret.Parsers`. Document the configurable `Ferret:Parsers:MaxExtractedCharacters` setting (default unlimited). Mention `ferret doctor` shows installed parsers and the supported-extension count.

- [ ] **Step 4: Build the benchmark project (compile-only verification)**

Run: `dotnet build tests/Ferret.Benchmarks -c Release`
Expected: build succeeds. (Full benchmark execution is run on demand, not in CI.)

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs docs/benchmarks/parser-pack-1/README.md README.md
git commit -m "feat(bench): add parser throughput benchmark and Enterprise Content Pack 1 docs"
```

---

## Self-Review

**Spec coverage:**
- Expanded text/code/config MIME mappings + DocumentKind → Task 2 ✅
- Expanded binary denylist → Task 2 ✅
- **`CsvParser` (structure-aware CSV/TSV, in ParserPlatform, no new package)** → Task 2b ✅
- `Ferret.Parsers.Pdf` (PdfPig) → Task 3 ✅
- `Ferret.Parsers.Office` (**DOCX + XLSX**) → Task 4 ✅
- Additive MimeTypeResolver / PDF+DOCX+**XLSX** dedicated media types (XLSX → `Data`) → Task 2 ✅
- Parseable-binary distinct from opaque (`MediaCategory`) → Task 1 + Task 2 ✅
- **Excel streaming reader + shared strings + cached values (formula expression never emitted)** → Task 4 (Step E4) ✅
- **Uniform extracted-text limit across PDF/Word/Excel** via shared `ExtractionLimiter` (default unlimited) → Task 1 (Steps 8–9) + Tasks 3/4 ✅
- **`DocumentMetadata` key constants (no string drift)** → Task 1 (Step 7), used by all parsers ✅
- **Reserved `ParserCapabilities.StructuredExtraction` (unused, future-proof)** → Task 1 (Step 10) ✅
- `ParserPackModule` composition (**7 parsers**) → Task 5 ✅
- Parser principle (text + metadata only, no calc/formula) → Global Constraints + Tasks 2b/3/4 ✅
- Lightweight metadata schema (via constants) → Tasks 3 (PDF) + 4 (DOCX/XLSX) ✅
- `GetServices<IContentParser>()` aggregation (registry untouched) → Task 5 test ✅
- **Dispatcher (public API) routing verified** → Task 5 (in-memory) + Task 8 (PDF/DOCX/XLSX files) ✅
- `ferret doctor` parser introspection (**7 parsers**) → Task 6 ✅
- Corpus generator with **`CorpusTable`** + **XLSX renderer** + **9 enterprise tabular archetypes** + **enterprise-like titles** → Task 7 ✅
- **Metadata-search-ready schema; indexed-doc-count & parsing-telemetry reserved** → spec (doctor section) ✅
- Unit tests → Tasks 1–7; end-to-end integration test incl. **XLSX cell search** → Task 8; performance report incl. **large-workbook XLSX + peak working set** → Task 9; docs → Task 9 ✅
- Acceptance criteria (PDF/DOCX/XLSX/CSV searchable, Jira-export cell retrievable, opaque excluded, `Data` kind, config limit, 7 parsers) → Tasks 4/6/8 ✅
- **Sprint map + parallel subagent execution model** → Sprint Map section ✅
- DocumentKind evolution / PowerPoint fast-follow → documented in spec; no code ✅
- Reserved Enterprise Content Pack 2 (PPTX/Outlook/Visio/RTF/ODT/ODS/HTML/XML) → spec only, no task ✅

**Placeholder scan:** No "TBD"/"handle edge cases" left; failure handling is concrete (throw → dispatcher `Failed`; empty text → `Empty`). Package versions are **pinned** (PdfPig `1.7.0-custom-5` — see version-deviation note — OpenXml 3.1.0) — no "verify latest" hedges. The one genuinely environment-dependent item (OOXML byte-determinism for `.docx`/`.xlsx`) carries an explicit fallback (compare extracted text).

**Type consistency:** `MediaCategory`, `DocumentMetadata`, `ParserOptions`, `ExtractionLimiter` (all Task 1) consumed identically across Tasks 2/2b/3/4/6. All four heavyweight/structured parsers (Csv, Pdf, Word, Excel) take `ParserOptions` and call `ExtractionLimiter.ApplyCharacterLimit`; `ParserPlatformModule` (Csv), `PdfParserModule`, and `OfficeParserModule` each `TryAddSingleton(new ParserOptions())` (idempotent — `TryAdd` means the first registration wins, so composing them in `ParserPackModule` is safe). `OfficeMediaTypes.Docx`/`.Xlsx` (Task 4) reused in Task 5 test. `CorpusDocument(Title, Blocks, Tables)` and `CorpusTable(Headers, Rows)` (Task 7) consumed by `DocxRenderer`/`XlsxRenderer`/`EnterpriseArchetypes`/generator consistently. `SyntheticEnterpriseCorpusGenerator(int seed).Generate(CorpusSize, string)` consistent across Tasks 7/8/9. `TestAsset.For(path, mediaType)` referenced in Tasks 8/9 (define once per consuming project).
