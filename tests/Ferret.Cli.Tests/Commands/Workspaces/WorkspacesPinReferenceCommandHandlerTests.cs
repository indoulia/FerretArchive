using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Knowledge.Federation;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Tests.Commands.Workspaces;

public sealed class WorkspacesPinReferenceCommandHandlerTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeOutput _output;
    private readonly FakeContext _context;

    public WorkspacesPinReferenceCommandHandlerTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-pin-reference-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
        _output = new FakeOutput();
        _context = new FakeContext(new FakeServices(_output));
    }

    [Fact]
    public async Task PinReference_ForAnExistingReference_SetsPinnedStateHashToTheCurrentFingerprint()
    {
        var (source, target) = await SaveReferencingWorkspacesAsync();
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(target.WorkspaceId, "current-fingerprint");
        var handler = new WorkspacesPinReferenceCommandHandler(_registry, fingerprints);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        var updated = await _registry.ResolveAsync(source.WorkspaceId);
        var reference = Assert.Single(updated!.References);
        Assert.Equal("current-fingerprint", reference.PinnedStateHash);
    }

    [Fact]
    public async Task PinReference_WhenNoSuchReferenceExists_Fails()
    {
        var target = await SaveWorkspaceAsync("shared-lib");
        await SaveWorkspaceAsync("service-a");
        var handler = new WorkspacesPinReferenceCommandHandler(_registry, new FakeWorkspaceStateFingerprintProvider());
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("does not reference", StringComparison.OrdinalIgnoreCase));
        _ = target;
    }

    [Fact]
    public async Task PinReference_WhenTheTargetsFingerprintCannotBeComputed_FailsClosed()
    {
        var (source, target) = await SaveReferencingWorkspacesAsync();
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(target.WorkspaceId, fingerprint: null);
        var handler = new WorkspacesPinReferenceCommandHandler(_registry, fingerprints);
        _context.With("workspace", "service-a").With("target", "shared-lib");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        var unchanged = await _registry.ResolveAsync(source.WorkspaceId);
        Assert.Null(Assert.Single(unchanged!.References).PinnedStateHash);
    }

    public void Dispose()
    {
        if (Directory.Exists(_registryRoot))
        {
            Directory.Delete(_registryRoot, recursive: true);
        }
    }

    private async Task<WorkspaceRegistryEntry> SaveWorkspaceAsync(string name)
    {
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = name };
        await _registry.SaveAsync(entry);
        return entry;
    }

    private async Task<(WorkspaceRegistryEntry Source, WorkspaceRegistryEntry Target)> SaveReferencingWorkspacesAsync()
    {
        var target = await SaveWorkspaceAsync("shared-lib");
        var source = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            References = [new WorkspaceReference { WorkspaceId = target.WorkspaceId }],
        };
        await _registry.SaveAsync(source);
        return (source, target);
    }

    private sealed class FakeWorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
    {
        private readonly Dictionary<Guid, string?> _fingerprintsByWorkspaceId = [];

        public void Register(Guid workspaceId, string? fingerprint) => _fingerprintsByWorkspaceId[workspaceId] = fingerprint;

        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            Task.FromResult(_fingerprintsByWorkspaceId.GetValueOrDefault(entry.WorkspaceId));
    }
}
