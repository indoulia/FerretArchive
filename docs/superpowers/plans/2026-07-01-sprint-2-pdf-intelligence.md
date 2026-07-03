# Sprint 2 — PDF Intelligence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship end-to-end PDF indexing — a new dependency-isolated `Ferret.Parsers.Pdf` package (PdfPig), the first `Ferret.Parsers` composition project (`ParserPackModule`), and a one-time CLI wiring migration — proving `ferret index` → `ferret search` finds PDF content.

**Architecture:** PDF is the first parser that lives **outside** `Ferret.ParserPlatform` (its PdfPig dependency must never leak into the platform), so this sprint introduces two new things beyond the parser itself: (1) `Ferret.Parsers.Pdf`, a sibling package holding `PdfParser` + `PdfParserModule`; and (2) `Ferret.Parsers`, a thin composition project whose `ParserPackModule` composes the platform (registry, dispatcher, `MimeTypeResolver`, built-in text/CSV parsers) **plus** the PDF package. The single CLI callsite (`IndexCliModule.cs:68`) migrates from `ParserPlatformModule.ConfigureServices` to `ParserPackModule.ConfigureServices`. The registry already aggregates every `IContentParser` via `GetServices<IContentParser>()`, so PDF goes live the moment the CLI is wired. Sprint 1 already reclassified `.pdf` → `application/pdf` (`BinaryParseable`, `Prose`), so no resolver change is needed.

**Tech Stack:** .NET 9, C#, xUnit, Microsoft.Extensions.DependencyInjection, UglyToad.PdfPig.

