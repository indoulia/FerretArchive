using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspace;
using Ferret.Core.Primitives;
using Ferret.Core.Results;
using Ferret.Core.Workspace;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Workspace;

// Fakes shared by both handler test classes

internal sealed class FakeOutput : IOutputFormatter
{
    private readonly List<string> _lines = [];
    internal IReadOnlyList<string> Lines => _lines;
    public void WriteLine(string text = "") => _lines.Add(text);
    public void WriteSuccess(string message) => _lines.Add($"✓ {message}");
    public void WriteError(string message) => _lines.Add($"✗ {message}");
    public void WriteVerbose(string message) => _lines.Add($"  {message}");
}

internal sealed class FakeServices : IFerretServices
{
    internal FakeServices(FakeOutput output) => Output = output;
    public IOutputFormatter Output { get; }
    public IConfiguration Configuration => new ConfigurationBuilder().Build();
    public ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;
    public IServiceProvider Services => new ServiceCollection().BuildServiceProvider();
    public Ferret.Core.Runtime.IRuntimeHost? Runtime => null;
}

internal sealed class FakeContext : IFerretContext
{
    internal FakeContext(IFerretServices services, string workingDirectory = @"C:\fake\cwd")
    {
        Services = services;
        WorkingDirectory = workingDirectory;
    }

    public CancellationToken CancellationToken => CancellationToken.None;
    public VerbosityLevel Verbosity => VerbosityLevel.Normal;
    public OutputFormat OutputFormat => OutputFormat.Text;
    public IFerretServices Services { get; }
    public string WorkingDirectory { get; }
    public T? GetOption<T>(string name) => default;
}

internal sealed class FakeWorkspaceEngine : IWorkspaceEngine
{
    // Properties first (SA1201)
    internal WorkspaceInitResult InitResult { get; set; } = WorkspaceInitResult.Success(MakeCtx("fake"));

    internal WorkspaceContext LoadResult { get; set; } = MakeCtx("fake");

    internal Exception? LoadException { get; set; }

    public Task<WorkspaceInitResult> InitialiseAsync(WorkspacePath r, WorkspaceOptions? o = null, CancellationToken ct = default)
        => Task.FromResult(InitResult);

    public Task<WorkspaceContext> LoadAsync(WorkspacePath r, WorkspaceOptions? o = null, CancellationToken ct = default)
    {
        if (LoadException is not null)
        {
            throw LoadException;
        }

        return Task.FromResult(LoadResult);
    }

