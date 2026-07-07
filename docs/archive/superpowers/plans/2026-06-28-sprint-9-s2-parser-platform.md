# Sprint 9 — Section 2: Parser Platform

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Implement `Ferret.ParserPlatform` — the concrete parser dispatch infrastructure including `MimeTypeResolver`, `ParserRegistry`, `ParserDispatcher`, and three built-in parsers (`PlainTextParser`, `MarkdownParser`, `JsonParser`). Also updates `Ferret.Connectors.Filesystem` to populate `AssetDescriptor.MediaType` using `IMimeTypeResolver`. After this section, every file discovered by the filesystem connector carries a MIME type, and every text-format file can be parsed into a `Document` ready for indexing.

**Architecture:** `Ferret.ParserPlatform` references `Ferret.Core` only — never `Ferret.Cli`. `IMimeTypeResolver` lives in `Ferret.Core` so that `Ferret.Connectors.Filesystem` can use it without referencing `Ferret.ParserPlatform`. Parsers dispatch by `MediaType` — never inspect file extensions. `ParserRegistryBuilder` is the only construction path for `IParserRegistry`.

**ADR:** See `docs/adr/0014-document-processing-architecture.md` (written in Section 1).

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `sealed` on all concrete classes, `required` on record/class properties with no sensible default.

---

## Prerequisites

Section 1 (Core Document Contracts) must be **complete** before starting this section:
- `Ferret.Core.Documents` namespace: all 16 types added and green
- `Ferret.Core.Indexing` namespace: all 5 types added and green
- `Ferret.Core.Events.Indexing` namespace: all 7 event types added and green
- `Primitives.DocumentId.From(AssetId)` factory extension present
- `dotnet test tests/Ferret.Core.Tests` passes
- `dotnet build src/Ferret.sln` passes

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes
- `required` keyword on record/class properties with no sensible default
- `Ferret.ParserPlatform` references `Ferret.Core` only — no reference to `Ferret.Cli` or `Ferret.ConnectorPlatform`
- `Ferret.Connectors.Filesystem` references `Ferret.Core` only — `IMimeTypeResolver` comes from Core
- `CanParse(string mediaType)` is pure — no I/O, no exceptions, no side effects
- `MimeTypeResolver` uses a `static readonly Dictionary<string, string>` — data-driven, not switch statements
- JSON flattening uses deterministic (lexicographic) property ordering
- `ParserRegistryBuilder.Build` is the only construction path for `IParserRegistry`
- Parsers dispatch by `MediaType` — never re-examine file extensions downstream
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-9):`, `test(sprint-9):`, `chore(sprint-9):`
- **No intermediate commit until all Sprint 9 sections are complete** — accumulate changes, single commit at sprint end

---

## File Inventory

### New Source Files

| File | Project |
|---|---|
| `src/Ferret.ParserPlatform/Ferret.ParserPlatform.csproj` | new |
| `src/Ferret.ParserPlatform/Properties/AssemblyInfo.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/ParserRegistry.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/ParserRegistryBuilder.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/ParserDispatcher.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/MimeTypeResolver.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/Parsers/PlainTextParser.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/Parsers/MarkdownParser.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/Parsers/JsonParser.cs` | Ferret.ParserPlatform |
| `src/Ferret.ParserPlatform/ParserPlatformModule.cs` | Ferret.ParserPlatform |

### Modified Source Files

| File | Change |
|---|---|
| `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` | Accept `IMimeTypeResolver`; populate `MediaType` in `BuildDescriptor` |
| `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs` | Accept `IMimeTypeResolver` via DI constructor injection |
| `src/Ferret.sln` | Add Ferret.ParserPlatform and Ferret.ParserPlatform.Tests projects |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.ParserPlatform.Tests/Ferret.ParserPlatform.Tests.csproj` | new |
| `tests/Ferret.ParserPlatform.Tests/Fakes/FakeContentParser.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/ParserRegistryTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/ParserRegistryBuilderTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/ParserDispatcherTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/Parsers/PlainTextParserTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/Parsers/MarkdownParserTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.ParserPlatform.Tests/Parsers/JsonParserTests.cs` | Ferret.ParserPlatform.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorMediaTypeTests.cs` | Ferret.Connectors.Filesystem.Tests |

---

## Task 1: Project Scaffold

**Why first:** All subsequent tasks require the `Ferret.ParserPlatform` project and test project to exist with the correct project references and solution registration.

**Files:**
- Create: `src/Ferret.ParserPlatform/Ferret.ParserPlatform.csproj`
- Create: `src/Ferret.ParserPlatform/Properties/AssemblyInfo.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/Ferret.ParserPlatform.Tests.csproj`
- Create: `tests/Ferret.ParserPlatform.Tests/Fakes/FakeContentParser.cs`
- Modify: `src/Ferret.sln`

**Interfaces:**
- Produces: compilable project skeleton — consumed by all subsequent tasks

- [ ] **Step 1: Create `Ferret.ParserPlatform.csproj`**

`src/Ferret.ParserPlatform/Ferret.ParserPlatform.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisMode>All</AnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Ferret.ParserPlatform</RootNamespace>
    <AssemblyName>Ferret.ParserPlatform</AssemblyName>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create `Properties/AssemblyInfo.cs`**

`src/Ferret.ParserPlatform/Properties/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.ParserPlatform.Tests")]
```

- [ ] **Step 3: Create `Ferret.ParserPlatform.Tests.csproj`**

`tests/Ferret.ParserPlatform.Tests/Ferret.ParserPlatform.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.ParserPlatform.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ParserPlatform\Ferret.ParserPlatform.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Add projects to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.ParserPlatform/Ferret.ParserPlatform.csproj
dotnet sln src/Ferret.sln add tests/Ferret.ParserPlatform.Tests/Ferret.ParserPlatform.Tests.csproj
```

- [ ] **Step 5: Create `Fakes/FakeContentParser.cs`**

`tests/Ferret.ParserPlatform.Tests/Fakes/FakeContentParser.cs`:

```csharp
using Ferret.Core.Documents;

namespace Ferret.ParserPlatform.Tests.Fakes;

/// <summary>Test double for IContentParser. Used in registry and dispatcher tests.</summary>
internal sealed class FakeContentParser : IContentParser
{
    internal FakeContentParser(string mediaType, int priority = 100)
    {
        Descriptor = new ParserDescriptor
        {
            Id = new ParserId(mediaType),
            Name = $"{mediaType} parser",
            Version = "1.0",
            SupportedMediaTypes = [mediaType],
            Capabilities = [ParserCapabilities.PlainTextExtraction],
            Priority = priority,
        };
    }

    public ParserDescriptor Descriptor { get; }

    public bool CanParse(string mediaType) =>
        mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

    public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
        throw new NotImplementedException("FakeContentParser.ParseAsync not used in registry tests");
}
```

- [ ] **Step 6: Verify scaffold compiles**

