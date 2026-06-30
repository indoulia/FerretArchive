# Sprint 9 — Section 3: Index Engine

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Implement `Ferret.Indexing` — the SQLite FTS5 keyword index engine and full ingestion pipeline. After this section, the complete content ingestion pipeline is wired: `IConnectorRegistry` → `IAssetSource`/`IAssetReader` → `IParserDispatcher` → `IIndexEngine` → `.ferret/indexes/keyword/keyword-index.db`. Section 3 also makes retroactive non-breaking additions to Section 1 contracts (Task 1) and implements `IAssetReader` on `FilesystemConnector` (Task 3).

**Architecture:** `Ferret.Indexing` references `Ferret.Core` and `Microsoft.Data.Sqlite` only — never `Ferret.Cli` or any connector project. The db file path is injected by the CLI (S5) — `Ferret.Indexing` is never aware of workspace paths. `IAssetReader` is separate from `IAssetSource`: discovery and content retrieval are distinct concerns. `IndexPipeline` checks `connector is IAssetReader` — connectors without reading capability skip content retrieval. Storage engines never own orchestration.

**ADR:** `docs/adr/0014-document-processing-architecture.md` (Section 1) — Principle 9 and `IIndexStore` reservation added in Task 1.

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `sealed` on all concrete classes, `required` on record/class properties with no sensible default.

---

## Prerequisites

Section 2 (Parser Platform) must be **complete** before starting this section:
- `Ferret.ParserPlatform` project merged and green
- `IParserDispatcher`, `IParserRegistry`, `MimeTypeResolver` present
- `FilesystemConnector` populates `AssetDescriptor.MediaType` at discovery time
- `dotnet test` passes on all existing test projects
- `dotnet build src/Ferret.sln` passes

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes
- `required` keyword on record/class properties with no sensible default
- `Ferret.Indexing` references `Ferret.Core` and `Microsoft.Data.Sqlite` only
- `Ferret.Indexing` is NEVER aware of workspace paths — db path injected by CLI (S5)
- `IAssetReader` is separate from `IAssetSource`: discovery never implies content retrieval
- `IndexingModule` registers only `IIndexPipeline`; `IIndexEngine` is registered by S5 CLI host
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-9):`, `test(sprint-9):`, `chore(sprint-9):`
- **No intermediate commit until all Sprint 9 sections are complete** — accumulate changes, single commit at sprint end

---

## File Inventory

### New Source Files (Ferret.Core additions)

| File | Change |
|---|---|
| `src/Ferret.Core/Connectors/IAssetReader.cs` | New interface |
| `src/Ferret.Core/Events/Indexing/DocumentDiscoveredEvent.cs` | 8th indexing event |

### Modified Source Files (Ferret.Core)

| File | Change |
|---|---|
| `src/Ferret.Core/Indexing/IndexResult.cs` | Add `AssetsProcessed` field |
| `src/Ferret.Core/Indexing/IndexPipelineOptions.cs` | Add `ForceRebuild = false` |
| `src/Ferret.Core/Indexing/IIndexEngine.cs` | Replace `RebuildAsync` with `ClearAsync` |
| `docs/adr/0014-document-processing-architecture.md` | Principle 9 + IIndexStore reservation + corruption deferral note |

### New Source Files (Ferret.Indexing)

| File |
|---|
| `src/Ferret.Indexing/Ferret.Indexing.csproj` |
| `src/Ferret.Indexing/Properties/AssemblyInfo.cs` |
| `src/Ferret.Indexing/SqliteKeywordIndexEngine.cs` |
| `src/Ferret.Indexing/IndexPipeline.cs` |
| `src/Ferret.Indexing/IndexingModule.cs` |

### Modified Source Files (FilesystemConnector)

| File | Change |
|---|---|
| `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` | Implement `IAssetReader` |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.Core.Tests/Connectors/AssetReaderContractTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Indexing.Tests/Ferret.Indexing.Tests.csproj` | new |
| `tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineTests.cs` | Ferret.Indexing.Tests |
| `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs` | Ferret.Indexing.Tests |
| `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorReaderTests.cs` | Ferret.Connectors.Filesystem.Tests |

---

## Task 1: S1 Contract Updates (Retroactive Non-Breaking Additions)

**Why first:** Every subsequent task in this section depends on the revised contracts. `IAssetReader` is required before Task 3 (`FilesystemConnector`) and Task 5 (`IndexPipeline`). `ClearAsync` replacing `RebuildAsync` on `IIndexEngine` is required before Task 4 (`SqliteKeywordIndexEngine`). `AssetsProcessed` and `ForceRebuild` are required before Task 5 (`IndexPipeline`). `DocumentDiscoveredEvent` is published by `IndexPipeline` in Task 5. All additions are non-breaking — no existing types are removed.

**Files:**
- Create: `src/Ferret.Core/Connectors/IAssetReader.cs`
- Create: `src/Ferret.Core/Events/Indexing/DocumentDiscoveredEvent.cs`
- Modify: `src/Ferret.Core/Indexing/IndexResult.cs`
- Modify: `src/Ferret.Core/Indexing/IndexPipelineOptions.cs`
- Modify: `src/Ferret.Core/Indexing/IIndexEngine.cs`
- Modify: `docs/adr/0014-document-processing-architecture.md`
- Create: `tests/Ferret.Core.Tests/Connectors/AssetReaderContractTests.cs`

