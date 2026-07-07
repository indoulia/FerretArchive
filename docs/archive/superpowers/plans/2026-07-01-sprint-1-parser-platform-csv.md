# Sprint 1 — Parser Platform & CSV Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the first slice of Enterprise Content Pack 1 — core parser primitives, a one-and-done MimeTypeResolver expansion (including filename resolution for `Dockerfile`/`Makefile`), a dependency-free CSV/TSV parser, and end-to-end proof that `ferret index` → `ferret search` finds enterprise CSV exports.

**Architecture:** All work lands in two existing projects: `Ferret.Core` (content primitives) and `Ferret.ParserPlatform` (resolver + CSV parser, registered through the already-wired `ParserPlatformModule`). No new packages, no new `.csproj`, no CLI wiring changes. The parser registry already aggregates every `IContentParser` via `GetServices<IContentParser>()`, so CSV becomes live at `ferret index` the moment it is registered. E2E tests live in the existing `Ferret.E2E.Tests` project.

**Tech Stack:** .NET 9, C#, xUnit, Microsoft.Extensions.DependencyInjection.

**Spec:** `docs/superpowers/specs/2026-07-01-sprint-1-parser-platform-csv-design.md`
**Parent plan (source of reused code):** `docs/superpowers/plans/2026-07-01-parser-pack-1.md`

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props` — do NOT set `<TargetFramework>` in any csproj.
- **Central Package Management:** no `Version` attribute on `<PackageReference>`; versions live in `Directory.Packages.props`. (Sprint 1 adds no new packages.)
- **Parsers MUST be `sealed`.** `CanParse` is pure: no I/O, never throws, deterministic.
- **Parser responsibility:** extract text + lightweight metadata from the stream only. No chunking, tokenization, embedding, summarization, or AI processing.
- **Shared extraction limit:** the CSV parser takes `ParserOptions` and applies the shared `ExtractionLimiter.ApplyCharacterLimit` (default `null` = unlimited); when exceeded, truncate `PlainText` and set `Metadata[DocumentMetadata.Truncated]="true"`.
- **Metadata keys are `DocumentMetadata.*` constants**, never raw strings.
- **Stream ownership:** parsers MUST NOT dispose/close the content stream (`leaveOpen: true`).
- **Failure signaling:** a parser signals failure by throwing; `ParserDispatcher` converts to `Failed`. Empty/whitespace `PlainText` becomes `Empty`. `OperationCanceledException` must propagate.
- **Backward compatibility:** no breaking changes to existing indexes, parser contracts, CLI behavior, or public APIs. Existing text/markdown/JSON indexing unchanged.
- **StyleCop:** public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | Core parser foundation (primitives + limiter) | `Ferret.Core` |
| 2 | MimeTypeResolver expansion + filename resolution | `Ferret.ParserPlatform` |
| 3 | CSV/TSV parser | `Ferret.ParserPlatform` |
| 4 | End-to-end CSV indexing validation | `Ferret.E2E.Tests` |

Task 1 is a hard barrier (Tasks 2 and 3 consume its types). Task 2 and Task 3 both depend on Task 1; Task 3 also depends on Task 2 only for the `.csv`/`.tsv` classification already present today (so it can run right after Task 1 if needed). Task 4 depends on Task 3 being registered.

---

### Task 1: Core parser foundation (Ferret.Core)

**Files:**
- Create: `src/Ferret.Core/Documents/MediaCategory.cs`
- Modify: `src/Ferret.Core/Documents/MediaTypeInfo.cs`
- Create: `src/Ferret.Core/Documents/DocumentMetadata.cs`
- Create: `src/Ferret.Core/Documents/ParserOptions.cs`
- Create: `src/Ferret.Core/Documents/ExtractionLimiter.cs`
- Modify: `src/Ferret.Core/Documents/ParserCapabilities.cs`
- Test: `tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs`
- Test: `tests/Ferret.Core.Tests/Documents/ExtractionLimiterTests.cs`

**Interfaces:**
- Produces: `enum MediaCategory { Text, BinaryParseable, BinaryOpaque }`; `MediaTypeInfo.Category` (required init); computed `IsText`/`IsBinary`.
- Produces: `static class DocumentMetadata` with `const string` keys `Author, Subject, Keywords, PageCount, SheetCount, Created, Modified, Category, Truncated`.
- Produces: `sealed record ParserOptions { long? MaxExtractedCharacters { get; init; } }`.
- Produces: `static class ExtractionLimiter { (string Text, bool Truncated) ApplyCharacterLimit(string text, ParserOptions options); }`.
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

- [ ] **Step 6: Build the solution to surface consumers of the removed `IsText`/`IsBinary` setters**

Run: `dotnet build src/Ferret.sln`
Expected: FAIL only in `MimeTypeResolver.cs` (it sets `IsText`/`IsBinary` directly). That file is fixed in Task 2. If Task 1 is being committed independently, temporarily set `Category` in the resolver helpers to keep the build green — Task 2 rewrites them anyway. Do not touch any other file.

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

    /// <summary>Number of data rows (tabular formats; excludes the header row).</summary>
    public const string RowCount = "RowCount";

    /// <summary>Number of columns (tabular formats; header field count).</summary>
    public const string ColumnCount = "ColumnCount";

    /// <summary>Set to "true"/"false": whether a tabular document's first row is treated as a header.</summary>
    public const string HasHeader = "HasHeader";
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

    [Fact]
    public void Limit_Larger_Than_Int_MaxValue_Does_Not_Truncate_Or_Overflow()
    {
        var options = new ParserOptions { MaxExtractedCharacters = (long)int.MaxValue + 1000 };
        var (text, truncated) = ExtractionLimiter.ApplyCharacterLimit("hello world", options);
        Assert.Equal("hello world", text);
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
            // max < text.Length (an int) here, so it fits int; Math.Min guards against any future reordering.
            var limit = (int)Math.Min(max, text.Length);
            return (text[..limit], true);
        }

        return (text, false);
    }
}
```