    public Task<WorkspaceHealthReport> GetHealthAsync(WorkspaceContext c, HealthCheckDepth d = HealthCheckDepth.Quick, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<Changeset> GetChangesetAsync(WorkspaceContext c, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext c, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<ValidationResult> ValidateAsync(WorkspaceContext c, CancellationToken ct = default) => throw new NotImplementedException();

    private static WorkspaceContext MakeCtx(string name) =>
        WorkspaceContext.Create(
            WorkspacePath.Create(@"C:\fake"),
            WorkspaceId.Create("ws-fake"),
            WorkspaceMetadata.Create(name, string.Empty, "1.0", DateTimeOffset.UtcNow),
            WorkspaceCapabilities.Create(false, 0, 0));
}

internal sealed class FakeLocator : IWorkspaceLocator
{
    internal WorkspacePath? LocateResult { get; set; }
    public Task<WorkspacePath?> LocateAsync(WorkspacePath s, CancellationToken ct = default) => Task.FromResult(LocateResult);
    public Task<bool> ExistsAsync(WorkspacePath r, CancellationToken ct = default) => Task.FromResult(LocateResult is not null);
}

internal sealed class FakeInitFormatter : IWorkspaceInitFormatter
{
    internal WorkspaceInitView? LastView { get; private set; }
    public void Format(WorkspaceInitView view, IOutputFormatter output) => LastView = view;
}

internal sealed class FakeStatusFormatter : IWorkspaceStatusFormatter
{
    internal WorkspaceStatusView? LastView { get; private set; }
    public void Format(WorkspaceStatusView view, IOutputFormatter output) => LastView = view;
}

public sealed class WorkspaceInitCommandHandlerTests
{
    private static (FakeOutput Output, FakeContext Ctx) MakeCtx(string workingDir = @"C:\fake\cwd")
    {
        var o = new FakeOutput();
        return (o, new FakeContext(new FakeServices(o), workingDir));
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitSucceeds_ReturnsSuccess()
    {
        var (_, ctx) = MakeCtx();
        var result = await new WorkspaceInitCommandHandler(new FakeWorkspaceEngine(), new FakeInitFormatter())
            .ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitSucceeds_FormatterReceivesSucceededView()
    {
        var (_, ctx) = MakeCtx(@"C:\myproject");
        var formatter = new FakeInitFormatter();
        await new WorkspaceInitCommandHandler(new FakeWorkspaceEngine(), formatter).ExecuteAsync(ctx);
        Assert.True(formatter.LastView!.Succeeded);
        Assert.Equal(@"C:\myproject", formatter.LastView.RootPath);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitFails_ReturnsFailure()
    {
        var engine = new FakeWorkspaceEngine { InitResult = WorkspaceInitResult.Failure("already exists") };
        var (_, ctx) = MakeCtx();
        var result = await new WorkspaceInitCommandHandler(engine, new FakeInitFormatter()).ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitFails_FormatterReceivesFailureView()
    {
        var engine = new FakeWorkspaceEngine { InitResult = WorkspaceInitResult.Failure("already exists") };
        var (_, ctx) = MakeCtx();
        var formatter = new FakeInitFormatter();
        await new WorkspaceInitCommandHandler(engine, formatter).ExecuteAsync(ctx);
        Assert.False(formatter.LastView!.Succeeded);
        Assert.Contains("already exists", formatter.LastView.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_UsesWorkingDirectoryFromContext_NotCurrentDirectory()
    {
        var (_, ctx) = MakeCtx(@"C:\custom\dir");
        var formatter = new FakeInitFormatter();
        await new WorkspaceInitCommandHandler(new FakeWorkspaceEngine(), formatter).ExecuteAsync(ctx);
        Assert.Equal(@"C:\custom\dir", formatter.LastView!.RootPath);
    }
}

public sealed class WorkspaceStatusCommandHandlerTests
{
    private static (FakeOutput Output, FakeContext Ctx) MakeCtx(string workingDir = @"C:\fake\cwd")
    {
        var o = new FakeOutput();
        return (o, new FakeContext(new FakeServices(o), workingDir));
    }

    [Fact]
    public async Task ExecuteAsync_NotInWorkspace_ReturnsSuccess()
    {
        var locator = new FakeLocator { LocateResult = null };
        var (_, ctx) = MakeCtx();
        var result = await new WorkspaceStatusCommandHandler(locator, new FakeWorkspaceEngine(), new FakeStatusFormatter())
            .ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task ExecuteAsync_NotInWorkspace_FormatterReceivesNotInWorkspaceView()
    {
        var locator = new FakeLocator { LocateResult = null };
        var (_, ctx) = MakeCtx();
        var formatter = new FakeStatusFormatter();
        await new WorkspaceStatusCommandHandler(locator, new FakeWorkspaceEngine(), formatter).ExecuteAsync(ctx);
        Assert.False(formatter.LastView!.IsInWorkspace);
        Assert.Null(formatter.LastView.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_InWorkspace_ReturnsSuccess()
    {
        var locator = new FakeLocator { LocateResult = WorkspacePath.Create(@"C:\fake") };
        var (_, ctx) = MakeCtx();
        var result = await new WorkspaceStatusCommandHandler(locator, new FakeWorkspaceEngine(), new FakeStatusFormatter())
            .ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task ExecuteAsync_InWorkspace_FormatterReceivesWorkspaceName()
    {
        var locator = new FakeLocator { LocateResult = WorkspacePath.Create(@"C:\fake") };
        var (_, ctx) = MakeCtx();
        var formatter = new FakeStatusFormatter();
        await new WorkspaceStatusCommandHandler(locator, new FakeWorkspaceEngine(), formatter).ExecuteAsync(ctx);
        Assert.True(formatter.LastView!.IsInWorkspace);
        Assert.Equal("fake", formatter.LastView.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoadThrows_ReturnsFailure()
    {
        var locator = new FakeLocator { LocateResult = WorkspacePath.Create(@"C:\fake") };
        var engine = new FakeWorkspaceEngine { LoadException = new InvalidOperationException("corrupt JSON") };
        var (_, ctx) = MakeCtx();
        var result = await new WorkspaceStatusCommandHandler(locator, engine, new FakeStatusFormatter())
            .ExecuteAsync(ctx);
        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoadThrows_FormatterReceivesErrorView()
    {
        var locator = new FakeLocator { LocateResult = WorkspacePath.Create(@"C:\fake") };
        var engine = new FakeWorkspaceEngine { LoadException = new InvalidOperationException("corrupt JSON") };
        var (_, ctx) = MakeCtx();
        var formatter = new FakeStatusFormatter();
        await new WorkspaceStatusCommandHandler(locator, engine, formatter).ExecuteAsync(ctx);
        Assert.NotNull(formatter.LastView!.ErrorMessage);
        Assert.Contains("corrupt", formatter.LastView.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
