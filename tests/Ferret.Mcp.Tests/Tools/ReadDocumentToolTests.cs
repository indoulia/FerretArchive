using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class ReadDocumentToolTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingDocument_ReturnsContent()
    {
        var doc = MakeDocument("doc-1", "hello world", "Hello");
        var service = new FakeDocumentService(doc);
        var sut = new ReadDocumentTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("document_id", "doc-1")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("hello world", result.Content[0].Text, StringComparison.Ordinal);
        Assert.Contains("Hello", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_MissingDocument_ReturnsError()
    {
        var service = new FakeDocumentService(null);
        var sut = new ReadDocumentTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("document_id", "missing")), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new ReadDocumentTool(new FakeDocumentService(null));
        Assert.Equal("read_document", sut.Descriptor.Name);
    }

    private static Document MakeDocument(string id, string plainText, string title) => new()
    {
        Id = DocumentId.Create(id),
        SourceAssetId = new AssetId(id),
        ConnectorId = new ConnectorId("fs"),
        InstanceId = new ConnectorInstanceId("fs-1"),
        MediaType = "text/plain",
        Kind = DocumentKind.Code,
        PlainText = plainText,
        Title = title,
        ProducedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeDocumentService(Document? document) : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct) =>
            Task.FromResult(document);
    }
}