Run again → PASS.

- [ ] **Step 10: Reserve the `StructuredExtraction` capability**

In `src/Ferret.Core/Documents/ParserCapabilities.cs`, add alongside the existing members (leave `All` and existing members intact):

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

Do not add it to any parser's `Capabilities` list this milestone.

- [ ] **Step 11: Commit**

```bash
git add src/Ferret.Core/Documents/MediaCategory.cs src/Ferret.Core/Documents/MediaTypeInfo.cs src/Ferret.Core/Documents/DocumentMetadata.cs src/Ferret.Core/Documents/ParserOptions.cs src/Ferret.Core/Documents/ExtractionLimiter.cs src/Ferret.Core/Documents/ParserCapabilities.cs tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs tests/Ferret.Core.Tests/Documents/ExtractionLimiterTests.cs
git commit -m "feat(core): add MediaCategory, DocumentMetadata, ParserOptions, ExtractionLimiter, reserved StructuredExtraction"
```

---

### Task 2: MimeTypeResolver expansion + filename resolution (Ferret.ParserPlatform)

**Files:**
- Modify: `src/Ferret.ParserPlatform/MimeTypeResolver.cs`
- Test: `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs` (create if absent)

**Interfaces:**
- Consumes: `MediaCategory`, `MediaTypeInfo.Category` (Task 1).
- Produces: resolver emits `application/pdf` (`BinaryParseable`, `Prose`) for `.pdf`; the OpenXML wordprocessing media type (`BinaryParseable`, `Prose`) for `.docx`; the spreadsheet media type (`BinaryParseable`, `Data`) for `.xlsx`; expanded text/code/config mappings; expanded `BinaryOpaque` denylist; and a **filename map** (`Dockerfile`, `Makefile`) consulted after the extension lookup misses.

