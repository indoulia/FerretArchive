# Ferret Benchmark Suite — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an automated Engineering Productivity Benchmark Suite for Ferret RC1 covering platform performance, scale, context quality, context effectiveness (token efficiency), and context assembly stage timing.

**Architecture:** BenchmarkDotNet for Platform, Scale, and Stage benchmarks; a custom runner for Context Quality and Context Effectiveness; a PowerShell orchestration script that runs all phases and writes versioned reports to `docs/benchmarks/RC1/`.

**Tech Stack:** .NET 9, BenchmarkDotNet 0.14+, SQLite FTS5 (Ferret.Search), Ferret.AI.Context, System.Text.Json for eval dataset.

**Spec:** `docs/superpowers/specs/2026-06-30-benchmark-suite-spec.md`

## Global Constraints

- Target framework: net9.0
- No live LLM API calls in automated benchmarks (AI benchmarks measure token estimates only)
- BenchmarkDotNet benchmarks must run in Release configuration
- All file paths in benchmark code are absolute, derived from `Path.GetTempPath()` — never hardcoded
- Follow patterns from `tests/Ferret.Indexing.Tests/EndToEnd/EndToEndIndexPipelineTests.cs:BuildRealPipeline` for pipeline construction
- No new project references beyond what is listed in each task
- ANTHROPIC_API_KEY is optional; live-call extension is gated behind env var check
- Benchmark output artifacts go to `BenchmarkDotNet.Artifacts/` (already gitignored)
- Quality/ContextEffectiveness runner output goes to `docs/benchmarks/RC1/`
- "AI Benchmark" is renamed to "Context Effectiveness" throughout — we are benchmarking Ferret's context, not models
- Real-corpus runs (Ferret-self + external repos) are opt-in via `--real-corpus` flag; not default CI
- TTFUC (Time to First Useful Context) is the primary signature metric — end-to-end `ContextAssembler.AssembleAsync` latency

---

### Task 1: Project Setup — csproj + BenchmarkSetupBase

**Files:**
- Modify: `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj`
- Create: `tests/Ferret.Benchmarks/BenchmarkSetupBase.cs`

**Interfaces:**
- Produces: `BenchmarkSetupBase` abstract class with `BuildIndexPipeline(string rootPath, string dbPath)` and `BuildSearchService(string dbPath, string rootPath)` static helpers; used by all benchmark classes in Tasks 2–5.

- [ ] **Step 1: Add project references to csproj**

Replace the `<ItemGroup>` block in `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` so it reads:

```xml
  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Ferret.Core\Ferret.Core.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Indexing\Ferret.Indexing.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Search\Ferret.Search.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ConnectorPlatform\Ferret.ConnectorPlatform.csproj" />
    <ProjectReference Include="..\..\src\Ferret.ParserPlatform\Ferret.ParserPlatform.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Connectors.Filesystem\Ferret.Connectors.Filesystem.csproj" />
    <ProjectReference Include="..\..\src\Ferret.AI\Ferret.AI.csproj" />
    <ProjectReference Include="..\..\src\Ferret.Mcp\Ferret.Mcp.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Write BenchmarkSetupBase.cs**

```csharp
using Ferret.AI.Context;
using Ferret.ConnectorPlatform;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Ferret.Indexing.Stores;
using Ferret.ParserPlatform;
using Ferret.ParserPlatform.Parsers;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Benchmarks;

/// <summary>
/// Shared pipeline-construction helpers used by all benchmark classes.
/// Mirrors the pattern from EndToEndIndexPipelineTests.BuildRealPipeline.
/// </summary>
internal static class BenchmarkSetupBase
{
    internal static IndexPipeline BuildIndexPipeline(string rootPath, string dbPath)
    {
        var mimeResolver = new MimeTypeResolver();
        var parserRegistry = ParserRegistryBuilder.Build(
            [new PlainTextParser(), new MarkdownParser(), new JsonParser()]);
        var dispatcher = new ParserDispatcher(parserRegistry);

        var store = new ConnectorInstanceStore();
        var workspacePath = WorkspacePath.Create(rootPath);
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("bench-instance"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Benchmark Filesystem",
            IsEnabled = true,
            Configuration = ConnectorConfiguration.FromDictionary(
                new Dictionary<string, string> { ["rootPath"] = rootPath }),
        };
        store.SaveAsync(workspacePath, [instance], CancellationToken.None).GetAwaiter().GetResult();

        var factory = new FilesystemConnectorFactory(
            new FilesystemConnectorConfiguration { RootPath = rootPath },
            mimeResolver);
        var manager = ConnectorPlatformFactory.CreateConnectorManager(store, [factory], workspacePath);

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var engine = new SqliteKeywordIndexEngine(dbPath);

        return new IndexPipeline(manager, dispatcher, engine, NullEventBus.Instance);
    }

    internal static SearchService BuildSearchService(string rootPath)
    {
        var workspaceContext = new BenchmarkWorkspaceContext(rootPath);
        var provider = new Bm25SearchProvider(workspaceContext);
        var parser = new DefaultQueryParser();
        return new SearchService(parser, [provider], []);
    }

    internal static string NewTempDir() =>
        Path.Combine(Path.GetTempPath(), $"ferret-bench-{Guid.NewGuid():N}");
}

internal sealed class BenchmarkWorkspaceContext : IWorkspaceContext
{
    private readonly WorkspacePath _path;

    internal BenchmarkWorkspaceContext(string rootPath) =>
        _path = WorkspacePath.Create(rootPath);