```
dotnet build src/Ferret.ParserPlatform/Ferret.ParserPlatform.csproj
dotnet build tests/Ferret.ParserPlatform.Tests/Ferret.ParserPlatform.Tests.csproj
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

---

## Task 2: `ParserRegistry` + `ParserRegistryBuilder`

**Why:** `IParserRegistry` is the read interface used by `ParserDispatcher`. `ParserRegistryBuilder.Build` is the only construction path — enforces uniqueness invariants at startup rather than at dispatch time.

**Files:**
- Create: `src/Ferret.ParserPlatform/ParserRegistry.cs`
- Create: `src/Ferret.ParserPlatform/ParserRegistryBuilder.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/ParserRegistryTests.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/ParserRegistryBuilderTests.cs`

**Interfaces:**
- Consumes: `IContentParser`, `IParserRegistry`, `ParserDescriptor`, `ParserId` (all from `Ferret.Core.Documents`), `FakeContentParser` (test fake)
- Produces: `ParserRegistry`, `ParserRegistryBuilder` — consumed by Task 3 (ParserDispatcher), Task 8 (ParserPlatformModule)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.ParserPlatform.Tests/ParserRegistryTests.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Tests.Fakes;
using Xunit;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserRegistryTests
{
    [Fact]
    public void GetAll_Returns_Descriptors_In_Priority_Descending_Order()
    {
        var low = new FakeContentParser("text/low", priority: 100);
        var high = new FakeContentParser("text/high", priority: 200);
        var registry = ParserRegistryBuilder.Build([low, high]);

        var all = registry.GetAll();

        Assert.Equal(200, all[0].Priority);
        Assert.Equal(100, all[1].Priority);
    }

    [Fact]
    public void GetById_Returns_Correct_Descriptor()
    {
        var parser = new FakeContentParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);

        var result = registry.GetById(new ParserId("text/plain"));

        Assert.NotNull(result);
        Assert.Equal("text/plain", result.Id.Value);
    }

    [Fact]
    public void GetById_Returns_Null_For_Unknown_Id()
    {
        var registry = ParserRegistryBuilder.Build([new FakeContentParser("text/plain")]);

        Assert.Null(registry.GetById(new ParserId("text/markdown")));
    }

    [Fact]
    public void GetParserFor_Returns_Highest_Priority_Compatible_Parser()
    {
        var low = new FakeContentParser("text/plain", priority: 100);
        var high = new FakeContentParser("text/plain", priority: 200);
        // Build with duplicate (MediaType, Priority) must throw — use distinct priorities here
        var registry = ParserRegistryBuilder.Build([low, high]);

        var result = registry.GetParserFor("text/plain");

        Assert.NotNull(result);
        Assert.Equal(200, result.Descriptor.Priority);
    }

    [Fact]
    public void GetParserFor_Returns_Null_When_No_Parser_Matches()
    {
        var registry = ParserRegistryBuilder.Build([new FakeContentParser("text/plain")]);

        Assert.Null(registry.GetParserFor("application/json"));
    }

    [Fact]
    public void GetParserFor_Is_Case_Insensitive()
    {
        var registry = ParserRegistryBuilder.Build([new FakeContentParser("text/plain")]);

        Assert.NotNull(registry.GetParserFor("TEXT/PLAIN"));
    }

    [Fact]
    public void Empty_Registry_Returns_Null_From_GetParserFor()
    {
        var registry = ParserRegistryBuilder.Build([]);

        Assert.Null(registry.GetParserFor("text/plain"));
    }

    [Fact]
    public void Empty_Registry_Returns_Empty_From_GetAll()
    {
        var registry = ParserRegistryBuilder.Build([]);

        Assert.Empty(registry.GetAll());
    }
}
```

Create `tests/Ferret.ParserPlatform.Tests/ParserRegistryBuilderTests.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Tests.Fakes;
using Xunit;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserRegistryBuilderTests
{
    [Fact]
    public void Build_Throws_On_Duplicate_ParserId()
    {
        var a = new FakeContentParser("text/plain", priority: 100);
        var b = new FakeContentParser("text/plain", priority: 200);

        // Two parsers with the same ParserId (derived from mediaType in FakeContentParser)
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ParserRegistryBuilder.Build([a, b]));

        Assert.Contains("text/plain", ex.Message);
    }

    [Fact]
    public void Build_Throws_On_Duplicate_MediaType_Priority_Combination()
    {
        // Create two parsers with distinct ParserId but same (MediaType, Priority) after
        // registering via overriding Descriptor — use a custom fake with forced collision
        var colliding = new CollidingFakeParser("text/md-a", "text/shared", 200);
        var colliding2 = new CollidingFakeParser("text/md-b", "text/shared", 200);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ParserRegistryBuilder.Build([colliding, colliding2]));

        Assert.Contains("text/shared", ex.Message);
    }

    [Fact]
    public void Build_Succeeds_With_Same_MediaType_Different_Priority()
    {
        var low = new FakeContentParser("text/plain", priority: 100);
        // Must have a different ParserId to pass duplicate ParserId check
        var highOverride = new CollidingFakeParser("text/plain-override", "text/plain", 200);

        var registry = ParserRegistryBuilder.Build([low, highOverride]);

        Assert.Equal(2, registry.GetAll().Count);
    }

    [Fact]
    public void Build_With_Empty_Collection_Succeeds()
    {
        var registry = ParserRegistryBuilder.Build([]);

        Assert.Empty(registry.GetAll());
    }

    /// <summary>Parser with a distinct ParserId but a configurable CanParse media type and priority.</summary>
    private sealed class CollidingFakeParser : IContentParser
    {
        private readonly string _canParseMediaType;

        internal CollidingFakeParser(string parserId, string canParseMediaType, int priority)
        {
            _canParseMediaType = canParseMediaType;
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(parserId),
                Name = $"{parserId} parser",
                Version = "1.0",
                SupportedMediaTypes = [canParseMediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = priority,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(_canParseMediaType, StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
            throw new NotImplementedException();
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "ParserRegistryTests|ParserRegistryBuilderTests"
```

Expected: FAIL — `ParserRegistryBuilder` not found.

- [ ] **Step 3: Create `ParserRegistryBuilder.cs`**

`src/Ferret.ParserPlatform/ParserRegistryBuilder.cs`:

```csharp
using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Static factory that constructs an immutable <see cref="IParserRegistry"/> from a collection
/// of registered parsers. Mirrors RegistryBuilder in Ferret.ConnectorPlatform.
/// Validates uniqueness invariants at build time so dispatch never encounters ambiguity.
/// </summary>
public static class ParserRegistryBuilder
{
    /// <summary>
    /// Builds an <see cref="IParserRegistry"/> from the provided parsers.
    /// Parsers are ordered by priority descending at build time.
    /// </summary>
    /// <param name="parsers">The parsers to register.</param>
    /// <returns>An immutable <see cref="IParserRegistry"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two parsers share the same <see cref="ParserId"/>, or when two parsers
    /// share the same (SupportedMediaType, Priority) combination.
    /// </exception>
    public static IParserRegistry Build(IEnumerable<IContentParser> parsers)
    {
        var ordered = parsers
            .OrderByDescending(p => p.Descriptor.Priority)
            .ToList();

        ValidateDuplicateParserId(ordered);
        ValidateDuplicateMediaTypePriority(ordered);

        return new ParserRegistry(ordered);
    }

    private static void ValidateDuplicateParserId(List<IContentParser> parsers)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parser in parsers)
        {
            var id = parser.Descriptor.Id.Value;
            if (!seen.Add(id))
            {
                throw new InvalidOperationException(
                    $"Duplicate ParserId '{id}' — each parser must have a unique identifier.");
            }
        }
    }

    private static void ValidateDuplicateMediaTypePriority(List<IContentParser> parsers)
    {
        var seen = new HashSet<(string MediaType, int Priority)>();
        foreach (var parser in parsers)
        {
            foreach (var mediaType in parser.Descriptor.SupportedMediaTypes)
            {
                var key = (mediaType, parser.Descriptor.Priority);
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate (MediaType='{mediaType}', Priority={parser.Descriptor.Priority}) " +
                        $"combination — assign different priorities to parsers that handle the same media type.");
                }
            }
        }
    }
}
```

- [ ] **Step 4: Create `ParserRegistry.cs`**

`src/Ferret.ParserPlatform/ParserRegistry.cs`:

