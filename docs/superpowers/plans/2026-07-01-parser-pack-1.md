# Parser Pack 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add PDF and DOCX content parsers (plus expanded MIME mappings and a deterministic multi-format corpus generator) so Ferret indexes enterprise documents, not just code/text.

**Architecture:** `Ferret.ParserPlatform` stays intact (registry, dispatcher, `MimeTypeResolver`, 3 built-in parsers). Two new sibling packages hold heavyweight-format parsers — `Ferret.Parsers.Pdf` (PdfPig) and `Ferret.Parsers.Office` (OpenXml, DOCX only). A thin `Ferret.Parsers` project composes all three via `ParserPackModule`. The registry auto-aggregates every `IContentParser` via `GetServices<IContentParser>()`, so the dispatcher/registry/contract are untouched. Only `MimeTypeResolver` + `MediaTypeInfo` change (additively).

**Tech Stack:** .NET 9, C#, xUnit, Microsoft.Extensions.DependencyInjection, UglyToad.PdfPig, DocumentFormat.OpenXml, BenchmarkDotNet.

**Spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md`

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props` — do NOT set `<TargetFramework>` in new csproj files.
- **Central Package Management:** every NuGet version lives in `Directory.Packages.props`; `<PackageReference>` in csproj carries **no** `Version` attribute (STD-005 §11.2).
- **Parsers MUST be `sealed`.** `CanParse` is pure: no I/O, never throws, deterministic for a given input.
- **Parser responsibility (hard rule):** extract text + lightweight metadata from the stream only. NO chunking, tokenization, embedding, summarization, or AI processing.
- **Stream ownership:** parsers MUST NOT dispose or close the content stream (use `leaveOpen: true` on any reader).
- **Failure signaling:** a parser signals failure by **throwing** with a clear message — `ParserDispatcher` catches all non-cancellation exceptions and converts to `ParseResult<Document>.Failed(ex.Message)`. Empty/whitespace `PlainText` becomes `Empty`. `OperationCanceledException` must propagate.
- **Parser package isolation:** `Ferret.Parsers.Pdf` and `Ferret.Parsers.Office` must NOT reference each other, and `Ferret.ParserPlatform` must NOT reference either (no heavyweight deps in the platform).
- **StyleCop:** analyzers apply to all projects; public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.
- **New projects** must be added to `src/Ferret.sln` via `dotnet sln src/Ferret.sln add <path>`.

---

### Task 1: MediaCategory content model (Ferret.Core)

**Files:**
- Create: `src/Ferret.Core/Documents/MediaCategory.cs`
- Modify: `src/Ferret.Core/Documents/MediaTypeInfo.cs`
- Test: `tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs`

**Interfaces:**
- Produces: `enum MediaCategory { Text, BinaryParseable, BinaryOpaque }`; `MediaTypeInfo.Category` (required init); computed `IsText`/`IsBinary` derived from `Category`.

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

- [ ] **Step 7: Commit**

```bash
git add src/Ferret.Core/Documents/MediaCategory.cs src/Ferret.Core/Documents/MediaTypeInfo.cs tests/Ferret.Core.Tests/Documents/MediaTypeInfoTests.cs
git commit -m "feat(core): add MediaCategory and derive MediaTypeInfo flags from it"
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
```

`.xlsx`/`.pptx` stay `Binary()` (opaque this milestone). Add the expanded text/code/config entries:

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
git commit -m "feat(parsers): map PDF/DOCX to parseable-binary media types, expand text and binary maps"
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
  <PackageVersion Include="UglyToad.PdfPig" Version="0.1.9" />
