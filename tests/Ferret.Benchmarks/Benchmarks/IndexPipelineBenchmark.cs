using BenchmarkDotNet.Attributes;
using Ferret.Connectors.Filesystem;
using Ferret.Core.Connectors;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.ParserPlatform;
using Ferret.ParserPlatform.Parsers;
using Microsoft.Data.Sqlite;

namespace Ferret.Benchmarks.Benchmarks;

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
[MemoryDiagnoser]
public class IndexPipelineBenchmark : IDisposable
{
    private const int FileCount = 10_000;

    private static readonly WorkspaceId WorkspaceId = WorkspaceId.Create("bench-index");

    private IndexPipeline _pipeline = null!;
    private string _tempDir = string.Empty;
    private SqliteKeywordIndexEngine _indexEngine = null!;
    private IndexResult _lastResult = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases managed resources.</summary>
    /// <param name="disposing">True when called from Dispose().</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _indexEngine?.Dispose();
            SqliteConnection.ClearAllPools();
        }
    }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        // 1. Create temp directory with 10 000 fake .cs files, each exactly 200 chars
        _tempDir = Path.Combine(Path.GetTempPath(), $"ferret-bench-idx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var paddedContent = "public class C { public void M() { } } "
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

        // 5. Build SQLite index engine backed by a file-based SQLite database in the temp directory
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
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [IterationCleanup]
    public void AssertTarget()
    {
        // Enforce target: pipeline must complete within 60 s
        if (_lastResult is not null && _lastResult.Duration > TimeSpan.FromSeconds(60))
        {
            throw new InvalidOperationException(
                $"IndexPipelineBenchmark EXCEEDED target: {_lastResult.Duration.TotalSeconds:F1}s > 60s");
        }
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
internal sealed class SingleConnectorManager : IConnectorManager
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