    public WorkspacePath WorkspaceRoot => _path;
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

- [ ] **Step 4: Commit**

```powershell
git add tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj tests/Ferret.Benchmarks/BenchmarkSetupBase.cs
git commit -m "feat(benchmarks): project setup + BenchmarkSetupBase helpers"
```

---

### Task 2: Platform Benchmarks — IndexBenchmarks

**Files:**
- Create: `tests/Ferret.Benchmarks/Platform/IndexBenchmarks.cs`
- Create: `tests/Ferret.Benchmarks/Platform/TestCorpusGenerator.cs`

**Interfaces:**
- Consumes: `BenchmarkSetupBase.BuildIndexPipeline(rootPath, dbPath)` from Task 1
- Produces: `TestCorpusGenerator.GenerateAsync(directory, count)` used by Tasks 2, 3, 5

- [ ] **Step 1: Write TestCorpusGenerator.cs**

```csharp
namespace Ferret.Benchmarks.Platform;

/// <summary>
/// Generates synthetic Markdown files for benchmark corpora.
/// Each file has realistic word distribution (not random noise) so FTS5 ranking behaves realistically.
/// </summary>
internal static class TestCorpusGenerator
{
    private static readonly string[] Topics =
    [
        "connector", "indexing", "search", "parser", "workspace", "document",
        "pipeline", "plugin", "configuration", "context", "assembly", "provider",
        "token", "query", "filter", "result", "benchmark", "ferret", "engine", "sqlite"
    ];

    internal static async Task GenerateAsync(string directory, int fileCount)
    {
        Directory.CreateDirectory(directory);
        for (int i = 0; i < fileCount; i++)
        {
            var topic1 = Topics[i % Topics.Length];
            var topic2 = Topics[(i + 3) % Topics.Length];
            var content = $"""
                # {topic1} Module — File {i:D5}

                This document describes the {topic1} subsystem and its relationship to {topic2}.
                The {topic1} component processes {topic2} requests and returns structured results.
                Configuration options for {topic1} include timeout, retries, and {topic2} depth.

                ## Implementation Notes

                The {topic1} pipeline consists of three stages: discovery, processing, and emission.
                Each stage transforms the {topic2} input into a richer {topic1} output.
                Error handling in {topic1} follows the Result pattern — no exceptions cross boundaries.

                ## API Surface

                - `{topic1}Service.RunAsync(request)` — primary entry point
                - `{topic1}Options.Default` — sensible defaults for {topic2} workloads
                - `{topic1}Result.IsSuccess` — indicates pipeline completion

                ## Related: {topic2}

                The {topic2} subsystem depends on {topic1} for its core functionality.
                See the {topic2} design doc for details on the integration contract.
                """;

            await File.WriteAllTextAsync(
                Path.Combine(directory, $"{topic1}-{i:D5}.md"),
                content).ConfigureAwait(false);
        }
    }
}
```

- [ ] **Step 2: Write IndexBenchmarks.cs**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ferret.Core.Primitives;
using Ferret.Indexing;

namespace Ferret.Benchmarks.Platform;

/// <summary>
/// Measures full-index and incremental-index throughput for realistic corpora.
/// Run with: dotnet run -c Release -- --filter *IndexBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class IndexBenchmarks
{
    private string _corpusDir = null!;
    private string _dbPath = null!;
    private IndexPipeline _pipeline = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _corpusDir = BenchmarkSetupBase.NewTempDir();
        await TestCorpusGenerator.GenerateAsync(_corpusDir, fileCount: 1000);
        _dbPath = Path.Combine(_corpusDir, ".ferret", "bench.db");
        _pipeline = BenchmarkSetupBase.BuildIndexPipeline(_corpusDir, _dbPath);

        // Prime the pipeline (first run allocates DB schema).
        await _pipeline.RunAsync(
            WorkspaceId.Create("bench-prime"),
            IndexPipelineOptions.Default,
            CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_corpusDir))
            Directory.Delete(_corpusDir, recursive: true);
    }

    [Benchmark(Description = "Full index — 1,000 files (force rebuild)")]
    public async Task FullIndex_1000Files()
    {
        await _pipeline.RunAsync(
            WorkspaceId.Create("bench-full"),
            new IndexPipelineOptions { ForceRebuild = true },
            CancellationToken.None);
    }

    [Benchmark(Description = "Incremental index — 1 new file added")]
    public async Task IncrementalIndex_OneNewFile()
    {
        var newFile = Path.Combine(_corpusDir, $"incremental-{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(newFile,
            "# New Incremental Document\n\nContent for incremental index benchmark.");
        await _pipeline.RunAsync(
            WorkspaceId.Create("bench-incremental"),
            IndexPipelineOptions.Default,
            CancellationToken.None);
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

- [ ] **Step 4: Smoke-run in Debug to verify setup doesn't crash**

Run: `dotnet run --project tests/Ferret.Benchmarks -c Debug -- --filter *IndexBenchmarks* --job dry`
Expected: Dry run completes without exceptions, shows benchmark names

- [ ] **Step 5: Commit**

```powershell
git add tests/Ferret.Benchmarks/Platform/
git commit -m "feat(benchmarks): IndexBenchmarks + TestCorpusGenerator"
```

---

### Task 3: Platform Benchmarks — SearchBenchmarks + ContextAssemblyBenchmarks + Stage Benchmarks

**Files:**
- Create: `tests/Ferret.Benchmarks/Platform/SearchBenchmarks.cs`
- Create: `tests/Ferret.Benchmarks/Platform/ContextAssemblyBenchmarks.cs` — measures TTFUC (full pipeline)
- Create: `tests/Ferret.Benchmarks/Platform/ContextAssemblyStageBenchmarks.cs` — measures each stage individually

**Interfaces:**
- Consumes: `BenchmarkSetupBase.BuildIndexPipeline`, `BenchmarkSetupBase.BuildSearchService`, `TestCorpusGenerator.GenerateAsync` from Tasks 1–2
- Consumes: `ContextAssembler`, `ContextDeduplicator`, `ContentFilter`, `DocumentExpander`, `TokenEstimator` from `Ferret.AI`
- Produces: TTFUC (full pipeline), per-stage latencies (Search/Dedup/Expand/Filter/Budget), search latency

- [ ] **Step 1: Write SearchBenchmarks.cs**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ferret.Core.Search;
using Ferret.Search;

namespace Ferret.Benchmarks.Platform;

/// <summary>
/// Measures keyword search latency against a pre-built 1,000-file corpus.
/// Run with: dotnet run -c Release -- --filter *SearchBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class SearchBenchmarks
{
    private SearchService _searchService = null!;
    private string _corpusDir = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _corpusDir = BenchmarkSetupBase.NewTempDir();
        await TestCorpusGenerator.GenerateAsync(_corpusDir, fileCount: 1000);
        var dbPath = Path.Combine(_corpusDir, ".ferret", "bench.db");

        // Build and run the index so the DB is populated before measuring search.
        var pipeline = BenchmarkSetupBase.BuildIndexPipeline(_corpusDir, dbPath);
        await pipeline.RunAsync(
            Ferret.Core.Workspace.WorkspaceId.Create("bench-search-setup"),
            Ferret.Indexing.IndexPipelineOptions.Default,
            CancellationToken.None);

        _searchService = BenchmarkSetupBase.BuildSearchService(_corpusDir);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_corpusDir))
            Directory.Delete(_corpusDir, recursive: true);
    }

    [Benchmark(Description = "Search — single keyword")]
    public async Task Search_SingleKeyword()
    {
        await _searchService.SearchAsync("connector", new SearchOptions { MaxResults = 10 });
    }

    [Benchmark(Description = "Search — multi-keyword phrase")]
    public async Task Search_MultiKeyword()
    {
        await _searchService.SearchAsync("connector indexing pipeline", new SearchOptions { MaxResults = 10 });
    }

    [Benchmark(Description = "Search — no results (cold miss)")]
    public async Task Search_NoResults()
    {
        await _searchService.SearchAsync("zzznomatch", new SearchOptions { MaxResults = 10 });
    }
}
```

- [ ] **Step 2: Write ContextAssemblyBenchmarks.cs**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ferret.AI.Context;
using Ferret.Core.Context;
using Ferret.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Benchmarks.Platform;

/// <summary>
/// Measures context assembly end-to-end latency (search → deduplicate → expand → filter → budget).
/// Run with: dotnet run -c Release -- --filter *ContextAssemblyBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ContextAssemblyBenchmarks
{
    private ContextAssembler _assembler = null!;
    private string _corpusDir = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _corpusDir = BenchmarkSetupBase.NewTempDir();
        await TestCorpusGenerator.GenerateAsync(_corpusDir, fileCount: 1000);
        var dbPath = Path.Combine(_corpusDir, ".ferret", "bench.db");

        var pipeline = BenchmarkSetupBase.BuildIndexPipeline(_corpusDir, dbPath);
        await pipeline.RunAsync(
            Ferret.Core.Workspace.WorkspaceId.Create("bench-ctx-setup"),
            Ferret.Indexing.IndexPipelineOptions.Default,
            CancellationToken.None);

        var searchService = BenchmarkSetupBase.BuildSearchService(_corpusDir);

        // DocumentExpander requires IDocumentService — use the filesystem connector's document service.
        var documentService = BenchmarkSetupBase.BuildDocumentService(_corpusDir);
        var expander = new DocumentExpander(documentService, NullLogger<DocumentExpander>.Instance);
        _assembler = new ContextAssembler(searchService, expander, NullLogger<ContextAssembler>.Instance);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_corpusDir))
            Directory.Delete(_corpusDir, recursive: true);
    }

    [Benchmark(Description = "Context assembly — 10 documents max")]
    public async Task AssembleContext_10Docs()
    {
        var request = new ContextRequest
        {
            Query = "connector indexing pipeline",
            MaxDocuments = 10,
            MaxTokens = 8000,
        };
        await _assembler.AssembleAsync(request, CancellationToken.None);
    }

    [Benchmark(Description = "Context assembly — 5 documents max")]
    public async Task AssembleContext_5Docs()
    {
        var request = new ContextRequest
        {
            Query = "search provider bm25",
            MaxDocuments = 5,
            MaxTokens = 4000,
        };
        await _assembler.AssembleAsync(request, CancellationToken.None);
    }
}
```

- [ ] **Step 3: Write ContextAssemblyStageBenchmarks.cs**

This benchmark calls each pipeline stage directly (not via `ContextAssembler`) to isolate per-stage cost.

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ferret.AI.Context;
using Ferret.Core.Context;
using Ferret.Core.Search;
using Ferret.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Benchmarks.Platform;

