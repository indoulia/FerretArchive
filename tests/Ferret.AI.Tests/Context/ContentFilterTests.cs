using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class ContentFilterTests
{
    private static Document MakeDocument(string id, string plainText, DocumentKind kind = DocumentKind.Code) =>
        new()
        {
            Id = DocumentId.Create(id),
            SourceAssetId = new AssetId(id),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            MediaType = "text/plain",
            Kind = kind,
            PlainText = plainText,
            ProducedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public void Filter_EmptyContent_IsExcluded()
    {
        var docs = new[] { MakeDocument("a", string.Empty) };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_WhitespaceContent_IsExcluded()
    {
        var docs = new[] { MakeDocument("a", "   \n\t  ") };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ContentUnder50Chars_IsExcluded()
    {
        var docs = new[] { MakeDocument("a", "short") };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ContentExactly50Chars_IsExcluded()
    {
        // 50 chars after trim is the boundary — must be > 50 to pass
        var docs = new[] { MakeDocument("a", new string('x', 50)) };
        var result = ContentFilter.Filter(docs);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ContentOver50Chars_IsIncluded()
    {
        var docs = new[] { MakeDocument("a", new string('x', 51)) };
        var result = ContentFilter.Filter(docs);
        Assert.Single(result);
    }

    [Fact]
    public void Filter_NormalDocument_IsIncluded()
    {
        var content = "public class AuthService { private readonly IUserRepository _repo; }";
        var docs = new[] { MakeDocument("a", content) };
        var result = ContentFilter.Filter(docs);
        Assert.Single(result);
    }

    [Fact]
    public void Filter_ContentDuplicate_SecondIsExcluded()
    {
        var content = new string('x', 100);
        var docs = new[]
        {
            MakeDocument("a", content),
            MakeDocument("b", content), // same content, different id
        };
        var result = ContentFilter.Filter(docs);
        Assert.Single(result);
        Assert.Equal("a", result[0].Id.Value); // first wins
    }

    [Fact]
    public void Filter_DistinctContent_BothIncluded()
    {
        var docs = new[]
        {
            MakeDocument("a", new string('a', 100)),
            MakeDocument("b", new string('b', 100)),
        };
        var result = ContentFilter.Filter(docs);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Filter_EmptyList_ReturnsEmpty()
    {
        var result = ContentFilter.Filter([]);
        Assert.Empty(result);
    }
}