</ItemGroup>
```

(Verify `0.1.9` is the latest stable at implementation time; bump if newer.)

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
        var parser = new PdfParser();
        Assert.True(parser.CanParse("application/pdf"));
        Assert.False(parser.CanParse("text/plain"));
        Assert.False(parser.CanParse("application/octet-stream"));
    }

    [Fact]
    public async Task ParseAsync_Extracts_Text()
    {
        var parser = new PdfParser();
        using var stream = MakePdf("Hello enterprise document");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.Contains("Hello", doc.PlainText, StringComparison.Ordinal);
        Assert.Equal(DocumentKind.Prose, doc.Kind);
        Assert.Equal("application/pdf", doc.MediaType);
    }

    [Fact]
    public async Task ParseAsync_Sets_PageCount_Metadata()
    {
        var parser = new PdfParser();
        using var stream = MakePdf("page one text");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.Equal("1", doc.Metadata["PageCount"]);
    }

    [Fact]
    public async Task ParseAsync_Does_Not_Dispose_Stream()
    {
        var parser = new PdfParser();
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

        var metadata = BuildMetadata(pdf);

        var document = new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = PdfMediaType,
            Kind = DocumentKind.Prose,
            PlainText = sb.ToString().Trim(),
            Title = string.IsNullOrWhiteSpace(pdf.Information.Title) ? null : pdf.Information.Title,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
            Metadata = metadata,
        };

        return ValueTask.FromResult(document);
    }

    private static IReadOnlyDictionary<string, string> BuildMetadata(PdfDocument pdf)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PageCount"] = pdf.NumberOfPages.ToString(CultureInfo.InvariantCulture),
        };

        Add(map, "Author", pdf.Information.Author);
        Add(map, "Subject", pdf.Information.Subject);
        Add(map, "Keywords", pdf.Information.Keywords);
        Add(map, "Created", pdf.Information.CreationDate);
        Add(map, "Modified", pdf.Information.ModifiedDate);
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

namespace Ferret.Parsers.Pdf;

/// <summary>DI registration for the PDF parser package.</summary>
public static class PdfParserModule
{
    /// <summary>Registers <see cref="PdfParser"/> as an <see cref="IContentParser"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
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

### Task 4: Ferret.Parsers.Office — WordParser (OpenXml, DOCX only)

**Files:**
- Modify: `Directory.Packages.props` (add `DocumentFormat.OpenXml`)
- Create: `src/Ferret.Parsers.Office/Ferret.Parsers.Office.csproj`
- Create: `src/Ferret.Parsers.Office/OfficeMediaTypes.cs`
- Create: `src/Ferret.Parsers.Office/WordParser.cs`
- Create: `src/Ferret.Parsers.Office/OfficeParserModule.cs`
- Create: `tests/Ferret.Parsers.Office.Tests/Ferret.Parsers.Office.Tests.csproj`
- Create: `tests/Ferret.Parsers.Office.Tests/WordParserTests.cs`

**Interfaces:**
- Produces: `public static class OfficeMediaTypes { public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; }`; `public sealed class WordParser : IContentParser`; `public static class OfficeParserModule { static void ConfigureServices(IServiceCollection); }`.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`:

```xml
<ItemGroup Label="Office (OpenXML) parsing">
  <PackageVersion Include="DocumentFormat.OpenXml" Version="3.1.0" />
</ItemGroup>
```