/// <summary>
/// Benchmarks each context assembly stage independently to identify optimization targets.
/// Run with: dotnet run -c Release -- --filter *ContextAssemblyStageBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ContextAssemblyStageBenchmarks
{
    private SearchService _searchService = null!;
    private DocumentExpander _expander = null!;
    private IReadOnlyList<SearchHit> _searchHits = null!;
    private string _corpusDir = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _corpusDir = BenchmarkSetupBase.NewTempDir();
        await TestCorpusGenerator.GenerateAsync(_corpusDir, fileCount: 1000);
        var dbPath = Path.Combine(_corpusDir, ".ferret", "bench.db");

        var pipeline = BenchmarkSetupBase.BuildIndexPipeline(_corpusDir, dbPath);
        await pipeline.RunAsync(
            Ferret.Core.Workspace.WorkspaceId.Create("stage-bench-setup"),
            Ferret.Indexing.IndexPipelineOptions.Default,
            CancellationToken.None);

        _searchService = BenchmarkSetupBase.BuildSearchService(_corpusDir);
        var documentService = BenchmarkSetupBase.BuildDocumentService(_corpusDir);
        _expander = new DocumentExpander(documentService, NullLogger<DocumentExpander>.Instance);

        // Pre-run search once so we can benchmark downstream stages in isolation.
        var result = await _searchService.SearchAsync(
            "connector indexing pipeline", new SearchOptions { MaxResults = 20 });
        _searchHits = result.IsSuccess ? result.Hits : [];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_corpusDir))
            Directory.Delete(_corpusDir, recursive: true);
    }

    [Benchmark(Description = "Stage 1 — Search (BM25 FTS5)")]
    public async Task Stage_Search()
    {
        await _searchService.SearchAsync(
            "connector indexing pipeline", new SearchOptions { MaxResults = 20 });
    }

    [Benchmark(Description = "Stage 2 — Deduplication")]
    public IReadOnlyList<SearchHit> Stage_Dedup()
    {
        return ContextDeduplicator.Deduplicate(_searchHits);
    }

    [Benchmark(Description = "Stage 3 — Document Expand")]
    public async Task Stage_Expand()
    {
        var deduped = ContextDeduplicator.Deduplicate(_searchHits);
        await _expander.ExpandAsync(deduped, CancellationToken.None);
    }

    [Benchmark(Description = "Stage 4 — Content Filter")]
    public async Task Stage_Filter()
    {
        var deduped = ContextDeduplicator.Deduplicate(_searchHits);
        var documents = await _expander.ExpandAsync(deduped, CancellationToken.None);
        ContentFilter.Filter(documents);
    }
}
```

> **Note:** If `ContextDeduplicator.Deduplicate`, `ContentFilter.Filter`, or `DocumentExpander.ExpandAsync` are `internal`, add `[assembly: InternalsVisibleTo("Ferret.Benchmarks")]` to `Ferret.AI/AssemblyInfo.cs` (or the project file), matching the pattern used by other test projects in this solution.

- [ ] **Step 5: Locate IDocumentService and add BuildDocumentService to BenchmarkSetupBase**

Check `src/Ferret.Core/Documents/` or `src/Ferret.Connectors.Filesystem/` for a `FilesystemDocumentService` or similar. Add to `BenchmarkSetupBase.cs`:

```csharp
internal static IDocumentService BuildDocumentService(string rootPath)
{
    // FilesystemDocumentService reads file content from disk given a document URI.
    // Locate the concrete type in Ferret.Connectors.Filesystem and instantiate it.
    // If the type requires a connector manager, reuse BuildIndexPipeline's manager pattern.
    return new FilesystemDocumentService(rootPath);
}
```

> **Note:** If `FilesystemDocumentService` doesn't exist, grep for `IDocumentService` implementations: `grep -r "IDocumentService" src/ --include="*.cs" -l` and use the real type name.

- [ ] **Step 6: Build and smoke-run**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

Run: `dotnet run --project tests/Ferret.Benchmarks -c Debug -- --filter *SearchBenchmarks* --job dry`
Expected: Dry run completes, shows 3 benchmark methods

Run: `dotnet run --project tests/Ferret.Benchmarks -c Debug -- --filter *ContextAssemblyStageBenchmarks* --job dry`
Expected: Dry run shows 4 stage benchmarks

- [ ] **Step 7: Commit**

```powershell
git add tests/Ferret.Benchmarks/Platform/SearchBenchmarks.cs tests/Ferret.Benchmarks/Platform/ContextAssemblyBenchmarks.cs tests/Ferret.Benchmarks/Platform/ContextAssemblyStageBenchmarks.cs tests/Ferret.Benchmarks/BenchmarkSetupBase.cs
git commit -m "feat(benchmarks): SearchBenchmarks + ContextAssemblyBenchmarks (TTFUC) + stage breakdown"
```

---

### Task 4: Scale Benchmarks

**Files:**
- Create: `tests/Ferret.Benchmarks/Scale/ScaleIndexBenchmarks.cs`

**Interfaces:**
- Consumes: `TestCorpusGenerator.GenerateAsync`, `BenchmarkSetupBase.BuildIndexPipeline` from Tasks 1–2
- Produces: indexed throughput numbers per corpus size, used in the RC1 report table

- [ ] **Step 1: Write ScaleIndexBenchmarks.cs**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ferret.Core.Workspace;
using Ferret.Indexing;

namespace Ferret.Benchmarks.Scale;

/// <summary>
/// Measures index throughput across corpus sizes (200, 2000, 10000 files).
/// Run with: dotnet run -c Release -- --filter *ScaleIndexBenchmarks*
/// WARNING: 10,000-file corpus takes significant time. Run overnight or in CI.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class ScaleIndexBenchmarks
{
    [Params(200, 2000, 10000)]
    public int FileCount { get; set; }

    private string _corpusDir = null!;
    private string _dbPath = null!;
    private IndexPipeline _pipeline = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _corpusDir = BenchmarkSetupBase.NewTempDir();
        await Ferret.Benchmarks.Platform.TestCorpusGenerator.GenerateAsync(_corpusDir, FileCount);
        _dbPath = Path.Combine(_corpusDir, ".ferret", "scale-bench.db");
        _pipeline = BenchmarkSetupBase.BuildIndexPipeline(_corpusDir, _dbPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_corpusDir))
            Directory.Delete(_corpusDir, recursive: true);
    }

    [Benchmark(Description = "Full index — N files (force rebuild)")]
    public async Task FullIndex_NFiles()
    {
        await _pipeline.RunAsync(
            WorkspaceId.Create($"bench-scale-{FileCount}"),
            new IndexPipelineOptions { ForceRebuild = true },
            CancellationToken.None);
    }
}
```

- [ ] **Step 2: Build and smoke-run with smallest param only**

Run: `dotnet run --project tests/Ferret.Benchmarks -c Debug -- --filter *ScaleIndexBenchmarks* --job dry`
Expected: Dry run shows `FileCount=200`, `FileCount=2000`, `FileCount=10000` parameter variants

- [ ] **Step 3: Commit**

```powershell
git add tests/Ferret.Benchmarks/Scale/ScaleIndexBenchmarks.cs
git commit -m "feat(benchmarks): ScaleIndexBenchmarks — parameterized corpus sizes 200/2000/10000"
```

---

### Task 5: Context Quality Eval — Eval Dataset + Runner

**Files:**
- Create: `tests/Ferret.Benchmarks/Quality/EvalDataset/eval-dataset.json`
- Create: `tests/Ferret.Benchmarks/Quality/ContextQualityReport.cs`
- Create: `tests/Ferret.Benchmarks/Quality/ContextQualityRunner.cs`

**Interfaces:**
- Consumes: `BenchmarkSetupBase.BuildSearchService` and `BenchmarkSetupBase.BuildIndexPipeline` from Task 1
- Produces: `ContextQualityReport` with `Precision`, `Recall`, `MeanReciprocalRank`, `TokenCount` fields; runner writes JSON to `docs/benchmarks/reports/quality-YYYY-MM-DD.json`

- [ ] **Step 1: Write eval-dataset.json**

The dataset contains 20 Q&A pairs where each entry lists the query and the expected relevant document titles (relative to the Ferret repo). To run against Ferret's own source, the runner indexes `src/`.

