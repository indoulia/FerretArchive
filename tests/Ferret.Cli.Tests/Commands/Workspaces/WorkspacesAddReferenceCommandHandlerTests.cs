using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Tests.Commands.Workspaces;

public sealed class WorkspacesAddReferenceCommandHandlerTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeOutput _output;
    private readonly FakeContext _context;

    public WorkspacesAddReferenceCommandHandlerTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-workspaces-add-reference-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
        _output = new FakeOutput();
        _context = new FakeContext(new FakeServices(_output));
    }

    [Fact]
    public async Task AddReference_BetweenTwoExistingWorkspaces_Succeeds()
    {
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        await _registry.SaveAsync(a);
        await _registry.SaveAsync(b);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(a.WorkspaceId);
        Assert.Single(updated!.References);
        Assert.Equal(b.WorkspaceId, updated.References[0].WorkspaceId);
        Assert.Equal("read-only", updated.References[0].Mode);
        Assert.Equal(FileWorkspaceRegistry.ReferencesSchemaVersion, updated.SchemaVersion);
    }

    [Fact]
    public async Task AddReference_ByWorkspaceId_Succeeds()
    {
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        await _registry.SaveAsync(a);
        await _registry.SaveAsync(b);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", a.WorkspaceId.ToString()).With("target", b.WorkspaceId.ToString());

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task AddReference_WhenSourceMissing_FailsWithActionableMessage()
    {
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        await _registry.SaveAsync(b);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "does-not-exist").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddReference_WhenTargetMissing_FailsWithActionableMessage()
    {
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        await _registry.SaveAsync(a);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "does-not-exist");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddReference_SelfReference_Fails()
    {
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        await _registry.SaveAsync(a);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "service-a");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("cannot reference itself", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddReference_Duplicate_Fails()
    {
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        await _registry.SaveAsync(a);
        await _registry.SaveAsync(b);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");
        Assert.Equal(CommandResult.Success, await handler.ExecuteAsync(_context));

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("already references", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddReference_ThatWouldCreateADirectCycle_IsRejected()
    {
        // B already imports A; adding A -> B must be rejected as a cycle, not resolved.
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "a" };
        var b = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "b",
            References = [new WorkspaceReference { WorkspaceId = a.WorkspaceId }],
        };
        await _registry.SaveAsync(a);
        await _registry.SaveAsync(b);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "a").With("target", "b");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        var unchangedA = await _registry.ResolveAsync(a.WorkspaceId);
        Assert.Empty(unchangedA!.References);
    }

    [Fact]
    public async Task AddReference_ThatWouldCreateATransitiveCycle_IsRejected()
    {
        // C already imports B, B already imports A; adding A -> C must be rejected.
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "a" };
        var b = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "b",
            References = [new WorkspaceReference { WorkspaceId = a.WorkspaceId }],
        };
        var c = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "c",
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await _registry.SaveAsync(a);
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(c);
        var handler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "a").With("target", "c");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_registryRoot))
        {
            Directory.Delete(_registryRoot, recursive: true);
        }
    }
}
