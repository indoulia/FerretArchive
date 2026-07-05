using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Tests.Commands.Workspaces;

public sealed class WorkspacesRemoveReferenceCommandHandlerTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeOutput _output;
    private readonly FakeContext _context;

    public WorkspacesRemoveReferenceCommandHandlerTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-workspaces-remove-reference-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
        _output = new FakeOutput();
        _context = new FakeContext(new FakeServices(_output));
    }

    [Fact]
    public async Task RemoveReference_WhenReferenceExists_RemovesIt()
    {
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(a);
        var handler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(a.WorkspaceId);
        Assert.Empty(updated!.References);
    }

    [Fact]
    public async Task RemoveReference_ByWorkspaceId_RemovesIt()
    {
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(a);
        var handler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        _context.With("workspace", a.WorkspaceId.ToString()).With("target", b.WorkspaceId.ToString());

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task RemoveReference_LeavesOtherReferencesIntact()
    {
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        var c = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "other-lib" };
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }, new WorkspaceReference { WorkspaceId = c.WorkspaceId }],
        };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(c);
        await _registry.SaveAsync(a);
        var handler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(a.WorkspaceId);
        var remaining = Assert.Single(updated!.References);
        Assert.Equal(c.WorkspaceId, remaining.WorkspaceId);
    }

    [Fact]
    public async Task RemoveReference_WhenNoSuchReferenceExists_FailsWithActionableMessage()
    {
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(a);
        var handler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("does not reference", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RemoveReference_WhenSourceMissing_FailsWithActionableMessage()
    {
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        await _registry.SaveAsync(b);
        var handler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        _context.With("workspace", "does-not-exist").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RemoveReference_WhenTargetMissing_FailsWithActionableMessage()
    {
        var a = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" };
        await _registry.SaveAsync(a);
        var handler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "does-not-exist");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RemoveReference_SupportsTheMovedRepoRepairWorkflow()
    {
        // Dogfooding Sprint 1, Friction #4: a moved repo's stale reference should be removable and
        // re-addable without hand-editing the registry.
        var b = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(a);
        var removeHandler = new WorkspacesRemoveReferenceCommandHandler(_registry);
        var addHandler = new WorkspacesAddReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        Assert.Equal(CommandResult.Success, await removeHandler.ExecuteAsync(_context));
        Assert.Equal(CommandResult.Success, await addHandler.ExecuteAsync(_context));

        var updated = await _registry.ResolveAsync(a.WorkspaceId);
        Assert.Single(updated!.References);
    }

    public void Dispose()
    {
        if (Directory.Exists(_registryRoot))
        {
            Directory.Delete(_registryRoot, recursive: true);
        }
    }
}
