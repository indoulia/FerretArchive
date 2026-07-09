using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Watch;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Watch;

// ---------------------------------------------------------------------------
// Fakes
// ---------------------------------------------------------------------------

internal sealed class FakeWatchPipeline : IIndexPipeline
{
    internal int CallCount { get; private set; }

    internal List<Ferret.Core.Connectors.AssetId> SingleAssetCalls { get; } = [];

    public Task<IndexResult> RunAsync(WorkspaceId workspaceId, IndexPipelineOptions options, CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(new IndexResult
        {
            AssetsDiscovered = 1,
            AssetsProcessed = 1,
            DocumentsIndexed = 1,
            DocumentsSkipped = 0,
            Failures = 0,
            Warnings = 0,
            Duration = TimeSpan.Zero,
        });
    }

    public Task<IndexResult> RunSingleAssetAsync(WorkspaceId workspaceId, Ferret.Core.Connectors.AssetId assetId, CancellationToken ct = default)
    {
        lock (SingleAssetCalls)
        {
            SingleAssetCalls.Add(assetId);
        }

        return Task.FromResult(new IndexResult
        {
            AssetsDiscovered = 1,
            AssetsProcessed = 1,
            DocumentsIndexed = 1,
            DocumentsSkipped = 0,
            Failures = 0,
            Warnings = 0,
            Duration = TimeSpan.Zero,
        });
    }
}

internal sealed class FakeWatchStateStore : IIndexStateStore
{
    internal List<Ferret.Core.Connectors.AssetId> Removed { get; } = [];

    internal int SaveCount { get; private set; }

    public ValueTask<Ferret.Core.Connectors.AssetFingerprint?> GetFingerprintAsync(Ferret.Core.Connectors.AssetId assetId, CancellationToken ct = default) =>
        ValueTask.FromResult<Ferret.Core.Connectors.AssetFingerprint?>(null);

    public Task SetFingerprintAsync(Ferret.Core.Connectors.AssetId assetId, Ferret.Core.Connectors.AssetFingerprint fingerprint, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(Ferret.Core.Connectors.AssetId assetId, CancellationToken ct = default)
    {
        lock (Removed)
        {
            Removed.Add(assetId);
        }

        return Task.CompletedTask;
    }

    public ValueTask<IReadOnlySet<Ferret.Core.Connectors.AssetId>> GetAllKeysAsync(CancellationToken ct = default) =>
        ValueTask.FromResult<IReadOnlySet<Ferret.Core.Connectors.AssetId>>(new HashSet<Ferret.Core.Connectors.AssetId>());

    public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task SaveAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task SetIndexedGitHeadAsync(string? gitHeadSha, CancellationToken ct = default) => Task.CompletedTask;

    public ValueTask<string?> GetIndexedGitHeadAsync(CancellationToken ct = default) => ValueTask.FromResult<string?>(null);
}

internal sealed class FakeWatchIndexEngine : IIndexEngine
{
    internal List<DocumentId> Deleted { get; } = [];

