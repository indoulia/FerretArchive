using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Mcp.Resources;
using Xunit;

namespace Ferret.Mcp.Tests.Resources;

public sealed class WorkspaceStatusResourceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsJsonWithWorkspaceInfo()
    {
        var sut = new WorkspaceStatusResource(new FakeWorkspaceContext(), new FakeIndexEngine());

        var content = await sut.ReadAsync("workspace://status", CancellationToken.None);

        Assert.Equal("workspace://status", content.ResourceUri);
        Assert.Equal("application/json", content.MimeType);
        Assert.Contains("test-workspace", content.Text, StringComparison.Ordinal);
        Assert.Contains("documentCount", content.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Descriptor_HasCorrectUri()
    {
        var sut = new WorkspaceStatusResource(new FakeWorkspaceContext(), new FakeIndexEngine());
        Assert.Equal("workspace://status", sut.Descriptor.ResourceUri);
    }

    private sealed class FakeWorkspaceContext : IWorkspaceContext
    {
        public WorkspaceId WorkspaceId => WorkspaceId.Create("test-workspace");

        public WorkspacePath WorkspaceRoot => WorkspacePath.Create(Path.GetTempPath());
    }

    private sealed class FakeIndexEngine : IIndexEngine
    {
        public Task WriteAsync(Document document, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IndexStats> GetStatsAsync(CancellationToken ct = default) =>
            Task.FromResult(new IndexStats
            {
                DocumentCount = 5,
                TotalChars = 1000,
                IndexSizeBytes = 4096,
                LastIndexedAt = DateTimeOffset.UtcNow,
            });

        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Ferret.Core.Primitives.DocumentId documentId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