**Interfaces:**
- Produces: `IAssetReader`, `DocumentDiscoveredEvent`, revised `IndexResult`, revised `IndexPipelineOptions`, revised `IIndexEngine` — consumed by Tasks 3, 4, 5

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Connectors/AssetReaderContractTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class AssetReaderContractTests
{
    [Fact]
    public void IAssetReader_Is_An_Interface()
    {
        Assert.True(typeof(IAssetReader).IsInterface);
    }

    [Fact]
    public void IAssetReader_Has_OpenAsync_Method()
    {
        var method = typeof(IAssetReader).GetMethod("OpenAsync");

        Assert.NotNull(method);
    }

    [Fact]
    public void IAssetReader_OpenAsync_Returns_Task_Of_Stream()
    {
        var method = typeof(IAssetReader).GetMethod("OpenAsync");

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<Stream>), method.ReturnType);
    }

    [Fact]
    public void IAssetReader_Is_Separate_From_IAssetSource()
    {
        Assert.NotEqual(typeof(IAssetReader), typeof(IAssetSource));
        Assert.False(typeof(IAssetReader).IsAssignableTo(typeof(IAssetSource)));
        Assert.False(typeof(IAssetSource).IsAssignableTo(typeof(IAssetReader)));
    }

    [Fact]
    public void IndexResult_Has_AssetsProcessed_Property()
    {
        var prop = typeof(Ferret.Core.Indexing.IndexResult).GetProperty("AssetsProcessed");

        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop.PropertyType);
    }

    [Fact]
    public void IndexPipelineOptions_Has_ForceRebuild_Property()
    {
        var prop = typeof(Ferret.Core.Indexing.IndexPipelineOptions).GetProperty("ForceRebuild");

        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void IndexPipelineOptions_Default_ForceRebuild_Is_False()
    {
        Assert.False(Ferret.Core.Indexing.IndexPipelineOptions.Default.ForceRebuild);
    }

    [Fact]
    public void IIndexEngine_Has_ClearAsync_Not_RebuildAsync()
    {
        var clearMethod = typeof(Ferret.Core.Indexing.IIndexEngine).GetMethod("ClearAsync");
        var rebuildMethod = typeof(Ferret.Core.Indexing.IIndexEngine).GetMethod("RebuildAsync");

        Assert.NotNull(clearMethod);
        Assert.Null(rebuildMethod);
    }

    [Fact]
    public void DocumentDiscoveredEvent_Exists_In_Events_Indexing_Namespace()
    {
        var type = typeof(Ferret.Core.Events.Indexing.DocumentDiscoveredEvent);

        Assert.NotNull(type);
    }

    [Fact]
    public void DocumentDiscoveredEvent_Has_AssetId_Property()
    {
        var prop = typeof(Ferret.Core.Events.Indexing.DocumentDiscoveredEvent)
            .GetProperty("AssetId");

        Assert.NotNull(prop);
        Assert.Equal(typeof(Ferret.Core.Connectors.AssetId), prop.PropertyType);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "AssetReaderContractTests"
```

Expected: FAIL — `IAssetReader`, `DocumentDiscoveredEvent` not found; `AssetsProcessed`, `ForceRebuild`, `ClearAsync` not present.

- [ ] **Step 3: Create `IAssetReader.cs`**

`src/Ferret.Core/Connectors/IAssetReader.cs`:

```csharp
namespace Ferret.Core.Connectors;

/// <summary>
/// Provides content retrieval for discovered assets. Separate from <see cref="IAssetSource"/> (discovery).
/// A connector that implements both <see cref="IAssetSource"/> and <see cref="IAssetReader"/> supports
/// the full discover-then-read pipeline. Connectors without <see cref="IAssetReader"/> are skipped
/// during the content ingestion stage — only their asset metadata is available.
/// </summary>
public interface IAssetReader
{
    /// <summary>Opens a read-only stream for the asset's content. Caller owns disposal.</summary>
    /// <param name="asset">The asset whose content to open.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only stream positioned at the beginning of the asset's content.</returns>
    Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create `DocumentDiscoveredEvent.cs`**

`src/Ferret.Core/Events/Indexing/DocumentDiscoveredEvent.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published by <c>IndexPipeline</c> immediately before attempting to parse each discovered asset.
/// Represents the 8th indexing lifecycle event. Subscribers can use this for progress reporting.</summary>
public sealed class DocumentDiscoveredEvent : DomainEvent
{
    /// <summary>Initializes a new <see cref="DocumentDiscoveredEvent"/>.</summary>
    /// <param name="assetId">The string value of the discovered asset identifier.</param>
    /// <param name="correlationId">The correlation identifier for this pipeline run.</param>
    public DocumentDiscoveredEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId)
    {
        AssetId = new AssetId(assetId);
    }

    /// <summary>Gets the identifier of the discovered asset.</summary>
    public AssetId AssetId { get; }
}
```

- [ ] **Step 5: Modify `IndexResult.cs` — add `AssetsProcessed`**

In `src/Ferret.Core/Indexing/IndexResult.cs`, add the `AssetsProcessed` property after `AssetsDiscovered`:

```csharp
    /// <summary>Gets the number of assets that entered the parse/index stage
    /// (i.e. had a reader available and were not immediately skipped for lack of one).
    /// Invariant: <c>AssetsProcessed = DocumentsIndexed + DocumentsSkipped + Failures</c>.</summary>
    public required int AssetsProcessed { get; init; }
```

Read the file first to identify the insertion point after `AssetsDiscovered`.

- [ ] **Step 6: Modify `IndexPipelineOptions.cs` — add `ForceRebuild`**

In `src/Ferret.Core/Indexing/IndexPipelineOptions.cs`, add the `ForceRebuild` property and ensure `Default` has `ForceRebuild = false`:

```csharp
    /// <summary>Gets a value indicating whether to clear the index before running the pipeline.
    /// When true, <c>IIndexEngine.ClearAsync</c> is called before discovery begins.
    /// All previously indexed content is discarded. Default: false (incremental run).</summary>
    public bool ForceRebuild { get; init; } = false;
```

Read the file first to confirm the existing `Default` static property and insertion point.

- [ ] **Step 7: Modify `IIndexEngine.cs` — replace `RebuildAsync` with `ClearAsync`**

In `src/Ferret.Core/Indexing/IIndexEngine.cs`, replace the `RebuildAsync` method declaration with `ClearAsync`:

Remove:
```csharp
    /// <summary>Drops and recreates the index. All previously indexed content is lost.</summary>
    Task RebuildAsync(CancellationToken ct = default);
```

Replace with:
```csharp
    /// <summary>Deletes all documents from the index. All previously indexed content is lost.
    /// Called by <c>IIndexPipeline</c> when <c>IndexPipelineOptions.ForceRebuild</c> is true.
    /// Storage engines never own orchestration — the pipeline decides when to clear.</summary>
    Task ClearAsync(CancellationToken ct = default);

    // Reserved for Sprint 10 (incremental indexing — remove documents for deleted assets):
    // Task DeleteAsync(DocumentId documentId, CancellationToken ct = default);
```

Read `IIndexEngine.cs` first before editing.

- [ ] **Step 8: Update ADR-0014**

In `docs/adr/0014-document-processing-architecture.md`, make three additions:

1. Add Principle 9 to the principles section:
   > **Principle 9: Storage engines never own orchestration.** `IIndexEngine` reads and writes documents. `IIndexPipeline` owns the full lifecycle (discover → parse → normalize → index). No component below the pipeline boundary calls connectors or parsers.

2. Add `IIndexStore` to the reserved extension points table:
   > `IIndexStore` — storage backend abstraction below `IIndexEngine` (enables swapping SQLite for PostgreSQL, DuckDB, or Tantivy without changing orchestration)

3. Add a corruption recovery deferral note under Consequences or a dedicated section:
   > **Corruption Recovery (deferred):** `SqliteKeywordIndexEngine` propagates `SqliteException` on construction if the database file is corrupt. Automatic delete-and-recreate recovery is deferred to a future sprint. Operators can recover by deleting `.ferret/indexes/keyword/keyword-index.db` and re-running `ferret index`.

Read the ADR file first to locate the correct insertion points.

- [ ] **Step 9: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "AssetReaderContractTests"
dotnet test tests/Ferret.Core.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass (including existing S1 tests), 0 build errors.

---

## Task 2: Project Scaffold (`Ferret.Indexing` + `Ferret.Indexing.Tests`)

**Why:** All subsequent tasks in this section require the `Ferret.Indexing` project and test project to exist with the correct project references, solution registration, and test fakes.

**Files:**
- Create: `src/Ferret.Indexing/Ferret.Indexing.csproj`
- Create: `src/Ferret.Indexing/Properties/AssemblyInfo.cs`
- Create: `tests/Ferret.Indexing.Tests/Ferret.Indexing.Tests.csproj`
- Create: `tests/Ferret.Indexing.Tests/Fakes/FakeIndexEngine.cs`
- Create: `tests/Ferret.Indexing.Tests/Fakes/FakeParserDispatcher.cs`
- Create: `tests/Ferret.Indexing.Tests/Fakes/FakeEventBus.cs`
- Create: `tests/Ferret.Indexing.Tests/Fakes/FakeAssetSourceReader.cs`
- Create: `tests/Ferret.Indexing.Tests/Fakes/FakeConnectorRegistry.cs`
- Modify: `src/Ferret.sln`

**Interfaces:**
- Produces: compilable project skeleton with fakes — consumed by Tasks 4 and 5

- [ ] **Step 1: Create `Ferret.Indexing.csproj`**

`src/Ferret.Indexing/Ferret.Indexing.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisMode>All</AnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Ferret.Indexing</RootNamespace>
    <AssemblyName>Ferret.Indexing</AssemblyName>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="9.*" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create `Properties/AssemblyInfo.cs`**

`src/Ferret.Indexing/Properties/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ferret.Indexing.Tests")]
```

- [ ] **Step 3: Create `Ferret.Indexing.Tests.csproj`**

`tests/Ferret.Indexing.Tests/Ferret.Indexing.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.Indexing.Tests</RootNamespace>
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
    <ProjectReference Include="..\..\src\Ferret.Indexing\Ferret.Indexing.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Add projects to solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Indexing/Ferret.Indexing.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Indexing.Tests/Ferret.Indexing.Tests.csproj
```

- [ ] **Step 5: Create `Fakes/FakeIndexEngine.cs`**

`tests/Ferret.Indexing.Tests/Fakes/FakeIndexEngine.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.Core.Indexing;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IIndexEngine. Tracks writes and clears for assertion.</summary>
internal sealed class FakeIndexEngine : IIndexEngine
{
    private readonly List<Document> _written = [];

    /// <summary>Gets all documents written via WriteAsync.</summary>
    internal IReadOnlyList<Document> WrittenDocuments => _written;

    /// <summary>Gets the number of times ClearAsync was called.</summary>
    internal int ClearCount { get; private set; }

    /// <inheritdoc/>
    public Task WriteAsync(Document document, CancellationToken ct = default)
    {
        _written.Add(document);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IndexStats> GetStatsAsync(CancellationToken ct = default) =>
        Task.FromResult(new IndexStats
        {
            DocumentCount = _written.Count,
            TotalChars = _written.Sum(d => (long)d.PlainText.Length),
            LastIndexedAt = DateTimeOffset.UtcNow,
            IndexSizeBytes = 0,
        });

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default)
    {
        ClearCount++;
        _written.Clear();
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 6: Create `Fakes/FakeParserDispatcher.cs`**

`tests/Ferret.Indexing.Tests/Fakes/FakeParserDispatcher.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IParserDispatcher. Returns a configurable result per asset.</summary>
internal sealed class FakeParserDispatcher : IParserDispatcher
{
    private Func<AssetDescriptor, ParseResult<Document>>? _resultFactory;

    /// <summary>Configures the result factory. Defaults to returning Unsupported when not set.</summary>
    internal void SetResult(Func<AssetDescriptor, ParseResult<Document>> factory)
    {
        _resultFactory = factory;
    }

    /// <inheritdoc/>
    public ValueTask<ParseResult<Document>> DispatchAsync(
        Stream content,
        AssetDescriptor asset,
        CancellationToken ct = default)
    {
        var result = _resultFactory?.Invoke(asset)
            ?? ParseResult<Document>.Unsupported(asset.MediaType ?? "application/octet-stream");
        return ValueTask.FromResult(result);
    }
}
```

- [ ] **Step 7: Create `Fakes/FakeEventBus.cs`**

`tests/Ferret.Indexing.Tests/Fakes/FakeEventBus.cs`:

```csharp
using Ferret.Core.Events;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IEventBus. Records all published events for assertion.</summary>
internal sealed class FakeEventBus : IEventBus
{
    private readonly List<DomainEvent> _published = [];

    /// <summary>Gets all events published via PublishAsync.</summary>
    internal IReadOnlyList<DomainEvent> Published => _published;

    /// <inheritdoc/>
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent
    {
        _published.Add(domainEvent);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 8: Create `Fakes/FakeAssetSourceReader.cs`**

`tests/Ferret.Indexing.Tests/Fakes/FakeAssetSourceReader.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>
/// Test double that implements both IAssetSource and IAssetReader.
/// Configured with a list of AssetDescriptors to yield and optionally a stream factory.
/// </summary>
internal sealed class FakeAssetSourceReader : IAssetSource, IAssetReader
{
    private readonly List<AssetDescriptor> _assets;
    private readonly Func<AssetDescriptor, Stream>? _streamFactory;

    internal FakeAssetSourceReader(
        IEnumerable<AssetDescriptor> assets,
        Func<AssetDescriptor, Stream>? streamFactory = null)
    {
        _assets = assets.ToList();
        _streamFactory = streamFactory;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var asset in _assets)
        {
            ct.ThrowIfCancellationRequested();
            yield return asset;
            await Task.Yield();
        }
    }

    /// <inheritdoc/>
    public Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default)
    {
        var stream = _streamFactory?.Invoke(asset)
            ?? new MemoryStream(System.Text.Encoding.UTF8.GetBytes("sample content"));
        return Task.FromResult(stream);
    }
}
```

- [ ] **Step 9: Create `Fakes/FakeConnectorRegistry.cs`**

`tests/Ferret.Indexing.Tests/Fakes/FakeConnectorRegistry.cs`:

```csharp
using Ferret.Core.Connectors;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IConnectorRegistry. Returns a pre-configured list of connectors.</summary>
internal sealed class FakeConnectorRegistry : IConnectorRegistry
{
    private readonly List<IConnector> _connectors;

    internal FakeConnectorRegistry(IEnumerable<IConnector> connectors)
    {
        _connectors = connectors.ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ConnectorDescriptor> GetAll() =>
        _connectors.Select(c => c.Descriptor).ToList();

    /// <inheritdoc/>
    public IConnector? GetByInstanceId(ConnectorInstanceId instanceId) =>
        _connectors.FirstOrDefault(c => c.Descriptor.InstanceId == instanceId);

    /// <inheritdoc/>
    public IReadOnlyList<IConnector> GetEnabled() =>
        _connectors.Where(c => c.Descriptor.IsEnabled).ToList();
}
```

Note: Read `IConnectorRegistry` and `IConnector` in `Ferret.Core` to confirm the exact interface before writing fakes. Adjust method names if they differ from the above.

- [ ] **Step 10: Verify scaffold compiles**

```
dotnet build src/Ferret.Indexing/Ferret.Indexing.csproj
dotnet build tests/Ferret.Indexing.Tests/Ferret.Indexing.Tests.csproj
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings.

---

## Task 3: `FilesystemConnector` implements `IAssetReader`

**Why:** `IndexPipeline` checks `connector is IAssetReader` before opening content streams. Without this, all filesystem assets are skipped during indexing. This is a targeted modification — no changes to existing discovery logic.

**Files:**
- Modify: `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs`
- Create: `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorReaderTests.cs`

**Interfaces:**
- Consumes: `IAssetReader` (Task 1), `AssetDescriptor` (Core)
- Produces: `FilesystemConnector` implementing both `IAssetSource` and `IAssetReader` — consumed by Task 5 (IndexPipeline integration tests)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Connectors.Filesystem.Tests/FilesystemConnectorReaderTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Connectors.Filesystem.Tests;

public sealed class FilesystemConnectorReaderTests
{
    [Fact]
    public void FilesystemConnector_Implements_IAssetReader()
    {
        // Check at type level — connector must implement both interfaces
        Assert.True(typeof(IAssetReader).IsAssignableFrom(typeof(FilesystemConnector)));
    }

    [Fact]
    public void FilesystemConnector_Implements_Both_IAssetSource_And_IAssetReader()
    {
        Assert.True(typeof(IAssetSource).IsAssignableFrom(typeof(FilesystemConnector)));
        Assert.True(typeof(IAssetReader).IsAssignableFrom(typeof(FilesystemConnector)));
    }

    [Fact]
    public async Task OpenAsync_Returns_Stream_With_Correct_Content()
    {
        using var tmp = new TempDirectory();
        var expected = "Hello from the filesystem connector reader.";
        var filePath = Path.Combine(tmp.Path, "test.txt");
        File.WriteAllText(filePath, expected);

        var connector = CreateConnector(tmp.Path);
        var asset = MakeAsset(filePath);

        await using var stream = await ((IAssetReader)connector).OpenAsync(asset);
        using var reader = new StreamReader(stream);
        var actual = await reader.ReadToEndAsync();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task OpenAsync_Throws_FileNotFoundException_For_Missing_Path()
    {
        using var tmp = new TempDirectory();
        var connector = CreateConnector(tmp.Path);
        var asset = MakeAsset(Path.Combine(tmp.Path, "nonexistent.txt"));

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await using var stream = await ((IAssetReader)connector).OpenAsync(asset);
        });
    }

    [Fact]
    public async Task OpenAsync_Respects_Cancellation()
    {
        using var tmp = new TempDirectory();
        File.WriteAllText(Path.Combine(tmp.Path, "file.txt"), "content");
        var connector = CreateConnector(tmp.Path);
        var asset = MakeAsset(Path.Combine(tmp.Path, "file.txt"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await using var stream = await ((IAssetReader)connector).OpenAsync(asset, cts.Token);
        });
    }

    private static FilesystemConnector CreateConnector(string rootPath)
    {
        // Adjust this helper to match the actual FilesystemConnector constructor API.
        // Read FilesystemConnector.cs first to verify the constructor signature.
        var resolver = new Ferret.ParserPlatform.MimeTypeResolver();
        var config = new Ferret.Core.Workspace.ConnectorConfig
        {
            InstanceId = new ConnectorInstanceId("test"),
            ConnectorId = new ConnectorId("filesystem"),
            DisplayName = "test",
            IsEnabled = true,
            Settings = new Dictionary<string, string> { ["RootPath"] = rootPath },
        };
        return new FilesystemConnector(config, resolver);
    }

    private static AssetDescriptor MakeAsset(string filePath)
    {
        var uri = new Uri(filePath);
        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = Path.GetFileName(filePath),
            LastModified = DateTimeOffset.UtcNow,
        };
    }
}
```

Note: Read `FilesystemConnector.cs` and `TempDirectory` in the test project before writing. Adjust the `CreateConnector` helper to match the actual constructor signature.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorReaderTests"
```

Expected: FAIL — `FilesystemConnector` does not implement `IAssetReader`.

- [ ] **Step 3: Implement `IAssetReader` on `FilesystemConnector`**

Read `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` first. Then:

1. Add `IAssetReader` to the class declaration:
   ```csharp
   // Before:
   public sealed class FilesystemConnector : IAssetSource, IConnector
   // After:
   public sealed class FilesystemConnector : IAssetSource, IAssetReader, IConnector
   ```

2. Add the `OpenAsync` implementation at the end of the class body:
   ```csharp
   /// <inheritdoc/>
   public Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default)
   {
       ct.ThrowIfCancellationRequested();
       return Task.FromResult<Stream>(File.OpenRead(asset.CanonicalUri.LocalPath));
   }
   ```

Do not modify any other methods. The `CanonicalUri.LocalPath` gives the local file path from the URI stored in the asset descriptor.

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Connectors.Filesystem.Tests --filter "FilesystemConnectorReaderTests"
dotnet test tests/Ferret.Connectors.Filesystem.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 4: `SqliteKeywordIndexEngine`

**Why:** `IndexPipeline` (Task 5) depends on `IIndexEngine` being available. The SQLite FTS5 engine is the only `IIndexEngine` implementation in Sprint 9. Tests use a temp file path — no workspace dependency. The two-table schema (metadata + FTS5 virtual table) enables both structured queries (by connector, instance, media type) and full-text search in S5.

**Files:**
- Create: `src/Ferret.Indexing/SqliteKeywordIndexEngine.cs`
- Create: `tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineTests.cs`

**Interfaces:**
- Consumes: `IIndexEngine` (Task 1 revision), `Document`, `IndexStats` (Core), `Microsoft.Data.Sqlite`
- Produces: `SqliteKeywordIndexEngine` — registered by S5 CLI host with resolved db path

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Indexing.Tests;

public sealed class SqliteKeywordIndexEngineTests : IDisposable
{
    private readonly string _dbDir;
    private readonly string _dbPath;

    public SqliteKeywordIndexEngineTests()
    {
        _dbDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_dbDir);
        _dbPath = Path.Combine(_dbDir, "keyword-index.db");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dbDir))
        {
            Directory.Delete(_dbDir, recursive: true);
        }
    }

    [Fact]
    public async Task WriteAsync_Creates_Database_File()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        var doc = BuildDocument("doc-1");

        await engine.WriteAsync(doc);

        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public async Task WriteAsync_Upserts_By_DocumentId()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        var doc1 = BuildDocument("doc-1", "first content");
        var doc2 = BuildDocument("doc-1", "updated content");

        await engine.WriteAsync(doc1);
        await engine.WriteAsync(doc2);
        var stats = await engine.GetStatsAsync();

        Assert.Equal(1, stats.DocumentCount);
    }

    [Fact]
    public async Task GetStatsAsync_Returns_Zero_For_Empty_Index()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);

        var stats = await engine.GetStatsAsync();

        Assert.Equal(0, stats.DocumentCount);
    }

    [Fact]
    public async Task GetStatsAsync_Returns_Correct_Count_After_Multiple_Writes()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);

        await engine.WriteAsync(BuildDocument("doc-1"));
        await engine.WriteAsync(BuildDocument("doc-2"));
        await engine.WriteAsync(BuildDocument("doc-3"));
        var stats = await engine.GetStatsAsync();

        Assert.Equal(3, stats.DocumentCount);
    }

    [Fact]
    public async Task ClearAsync_Removes_All_Documents()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);

        await engine.WriteAsync(BuildDocument("doc-1"));
        await engine.WriteAsync(BuildDocument("doc-2"));
        await engine.WriteAsync(BuildDocument("doc-3"));
        await engine.ClearAsync();
        var stats = await engine.GetStatsAsync();

        Assert.Equal(0, stats.DocumentCount);
    }

    [Fact]
    public async Task WriteAsync_Creates_Parent_Directories_If_Missing()
    {
        var deepPath = Path.Combine(_dbDir, "a", "b", "c", "keyword-index.db");
        using var engine = new SqliteKeywordIndexEngine(deepPath);

        await engine.WriteAsync(BuildDocument("doc-1"));

        Assert.True(File.Exists(deepPath));
    }

    [Fact]
    public void Constructor_Sets_UserVersion_To_1()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);

        // Open the db directly and check PRAGMA user_version
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(cmd.ExecuteScalar());

        Assert.Equal(1, version);
    }

    [Fact]
    public void Constructor_Throws_For_Future_Schema_Version()
    {
        // Pre-create a db with user_version = 99
        using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA user_version = 99;";
            cmd.ExecuteNonQuery();
        }

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new SqliteKeywordIndexEngine(_dbPath));

        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public void Constructor_Propagates_SqliteException_For_Corrupt_File()
    {
        // Write garbage bytes to simulate a corrupt SQLite file
        File.WriteAllBytes(_dbPath, [0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE]);

        Assert.Throws<SqliteException>(() => new SqliteKeywordIndexEngine(_dbPath));
    }

    private static Document BuildDocument(string id, string plainText = "sample content")
    {
        var assetId = new AssetId($"filesystem:///src/{id}.txt");
        return new Document
        {
            Id = DocumentId.From(assetId),
            SourceAssetId = assetId,
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            MediaType = "text/plain",
            Kind = DocumentKind.Unknown,
            PlainText = plainText,
            ProducedAt = DateTimeOffset.UtcNow,
        };
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Indexing.Tests --filter "SqliteKeywordIndexEngineTests"
```

Expected: FAIL — `SqliteKeywordIndexEngine` not found.

- [ ] **Step 3: Create `SqliteKeywordIndexEngine.cs`**

`src/Ferret.Indexing/SqliteKeywordIndexEngine.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Microsoft.Data.Sqlite;

namespace Ferret.Indexing;

/// <summary>
/// SQLite FTS5 keyword index engine. Two-table schema:
/// <list type="bullet">
///   <item><c>documents</c> — regular metadata table keyed by document_id.</item>
///   <item><c>documents_fts</c> — FTS5 virtual table for full-text search over title and plain_text.</item>
/// </list>
/// <c>WriteAsync</c> is an upsert: updates <c>documents</c> and delete-then-inserts <c>documents_fts</c>.
/// <c>ClearAsync</c> deletes all rows from both tables.
/// <c>GetStatsAsync</c> counts rows in <c>documents</c>.
/// <para>Schema version is tracked via <c>PRAGMA user_version</c>. Version 0 = new database;
/// version 1 = Sprint 9 schema. A future version greater than 1 throws <see cref="InvalidOperationException"/>.</para>
/// <para>Corruption recovery (delete and recreate) is deferred — see ADR-0014.</para>
/// </summary>
public sealed class SqliteKeywordIndexEngine : IIndexEngine, IDisposable
{
    private const int CurrentSchemaVersion = 1;

    private readonly SqliteConnection _connection;
    private bool _disposed;

    /// <summary>
    /// Initializes the engine and opens (or creates) the SQLite database at <paramref name="dbPath"/>.
    /// Creates parent directories if they do not exist.
    /// </summary>
    /// <param name="dbPath">Absolute path to the SQLite database file. Injected by the CLI host (S5).</param>
    /// <exception cref="InvalidOperationException">Thrown when the database schema version is newer than supported.</exception>
    /// <exception cref="SqliteException">Propagated when SQLite cannot open the file (e.g. corruption).</exception>
    public SqliteKeywordIndexEngine(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        EnsureSchema();
    }

    /// <inheritdoc/>
    public Task WriteAsync(Document document, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var transaction = _connection.BeginTransaction();

        // Upsert metadata table
        using var upsertCmd = _connection.CreateCommand();
        upsertCmd.Transaction = transaction;
        upsertCmd.CommandText = """
            INSERT INTO documents
                (document_id, canonical_uri, asset_id, connector_id, instance_id,
                 media_type, kind, title, produced_at, source_fingerprint)
            VALUES
                (@document_id, @canonical_uri, @asset_id, @connector_id, @instance_id,
                 @media_type, @kind, @title, @produced_at, @source_fingerprint)
            ON CONFLICT(document_id) DO UPDATE SET
                canonical_uri      = excluded.canonical_uri,
                asset_id           = excluded.asset_id,
                connector_id       = excluded.connector_id,
                instance_id        = excluded.instance_id,
                media_type         = excluded.media_type,
                kind               = excluded.kind,
                title              = excluded.title,
                produced_at        = excluded.produced_at,
                source_fingerprint = excluded.source_fingerprint;
            """;
        upsertCmd.Parameters.AddWithValue("@document_id", document.Id.Value);
        upsertCmd.Parameters.AddWithValue("@canonical_uri", document.SourceAssetId.Value);
        upsertCmd.Parameters.AddWithValue("@asset_id", document.SourceAssetId.Value);
        upsertCmd.Parameters.AddWithValue("@connector_id", document.ConnectorId.Value);
        upsertCmd.Parameters.AddWithValue("@instance_id", document.InstanceId.Value);
        upsertCmd.Parameters.AddWithValue("@media_type", document.MediaType);
        upsertCmd.Parameters.AddWithValue("@kind", (int)document.Kind);
        upsertCmd.Parameters.AddWithValue("@title", (object?)document.Title ?? DBNull.Value);
        upsertCmd.Parameters.AddWithValue("@produced_at", document.ProducedAt.ToString("O"));
        upsertCmd.Parameters.AddWithValue("@source_fingerprint",
            (object?)document.SourceFingerprint?.Value ?? DBNull.Value);
        upsertCmd.ExecuteNonQuery();

        // Delete then insert FTS5 (no UPSERT on virtual tables)
        using var deleteFtsCmd = _connection.CreateCommand();
        deleteFtsCmd.Transaction = transaction;
        deleteFtsCmd.CommandText = "DELETE FROM documents_fts WHERE document_id = @document_id;";
        deleteFtsCmd.Parameters.AddWithValue("@document_id", document.Id.Value);
        deleteFtsCmd.ExecuteNonQuery();

        using var insertFtsCmd = _connection.CreateCommand();
        insertFtsCmd.Transaction = transaction;
        insertFtsCmd.CommandText = """
            INSERT INTO documents_fts (document_id, title, plain_text)
            VALUES (@document_id, @title, @plain_text);
            """;
        insertFtsCmd.Parameters.AddWithValue("@document_id", document.Id.Value);
        insertFtsCmd.Parameters.AddWithValue("@title", (object?)document.Title ?? DBNull.Value);
        insertFtsCmd.Parameters.AddWithValue("@plain_text", document.PlainText);
        insertFtsCmd.ExecuteNonQuery();

        transaction.Commit();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IndexStats> GetStatsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM documents;";
        var count = Convert.ToInt64(cmd.ExecuteScalar());

        return Task.FromResult(new IndexStats
        {
            DocumentCount = count,
            TotalChars = 0,
            LastIndexedAt = DateTimeOffset.UtcNow,
            IndexSizeBytes = 0,
        });
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var transaction = _connection.BeginTransaction();

        using var clearFtsCmd = _connection.CreateCommand();
        clearFtsCmd.Transaction = transaction;
        clearFtsCmd.CommandText = "DELETE FROM documents_fts;";
        clearFtsCmd.ExecuteNonQuery();

        using var clearCmd = _connection.CreateCommand();
        clearCmd.Transaction = transaction;
        clearCmd.CommandText = "DELETE FROM documents;";
        clearCmd.ExecuteNonQuery();

        transaction.Commit();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }

    private void EnsureSchema()
    {
        using var versionCmd = _connection.CreateCommand();
        versionCmd.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(versionCmd.ExecuteScalar());

        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Index schema version {version} is not supported. " +
                $"This build supports up to version {CurrentSchemaVersion}. " +
                "Delete the index file and re-run 'ferret index' to rebuild.");
        }

        if (version == CurrentSchemaVersion)
        {
            return;
        }

        // version == 0: new database — create schema
        using var transaction = _connection.BeginTransaction();

        using var createDocsCmd = _connection.CreateCommand();
        createDocsCmd.Transaction = transaction;
        createDocsCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS documents (
                document_id        TEXT NOT NULL PRIMARY KEY,
                canonical_uri      TEXT NOT NULL,
                asset_id           TEXT NOT NULL,
                connector_id       TEXT NOT NULL,
                instance_id        TEXT NOT NULL,
                media_type         TEXT NOT NULL,
                kind               INTEGER NOT NULL,
                title              TEXT,
                produced_at        TEXT NOT NULL,
                source_fingerprint TEXT
            );
            """;
        createDocsCmd.ExecuteNonQuery();

        using var createFtsCmd = _connection.CreateCommand();
        createFtsCmd.Transaction = transaction;
        createFtsCmd.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS documents_fts USING fts5(
                document_id UNINDEXED,
                title,
                plain_text,
                tokenize = 'porter unicode61'
            );
            """;
        createFtsCmd.ExecuteNonQuery();

        using var versionSetCmd = _connection.CreateCommand();
        versionSetCmd.Transaction = transaction;
        versionSetCmd.CommandText = $"PRAGMA user_version = {CurrentSchemaVersion};";
        versionSetCmd.ExecuteNonQuery();

        transaction.Commit();
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Indexing.Tests --filter "SqliteKeywordIndexEngineTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 5: `IndexPipeline`

**Why:** `IndexPipeline` is the core deliverable of Section 3 — it wires connectors, readers, parsers, and the index engine into a single orchestrated run. All pipeline behavior (ForceRebuild, per-asset isolation, event publishing, counters) is verified via fakes — no real database or filesystem required.

**Files:**
- Create: `src/Ferret.Indexing/IndexPipeline.cs`
- Create: `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs`

**Interfaces:**
- Consumes: `IConnectorRegistry`, `IAssetSource`, `IAssetReader`, `IParserDispatcher`, `IIndexEngine`, `IEventBus`, `IndexPipelineOptions`, `IndexResult`, `DocumentDiscoveredEvent` and all 7 S1 events (Core)
- Produces: `IndexPipeline` — registered as `IIndexPipeline` by `IndexingModule`

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Events.Indexing;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing.Tests.Fakes;
using Xunit;

namespace Ferret.Indexing.Tests;

public sealed class IndexPipelineTests
{
    private static AssetDescriptor MakeAsset(string id, string mediaType = "text/plain")
    {
        var uri = new Uri($"filesystem:///src/{id}.txt");
        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = $"{id}.txt",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = mediaType,
        };
    }

    private static Document MakeDocument(AssetDescriptor asset) => new()
    {
        Id = DocumentId.From(asset.Id),
        SourceAssetId = asset.Id,
        ConnectorId = asset.ConnectorId,
        InstanceId = asset.InstanceId,
        MediaType = asset.MediaType ?? "text/plain",
        Kind = DocumentKind.Unknown,
        PlainText = "content",
        ProducedAt = DateTimeOffset.UtcNow,
    };

    private static (IndexPipeline pipeline, FakeIndexEngine engine, FakeEventBus bus)
        BuildPipeline(
            FakeConnectorRegistry registry,
            FakeParserDispatcher dispatcher)
    {
        var engine = new FakeIndexEngine();
        var bus = new FakeEventBus();
        var correlationId = new CorrelationId("test-run");
        var pipeline = new IndexPipeline(registry, dispatcher, engine, bus, correlationId);
        return (pipeline, engine, bus);
    }

    [Fact]
    public async Task Empty_Connector_Returns_Zero_Discovered()
    {
        var sourceReader = new FakeAssetSourceReader([]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(0, result.AssetsDiscovered);
        Assert.Equal(0, result.DocumentsIndexed);
    }

    [Fact]
    public async Task Single_Asset_Parsed_Successfully_Returns_DocumentsIndexed_1()
    {
        var asset = MakeAsset("a");
        var sourceReader = new FakeAssetSourceReader([asset]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(MakeDocument(a)));
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(1, result.AssetsDiscovered);
        Assert.Equal(1, result.DocumentsIndexed);
        Assert.Equal(1, result.AssetsProcessed);
        Assert.Equal(0, result.DocumentsSkipped);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public async Task Unsupported_MediaType_Returns_DocumentsSkipped_1()
    {
        var asset = MakeAsset("a", "application/pdf");
        var sourceReader = new FakeAssetSourceReader([asset]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(_ => ParseResult<Document>.Unsupported("application/pdf"));
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(0, result.DocumentsIndexed);
        Assert.Equal(1, result.DocumentsSkipped);
        Assert.Equal(1, result.AssetsProcessed);
    }

    [Fact]
    public async Task Empty_Content_Returns_DocumentsSkipped_1()
    {
        var asset = MakeAsset("a");
        var sourceReader = new FakeAssetSourceReader([asset]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(_ => ParseResult<Document>.Empty());
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(0, result.DocumentsIndexed);
        Assert.Equal(1, result.DocumentsSkipped);
    }

    [Fact]
    public async Task Failed_Parse_Increments_Failures_And_Pipeline_Continues()
    {
        var asset = MakeAsset("a");
        var sourceReader = new FakeAssetSourceReader([asset]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(_ => ParseResult<Document>.Failed("bad parse"));
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(1, result.Failures);
        Assert.Equal(0, result.DocumentsIndexed);
        Assert.NotEmpty(result.FailureMessages);
    }

    [Fact]
    public async Task Two_Assets_First_Succeeds_Second_Fails()
    {
        var assetA = MakeAsset("a");
        var assetB = MakeAsset("b");
        var sourceReader = new FakeAssetSourceReader([assetA, assetB]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a =>
            a.Id.Value.Contains("a.txt")
                ? ParseResult<Document>.Success(MakeDocument(a))
                : ParseResult<Document>.Failed("bad parse"));
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(1, result.DocumentsIndexed);
        Assert.Equal(1, result.Failures);
    }

    [Fact]
    public async Task ForceRebuild_True_Calls_ClearAsync_Once()
    {
        var sourceReader = new FakeAssetSourceReader([]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        var (pipeline, engine, _) = BuildPipeline(registry, dispatcher);

        await pipeline.RunAsync(new IndexPipelineOptions { ForceRebuild = true });

        Assert.Equal(1, engine.ClearCount);
    }

    [Fact]
    public async Task ForceRebuild_False_Does_Not_Call_ClearAsync()
    {
        var sourceReader = new FakeAssetSourceReader([]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        var (pipeline, engine, _) = BuildPipeline(registry, dispatcher);

        await pipeline.RunAsync(new IndexPipelineOptions { ForceRebuild = false });

        Assert.Equal(0, engine.ClearCount);
    }

    [Fact]
    public async Task Connector_Without_IAssetReader_Skips_Asset()
    {
        var asset = MakeAsset("a");
        // Source-only connector — no IAssetReader
        var sourceOnly = new FakeConnectorSourceOnly(new FakeAssetSourceOnly([asset]));
        var registry = new FakeConnectorRegistry([sourceOnly]);
        var dispatcher = new FakeParserDispatcher();
        var (pipeline, _, bus) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(1, result.AssetsDiscovered);
        Assert.Equal(0, result.DocumentsIndexed);
        Assert.Equal(1, result.DocumentsSkipped);
        Assert.Contains(bus.Published, e => e is DocumentSkippedEvent);
    }

    [Fact]
    public async Task IndexingStartedEvent_And_IndexingCompletedEvent_Always_Published()
    {
        var sourceReader = new FakeAssetSourceReader([]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        var (pipeline, _, bus) = BuildPipeline(registry, dispatcher);

        await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Contains(bus.Published, e => e is IndexingStartedEvent);
        Assert.Contains(bus.Published, e => e is IndexingCompletedEvent);
    }

    [Fact]
    public async Task DocumentDiscoveredEvent_Published_For_Each_Asset()
    {
        var assetA = MakeAsset("a");
        var assetB = MakeAsset("b");
        var sourceReader = new FakeAssetSourceReader([assetA, assetB]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(MakeDocument(a)));
        var (pipeline, _, bus) = BuildPipeline(registry, dispatcher);

        await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(2, bus.Published.OfType<DocumentDiscoveredEvent>().Count());
    }

    [Fact]
    public async Task OperationCanceledException_Propagates()
    {
        var asset = MakeAsset("a");
        var sourceReader = new FakeAssetSourceReader([asset]);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        using var cts = new CancellationTokenSource();
        // Cancel immediately so the pipeline sees it on first iteration
        dispatcher.SetResult(_ =>
        {
            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return ParseResult<Document>.Unsupported("x");
        });
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.RunAsync(IndexPipelineOptions.Default, cts.Token));
    }

    [Fact]
    public async Task AssetsProcessed_Equals_Indexed_Plus_Skipped_Plus_Failures()
    {
        var assets = Enumerable.Range(0, 5).Select(i => MakeAsset($"asset-{i}")).ToList();
        var sourceReader = new FakeAssetSourceReader(assets);
        var fakeConnector = new FakeConnectorWithReader(sourceReader);
        var registry = new FakeConnectorRegistry([fakeConnector]);
        var dispatcher = new FakeParserDispatcher();
        var call = 0;
        dispatcher.SetResult(a =>
        {
            return (call++ % 3) switch
            {
                0 => ParseResult<Document>.Success(MakeDocument(a)),
                1 => ParseResult<Document>.Empty(),
                _ => ParseResult<Document>.Failed("err"),
            };
        });
        var (pipeline, _, _) = BuildPipeline(registry, dispatcher);

        var result = await pipeline.RunAsync(IndexPipelineOptions.Default);

        Assert.Equal(
            result.DocumentsIndexed + result.DocumentsSkipped + result.Failures,
            result.AssetsProcessed);
    }

    // ---- Inner fakes for source-only connector (no IAssetReader) ----

    private sealed class FakeConnectorWithReader : IConnector, IAssetSource, IAssetReader
    {
        private readonly FakeAssetSourceReader _inner;

        internal FakeConnectorWithReader(FakeAssetSourceReader inner)
        {
            _inner = inner;
            Descriptor = new ConnectorDescriptor
            {
                ConnectorId = new ConnectorId("filesystem"),
                InstanceId = new ConnectorInstanceId("test"),
                DisplayName = "test",
                IsEnabled = true,
                ConnectorType = ConnectorType.Filesystem,
                Version = "1.0",
            };
        }

        public ConnectorDescriptor Descriptor { get; }

        public IAsyncEnumerable<AssetDescriptor> DiscoverAsync(CancellationToken ct = default) =>
            _inner.DiscoverAsync(ct);

        public Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default) =>
            _inner.OpenAsync(asset, ct);

        public Task<ConnectorHealth> CheckHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Healthy);
    }

    private sealed class FakeAssetSourceOnly : IAssetSource
    {
        private readonly List<AssetDescriptor> _assets;

        internal FakeAssetSourceOnly(IEnumerable<AssetDescriptor> assets)
        {
            _assets = assets.ToList();
        }

        public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var a in _assets)
            {
                ct.ThrowIfCancellationRequested();
                yield return a;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeConnectorSourceOnly : IConnector, IAssetSource
    {
        private readonly FakeAssetSourceOnly _inner;

        internal FakeConnectorSourceOnly(FakeAssetSourceOnly inner)
        {
            _inner = inner;
            Descriptor = new ConnectorDescriptor
            {
                ConnectorId = new ConnectorId("source-only"),
                InstanceId = new ConnectorInstanceId("test-source-only"),
                DisplayName = "source-only",
                IsEnabled = true,
                ConnectorType = ConnectorType.Filesystem,
                Version = "1.0",
            };
        }

        public ConnectorDescriptor Descriptor { get; }

        public IAsyncEnumerable<AssetDescriptor> DiscoverAsync(CancellationToken ct = default) =>
            _inner.DiscoverAsync(ct);

        public Task<ConnectorHealth> CheckHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Healthy);
    }
}
```

Note: Read `IConnector`, `ConnectorDescriptor`, `ConnectorHealth`, and `ConnectorType` in `Ferret.Core` before writing. Adjust the inner fake constructors to match the actual `ConnectorDescriptor` shape.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Indexing.Tests --filter "IndexPipelineTests"
```

Expected: FAIL — `IndexPipeline` not found.

- [ ] **Step 3: Create `IndexPipeline.cs`**

`src/Ferret.Indexing/IndexPipeline.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Events;
using Ferret.Core.Events.Indexing;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Indexing;

/// <summary>
/// Full ingestion pipeline orchestration.
/// Pipeline stage order (reserved stages shown in comment):
/// Connector → AssetDescriptor → IAssetReader → Stream → IParserDispatcher → Document
/// → IContentNormalizer (reserved) → IIndexEngine.
/// <para>
/// <see cref="IIndexEngine"/> reads and writes documents.
/// <see cref="IndexPipeline"/> owns the full lifecycle.
/// No component below the pipeline boundary calls connectors or parsers.
/// </para>
/// </summary>
public sealed class IndexPipeline : IIndexPipeline
{
    private readonly IConnectorRegistry _registry;
    private readonly IParserDispatcher _dispatcher;
    private readonly IIndexEngine _engine;
    private readonly IEventBus _bus;
    private readonly CorrelationId _correlationId;

    /// <summary>Initializes a new <see cref="IndexPipeline"/>.</summary>
    /// <param name="registry">Provides enabled connectors.</param>
    /// <param name="dispatcher">Routes content streams to the correct parser.</param>
    /// <param name="engine">Writes documents to the keyword index.</param>
    /// <param name="bus">Event bus for lifecycle event publication.</param>
    /// <param name="correlationId">Correlation identifier for this pipeline run.</param>
    public IndexPipeline(
        IConnectorRegistry registry,
        IParserDispatcher dispatcher,
        IIndexEngine engine,
        IEventBus bus,
        CorrelationId correlationId)
    {
        _registry = registry;
        _dispatcher = dispatcher;
        _engine = engine;
        _bus = bus;
        _correlationId = correlationId;
    }

    /// <inheritdoc/>
    public async Task<IndexResult> RunAsync(
        IndexPipelineOptions options,
        CancellationToken ct = default)
    {
        await _bus.PublishAsync(new IndexingStartedEvent("workspace", _correlationId)
        {
            IsRebuild = options.ForceRebuild,
        }, ct).ConfigureAwait(false);

        try
        {
            return await RunCoreAsync(options, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _bus.PublishAsync(new IndexingFailedEvent("workspace", _correlationId)
            {
                ErrorMessage = ex.Message,
            }, ct).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<IndexResult> RunCoreAsync(
        IndexPipelineOptions options,
        CancellationToken ct)
    {
        if (options.ForceRebuild)
        {
            await _engine.ClearAsync(ct).ConfigureAwait(false);
        }

        var started = DateTimeOffset.UtcNow;
        var discovered = 0;
        var processed = 0;
        var indexed = 0;
        var skipped = 0;
        var failures = 0;
        var failureMessages = new List<string>();

        var connectors = _registry.GetEnabled();

        foreach (var connector in connectors)
        {
            if (connector is not IAssetSource assetSource)
            {
                continue;
            }

            await foreach (var asset in assetSource.DiscoverAsync(ct).ConfigureAwait(false))
            {
                discovered++;

                await _bus.PublishAsync(
                    new DocumentDiscoveredEvent(asset.Id.Value, _correlationId), ct)
                    .ConfigureAwait(false);

                if (connector is not IAssetReader reader)
                {
                    skipped++;
                    await _bus.PublishAsync(new DocumentSkippedEvent(asset.Id.Value, _correlationId)
                    {
                        AssetId = asset.Id,
                        Reason = "No reader: connector does not implement IAssetReader.",
                    }, ct).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    await using var stream = await reader.OpenAsync(asset, ct).ConfigureAwait(false);
                    var parseResult = await _dispatcher.DispatchAsync(stream, asset, ct).ConfigureAwait(false);
                    processed++;

                    switch (parseResult.Kind)
                    {
                        case Core.Documents.ParseResultKind.Success:
                            var doc = parseResult.Value!;
                            await _engine.WriteAsync(doc, ct).ConfigureAwait(false);

                            await _bus.PublishAsync(new DocumentParsedEvent(asset.Id.Value, _correlationId)
                            {
                                AssetId = asset.Id,
                                DocumentId = doc.Id,
                                MediaType = asset.MediaType ?? "application/octet-stream",
                            }, ct).ConfigureAwait(false);

                            await _bus.PublishAsync(new DocumentIndexedEvent(doc.Id.Value, _correlationId)
                            {
                                DocumentId = doc.Id,
                                AssetId = asset.Id,
                                MediaType = asset.MediaType ?? "application/octet-stream",
                                CharCount = doc.PlainText.Length,
                            }, ct).ConfigureAwait(false);

                            indexed++;
                            break;

                        case Core.Documents.ParseResultKind.Unsupported:
                            skipped++;
                            await _bus.PublishAsync(new DocumentSkippedEvent(asset.Id.Value, _correlationId)
                            {
                                AssetId = asset.Id,
                                Reason = $"Unsupported: {asset.MediaType ?? "application/octet-stream"}",
                            }, ct).ConfigureAwait(false);
                            break;

                        case Core.Documents.ParseResultKind.Empty:
                            skipped++;
                            await _bus.PublishAsync(new DocumentSkippedEvent(asset.Id.Value, _correlationId)
                            {
                                AssetId = asset.Id,
                                Reason = "Empty content.",
                            }, ct).ConfigureAwait(false);
                            break;

                        case Core.Documents.ParseResultKind.Failed:
                            failures++;
                            var errorMsg = parseResult.Diagnostics.FirstOrDefault()?.Message ?? "Unknown parse error.";
                            failureMessages.Add($"{asset.DisplayName}: {errorMsg}");

                            await _bus.PublishAsync(
                                new DocumentParsingFailedEvent(asset.Id.Value, _correlationId)
                                {
                                    AssetId = asset.Id,
                                    MediaType = asset.MediaType ?? "application/octet-stream",
                                    ErrorMessage = errorMsg,
                                }, ct).ConfigureAwait(false);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    processed++;
                    failures++;
                    failureMessages.Add($"{asset.DisplayName}: {ex.Message}");
                }
            }
        }

        var result = new IndexResult
        {
            AssetsDiscovered = discovered,
            AssetsProcessed = processed,
            DocumentsIndexed = indexed,
            DocumentsSkipped = skipped,
            Failures = failures,
            Warnings = 0,
            Duration = DateTimeOffset.UtcNow - started,
            FailureMessages = failureMessages,
        };

        await _bus.PublishAsync(
            new IndexingCompletedEvent("workspace", _correlationId) { Result = result }, ct)
            .ConfigureAwait(false);

        return result;
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Indexing.Tests --filter "IndexPipelineTests"
dotnet test tests/Ferret.Indexing.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 6: `IndexingModule` + Final Integration Check

**Why last:** `IndexingModule` depends on `IndexPipeline` (Task 5). After this task the full ingestion pipeline is wired and all S3 outputs are present. The final build and test run confirms the complete section is green before moving to Section 4.

**Files:**
- Create: `src/Ferret.Indexing/IndexingModule.cs`

**Interfaces:**
- Consumes: `IIndexPipeline`, `IndexPipeline` (Task 5)
- Produces: `IndexingModule` — used by S5 CLI host to register `IIndexPipeline`

- [ ] **Step 1: Write failing tests**

Add `IndexingModuleTests` class inside `tests/Ferret.Indexing.Tests/`:

Create `tests/Ferret.Indexing.Tests/IndexingModuleTests.cs`:

```csharp
using Ferret.Core.Indexing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferret.Indexing.Tests;

public sealed class IndexingModuleTests
{
    [Fact]
    public void ConfigureServices_Registers_IIndexPipeline_As_Singleton()
    {
        // IndexPipeline requires constructor arguments — register stubs first
        var services = new ServiceCollection();

        // Register required dependencies so DI can construct IndexPipeline
        services.AddSingleton<Ferret.Core.Connectors.IConnectorRegistry>(
            new Ferret.Indexing.Tests.Fakes.FakeConnectorRegistry([]));
        services.AddSingleton<Ferret.Core.Documents.IParserDispatcher>(
            new Ferret.Indexing.Tests.Fakes.FakeParserDispatcher());
        services.AddSingleton<IIndexEngine>(
            new Ferret.Indexing.Tests.Fakes.FakeIndexEngine());
        services.AddSingleton<Ferret.Core.Events.IEventBus>(
            new Ferret.Indexing.Tests.Fakes.FakeEventBus());
        services.AddSingleton(new Ferret.Core.Primitives.CorrelationId("test"));

        IndexingModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IIndexPipeline>());
    }

    [Fact]
    public void GetCommands_Returns_Empty_List()
    {
        var commands = IndexingModule.GetCommands();

        Assert.Empty(commands);
    }

    [Fact]
    public void IIndexEngine_Is_Not_Registered_By_IndexingModule()
    {
        // IIndexEngine registration is deferred to S5 CLI host (needs resolved db path)
        var services = new ServiceCollection();
        IndexingModule.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        Assert.Null(sp.GetService<IIndexEngine>());
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Indexing.Tests --filter "IndexingModuleTests"
```

Expected: FAIL — `IndexingModule` not found.

- [ ] **Step 3: Create `IndexingModule.cs`**

`src/Ferret.Indexing/IndexingModule.cs`:

```csharp
using Ferret.Core.Indexing;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Indexing;

/// <summary>
/// Registers <see cref="Ferret.Indexing"/> services into an <see cref="IServiceCollection"/>.
/// <para>
/// <see cref="IIndexEngine"/> is NOT registered here. The CLI host (S5) is responsible for
/// constructing <see cref="SqliteKeywordIndexEngine"/> with the workspace-resolved db path and
/// registering it as <see cref="IIndexEngine"/> before calling <see cref="ConfigureServices"/>.
/// This keeps <see cref="Ferret.Indexing"/> free of workspace path knowledge.
/// </para>
/// </summary>
public static class IndexingModule
{
    /// <summary>Registers <see cref="IIndexPipeline"/> as a singleton. Call after registering
    /// <see cref="IIndexEngine"/>, <see cref="Ferret.Core.Connectors.IConnectorRegistry"/>,
    /// <see cref="Ferret.Core.Documents.IParserDispatcher"/>, and <see cref="Ferret.Core.Events.IEventBus"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IIndexPipeline, IndexPipeline>();
    }

    /// <summary>Returns an empty list — <see cref="Ferret.Indexing"/> registers no CLI commands.
    /// CLI commands for indexing are registered by the CLI host (S5).</summary>
    public static IReadOnlyList<object> GetCommands() => [];
}
```

- [ ] **Step 4: Confirm green — full suite**

```
dotnet test tests/Ferret.Indexing.Tests
dotnet test tests/Ferret.Core.Tests
dotnet test tests/Ferret.Connectors.Filesystem.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass across all test projects, 0 build errors, 0 warnings.

---

## Section 3 Complete

**Outputs of Section 3:**

- `IAssetReader` — new Core interface (`Ferret.Core.Connectors`); separates content retrieval from discovery
- `DocumentDiscoveredEvent` — 8th indexing lifecycle event; published before each parse attempt
- `IndexResult.AssetsProcessed` — invariant: `AssetsProcessed = DocumentsIndexed + DocumentsSkipped + Failures`
- `IndexPipelineOptions.ForceRebuild` — controls whether `ClearAsync` is called before discovery
- `IIndexEngine.ClearAsync` — replaces `RebuildAsync`; storage engine never calls connectors or parsers
- ADR-0014 updated — Principle 9 (storage engines never own orchestration), `IIndexStore` reservation, corruption deferral note
- `Ferret.Indexing` project — 3 source files:
  - `SqliteKeywordIndexEngine` — two-table FTS5 schema, `user_version` guard, upsert, `ClearAsync`, parent-dir creation
  - `IndexPipeline` — full orchestration with `ForceRebuild`, 8 lifecycle events, per-asset failure isolation
  - `IndexingModule` — static module; registers only `IIndexPipeline`; `IIndexEngine` deferred to S5
- `FilesystemConnector` updated — implements `IAssetReader` (`OpenAsync` via `File.OpenRead`)
- `Ferret.Indexing.Tests` project — fakes + `SqliteKeywordIndexEngineTests` + `IndexPipelineTests` + `IndexingModuleTests`
- All existing tests still pass, `dotnet build src/Ferret.sln` clean

**What Section 4 (Connector Config CLI) depends on from Section 3:**

- Nothing from S3 directly. Section 4 depends on `IConnectorRegistry` (Sprint 8), workspace `connectors.json`, and `WorkspacePath` resolution — none of which are in S3.

**What Section 5 (Wire-up) depends on from Section 3:**

- `IIndexPipeline` — `IndexPipeline` implementation to call from `ferret index` command handler
- `SqliteKeywordIndexEngine` — to construct with the workspace-resolved db path (`WorkspacePath + ".ferret/indexes/keyword/keyword-index.db"`) and register as `IIndexEngine` before `IndexingModule.ConfigureServices`
- `IndexingModule.ConfigureServices` — to register `IIndexPipeline` in the DI container
- `IAssetReader` — `FilesystemConnector` already implements it; S5 does not need to change it