```json
[
  {
    "id": "q01",
    "query": "how do I add a connector",
    "relevant_titles": ["IConnector", "ConnectorPlatformFactory", "FilesystemConnectorFactory", "ConnectorInstance"]
  },
  {
    "id": "q02",
    "query": "how does the index pipeline work",
    "relevant_titles": ["IndexPipeline", "IndexPipelineOptions", "IndexPipelineResult", "SqliteKeywordIndexEngine"]
  },
  {
    "id": "q03",
    "query": "search bm25 fts5",
    "relevant_titles": ["Bm25SearchProvider", "SearchService", "QueryTranslator", "ISearchProvider"]
  },
  {
    "id": "q04",
    "query": "context assembly pipeline",
    "relevant_titles": ["ContextAssembler", "ContextRequest", "ContextPackage", "DocumentExpander", "ContentFilter"]
  },
  {
    "id": "q05",
    "query": "MCP tool implementation",
    "relevant_titles": ["ContextTool", "SearchTool", "McpServer"]
  },
  {
    "id": "q06",
    "query": "workspace configuration",
    "relevant_titles": ["WorkspaceContext", "WorkspacePath", "WorkspaceId"]
  },
  {
    "id": "q07",
    "query": "plugin loading and discovery",
    "relevant_titles": ["PluginLoader", "IPlugin", "PluginDescriptor"]
  },
  {
    "id": "q08",
    "query": "parser platform markdown",
    "relevant_titles": ["MarkdownParser", "ParserDispatcher", "ParserRegistryBuilder", "IParser"]
  },
  {
    "id": "q09",
    "query": "incremental indexing changed files",
    "relevant_titles": ["IndexPipeline", "IndexPipelineOptions", "ConnectorManager"]
  },
  {
    "id": "q10",
    "query": "search result deduplication",
    "relevant_titles": ["ContextDeduplicator", "SearchHit", "DocumentId"]
  },
  {
    "id": "q11",
    "query": "token budget context window",
    "relevant_titles": ["TokenEstimator", "ContextAssembler", "ContextRequest"]
  },
  {
    "id": "q12",
    "query": "document service fetch content",
    "relevant_titles": ["IDocumentService", "DocumentExpander"]
  },
  {
    "id": "q13",
    "query": "query parser keywords",
    "relevant_titles": ["DefaultQueryParser", "KeywordExpression", "SearchQuery", "IQueryParser"]
  },
  {
    "id": "q14",
    "query": "telemetry events",
    "relevant_titles": ["ITelemetryCollector", "TelemetryEvent", "NullEventBus"]
  },
  {
    "id": "q15",
    "query": "CLI commands context assemble",
    "relevant_titles": ["ContextAssembleCommandHandler", "FerretCli"]
  },
  {
    "id": "q16",
    "query": "search post processors",
    "relevant_titles": ["ISearchPostProcessor", "SearchService"]
  },
  {
    "id": "q17",
    "query": "configuration AI provider",
    "relevant_titles": ["AiConfiguration", "OpenAiProvider", "OllamaProvider"]
  },
  {
    "id": "q18",
    "query": "mime type resolver binary detection",
    "relevant_titles": ["MimeTypeResolver", "FilesystemConnectorFactory"]
  },
  {
    "id": "q19",
    "query": "connector instance store persistence",
    "relevant_titles": ["ConnectorInstanceStore", "ConnectorInstance", "ConnectorConfiguration"]
  },
  {
    "id": "q20",
    "query": "search options max results mode",
    "relevant_titles": ["SearchOptions", "SearchExecutionMode", "SearchService"]
  }
]
```

- [ ] **Step 2: Write ContextQualityReport.cs**

```csharp
using System.Text.Json.Serialization;

namespace Ferret.Benchmarks.Quality;

public sealed class ContextQualityReport
{
    [JsonPropertyName("run_date")]
    public string RunDate { get; init; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [JsonPropertyName("corpus_path")]
    public string CorpusPath { get; init; } = string.Empty;

    [JsonPropertyName("dataset_size")]
    public int DatasetSize { get; init; }

    [JsonPropertyName("k")]
    public int K { get; init; }

    // Standard precision/recall
    [JsonPropertyName("precision_at_k")]
    public double PrecisionAtK { get; init; }

    [JsonPropertyName("recall_at_k")]
    public double RecallAtK { get; init; }

    [JsonPropertyName("mean_reciprocal_rank")]
    public double MeanReciprocalRank { get; init; }

    // IR standard metrics
    [JsonPropertyName("ndcg_at_10")]
    public double NdcgAt10 { get; init; }

    [JsonPropertyName("success_at_1")]
    public double SuccessAt1 { get; init; }

    [JsonPropertyName("success_at_5")]
    public double SuccessAt5 { get; init; }

    [JsonPropertyName("success_at_10")]
    public double SuccessAt10 { get; init; }

    [JsonPropertyName("avg_token_count")]
    public int AvgTokenCount { get; init; }

    [JsonPropertyName("per_query")]
    public List<QueryResult> PerQuery { get; init; } = [];
}

public sealed class QueryResult
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;

    [JsonPropertyName("relevant_retrieved")]
    public int RelevantRetrieved { get; init; }

    [JsonPropertyName("total_relevant")]
    public int TotalRelevant { get; init; }

    [JsonPropertyName("total_retrieved")]
    public int TotalRetrieved { get; init; }

    [JsonPropertyName("reciprocal_rank")]
    public double ReciprocalRank { get; init; }

    [JsonPropertyName("ndcg")]
    public double Ndcg { get; init; }

    // Success@k: true if at least one relevant doc was in top k
    [JsonPropertyName("success_at_1")]
    public bool SuccessAt1 { get; init; }

    [JsonPropertyName("success_at_5")]
    public bool SuccessAt5 { get; init; }

    [JsonPropertyName("success_at_10")]
    public bool SuccessAt10 { get; init; }

    [JsonPropertyName("token_count")]
    public int TokenCount { get; init; }
}
```

- [ ] **Step 3: Write ContextQualityRunner.cs**