> **Two commits.** This task lands as **2a** (pure `MediaTypeInfo`/`Category`
> migration — pairs with Task 1's breaking change, no behavior change) then
> **2b** (MIME expansion + filename resolution, TDD). Splitting keeps review and
> rollback clean.

#### Commit 2a — migrate the resolver to `Category` (refactor, no behavior change)

- [ ] **Step 1: Migrate the existing `Text`/`Binary` helpers and `UnknownText` to set `Category`**

The current file sets the now-removed `IsText`/`IsBinary` init properties, so the
solution fails to build after Task 1. Fix it by switching to `Category`. Replace
the two existing helpers:

```csharp
private static MediaTypeInfo Text(string mediaType, DocumentKind kind) => new()
{
    MediaType = mediaType,
    Category = MediaCategory.Text,
    SuggestedKind = kind,
    Confidence = 1.0,
};

private static MediaTypeInfo Binary() => new()
{
    MediaType = "application/octet-stream",
    Category = MediaCategory.BinaryOpaque,
    Confidence = 1.0,
};
```

Replace the `UnknownText` field:

```csharp
private static readonly MediaTypeInfo UnknownText = new()
{
    MediaType = "text/plain",
    Category = MediaCategory.Text,
    Confidence = 0.5,
};
```

Do not change the `Map` entries or `Resolve()` yet — this commit is a pure
type migration.

- [ ] **Step 2: Build and run existing suites to confirm the refactor is behavior-neutral**

Run: `dotnet build src/Ferret.sln && dotnet test tests/Ferret.ParserPlatform.Tests && dotnet test tests/Ferret.Core.Tests`
Expected: build succeeds (the Task-1 setter break is resolved), all existing tests PASS. No new tests yet.

- [ ] **Step 3: Commit 2a**

```bash
git add src/Ferret.ParserPlatform/MimeTypeResolver.cs
git commit -m "refactor(parsers): migrate MimeTypeResolver to MediaCategory"
```

#### Commit 2b — MIME expansion + filename resolution (TDD)

- [ ] **Step 4: Write the failing tests**

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

    [Theory]
    [InlineData("Dockerfile")]
    [InlineData("Makefile")]
    public void Extensionless_Build_Files_Resolve_By_Name(string fileName)
    {
        var info = Resolver.Resolve(fileName);
        Assert.Equal(MediaCategory.Text, info.Category);
        Assert.Equal(DocumentKind.Config, info.SuggestedKind);
        Assert.Equal(1.0, info.Confidence);
    }

    [Fact]
    public void FileName_Lookup_Is_Case_Insensitive()
    {
        Assert.Equal(MediaCategory.Text, Resolver.Resolve("dockerfile").Category);
    }

    [Fact]
    public void FileName_Lookup_Uses_Base_Name_From_Path()
    {
        var info = Resolver.Resolve("/repo/build/Makefile");
        Assert.Equal(DocumentKind.Config, info.SuggestedKind);
    }

    [Fact]
    public void Known_Extension_Wins_Over_FileName_And_Unknown_Falls_Back_To_Text()
    {
        Assert.Equal("text/markdown", Resolver.Resolve("README.md").MediaType);
        var unknown = Resolver.Resolve("mystery.zzz");
        Assert.Equal("text/plain", unknown.MediaType);
        Assert.Equal(0.5, unknown.Confidence);
    }

    // Regression snapshot: a representative set of mappings across every category.
    // Guards the central resolver against accidental drift when entries are added later.
    [Theory]
    [InlineData("Program.cs", "text/x-csharp", MediaCategory.Text)]
    [InlineData("README.md", "text/markdown", MediaCategory.Text)]
    [InlineData("data.json", "application/json", MediaCategory.Text)]
    [InlineData("config.xml", "text/xml", MediaCategory.Text)]
    [InlineData("index.html", "text/html", MediaCategory.Text)]
    [InlineData("report.pdf", "application/pdf", MediaCategory.BinaryParseable)]
    [InlineData("spec.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", MediaCategory.BinaryParseable)]
    [InlineData("export.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", MediaCategory.BinaryParseable)]
    [InlineData("archive.zip", "application/octet-stream", MediaCategory.BinaryOpaque)]
    [InlineData("tool.exe", "application/octet-stream", MediaCategory.BinaryOpaque)]
    [InlineData("Dockerfile", "text/x-dockerfile", MediaCategory.Text)]
    [InlineData("Makefile", "text/x-makefile", MediaCategory.Text)]
    public void Representative_Mappings_Are_Stable(string fileName, string expectedMediaType, MediaCategory expectedCategory)
    {
        var info = Resolver.Resolve(fileName);
        Assert.Equal(expectedMediaType, info.MediaType);
        Assert.Equal(expectedCategory, info.Category);
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter MimeTypeResolverTests`
Expected: FAIL — `.pdf` currently resolves to `application/octet-stream`; `Dockerfile` returns the `text/plain` fallback (Confidence 0.5), not Config.

- [ ] **Step 6: Add the `ParseableBinary` helper, reclassify `.pdf`/`.docx`/`.xlsx`, and add the expanded entries**

Add the new helper alongside the migrated `Text`/`Binary` helpers:

```csharp
private static MediaTypeInfo ParseableBinary(string mediaType, DocumentKind kind) => new()
{
    MediaType = mediaType,
    Category = MediaCategory.BinaryParseable,
    SuggestedKind = kind,
    Confidence = 1.0,
};
```

In the `Map` dictionary, replace the three existing `Binary()` entries for `.pdf`/`.docx`/`.xlsx` with:

```csharp
[".pdf"] = ParseableBinary("application/pdf", DocumentKind.Prose),
[".docx"] = ParseableBinary("application/vnd.openxmlformats-officedocument.wordprocessingml.document", DocumentKind.Prose),
[".xlsx"] = ParseableBinary("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", DocumentKind.Data),
```

(`.pptx` stays `Binary()` — opaque, deferred.) Add these new text/code/config entries:

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

Add the expanded binary denylist (not already mapped):

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

- [ ] **Step 7: Add the filename map**

Add a second static dictionary next to `Map`, keyed on the full file name (extensionless build files are first-class and the map is trivially extensible):

```csharp
private static readonly Dictionary<string, MediaTypeInfo> FileNameMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dockerfile"] = Text("text/x-dockerfile", DocumentKind.Config),
        ["Makefile"] = Text("text/x-makefile", DocumentKind.Config),
    };
```

- [ ] **Step 8: Rewrite `Resolve()` to fall back to the filename map**

Replace the body of `Resolve`. Order: **extension lookup → filename lookup → `UnknownText`.** A present, known extension always wins; the filename map is consulted only when the extension lookup misses; `Path.GetFileName` strips any directory so full paths resolve.

```csharp
/// <inheritdoc/>
public MediaTypeInfo Resolve(string fileName)
{
    ArgumentNullException.ThrowIfNull(fileName);

    if (fileName.Length == 0)
    {
        return UnknownText;
    }

    var ext = Path.GetExtension(fileName);
    if (!string.IsNullOrEmpty(ext) && ext != "." && Map.TryGetValue(ext, out var byExtension))
    {
        return byExtension;
    }

    var name = Path.GetFileName(fileName);
    if (name.Length > 0 && FileNameMap.TryGetValue(name, out var byName))
    {
        return byName;
    }

    return UnknownText;
}
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter MimeTypeResolverTests`
Expected: PASS.

- [ ] **Step 10: Run the full ParserPlatform + Core suites and build the solution to confirm no regressions**

Run: `dotnet build src/Ferret.sln && dotnet test tests/Ferret.ParserPlatform.Tests && dotnet test tests/Ferret.Core.Tests`
Expected: build succeeds, all tests PASS.

- [ ] **Step 11: Commit 2b**

```bash
git add src/Ferret.ParserPlatform/MimeTypeResolver.cs tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs
git commit -m "feat(parsers): expand MIME map, reclassify PDF/DOCX/XLSX as parseable-binary, add filename resolution for Dockerfile/Makefile"
```

---

### Task 3: CsvParser (Ferret.ParserPlatform)

**Files:**
- Create: `src/Ferret.ParserPlatform/Parsers/CsvRecordReader.cs`
- Create: `src/Ferret.ParserPlatform/Parsers/CsvParser.cs`
- Modify: `src/Ferret.ParserPlatform/ParserPlatformModule.cs`
- Test: `tests/Ferret.ParserPlatform.Tests/Parsers/CsvParserTests.cs`

**Interfaces:**
- Consumes: `ParserOptions`, `ExtractionLimiter`, `DocumentMetadata` (Task 1); `IContentParser`, `ParserDescriptor`, `ParserId`, `ParseContext`, `Document`, `DocumentId`, `DocumentKind`, `ParserCapabilities` (Ferret.Core).
- Produces: `public sealed class CsvParser : IContentParser` (ctor takes `ParserOptions`; `CanParse` matches `text/csv` + `text/tab-separated-values`; priority 200); `internal static class CsvRecordReader { IEnumerable<IReadOnlyList<string>> ReadRecords(string text, char delimiter); }`.

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

    [Fact]
    public async Task ParseAsync_Tsv_Splits_On_Tab()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key\tSummary\nBUG-3\tCrash on save\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/tab-separated-values")));

        Assert.Contains("Crash on save", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Does_Not_Dispose_Stream()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("a,b\n1,2\n");

        await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.True(stream.CanRead); // not disposed
    }

    [Fact]
    public async Task ParseAsync_Populates_Row_Column_And_Header_Metadata()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream("Key,Summary,Severity\nBUG-1,Login fails,High\nBUG-2,Crash,Low\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Equal("2", doc.Metadata[DocumentMetadata.RowCount]);      // data rows, excludes header
        Assert.Equal("3", doc.Metadata[DocumentMetadata.ColumnCount]);   // header field count
        Assert.Equal("true", doc.Metadata[DocumentMetadata.HasHeader]);
    }

    [Fact]
    public async Task ParseAsync_BlankFile_YieldsEmptyText_And_NoHeader()
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream(string.Empty);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Equal(string.Empty, doc.PlainText);
        Assert.Equal("false", doc.Metadata[DocumentMetadata.HasHeader]);
        Assert.Equal("0", doc.Metadata[DocumentMetadata.RowCount]);
    }

    [Theory]
    [InlineData("Key,Summary\nBUG-1,\n")]                        // empty trailing column
    [InlineData("Key,Summary,\nBUG-1,Login fails,\n")]           // trailing comma / empty header col
    [InlineData("Key,Summary\n\nBUG-1,Login fails\n")]           // empty row in the middle
    [InlineData("Key,Summary\nBUG-1,\"unterminated quote\n")]    // unmatched quote — must not throw
    public async Task ParseAsync_MalformedInput_DoesNotThrow(string content)
    {
        var parser = new CsvParser(new ParserOptions());
        using var stream = MakeStream(content);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.NotNull(doc); // parser is total over messy enterprise exports
        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Honors_Utf8_Bom()
    {
        var parser = new CsvParser(new ParserOptions());
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes("Key,Summary\nBUG-1,Café crash\n"))
            .ToArray();
        using var stream = new MemoryStream(bytes);

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains("Café crash", doc.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("﻿", doc.PlainText); // BOM stripped by the reader, not indexed
    }

    [Fact]
    public async Task ParseAsync_VeryLongCell_IsPreserved()
    {
        var parser = new CsvParser(new ParserOptions());
        var longCell = new string('x', 100_000);
        using var stream = MakeStream($"Key,Notes\nBUG-1,{longCell}\n");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Contains(longCell, doc.PlainText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task ParseAsync_ScalesLinearly_WithoutExhaustingMemory(int rows)
    {
        var parser = new CsvParser(new ParserOptions());
        var sb = new StringBuilder("Key,Summary,Severity\n");
        for (var i = 0; i < rows; i++)
        {
            sb.Append("BUG-").Append(i).Append(",Issue ").Append(i).Append(",High\n");
        }

        using var stream = MakeStream(sb.ToString());

        // Bounded working set: the reader streams records; assert the parse completes,
        // reports the exact row count, and never throws OutOfMemoryException.
        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("text/csv")));

        Assert.Equal(rows.ToString(System.Globalization.CultureInfo.InvariantCulture), doc.Metadata[DocumentMetadata.RowCount]);
        Assert.Contains("BUG-" + (rows - 1), doc.PlainText, StringComparison.Ordinal);
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
```

- [ ] **Step 5: Register `CsvParser` in `ParserPlatformModule`**

In `src/Ferret.ParserPlatform/ParserPlatformModule.cs`, add the `using` and register the parser plus a default `ParserOptions` (so the ctor resolves), alongside the existing built-in registrations:

```csharp
using Microsoft.Extensions.DependencyInjection.Extensions; // for TryAddSingleton
// ...
services.TryAddSingleton(new ParserOptions()); // unlimited default; host may override before wiring
services.AddSingleton<IContentParser, CsvParser>();
```

The registry factory already aggregates via `GetServices<IContentParser>()`, so no registry change.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.ParserPlatform.Tests --filter CsvParserTests`
Expected: all CSV tests PASS (correctness, metadata, malformed-input, BOM, long-cell, and the 100/1k/10k/100k scale theory). CSV/TSV are now live through the already-wired `ParserPlatformModule` — no CLI change needed.

- [ ] **Step 7: Commit**

```bash
git add src/Ferret.ParserPlatform/Parsers/CsvRecordReader.cs src/Ferret.ParserPlatform/Parsers/CsvParser.cs src/Ferret.ParserPlatform/ParserPlatformModule.cs tests/Ferret.ParserPlatform.Tests/Parsers/CsvParserTests.cs
git commit -m "feat(parsers): add structure-aware CsvParser for CSV/TSV enterprise exports"
```

---

### Task 4: End-to-end CSV indexing validation (Ferret.E2E.Tests)

**Files:**
- Modify: `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs` (add `WriteEnterpriseCsvFilesAsync`)
- Create: `tests/Ferret.E2E.Tests/Tests/CsvIndexE2ETests.cs`

**Interfaces:**
- Consumes: `WorkspaceFixture.InitializeAsync()`, `WorkspaceFixture.RunAsync(string args, TimeSpan? timeout = null)` returning `(int ExitCode, string Stdout, string Stderr)`, `WorkspaceFixture.WorkspaceDir` (existing).
- Produces: `WorkspaceFixture.WriteEnterpriseCsvFilesAsync()` writing realistic Jira-style CSV + TSV exports into `WorkspaceDir`.

- [ ] **Step 1: Add a realistic enterprise CSV/TSV fixture writer to `WorkspaceFixture`**

Add this method to `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs` (mirrors the existing `WriteSampleCsFilesAsync`). It writes a Jira-style issue export (`issues.csv`) — including a quoted summary with an embedded comma — and an Azure DevOps-style TSV (`workitems.tsv`):

```csharp
/// <summary>Writes realistic enterprise CSV/TSV exports (Jira / Azure DevOps style) into the workspace.</summary>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public async Task WriteEnterpriseCsvFilesAsync()
{
    const string issuesCsv =
        "Key,Summary,Severity,Status,Assignee,Sprint\n" +
        "PROJ-101,Login fails for SSO users,High,Open,Dana Wells,Sprint 14\n" +
        "PROJ-102,\"Timeout on export, then crash\",Critical,In Progress,Rahul Menon,Sprint 14\n" +
        "PROJ-103,Add audit log retention policy,Medium,Done,Dana Wells,Sprint 13\n";

    await File.WriteAllTextAsync(
        Path.Join(WorkspaceDir, "issues.csv"),
        issuesCsv).ConfigureAwait(false);

    const string workItemsTsv =
        "ID\tTitle\tState\tAssignedTo\tIteration\n" +
        "5001\tAuthentication token refresh\tActive\tPriya Nair\tSprint 14\n" +
        "5002\tCustomer risk register review\tClosed\tOmar Said\tSprint 13\n";

    await File.WriteAllTextAsync(
        Path.Join(WorkspaceDir, "workitems.tsv"),
        workItemsTsv).ConfigureAwait(false);
}
```

- [ ] **Step 2: Write the failing E2E tests**

```csharp
// tests/Ferret.E2E.Tests/Tests/CsvIndexE2ETests.cs
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E: index enterprise CSV/TSV exports, then prove the rows are searchable.</summary>
public sealed class CsvIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteEnterpriseCsvFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search after indexing CSV returns exit code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_AfterCsvIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search authentication");

        Assert.Equal(0, exitCode);
    }

    /// <summary>An issue key from the CSV is searchable and points at issues.csv.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_IssueKey_ReturnsIssuesCsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search PROJ-101");

        Assert.Contains("issues.csv", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A quoted-field value (embedded comma) is indexed as a single searchable cell.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_QuotedFieldValue_ReturnsIssuesCsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search timeout");

        Assert.Contains("issues.csv", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A column value (assignee) from the CSV is searchable.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_Assignee_ReturnsIssuesCsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search Dana");

        Assert.Contains("issues.csv", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A TSV work-item title is searchable and points at workitems.tsv.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_TsvTitle_ReturnsWorkItemsTsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search risk");

        Assert.Contains("workitems.tsv", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 3: Run the E2E tests to verify they pass**

Run: `dotnet test tests/Ferret.E2E.Tests --filter CsvIndexE2ETests`
Expected: PASS (5 tests). If `Search_IssueKey_ReturnsIssuesCsv` fails on tokenization of `PROJ-101`, confirm the query tokenizer splits on `-`; if it treats the hyphenated key as one token, assert on `search PROJ` instead (a value guaranteed present) — but attempt the exact key first, since the CSV parser emits the raw cell text.

- [ ] **Step 4: Run the full E2E suite to confirm no regression in existing index/search tests**

Run: `dotnet test tests/Ferret.E2E.Tests`
Expected: PASS (existing `IndexE2ETests` / `SearchE2ETests` unaffected — the new fixture method is additive).

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs tests/Ferret.E2E.Tests/Tests/CsvIndexE2ETests.cs
git commit -m "test(e2e): validate CSV/TSV enterprise exports index and search end-to-end"
```

---

## Final verification

- [ ] **Full solution build + test**

Run: `dotnet build src/Ferret.sln && dotnet test src/Ferret.sln`
Expected: build clean, all tests green.

- [ ] **Acceptance criteria check** (from the spec)

Confirm each: existing text/markdown/JSON indexing unchanged · CSV searchable e2e · TSV searchable e2e · `Dockerfile` resolved · `Makefile` resolved · `.pdf`/`.docx`/`.xlsx` classified `BinaryParseable` · expanded binary denylist blocks opaque files · 100% regression tests green · no new NuGet deps · no CLI changes · existing indexes compatible.

## Definition of Done

Sprint 1 is done when every box below is checked:

- [ ] All unit tests pass (`Ferret.Core.Tests`, `Ferret.ParserPlatform.Tests`)
- [ ] All E2E tests pass (`Ferret.E2E.Tests`, incl. `CsvIndexE2ETests`)
- [ ] Existing parser/index/search tests unchanged and green (no regressions)
- [ ] No public API regressions (existing consumers compile unchanged except the intended `MediaTypeInfo.Category` migration)
- [ ] No additional NuGet dependencies (`Directory.Packages.props` unchanged)
- [ ] No analyzer / StyleCop warnings introduced (`dotnet build` clean)
- [ ] Build + tests pass on Windows and Linux (CI matrix)
- [ ] Five commits landed: Task 1 · Task 2a (`Category` refactor) · Task 2b (MIME + filename) · Task 3 (CSV) · Task 4 (E2E)

> **Commit count note:** Task 2 intentionally lands as two commits (2a refactor,
> 2b feature), so Sprint 1 is **five** commits, not four.
