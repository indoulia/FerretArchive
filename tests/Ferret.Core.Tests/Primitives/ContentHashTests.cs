using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Primitives;

public sealed class ContentHashTests
{
    [Fact]
    public void ContentHash_Create_ReturnsInstance()
    {
        var hash = ContentHash.Create("sha256", "abc123");
        Assert.Equal("sha256", hash.Algorithm);
        Assert.Equal("abc123", hash.Hex);
    }

    [Fact]
    public void ContentHash_Create_ThrowsOnEmptyAlgorithm() =>
        Assert.Throws<ArgumentException>(() => ContentHash.Create(string.Empty, "abc"));

    [Fact]
    public void ContentHash_Create_ThrowsOnEmptyHex() =>
        Assert.Throws<ArgumentException>(() => ContentHash.Create("sha256", string.Empty));

    [Fact]
    public void ContentHash_Equality_SameValues_IsEqual()
    {
        var a = ContentHash.Create("sha256", "abc");
        var b = ContentHash.Create("sha256", "abc");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ContentHash_Equality_DifferentHex_IsNotEqual()
    {
        var a = ContentHash.Create("sha256", "abc");
        var b = ContentHash.Create("sha256", "def");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ContentHash_ToString_ReturnsCombined() =>
        Assert.Equal("sha256:abc123", ContentHash.Create("sha256", "abc123").ToString());
}
