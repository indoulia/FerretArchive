using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Xunit;

namespace Ferret.Core.Tests.Documents;

public sealed class DocumentTypedIdTests
{
    [Fact]
    public void DocumentId_From_AssetId_Is_Deterministic()
    {
        var assetId = new AssetId("filesystem:///src/Program.cs");
        Assert.Equal(DocumentId.From(assetId), DocumentId.From(assetId));
    }

    [Fact]
    public void DocumentId_From_Different_AssetIds_Are_Not_Equal()
    {
        var a = DocumentId.From(new AssetId("filesystem:///src/A.cs"));
        var b = DocumentId.From(new AssetId("filesystem:///src/B.cs"));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DocumentId_From_Preserves_AssetId_Value()
    {
        var assetId = new AssetId("filesystem:///src/Program.cs");
        Assert.Equal(assetId.Value, DocumentId.From(assetId).Value);
    }

    [Fact]
    public void ParserId_Equality_By_Value()
    {
        Assert.Equal(new ParserId("text/plain"), new ParserId("text/plain"));
    }

    [Fact]
    public void ParserId_Inequality_Different_Value()
    {
        Assert.NotEqual(new ParserId("text/plain"), new ParserId("text/markdown"));
    }

    [Fact]
    public void ParserId_ToString_Returns_Value()
    {
        Assert.Equal("text/markdown", new ParserId("text/markdown").ToString());
    }

    [Fact]
    public void DocumentKind_Has_Expected_Integer_Values()
    {
        Assert.Equal(0, (int)DocumentKind.Code);
        Assert.Equal(1, (int)DocumentKind.Prose);
        Assert.Equal(2, (int)DocumentKind.Data);
        Assert.Equal(3, (int)DocumentKind.Config);
        Assert.Equal(99, (int)DocumentKind.Unknown);
    }

    [Fact]
    public void DocumentSection_Equality_By_Value()
    {
        var a = new DocumentSection("Introduction", "Content here.", 1, 5);
        var b = new DocumentSection("Introduction", "Content here.", 1, 5);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DocumentSection_Title_May_Be_Null()
    {
        var section = new DocumentSection(null, "Content", 1, 1);
        Assert.Null(section.Title);
    }
}