```csharp
using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Immutable registry of content parsers, ordered by priority descending.
/// Constructed exclusively via <see cref="ParserRegistryBuilder.Build"/>.
/// </summary>
internal sealed class ParserRegistry : IParserRegistry
{
    private readonly IReadOnlyList<IContentParser> _parsers;
    private readonly Dictionary<string, ParserDescriptor> _byId;

    internal ParserRegistry(IReadOnlyList<IContentParser> parsers)
    {
        _parsers = parsers;
        _byId = parsers.ToDictionary(
            p => p.Descriptor.Id.Value,
            p => p.Descriptor,
            StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ParserDescriptor> GetAll() =>
        _parsers.Select(p => p.Descriptor).ToList();

    /// <inheritdoc/>
    public ParserDescriptor? GetById(ParserId id) =>
        _byId.GetValueOrDefault(id.Value);

    /// <inheritdoc/>
    public IContentParser? GetParserFor(string mediaType) =>
        _parsers.FirstOrDefault(p => p.CanParse(mediaType));
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "ParserRegistryTests|ParserRegistryBuilderTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 3: `ParserDispatcher`

**Why:** The dispatcher is the single entry point for all parsing. It owns the null-check, empty-stream check, cancellation propagation, and failure isolation logic. No caller ever calls `IContentParser.ParseAsync` directly.

**Files:**
- Create: `src/Ferret.ParserPlatform/ParserDispatcher.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/ParserDispatcherTests.cs`

**Interfaces:**
- Consumes: `IParserRegistry` (Task 2), `IParserDispatcher`, `ParseResult<Document>`, `Document`, `ParseContext`, `AssetDescriptor` (Core)
- Produces: `ParserDispatcher` — consumed by Task 8 (ParserPlatformModule), S3 (IndexingPipeline), S5 (wire-up)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.ParserPlatform.Tests/ParserDispatcherTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Tests.Fakes;
using Xunit;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_Returns_Success_When_Parser_Registered()
    {
        var parser = new CapableParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("hello world");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ParseResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task DispatchAsync_Returns_Unsupported_When_No_Parser_Registered()
    {
        var registry = ParserRegistryBuilder.Build([]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("application/pdf"));

        Assert.Equal(ParseResultKind.Unsupported, result.Kind);
    }

    [Fact]
    public async Task DispatchAsync_Uses_OctetStream_When_Asset_Has_No_MediaType()
    {
        var registry = ParserRegistryBuilder.Build([]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");
        var asset = MakeAsset(null);

        var result = await dispatcher.DispatchAsync(stream, asset);

        Assert.Equal(ParseResultKind.Unsupported, result.Kind);
        Assert.Contains("application/octet-stream", result.Diagnostics[0].Message);
    }

    [Fact]
    public async Task DispatchAsync_Returns_Empty_When_Stream_Is_Empty()
    {
        var parser = new CapableParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = new MemoryStream();

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.Equal(ParseResultKind.Empty, result.Kind);
    }

    [Fact]
    public async Task DispatchAsync_Returns_Failed_When_Parser_Throws()
    {
        var parser = new ThrowingParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.Equal(ParseResultKind.Failed, result.Kind);
        Assert.Contains("simulated failure", result.Diagnostics[0].Message);
    }

    [Fact]
    public async Task DispatchAsync_Propagates_OperationCanceledException()
    {
        var parser = new CancellingParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("content");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.DispatchAsync(stream, MakeAsset("text/plain"), cts.Token).AsTask());
    }

    [Fact]
    public async Task DispatchAsync_Returns_Empty_When_Document_PlainText_Is_Whitespace()
    {
        var parser = new WhitespaceParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);
        var dispatcher = new ParserDispatcher(registry);
        using var stream = MakeStream("   \n\t  ");

        var result = await dispatcher.DispatchAsync(stream, MakeAsset("text/plain"));

        Assert.Equal(ParseResultKind.Empty, result.Kind);
    }

    private static MemoryStream MakeStream(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }

    private static AssetDescriptor MakeAsset(string? mediaType) => new()
    {
        Id = AssetId.From(new Uri("filesystem:///src/test.txt")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///src/test.txt"),
        DisplayName = "test.txt",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = mediaType,
    };

    private static Document MakeDocument(AssetDescriptor asset, string plainText) => new()
    {
        Id = DocumentId.From(asset.Id),
        SourceAssetId = asset.Id,
        ConnectorId = asset.ConnectorId,
        InstanceId = asset.InstanceId,
        MediaType = asset.MediaType ?? "text/plain",
        Kind = DocumentKind.Unknown,
        PlainText = plainText,
        ProducedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>Returns a minimal valid Document with non-empty PlainText.</summary>
    private sealed class CapableParser : IContentParser
    {
        internal CapableParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Capable",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct)
        {
            var doc = new Document
            {
                Id = DocumentId.From(context.Asset.Id),
                SourceAssetId = context.Asset.Id,
                ConnectorId = context.Asset.ConnectorId,
                InstanceId = context.Asset.InstanceId,
                MediaType = context.Asset.MediaType ?? "text/plain",
                Kind = DocumentKind.Unknown,
                PlainText = "parsed content",
                ProducedAt = DateTimeOffset.UtcNow,
            };
            return ValueTask.FromResult(doc);
        }
    }

    private sealed class ThrowingParser : IContentParser
    {
        internal ThrowingParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Throwing",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
            throw new InvalidOperationException("simulated failure");
    }

    private sealed class CancellingParser : IContentParser
    {
        internal CancellingParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Cancelling",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            throw new InvalidOperationException("should not reach here");
        }
    }

    private sealed class WhitespaceParser : IContentParser
    {
        internal WhitespaceParser(string mediaType)
        {
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(mediaType),
                Name = "Whitespace",
                Version = "1.0",
                SupportedMediaTypes = [mediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = 100,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct)
        {
            var doc = new Document
            {
                Id = DocumentId.From(context.Asset.Id),
                SourceAssetId = context.Asset.Id,
                ConnectorId = context.Asset.ConnectorId,
                InstanceId = context.Asset.InstanceId,
                MediaType = context.Asset.MediaType ?? "text/plain",
                Kind = DocumentKind.Unknown,
                PlainText = "   \n\t  ",
                ProducedAt = DateTimeOffset.UtcNow,
            };
            return ValueTask.FromResult(doc);
        }
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "ParserDispatcherTests"
```

Expected: FAIL — `ParserDispatcher` not found.

- [ ] **Step 3: Create `ParserDispatcher.cs`**

`src/Ferret.ParserPlatform/ParserDispatcher.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Routes parse requests to the highest-priority compatible <see cref="IContentParser"/>
/// based on <see cref="AssetDescriptor.MediaType"/>. Never throws — all failure modes
/// are expressed as <see cref="ParseResultKind"/> values.
/// OperationCanceledException is the only exception that propagates.
/// </summary>
public sealed class ParserDispatcher : IParserDispatcher
{
    private readonly IParserRegistry _registry;

    /// <summary>Initializes a new <see cref="ParserDispatcher"/>.</summary>
    /// <param name="registry">The parser registry to dispatch against.</param>
    public ParserDispatcher(IParserRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public async ValueTask<ParseResult<Document>> DispatchAsync(
        Stream content,
        AssetDescriptor asset,
        CancellationToken ct = default)
    {
        var mediaType = asset.MediaType ?? "application/octet-stream";

        var parser = _registry.GetParserFor(mediaType);
        if (parser is null)
        {
            return ParseResult<Document>.Unsupported(mediaType);
        }

        if (content.Length == 0)
        {
            return ParseResult<Document>.Empty();
        }

        try
        {
            var document = await parser.ParseAsync(content, ParseContext.For(asset), ct)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(document.PlainText))
            {
                return ParseResult<Document>.Empty();
            }

            return ParseResult<Document>.Success(document);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ParseResult<Document>.Failed(ex.Message);
        }
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "ParserDispatcherTests|ParserRegistryTests|ParserRegistryBuilderTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 4: `MimeTypeResolver` + `FilesystemConnector` MediaType Update

**Why:** `MimeTypeResolver` is the concrete implementation of `IMimeTypeResolver` — the resolver that lives in `Ferret.ParserPlatform`. `FilesystemConnector` must populate `AssetDescriptor.MediaType` at discovery time so the dispatcher has MIME type context without needing to re-read files.

**Files:**
- Create: `src/Ferret.ParserPlatform/MimeTypeResolver.cs`
- Modify: `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs`
- Modify: `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorMediaTypeTests.cs`

**Interfaces:**
- Consumes: `IMimeTypeResolver`, `MediaTypeInfo`, `DocumentKind` (Core), `AssetDescriptor` (Connectors)
- Produces: `MimeTypeResolver` — consumed by `FilesystemConnectorFactory` (DI), Task 8 (module registration), S3 (IndexingPipeline skips binary assets)

- [ ] **Step 1: Write failing tests for MimeTypeResolver**

Create `tests/Ferret.ParserPlatform.Tests/MimeTypeResolverTests.cs`:

```csharp
using Ferret.Core.Documents;
using Xunit;

namespace Ferret.ParserPlatform.Tests;

public sealed class MimeTypeResolverTests
{
    private readonly MimeTypeResolver _resolver = new();