```csharp
using System.Text.Json;
using Ferret.AI.Context;
using Ferret.Core.Context;
using Ferret.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Benchmarks.Quality;

/// <summary>
/// Runs the context quality eval against an indexed corpus.
/// Usage: call RunAsync(corpusPath, k, outputPath) — not a BenchmarkDotNet benchmark.
/// </summary>
public sealed class ContextQualityRunner
{
    private readonly ContextAssembler _assembler;

    public ContextQualityRunner(string corpusPath)
    {
        var searchService = BenchmarkSetupBase.BuildSearchService(corpusPath);
        var documentService = BenchmarkSetupBase.BuildDocumentService(corpusPath);
        var expander = new DocumentExpander(documentService, NullLogger<DocumentExpander>.Instance);
        _assembler = new ContextAssembler(searchService, expander, NullLogger<ContextAssembler>.Instance);
    }

    public static async Task<ContextQualityReport> RunAsync(
        string corpusPath,
        string datasetPath,
        int k = 10,
        string? outputPath = null,
        CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(datasetPath, ct);
        var dataset = JsonSerializer.Deserialize<List<EvalEntry>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize eval-dataset.json");

        var runner = new ContextQualityRunner(corpusPath);
        var queryResults = new List<QueryResult>();

        foreach (var entry in dataset)
        {
            var request = new ContextRequest
            {
                Query = entry.Query,
                MaxDocuments = k,
                MaxTokens = 16000,
            };

            var package = await runner._assembler.AssembleAsync(request, ct);

            var docs = package.Documents;

            // Per-document relevance flags (binary: relevant title substring match).
            var relevanceFlags = docs
                .Select(d => entry.RelevantTitles.Any(t =>
                    (d.Title ?? d.Id.Value).Contains(t, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            int relevantRetrieved = relevanceFlags.Count(f => f);

            // Reciprocal rank: position of first relevant result.
            double rr = 0;
            for (int i = 0; i < relevanceFlags.Count; i++)
            {
                if (relevanceFlags[i]) { rr = 1.0 / (i + 1); break; }
            }

            // nDCG@10 with binary relevance (grade 1 = relevant, 0 = not).
            // DCG = sum(rel_i / log2(i+2)) for i in [0, min(k,10)).
            double dcg = 0;
            double idcg = 0;
            int numRelevant = entry.RelevantTitles.Count;
            for (int i = 0; i < Math.Min(docs.Count, 10); i++)
            {
                if (relevanceFlags[i]) dcg += 1.0 / Math.Log2(i + 2);
            }
            for (int i = 0; i < Math.Min(numRelevant, 10); i++)
            {
                idcg += 1.0 / Math.Log2(i + 2);
            }
            double ndcg = idcg == 0 ? 0 : dcg / idcg;

            // Success@k: true if at least one relevant doc is in top k.
            bool s1 = relevanceFlags.Count >= 1 && relevanceFlags[0];
            bool s5 = relevanceFlags.Take(5).Any(f => f);
            bool s10 = relevanceFlags.Take(10).Any(f => f);

            int tokenCount = docs.Sum(d => EstimateTokens(d.Content ?? string.Empty));

            queryResults.Add(new QueryResult
            {
                Id = entry.Id,
                Query = entry.Query,
                RelevantRetrieved = relevantRetrieved,
                TotalRelevant = entry.RelevantTitles.Count,
                TotalRetrieved = docs.Count,
                ReciprocalRank = rr,
                Ndcg = ndcg,
                SuccessAt1 = s1,
                SuccessAt5 = s5,
                SuccessAt10 = s10,
                TokenCount = tokenCount,
            });
        }

        var report = new ContextQualityReport
        {
            RunDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            CorpusPath = corpusPath,
            DatasetSize = dataset.Count,
            K = k,
            PrecisionAtK = queryResults.Average(r =>
                r.TotalRetrieved == 0 ? 0 : (double)r.RelevantRetrieved / r.TotalRetrieved),
            RecallAtK = queryResults.Average(r =>
                r.TotalRelevant == 0 ? 0 : (double)r.RelevantRetrieved / r.TotalRelevant),
            MeanReciprocalRank = queryResults.Average(r => r.ReciprocalRank),
            NdcgAt10 = queryResults.Average(r => r.Ndcg),
            SuccessAt1 = queryResults.Average(r => r.SuccessAt1 ? 1.0 : 0.0),
            SuccessAt5 = queryResults.Average(r => r.SuccessAt5 ? 1.0 : 0.0),
            SuccessAt10 = queryResults.Average(r => r.SuccessAt10 ? 1.0 : 0.0),
            AvgTokenCount = (int)queryResults.Average(r => r.TokenCount),
            PerQuery = queryResults,
        };

        if (outputPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var outJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, outJson, ct);
        }

        return report;
    }

    private static int EstimateTokens(string text) =>
        // Rough estimate: 1 token ≈ 4 characters (consistent with OpenAI/Anthropic rules of thumb).
        text.Length / 4;
}

internal sealed record EvalEntry(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("query")] string Query,
    [property: System.Text.Json.Serialization.JsonPropertyName("relevant_titles")] List<string> RelevantTitles);
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

- [ ] **Step 5: Commit**

```powershell
git add tests/Ferret.Benchmarks/Quality/
git commit -m "feat(benchmarks): ContextQualityRunner + 20-query eval dataset"
```

---

### Task 6: Context Effectiveness Runner — Token Efficiency + Compression Ratio

**Files:**
- Create: `tests/Ferret.Benchmarks/ContextEffectiveness/ContextEffectivenessReport.cs`
- Create: `tests/Ferret.Benchmarks/ContextEffectiveness/Prompts/benchmark-prompts.json`
- Create: `tests/Ferret.Benchmarks/ContextEffectiveness/ContextEffectivenessRunner.cs`

**Interfaces:**
- Consumes: `BenchmarkSetupBase.BuildSearchService`, `ContextAssembler` from Tasks 1 + 5
- Produces: `ContextEffectivenessReport` with `TokenReductionPercent`, `ContextCompressionRatio`, `DocumentsSurfaced`, `BaselineTokenEstimate`, `FerretTokenEstimate` fields

- [ ] **Step 1: Write ContextEffectivenessReport.cs**

```csharp
using System.Text.Json.Serialization;

namespace Ferret.Benchmarks.ContextEffectiveness;

public sealed class ContextEffectivenessReport
{
    [JsonPropertyName("run_date")]
    public string RunDate { get; init; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [JsonPropertyName("corpus_path")]
    public string CorpusPath { get; init; } = string.Empty;

    [JsonPropertyName("total_files_in_corpus")]
    public int TotalFilesInCorpus { get; init; }

    [JsonPropertyName("avg_baseline_tokens")]
    public int AvgBaselineTokens { get; init; }

    [JsonPropertyName("avg_ferret_tokens")]
    public int AvgFerretTokens { get; init; }

    [JsonPropertyName("token_reduction_percent")]
    public double TokenReductionPercent { get; init; }

    /// <summary>
    /// Fraction of corpus tokens consumed by Ferret context: ferret_tokens / corpus_tokens.
    /// E.g. 0.0028 means Ferret uses 0.28% of the full corpus (99.72% compression).
    /// </summary>
    [JsonPropertyName("context_compression_ratio")]
    public double ContextCompressionRatio { get; init; }

    [JsonPropertyName("avg_documents_surfaced")]
    public double AvgDocumentsSurfaced { get; init; }

    [JsonPropertyName("per_prompt")]
    public List<PromptResult> PerPrompt { get; init; } = [];
}

public sealed class PromptResult
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("baseline_tokens")]
    public int BaselineTokens { get; init; }

    [JsonPropertyName("ferret_tokens")]
    public int FerretTokens { get; init; }

    [JsonPropertyName("documents_surfaced")]
    public int DocumentsSurfaced { get; init; }

    [JsonPropertyName("token_reduction_percent")]
    public double TokenReductionPercent { get; init; }

    /// <summary>ferret_tokens / baseline_tokens — e.g. 0.003 = 99.7% compression.</summary>
    [JsonPropertyName("context_compression_ratio")]
    public double ContextCompressionRatio { get; init; }
}
```

- [ ] **Step 2: Write ContextEffectiveness/Prompts/benchmark-prompts.json**

```json
[
  { "id": "p01", "prompt": "How do I add a new connector to Ferret?" },
  { "id": "p02", "prompt": "Explain the full index pipeline from connector to SQLite." },
  { "id": "p03", "prompt": "How does BM25 search work in Ferret?" },
  { "id": "p04", "prompt": "How is context assembled from search results?" },
  { "id": "p05", "prompt": "How do I configure the MCP server?" },
  { "id": "p06", "prompt": "What is the plugin loading mechanism?" },
  { "id": "p07", "prompt": "How does incremental indexing detect file changes?" },
  { "id": "p08", "prompt": "How are duplicate search results handled?" },
  { "id": "p09", "prompt": "What is the token budget enforcement in context assembly?" },
  { "id": "p10", "prompt": "How do I write a custom parser?" }
]
```

- [ ] **Step 3: Write ContextEffectivenessRunner.cs**

```csharp
using System.Text.Json;
using Ferret.AI.Context;
using Ferret.Core.Context;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Benchmarks.ContextEffectiveness;

/// <summary>
/// Measures context effectiveness: compares estimated tokens needed with vs without Ferret context.
/// Baseline = entire corpus character count / 4 (all files as context).
/// Ferret   = context package size for the same query / 4.
/// No live LLM calls are made; this is a static token estimation.
/// Context Compression Ratio = ferret_tokens / corpus_tokens.
/// </summary>
public sealed class ContextEffectivenessRunner
{
    public static async Task<ContextEffectivenessReport> RunAsync(
        string corpusPath,
        string promptsPath,
        int maxDocs = 10,
        string? outputPath = null,
        CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(promptsPath, ct);
        var prompts = JsonSerializer.Deserialize<List<BenchmarkPrompt>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize benchmark-prompts.json");

        // Baseline: total character count of all indexed files.
        var allFiles = Directory.GetFiles(corpusPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".ferret", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        long totalChars = 0;
        foreach (var file in allFiles)
        {
            try { totalChars += new FileInfo(file).Length; }
            catch { /* skip locked files */ }
        }
        int baselineTokens = (int)(totalChars / 4);

        var searchService = BenchmarkSetupBase.BuildSearchService(corpusPath);
        var documentService = BenchmarkSetupBase.BuildDocumentService(corpusPath);
        var expander = new DocumentExpander(documentService, NullLogger<DocumentExpander>.Instance);
        var assembler = new ContextAssembler(searchService, expander, NullLogger<ContextAssembler>.Instance);

        var promptResults = new List<PromptResult>();

        foreach (var prompt in prompts)
        {
            var request = new ContextRequest
            {
                Query = prompt.Prompt,
                MaxDocuments = maxDocs,
                MaxTokens = 16000,
            };
            var package = await assembler.AssembleAsync(request, ct);
            int ferretChars = package.Documents.Sum(d => (d.Content ?? string.Empty).Length);
            int ferretTokens = ferretChars / 4;
            double reduction = baselineTokens == 0 ? 0
                : Math.Round((1.0 - (double)ferretTokens / baselineTokens) * 100, 1);

            double compressionRatio = baselineTokens == 0 ? 0
                : Math.Round((double)ferretTokens / baselineTokens, 6);

            promptResults.Add(new PromptResult
            {
                Id = prompt.Id,
                Prompt = prompt.Prompt,
                BaselineTokens = baselineTokens,
                FerretTokens = ferretTokens,
                DocumentsSurfaced = package.Documents.Count,
                TokenReductionPercent = reduction,
                ContextCompressionRatio = compressionRatio,
            });
        }

        var report = new ContextEffectivenessReport
        {
            RunDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            CorpusPath = corpusPath,
            TotalFilesInCorpus = allFiles.Length,
            AvgBaselineTokens = baselineTokens,
            AvgFerretTokens = (int)promptResults.Average(r => r.FerretTokens),
            TokenReductionPercent = Math.Round(promptResults.Average(r => r.TokenReductionPercent), 1),
            ContextCompressionRatio = Math.Round(promptResults.Average(r => r.ContextCompressionRatio), 6),
            AvgDocumentsSurfaced = Math.Round(promptResults.Average(r => r.DocumentsSurfaced), 1),
            PerPrompt = promptResults,
        };

        if (outputPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var outJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, outJson, ct);
        }

        return report;
    }
}