**Milestone spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md`
**Parent plan (source of reused code):** `docs/superpowers/plans/2026-07-01-parser-pack-1.md` (Task 3 = PDF package; Task 5 = composition + wiring)
**Predecessor:** `docs/superpowers/plans/2026-07-01-sprint-1-parser-platform-csv.md` (must be implemented first — provides `ParserOptions`, `ExtractionLimiter`, `DocumentMetadata`, `MediaCategory`, and the `.pdf` → parseable-binary mapping).

## Global Constraints

- **Target framework:** `net9.0`, inherited from `Directory.Build.props` — do NOT set `<TargetFramework>` in any csproj.
- **Central Package Management:** every NuGet version lives in `Directory.Packages.props`; `<PackageReference>` carries **no** `Version` attribute.
- **Parser package isolation:** `Ferret.ParserPlatform` MUST NOT reference `Ferret.Parsers.Pdf`. Heavyweight deps (PdfPig) live only in the PDF package.
- **Parsers MUST be `sealed`.** `CanParse` is pure: no I/O, never throws, deterministic.
- **Parser responsibility:** extract text + lightweight metadata from the stream only. No chunking, tokenization, embedding, summarization, or AI processing.
- **Extracted-text limit:** `PdfParser` takes `ParserOptions` and applies the shared `ExtractionLimiter.ApplyCharacterLimit` (default `null` = unlimited); when exceeded, truncate `PlainText` and set `Metadata[DocumentMetadata.Truncated]="true"`.
- **Metadata keys are `DocumentMetadata.*` constants**, never raw strings.
- **Stream ownership:** parsers MUST NOT dispose/close the content stream.
- **Failure signaling:** a parser signals failure by throwing; `ParserDispatcher` converts to `Failed`. Empty/whitespace `PlainText` (image-only PDF) becomes `Empty`. `OperationCanceledException` must propagate.
- **Pinned dependency:** `UglyToad.PdfPig` `1.7.0-custom-5` (implementation deviation from the originally-planned `0.1.9`, which is not obtainable from the configured NuGet feeds — only `0.1.9-alpha001-patch1` and `1.7.0-custom-5` are exposed). The `1.7.0-custom-5` build was validated by the `Ferret.Parsers.Pdf.Tests` suite against the full reader/writer surface. **API difference adapted:** `PdfDocumentBuilder` is `IDisposable` in this build (was not in 0.1.x) — writer/fixture code disposes it. Bumping is a separate maintenance task.
- **CLI wiring is a single edit:** touch `IndexCliModule.cs:68` exactly once (Task 2). No other host callsite changes this sprint.
- **New projects** must be added to `src/Ferret.sln` via `dotnet sln src/Ferret.sln add <path>`.
- **Backward compatibility:** no breaking changes to existing indexes, parser contracts, CLI behavior, or public APIs. Existing text/markdown/JSON/CSV indexing unchanged.
- **StyleCop:** public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | `Ferret.Parsers.Pdf` package (PdfParser + module + unit tests) | `Ferret.Parsers.Pdf` (new) |
| 1b | PDF robustness (pre-open cancellation, extraction-limit test) | `Ferret.Parsers.Pdf` |
| 2 | `Ferret.Parsers` composition project + CLI wiring | `Ferret.Parsers` (new), `Ferret.Cli` |
| 3 | End-to-end PDF indexing validation | `Ferret.E2E.Tests` |

Task 1 has no dependency beyond Sprint 1. Task 1b hardens Task 1's parser (consumes only Task 1). Task 2 depends on Task 1 (its `ParserPackModule` references the PDF package). Task 3 depends on Task 2 (the published CLI binary must be wired before `ferret index` parses PDFs).

> **Forward note:** `ParserPackModule` and `Ferret.Parsers.csproj` are created here composing **platform + PDF only**. Sprint 3 (Office Intelligence) adds the Office package to both. The composition-module test asserts **5 parsers** this sprint (PlainText, Markdown, JSON, CSV, PDF); Sprint 3 updates it to 7.

---

### Task 1: Ferret.Parsers.Pdf — PdfParser (PdfPig)

**Files:**
- Modify: `Directory.Packages.props` (add `UglyToad.PdfPig`)
- Create: `src/Ferret.Parsers.Pdf/Ferret.Parsers.Pdf.csproj`
- Create: `src/Ferret.Parsers.Pdf/PdfParser.cs`
- Create: `src/Ferret.Parsers.Pdf/PdfParserModule.cs`
- Create: `tests/Ferret.Parsers.Pdf.Tests/Ferret.Parsers.Pdf.Tests.csproj`
- Create: `tests/Ferret.Parsers.Pdf.Tests/PdfParserTests.cs`

**Interfaces:**
- Consumes: `ParserOptions`, `ExtractionLimiter`, `DocumentMetadata` (Sprint 1, `Ferret.Core`); `IContentParser`, `ParserDescriptor`, `ParserId`, `ParseContext`, `Document`, `DocumentId`, `DocumentKind`, `ParserCapabilities` (`Ferret.Core`).
- Produces: `public sealed class PdfParser : IContentParser` (ctor takes `ParserOptions`; `CanParse("application/pdf")`; priority 200; `public const string PdfMediaType`); `public static class PdfParserModule { static void ConfigureServices(IServiceCollection); }` — `TryAddSingleton(new ParserOptions())` + `AddSingleton<IContentParser, PdfParser>()`.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`, add a new `ItemGroup`:

```xml
<ItemGroup Label="PDF parsing">
  <PackageVersion Include="UglyToad.PdfPig" Version="1.7.0-custom-5" />
</ItemGroup>
```

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

- [ ] **Step 3: Write the failing tests**

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

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.Parsers.Pdf.Tests`
Expected: FAIL — `PdfParser` does not exist.

- [ ] **Step 5: Create the test project file**

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

- [ ] **Step 6: Add projects to the solution**

```bash
dotnet sln src/Ferret.sln add src/Ferret.Parsers.Pdf/Ferret.Parsers.Pdf.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Parsers.Pdf.Tests/Ferret.Parsers.Pdf.Tests.csproj
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

> Image-only/scanned PDFs yield empty `page.Text`, so `PlainText` is empty → dispatcher returns `Empty` (no garbage indexed). Password-protected/corrupt PDFs make `PdfDocument.Open` throw → dispatcher returns `Failed`. Both behaviors satisfy the spec without special-casing.

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
Expected: PASS (4 tests).

- [ ] **Step 10: Commit**

```bash
git add Directory.Packages.props src/Ferret.Parsers.Pdf tests/Ferret.Parsers.Pdf.Tests src/Ferret.sln
git commit -m "feat(parsers): add Ferret.Parsers.Pdf with PdfPig-based PdfParser"
```

