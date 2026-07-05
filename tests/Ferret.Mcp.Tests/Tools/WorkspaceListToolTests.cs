using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Ferret.Workspace.Graph;

using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class WorkspaceListToolTests
{
    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new WorkspaceListTool(new FakeWorkspaceRegistry([]));
        Assert.Equal("workspace_list", sut.Descriptor.Name);
    }

    [Fact]
    public async Task ExecuteAsync_NoWorkspaces_ReturnsEmptyJsonArray()
    {
        var sut = new WorkspaceListTool(new FakeWorkspaceRegistry([]));

        var result = await sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("[]", result.Content[0].Text!.Trim());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsWorkspacesOrderedByName_MatchingCliListFields()
    {
        var zebra = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "zebra",
            Kind = "team",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "github.com/acme/a" }, new RepoMember { Remote = "github.com/acme/b" }] },
        };
        var alpha = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "alpha",
            Kind = "personal",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "github.com/acme/c" }] },
        };
        var sut = new WorkspaceListTool(new FakeWorkspaceRegistry([zebra, alpha]));

        var result = await sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None);

        var text = result.Content[0].Text!;
        Assert.False(result.IsError);
        var alphaIndex = text.IndexOf("alpha", StringComparison.Ordinal);
        var zebraIndex = text.IndexOf("zebra", StringComparison.Ordinal);
        Assert.True(alphaIndex >= 0 && zebraIndex >= 0 && alphaIndex < zebraIndex);
        Assert.Contains("\"kind\": \"personal\"", text, StringComparison.Ordinal);
        Assert.Contains("\"repoCount\": 2", text, StringComparison.Ordinal);
        Assert.Contains("22222222-2222-2222-2222-222222222222", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_RegistryCorrupt_ReturnsErrorResult()
    {
        var sut = new WorkspaceListTool(new ThrowingWorkspaceRegistry());

        var result = await sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None);

        Assert.True(result.IsError);
    }

    private sealed class FakeWorkspaceRegistry(IReadOnlyList<WorkspaceRegistryEntry> entries) : IWorkspaceRegistry
    {
        public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default) =>
            Task.FromResult(entries.FirstOrDefault(e => e.WorkspaceId == workspaceId));

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult(entries);

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ThrowingWorkspaceRegistry : IWorkspaceRegistry
    {
        public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default) =>
            throw new WorkspaceRegistryCorruptException("bad.json", "malformed");

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            throw new WorkspaceRegistryCorruptException("bad.json", "malformed");

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
    }
}
