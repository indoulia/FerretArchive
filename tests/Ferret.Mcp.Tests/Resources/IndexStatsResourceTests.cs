using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Mcp.Resources;
using Xunit;

namespace Ferret.Mcp.Tests.Resources;

public sealed class IndexStatsResourceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsJsonWithStats()
    {
        var engine = new FakeIndexEngine(documentCount: 100);
        var sut = new IndexStatsResource(engine);

        var content = await sut.ReadAsync("workspace://index/stats", CancellationToken.None);

        Assert.Equal("workspace://index/stats", content.ResourceUri);
        Assert.Contains("100", content.Text, StringComparison.Ordinal);
        Assert.Contains("documentCount", content.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Descriptor_HasCorrectUri()
    {
        var sut = new IndexStatsResource(new FakeIndexEngine(0));
        Assert.Equal("workspace://index/stats", sut.Descriptor.ResourceUri);
    }

    private sealed class FakeIndexEngine(long documentCount) : IIndexEngine
    {
        public Task WriteAsync(Document document, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IndexStats> GetStatsAsync(CancellationToken ct = default) =>
            Task.FromResult(new IndexStats
            {
                DocumentCount = documentCount,
                TotalChars = 0,
                IndexSizeBytes = 0,
                LastIndexedAt = DateTimeOffset.MinValue,
            });

        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Ferret.Core.Primitives.DocumentId documentId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