---

### Task 1b: PDF robustness (cancellation + extraction limit)

Two hardening changes the review flagged: a pre-open cancellation check (PdfPig's `Open` is synchronous and uncancellable, so a very large PDF must at least honor an already-cancelled token before the expensive open), and an explicit PDF extraction-limit test (Task 1 tested extraction but not the limit).

**Files:**
- Modify: `src/Ferret.Parsers.Pdf/PdfParser.cs` (add pre-open cancellation check)
- Modify: `tests/Ferret.Parsers.Pdf.Tests/PdfParserTests.cs` (add extraction-limit + cancellation tests)

**Interfaces:** unchanged (behavioral hardening only).

> **Encrypted PDFs:** password-protected PDFs are treated identically to corrupt PDFs — `PdfDocument.Open` throws, the exception propagates to `ParserDispatcher`, which returns `Failed`. No dedicated fixture test is added: PdfPig's writer cannot produce an encrypted PDF, so a real fixture would have to be committed, and the throw→`Failed` contract is already exercised by the corrupt-bytes path. This behavior is documented, not separately tested.
>
> **Future metadata (deferred):** PdfPig also exposes `Producer`, `Creator`, and PDF version. These are intentionally NOT captured — doing so would expand the now-shipped `DocumentMetadata` contract (Sprint 1, merged to `main`) and ripple through every parser, the corpus generator, and validation, for fields nothing consumes yet. Extend `DocumentMetadata` with `Producer`/`CreatorApplication`/`PdfVersion` only once metadata search becomes a supported feature.

- [ ] **Step 1: Add a pre-open cancellation check to `PdfParser`**

In `src/Ferret.Parsers.Pdf/PdfParser.cs`, add the check immediately after the null-argument guards in `ParseAsync`, before `PdfDocument.Open`:

```csharp
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        // PdfPig's Open is synchronous and cannot be cancelled mid-open; honor an already-cancelled
        // token before paying the (potentially large) open cost. Per-page cancellation follows below.
        ct.ThrowIfCancellationRequested();

        using var pdf = PdfDocument.Open(content);
```

- [ ] **Step 2: Add extraction-limit and cancellation tests**

Add to `tests/Ferret.Parsers.Pdf.Tests/PdfParserTests.cs`:

```csharp
[Fact]
public async Task ParseAsync_Honors_Configured_Extraction_Limit()
{
    var parser = new PdfParser(new ParserOptions { MaxExtractedCharacters = 10 });
    using var stream = MakePdf("This is a fairly long line of extracted PDF text well beyond ten characters.");

    var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

    Assert.True(doc.PlainText.Length <= 10);
    Assert.Equal("true", doc.Metadata[DocumentMetadata.Truncated]);
}

[Fact]
public async Task ParseAsync_Throws_When_Token_Already_Cancelled()
{
    var parser = new PdfParser(new ParserOptions());
    using var stream = MakePdf("content");
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        async () => await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")), cts.Token));
}
```

- [ ] **Step 3: Run the PDF tests**

Run: `dotnet test tests/Ferret.Parsers.Pdf.Tests`
Expected: PASS (6 tests — the 4 from Task 1 plus these 2). The limit test proves PDF honors `ExtractionLimiter` uniformly with DOCX/XLSX; the cancellation test proves an already-cancelled token short-circuits before `Open`.

- [ ] **Step 4: Commit**

```bash
git add src/Ferret.Parsers.Pdf/PdfParser.cs tests/Ferret.Parsers.Pdf.Tests/PdfParserTests.cs
git commit -m "test(parsers): honor extraction limit and pre-open cancellation in PdfParser"
```

---

### Task 2: Ferret.Parsers composition project + CLI wiring

**Files:**
- Create: `src/Ferret.Parsers/Ferret.Parsers.csproj`
- Create: `src/Ferret.Parsers/ParserPackModule.cs`
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj` (add ProjectReference to `Ferret.Parsers`)
- Modify: `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs:68`
- Create: `tests/Ferret.Parsers.Tests/Ferret.Parsers.Tests.csproj`
- Create: `tests/Ferret.Parsers.Tests/ParserPackModuleTests.cs`

**Interfaces:**
- Consumes: `ParserPlatformModule.ConfigureServices`, `PdfParserModule.ConfigureServices` (Task 1), `IParserDispatcher`, `IParserRegistry`, `IContentParser`.
- Produces: `public static class ParserPackModule { static void ConfigureServices(IServiceCollection); }` composing **platform + PDF** (Sprint 3 adds Office).

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
  </ItemGroup>

</Project>
```

> The Office ProjectReference is added in Sprint 3 — omit it here (the package does not exist yet).

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
    public void Registers_All_Five_Parsers()
    {
        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var parsers = provider.GetServices<IContentParser>().ToList();
        Assert.Equal(5, parsers.Count); // PlainText, Markdown, Json, Csv, Pdf (Office added in Sprint 3)
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

> Real PDF-file dispatch through the composed pack is asserted end-to-end in Task 3. This test covers the composed dispatcher wiring cheaply with an in-memory text stream.

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
using Ferret.Parsers.Pdf;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Parsers;

/// <summary>
/// Single composition entry point for the parser pack: the platform (registry, dispatcher,
/// MimeTypeResolver, built-in text/CSV parsers) plus the PDF parser package. Hosts call this once
/// instead of wiring each parser module individually. Sprint 3 adds the Office package here.
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
Expected: PASS (2 tests).

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
        // Parser pack — platform + PDF parser; resolves IParserDispatcher required by IIndexPipeline.
        Ferret.Parsers.ParserPackModule.ConfigureServices(services);
```

(Remove the now-unused `using Ferret.ParserPlatform;` only if no other symbol from it is referenced in the file.)

- [ ] **Step 9: Build and run the CLI test suite**

Run: `dotnet build src/Ferret.sln && dotnet test tests/Ferret.Cli.Tests`
Expected: build + tests PASS (CSV still registers through `ParserPlatformModule`, which `ParserPackModule` calls, so `.csv`/`.tsv` behavior is unchanged).

- [ ] **Step 10: Commit**

```bash
git add src/Ferret.Parsers tests/Ferret.Parsers.Tests src/Ferret.Cli/Ferret.Cli.csproj src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs src/Ferret.sln
git commit -m "feat(parsers): add ParserPackModule composition (platform + PDF) and wire it into the index command"
```

---

### Task 3: End-to-end PDF indexing validation (Ferret.E2E.Tests)

**Files:**
- Modify: `tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj` (add `UglyToad.PdfPig` for writing test PDFs)
- Modify: `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs` (add `WriteSamplePdfFilesAsync`)
- Create: `tests/Ferret.E2E.Tests/Tests/PdfIndexE2ETests.cs`

**Interfaces:**
- Consumes: `WorkspaceFixture.InitializeAsync()`, `WorkspaceFixture.RunAsync(string args, TimeSpan? timeout = null)` returning `(int ExitCode, string Stdout, string Stderr)`, `WorkspaceFixture.WorkspaceDir`, `WorkspaceFixture.DisposeAsync()` (existing).
- Produces: `WorkspaceFixture.WriteSamplePdfFilesAsync()` writing real, text-bearing PDFs into `WorkspaceDir` via PdfPig's writer.

> The E2E project drives the **published `ferret` binary** (no ProjectReference to the CLI), so this task validates that the Task-2 wiring genuinely takes effect in a shipped build.

- [ ] **Step 1: Add the PDF writer dependency to the E2E project**

In `tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj`, add to the existing `<ItemGroup>`:

```xml
<PackageReference Include="UglyToad.PdfPig" />
```

(The version resolves from `Directory.Packages.props`, added in Task 1. This package is used only to author binary test fixtures, never referenced by the CLI under test.)

- [ ] **Step 2: Add a real-PDF fixture writer to `WorkspaceFixture`**

Add this method to `tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs` (mirrors the existing `WriteSampleCsFilesAsync`). Also add the required `using` lines at the top of the file: `using UglyToad.PdfPig.Core;`, `using UglyToad.PdfPig.Fonts.Standard14Fonts;`, `using UglyToad.PdfPig.Writer;`.

```csharp
/// <summary>Writes two real, text-bearing PDFs into the workspace using PdfPig's writer.</summary>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public async Task WriteSamplePdfFilesAsync()
{
    WritePdf(
        Path.Join(WorkspaceDir, "architecture-decision.pdf"),
        "Architecture Decision Record",
        "We will adopt a streaming indexing pipeline to maximize throughput.");

    WritePdf(
        Path.Join(WorkspaceDir, "incident-report.pdf"),
        "Incident Report",
        "Root cause was a saturated connection pool during the nightly export.");

    await Task.CompletedTask.ConfigureAwait(false);
}

private static void WritePdf(string path, string title, string body)
{
    var builder = new PdfDocumentBuilder();
    var font = builder.AddStandard14Font(Standard14Font.Helvetica);
    var page = builder.AddPage(595, 842);
    page.AddText(title, 14, new PdfPoint(25, 800), font);
    page.AddText(body, 11, new PdfPoint(25, 770), font);
    File.WriteAllBytes(path, builder.Build());
}
```

- [ ] **Step 3: Write the failing E2E tests**

```csharp
// tests/Ferret.E2E.Tests/Tests/PdfIndexE2ETests.cs
using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E: index real PDFs through the published binary, then prove the text is searchable.</summary>
public sealed class PdfIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSamplePdfFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search after indexing PDFs returns exit code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_AfterPdfIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search throughput");

        Assert.Equal(0, exitCode);
    }

    /// <summary>A word from a PDF body is searchable and points at the source PDF.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_PdfBodyWord_ReturnsSourcePdf()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search throughput");

        Assert.Contains("architecture-decision.pdf", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A word from the second PDF is searchable and points at that file.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_SecondPdfWord_ReturnsIncidentReport()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search saturated");

        Assert.Contains("incident-report.pdf", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 4: Run the E2E tests to verify they pass**

Run: `dotnet test tests/Ferret.E2E.Tests --filter PdfIndexE2ETests`
Expected: PASS (3 tests). If a body word does not surface (PdfPig writer/reader whitespace differences), assert on a single distinctive lowercase token guaranteed present (`throughput`, `saturated`), or fall back to the title word `Architecture` — but prefer the body-word assertion, which proves page-text extraction.

- [ ] **Step 5: Run the full E2E suite to confirm no regression**

Run: `dotnet test tests/Ferret.E2E.Tests`
Expected: PASS (existing `IndexE2ETests` / `SearchE2ETests` / `DoctorE2ETests` unaffected — the new fixture method and test class are additive).

- [ ] **Step 6: Commit**

```bash
git add tests/Ferret.E2E.Tests/Ferret.E2E.Tests.csproj tests/Ferret.E2E.Tests/Fixtures/WorkspaceFixture.cs tests/Ferret.E2E.Tests/Tests/PdfIndexE2ETests.cs
git commit -m "test(e2e): validate PDF documents index and search end-to-end"
```

---

## Final verification

- [ ] **Full solution build + test**

Run: `dotnet build src/Ferret.sln && dotnet test src/Ferret.sln`
Expected: build clean, all tests green.

- [ ] **Manually confirm PDF is live**

Run: `dotnet run --project src/Ferret.Cli -- doctor`
Expected: `ferret doctor` succeeds (the parser-count line is added in Sprint 3; here just confirm the CLI wires `ParserPackModule` without error).

- [ ] **Acceptance criteria check**

Confirm each: `Ferret.Parsers.Pdf` builds with no PdfPig leak into the platform · `ParserPackModule` composes platform + PDF (5 parsers) · CLI migrated to `ParserPackModule` at exactly one callsite · a real PDF indexes and its body text is searchable e2e · image-only PDF → `Empty`, corrupt/encrypted PDF → `Failed` (unit-level) · PDF honors the configured extraction limit and an already-cancelled token (Task 1b) · existing text/markdown/JSON/CSV indexing unchanged · no `Version` attributes on `<PackageReference>` · PdfPig pinned to 0.1.9 · all existing tests still green.
