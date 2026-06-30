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
using Ferret.ParserPlatform.Parsers;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;

using Microsoft.Data.Sqlite;

namespace Ferret.Benchmarks.Benchmarks;

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
[MemoryDiagnoser]
public sealed class SearchBenchmark : IDisposable
{
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

    private SearchService _searchService = null!;
    private string _tempDir = string.Empty;
    private SqliteKeywordIndexEngine _indexEngine = null!;
    private TimeSpan _lastMeanDuration;

    /// <inheritdoc/>
    public void Dispose()
    {
        _indexEngine?.Dispose();
        SqliteConnection.ClearAllPools();
    }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        // 1. Write 1 000 fake .cs files to a temp directory
        _tempDir = Path.Combine(Path.GetTempPath(), $"ferret-bench-search-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        for (var i = 0; i < IndexedFileCount; i++)
        {
            var content = $"// file {i} public class Service{i} execute authentication token workspace"
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
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [IterationCleanup]
    public void AssertTarget()
    {
        // Enforce target: mean query duration must be under 200 ms
        if (_lastMeanDuration > TimeSpan.FromMilliseconds(200))
        {
            throw new InvalidOperationException(
                $"SearchBenchmark EXCEEDED target: mean {_lastMeanDuration.TotalMilliseconds:F1}ms > 200ms");
        }
    }

    [Benchmark]
    public async Task<TimeSpan> Run10Queries()
    {
        var totalDuration = TimeSpan.Zero;

        foreach (var q in Queries)
        {
            var result = await _searchService.SearchAsync(q, SearchOptions.Default);

            if (result.IsSuccess && result.ExecutionInfo is not null)
            {
                totalDuration += result.ExecutionInfo.Duration;
            }
        }

        _lastMeanDuration = totalDuration / Queries.Length;
        return _lastMeanDuration;
    }
}

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
