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
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var handler = new WatchCommandHandler(
                new FakeWatchPipeline(),
                new FakeWatchIndexEngine(),
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
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var handler = new WatchCommandHandler(
                new FakeWatchPipeline(),
                new FakeWatchIndexEngine(),
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
}