internal sealed record BenchmarkPrompt(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("prompt")] string Prompt);
```
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

- [ ] **Step 5: Commit**

```powershell
git add tests/Ferret.Benchmarks/ContextEffectiveness/
git commit -m "feat(benchmarks): ContextEffectivenessRunner — token reduction + compression ratio (no live LLM calls)"
```

---

### Task 7: BenchmarkReporter + Historical Directory + BENCHMARK-001-RC1.md Template

**Files:**
- Create: `tests/Ferret.Benchmarks/Reports/BenchmarkReporter.cs`
- Create: `docs/benchmarks/RC1/BENCHMARK-001-RC1.md`
- Create: `docs/benchmarks/history.md`
- Create: `benchmarks/run-benchmarks.ps1`

**Interfaces:**
- Consumes: `ContextQualityReport` (Task 5), `ContextEffectivenessReport` (Task 6), BenchmarkDotNet JSON artifacts
- Produces: populated `docs/benchmarks/RC1/BENCHMARK-001-RC1-YYYY-MM-DD.md`; updates `docs/benchmarks/history.md` trend table

- [ ] **Step 1: Write BenchmarkReporter.cs**

```csharp
using System.Text;
using System.Text.Json;
using Ferret.Benchmarks.ContextEffectiveness;
using Ferret.Benchmarks.Quality;

namespace Ferret.Benchmarks.Reports;