(Verify latest stable 3.x at implementation time.)

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
        var parser = new WordParser();
        Assert.True(parser.CanParse(OfficeMediaTypes.Docx));
        Assert.False(parser.CanParse("application/pdf"));
        Assert.False(parser.CanParse("application/msword")); // legacy .doc unsupported
    }

    [Fact]
    public async Task ParseAsync_Extracts_Paragraph_And_Table_Text()
    {
        var parser = new WordParser();
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

        var metadata = BuildMetadata(word);
        var props = word.PackageProperties;

        var document = new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = OfficeMediaTypes.Docx,
            Kind = DocumentKind.Prose,
            PlainText = sb.ToString().Trim(),
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

    private static IReadOnlyDictionary<string, string> BuildMetadata(WordprocessingDocument word)
    {
        var props = word.PackageProperties;
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        Add(map, "Author", props.Creator);
        Add(map, "Subject", props.Subject);
        Add(map, "Keywords", props.Keywords);
        Add(map, "Category", props.Category);
        Add(map, "Created", props.Created?.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
        Add(map, "Modified", props.Modified?.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
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

namespace Ferret.Parsers.Office;

/// <summary>DI registration for the Office parser package. Word (.docx) only this milestone.</summary>
public static class OfficeParserModule
{
    /// <summary>Registers <see cref="WordParser"/> as an <see cref="IContentParser"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IContentParser, WordParser>();
    }
}
```

- [ ] **Step 8: Add projects to the solution, build, and run tests**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Parsers.Office/Ferret.Parsers.Office.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Parsers.Office.Tests/Ferret.Parsers.Office.Tests.csproj
dotnet test tests/Ferret.Parsers.Office.Tests
```

Expected: PASS (2 tests).

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props src/Ferret.Parsers.Office tests/Ferret.Parsers.Office.Tests src/Ferret.sln
git commit -m "feat(parsers): add Ferret.Parsers.Office with OpenXml-based WordParser (docx)"
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
using Ferret.Core.Documents;
using Ferret.Parsers;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers.Tests;

public sealed class ParserPackModuleTests
{
    [Fact]
    public void Registers_All_Five_Parsers_And_Resolves_Pdf_And_Docx()
    {
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var parsers = provider.GetServices<IContentParser>().ToList();
        Assert.Equal(5, parsers.Count); // PlainText, Markdown, Json, Pdf, Word

        var registry = provider.GetRequiredService<IParserRegistry>();
        Assert.NotNull(registry.GetParserFor("application/pdf"));
        Assert.NotNull(registry.GetParserFor("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        Assert.NotNull(registry.GetParserFor("text/x-csharp")); // built-in plain text still works
    }
}
```

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
Expected: output includes a line naming the 5 installed parsers and the supported-extension count.

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
- Create: `tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs`
- Create: `tests/Ferret.Benchmarks.Tests/Corpus/CorpusGeneratorTests.cs` (new test project, see Step 8)

> **Note on test placement:** `Ferret.Benchmarks` is a console (BenchmarkDotNet) project, not a test project. Put the generator's unit tests in a sibling `tests/Ferret.Benchmarks.Tests` xUnit project that references `Ferret.Benchmarks`.

**Interfaces:**
- Produces:
  - `sealed record CorpusBlock(CorpusBlockKind Kind, string Text)` and `enum CorpusBlockKind { Heading, Paragraph, CodeLine, KeyValue }`
  - `sealed record CorpusDocument(string Title, IReadOnlyList<CorpusBlock> Blocks)`
  - `interface IDocumentRenderer { string Extension { get; } void Render(CorpusDocument doc, Stream output); }`
  - `enum CorpusSize { Small, Medium, Enterprise }`
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
            Assert.True(Directory.Exists(Path.Join(dir, "Mixed")));
            Assert.NotEmpty(Directory.GetFiles(Path.Join(dir, "PDF"), "*.pdf"));
            Assert.NotEmpty(Directory.GetFiles(Path.Join(dir, "Word"), "*.docx"));
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

/// <summary>A logical, format-agnostic document. Renderers turn it into concrete file bytes.</summary>
public sealed record CorpusDocument(string Title, IReadOnlyList<CorpusBlock> Blocks);
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

        main.Document = new Document(body);
        word.PackageProperties.Title = doc.Title;
        word.PackageProperties.Creator = "Synthetic Corpus Generator";
    }
}
```

> **Determinism caveat:** OpenXml/Office Open XML packages embed creation timestamps by default. To keep bytes identical across runs, the generator MUST set fixed package timestamps (see Step 7). If byte-identical `.docx` proves impractical, the determinism test asserts identical **extracted text** for `.docx` instead of identical bytes — note this explicitly in the test if you take that path.

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

- [ ] **Step 7: Implement the generator (seeded, deterministic)**

```csharp
// tests/Ferret.Benchmarks/Corpus/SyntheticEnterpriseCorpusGenerator.cs
using System.Globalization;

using Ferret.Benchmarks.Corpus.Renderers;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Generates a deterministic, multi-format synthetic enterprise corpus: source code, documentation,
/// PDFs, Word documents, and a mixed repo tree. Same seed + size produces identical output.
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

    private CorpusDocument BuildDocument(Random rng, int index)
    {
        var blocks = new List<CorpusBlock>();
        var paraCount = 3 + rng.Next(5);
        for (var p = 0; p < paraCount; p++)
        {
            blocks.Add(new CorpusBlock(CorpusBlockKind.Paragraph, Sentence(rng)));
        }

        return new CorpusDocument(
            string.Create(CultureInfo.InvariantCulture, $"Document {index} {Words[rng.Next(Words.Length)]}"),
            blocks);
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

    private static (int Code, int Docs, int Pdf, int Word, int Mixed) CountsFor(CorpusSize size) => size switch
    {
        CorpusSize.Small => (100, 30, 30, 20, 20),
        CorpusSize.Medium => (1000, 300, 300, 200, 200),
        CorpusSize.Enterprise => (9000, 2000, 2000, 1000, 1000),
        _ => (100, 30, 30, 20, 20),
    };
}
```

> Implementation note for determinism: if DOCX/PDF embed wall-clock timestamps, set them to `FixedTimestamp` inside the respective renderers (e.g. `word.PackageProperties.Created = FixedTimestamp;`). Verify the determinism test passes; if byte-identical DOCX is impractical, switch that test to compare extracted text (documented in Step 1's test file).

- [ ] **Step 8: Create the benchmark-tests project and wire references**

Add to `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` (new ItemGroup):

```xml
<ItemGroup>
  <PackageReference Include="UglyToad.PdfPig" />
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

### Task 8: End-to-end integration test (index PDF + DOCX, exclude opaque binaries)

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
    public async Task Pdf_And_Docx_Are_Parsed_And_Opaque_Binaries_Are_Not()
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

        // 4. Parse one PDF and one DOCX directly through the dispatcher (full resolve path).
        var pdfPath = Directory.GetFiles(Path.Join(root, "PDF"), "*.pdf").OrderBy(p => p).First();
        var docxPath = Directory.GetFiles(Path.Join(root, "Word"), "*.docx").OrderBy(p => p).First();

        var pdfResult = await DispatchFile(dispatcher, resolver, pdfPath);
        var docxResult = await DispatchFile(dispatcher, resolver, docxPath);
        var soResult = await DispatchFile(dispatcher, resolver, Path.Join(root, "SourceCode", "native.so"));

        Assert.Equal(ParseResultKind.Success, pdfResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(pdfResult.Value!.PlainText));
        Assert.Equal(ParseResultKind.Success, docxResult.Kind);
        Assert.False(string.IsNullOrWhiteSpace(docxResult.Value!.PlainText));

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
git commit -m "test(parsers): end-to-end PDF/DOCX parsing and opaque-binary exclusion"
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

/// <summary>Measures parse throughput per document type (PDF, DOCX, code, markdown) over a Small corpus.</summary>
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

    private async Task ParseOne(string path)
    {
        var mediaType = _resolver.Resolve(Path.GetFileName(path)).MediaType;
        var asset = TestAsset.For(path, mediaType);
        await using var fs = File.OpenRead(path);
        await _dispatcher.DispatchAsync(fs, asset);
    }
}
```

> Reuse/define the same `TestAsset.For` helper used in Task 8 (place a shared copy in the benchmark project). The report should record documents/sec and MB/sec per type, and parser-time vs index-time when run through the full pipeline benchmark.

- [ ] **Step 2: Create the report skeleton**

```markdown
<!-- docs/benchmarks/parser-pack-1/README.md -->
# Parser Pack 1 — Performance Report

## Objective
Measure indexing throughput and parse cost for PDF and DOCX vs text/code.

## Environment
(CPU, RAM, .NET version, corpus size)

## Methodology
Deterministic Small/Medium corpus via SyntheticEnterpriseCorpusGenerator (seed pinned).
Run: `dotnet run -c Release --project tests/Ferret.Benchmarks`

## Raw Measurements
| Type | Docs/sec | MB/sec | Parser time | Index time |
| ---- | -------- | ------ | ----------- | ---------- |
| PDF  |          |        |             |            |
| DOCX |          |        |             |            |
| Code |          |        |             |            |

## Observations

## Future Optimization Opportunities
```

- [ ] **Step 3: Update README supported file types**

In `README.md`, add a "Supported file types" section listing: source code & text/config (via PlainText/Markdown/JSON), **PDF** (`Ferret.Parsers.Pdf`), **Word .docx** (`Ferret.Parsers.Office`), composed via `Ferret.Parsers`. Mention `ferret doctor` shows installed parsers and the supported-extension count.

- [ ] **Step 4: Build the benchmark project (compile-only verification)**

Run: `dotnet build tests/Ferret.Benchmarks -c Release`
Expected: build succeeds. (Full benchmark execution is run on demand, not in CI.)

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs docs/benchmarks/parser-pack-1/README.md README.md
git commit -m "feat(bench): add parser throughput benchmark and Parser Pack 1 docs"
```

---

## Self-Review

**Spec coverage:**
- Expanded text/code/config MIME mappings + DocumentKind → Task 2 ✅
- Expanded binary denylist → Task 2 ✅
- `Ferret.Parsers.Pdf` (PdfPig) → Task 3 ✅
- `Ferret.Parsers.Office` (DOCX only) → Task 4 ✅
- Additive MimeTypeResolver / PDF+DOCX dedicated media types → Task 2 ✅
- Parseable-binary distinct from opaque (`MediaCategory`) → Task 1 + Task 2 ✅
- `ParserPackModule` composition → Task 5 ✅
- Parser principle (text + metadata only) → enforced in Global Constraints + Tasks 3/4 ✅
- Lightweight metadata schema → Tasks 3 (PDF) + 4 (DOCX) ✅
- `GetServices<IContentParser>()` aggregation (registry untouched) → Task 5 test ✅
- `ferret doctor` parser introspection → Task 6 ✅
- Synthetic Enterprise Corpus Generator (abstract + renderers, deterministic, not committed) → Task 7 ✅
- Unit tests → Tasks 1–7; end-to-end integration test → Task 8; performance report → Task 9; docs → Task 9 ✅
- DocumentKind evolution note / Office future parsers → documented in spec; no code needed ✅
- Reserved Parser Pack 2 → spec only, no task ✅

**Placeholder scan:** No "TBD"/"handle edge cases" left; failure handling is concrete (throw → dispatcher `Failed`; empty text → `Empty`). The two genuinely environment-dependent items (exact PdfPig/OpenXml package versions; DOCX/PDF byte-determinism) are called out with explicit fallback instructions, not left vague.

**Type consistency:** `MediaCategory` (Task 1) used identically in Task 2 and Task 6. `ParserPackModule.ConfigureServices` (Task 5) consumed in Tasks 6/8/9. `SyntheticEnterpriseCorpusGenerator(int seed).Generate(CorpusSize, string)` consistent across Tasks 7/8/9. `OfficeMediaTypes.Docx` (Task 4) reused in Task 5 test. `IDocumentRenderer.Extension`/`Render` consistent across all renderers. `TestAsset.For(path, mediaType)` helper referenced in Tasks 8/9 (define once in each consuming project).
