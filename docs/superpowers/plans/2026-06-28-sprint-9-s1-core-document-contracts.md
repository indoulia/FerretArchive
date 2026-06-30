# Sprint 9 — Section 1: Core Document Contracts

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Extend `Ferret.Core` with two new namespaces — `Ferret.Core.Documents` and `Ferret.Core.Indexing` — and add indexing lifecycle events to `Ferret.Core.Events`. These are non-breaking M1 additions. No existing type changes. Every downstream section (S2 Parser Platform, S3 Index Engine, S4 Connector Config CLI, S5 Wire-up) depends on this section.

**Architecture:** `Document` is the canonical parsing output — the parallel to `AssetDescriptor` for the Connector Platform. `IIndexPipeline` is the orchestration boundary; CLI handlers never touch `IIndexEngine` directly. Parser dispatch is by `MediaType`, not file extension. Indexing lifecycle events flow through the existing `Ferret.Events` bus.

**ADR:** `docs/adr/0014-document-processing-architecture.md` — written as Task 7 in this section.

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `required` on record properties with no sensible default, `sealed` on all concrete classes.

---

## Prerequisites

Sprint 8 must be **complete** before starting this section:
- `Ferret.ConnectorPlatform`, `Ferret.Connectors.Filesystem` merged and green
- `ferret connector list` and `ferret connector info` working
- `dotnet test` passes on `master`
- Tag `v0.8.0-sprint8` applied

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes
- `required` keyword on record/class properties with no sensible default
- No breaking changes to existing `Ferret.Core.*` types
- `Primitives.DocumentId` already exists — extend it; do not create a conflicting `Documents.DocumentId`
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-9):`, `test(sprint-9):`, `chore(sprint-9):`
- **No intermediate commit until all Sprint 9 sections are complete** — accumulate changes, single commit at sprint end

---

## File Inventory

### New Source Files

| File | Namespace |
|---|---|
| `src/Ferret.Core/Documents/ParserId.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/DocumentKind.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/DocumentSection.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/Document.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParseContext.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/MediaTypeInfo.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/IMimeTypeResolver.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParseDiagnostic.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParseResultKind.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParseResult.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParserCapability.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParserCapabilities.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/ParserDescriptor.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/IContentParser.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/IParserRegistry.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/IParserDispatcher.cs` | `Ferret.Core.Documents` |
| `src/Ferret.Core/Documents/IContentNormalizer.cs` | `Ferret.Core.Documents` (reserved stub) |
| `src/Ferret.Core/Indexing/IndexResult.cs` | `Ferret.Core.Indexing` |
| `src/Ferret.Core/Indexing/IndexStats.cs` | `Ferret.Core.Indexing` |
| `src/Ferret.Core/Indexing/IndexPipelineOptions.cs` | `Ferret.Core.Indexing` |
| `src/Ferret.Core/Indexing/IIndexEngine.cs` | `Ferret.Core.Indexing` |
| `src/Ferret.Core/Indexing/IIndexPipeline.cs` | `Ferret.Core.Indexing` |
| `src/Ferret.Core/Events/Indexing/IndexingStartedEvent.cs` | `Ferret.Core.Events.Indexing` |
| `src/Ferret.Core/Events/Indexing/DocumentParsedEvent.cs` | `Ferret.Core.Events.Indexing` |
| `src/Ferret.Core/Events/Indexing/DocumentIndexedEvent.cs` | `Ferret.Core.Events.Indexing` |
| `src/Ferret.Core/Events/Indexing/DocumentSkippedEvent.cs` | `Ferret.Core.Events.Indexing` |
| `src/Ferret.Core/Events/Indexing/DocumentParsingFailedEvent.cs` | `Ferret.Core.Events.Indexing` |
| `src/Ferret.Core/Events/Indexing/IndexingCompletedEvent.cs` | `Ferret.Core.Events.Indexing` |
| `src/Ferret.Core/Events/Indexing/IndexingFailedEvent.cs` | `Ferret.Core.Events.Indexing` |

### Modified Source Files

| File | Change |
|---|---|
| `src/Ferret.Core/Primitives/DocumentId.cs` | Add `From(AssetId)` static factory method |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.Core.Tests/Documents/DocumentTypedIdTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Documents/DocumentTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Documents/ParseResultTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Documents/ParserCapabilityTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Documents/ParserDescriptorTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Indexing/IndexResultTests.cs` | Ferret.Core.Tests |

### New Doc Files

| File |
|---|
| `docs/adr/0014-document-processing-architecture.md` |

---

## Task 1: Typed IDs + DocumentKind + DocumentSection

**Why first:** Every subsequent type in `Ferret.Core.Documents` and `Ferret.Core.Indexing` depends on these primitives.

**Files:**
- Modify: `src/Ferret.Core/Primitives/DocumentId.cs`
- Create: `src/Ferret.Core/Documents/ParserId.cs`
- Create: `src/Ferret.Core/Documents/DocumentKind.cs`
- Create: `src/Ferret.Core/Documents/DocumentSection.cs`
- Create: `tests/Ferret.Core.Tests/Documents/DocumentTypedIdTests.cs`

