# Sprint 14 S3: Performance and Memory Tuning Implementation Plan

> **For agentic workers:** Read this plan top-to-bottom before writing any code. Each task builds on the previous. Follow TDD strictly: write the failing test/benchmark assertion first, confirm it fails, then implement, then verify it passes. Commit after each task. Use `tokensave_context` as primary exploration tool.

---

## Goal

Add a `Ferret.Benchmarks` project using BenchmarkDotNet to measure and enforce performance targets:

- Index pipeline: < 60 s for 10 000 fake `.cs` files (200 chars each)
- Search: < 200 ms mean per query over 10 queries against a 1 000-file index
- Memory profiling via `[MemoryDiagnoser]` on all benchmarks

---

## Architecture

BenchmarkDotNet benchmarks are **not** unit tests — they live in a standalone console executable project under `tests/` and are invoked with `dotnet run`. They reference production `Ferret.Indexing`, `Ferret.Search`, and `Ferret.Core` assemblies directly and construct real implementations, wiring DI manually (same pattern as `SearchServiceTests` and `IndexPipelineTests`).

The project is added to the solution but excluded from test discovery (`IsTestProject` is not set to `true`).

---

## Tech Stack

- .NET 9, C# 13
- BenchmarkDotNet 0.14.0
- `[MemoryDiagnoser]` attribute on all benchmark classes
- xUnit is **not** used — benchmarks are standalone

---

## Global Constraints

- Central Package Management is enforced (`Directory.Packages.props`). BenchmarkDotNet must be declared there with a `Version` attribute; the `.csproj` must reference it without a version.
- All `.csproj` files use `<TargetFramework>net9.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- No `[MemoryDiagnoser]` on abstract classes or partial types — both benchmark classes are `public sealed`.
- All benchmark `[GlobalSetup]` methods must be async (`async Task`) because pipeline setup involves async work.
- The 60 s / 200 ms targets are enforced as inline `Assert`-style guards inside `[IterationCleanup]` (not BenchmarkDotNet validator extensions — keep it simple).
- Commit prefix: `feat(sprint-14):` for new files, `test(sprint-14):` for test/benchmark files.

---

## File Structure

```
Directory.Packages.props                            ← add BenchmarkDotNet version
tests/
  Ferret.Benchmarks/
    Ferret.Benchmarks.csproj                        ← Task 1
    Benchmarks/
      IndexPipelineBenchmark.cs                     ← Tasks 2, 4
      SearchBenchmark.cs                            ← Tasks 3, 4
    Program.cs                                      ← Task 1
```

---

## Task 1: Create `Ferret.Benchmarks` project

**Files:**
- `Directory.Packages.props` — add `BenchmarkDotNet` version entry
- `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` — project file
- `tests/Ferret.Benchmarks/Program.cs` — BenchmarkDotNet entrypoint
- `src/Ferret.sln` — add project reference

**Steps:**

- [ ] Add `BenchmarkDotNet` to `Directory.Packages.props` inside the existing `<ItemGroup>` blocks (add a new group labelled "Benchmarks"):

```xml
  <ItemGroup Label="Benchmarks">
    <PackageVersion Include="BenchmarkDotNet" Version="0.14.0" />
  </ItemGroup>
```

- [ ] Create `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>Ferret.Benchmarks</RootNamespace>
    <AssemblyName>Ferret.Benchmarks</AssemblyName>
    <!-- Optimise for accurate measurements -->
    <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
    <Optimize>true</Optimize>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Indexing\Ferret.Indexing.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Search\Ferret.Search.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ConnectorPlatform\Ferret.ConnectorPlatform.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ParserPlatform\Ferret.ParserPlatform.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Connectors.Filesystem\Ferret.Connectors.Filesystem.csproj" />
  </ItemGroup>

