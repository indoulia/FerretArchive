using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class DocumentExpanderTests
{
    private static FileSearchHit MakeHit(string docId) => new()
    {
        DocumentId = DocumentId.Create(docId),
        ConnectorInstanceId = new ConnectorInstanceId("test"),
        CanonicalUri = new Uri($"filesystem:///{docId}"),
        DisplayName = docId,
        Kind = SearchHitKind.File,
        Score = 0.9f,
        Snippet = new HighlightedText { Spans = [] },
    };

    private static Document MakeDocument(string docId, string text) => new()
    {
        Id = DocumentId.Create(docId),
        SourceAssetId = new AssetId(docId),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        MediaType = "text/plain",
        Kind = DocumentKind.Code,
        PlainText = text,
        ProducedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task ExpandAsync_AllHitsFound_ReturnsAllDocuments()
    {
        var docService = new StubDocumentService(new Dictionary<string, Document>
        {
            ["doc-a"] = MakeDocument("doc-a", "content a"),
            ["doc-b"] = MakeDocument("doc-b", "content b"),
        });
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);

        var hits = new[] { MakeHit("doc-a"), MakeHit("doc-b") };
        var result = await expander.ExpandAsync(hits, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Id.Value == "doc-a");
        Assert.Contains(result, d => d.Id.Value == "doc-b");
    }

    [Fact]
    public async Task ExpandAsync_MissingDocument_IsSkipped()
    {
        var docService = new StubDocumentService(new Dictionary<string, Document>
        {
            ["doc-a"] = MakeDocument("doc-a", "content a"),
        });
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);

        var hits = new[] { MakeHit("doc-a"), MakeHit("doc-b") };
        var result = await expander.ExpandAsync(hits, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("doc-a", result[0].Id.Value);
    }

    [Fact]
    public async Task ExpandAsync_EmptyHits_ReturnsEmpty()
    {
        var docService = new StubDocumentService(new Dictionary<string, Document>());
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);

        var result = await expander.ExpandAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    private sealed class StubDocumentService(Dictionary<string, Document> store) : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct)
        {
            store.TryGetValue(id.Value, out var doc);
            return Task.FromResult(doc);
        }
    }
}