    [Theory]
    [InlineData(".md", "text/markdown", true, false)]
    [InlineData(".markdown", "text/markdown", true, false)]
    [InlineData(".txt", "text/plain", true, false)]
    [InlineData(".json", "application/json", true, false)]
    [InlineData(".cs", "text/x-csharp", true, false)]
    [InlineData(".py", "text/x-python", true, false)]
    [InlineData(".js", "text/javascript", true, false)]
    [InlineData(".ts", "text/typescript", true, false)]
    [InlineData(".yaml", "text/yaml", true, false)]
    [InlineData(".yml", "text/yaml", true, false)]
    [InlineData(".toml", "application/toml", true, false)]
    [InlineData(".sh", "text/x-sh", true, false)]
    [InlineData(".ps1", "text/x-powershell", true, false)]
    [InlineData(".sql", "text/x-sql", true, false)]
    [InlineData(".go", "text/x-go", true, false)]
    [InlineData(".rs", "text/x-rust", true, false)]
    [InlineData(".csv", "text/csv", true, false)]
    [InlineData(".html", "text/html", true, false)]
    [InlineData(".xml", "text/xml", true, false)]
    [InlineData(".css", "text/css", true, false)]
    [InlineData(".proto", "text/x-protobuf", true, false)]
    [InlineData(".tf", "text/x-terraform", true, false)]
    [InlineData(".graphql", "text/x-graphql", true, false)]
    public void Resolve_Returns_Correct_MediaType_For_Text_Extension(
        string ext, string expectedMediaType, bool expectedIsText, bool expectedIsBinary)
    {
        var result = _resolver.Resolve($"file{ext}");

        Assert.Equal(expectedMediaType, result.MediaType);
        Assert.Equal(expectedIsText, result.IsText);
        Assert.Equal(expectedIsBinary, result.IsBinary);
    }

    [Theory]
    [InlineData(".dll")]
    [InlineData(".exe")]
    [InlineData(".pdb")]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".zip")]
    [InlineData(".pdf")]
    [InlineData(".mp4")]
    public void Resolve_Returns_OctetStream_For_Binary_Extension(string ext)
    {
        var result = _resolver.Resolve($"file{ext}");

        Assert.Equal("application/octet-stream", result.MediaType);
        Assert.True(result.IsBinary);
        Assert.False(result.IsText);
        Assert.Equal(1.0, result.Confidence);
    }

    [Fact]
    public void Resolve_Returns_Correct_SuggestedKind_For_Code()
    {
        var result = _resolver.Resolve("Program.cs");

        Assert.Equal(Ferret.Core.Documents.DocumentKind.Code, result.SuggestedKind);
    }

    [Fact]
    public void Resolve_Returns_Correct_SuggestedKind_For_Prose()
    {
        var result = _resolver.Resolve("README.md");

        Assert.Equal(Ferret.Core.Documents.DocumentKind.Prose, result.SuggestedKind);
    }

    [Fact]
    public void Resolve_Returns_Correct_SuggestedKind_For_Config()
    {
        var result = _resolver.Resolve("config.yaml");

        Assert.Equal(Ferret.Core.Documents.DocumentKind.Config, result.SuggestedKind);
    }

    [Fact]
    public void Resolve_Returns_Correct_SuggestedKind_For_Data()
    {
        var result = _resolver.Resolve("data.json");

        Assert.Equal(Ferret.Core.Documents.DocumentKind.Data, result.SuggestedKind);
    }

    [Fact]
    public void Resolve_Returns_PlainText_With_Low_Confidence_For_Unknown_Extension()
    {
        var result = _resolver.Resolve("file.unknownxyz");

        Assert.Equal("text/plain", result.MediaType);
        Assert.True(result.IsText);
        Assert.Equal(0.5, result.Confidence);
    }

    [Fact]
    public void Resolve_Never_Throws_For_Any_Input()
    {
        // Edge cases: no extension, just dot, empty-ish names
        var ex1 = Record.Exception(() => _resolver.Resolve("noextension"));
        var ex2 = Record.Exception(() => _resolver.Resolve("."));
        var ex3 = Record.Exception(() => _resolver.Resolve("file."));
        var ex4 = Record.Exception(() => _resolver.Resolve(string.Empty));

        Assert.Null(ex1);
        Assert.Null(ex2);
        Assert.Null(ex3);
        Assert.Null(ex4);
    }

    [Fact]
    public void Resolve_Is_Case_Insensitive_For_Extension()
    {
        var lower = _resolver.Resolve("file.md");
        var upper = _resolver.Resolve("FILE.MD");

        Assert.Equal(lower.MediaType, upper.MediaType);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "MimeTypeResolverTests"
```

Expected: FAIL — `MimeTypeResolver` not found.

- [ ] **Step 3: Create `MimeTypeResolver.cs`**

`src/Ferret.ParserPlatform/MimeTypeResolver.cs`:

```csharp
using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Resolves MIME type metadata from a file name by extension lookup.
/// Uses a static dictionary — no I/O, no external libraries, deterministic.
/// Unknown non-binary extensions default to text/plain with Confidence=0.5.
/// Resolution happens once at the connector edge; downstream code uses AssetDescriptor.MediaType.
/// </summary>
public sealed class MimeTypeResolver : IMimeTypeResolver
{
    private static readonly Dictionary<string, MediaTypeInfo> s_map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Prose
            [".txt"]      = Text("text/plain",                    DocumentKind.Unknown),
            [".md"]       = Text("text/markdown",                 DocumentKind.Prose),
            [".markdown"] = Text("text/markdown",                 DocumentKind.Prose),

            // Data
            [".json"]     = Text("application/json",              DocumentKind.Data),
            [".jsonc"]    = Text("application/json",              DocumentKind.Data),
            [".csv"]      = Text("text/csv",                      DocumentKind.Data),
            [".tsv"]      = Text("text/tab-separated-values",     DocumentKind.Data),
            [".xml"]      = Text("text/xml",                      DocumentKind.Data),

            // Config
            [".yaml"]     = Text("text/yaml",                     DocumentKind.Config),
            [".yml"]      = Text("text/yaml",                     DocumentKind.Config),
            [".toml"]     = Text("application/toml",              DocumentKind.Config),
            [".proto"]    = Text("text/x-protobuf",               DocumentKind.Config),
            [".tf"]       = Text("text/x-terraform",              DocumentKind.Config),
            [".graphql"]  = Text("text/x-graphql",                DocumentKind.Config),

            // Code — general web
            [".html"]     = Text("text/html",                     DocumentKind.Prose),
            [".htm"]      = Text("text/html",                     DocumentKind.Prose),
            [".css"]      = Text("text/css",                      DocumentKind.Code),
            [".js"]       = Text("text/javascript",               DocumentKind.Code),
            [".jsx"]      = Text("text/javascript",               DocumentKind.Code),
            [".ts"]       = Text("text/typescript",               DocumentKind.Code),
            [".tsx"]      = Text("text/typescript",               DocumentKind.Code),
            [".vue"]      = Text("text/x-vue",                    DocumentKind.Code),
            [".razor"]    = Text("text/x-razor",                  DocumentKind.Code),
            [".cshtml"]   = Text("text/x-razor",                  DocumentKind.Code),

            // Code — .NET / JVM
            [".cs"]       = Text("text/x-csharp",                 DocumentKind.Code),
            [".java"]     = Text("text/x-java",                   DocumentKind.Code),
            [".kt"]       = Text("text/x-kotlin",                 DocumentKind.Code),

            // Code — scripting
            [".py"]       = Text("text/x-python",                 DocumentKind.Code),
            [".rb"]       = Text("text/x-ruby",                   DocumentKind.Code),
            [".swift"]    = Text("text/x-swift",                  DocumentKind.Code),
            [".go"]       = Text("text/x-go",                     DocumentKind.Code),
            [".rs"]       = Text("text/x-rust",                   DocumentKind.Code),

            // Code — systems
            [".c"]        = Text("text/x-c",                      DocumentKind.Code),
            [".h"]        = Text("text/x-c",                      DocumentKind.Code),
            [".cpp"]      = Text("text/x-c++",                    DocumentKind.Code),
            [".hpp"]      = Text("text/x-c++",                    DocumentKind.Code),

            // Code — shell / ops
            [".sh"]       = Text("text/x-sh",                     DocumentKind.Code),
            [".bash"]     = Text("text/x-sh",                     DocumentKind.Code),
            [".ps1"]      = Text("text/x-powershell",             DocumentKind.Code),
            [".sql"]      = Text("text/x-sql",                    DocumentKind.Code),

            // Binary — executables and objects
            [".dll"]      = Binary(),
            [".exe"]      = Binary(),
            [".pdb"]      = Binary(),
            [".obj"]      = Binary(),
            [".bin"]      = Binary(),

            // Binary — archives
            [".zip"]      = Binary(),
            [".gz"]       = Binary(),
            [".tar"]      = Binary(),
            [".7z"]       = Binary(),
            [".rar"]      = Binary(),

            // Binary — images
            [".png"]      = Binary(),
            [".jpg"]      = Binary(),
            [".jpeg"]     = Binary(),
            [".gif"]      = Binary(),
            [".bmp"]      = Binary(),
            [".ico"]      = Binary(),
            [".svg"]      = Binary(),

            // Binary — documents
            [".pdf"]      = Binary(),
            [".docx"]     = Binary(),
            [".xlsx"]     = Binary(),
            [".pptx"]     = Binary(),

            // Binary — media
            [".mp3"]      = Binary(),
            [".mp4"]      = Binary(),
            [".avi"]      = Binary(),
            [".mov"]      = Binary(),

            // Binary — fonts
            [".ttf"]      = Binary(),
            [".woff"]     = Binary(),
            [".woff2"]    = Binary(),
            [".eot"]      = Binary(),
        };

    private static readonly MediaTypeInfo s_unknownText = new()
    {
        MediaType = "text/plain",
        IsText = true,
        IsBinary = false,
        Confidence = 0.5,
    };

    /// <inheritdoc/>
    public MediaTypeInfo Resolve(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return s_unknownText;
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || ext == ".")
        {
            return s_unknownText;
        }

        return s_map.TryGetValue(ext, out var info) ? info : s_unknownText;
    }

    private static MediaTypeInfo Text(string mediaType, DocumentKind kind) => new()
    {
        MediaType = mediaType,
        IsText = true,
        IsBinary = false,
        SuggestedKind = kind,
        Confidence = 1.0,
    };

    private static MediaTypeInfo Binary() => new()
    {
        MediaType = "application/octet-stream",
        IsText = false,
        IsBinary = true,
        Confidence = 1.0,
    };
}
```

- [ ] **Step 4: Update `FilesystemConnector.cs`**

In `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs`, inject `IMimeTypeResolver` via constructor and call `Resolve` in `BuildDescriptor` (or equivalent asset creation site):

```csharp
// Add constructor parameter:
private readonly IMimeTypeResolver _mimeTypeResolver;

