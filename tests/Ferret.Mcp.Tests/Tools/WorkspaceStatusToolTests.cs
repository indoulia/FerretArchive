using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;

using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class WorkspaceStatusToolTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsJsonWithWorkspaceInfo()
    {
        var context = new FakeWorkspaceContext();
        var engine = new FakeIndexEngine(new IndexStats
        {
            DocumentCount = 42,
            TotalChars = 100000,
            LastIndexedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
            IndexSizeBytes = 512000,
        });
        var sut = new WorkspaceStatusTool(context, engine);

        var result = await sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None);

        Assert.False(result.IsError);
        var text = result.Content[0].Text!;
        Assert.Contains("42", text, StringComparison.Ordinal);
        Assert.Contains("test-workspace", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new WorkspaceStatusTool(new FakeWorkspaceContext(), new FakeIndexEngine(default!));
        Assert.Equal("workspace_status", sut.Descriptor.Name);
    }

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test-workspace");

        public WorkspacePath WorkspaceRoot => WorkspacePath.Create(Path.GetTempPath());
    }

    private sealed class FakeIndexEngine(IndexStats stats) : IIndexEngine
    {
        public Task WriteAsync(Document document, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IndexStats> GetStatsAsync(CancellationToken ct = default) => Task.FromResult(stats);

        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Ferret.Core.Primitives.DocumentId documentId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
