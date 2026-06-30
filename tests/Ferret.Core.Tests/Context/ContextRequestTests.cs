using Ferret.Core.Context;
using Xunit;

namespace Ferret.Core.Tests.Context;

public sealed class ContextRequestTests
{
    [Fact]
    public void Create_MinimalQuery_HasDefaults()
    {
        var req = new ContextRequest { Query = "authentication" };
        Assert.Equal("authentication", req.Query);
        Assert.Equal(8000, req.MaxTokens);
        Assert.Equal(10, req.MaxDocuments);
        Assert.True(req.IncludeSections);
    }

    [Fact]
    public void Create_CustomValues_ArePreserved()
    {
        var req = new ContextRequest
        {
            Query = "database migrations",
            MaxTokens = 4000,
            MaxDocuments = 5,
            IncludeSections = false,
        };
        Assert.Equal(4000, req.MaxTokens);
        Assert.Equal(5, req.MaxDocuments);
        Assert.False(req.IncludeSections);
    }

    [Fact]
    public void Query_CannotBeNull()
    {
        // record init will throw if Query is null because it's required
        Assert.Throws<InvalidOperationException>(() =>
        {
            var unused = new ContextRequest { Query = null! };
        });
    }
}
