using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class TypedIdTests
{
    [Fact]
    public void ConnectorId_Equality_By_Value()
    {
        var a = new ConnectorId("filesystem");
        var b = new ConnectorId("filesystem");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ConnectorId_Inequality_Different_Value()
    {
        Assert.NotEqual(new ConnectorId("filesystem"), new ConnectorId("git"));
    }

    [Fact]
    public void ConnectorId_ToString_Returns_Value()
    {
        Assert.Equal("filesystem", new ConnectorId("filesystem").ToString());
    }

    [Fact]
    public void ConnectorInstanceId_Equality_By_Value()
    {
        Assert.Equal(new ConnectorInstanceId("src-root"), new ConnectorInstanceId("src-root"));
    }

    [Fact]
    public void AssetId_From_Uri_Is_Deterministic()
    {
        var uri = new Uri("filesystem:///src/Program.cs");
        Assert.Equal(AssetId.From(uri), AssetId.From(uri));
    }

    [Fact]
    public void AssetId_From_Different_Uris_Are_Not_Equal()
    {
        var a = AssetId.From(new Uri("filesystem:///src/Program.cs"));
        var b = AssetId.From(new Uri("filesystem:///src/Other.cs"));
        Assert.NotEqual(a, b);
    }
}