</Project>
```

- [ ] Create `tests/Ferret.Benchmarks/Program.cs`:

```csharp
using BenchmarkDotNet.Running;
using Ferret.Benchmarks;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args);
```

- [ ] Add the project to the solution:

```
dotnet sln src/Ferret.sln add tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj
```

- [ ] Verify the project builds in Release mode (no benchmark run yet):

```
dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release
```

---

## Task 2: `IndexPipelineBenchmark` — index 10 000 fake `.cs` files

**Files:**
- `tests/Ferret.Benchmarks/Benchmarks/IndexPipelineBenchmark.cs`

**Interfaces used:**
- `IIndexPipeline` (`Ferret.Core.Indexing`) — `Task<IndexResult> RunAsync(WorkspaceId, IndexPipelineOptions, CancellationToken)`
- `IndexResult` properties: `DocumentsIndexed`, `DocumentsSkipped`, `AssetsDiscovered`, `Failures`, `Duration` (all `required int` / `required TimeSpan`)
- `IndexPipelineOptions.Default` — shared default, no force rebuild
- `WorkspaceId.Create(string)` — factory

**Steps:**

- [ ] Create `tests/Ferret.Benchmarks/Benchmarks/IndexPipelineBenchmark.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.ParserPlatform;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>
/// Measures IIndexPipeline throughput for 10 000 fake .cs files (200 chars each).
/// Target: Duration &lt; 60 s.
/// Memory baseline captured by [MemoryDiagnoser].
/// </summary>
[MemoryDiagnoser]
public sealed class IndexPipelineBenchmark
{
    private static readonly WorkspaceId WorkspaceId = WorkspaceId.Create("bench-index");

    private IIndexPipeline _pipeline = null!;
    private string _tempDir = string.Empty;
    private SqliteKeywordIndexEngine _indexEngine = null!;
    private IndexResult _lastResult = null!;

    private const int FileCount = 10_000;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        // 1. Create temp directory with 10 000 fake .cs files, each exactly 200 chars
        _tempDir = Path.Combine(Path.GetTempPath(), $"ferret-bench-idx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var paddedContent = ("public class C { public void M() { } } ")
            .PadRight(200, ' ');

        for (var i = 0; i < FileCount; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_tempDir, $"File{i:D5}.cs"),
                paddedContent);
        }

        // 2. Build FilesystemConnector pointing at the temp directory
        var config = new FilesystemConnectorConfiguration
        {
            RootPath = _tempDir,
            IncludeExtensions = [".cs"],
            ExcludeExtensions = [],
        };
        var mimeResolver = new MimeTypeResolver();
        var connector = new FilesystemConnector(config, mimeResolver);

        // 3. Wrap the connector in a stub IConnectorManager
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("bench-fs"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Bench Filesystem",
        };
        var connectorManager = new SingleConnectorManager(connector, instance);

        // 4. Build parser dispatcher with plain-text parser (handles text/x-csharp as plain text)
        var parserRegistry = ParserRegistryBuilder.Build([new PlainTextParser()]);
        var parserDispatcher = new ParserDispatcher(parserRegistry);

        // 5. Build SQLite index engine backed by an in-memory SQLite database
        var dbPath = Path.Combine(_tempDir, "bench-index.db");
        _indexEngine = new SqliteKeywordIndexEngine(dbPath);

        _pipeline = new IndexPipeline(
            connectorManager,
            parserDispatcher,
            _indexEngine,
            NullEventBus.Instance);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _indexEngine?.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [IterationCleanup]
    public void AssertTarget()
    {
        // Enforce target: pipeline must complete within 60 s
        if (_lastResult is not null && _lastResult.Duration > TimeSpan.FromSeconds(60))
            throw new InvalidOperationException(
                $"IndexPipelineBenchmark EXCEEDED target: {_lastResult.Duration.TotalSeconds:F1}s > 60s");
    }

    [Benchmark]
    public async Task<IndexResult> RunPipeline_10kFiles()
    {
        _lastResult = await _pipeline.RunAsync(
            WorkspaceId,
            new IndexPipelineOptions { ForceRebuild = true });
        return _lastResult;
    }
}

/// <summary>
/// Minimal IConnectorManager that exposes a single pre-constructed connector as an active runtime.
/// Avoids the full ConnectorPlatform wiring (store + factory + DI) for benchmark simplicity.
/// </summary>
internal sealed class SingleConnectorManager : Ferret.Core.Connectors.IConnectorManager
{
    private readonly IReadOnlyList<ConnectorRuntime> _runtimes;

