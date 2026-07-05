using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Workspaces;

// Fakes scoped to this test file — a real FileWorkspaceRegistry against a temp directory is used
// throughout rather than a fake registry, per this WIP's "avoid mocking the persistence layer
// unless isolation genuinely requires it" — WIP-010/011 already validated that layer; these tests
// exercise the CLI's actual behavior against it, not a stand-in.

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
    private readonly Dictionary<string, string?> _options = [];

    internal FakeContext(IFerretServices services) => Services = services;

    public CancellationToken CancellationToken => CancellationToken.None;
    public VerbosityLevel Verbosity => VerbosityLevel.Normal;
    public OutputFormat OutputFormat => OutputFormat.Text;
    public IFerretServices Services { get; }
    public string WorkingDirectory => @"C:\fake\cwd";

    internal FakeContext With(string name, string? value)
    {
        _options[name] = value;
        return this;
    }

    public T? GetOption<T>(string name) =>
        _options.TryGetValue(name, out var value) && value is T typed ? typed : default;
}

public sealed class WorkspacesCommandHandlerTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeOutput _output;
    private readonly FakeContext _context;

    public WorkspacesCommandHandlerTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-workspaces-cli-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
        _output = new FakeOutput();
        _context = new FakeContext(new FakeServices(_output));
    }

    [Fact]
    public async Task Create_WithValidNameAndKind_Succeeds()
    {
        var handler = new WorkspacesCreateCommandHandler(_registry);
        _context.With("name", "customer-platform").With("kind", "team");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var all = await _registry.ListAsync();
        Assert.Contains(all, e => e.Name == "customer-platform" && e.Kind == "team");
    }

    [Fact]
    public async Task Create_WithMissingName_FailsWithActionableMessage()
    {
        var handler = new WorkspacesCreateCommandHandler(_registry);

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("name is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_WithInvalidKind_FailsAndListsValidValues()
    {
        var handler = new WorkspacesCreateCommandHandler(_registry);
        _context.With("name", "x").With("kind", "enterprise");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("personal", StringComparison.Ordinal) && l.Contains("team", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Create_WithDuplicateName_Fails()
    {
        var handler = new WorkspacesCreateCommandHandler(_registry);
        await _registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" });
        _context.With("name", "customer-platform");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("already exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task List_WhenEmpty_ShowsHelpfulMessage()
    {
        var handler = new WorkspacesListCommandHandler(_registry, new TextWorkspacesListFormatter());

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(_output.Lines, l => l.Contains("No workspaces yet", StringComparison.Ordinal));
    }

    [Fact]
    public async Task List_ShowsEveryWorkspace()
    {
        await _registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "workspace-a" });
        await _registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "workspace-b" });
        var handler = new WorkspacesListCommandHandler(_registry, new TextWorkspacesListFormatter());

        await handler.ExecuteAsync(_context);

        Assert.Contains(_output.Lines, l => l.Contains("workspace-a", StringComparison.Ordinal));
        Assert.Contains(_output.Lines, l => l.Contains("workspace-b", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Show_ByName_DisplaysDetail()
    {
        await _registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform", Kind = "team" });
        var handler = new WorkspacesShowCommandHandler(_registry, new TextWorkspacesShowFormatter());
        _context.With("workspace", "customer-platform");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(_output.Lines, l => l.Contains("customer-platform", StringComparison.Ordinal));
        Assert.Contains(_output.Lines, l => l.Contains("team", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Show_UnknownWorkspace_FailsWithActionableMessage()
    {
        var handler = new WorkspacesShowCommandHandler(_registry, new TextWorkspacesShowFormatter());
        _context.With("workspace", "does-not-exist");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase) && l.Contains("workspaces list", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddRepo_WithRealGitRepo_AddsCanonicalizedIdentity()
    {
        var workspace = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await _registry.SaveAsync(workspace);
        var repoPath = CreateFakeGitRepo("git@github.com:acme/service-a.git");
        var handler = new WorkspacesAddRepoCommandHandler(_registry);
        _context.With("workspace", "customer-platform").With("path", repoPath);

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(workspace.WorkspaceId);
        Assert.Contains(updated!.Members.Repos, r => r.Remote == "github.com/acme/service-a");
    }

    [Fact]
    public async Task AddRepo_ThenAddRepoAgainWithDifferentUrlFormat_IsRecognizedAsDuplicate()
    {
        // The WIP-012 acceptance criterion this exists to prove: git@... and https://... for the
        // same repo must resolve to the same identity, not two separate members.
        var workspace = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await _registry.SaveAsync(workspace);
        var repoPath = CreateFakeGitRepo("git@github.com:acme/service-a.git");
        var handler = new WorkspacesAddRepoCommandHandler(_registry);
        _context.With("workspace", "customer-platform").With("path", repoPath);
        await handler.ExecuteAsync(_context);

        // Same repo, remote rewritten to the https form (as if the user re-ran add-repo after
        // switching remotes) — resolved identity must match, so this is a duplicate, not a second member.
        await File.WriteAllTextAsync(Path.Join(repoPath, ".git", "config"), "[remote \"origin\"]\n    url = https://github.com/acme/service-a.git");
        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("already a member", StringComparison.OrdinalIgnoreCase));
        var updated = await _registry.ResolveAsync(workspace.WorkspaceId);
        Assert.Single(updated!.Members.Repos);
    }

    [Fact]
    public async Task AddRepo_ToUnknownWorkspace_FailsWithActionableMessage()
    {
        var repoPath = CreateFakeGitRepo("git@github.com:acme/service-a.git");
        var handler = new WorkspacesAddRepoCommandHandler(_registry);
        _context.With("workspace", "does-not-exist").With("path", repoPath);

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddRepo_WithNonexistentPath_FailsWithActionableMessage()
    {
        var workspace = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await _registry.SaveAsync(workspace);
        var handler = new WorkspacesAddRepoCommandHandler(_registry);
        _context.With("workspace", "customer-platform").With("path", Path.Join(_registryRoot, "no-such-path"));

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddRepo_WithPathThatIsNotAGitRepo_FailsWithActionableMessage()
    {
        var workspace = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await _registry.SaveAsync(workspace);
        var notARepo = Path.Join(_registryRoot, "not-a-repo");
        Directory.CreateDirectory(notARepo);
        var handler = new WorkspacesAddRepoCommandHandler(_registry);
        _context.With("workspace", "customer-platform").With("path", notARepo);

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not a git repository", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RemoveRepo_RemovesAPreviouslyAddedRepo()
    {
        var workspace = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await _registry.SaveAsync(workspace);
        var repoPath = CreateFakeGitRepo("git@github.com:acme/service-a.git");
        await new WorkspacesAddRepoCommandHandler(_registry).ExecuteAsync(_context.With("workspace", "customer-platform").With("path", repoPath));
        var handler = new WorkspacesRemoveRepoCommandHandler(_registry);

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(workspace.WorkspaceId);
        Assert.Empty(updated!.Members.Repos);
    }

    [Fact]
    public async Task RemoveRepo_ThatWasNeverAdded_FailsWithActionableMessage()
    {
        var workspace = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await _registry.SaveAsync(workspace);
        var repoPath = CreateFakeGitRepo("git@github.com:acme/service-a.git");
        var handler = new WorkspacesRemoveRepoCommandHandler(_registry);
        _context.With("workspace", "customer-platform").With("path", repoPath);

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("is not a member", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_registryRoot))
        {
            Directory.Delete(_registryRoot, recursive: true);
        }
    }

    private string CreateFakeGitRepo(string originUrl)
    {
        var repoPath = Path.Join(_registryRoot, $"repo-{Guid.NewGuid():N}");
        var gitDir = Path.Join(repoPath, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Join(gitDir, "config"), $"[remote \"origin\"]\n    url = {originUrl}");
        return repoPath;
    }
}