**Interfaces:**
- Produces: `Primitives.DocumentId.From(AssetId)`, `ParserId`, `DocumentKind`, `DocumentSection` — consumed by Tasks 2, 3

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Documents/DocumentTypedIdTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class DocumentTypedIdTests
{
    [Fact]
    public void DocumentId_From_AssetId_Is_Deterministic()
    {
        var assetId = new AssetId("filesystem:///src/Program.cs");
        Assert.Equal(DocumentId.From(assetId), DocumentId.From(assetId));
    }

    [Fact]
    public void DocumentId_From_Different_AssetIds_Are_Not_Equal()
    {
        var a = DocumentId.From(new AssetId("filesystem:///src/A.cs"));
        var b = DocumentId.From(new AssetId("filesystem:///src/B.cs"));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DocumentId_From_Preserves_AssetId_Value()
    {
        var assetId = new AssetId("filesystem:///src/Program.cs");
        Assert.Equal(assetId.Value, DocumentId.From(assetId).Value);
    }

    [Fact]
    public void ParserId_Equality_By_Value()
    {
        Assert.Equal(new ParserId("text/plain"), new ParserId("text/plain"));
    }

    [Fact]
    public void ParserId_Inequality_Different_Value()
    {
        Assert.NotEqual(new ParserId("text/plain"), new ParserId("text/markdown"));
    }

    [Fact]
    public void ParserId_ToString_Returns_Value()
    {
        Assert.Equal("text/markdown", new ParserId("text/markdown").ToString());
    }

    [Fact]
    public void DocumentKind_Has_Expected_Integer_Values()
    {
        Assert.Equal(0, (int)DocumentKind.Code);
        Assert.Equal(1, (int)DocumentKind.Prose);
        Assert.Equal(2, (int)DocumentKind.Data);
        Assert.Equal(3, (int)DocumentKind.Config);
        Assert.Equal(99, (int)DocumentKind.Unknown);
    }

    [Fact]
    public void DocumentSection_Equality_By_Value()
    {
        var a = new DocumentSection("Introduction", "Content here.", 1, 5);
        var b = new DocumentSection("Introduction", "Content here.", 1, 5);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DocumentSection_Title_May_Be_Null()
    {
        var section = new DocumentSection(null, "Content", 1, 1);
        Assert.Null(section.Title);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "DocumentTypedIdTests"
```

Expected: FAIL — `DocumentId.From`, `ParserId`, `DocumentKind`, `DocumentSection` not found.

- [ ] **Step 3: Extend `Primitives.DocumentId` with `From(AssetId)`**

In `src/Ferret.Core/Primitives/DocumentId.cs`, add after the existing `Create` method:

```csharp
using Ferret.Core.Connectors;
```

Add the `using` directive at the top, then add:

```csharp
    /// <summary>Derives a deterministic <see cref="DocumentId"/> from the source <see cref="AssetId"/>.
    /// The resulting DocumentId equals the AssetId value — one asset produces one document.</summary>
    /// <param name="assetId">The source asset identifier.</param>
    /// <returns>A deterministic <see cref="DocumentId"/>.</returns>
    public static DocumentId From(AssetId assetId) => Create(assetId.Value);
```

- [ ] **Step 4: Create `ParserId.cs`**

`src/Ferret.Core/Documents/ParserId.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>Strongly-typed identifier for a content parser. By convention, use the primary MIME type the parser handles (e.g. "text/plain").</summary>
/// <param name="Value">The raw string value.</param>
public sealed record ParserId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
```

- [ ] **Step 5: Create `DocumentKind.cs`**

`src/Ferret.Core/Documents/DocumentKind.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// Classifies the semantic kind of a Document.
/// Assigned by the parser — not inferred from MediaType.
/// The parser has content-level context that MediaType alone cannot provide.
/// </summary>
public enum DocumentKind
{
    /// <summary>Source code in any programming language.</summary>
    Code = 0,

    /// <summary>Human-readable prose: documentation, README files, Markdown articles.</summary>
    Prose = 1,

    /// <summary>Structured data: JSON arrays, CSV datasets, tabular files.</summary>
    Data = 2,

    /// <summary>Configuration: JSON configs, TOML settings, YAML manifests.</summary>
    Config = 3,

    /// <summary>Kind could not be determined by the parser.</summary>
    Unknown = 99,
}
```

- [ ] **Step 6: Create `DocumentSection.cs`**

`src/Ferret.Core/Documents/DocumentSection.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// A logically distinct section within a Document, extracted by the parser.
/// Sprint 9: H1 and H2 Markdown headings. Future parsers may extract any structural heading.
/// </summary>
/// <param name="Title">The section title extracted by the parser (e.g. a Markdown heading). May be null.</param>
/// <param name="Content">The plain-text content of this section.</param>
/// <param name="StartLine">The 1-based source line number where this section begins.</param>
/// <param name="EndLine">The 1-based source line number where this section ends (inclusive).</param>
public sealed record DocumentSection(string? Title, string Content, int StartLine, int EndLine);
```

- [ ] **Step 7: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "DocumentTypedIdTests"
dotnet build src/Ferret.sln
```

Expected: 9 tests pass, 0 build errors.

---

## Task 2: `Document` record + `ParseContext`

**Files:**
- Create: `src/Ferret.Core/Documents/Document.cs`
- Create: `src/Ferret.Core/Documents/ParseContext.cs`
- Create: `tests/Ferret.Core.Tests/Documents/DocumentTests.cs`

**Interfaces:**
- Consumes: `DocumentId` (Primitives), `ParserId`, `DocumentKind`, `DocumentSection`, `AssetId`, `ConnectorId`, `ConnectorInstanceId`, `AssetFingerprint`, `AssetDescriptor` (all from Ferret.Core)
- Produces: `Document`, `ParseContext` — consumed by Tasks 4 (IContentParser), S2 (parsers), S3 (IIndexEngine)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Documents/DocumentTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class DocumentTests
{
    [Fact]
    public void Document_Id_Matches_SourceAssetId()
    {
        var assetId = new AssetId("filesystem:///src/Program.cs");
        var doc = MakeDocument(assetId);
        Assert.Equal(DocumentId.From(assetId), doc.Id);
    }

    [Fact]
    public void Document_Metadata_Defaults_To_Empty()
    {
        var doc = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        Assert.Empty(doc.Metadata);
    }

    [Fact]
    public void Document_Sections_Defaults_To_Empty()
    {
        var doc = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        Assert.Empty(doc.Sections);
    }

    [Fact]
    public void Document_SourceFingerprint_May_Be_Null()
    {
        var doc = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        Assert.Null(doc.SourceFingerprint);
    }

    [Fact]
    public void Document_Has_No_Public_Setters()
    {
        var props = typeof(Document).GetProperties();
        Assert.All(props, p => Assert.False(
            p.CanWrite && (p.SetMethod?.IsPublic ?? false),
            $"Property '{p.Name}' must not have a public setter — Document is immutable"));
    }

    [Fact]
    public void Document_With_Expression_Creates_New_Instance_Leaving_Original_Unchanged()
    {
        var original = MakeDocument(new AssetId("filesystem:///src/A.cs"));
        var modified = original with { Title = "Updated Title" };

        Assert.NotSame(original, modified);
        Assert.Null(original.Title);
        Assert.Equal("Updated Title", modified.Title);
    }

    [Fact]
    public void ParseContext_For_Sets_Asset()
    {
        var asset = MakeAsset(new Uri("filesystem:///src/A.cs"));
        var ctx = ParseContext.For(asset);
        Assert.Same(asset, ctx.Asset);
    }

    private static Document MakeDocument(AssetId assetId) => new()
    {
        Id = DocumentId.From(assetId),
        SourceAssetId = assetId,
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        MediaType = "text/x-csharp",
        Kind = DocumentKind.Code,
        PlainText = "class Program { }",
        ProducedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
    };

    private static AssetDescriptor MakeAsset(Uri uri) => new()
    {
        Id = AssetId.From(uri),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("src-root"),
        Kind = AssetKind.File,
        CanonicalUri = uri,
        DisplayName = "A.cs",
        LastModified = DateTimeOffset.UtcNow,
    };
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "DocumentTests"
```

Expected: FAIL — `Document`, `ParseContext` not found.

- [ ] **Step 3: Create `Document.cs`**

`src/Ferret.Core/Documents/Document.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Documents;

/// <summary>
/// The canonical output of the parsing stage — the Document Platform's parallel to AssetDescriptor
/// in the Connector Platform. Immutable: any transformation creates a new Document instance.
/// Provenance is always preserved: Document → AssetDescriptor → IConnector → IConnectorRegistry.
/// </summary>
public sealed record Document
{
    /// <summary>Gets the document identifier, derived deterministically from <see cref="SourceAssetId"/>.
    /// DocumentId equals SourceAssetId.Value — one asset, one document in Sprint 9.</summary>
    public required DocumentId Id { get; init; }

    /// <summary>Gets the identifier of the source asset that produced this document.</summary>
    public required AssetId SourceAssetId { get; init; }

    /// <summary>Gets the connector type that owns the source asset.</summary>
    public required ConnectorId ConnectorId { get; init; }

    /// <summary>Gets the workspace-scoped connector instance that owns the source asset.</summary>
    public required ConnectorInstanceId InstanceId { get; init; }

    /// <summary>Gets the MIME type of the source content (e.g. "text/markdown").</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the semantic kind of this document. Assigned by the parser — not inferred from MediaType.</summary>
    public required DocumentKind Kind { get; init; }

    /// <summary>Gets the full plain-text representation of the document content.
    /// This is the primary field indexed by the keyword (FTS5) index.</summary>
    public required string PlainText { get; init; }

    /// <summary>Gets the UTC timestamp at which this document was produced by the parser.</summary>
    public required DateTimeOffset ProducedAt { get; init; }

    /// <summary>Gets the fingerprint of the source asset at the time this document was produced.
    /// Used by the indexing pipeline to determine whether re-parsing is needed without re-reading source content.
    /// This is the foundation for incremental indexing in a future sprint.</summary>
    public AssetFingerprint? SourceFingerprint { get; init; }

    /// <summary>Gets the document title extracted by the parser (e.g. first H1 in Markdown). May be null.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the structural sections extracted by the parser.
    /// Sprint 9: H1/H2 Markdown headings. Future parsers may extract any structural element.</summary>
    public IReadOnlyList<DocumentSection> Sections { get; init; } = [];

    /// <summary>Gets parser-assigned metadata as key-value pairs.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
```

- [ ] **Step 4: Create `ParseContext.cs`**

`src/Ferret.Core/Documents/ParseContext.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Core.Documents;

/// <summary>Contextual information provided to a parser alongside the content stream.
/// Gives the parser access to asset provenance without requiring extra parameters.</summary>
public sealed class ParseContext
{
    /// <summary>Gets the asset descriptor for the content being parsed.</summary>
    public required AssetDescriptor Asset { get; init; }

    /// <summary>Creates a <see cref="ParseContext"/> for the given asset.</summary>
    /// <param name="asset">The asset whose content is being parsed.</param>
    /// <returns>A new <see cref="ParseContext"/>.</returns>
    public static ParseContext For(AssetDescriptor asset) => new() { Asset = asset };
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "DocumentTests|DocumentTypedIdTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 3: `MediaTypeInfo` + `IMimeTypeResolver` + `ParseResult<T>`

**Files:**
- Create: `src/Ferret.Core/Documents/MediaTypeInfo.cs`
- Create: `src/Ferret.Core/Documents/IMimeTypeResolver.cs`
- Create: `src/Ferret.Core/Documents/ParseDiagnostic.cs`
- Create: `src/Ferret.Core/Documents/ParseResultKind.cs`
- Create: `src/Ferret.Core/Documents/ParseResult.cs`
- Create: `tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs`
- Create: `tests/Ferret.Core.Tests/Documents/ParseResultTests.cs`

**Interfaces:**
- Consumes: `DocumentKind` (Task 1)
- Produces: `MediaTypeInfo`, `IMimeTypeResolver`, `ParseResult<T>` — consumed by Task 4 (IParserDispatcher), S2 (MimeTypeResolver, ParserDispatcher)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs`:

```csharp
using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class MediaTypeInfoTests
{
    [Fact]
    public void MediaTypeInfo_Has_No_Public_Setters()
    {
        var props = typeof(MediaTypeInfo).GetProperties();
        Assert.All(props, p => Assert.False(
            p.CanWrite && (p.SetMethod?.IsPublic ?? false),
            $"Property '{p.Name}' must not have a public setter"));
    }

    [Fact]
    public void MediaTypeInfo_IsText_And_IsBinary_Are_Mutually_Exclusive()
    {
        var text = new MediaTypeInfo
        {
            MediaType = "text/plain",
            IsText = true,
            IsBinary = false,
        };
        Assert.True(text.IsText);
        Assert.False(text.IsBinary);
    }

    [Fact]
    public void MediaTypeInfo_Confidence_Defaults_To_One()
    {
        var info = new MediaTypeInfo { MediaType = "text/plain", IsText = true, IsBinary = false };
        Assert.Equal(1.0, info.Confidence);
    }

    [Fact]
    public void MediaTypeInfo_SuggestedKind_Defaults_To_Null()
    {
        var info = new MediaTypeInfo { MediaType = "text/plain", IsText = true, IsBinary = false };
        Assert.Null(info.SuggestedKind);
    }
}
```

Create `tests/Ferret.Core.Tests/Documents/ParseResultTests.cs`:

```csharp
using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class ParseResultTests
{
    [Fact]
    public void Success_IsSuccess_True_And_Value_Set()
    {
        var result = ParseResult<string>.Success("hello");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
        Assert.Equal(ParseResultKind.Success, result.Kind);
    }

    [Fact]
    public void Unsupported_IsSuccess_False_And_Contains_MediaType()
    {
        var result = ParseResult<string>.Unsupported("application/pdf");
        Assert.False(result.IsSuccess);
        Assert.Equal(ParseResultKind.Unsupported, result.Kind);
        Assert.Contains("application/pdf", result.Diagnostics[0].Message);
    }

    [Fact]
    public void Empty_IsSuccess_False()
    {
        var result = ParseResult<string>.Empty();
        Assert.False(result.IsSuccess);
        Assert.Equal(ParseResultKind.Empty, result.Kind);
    }

    [Fact]
    public void Failed_IsSuccess_False_And_Has_Error_Diagnostic()
    {
        var result = ParseResult<string>.Failed("bad JSON at line 3");
        Assert.False(result.IsSuccess);
        Assert.Equal(ParseResultKind.Failed, result.Kind);
        Assert.Single(result.Diagnostics);
        Assert.Equal(ParseDiagnosticSeverity.Error, result.Diagnostics[0].Severity);
        Assert.Equal("bad JSON at line 3", result.Diagnostics[0].Message);
    }

    [Fact]
    public void Success_Value_Is_Null_For_Non_Success_Results()
    {
        Assert.Null(ParseResult<string>.Unsupported("x/y").Value);
        Assert.Null(ParseResult<string>.Empty().Value);
        Assert.Null(ParseResult<string>.Failed("err").Value);
    }

    [Fact]
    public void Success_Has_Empty_Diagnostics()
    {
        var result = ParseResult<string>.Success("ok");
        Assert.Empty(result.Diagnostics);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "MediaTypeInfoTests|ParseResultTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `MediaTypeInfo.cs`**

`src/Ferret.Core/Documents/MediaTypeInfo.cs`:

```csharp
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

    /// <summary>Gets a value indicating whether the content is expected to be human-readable text.</summary>
    public required bool IsText { get; init; }

    /// <summary>Gets a value indicating whether the content is expected to be binary (non-text).</summary>
    public required bool IsBinary { get; init; }

    /// <summary>Gets an optional suggested DocumentKind hint for the parser.
    /// The parser may override this based on content inspection.</summary>
    public DocumentKind? SuggestedKind { get; init; }

    /// <summary>Gets the resolver's confidence in this classification (0.0–1.0).
    /// 1.0 means the extension is well-known; lower values indicate a fallback mapping.</summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>Creates a <see cref="MediaTypeInfo"/> representing an unrecognized binary file.</summary>
    public static MediaTypeInfo Unknown => new()
    {
        MediaType = "application/octet-stream",
        IsText = false,
        IsBinary = true,
        Confidence = 0.5,
    };
}
```

- [ ] **Step 4: Create `IMimeTypeResolver.cs`**

`src/Ferret.Core/Documents/IMimeTypeResolver.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// Resolves MIME type information from a file name.
/// Lives in Ferret.Core so that Ferret.Connectors.* can populate AssetDescriptor.MediaType
/// without referencing Ferret.ParserPlatform. The implementation lives in Ferret.ParserPlatform.
/// Resolution happens once at the connector edge — never re-resolved downstream.
/// </summary>
public interface IMimeTypeResolver
{
    /// <summary>Resolves the MIME type and related metadata for the given file name.
    /// Never throws. Returns <see cref="MediaTypeInfo.Unknown"/> for unrecognized file types.</summary>
    /// <param name="fileName">The file name including extension (e.g. "README.md").</param>
    /// <returns>A <see cref="MediaTypeInfo"/> describing the resolved type.</returns>
    MediaTypeInfo Resolve(string fileName);
}
```

- [ ] **Step 5: Create `ParseDiagnostic.cs`**

`src/Ferret.Core/Documents/ParseDiagnostic.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>A diagnostic message produced during parsing. Severity determines whether
/// the result is usable (Warning) or not (Error).</summary>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
public sealed record ParseDiagnostic(ParseDiagnosticSeverity Severity, string Message);

/// <summary>Severity levels for parse diagnostics.</summary>
public enum ParseDiagnosticSeverity
{
    /// <summary>Informational note — does not affect result usability.</summary>
    Info = 0,

    /// <summary>Non-fatal issue (e.g. encoding fallback, malformed section) — result is still usable.</summary>
    Warning = 1,

    /// <summary>Fatal parse error — result is not usable.</summary>
    Error = 2,
}
```

- [ ] **Step 6: Create `ParseResultKind.cs`**

`src/Ferret.Core/Documents/ParseResultKind.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>Describes the outcome of a parse dispatch attempt.</summary>
public enum ParseResultKind
{
    /// <summary>The content was parsed successfully and a Document was produced.</summary>
    Success = 0,

    /// <summary>No parser is registered for the asset's media type.</summary>
    Unsupported = 1,

    /// <summary>The content stream was empty or contained only whitespace.</summary>
    Empty = 2,

    /// <summary>The parser encountered an error during parsing. See Diagnostics for detail.</summary>
    Failed = 3,
}
```

- [ ] **Step 7: Create `ParseResult.cs`**

`src/Ferret.Core/Documents/ParseResult.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// Represents the outcome of a parse dispatch operation.
/// All failure modes are explicit outcomes — the dispatcher never throws.
/// Use the static factory methods to construct instances.
/// </summary>
public sealed class ParseResult<T>
{
    private ParseResult() { }

    /// <summary>Gets a value indicating whether parsing produced a valid result.</summary>
    public bool IsSuccess => Kind == ParseResultKind.Success;

    /// <summary>Gets the parsed value. Only valid when <see cref="IsSuccess"/> is true.</summary>
    public T? Value { get; private init; }

    /// <summary>Gets the outcome kind.</summary>
    public ParseResultKind Kind { get; private init; }

    /// <summary>Gets diagnostics collected during parsing (warnings, errors, info notes).</summary>
    public IReadOnlyList<ParseDiagnostic> Diagnostics { get; private init; } = [];

    /// <summary>Parsing succeeded and produced a valid result.</summary>
    public static ParseResult<T> Success(T value) =>
        new() { Kind = ParseResultKind.Success, Value = value };

    /// <summary>No parser is registered for the given media type.</summary>
    public static ParseResult<T> Unsupported(string mediaType) =>
        new()
        {
            Kind = ParseResultKind.Unsupported,
            Diagnostics = [new ParseDiagnostic(ParseDiagnosticSeverity.Warning,
                $"No parser registered for media type '{mediaType}'")],
        };

    /// <summary>The content stream was empty or whitespace-only.</summary>
    public static ParseResult<T> Empty() =>
        new() { Kind = ParseResultKind.Empty };

    /// <summary>The parser failed with an error message.</summary>
    public static ParseResult<T> Failed(string message) =>
        new()
        {
            Kind = ParseResultKind.Failed,
            Diagnostics = [new ParseDiagnostic(ParseDiagnosticSeverity.Error, message)],
        };

    /// <summary>The parser failed and collected multiple diagnostics.</summary>
    public static ParseResult<T> Failed(IReadOnlyList<ParseDiagnostic> diagnostics) =>
        new() { Kind = ParseResultKind.Failed, Diagnostics = diagnostics };
}
```

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "MediaTypeInfoTests|ParseResultTests|DocumentTests|DocumentTypedIdTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 4: Parser Capability Model + Parser Interfaces

**Files:**
- Create: `src/Ferret.Core/Documents/ParserCapability.cs`
- Create: `src/Ferret.Core/Documents/ParserCapabilities.cs`
- Create: `src/Ferret.Core/Documents/ParserDescriptor.cs`
- Create: `src/Ferret.Core/Documents/IContentParser.cs`
- Create: `src/Ferret.Core/Documents/IParserRegistry.cs`
- Create: `src/Ferret.Core/Documents/IParserDispatcher.cs`
- Create: `src/Ferret.Core/Documents/IContentNormalizer.cs`
- Create: `tests/Ferret.Core.Tests/Documents/ParserCapabilityTests.cs`
- Create: `tests/Ferret.Core.Tests/Documents/ParserDescriptorTests.cs`

**Interfaces:**
- Consumes: `ParserId`, `DocumentKind`, `Document`, `ParseContext`, `ParseResult<T>`, `AssetDescriptor`
- Produces: parser capability model + interfaces — consumed by S2 (ParserRegistry, ParserDispatcher, built-in parsers)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Documents/ParserCapabilityTests.cs`:

```csharp
using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class ParserCapabilityTests
{
    [Fact]
    public void PlainTextExtraction_Singleton_Is_Referentially_Stable()
    {
        Assert.Same(ParserCapabilities.PlainTextExtraction, ParserCapabilities.PlainTextExtraction);
    }

    [Fact]
    public void SectionExtraction_Is_In_All()
    {
        Assert.Contains(ParserCapabilities.SectionExtraction, ParserCapabilities.All);
    }

    [Fact]
    public void All_Has_Four_Entries()
    {
        Assert.Equal(4, ParserCapabilities.All.Count);
    }

    [Fact]
    public void ParserCapability_Equality_By_All_Fields()
    {
        var a = new ParserCapability("plain-text", "Plain Text Extraction", "1.0", "desc");
        var b = new ParserCapability("plain-text", "Plain Text Extraction", "1.0", "desc");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ParserCapability_Inequality_Different_Id()
    {
        Assert.NotEqual(
            new ParserCapability("a", "A", "1.0", "x"),
            new ParserCapability("b", "B", "1.0", "x"));
    }
}
```

Create `tests/Ferret.Core.Tests/Documents/ParserDescriptorTests.cs`:

```csharp
using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class ParserDescriptorTests
{
    [Fact]
    public void ParserDescriptor_Has_No_Public_Setters()
    {
        var props = typeof(ParserDescriptor).GetProperties();
        Assert.All(props, p => Assert.False(
            p.CanWrite && (p.SetMethod?.IsPublic ?? false),
            $"Property '{p.Name}' must not have a public setter"));
    }

    [Fact]
    public void ParserDescriptor_Priority_Defaults_To_100()
    {
        var desc = MakeDescriptor("text/plain", priority: 100);
        Assert.Equal(100, desc.Priority);
    }

    [Fact]
    public void ParserDescriptor_Supports_Higher_Priority()
    {
        var desc = MakeDescriptor("text/markdown", priority: 200);
        Assert.Equal(200, desc.Priority);
    }

    [Fact]
    public void ParserDescriptor_SupportedMediaTypes_Not_Empty()
    {
        var desc = MakeDescriptor("text/plain");
        Assert.NotEmpty(desc.SupportedMediaTypes);
    }

    private static ParserDescriptor MakeDescriptor(string mediaType, int priority = 100) =>
        new()
        {
            Id = new ParserId(mediaType),
            Name = "Test Parser",
            Version = "1.0",
            SupportedMediaTypes = [mediaType],
            Capabilities = [ParserCapabilities.PlainTextExtraction],
            Priority = priority,
        };
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "ParserCapabilityTests|ParserDescriptorTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `ParserCapability.cs`**

`src/Ferret.Core/Documents/ParserCapability.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>Describes a specific capability a content parser can provide.
/// Use <see cref="ParserCapabilities"/> for well-known singletons.</summary>
/// <param name="Id">Unique capability identifier (e.g. "plain-text").</param>
/// <param name="Name">Human-readable capability name.</param>
/// <param name="Version">Semantic version of this capability.</param>
/// <param name="Description">Short description for CLI display.</param>
public sealed record ParserCapability(string Id, string Name, string Version, string Description);
```

- [ ] **Step 4: Create `ParserCapabilities.cs`**

`src/Ferret.Core/Documents/ParserCapabilities.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>Well-known parser capabilities as immutable singletons.
/// Use these instead of constructing new <see cref="ParserCapability"/> instances.</summary>
public static class ParserCapabilities
{
    /// <summary>Parser extracts full plain text for keyword indexing. All built-in parsers provide this.</summary>
    public static readonly ParserCapability PlainTextExtraction =
        new("plain-text", "Plain Text Extraction", "1.0",
            "Extracts the full plain-text representation of the document for keyword indexing.");

    /// <summary>Parser extracts structural sections (e.g. Markdown headings, notebook cells).</summary>
    public static readonly ParserCapability SectionExtraction =
        new("section-extraction", "Section Extraction", "1.0",
            "Extracts logically distinct sections as DocumentSection entries.");

    /// <summary>Parser extracts structured metadata (e.g. JSON properties, YAML front matter).</summary>
    public static readonly ParserCapability MetadataExtraction =
        new("metadata-extraction", "Metadata Extraction", "1.0",
            "Extracts key-value metadata into Document.Metadata.");

    /// <summary>Parser extracts hyperlinks or cross-references. Reserved for future sprints.</summary>
    public static readonly ParserCapability LinkExtraction =
        new("link-extraction", "Link Extraction", "1.0",
            "Extracts hyperlinks and cross-references from content.");

    /// <summary>All well-known capabilities in definition order.</summary>
    public static IReadOnlyList<ParserCapability> All { get; } = [
        PlainTextExtraction, SectionExtraction, MetadataExtraction, LinkExtraction,
    ];
}
```

- [ ] **Step 5: Create `ParserDescriptor.cs`**

`src/Ferret.Core/Documents/ParserDescriptor.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// Static descriptor for a registered content parser type. Immutable — no public setters.
/// Mirrors ConnectorDescriptor in the Connector Platform.
/// Priority determines dispatch order when multiple parsers support the same media type —
/// higher priority always wins over a more general parser (e.g. MarkdownParser 200 &gt; PlainTextParser 100).
/// </summary>
public sealed record ParserDescriptor
{
    /// <summary>Gets the parser identifier. By convention, use the primary MIME type handled.</summary>
    public required ParserId Id { get; init; }

    /// <summary>Gets the human-readable parser name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the parser version string.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the MIME types this parser handles.</summary>
    public required IReadOnlyList<string> SupportedMediaTypes { get; init; }

    /// <summary>Gets the capabilities this parser provides.</summary>
    public required IReadOnlyList<ParserCapability> Capabilities { get; init; }

    /// <summary>Gets the dispatch priority. Higher values win when multiple parsers support the same media type.
    /// Convention: 100 = general fallback, 200 = specific format, 500 = user-supplied override.</summary>
    public int Priority { get; init; } = 100;
}
```

- [ ] **Step 6: Create `IContentParser.cs`**

`src/Ferret.Core/Documents/IContentParser.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// A content parser that transforms a raw content stream into a <see cref="Document"/>.
/// Implementations MUST be sealed. CanParse is pure — no I/O, no exceptions, no side effects.
/// Parsers are responsible for assigning DocumentKind — never infer it from MediaType alone.
/// </summary>
public interface IContentParser
{
    /// <summary>Gets the static descriptor for this parser.</summary>
    ParserDescriptor Descriptor { get; }

    /// <summary>Returns true if this parser can handle the given MIME type.
    /// Pure — no I/O, never throws, always returns the same result for the same input.</summary>
    /// <param name="mediaType">The MIME type to check (e.g. "text/markdown").</param>
    bool CanParse(string mediaType);

    /// <summary>Parses the content stream and produces a Document.
    /// The stream is positioned at the beginning. Do not close or dispose it.</summary>
    /// <param name="content">The raw content stream to parse.</param>
    /// <param name="context">Contextual information including the source AssetDescriptor.</param>
    /// <param name="ct">A cancellation token.</param>
    ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct = default);
}
```

- [ ] **Step 7: Create `IParserRegistry.cs`**

`src/Ferret.Core/Documents/IParserRegistry.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>Read-only registry of all registered content parser descriptors.
/// Mirrors IConnectorRegistry in the Connector Platform.</summary>
public interface IParserRegistry
{
    /// <summary>Returns all registered parser descriptors, ordered by priority descending.</summary>
    IReadOnlyList<ParserDescriptor> GetAll();

    /// <summary>Returns the descriptor for the given parser ID, or null if not registered.</summary>
    ParserDescriptor? GetById(ParserId id);

    /// <summary>Returns the highest-priority parser that can handle the given media type,
    /// or null if no registered parser supports it.
    /// Callers check for null — there is no separate CanParse method on the registry.</summary>
    IContentParser? GetParserFor(string mediaType);
}
```

- [ ] **Step 8: Create `IParserDispatcher.cs`**

`src/Ferret.Core/Documents/IParserDispatcher.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Core.Documents;

/// <summary>
/// Dispatches parse requests to the appropriate <see cref="IContentParser"/> based on
/// <see cref="AssetDescriptor.MediaType"/>. Returns a <see cref="ParseResult{T}"/> —
/// the dispatcher never throws. All failure modes are explicit outcomes.
/// </summary>
public interface IParserDispatcher
{
    /// <summary>Selects the highest-priority compatible parser and parses the content stream.</summary>
    /// <param name="content">The raw content stream, positioned at the beginning.</param>
    /// <param name="asset">The source asset descriptor — MediaType drives parser selection.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ParseResult{Document}"/> describing the outcome.</returns>
    ValueTask<ParseResult<Document>> DispatchAsync(
        Stream content,
        AssetDescriptor asset,
        CancellationToken ct = default);
}
```

- [ ] **Step 9: Create `IContentNormalizer.cs` (reserved stub)**

`src/Ferret.Core/Documents/IContentNormalizer.cs`:

```csharp
namespace Ferret.Core.Documents;

/// <summary>
/// Reserved extension point for post-parse document normalization.
/// Pipeline position: Parser → Normalizer → Document.
/// Examples (future sprints): Unicode normalization, line-ending normalization,
/// whitespace cleanup, HTML entity decoding.
/// Not implemented in Sprint 9.
/// </summary>
public interface IContentNormalizer
{
    // Sprint 10+:
    // ValueTask<Document> NormalizeAsync(Document document, CancellationToken ct = default);
}
```

- [ ] **Step 10: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "ParserCapabilityTests|ParserDescriptorTests"
dotnet test tests/Ferret.Core.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass including Tasks 1–3, 0 build errors.

---

## Task 5: Indexing Contracts (`Ferret.Core.Indexing`)

**Files:**
- Create: `src/Ferret.Core/Indexing/IndexResult.cs`
- Create: `src/Ferret.Core/Indexing/IndexStats.cs`
- Create: `src/Ferret.Core/Indexing/IndexPipelineOptions.cs`
- Create: `src/Ferret.Core/Indexing/IIndexEngine.cs`
- Create: `src/Ferret.Core/Indexing/IIndexPipeline.cs`
- Create: `tests/Ferret.Core.Tests/Indexing/IndexResultTests.cs`

**Interfaces:**
- Consumes: `Document` (Task 2), `DocumentId` (Primitives), `ConnectorInstanceId` (Connectors)
- Produces: indexing contract types — consumed by S3 (KeywordIndex, IndexingPipeline), S5 (IndexCommandHandler)

No interface tests — these are pure contracts verified by `dotnet build`.

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Indexing/IndexResultTests.cs`:

```csharp
using Ferret.Core.Indexing;
using Xunit;

namespace Ferret.Core.Tests.Indexing;

public sealed class IndexResultTests
{
    [Fact]
    public void IndexResult_FailureMessages_Defaults_To_Empty()
    {
        var result = MakeResult();
        Assert.Empty(result.FailureMessages);
        Assert.Empty(result.WarningMessages);
    }

    [Fact]
    public void IndexResult_Has_No_Public_Setters()
    {
        var props = typeof(IndexResult).GetProperties();
        Assert.All(props, p => Assert.False(
            p.CanWrite && (p.SetMethod?.IsPublic ?? false),
            $"Property '{p.Name}' must not have a public setter"));
    }

    [Fact]
    public void IndexStats_Has_No_Public_Setters()
    {
        var props = typeof(IndexStats).GetProperties();
        Assert.All(props, p => Assert.False(
            p.CanWrite && (p.SetMethod?.IsPublic ?? false),
            $"Property '{p.Name}' must not have a public setter"));
    }

    [Fact]
    public void IndexPipelineOptions_Default_Has_No_InstanceId_Filter_And_No_ForceRebuild()
    {
        Assert.Null(IndexPipelineOptions.Default.InstanceId);
        Assert.False(IndexPipelineOptions.Default.ForceRebuild);
    }

    private static IndexResult MakeResult() => new()
    {
        AssetsDiscovered = 10,
        DocumentsIndexed = 8,
        DocumentsSkipped = 2,
        Failures = 0,
        Warnings = 0,
        Duration = TimeSpan.FromSeconds(1.5),
    };
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "IndexResultTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `IndexResult.cs`**

`src/Ferret.Core/Indexing/IndexResult.cs`:

```csharp
namespace Ferret.Core.Indexing;

/// <summary>The outcome of a complete index pipeline run. Immutable.</summary>
public sealed record IndexResult
{
    /// <summary>Gets the total number of assets discovered across all configured connectors.</summary>
    public required int AssetsDiscovered { get; init; }

    /// <summary>Gets the number of documents successfully written to the index.</summary>
    public required int DocumentsIndexed { get; init; }

    /// <summary>Gets the number of assets skipped (unsupported media type, empty content, binary).</summary>
    public required int DocumentsSkipped { get; init; }

    /// <summary>Gets the number of assets that failed to parse or index.</summary>
    public required int Failures { get; init; }

    /// <summary>Gets the number of non-fatal warnings encountered during the run.</summary>
    public required int Warnings { get; init; }

    /// <summary>Gets the total wall-clock duration of the pipeline run.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Gets failure messages for diagnostics and display.</summary>
    public IReadOnlyList<string> FailureMessages { get; init; } = [];

    /// <summary>Gets warning messages for diagnostics and display.</summary>
    public IReadOnlyList<string> WarningMessages { get; init; } = [];
}
```

- [ ] **Step 4: Create `IndexStats.cs`**

`src/Ferret.Core/Indexing/IndexStats.cs`:

```csharp
namespace Ferret.Core.Indexing;

/// <summary>Current state of the keyword index store. Immutable snapshot.</summary>
public sealed record IndexStats
{
    /// <summary>Gets the number of documents currently in the index.</summary>
    public required long DocumentCount { get; init; }

    /// <summary>Gets the total number of characters across all indexed plain-text fields.</summary>
    public required long TotalChars { get; init; }

    /// <summary>Gets the UTC timestamp of the most recent successful indexing run.</summary>
    public required DateTimeOffset LastIndexedAt { get; init; }

    /// <summary>Gets the on-disk size of the index file in bytes.</summary>
    public required long IndexSizeBytes { get; init; }
}
```

- [ ] **Step 5: Create `IndexPipelineOptions.cs`**

`src/Ferret.Core/Indexing/IndexPipelineOptions.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Core.Indexing;

/// <summary>Options controlling a single index pipeline run.</summary>
public sealed class IndexPipelineOptions
{
    /// <summary>Gets an optional filter to run the pipeline for a single connector instance only.
    /// Null means all enabled instances from connectors.json are processed.</summary>
    public ConnectorInstanceId? InstanceId { get; init; }

    /// <summary>Gets a value indicating whether to drop and rebuild the index from scratch,
    /// discarding all previously indexed content.</summary>
    public bool ForceRebuild { get; init; }

    /// <summary>Gets a shared default instance — all enabled connectors, no forced rebuild.</summary>
    public static IndexPipelineOptions Default { get; } = new();
}
```

- [ ] **Step 6: Create `IIndexEngine.cs`**

`src/Ferret.Core/Indexing/IIndexEngine.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.Core.Indexing;

/// <summary>
/// The write and administrative interface for the keyword index store.
/// Implementations (e.g. SQLite FTS5) live in Ferret.Indexing.
/// CLI handlers never call this directly — they go through IIndexPipeline.
/// </summary>
public interface IIndexEngine
{
    /// <summary>Writes or replaces the document in the index (upsert on DocumentId).</summary>
    Task WriteAsync(Document document, CancellationToken ct = default);

    /// <summary>Returns current statistics about the index store.</summary>
    Task<IndexStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>Drops and recreates the index. All previously indexed content is lost.</summary>
    Task RebuildAsync(CancellationToken ct = default);

    // Reserved for Sprint 10 (incremental indexing — remove documents for deleted assets):
    // Task DeleteAsync(DocumentId documentId, CancellationToken ct = default);
}
```

- [ ] **Step 7: Create `IIndexPipeline.cs`**

`src/Ferret.Core/Indexing/IIndexPipeline.cs`:

```csharp
namespace Ferret.Core.Indexing;

/// <summary>
/// Orchestrates the full ingestion pipeline: discover assets → parse content → write to index.
/// The CLI handler is a thin presentation layer over this interface.
/// Lifecycle events are published through Ferret.Events during execution.
/// </summary>
public interface IIndexPipeline
{
    /// <summary>Runs the complete ingestion pipeline and returns a summary of the outcome.</summary>
    /// <param name="options">Options controlling which connectors to run and whether to force rebuild.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<IndexResult> RunAsync(IndexPipelineOptions options, CancellationToken ct = default);
}
```

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "IndexResultTests"
dotnet test tests/Ferret.Core.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 6: Indexing Lifecycle Events

**Files:**
- Create: `src/Ferret.Core/Events/Indexing/IndexingStartedEvent.cs`
- Create: `src/Ferret.Core/Events/Indexing/DocumentParsedEvent.cs`
- Create: `src/Ferret.Core/Events/Indexing/DocumentIndexedEvent.cs`
- Create: `src/Ferret.Core/Events/Indexing/DocumentSkippedEvent.cs`
- Create: `src/Ferret.Core/Events/Indexing/DocumentParsingFailedEvent.cs`
- Create: `src/Ferret.Core/Events/Indexing/IndexingCompletedEvent.cs`
- Create: `src/Ferret.Core/Events/Indexing/IndexingFailedEvent.cs`

**Interfaces:**
- Consumes: `DomainEvent` base class (`Ferret.Core.Events`), `CorrelationId` (`Ferret.Core.Primitives`), `AssetId`, `DocumentId` (Primitives), `IndexResult` (Task 5)
- Produces: indexing event types — published by `IndexingPipeline` (S3), subscribed by `IndexCommandHandler` (S5) for progress display

No unit tests — event types are structural; verified by `dotnet build`. The `IndexCommandHandler` integration test in S5 covers event subscription end-to-end.

- [ ] **Step 1: Create `IndexingStartedEvent.cs`**

`src/Ferret.Core/Events/Indexing/IndexingStartedEvent.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an index pipeline run begins. Aggregate: workspace ID.</summary>
public sealed class IndexingStartedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="IndexingStartedEvent"/>.</summary>
    public IndexingStartedEvent(string workspaceId, CorrelationId correlationId)
        : base(workspaceId, correlationId) { }

    /// <summary>Gets a value indicating whether this is a full rebuild run.</summary>
    public bool IsRebuild { get; init; }
}
```

- [ ] **Step 2: Create `DocumentParsedEvent.cs`**

`src/Ferret.Core/Events/Indexing/DocumentParsedEvent.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an asset has been successfully parsed into a Document.</summary>
public sealed class DocumentParsedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="DocumentParsedEvent"/>.</summary>
    public DocumentParsedEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId) { }

    /// <summary>Gets the source asset identifier.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the produced document identifier.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the MIME type of the parsed content.</summary>
    public required string MediaType { get; init; }
}
```

- [ ] **Step 3: Create `DocumentIndexedEvent.cs`**

`src/Ferret.Core/Events/Indexing/DocumentIndexedEvent.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when a Document has been written to the keyword index.
/// The CLI subscribes to this event to display per-document progress.</summary>
public sealed class DocumentIndexedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="DocumentIndexedEvent"/>.</summary>
    public DocumentIndexedEvent(string documentId, CorrelationId correlationId)
        : base(documentId, correlationId) { }

    /// <summary>Gets the indexed document identifier.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the source asset identifier.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the MIME type of the indexed content.</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the number of characters in the indexed plain-text field.</summary>
    public required long CharCount { get; init; }
}
```

- [ ] **Step 4: Create `DocumentSkippedEvent.cs`**

`src/Ferret.Core/Events/Indexing/DocumentSkippedEvent.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an asset is skipped during indexing (unsupported media type, empty content).</summary>
public sealed class DocumentSkippedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="DocumentSkippedEvent"/>.</summary>
    public DocumentSkippedEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId) { }

    /// <summary>Gets the skipped asset identifier.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the reason for skipping (e.g. "Unsupported media type: application/octet-stream").</summary>
    public required string Reason { get; init; }
}
```

- [ ] **Step 5: Create `DocumentParsingFailedEvent.cs`**

`src/Ferret.Core/Events/Indexing/DocumentParsingFailedEvent.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when a parser fails for a specific asset.
/// Retains the error message so logs capture diagnostic detail even when the pipeline continues.</summary>
public sealed class DocumentParsingFailedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="DocumentParsingFailedEvent"/>.</summary>
    public DocumentParsingFailedEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId) { }

    /// <summary>Gets the asset that failed to parse.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the MIME type that was dispatched to the parser.</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the error message from the parser failure.</summary>
    public required string ErrorMessage { get; init; }
}
```

- [ ] **Step 6: Create `IndexingCompletedEvent.cs`**

`src/Ferret.Core/Events/Indexing/IndexingCompletedEvent.cs`:

```csharp
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an index pipeline run completes successfully.</summary>
public sealed class IndexingCompletedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="IndexingCompletedEvent"/>.</summary>
    public IndexingCompletedEvent(string workspaceId, CorrelationId correlationId)
        : base(workspaceId, correlationId) { }

    /// <summary>Gets the pipeline run outcome.</summary>
    public required IndexResult Result { get; init; }
}
```

- [ ] **Step 7: Create `IndexingFailedEvent.cs`**

`src/Ferret.Core/Events/Indexing/IndexingFailedEvent.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when the index pipeline itself fails (not a per-document failure).</summary>
public sealed class IndexingFailedEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="IndexingFailedEvent"/>.</summary>
    public IndexingFailedEvent(string workspaceId, CorrelationId correlationId)
        : base(workspaceId, correlationId) { }

    /// <summary>Gets the pipeline-level error message.</summary>
    public required string ErrorMessage { get; init; }
}
```

- [ ] **Step 8: Build and verify**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Core.Tests
```

Expected: 0 errors, all existing + new tests pass.

---

## Task 7: ADR-0014 — Document Processing Architecture

**Files:**
- Create: `docs/adr/0014-document-processing-architecture.md`

**Why:** This ADR is the architectural contract for the Document Platform. All future parsers, indexers, knowledge extractors, and context assemblers must conform to the eight principles recorded here.

- [ ] **Step 1: Create ADR-0014**

Write `docs/adr/0014-document-processing-architecture.md` with the full ADR content (all 8 principles, consequences, reserved extension points, traceability table).

- [ ] **Step 2: Verify build is still clean**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Core.Tests
```

Expected: 0 errors, all tests pass.

---

## Section 1 Complete

**Outputs of Section 1:**
- `Ferret.Core.Documents` namespace — 16 new types (DocumentKind, DocumentSection, Document, ParseContext, MediaTypeInfo, IMimeTypeResolver, ParseDiagnostic, ParseResultKind, ParseResult, ParserCapability, ParserCapabilities, ParserDescriptor, IContentParser, IParserRegistry, IParserDispatcher, IContentNormalizer)
- `Ferret.Core.Indexing` namespace — 5 new types (IndexResult, IndexStats, IndexPipelineOptions, IIndexEngine, IIndexPipeline)
- `Ferret.Core.Events.Indexing` namespace — 7 new event types
- `Primitives.DocumentId.From(AssetId)` factory extension
- ADR-0014 committed
- All tests pass, `dotnet build` clean

**What Section 2 (Parser Platform) depends on from here:**
- `IContentParser`, `IParserRegistry`, `IParserDispatcher` — implements these interfaces
- `IMimeTypeResolver` — provides `MimeTypeResolver` implementation
- `ParseResult<Document>` — returned by `ParserDispatcher`
- `Document`, `ParseContext`, `DocumentKind`, `DocumentSection` — produced by built-in parsers
- `MediaTypeInfo` — returned by `MimeTypeResolver`, consumed by `FilesystemConnector`