    public SingleConnectorManager(IConnector connector, ConnectorInstance instance)
    {
        var runtime = new ConnectorRuntime
        {
            Instance = instance,
            Connector = connector,
            Status = new ConnectorStatus
            {
                ConnectorId = instance.ConnectorType,
                InstanceId = instance.Id,
                IsActive = true,
                Health = ConnectorHealth.Connected(DateTimeOffset.UtcNow),
            },
        };
        _runtimes = [runtime];
    }

    public Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(CancellationToken ct = default) =>
        Task.FromResult(_runtimes);

    public Task<ConnectorInstance?> GetInstanceAsync(ConnectorInstanceId id, CancellationToken ct = default) =>
        Task.FromResult(_runtimes.FirstOrDefault(r => r.Instance.Id == id)?.Instance);
}
```

- [ ] Run the benchmark in a short smoke mode to confirm it compiles and executes:

```
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*IndexPipeline*' --job short
```

---

## Task 3: `SearchBenchmark` — 10 queries against a 1 000-file index

**Files:**
- `tests/Ferret.Benchmarks/Benchmarks/SearchBenchmark.cs`

**Interfaces used:**
- `ISearchService` (`Ferret.Core.Search`) — `Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)`
- `SearchOptions.Default` — shared default (`MaxResults = 10`, `Mode = SearchExecutionMode.Keyword`)
- `SearchServiceResult.ExecutionInfo.Duration` (`TimeSpan`) — per-query duration
- `SearchServiceStatus.Success` — assert success
- `SearchService` constructor: `SearchService(IQueryParser, IEnumerable<ISearchProvider>, IEnumerable<ISearchPostProcessor>)`
- `QueryParser` — `public sealed class QueryParser : IQueryParser`

**Steps:**

- [ ] Create `tests/Ferret.Benchmarks/Benchmarks/SearchBenchmark.cs`:

```csharp
using BenchmarkDotNet.Attributes;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Ferret.ParserPlatform;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;
using Microsoft.Data.Sqlite;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>
/// Measures ISearchService throughput: 10 keyword queries against a 1 000-file index.
/// Target: mean ExecutionInfo.Duration &lt; 200 ms per query.
/// Memory baseline captured by [MemoryDiagnoser].
/// </summary>
[MemoryDiagnoser]
public sealed class SearchBenchmark
{
    private ISearchService _searchService = null!;
    private string _tempDir = string.Empty;
    private SqliteKeywordIndexEngine _indexEngine = null!;
    private TimeSpan _lastMeanDuration;

    private const int IndexedFileCount = 1_000;

    private static readonly string[] Queries =
    [
        "authentication",
        "token manager",
        "workspace context",
        "pipeline runner",
        "index engine",
        "connector filesystem",
        "search provider",
        "document parser",
        "result aggregator",
        "query builder",
    ];