    public Task WriteAsync(Ferret.Core.Documents.Document document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IndexStats> GetStatsAsync(CancellationToken ct = default)
        => Task.FromResult(new IndexStats
        {
            DocumentCount = 0,
            TotalChars = 0,
            LastIndexedAt = DateTimeOffset.UtcNow,
            IndexSizeBytes = 0,
        });

    public Task ClearAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAsync(DocumentId documentId, CancellationToken ct = default)
    {
        Deleted.Add(documentId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeWatchWorkspaceContext : IWorkspaceContext
{
    internal FakeWatchWorkspaceContext(string root)
        => WorkspaceRoot = WorkspacePath.Create(root);

    public WorkspaceId WorkspaceId { get; } = WorkspaceId.Create("test");

    public WorkspacePath WorkspaceRoot { get; }
}

internal sealed class FakeWatchOutput : IOutputFormatter
{
    public void WriteLine(string text = "")
    {
    }

    public void WriteSuccess(string message)
    {
    }

    public void WriteError(string message)
    {
    }

    public void WriteVerbose(string message)
    {
    }
}

internal sealed class FakeWatchServices : IFerretServices
{
    public IOutputFormatter Output { get; } = new FakeWatchOutput();

    public IConfiguration Configuration => new ConfigurationBuilder().Build();

    public ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;

    public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();

    public Ferret.Core.Runtime.IRuntimeHost? Runtime => null;
}

internal sealed class FakeWatchFerretContext : IFerretContext
{
    internal FakeWatchFerretContext(CancellationToken ct)
        => CancellationToken = ct;

    public CancellationToken CancellationToken { get; }

    public VerbosityLevel Verbosity => VerbosityLevel.Normal;

    public OutputFormat OutputFormat => OutputFormat.Text;

    public IFerretServices Services { get; } = new FakeWatchServices();

    public string WorkingDirectory => @"C:\fake\cwd";

    public T? GetOption<T>(string name) => default;
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class WatchCommandHandlerTests
{
    [Fact]
    public void WatchCommandHandler_CanBeInstantiated()
    {
        var tmpDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var handler = new WatchCommandHandler(
                new FakeWatchPipeline(),
                new FakeWatchIndexEngine(),
                new FakeWatchStateStore(),
                new FakeWatchWorkspaceContext(tmpDir),
                NullLogger<WatchCommandHandler>.Instance);
            Assert.NotNull(handler);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CancelsCleanly_ReturnsSuccess()
    {
        var tmpDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var handler = new WatchCommandHandler(
                new FakeWatchPipeline(),
                new FakeWatchIndexEngine(),
                new FakeWatchStateStore(),
                new FakeWatchWorkspaceContext(tmpDir),
                NullLogger<WatchCommandHandler>.Instance);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var result = await handler.ExecuteAsync(new FakeWatchFerretContext(cts.Token));
            Assert.Equal(CommandResult.Success, result);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_FileChanged_CallsRunSingleAssetAsync_NotFullRunAsync()
    {
        var tmpDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var pipeline = new FakeWatchPipeline();
            var handler = new WatchCommandHandler(
                pipeline,
                new FakeWatchIndexEngine(),
                new FakeWatchStateStore(),
                new FakeWatchWorkspaceContext(tmpDir),
                NullLogger<WatchCommandHandler>.Instance);

            using var cts = new CancellationTokenSource();
            var executeTask = handler.ExecuteAsync(new FakeWatchFerretContext(cts.Token));

            await Task.Delay(100); // let the FileSystemWatcher attach before writing
            var filePath = Path.Join(tmpDir, "changed.cs");
            await File.WriteAllTextAsync(filePath, "class Changed {}");

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (pipeline.SingleAssetCalls.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            await cts.CancelAsync();
            await executeTask;

            Assert.Equal(0, pipeline.CallCount);
            Assert.Contains(pipeline.SingleAssetCalls, id => id.Value.EndsWith("changed.cs", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_FileDeleted_RemovesFromEngineAndStateStore()
    {
        var tmpDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        var filePath = Path.Join(tmpDir, "todelete.cs");
        await File.WriteAllTextAsync(filePath, "class ToDelete {}");
        try
        {
            var engine = new FakeWatchIndexEngine();
            var stateStore = new FakeWatchStateStore();
            var handler = new WatchCommandHandler(
                new FakeWatchPipeline(),
                engine,
                stateStore,
                new FakeWatchWorkspaceContext(tmpDir),
                NullLogger<WatchCommandHandler>.Instance);

            using var cts = new CancellationTokenSource();
            var executeTask = handler.ExecuteAsync(new FakeWatchFerretContext(cts.Token));

            await Task.Delay(100); // let the FileSystemWatcher attach before deleting
            File.Delete(filePath);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (stateStore.Removed.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            await cts.CancelAsync();
            await executeTask;

            Assert.Single(engine.Deleted);
            Assert.Single(stateStore.Removed);
            Assert.EndsWith("todelete.cs", stateStore.Removed[0].Value, StringComparison.Ordinal);
            Assert.True(stateStore.SaveCount > 0, "a deletion-only batch must flush the state store, not leave the removal in memory only");
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
