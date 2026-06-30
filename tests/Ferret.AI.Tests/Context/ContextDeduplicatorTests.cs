using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class ContextDeduplicatorTests
{
    private static FileSearchHit MakeHit(string docId, float score) => new()
    {
        DocumentId = DocumentId.Create(docId),
        ConnectorInstanceId = new ConnectorInstanceId("fs-test"),
        CanonicalUri = new Uri($"file:///{docId}"),
        DisplayName = docId,
        Kind = SearchHitKind.File,
        Score = score,
        Snippet = HighlightedText.Plain(string.Empty),
    };

    [Fact]
    public void Deduplicate_NoDuplicates_ReturnsSameCount()
    {
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.7f) };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Deduplicate_WithDuplicate_ReturnsFirstOccurrence()
    {
        var hits = new[]
        {
            MakeHit("a", 0.9f),
            MakeHit("a", 0.5f), // duplicate — should be removed
            MakeHit("b", 0.7f),
        };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Equal(2, result.Count);
        Assert.Equal(0.9f, result[0].Score); // first occurrence kept
    }

    [Fact]
    public void Deduplicate_AllDuplicates_ReturnsOne()
    {
        var hits = new[]
        {
            MakeHit("a", 0.9f),
            MakeHit("a", 0.7f),
            MakeHit("a", 0.5f),
        };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Single(result);
    }

    [Fact]
    public void Deduplicate_EmptyList_ReturnsEmpty()
    {
        var result = ContextDeduplicator.Deduplicate([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Deduplicate_PreservesInputOrder()
    {
        var hits = new[]
        {
            MakeHit("c", 0.6f),
            MakeHit("a", 0.9f),
            MakeHit("b", 0.7f),
        };
        var result = ContextDeduplicator.Deduplicate(hits);
        Assert.Equal("c", result[0].DocumentId.Value);
        Assert.Equal("a", result[1].DocumentId.Value);
        Assert.Equal("b", result[2].DocumentId.Value);
    }
}
