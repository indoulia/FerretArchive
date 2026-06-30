using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Indexing.Formatting;
using Ferret.Cli.Commands.Indexing.ViewModels;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Indexing;

// ---------------------------------------------------------------------------
// Inner fakes
// ---------------------------------------------------------------------------

internal sealed class FakeIndexOutput : IOutputFormatter
{
    private readonly List<string> _lines = [];
    internal IReadOnlyList<string> Lines => _lines;
    public void WriteLine(string text = "") => _lines.Add(text);
    public void WriteSuccess(string message) => _lines.Add($"✓ {message}");
    public void WriteError(string message) => _lines.Add($"✗ {message}");
    public void WriteVerbose(string message) => _lines.Add($"  {message}");
}

internal sealed class FakeIndexServices : IFerretServices
{
    internal FakeIndexServices(FakeIndexOutput output) => Output = output;
    public IOutputFormatter Output { get; }
    public IConfiguration Configuration => new ConfigurationBuilder().Build();
    public ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;
    public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();
    public Ferret.Core.Runtime.IRuntimeHost? Runtime => null;
}

internal sealed class FakeFerretContext : IFerretContext
{
    private readonly bool _rebuild;

    internal FakeFerretContext(IFerretServices services, bool rebuild = false)
    {
        Services = services;
        _rebuild = rebuild;
    }

    public CancellationToken CancellationToken => CancellationToken.None;
    public VerbosityLevel Verbosity => VerbosityLevel.Normal;
    public OutputFormat OutputFormat => OutputFormat.Text;
    public IFerretServices Services { get; }
    public string WorkingDirectory => @"C:\fake\cwd";

    public T? GetOption<T>(string name)
    {
        if (name == "rebuild" && typeof(T) == typeof(bool))
        {
            return (T)(object)_rebuild;
        }

        return default;
    }
}

internal sealed class FakeVerboseFerretContext : IFerretContext
{
    internal FakeVerboseFerretContext(IFerretServices services) => Services = services;
    public CancellationToken CancellationToken => CancellationToken.None;
    public VerbosityLevel Verbosity => VerbosityLevel.Verbose;
    public OutputFormat OutputFormat => OutputFormat.Text;
    public IFerretServices Services { get; }
    public string WorkingDirectory => @"C:\fake\cwd";

    public T? GetOption<T>(string name)
    {
        if (name == "verbose" && typeof(T) == typeof(bool))
        {
            return (T)(object)true;
        }

        if (name == "rebuild" && typeof(T) == typeof(bool))
        {
            return (T)(object)false;
        }

        return default;
    }
}

internal sealed class FakeIndexPipeline : IIndexPipeline
{
    internal IndexPipelineOptions? LastOptions { get; private set; }
    internal IndexResult Result { get; set; } = new IndexResult
    {
        AssetsDiscovered = 10,
        AssetsProcessed = 10,
        DocumentsIndexed = 8,
        DocumentsSkipped = 1,
        Failures = 0,
        Warnings = 0,
        Duration = TimeSpan.FromSeconds(1.23),
    };

    public Task<IndexResult> RunAsync(WorkspaceId workspaceId, IndexPipelineOptions options, CancellationToken ct = default)
    {
        LastOptions = options;
        return Task.FromResult(Result);
    }
}

internal sealed class FakeWorkspaceContext : IWorkspaceContext
{
    public WorkspaceId WorkspaceId { get; } = WorkspaceId.Create("test");
    public WorkspacePath WorkspaceRoot { get; } = WorkspacePath.Create(@"C:\fake\workspace");
}

// ---------------------------------------------------------------------------
// ViewModel mapping tests
// ---------------------------------------------------------------------------

public sealed class IndexSummaryViewModelTests
{
    private static IndexResult MakeResult(
        int discovered = 10,
        int processed = 10,
        int indexed = 8,
        int skipped = 1,
        int failures = 0,
        int warnings = 0,
        double durationSeconds = 1.23,
        IReadOnlyList<string>? failureMessages = null) =>
        new IndexResult
        {
            AssetsDiscovered = discovered,
            AssetsProcessed = processed,
            DocumentsIndexed = indexed,
            DocumentsSkipped = skipped,
            Failures = failures,
            Warnings = warnings,
            Duration = TimeSpan.FromSeconds(durationSeconds),
            FailureMessages = failureMessages ?? [],
        };