    [GlobalSetup]
    public async Task SetupAsync()
    {
        // 1. Write 1 000 fake .cs files to a temp directory
        _tempDir = Path.Combine(Path.GetTempPath(), $"ferret-bench-search-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        for (var i = 0; i < IndexedFileCount; i++)
        {
            var content = ($"// file {i} public class Service{i} execute authentication token workspace")
                .PadRight(200, ' ');
            await File.WriteAllTextAsync(
                Path.Combine(_tempDir, $"Service{i:D4}.cs"),
                content);
        }

        // 2. Build and run the index pipeline so documents exist in the SQLite index
        var config = new FilesystemConnectorConfiguration
        {
            RootPath = _tempDir,
            IncludeExtensions = [".cs"],
            ExcludeExtensions = [],
        };
        var mimeResolver = new MimeTypeResolver();
        var connector = new FilesystemConnector(config, mimeResolver);

        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("bench-search-fs"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Bench Search Filesystem",
        };
        var connectorManager = new SingleConnectorManager(connector, instance);

        var parserRegistry = ParserRegistryBuilder.Build([new PlainTextParser()]);
        var parserDispatcher = new ParserDispatcher(parserRegistry);

        // Bm25SearchProvider derives its DB path from:
        //   _workspace.WorkspaceRoot.FullPath + "/.ferret/indexes/keyword/keyword-index.db"
        // So we must create the SQLite index at that exact location.
        var indexDir = Path.Combine(_tempDir, ".ferret", "indexes", "keyword");
        Directory.CreateDirectory(indexDir);
        var dbPath = Path.Combine(indexDir, "keyword-index.db");
        _indexEngine = new SqliteKeywordIndexEngine(dbPath);

        var indexPipeline = new IndexPipeline(
            connectorManager,
            parserDispatcher,
            _indexEngine,
            NullEventBus.Instance);

        await indexPipeline.RunAsync(
            WorkspaceId.Create("bench-search"),
            IndexPipelineOptions.Default);

        // 3. Build Bm25SearchProvider backed by the same SQLite database.
        //    WorkspaceRoot points to _tempDir so GetDatabasePath() resolves to dbPath above.
        var workspaceContext = new BenchmarkWorkspaceContext(WorkspacePath.Create(_tempDir));
        var searchProvider = new Bm25SearchProvider(workspaceContext);

        _searchService = new SearchService(
            new QueryParser(),
            [searchProvider],
            []);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _indexEngine?.Dispose();
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [IterationCleanup]
    public void AssertTarget()
    {
        // Enforce target: mean query duration must be under 200 ms
        if (_lastMeanDuration > TimeSpan.FromMilliseconds(200))
            throw new InvalidOperationException(
                $"SearchBenchmark EXCEEDED target: mean {_lastMeanDuration.TotalMilliseconds:F1}ms > 200ms");
    }

    [Benchmark]
    public async Task<TimeSpan> Run10Queries()
    {
        var totalDuration = TimeSpan.Zero;

        foreach (var q in Queries)
        {
            var result = await _searchService.SearchAsync(q, SearchOptions.Default);

            if (result.IsSuccess && result.ExecutionInfo is not null)
                totalDuration += result.ExecutionInfo.Duration;
        }

        _lastMeanDuration = totalDuration / Queries.Length;
        return _lastMeanDuration;
    }
}
```

> **Implementation note on `Bm25SearchProvider`:** The provider takes `IWorkspaceContext` to locate the SQLite index database path. `BenchmarkWorkspaceContext` is a minimal implementation (see below) that returns the pre-computed `dbPath`. If `IWorkspaceContext` has a different surface, check `src/Ferret.Core/Workspace/IWorkspaceContext.cs` and adapt the stub accordingly.

- [ ] Add the `BenchmarkWorkspaceContext` stub at the bottom of `SearchBenchmark.cs` (same file, after the benchmark class):

```csharp
/// <summary>
/// Minimal IWorkspaceContext for benchmarks.
/// Bm25SearchProvider.GetDatabasePath() calls _workspace.WorkspaceRoot.FullPath,
/// so WorkspaceRoot must point to _tempDir; the DB is discovered at
/// _tempDir/.ferret/indexes/keyword/keyword-index.db automatically.
/// IWorkspaceContext has two members: WorkspaceId and WorkspaceRoot — both implemented here.
/// </summary>
internal sealed class BenchmarkWorkspaceContext : IWorkspaceContext
{
    public BenchmarkWorkspaceContext(WorkspacePath workspaceRoot)
    {
        WorkspaceRoot = workspaceRoot;
        WorkspaceId = WorkspaceId.Create("bench-search");
    }

    public WorkspaceId WorkspaceId { get; }
    public WorkspacePath WorkspaceRoot { get; }
}
```

- [ ] Run smoke check:

```
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*Search*' --job short
```

---

## Task 4: `[MemoryDiagnoser]` annotation and expected allocations documentation

**Files:**
- `tests/Ferret.Benchmarks/Benchmarks/IndexPipelineBenchmark.cs` — already has `[MemoryDiagnoser]`
- `tests/Ferret.Benchmarks/Benchmarks/SearchBenchmark.cs` — already has `[MemoryDiagnoser]`

Both benchmark classes were authored in Tasks 2 and 3 with `[MemoryDiagnoser]` already applied. This task documents what the attribute measures and records baseline expectations in code comments.

**Steps:**

- [ ] Verify `[MemoryDiagnoser]` is present on both classes (grep check):

```
grep -r "MemoryDiagnoser" tests/Ferret.Benchmarks/
```

Expected output:
```
tests/Ferret.Benchmarks/Benchmarks/IndexPipelineBenchmark.cs:[MemoryDiagnoser]
tests/Ferret.Benchmarks/Benchmarks/SearchBenchmark.cs:[MemoryDiagnoser]
```

- [ ] Add memory baseline XML-doc block at the top of `IndexPipelineBenchmark.cs` (replace the existing summary comment):

```csharp
/// <summary>
/// Measures IIndexPipeline throughput for 10 000 fake .cs files (200 chars each).
/// Target: Duration &lt; 60 s.
///
/// Memory baseline (BenchmarkDotNet MemoryDiagnoser columns):
///   Gen0  — expected &lt; 500 collections per 10 000-file run (transient parse buffers)
///   Gen1  — expected &lt; 50 collections (byte[] leased from ArrayPool, then released)
///   Alloc — expected &lt; 500 MB for 10 000 files (50 KB/file parse overhead)
///
/// If Gen0 &gt; 1000 or Alloc &gt; 1 GB, investigate IAssetReader stream pooling.
/// </summary>
```

- [ ] Add memory baseline XML-doc block at the top of `SearchBenchmark.cs` (replace the existing summary comment):

```csharp
/// <summary>
/// Measures ISearchService throughput: 10 keyword queries against a 1 000-file index.
/// Target: mean ExecutionInfo.Duration &lt; 200 ms per query.
///
/// Memory baseline (BenchmarkDotNet MemoryDiagnoser columns):
///   Gen0  — expected &lt; 20 collections per 10-query batch (query AST allocation)
///   Gen1  — expected 0 (no long-lived transient objects)
///   Alloc — expected &lt; 5 MB per 10-query batch (SearchResult + SearchHit records)
///
/// If Gen0 &gt; 100 or Alloc &gt; 50 MB, investigate SearchResult hit-list pooling.
/// </summary>
```

- [ ] Run both benchmarks together with memory output to capture a real baseline (use `--job short` for speed):

```
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*' --job short
```

The console output will include columns: `Mean`, `Error`, `StdDev`, `Gen0`, `Gen1`, `Allocated`. Record the actual values in a comment inside each class — this becomes the regression baseline for future sprints.

---

## Task 5: Running instructions

**Files:** none (documentation only, embedded in this plan)

**How to run all benchmarks (full precision):**

```bash
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*'
```

**How to run a specific benchmark:**

```bash
# Index pipeline only
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*IndexPipeline*'

# Search only
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*Search*'
```

**How to run in short mode (fast CI smoke check, less accurate):**

```bash
dotnet run --project tests/Ferret.Benchmarks -c Release -- --filter '*' --job short
```

**Important:** Always run in `-c Release`. Debug builds disable JIT optimizations; benchmark numbers from Debug builds are meaningless and will violate the targets trivially.

**Expected output columns** (BenchmarkDotNet default + MemoryDiagnoser):

| Column    | Description                                      |
|-----------|--------------------------------------------------|
| Mean      | Arithmetic mean of all measured iterations       |
| Error     | Half of 99.9% confidence interval                |
| StdDev    | Standard deviation of all measurements           |
| Gen0      | GC Gen0 collections per 1000 operations          |
| Gen1      | GC Gen1 collections per 1000 operations          |
| Allocated | Memory allocated per operation (bytes)           |

**Performance targets enforced at runtime:**

| Benchmark                          | Target        | Enforcement                          |
|------------------------------------|---------------|--------------------------------------|
| `IndexPipelineBenchmark.RunPipeline_10kFiles` | Duration < 60 s | `[IterationCleanup]` throws if exceeded |
| `SearchBenchmark.Run10Queries`     | Mean < 200 ms | `[IterationCleanup]` throws if exceeded |