// In constructor — add after existing parameters:
// _mimeTypeResolver = mimeTypeResolver;

// In the method that builds AssetDescriptor (BuildDescriptor or equivalent):
MediaType = _mimeTypeResolver.Resolve(entry.Name).MediaType,
```

Read the actual constructor and descriptor-building method first, then apply the targeted edit. Do not change any existing logic beyond injecting the resolver and setting `MediaType`.

- [ ] **Step 5: Update `FilesystemConnectorFactory.cs`**

In `src/Ferret.Connectors.Filesystem/FilesystemConnectorFactory.cs`, add `IMimeTypeResolver` as a constructor parameter and pass it when constructing `FilesystemConnector`:

```csharp
// Add constructor parameter:
private readonly IMimeTypeResolver _mimeTypeResolver;

// In constructor:
// _mimeTypeResolver = mimeTypeResolver;

// Pass to FilesystemConnector constructor:
// new FilesystemConnector(config, _mimeTypeResolver, ...)
```

Read the factory constructor and `Create`/`CreateConnector` method before editing.

- [ ] **Step 6: Write FilesystemConnector MediaType tests**

Create `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorMediaTypeTests.cs`:

```csharp
using Ferret.Core.Documents;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorMediaTypeTests
{
    [Fact]
    public async Task Discovered_Md_File_Has_Markdown_MediaType()
    {
        using var tmp = new TempDirectory();
        File.WriteAllText(Path.Combine(tmp.Path, "README.md"), "# Hello");
        var resolver = new Ferret.ParserPlatform.MimeTypeResolver();

        var assets = await DiscoverAsync(tmp.Path, resolver);
        var md = assets.FirstOrDefault(a => a.DisplayName == "README.md");

        Assert.NotNull(md);
        Assert.Equal("text/markdown", md.MediaType);
    }

    [Fact]
    public async Task Discovered_Cs_File_Has_CSharp_MediaType()
    {
        using var tmp = new TempDirectory();
        File.WriteAllText(Path.Combine(tmp.Path, "Program.cs"), "// code");
        var resolver = new Ferret.ParserPlatform.MimeTypeResolver();

        var assets = await DiscoverAsync(tmp.Path, resolver);
        var cs = assets.FirstOrDefault(a => a.DisplayName == "Program.cs");

        Assert.NotNull(cs);
        Assert.Equal("text/x-csharp", cs.MediaType);
    }

    [Fact]
    public async Task Discovered_Dll_File_Has_OctetStream_MediaType()
    {
        using var tmp = new TempDirectory();
        File.WriteAllBytes(Path.Combine(tmp.Path, "lib.dll"), [0x4D, 0x5A]);
        var resolver = new Ferret.ParserPlatform.MimeTypeResolver();

        var assets = await DiscoverAsync(tmp.Path, resolver);
        var dll = assets.FirstOrDefault(a => a.DisplayName == "lib.dll");

        Assert.NotNull(dll);
        Assert.Equal("application/octet-stream", dll.MediaType);
    }

    private static async Task<List<Ferret.Core.Connectors.AssetDescriptor>> DiscoverAsync(
        string rootPath, IMimeTypeResolver resolver)
    {
        // Construct FilesystemConnector via factory using real resolver
        // Adjust this helper to match the actual FilesystemConnectorFactory API
        var factory = new FilesystemConnectorFactory(resolver);
        var config = new Ferret.Core.Workspace.ConnectorConfig
        {
            InstanceId = new Ferret.Core.Connectors.ConnectorInstanceId("test"),
            ConnectorId = new Ferret.Core.Connectors.ConnectorId("filesystem"),
            DisplayName = "test",
            IsEnabled = true,
            Settings = new Dictionary<string, string> { ["RootPath"] = rootPath },
        };
        var connector = factory.Create(config);
        var results = new List<Ferret.Core.Connectors.AssetDescriptor>();
        await foreach (var asset in connector.DiscoverAsync())
        {
            results.Add(asset);
        }
        return results;
    }
}
```

Note: Adjust `TempDirectory`, `FilesystemConnectorFactory`, `ConnectorConfig`, and `DiscoverAsync` call sites to match the actual API from `Ferret.Connectors.Filesystem`. Read those files before writing the test.

- [ ] **Step 7: Confirm green**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "MimeTypeResolverTests"
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorMediaTypeTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 5: `PlainTextParser`

**Why:** The fallback parser — handles any `text/*` media type. All files with text MIME types are parseable after this task. Lower priority (100) than format-specific parsers.

**Files:**
- Create: `src/Ferret.ParserPlatform/Parsers/PlainTextParser.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/Parsers/PlainTextParserTests.cs`

**Interfaces:**
- Consumes: `IContentParser`, `Document`, `ParseContext`, `DocumentKind`, `ParserDescriptor`, `ParserCapabilities` (Core)
- Produces: `PlainTextParser` — consumed by Task 8 (ParserPlatformModule registration), S3 (IndexingPipeline via ParserDispatcher)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.ParserPlatform.Tests/Parsers/PlainTextParserTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;
using Xunit;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class PlainTextParserTests
{
    private readonly PlainTextParser _parser = new();

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/x-csharp")]
    [InlineData("text/markdown")]
    [InlineData("text/javascript")]
    [InlineData("text/x-go")]
    [InlineData("text/yaml")]
    [InlineData("TEXT/PLAIN")]
    public void CanParse_Returns_True_For_Text_MediaTypes(string mediaType)
    {
        Assert.True(_parser.CanParse(mediaType));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/octet-stream")]
    [InlineData("image/png")]
    [InlineData("")]
    public void CanParse_Returns_False_For_Non_Text_MediaTypes(string mediaType)
    {
        Assert.False(_parser.CanParse(mediaType));
    }

    [Fact]
    public void Descriptor_Priority_Is_100()
    {
        Assert.Equal(100, _parser.Descriptor.Priority);
    }

    [Fact]
    public void Descriptor_Id_Is_Text_Plain()
    {
        Assert.Equal("text/plain", _parser.Descriptor.Id.Value);
    }

    [Fact]
    public async Task ParseAsync_PlainText_Equals_Full_Content()
    {
        var content = "Hello, world!\nSecond line.";
        using var stream = MakeStream(content);
        var context = MakeContext("text/plain");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(content, doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_CSharp_File_Has_DocumentKind_Code()
    {
        using var stream = MakeStream("class Foo { }");
        var context = MakeContext("text/x-csharp");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Code, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_PlainText_Has_DocumentKind_Unknown()
    {
        using var stream = MakeStream("some content");
        var context = MakeContext("text/plain");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Unknown, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Yaml_Has_DocumentKind_Config()
    {
        using var stream = MakeStream("key: value");
        var context = MakeContext("text/yaml");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Config, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Csv_Has_DocumentKind_Data()
    {
        using var stream = MakeStream("a,b,c\n1,2,3");
        var context = MakeContext("text/csv");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_SourceFingerprint_From_Asset()
    {
        var fingerprint = new AssetFingerprint("sha256:abc123");
        using var stream = MakeStream("content");
        var context = MakeContext("text/plain", fingerprint);

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(fingerprint, doc.SourceFingerprint);
    }

    [Fact]
    public async Task ParseAsync_Empty_Stream_Returns_Document_With_Empty_PlainText()
    {
        // Dispatcher handles the Empty result — parser just returns what it got
        using var stream = new MemoryStream();
        var context = MakeContext("text/plain");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Equal(string.Empty, doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Title_Is_Null()
    {
        using var stream = MakeStream("content");
        var context = MakeContext("text/plain");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Null(doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Sections_Are_Empty()
    {
        using var stream = MakeStream("content");
        var context = MakeContext("text/plain");

        var doc = await _parser.ParseAsync(stream, context);

        Assert.Empty(doc.Sections);
    }

    private static MemoryStream MakeStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));

    private static ParseContext MakeContext(string mediaType, AssetFingerprint? fingerprint = null)
    {
        var uri = new Uri("filesystem:///src/test.txt");
        return ParseContext.For(new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = "test.txt",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = mediaType,
            Fingerprint = fingerprint,
        });
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "PlainTextParserTests"
```

Expected: FAIL — `PlainTextParser` not found.

- [ ] **Step 3: Create `Parsers/PlainTextParser.cs`**

`src/Ferret.ParserPlatform/Parsers/PlainTextParser.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Fallback parser for any <c>text/*</c> media type. Reads content as UTF-8 and produces
/// a Document whose PlainText is the full file content.
/// Priority 100 — lower than format-specific parsers (MarkdownParser, JsonParser at 200).
/// PlainTextParser produces a plain-text representation suitable for FTS5 keyword indexing.
/// It is not a rendering engine — no formatting, syntax highlighting, or semantic analysis.
/// </summary>
public sealed class PlainTextParser : IContentParser
{
    private static readonly HashSet<string> s_codeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/x-csharp", "text/x-python", "text/javascript", "text/typescript",
        "text/x-go", "text/x-rust", "text/x-java", "text/x-kotlin", "text/x-ruby",
        "text/x-swift", "text/x-c", "text/x-c++", "text/x-sh", "text/x-sql",
        "text/x-razor", "text/x-vue", "text/x-graphql", "text/css",
    };

    private static readonly HashSet<string> s_configTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/yaml", "text/x-terraform", "text/x-powershell", "text/x-protobuf",
        "application/toml",
    };

    private static readonly HashSet<string> s_dataTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv", "text/tab-separated-values", "text/xml",
    };

    /// <summary>Gets the static descriptor for the plain-text parser.</summary>
    public static readonly ParserDescriptor PlainTextDescriptor = new()
    {
        Id = new ParserId("text/plain"),
        Name = "Plain Text Parser",
        Version = "1.0",
        SupportedMediaTypes = ["text/*"],
        Capabilities = [ParserCapabilities.PlainTextExtraction],
        Priority = 100,
    };

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => PlainTextDescriptor;

    /// <inheritdoc/>
    /// <remarks>Returns true for any media type starting with "text/".</remarks>
    public bool CanParse(string mediaType) =>
        mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(
        Stream content,
        ParseContext context,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(content, System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var mediaType = context.Asset.MediaType ?? "text/plain";
        var kind = ResolveKind(mediaType);

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = mediaType,
            Kind = kind,
            PlainText = text,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
        };
    }

    private static DocumentKind ResolveKind(string mediaType)
    {
        if (s_codeTypes.Contains(mediaType))
        {
            return DocumentKind.Code;
        }

        if (s_configTypes.Contains(mediaType))
        {
            return DocumentKind.Config;
        }

        if (s_dataTypes.Contains(mediaType))
        {
            return DocumentKind.Data;
        }

        if (mediaType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentKind.Prose;
        }

        if (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentKind.Prose;
        }

        return DocumentKind.Unknown;
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "PlainTextParserTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 6: `MarkdownParser`

**Why:** Markdown is the most common documentation format in developer projects. Priority 200 — wins over `PlainTextParser` for `text/markdown`. Produces a `PlainText` approximation by stripping markdown syntax using Regex (no external library).

**Files:**
- Create: `src/Ferret.ParserPlatform/Parsers/MarkdownParser.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/Parsers/MarkdownParserTests.cs`

**Interfaces:**
- Consumes: `IContentParser`, `Document`, `ParseContext`, `DocumentKind`, `DocumentSection` (Core)
- Produces: `MarkdownParser` — consumed by Task 8 (module registration)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.ParserPlatform.Tests/Parsers/MarkdownParserTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;
using Xunit;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class MarkdownParserTests
{
    private readonly MarkdownParser _parser = new();

    [Fact]
    public void CanParse_Returns_True_For_Text_Markdown()
    {
        Assert.True(_parser.CanParse("text/markdown"));
    }

    [Fact]
    public void CanParse_Is_Case_Insensitive()
    {
        Assert.True(_parser.CanParse("TEXT/MARKDOWN"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Text_Plain()
    {
        Assert.False(_parser.CanParse("text/plain"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Application_Json()
    {
        Assert.False(_parser.CanParse("application/json"));
    }

    [Fact]
    public void Descriptor_Priority_Is_200()
    {
        Assert.Equal(200, _parser.Descriptor.Priority);
    }

    [Fact]
    public void Descriptor_Priority_Is_Higher_Than_PlainTextParser()
    {
        Assert.True(_parser.Descriptor.Priority > PlainTextParser.PlainTextDescriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_DocumentKind_Is_Prose()
    {
        using var stream = MakeStream("# Hello\n\nContent.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Equal(DocumentKind.Prose, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Title_Is_First_H1()
    {
        using var stream = MakeStream("# My Document\n\nSome text.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Equal("My Document", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Title_Is_Null_When_No_H1()
    {
        using var stream = MakeStream("## Section\n\nText.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Null(doc.Title);
    }

    [Fact]
    public async Task ParseAsync_H1_Heading_Stripped_In_PlainText()
    {
        using var stream = MakeStream("# Introduction\n\nContent here.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.DoesNotContain("# ", doc.PlainText);
        Assert.Contains("Introduction", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Bold_Markers_Stripped_In_PlainText()
    {
        using var stream = MakeStream("This is **bold** text.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.DoesNotContain("**", doc.PlainText);
        Assert.Contains("bold", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Italic_Markers_Stripped_In_PlainText()
    {
        using var stream = MakeStream("This is *italic* text.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.DoesNotContain("*italic*", doc.PlainText);
        Assert.Contains("italic", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Link_Text_Preserved_Url_Removed()
    {
        using var stream = MakeStream("See [the docs](https://example.com) for details.");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Contains("the docs", doc.PlainText);
        Assert.DoesNotContain("https://example.com", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Image_Tags_Removed()
    {
        using var stream = MakeStream("Here is an image: ![alt text](image.png).");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.DoesNotContain("![", doc.PlainText);
        Assert.DoesNotContain("image.png", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Code_Fence_Markers_Stripped_Content_Preserved()
    {
        using var stream = MakeStream("```csharp\nvar x = 1;\n```");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.DoesNotContain("```", doc.PlainText);
        Assert.Contains("var x = 1;", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_H2_Headings_Become_Sections()
    {
        var md = "# Title\n\n## Section One\n\nContent one.\n\n## Section Two\n\nContent two.";
        using var stream = MakeStream(md);
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.True(doc.Sections.Count >= 1);
        Assert.Contains(doc.Sections, s => s.Title == "Section One" || s.Title == "Section Two");
    }

    [Fact]
    public async Task ParseAsync_Sets_SourceFingerprint_From_Asset()
    {
        var fingerprint = new AssetFingerprint("sha256:xyz");
        using var stream = MakeStream("# Doc\n\nContent.");
        var doc = await _parser.ParseAsync(stream, MakeContext(fingerprint));

        Assert.Equal(fingerprint, doc.SourceFingerprint);
    }

    private static MemoryStream MakeStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));

    private static ParseContext MakeContext(AssetFingerprint? fingerprint = null)
    {
        var uri = new Uri("filesystem:///docs/readme.md");
        return ParseContext.For(new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = "readme.md",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/markdown",
            Fingerprint = fingerprint,
        });
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "MarkdownParserTests"
```

Expected: FAIL — `MarkdownParser` not found.

- [ ] **Step 3: Create `Parsers/MarkdownParser.cs`**

`src/Ferret.ParserPlatform/Parsers/MarkdownParser.cs`:

```csharp
using System.Text.RegularExpressions;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Content parser for <c>text/markdown</c>. Priority 200 — higher than PlainTextParser (100).
/// Strips Markdown syntax using Regex to produce a plain-text approximation for FTS5 indexing.
/// MarkdownParser produces a plain-text approximation suitable for indexing.
/// It is not intended to be a full Markdown rendering engine.
/// Section extraction: H1 and H2 headings become DocumentSection entries.
/// </summary>
public sealed class MarkdownParser : IContentParser
{
    // Regex patterns for stripping — compiled once, reused per call
    private static readonly Regex s_images =
        new(@"!\[.*?\]\(.*?\)", RegexOptions.Compiled);
    private static readonly Regex s_links =
        new(@"\[([^\]]+)\]\([^\)]*\)", RegexOptions.Compiled);
    private static readonly Regex s_codeFence =
        new(@"^```[^\n]*\n?", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex s_codeFenceEnd =
        new(@"^```\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex s_inlineCode =
        new(@"`([^`]*)`", RegexOptions.Compiled);
    private static readonly Regex s_bold =
        new(@"\*\*([^*]+)\*\*|__([^_]+)__", RegexOptions.Compiled);
    private static readonly Regex s_italic =
        new(@"\*([^*]+)\*|_([^_]+)_", RegexOptions.Compiled);
    private static readonly Regex s_headings =
        new(@"^#+\s*(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex s_htmlTags =
        new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex s_hrules =
        new(@"^[-*_]{3,}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex s_h1 =
        new(@"^#\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex s_h2 =
        new(@"^##\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly ParserDescriptor s_descriptor = new()
    {
        Id = new ParserId("text/markdown"),
        Name = "Markdown Parser",
        Version = "1.0",
        SupportedMediaTypes = ["text/markdown"],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.SectionExtraction],
        Priority = 200,
    };

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => s_descriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType) =>
        mediaType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(
        Stream content,
        ParseContext context,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(content, System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var title = ExtractTitle(raw);
        var sections = ExtractSections(raw);
        var plainText = StripMarkdown(raw);

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = "text/markdown",
            Kind = DocumentKind.Prose,
            PlainText = plainText,
            Title = title,
            Sections = sections,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
        };
    }

    private static string? ExtractTitle(string raw)
    {
        var m = s_h1.Match(raw);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static IReadOnlyList<DocumentSection> ExtractSections(string raw)
    {
        var lines = raw.Split('\n');
        var sections = new List<DocumentSection>();
        var h2 = new Regex(@"^##\s+(.+)$");

        for (var i = 0; i < lines.Length; i++)
        {
            var m = h2.Match(lines[i]);
            if (m.Success)
            {
                sections.Add(new DocumentSection(m.Groups[1].Value.Trim(), string.Empty, i + 1, i + 1));
            }
        }

        return sections;
    }

    private static string StripMarkdown(string raw)
    {
        var text = raw;
        text = s_images.Replace(text, string.Empty);
        text = s_links.Replace(text, "$1");
        text = s_codeFence.Replace(text, string.Empty);
        text = s_codeFenceEnd.Replace(text, string.Empty);
        text = s_inlineCode.Replace(text, "$1");
        text = s_bold.Replace(text, m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        text = s_italic.Replace(text, m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        text = s_headings.Replace(text, "$1");
        text = s_htmlTags.Replace(text, string.Empty);
        text = s_hrules.Replace(text, string.Empty);
        return text.Trim();
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "MarkdownParserTests|PlainTextParserTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 7: `JsonParser`

**Why:** JSON is the dominant structured data format in developer projects (config files, API responses, package manifests). Priority 200 — wins over `PlainTextParser`. Produces flattened key-path output using `System.Text.Json` (BCL, no external package).

**Files:**
- Create: `src/Ferret.ParserPlatform/Parsers/JsonParser.cs`
- Create: `tests/Ferret.ParserPlatform.Tests/Parsers/JsonParserTests.cs`

**Interfaces:**
- Consumes: `IContentParser`, `Document`, `ParseContext`, `DocumentKind` (Core), `System.Text.Json.JsonDocument` (BCL)
- Produces: `JsonParser` — consumed by Task 8 (module registration)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.ParserPlatform.Tests/Parsers/JsonParserTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;
using Xunit;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class JsonParserTests
{
    private readonly JsonParser _parser = new();

    [Fact]
    public void CanParse_Returns_True_For_Application_Json()
    {
        Assert.True(_parser.CanParse("application/json"));
    }

    [Fact]
    public void CanParse_Is_Case_Insensitive()
    {
        Assert.True(_parser.CanParse("APPLICATION/JSON"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Text_Plain()
    {
        Assert.False(_parser.CanParse("text/plain"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Text_Markdown()
    {
        Assert.False(_parser.CanParse("text/markdown"));
    }

    [Fact]
    public void Descriptor_Priority_Is_200()
    {
        Assert.Equal(200, _parser.Descriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_DocumentKind_Is_Data()
    {
        using var stream = MakeStream("""{"name":"Alice"}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Simple_Object_Flattened_Sorted_By_Key()
    {
        using var stream = MakeStream("""{"name":"Alice","age":30}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        var lines = doc.PlainText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Sorted lexicographically: age before name
        Assert.Contains("age: 30", lines[0]);
        Assert.Contains("name: Alice", lines[1]);
    }

    [Fact]
    public async Task ParseAsync_Nested_Object_Uses_Dot_Notation()
    {
        using var stream = MakeStream("""{"user":{"name":"Bob","age":25}}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Contains("user.age: 25", doc.PlainText);
        Assert.Contains("user.name: Bob", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_Array_Uses_Index_Notation()
    {
        using var stream = MakeStream("""{"items":["a","b","c"]}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Contains("items[0]: a", doc.PlainText);
        Assert.Contains("items[1]: b", doc.PlainText);
        Assert.Contains("items[2]: c", doc.PlainText);
    }

    [Fact]
    public async Task ParseAsync_TopLevel_Name_Property_Becomes_Title()
    {
        using var stream = MakeStream("""{"name":"My Package","version":"1.0"}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Equal("My Package", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_TopLevel_Title_Property_Becomes_Title_When_No_Name()
    {
        using var stream = MakeStream("""{"title":"My Config","env":"prod"}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Equal("My Config", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Name_Takes_Precedence_Over_Title()
    {
        using var stream = MakeStream("""{"name":"FromName","title":"FromTitle"}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Equal("FromName", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Title_Is_Null_When_No_Name_Or_Title()
    {
        using var stream = MakeStream("""{"key":"value"}""");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.Null(doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Invalid_Json_Returns_Failed_Result()
    {
        // JsonParser throws on invalid JSON — dispatcher wraps it as Failed
        using var stream = MakeStream("not valid json {");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _parser.ParseAsync(stream, MakeContext()).AsTask());
    }

    [Fact]
    public async Task ParseAsync_Empty_Object_Returns_Document_With_Empty_PlainText()
    {
        using var stream = MakeStream("{}");
        var doc = await _parser.ParseAsync(stream, MakeContext());

        Assert.True(string.IsNullOrWhiteSpace(doc.PlainText));
    }

    [Fact]
    public async Task ParseAsync_Sets_SourceFingerprint_From_Asset()
    {
        var fingerprint = new AssetFingerprint("sha256:def456");
        using var stream = MakeStream("""{"key":"value"}""");
        var doc = await _parser.ParseAsync(stream, MakeContext(fingerprint));

        Assert.Equal(fingerprint, doc.SourceFingerprint);
    }

    private static MemoryStream MakeStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));

    private static ParseContext MakeContext(AssetFingerprint? fingerprint = null)
    {
        var uri = new Uri("filesystem:///src/package.json");
        return ParseContext.For(new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = "package.json",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "application/json",
            Fingerprint = fingerprint,
        });
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "JsonParserTests"
```

Expected: FAIL — `JsonParser` not found.

- [ ] **Step 3: Create `Parsers/JsonParser.cs`**

`src/Ferret.ParserPlatform/Parsers/JsonParser.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Content parser for <c>application/json</c>. Priority 200 — higher than PlainTextParser.
/// Flattens JSON into dot-notation key-value pairs with deterministic (lexicographic) property ordering.
/// Uses System.Text.Json (BCL) — no external package dependency.
/// </summary>
public sealed class JsonParser : IContentParser
{
    private static readonly ParserDescriptor s_descriptor = new()
    {
        Id = new ParserId("application/json"),
        Name = "JSON Parser",
        Version = "1.0",
        SupportedMediaTypes = ["application/json"],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.MetadataExtraction],
        Priority = 200,
    };

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => s_descriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType) =>
        mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(
        Stream content,
        ParseContext context,
        CancellationToken ct = default)
    {
        using var doc = await JsonDocument.ParseAsync(content, cancellationToken: ct)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        FlattenElement(doc.RootElement, string.Empty, sb);
        var plainText = sb.ToString().Trim();

        var title = ExtractTitle(doc.RootElement);

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = "application/json",
            Kind = DocumentKind.Data,
            PlainText = plainText,
            Title = title,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
        };
    }

    private static void FlattenElement(JsonElement element, string prefix, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // Sort properties lexicographically for deterministic output
                var props = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ToList();
                foreach (var prop in props)
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenElement(prop.Value, key, sb);
                }
                break;

            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenElement(item, $"{prefix}[{i}]", sb);
                    i++;
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                sb.AppendLine($"{prefix}: {element}");
                break;

            case JsonValueKind.Null:
                // Skip null values — they add no searchable content
                break;

            default:
                break;
        }
    }

    private static string? ExtractTitle(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (root.TryGetProperty("name", out var nameProp) &&
            nameProp.ValueKind == JsonValueKind.String)
        {
            return nameProp.GetString();
        }

        if (root.TryGetProperty("title", out var titleProp) &&
            titleProp.ValueKind == JsonValueKind.String)
        {
            return titleProp.GetString();
        }

        return null;
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.ParserPlatform.Tests --filter "JsonParserTests|MarkdownParserTests|PlainTextParserTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 8: `ParserPlatformModule` + Final Wiring

**Why:** The module is the DI registration point for all `Ferret.ParserPlatform` services. After this task, any host that loads the module has a fully wired `IParserDispatcher` ready for use by the indexing pipeline in Section 3.

**Files:**
- Create: `src/Ferret.ParserPlatform/ParserPlatformModule.cs`

**Interfaces:**
- Consumes: `CliModuleBase` (Ferret.Cli), `IMimeTypeResolver`, `IContentParser`, `IParserRegistry`, `IParserDispatcher` (Core), all parser implementations (Tasks 5–7)
- Produces: fully registered parser pipeline — consumed by S3 (IndexingPipeline via `IParserDispatcher`), S5 (module loading)

**Note:** `ParserPlatformModule` registers services only — no CLI commands in Sprint 9. `GetCommands()` returns an empty list. The module must follow the same base class pattern as `WorkspaceCliModule` and `ConnectorCliModule`.

- [ ] **Step 1: Read the existing module pattern**

Read `src/Ferret.Cli/Commands/Workspace/WorkspaceCliModule.cs` (or the ConnectorCliModule equivalent) to confirm the exact base class, method signatures, and namespace conventions. Apply the same pattern.

- [ ] **Step 2: Create `ParserPlatformModule.cs`**

`src/Ferret.ParserPlatform/ParserPlatformModule.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Parsers;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.ParserPlatform;

/// <summary>
/// DI module for the Parser Platform. Registers all parser services so any host
/// with this module has a fully wired <see cref="IParserDispatcher"/>.
/// No CLI commands are registered in Sprint 9.
/// </summary>
public sealed class ParserPlatformModule
{
    /// <summary>Registers all Parser Platform services into the service collection.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMimeTypeResolver, MimeTypeResolver>();
        services.AddSingleton<IContentParser, PlainTextParser>();
        services.AddSingleton<IContentParser, MarkdownParser>();
        services.AddSingleton<IContentParser, JsonParser>();
        services.AddSingleton<IParserRegistry>(sp =>
            ParserRegistryBuilder.Build(sp.GetServices<IContentParser>()));
        services.AddSingleton<IParserDispatcher, ParserDispatcher>();
    }
}
```

**Note:** If the project has a `CliModuleBase` pattern accessible without referencing `Ferret.Cli`, adjust the class to inherit it. If `CliModuleBase` is in `Ferret.Cli` and that reference would violate the Core-only constraint, keep the module as a plain static class with `ConfigureServices`. The DI registration is what matters — the CLI module wiring happens in `CoreCliModule` (Section 5 sprint).

- [ ] **Step 3: Add `Microsoft.Extensions.DependencyInjection` reference to csproj if needed**

Check whether `Ferret.Core.csproj` already transitively provides `Microsoft.Extensions.DependencyInjection`. If not, add:

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.*" />
```

to `src/Ferret.ParserPlatform/Ferret.ParserPlatform.csproj`.

- [ ] **Step 4: Verify all parsers resolve through DI**

Create a quick smoke test in `tests/Ferret.ParserPlatform.Tests/ParserPlatformModuleTests.cs`:

```csharp
using Ferret.Core.Documents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserPlatformModuleTests
{
    [Fact]
    public void ConfigureServices_Registers_IMimeTypeResolver()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IMimeTypeResolver>());
    }

    [Fact]
    public void ConfigureServices_Registers_IParserDispatcher()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IParserDispatcher>());
    }

    [Fact]
    public void ConfigureServices_Registers_IParserRegistry_With_Three_Parsers()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<IParserRegistry>();

        Assert.Equal(3, registry.GetAll().Count);
    }

    [Fact]
    public void ConfigureServices_Registers_Markdown_Parser_At_Priority_200()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<IParserRegistry>();
        var markdown = registry.GetById(new ParserId("text/markdown"));

        Assert.NotNull(markdown);
        Assert.Equal(200, markdown.Priority);
    }

    [Fact]
    public void ConfigureServices_Markdown_Parser_Wins_Over_PlainText_For_Markdown_MediaType()
    {
        var services = new ServiceCollection();
        ParserPlatformModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<IParserRegistry>();
        var parser = registry.GetParserFor("text/markdown");

        Assert.NotNull(parser);
        Assert.Equal("text/markdown", parser.Descriptor.Id.Value);
    }
}
```

- [ ] **Step 5: Confirm green — full suite**

```
dotnet test tests/Ferret.ParserPlatform.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass (ParserRegistry, ParserRegistryBuilder, ParserDispatcher, MimeTypeResolver, PlainTextParser, MarkdownParser, JsonParser, ParserPlatformModule), 0 build errors, 0 warnings.

---

## Section 2 Complete

**Outputs of Section 2:**

- `Ferret.ParserPlatform` project — 10 new source files
  - `ParserRegistry` (internal) + `ParserRegistryBuilder` (static public factory)
  - `ParserDispatcher` — routes by `MediaType`, never throws, wraps failures as `ParseResultKind`
  - `MimeTypeResolver` — 35+ extension mappings, data-driven dictionary
  - `PlainTextParser` (Priority 100) — handles all `text/*`, maps media type to `DocumentKind`
  - `MarkdownParser` (Priority 200) — `text/markdown`, H1 title, H2 sections, Regex stripping
  - `JsonParser` (Priority 200) — `application/json`, lexicographic flattening, title from `name`/`title`
  - `ParserPlatformModule` — registers all services, no CLI commands
- `Ferret.Connectors.Filesystem` updated — `MediaType` populated at discovery time via `IMimeTypeResolver`
- `Ferret.ParserPlatform.Tests` project — 9 test files, full TDD coverage of all components
- `dotnet build src/Ferret.sln` passes clean

**What Section 3 (Index Engine) depends on from here:**

- `IParserDispatcher` — call `DispatchAsync(stream, asset)` to parse each asset during the indexing pipeline
- `IParserRegistry` — optional: inspect registered parsers for progress reporting or skipping unsupported types early
- `IMimeTypeResolver` — already wired into `FilesystemConnector`; `AssetDescriptor.MediaType` is now populated
- `Document` type (from Section 1) — passed to `IIndexEngine.WriteAsync` after successful dispatch
- `IIndexEngine` (from Section 1) — write the `Document` to the SQLite FTS5 store
- `IIndexPipeline` (from Section 1) — orchestrates the full discover → parse → write loop