    [Fact]
    public void IndexSummaryViewModel_From_Maps_AssetsDiscovered()
    {
        var result = MakeResult(discovered: 42);
        var vm = IndexSummaryViewModel.From(result, "db.path");
        Assert.Equal(42, vm.AssetsDiscovered);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_DocumentsIndexed()
    {
        var result = MakeResult(indexed: 7);
        var vm = IndexSummaryViewModel.From(result, "db.path");
        Assert.Equal(7, vm.DocumentsIndexed);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_Failures()
    {
        var messages = new[] { "file1.txt: parse error", "file2.txt: timeout" };
        var result = MakeResult(failures: 2, failureMessages: messages);
        var vm = IndexSummaryViewModel.From(result, "db.path");
        Assert.Equal(2, vm.Failures);
        Assert.Equal(messages, vm.FailureMessages);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_DatabasePath()
    {
        var result = MakeResult();
        var vm = IndexSummaryViewModel.From(result, @"C:\ws\.ferret\indexes\keyword\keyword-index.db");
        Assert.Equal(@"C:\ws\.ferret\indexes\keyword\keyword-index.db", vm.DatabasePath);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_Duration()
    {
        var result = MakeResult(durationSeconds: 5.5);
        var vm = IndexSummaryViewModel.From(result, "db.path");
        Assert.Equal(TimeSpan.FromSeconds(5.5), vm.Duration);
    }
}

// ---------------------------------------------------------------------------
// Formatter tests
// ---------------------------------------------------------------------------

public sealed class TextIndexSummaryFormatterTests
{
    private static IndexSummaryViewModel MakeVm(
        int discovered = 10,
        int processed = 10,
        int indexed = 8,
        int skipped = 1,
        int failures = 0,
        double durationSeconds = 1.23,
        string dbPath = @"C:\db\keyword-index.db",
        IReadOnlyList<string>? failureMessages = null) =>
        new IndexSummaryViewModel
        {
            AssetsDiscovered = discovered,
            AssetsProcessed = processed,
            DocumentsIndexed = indexed,
            DocumentsSkipped = skipped,
            Failures = failures,
            Duration = TimeSpan.FromSeconds(durationSeconds),
            DatabasePath = dbPath,
            FailureMessages = failureMessages ?? [],
        };

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Discovered()
    {
        var output = TextIndexSummaryFormatter.Format(MakeVm(discovered: 15));
        Assert.Contains("Discovered", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("15", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Indexed()
    {
        var output = TextIndexSummaryFormatter.Format(MakeVm(indexed: 12));
        Assert.Contains("Indexed", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("12", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Skipped_And_Failed()
    {
        var output = TextIndexSummaryFormatter.Format(MakeVm(skipped: 3, failures: 2));
        Assert.Contains("Skipped", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3", output, StringComparison.Ordinal);
        Assert.Contains("Failed", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Duration()
    {
        var output = TextIndexSummaryFormatter.Format(MakeVm(durationSeconds: 2.45));
        Assert.Contains("Duration", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2.45s", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_DatabasePath()
    {
        var output = TextIndexSummaryFormatter.Format(MakeVm(dbPath: @"C:\ws\.ferret\indexes\keyword\keyword-index.db"));
        Assert.Contains(@"C:\ws\.ferret\indexes\keyword\keyword-index.db", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_FailureMessages_When_Failures_NonZero()
    {
        var messages = new[] { "file1.txt: error", "file2.txt: timeout" };
        var output = TextIndexSummaryFormatter.Format(MakeVm(failures: 2, failureMessages: messages));
        Assert.Contains("file1.txt: error", output, StringComparison.Ordinal);
        Assert.Contains("file2.txt: timeout", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Does_Not_Contain_Failures_Section_When_Zero()
    {
        var output = TextIndexSummaryFormatter.Format(MakeVm(failures: 0));
        Assert.DoesNotContain("Failures:", output, StringComparison.OrdinalIgnoreCase);
    }
}

// ---------------------------------------------------------------------------
// Handler tests
// ---------------------------------------------------------------------------

public sealed class IndexCommandHandlerTests
{
    private static (FakeIndexOutput Output, FakeFerretContext Ctx) MakeCtx(bool rebuild = false)
    {
        var o = new FakeIndexOutput();
        return (o, new FakeFerretContext(new FakeIndexServices(o), rebuild));
    }

    private static IndexCommandHandler MakeHandler(
        FakeIndexPipeline? pipeline = null,
        FakeWorkspaceContext? workspaceCtx = null,
        SwappableEventBus? bus = null)
    {
        return new IndexCommandHandler(
            pipeline ?? new FakeIndexPipeline(),
            workspaceCtx ?? new FakeWorkspaceContext(),
            bus ?? new SwappableEventBus(NullEventBus.Instance));
    }

    [Fact]
    public async Task Handler_Returns_Success_When_No_Failures()
    {
        var pipeline = new FakeIndexPipeline { Result = new IndexResult { AssetsDiscovered = 5, AssetsProcessed = 5, DocumentsIndexed = 5, DocumentsSkipped = 0, Failures = 0, Warnings = 0, Duration = TimeSpan.FromSeconds(1) } };
        var (_, ctx) = MakeCtx();
        var result = await MakeHandler(pipeline).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task Handler_Returns_Failure_When_Failures_NonZero()
    {
        var pipeline = new FakeIndexPipeline { Result = new IndexResult { AssetsDiscovered = 5, AssetsProcessed = 5, DocumentsIndexed = 3, DocumentsSkipped = 0, Failures = 2, Warnings = 0, Duration = TimeSpan.FromSeconds(1) } };
        var (_, ctx) = MakeCtx();
        var result = await MakeHandler(pipeline).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task Handler_Passes_ForceRebuild_True_When_Rebuild_Option_Set()
    {
        var pipeline = new FakeIndexPipeline();
        var (_, ctx) = MakeCtx(rebuild: true);
        await MakeHandler(pipeline).ExecuteAsync(ctx);
        Assert.True(pipeline.LastOptions!.ForceRebuild);
    }

    [Fact]
    public async Task Handler_Passes_ForceRebuild_False_When_Rebuild_Option_Not_Set()
    {
        var pipeline = new FakeIndexPipeline();
        var (_, ctx) = MakeCtx(rebuild: false);
        await MakeHandler(pipeline).ExecuteAsync(ctx);
        Assert.False(pipeline.LastOptions!.ForceRebuild);
    }

    [Fact]
    public async Task Handler_VerboseMode_Succeeds_Without_Throwing()
    {
        var output = new FakeIndexOutput();
        var ctx = new FakeVerboseFerretContext(new FakeIndexServices(output));
        var result = await MakeHandler().ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task Handler_VerboseMode_Restores_InnerBus_After_Run()
    {
        var output = new FakeIndexOutput();
        var ctx = new FakeVerboseFerretContext(new FakeIndexServices(output));
        var bus = new SwappableEventBus(NullEventBus.Instance);
        await MakeHandler(bus: bus).ExecuteAsync(ctx);
        Assert.IsType<NullEventBus>(bus.Inner);
    }
}
