using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Tests.Commands.Workspaces;

public sealed class WorkspacesUnpinReferenceCommandHandlerTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeOutput _output;
    private readonly FakeContext _context;

    public WorkspacesUnpinReferenceCommandHandlerTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-unpin-reference-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
        _output = new FakeOutput();
        _context = new FakeContext(new FakeServices(_output));
    }

    [Fact]
    public async Task UnpinReference_ForAPinnedReference_ClearsThePin_SoTheReferenceFloatsAgain()
    {
        var target = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" };
        await _registry.SaveAsync(target);
        var source = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            References = [new WorkspaceReference { WorkspaceId = target.WorkspaceId, PinnedStateHash = "pinned-fingerprint" }],
        };
        await _registry.SaveAsync(source);
        var handler = new WorkspacesUnpinReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(source.WorkspaceId);
        Assert.Null(Assert.Single(updated!.References).PinnedStateHash);
    }

    [Fact]
    public async Task UnpinReference_WhenNoSuchReferenceExists_Fails()
    {
        await _registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "shared-lib" });
        await _registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "service-a" });
        var handler = new WorkspacesUnpinReferenceCommandHandler(_registry);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("does not reference", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_registryRoot))
        {
            Directory.Delete(_registryRoot, recursive: true);
        }
    }
}
