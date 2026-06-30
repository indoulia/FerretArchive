using Ferret.Core.Ai.Models;
using Xunit;

namespace Ferret.Core.Tests.Ai;

public sealed class RerankResultTests
{
    [Fact]
    public void RerankResult_Items_AreOrderedByDescendingScore()
    {
        var items = new[]
        {
            new RerankItem { Document = "b", Score = 0.5, Index = 1 },
            new RerankItem { Document = "a", Score = 0.9, Index = 0 },
            new RerankItem { Document = "c", Score = 0.2, Index = 2 },
        };
        var result = RerankResult.Create(items);
        Assert.Equal(0.9, result.Items[0].Score);
        Assert.Equal(0.5, result.Items[1].Score);
        Assert.Equal(0.2, result.Items[2].Score);
    }

    [Fact]
    public void RerankItem_PreservesOriginalIndex()
    {
        var item = new RerankItem { Document = "doc", Score = 0.8, Index = 3 };
        Assert.Equal(3, item.Index);
    }

    [Fact]
    public void RerankRequest_ModelId_DefaultsToNull()
    {
        var req = new RerankRequest { Query = "q", Documents = ["a", "b"] };
        Assert.Null(req.ModelId);
    }
}