/// <summary>
/// Reads benchmark outputs (BenchmarkDotNet JSON + quality/context-effectiveness JSON reports)
/// and writes a Markdown benchmark report in the BENCHMARK-001-RC1 format.
/// Output goes to docs/benchmarks/RC1/ to support historical comparisons across releases.
/// </summary>
public sealed class BenchmarkReporter
{
    public static async Task WriteReportAsync(
        string outputPath,
        ContextQualityReport qualityReport,
        ContextEffectivenessReport effectivenessReport,
        string? benchmarkDotNetJsonPath = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# BENCHMARK-001-RC1 — {qualityReport.RunDate}");
        sb.AppendLine();
        sb.AppendLine("## Objective");
        sb.AppendLine("Measure Ferret RC1 platform performance, context quality, and AI token efficiency.");
        sb.AppendLine();
        sb.AppendLine("## Environment");
        sb.AppendLine($"- **OS:** {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        sb.AppendLine($"- **Runtime:** {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"- **Corpus:** {aiReport.TotalFilesInCorpus} files");
        sb.AppendLine($"- **Eval dataset:** {qualityReport.DatasetSize} Q&A pairs");
        sb.AppendLine();

        if (benchmarkDotNetJsonPath is not null && File.Exists(benchmarkDotNetJsonPath))
        {
            sb.AppendLine("## Platform Benchmarks (BenchmarkDotNet)");
            sb.AppendLine();
            sb.AppendLine("| Benchmark | Mean | StdDev |");
            sb.AppendLine("| --------- | ---- | ------ |");
            var bdnJson = await File.ReadAllTextAsync(benchmarkDotNetJsonPath, ct);
            var bdn = JsonDocument.Parse(bdnJson);
            if (bdn.RootElement.TryGetProperty("Benchmarks", out var benchmarks))
            {
                foreach (var bench in benchmarks.EnumerateArray())
                {
                    var name = bench.GetProperty("FullName").GetString() ?? "unknown";
                    var stats = bench.GetProperty("Statistics");
                    var mean = stats.GetProperty("Mean").GetDouble() / 1_000_000; // ns → ms
                    var stdDev = stats.GetProperty("StandardDeviation").GetDouble() / 1_000_000;
                    sb.AppendLine($"| {name} | {mean:F2} ms | {stdDev:F2} ms |");
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Context Quality");
        sb.AppendLine();
        sb.AppendLine($"- **Precision@{qualityReport.K}:** {qualityReport.PrecisionAtK:P1}");
        sb.AppendLine($"- **Recall@{qualityReport.K}:** {qualityReport.RecallAtK:P1}");
        sb.AppendLine($"- **MRR:** {qualityReport.MeanReciprocalRank:F3}");
        sb.AppendLine($"- **nDCG@10:** {qualityReport.NdcgAt10:F3}");
        sb.AppendLine($"- **Success@1:** {qualityReport.SuccessAt1:P1}");
        sb.AppendLine($"- **Success@5:** {qualityReport.SuccessAt5:P1}");
        sb.AppendLine($"- **Success@10:** {qualityReport.SuccessAt10:P1}");
        sb.AppendLine($"- **Avg token count per context package:** {qualityReport.AvgTokenCount:N0}");
        sb.AppendLine();

        sb.AppendLine("## Context Effectiveness");
        sb.AppendLine();
        sb.AppendLine($"- **Corpus size (baseline):** ~{effectivenessReport.AvgBaselineTokens:N0} tokens");
        sb.AppendLine($"- **Ferret context (avg):** ~{effectivenessReport.AvgFerretTokens:N0} tokens");
        sb.AppendLine($"- **Token reduction:** {effectivenessReport.TokenReductionPercent:F1}%");
        sb.AppendLine($"- **Context compression ratio:** {effectivenessReport.ContextCompressionRatio:P2} of corpus");
        sb.AppendLine($"- **Avg documents surfaced:** {effectivenessReport.AvgDocumentsSurfaced:F1}");
        sb.AppendLine();

        sb.AppendLine("## Observations");
        sb.AppendLine();
        sb.AppendLine("_Fill in after reviewing the raw numbers._");
        sb.AppendLine();
        sb.AppendLine("## Future Optimization Opportunities");
        sb.AppendLine();
        sb.AppendLine("_Fill in based on observed bottlenecks._");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, sb.ToString(), ct);
    }
}
```

- [ ] **Step 2: Create docs/benchmarks/RC1/BENCHMARK-001-RC1.md (empty template)**

Create the directory `docs/benchmarks/RC1/` and write:

```markdown
# BENCHMARK-001-RC1

> **Status:** Pending first run.
> Run `benchmarks/run-benchmarks.ps1` to populate this file.

## Objective

Measure Ferret RC1 platform performance, context quality, and context effectiveness
against a realistic 1,000-file corpus indexed via the filesystem connector.

## Environment

| Property        | Value |
| --------------- | ----- |
| OS              |       |
| Runtime         |       |
| CPU             |       |
| RAM             |       |
| Corpus size     |       |
| Eval dataset    |       |

## Methodology

1. Generate 1,000 synthetic Markdown files via `TestCorpusGenerator`
2. Full index via `IndexPipeline.RunAsync(ForceRebuild=true)`
3. Platform benchmarks: BenchmarkDotNet Release mode, warmup=3, iteration=5
4. Stage benchmarks: each `ContextAssembler` stage timed individually
5. Quality eval: 20 Q&A pairs, Precision@10, Recall@10, MRR, nDCG@10, Success@1/5/10
6. Context Effectiveness: 10 prompts, static token estimation; compression ratio = ferret_tokens / corpus_tokens

## Results

_Populated by `BenchmarkReporter.WriteReportAsync` on each run._

## Observations

## Future Optimization Opportunities

---

## Reserved: Federation Benchmarks (V2+)

_Not applicable for RC1. Reserved for distributed Knowledge Space work._

## Reserved: Host Startup Benchmarks (future)

_Deferred until CLI, MCP, and REST hosts stabilize._
```

- [ ] **Step 3: Create docs/benchmarks/history.md (trend table)**

```markdown
# Ferret Benchmark History

Methodology pinned per release. Real-corpus runs use pinned commit hashes.

| Version | Index 1K (s) | Search P50 (ms) | TTFUC P50 (ms) | Compression Ratio | nDCG@10 | Success@10 |
| ------- | ------------ | --------------- | -------------- | ----------------- | ------- | ---------- |
| RC1     |              |                 |                |                   |         |            |
| RC2     |              |                 |                |                   |         |            |
| V1      |              |                 |                |                   |         |            |

_Fill each column after running `benchmarks/run-benchmarks.ps1` for that release._
```

- [ ] **Step 4: Write benchmarks/run-benchmarks.ps1**

```powershell
#!/usr/bin/env pwsh
# run-benchmarks.ps1 — runs all Ferret benchmark phases and writes BENCHMARK-001-RC1 report
# Usage: ./benchmarks/run-benchmarks.ps1 [-CorpusPath <path>] [-SkipBdn]

param(
    [string]$CorpusPath = "",
    [switch]$SkipBdn
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path $PSScriptRoot -Parent
$ReportDir = Join-Path $RepoRoot "docs\benchmarks\reports"
$Date = Get-Date -Format "yyyy-MM-dd"
$ReportPath = Join-Path $ReportDir "RC1" "BENCHMARK-001-RC1-$Date.md"

New-Item -ItemType Directory -Force -Path (Join-Path $ReportDir "RC1") | Out-Null

if (-not $CorpusPath) {
    $CorpusPath = Join-Path ([System.IO.Path]::GetTempPath()) "ferret-bench-rc1"
}

Write-Host "=== Ferret RC1 Benchmark Suite ===" -ForegroundColor Cyan
Write-Host "Corpus path : $CorpusPath"
Write-Host "Report path : $ReportPath"
Write-Host ""

# Phase 1: BenchmarkDotNet (Release)
if (-not $SkipBdn) {
    Write-Host "Phase 1: Platform Benchmarks (BenchmarkDotNet)..." -ForegroundColor Yellow
    $BdnArtifacts = Join-Path $RepoRoot "BenchmarkDotNet.Artifacts"
    dotnet run --project "$RepoRoot\tests\Ferret.Benchmarks" -c Release -- `
        --filter "*Platform*" `
        --exporters json `
        --artifacts "$BdnArtifacts"
    if ($LASTEXITCODE -ne 0) { Write-Error "BenchmarkDotNet failed"; exit 1 }
    Write-Host "Phase 1 complete." -ForegroundColor Green
} else {
    Write-Host "Phase 1: Skipped (--SkipBdn)" -ForegroundColor DarkGray
}

# Phase 2: Context Quality + Effectiveness runners
Write-Host ""
Write-Host "Phase 2/3: Quality + Context Effectiveness runners — run via:" -ForegroundColor Yellow
Write-Host "  dotnet run --project tests/Ferret.Benchmarks -- --quality --corpus $CorpusPath" -ForegroundColor Gray
Write-Host "  dotnet run --project tests/Ferret.Benchmarks -- --effectiveness --corpus $CorpusPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Real-corpus (opt-in):" -ForegroundColor Yellow
Write-Host "  dotnet run --project tests/Ferret.Benchmarks -- --quality --corpus . --real-corpus" -ForegroundColor Gray
Write-Host ""
Write-Host "Benchmark suite complete. See: $ReportDir" -ForegroundColor Cyan
```

- [ ] **Step 5: Build final verification**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

- [ ] **Step 6: Commit**

```powershell
git add tests/Ferret.Benchmarks/Reports/ docs/benchmarks/ benchmarks/run-benchmarks.ps1
git commit -m "feat(benchmarks): BenchmarkReporter + historical docs/benchmarks/RC1/ + history.md"
```

---

### Task 8: Benchmark CLI Driver — Wire Quality + Effectiveness Runners to a Runnable Entry Point

**Files:**
- Modify: `tests/Ferret.Benchmarks/Program.cs`

**Interfaces:**
- Consumes: `ContextQualityRunner.RunAsync`, `ContextEffectivenessRunner.RunAsync`, `BenchmarkReporter.WriteReportAsync`
- Produces: `Program.cs` that dispatches either BenchmarkDotNet (when args contain `--filter`) or the custom runners (when args contain `--quality` or `--effectiveness`)

- [ ] **Step 1: Rewrite Program.cs to support quality and effectiveness runner commands**

```csharp
using BenchmarkDotNet.Running;
using Ferret.Benchmarks.ContextEffectiveness;
using Ferret.Benchmarks.Quality;
using Ferret.Benchmarks.Reports;

// Dispatch: custom runners when a known flag is present, BenchmarkDotNet otherwise.
if (args.Contains("--quality"))
{
    var corpusPath = GetArg(args, "--corpus") ?? throw new ArgumentException("--corpus <path> required");
    var datasetPath = GetArg(args, "--dataset")
        ?? Path.Combine(AppContext.BaseDirectory, "Quality", "EvalDataset", "eval-dataset.json");
    var outputPath = GetArg(args, "--output")
        ?? Path.Combine("docs", "benchmarks", "RC1", $"quality-{DateTime.UtcNow:yyyy-MM-dd}.json");

    Console.WriteLine($"Running context quality eval against: {corpusPath}");
    var report = await ContextQualityRunner.RunAsync(corpusPath, datasetPath, k: 10, outputPath);
    Console.WriteLine($"Precision@10:  {report.PrecisionAtK:P1}");
    Console.WriteLine($"Recall@10:     {report.RecallAtK:P1}");
    Console.WriteLine($"MRR:           {report.MeanReciprocalRank:F3}");
    Console.WriteLine($"nDCG@10:       {report.NdcgAt10:F3}");
    Console.WriteLine($"Success@1/5/10: {report.SuccessAt1:P0} / {report.SuccessAt5:P0} / {report.SuccessAt10:P0}");
    Console.WriteLine($"Report:        {outputPath}");
}
else if (args.Contains("--effectiveness"))
{
    var corpusPath = GetArg(args, "--corpus") ?? throw new ArgumentException("--corpus <path> required");
    var promptsPath = GetArg(args, "--prompts")
        ?? Path.Combine(AppContext.BaseDirectory, "ContextEffectiveness", "Prompts", "benchmark-prompts.json");
    var outputPath = GetArg(args, "--output")
        ?? Path.Combine("docs", "benchmarks", "RC1", $"context-effectiveness-{DateTime.UtcNow:yyyy-MM-dd}.json");

    Console.WriteLine($"Running context effectiveness benchmark against: {corpusPath}");
    var report = await ContextEffectivenessRunner.RunAsync(corpusPath, promptsPath, maxDocs: 10, outputPath);
    Console.WriteLine($"Token reduction:       {report.TokenReductionPercent:F1}%");
    Console.WriteLine($"Compression ratio:     {report.ContextCompressionRatio:P2} of corpus");
    Console.WriteLine($"Avg docs surfaced:     {report.AvgDocumentsSurfaced:F1}");
    Console.WriteLine($"Report:                {outputPath}");
}
else
{
    // Default: BenchmarkDotNet
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

static string? GetArg(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}
```

- [ ] **Step 2: Copy eval-dataset.json and benchmark-prompts.json to output directory**

Add to `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj`:

```xml
  <ItemGroup>
    <None Update="Quality\EvalDataset\eval-dataset.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="ContextEffectiveness\Prompts\benchmark-prompts.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 3: Final build + smoke test**

Run: `dotnet build tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj -c Release`
Expected: Build succeeded, 0 error(s)

Run: `dotnet run --project tests/Ferret.Benchmarks -c Debug -- --help`
Expected: BenchmarkDotNet help output (default path still works)

- [ ] **Step 4: Commit**

```powershell
git add tests/Ferret.Benchmarks/Program.cs tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj
git commit -m "feat(benchmarks): CLI driver — --quality and --effectiveness runner dispatch in Program.cs"
```

---

---

### Task 9: Real-Corpus Benchmarks (Opt-In)

**Files:**
- Create: `tests/Ferret.Benchmarks/Scale/RealCorpusBenchmarks.cs`
- Create: `benchmarks/corpora/ferret/.gitkeep` (placeholder — Ferret's own repo used at runtime)

**Interfaces:**
- Consumes: `BenchmarkSetupBase.BuildIndexPipeline`, `BenchmarkSetupBase.BuildSearchService`
- Produces: index + TTFUC measurements for Ferret-self and any additional pinned-commit clones

- [ ] **Step 1: Write RealCorpusBenchmarks.cs**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Ferret.Core.Workspace;
using Ferret.Indexing;

namespace Ferret.Benchmarks.Scale;

/// <summary>
/// Benchmarks against real repositories instead of synthetic corpora.
/// Ferret-self is the default; additional repos can be cloned to benchmarks/corpora/.
/// Run with: dotnet run -c Release -- --filter *RealCorpusBenchmarks*
/// Not run in CI by default — use --real-corpus flag in run-benchmarks.ps1.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class RealCorpusBenchmarks
{
    // Resolved at runtime: the directory containing this repo's src/ folder.
    private static string FerretSelfPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    private string _corpusPath = null!;
    private string _dbPath = null!;
    private IndexPipeline _pipeline = null!;

    [GlobalSetup]
    public void Setup()
    {
        _corpusPath = FerretSelfPath;
        _dbPath = Path.Combine(Path.GetTempPath(), $"ferret-real-bench-{Guid.NewGuid():N}", ".ferret", "bench.db");
        _pipeline = BenchmarkSetupBase.BuildIndexPipeline(_corpusPath, _dbPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        var dir = Path.GetDirectoryName(Path.GetDirectoryName(_dbPath));
        if (dir is not null && Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Benchmark(Description = "Full index — Ferret-self (force rebuild)")]
    public async Task FullIndex_FerretSelf()
    {
        await _pipeline.RunAsync(
            WorkspaceId.Create("real-ferret"),
            new IndexPipelineOptions { ForceRebuild = true },
            CancellationToken.None);
    }
}
```

- [ ] **Step 2: Create benchmarks/corpora/ferret/.gitkeep**

```
echo. > benchmarks/corpora/ferret/.gitkeep
```

Add to `benchmarks/corpora/README.md`:

```markdown
# Benchmark Corpora

Real-corpus benchmarks use repos cloned here (or the local repo itself for Ferret-self).
All real-corpus runs must pin a git commit hash for reproducibility.

| Corpus    | Source                              | Notes                     |
| --------- | ----------------------------------- | ------------------------- |
| ferret    | This repo (auto-detected)           | Default real-corpus       |
| aspnet    | dotnet/aspnetcore (pinned commit)   | Optional, large clone     |
| eshop     | dotnet-architecture/eShopOnContainers | Medium, recommended     |
```

- [ ] **Step 3: Build and smoke-run**

Run: `dotnet run --project tests/Ferret.Benchmarks -c Debug -- --filter *RealCorpusBenchmarks* --job dry`
Expected: Dry run completes (uses the current repo as corpus path)

- [ ] **Step 4: Commit**

```powershell
git add tests/Ferret.Benchmarks/Scale/RealCorpusBenchmarks.cs benchmarks/corpora/
git commit -m "feat(benchmarks): RealCorpusBenchmarks — Ferret-self as default real corpus (opt-in)"
```

---

### Task 10: Engineering Productivity Protocol Document

**Files:**
- Create: `docs/benchmarks/PRODUCTIVITY-EVAL-PROTOCOL.md`

**Note on automation boundary:** The automatable parts of Engineering Productivity (docs retrieved, TTFUC, retrieval precision) are already measured in Tasks 3 and 5. What this task adds is the documented **human evaluation protocol** for the parts that require a developer with a stopwatch — prompt count, task completion time, follow-up questions. LLM-in-the-loop automation is a future extension.

- [ ] **Step 1: Write PRODUCTIVITY-EVAL-PROTOCOL.md**

```markdown
# Engineering Productivity Evaluation Protocol

**Version:** RC1
**Status:** Active

## Purpose

Measure whether Ferret reduces the time and effort required for common engineering tasks.
The automatable proxies (TTFUC, Precision@10) are collected automatically by the benchmark suite.
This protocol covers the human-evaluation component.

## Tasks

Each evaluator completes these 5 tasks against the same repository (Ferret-self, indexed):

| ID  | Task                                    | Success Criterion                         |
| --- | --------------------------------------- | ----------------------------------------- |
| P01 | Find where indexing starts              | Name the entry-point file and method      |
| P02 | Add a parser for a new file type        | Describe the files to create/modify       |
| P03 | Explain the connector lifecycle         | List the stages in order                  |
| P04 | Locate the BM25 implementation          | Name the file                             |
| P05 | List all search extension points        | Name the interfaces                       |

## Conditions

Run each task in two conditions:

- **Baseline:** Claude alone (no Ferret context, paste the question directly)
- **With Ferret:** Use `ferret context "<query>"` to assemble context, then prompt Claude

## Per-Task Measurements

For each task in each condition, record:

| Metric               | Unit    | How to measure                              |
| -------------------- | ------- | ------------------------------------------- |
| Time to answer       | seconds | Stopwatch from question sent to answer read |
| Prompt count         | count   | Number of prompts to reach correct answer   |
| Follow-up questions  | count   | Clarifying prompts required after first     |
| Answer correct?      | yes/no  | Evaluator validates against source code     |
| Difficulty rating    | 1–5     | Evaluator self-reports post-task            |

## Data Collection Template

```csv
evaluator,task_id,condition,time_seconds,prompt_count,followup_count,correct,difficulty
```

## Analysis

Report per-task mean and median for each metric across evaluators.
Compute improvement ratio: `baseline_time / ferret_time` per task.

## Minimum Sample

≥ 3 evaluators × 5 tasks × 2 conditions = 30 data points minimum for RC1.
```

- [ ] **Step 2: Commit**

```powershell
git add docs/benchmarks/PRODUCTIVITY-EVAL-PROTOCOL.md
git commit -m "docs(benchmarks): Engineering Productivity Eval Protocol — human study template for RC1"
```

---

## Self-Review

### Spec coverage

| Spec requirement | Task |
| --- | --- |
| Platform: full index, incremental | Task 2 — `IndexBenchmarks` |
| Platform: search latency | Task 3 — `SearchBenchmarks` |
| Platform: TTFUC (context assembly) | Task 3 — `ContextAssemblyBenchmarks` |
| Context assembly stage breakdown | Task 3 — `ContextAssemblyStageBenchmarks` |
| Scale — 200/2000/10000 synthetic | Task 4 — `ScaleIndexBenchmarks` |
| Scale — Ferret-self real corpus | Task 9 — `RealCorpusBenchmarks` |
| Quality — Precision/Recall/MRR | Task 5 — `ContextQualityRunner` |
| Quality — nDCG@10, Success@1/5/10 | Task 5 — `ContextQualityReport` + runner |
| Quality — eval dataset | Task 5 — `eval-dataset.json` (20 Q&A pairs) |
| Context Effectiveness — token reduction | Task 6 — `ContextEffectivenessRunner` |
| Context Effectiveness — compression ratio | Task 6 — `ContextEffectivenessReport` |
| Report format + historical directory | Task 7 — `BenchmarkReporter` + `docs/benchmarks/RC1/` |
| Historical trend table | Task 7 — `docs/benchmarks/history.md` |
| Run script | Task 7 — `run-benchmarks.ps1` |
| CLI dispatch | Task 8 — `Program.cs` |
| Engineering Productivity protocol | Task 10 — `PRODUCTIVITY-EVAL-PROTOCOL.md` |
| MCP benchmark | **Gap** — requires running server. Deferred + Reserved in BENCHMARK-001-RC1.md |
| Startup benchmark | **Gap** — doesn't fit BenchmarkDotNet process model. Deferred + Reserved |
| Federation benchmarks | **Reserved** — V2+. Reserved section in BENCHMARK-001-RC1.md |
| External real corpora (ASP.NET, Roslyn) | **Out of scope** — opt-in, documented in benchmarks/corpora/README.md |

### Deferred

- **MCP benchmark**: live server required; deferred, reserved section in report template
- **Startup benchmark**: cold-start process model mismatch; deferred, reserved section in report template
- **Live LLM effectiveness**: gated behind `ANTHROPIC_API_KEY`; runner uses static token estimates
- **External large repos** (Roslyn, ASP.NET Runtime): multi-GB clones; documented as opt-in only
- **Productivity LLM automation**: human eval protocol provided; LLM-in-the-loop is a future extension

