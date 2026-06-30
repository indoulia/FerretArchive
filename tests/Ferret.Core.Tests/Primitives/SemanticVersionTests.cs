using Ferret.Core.Primitives;

namespace Ferret.Core.Tests.Primitives;

public sealed class SemanticVersionTests
{
    [Fact]
    public void SemanticVersion_Create_ParsesCorrectly()
    {
        var v = SemanticVersion.Parse("1.2.3");
        Assert.Equal(1, v.Major);
        Assert.Equal(2, v.Minor);
        Assert.Equal(3, v.Patch);
        Assert.Null(v.PreRelease);
    }

    [Fact]
    public void SemanticVersion_Create_WithPreRelease()
    {
        var v = SemanticVersion.Parse("1.0.0-beta.1");
        Assert.Equal(1, v.Major);
        Assert.Equal(0, v.Minor);
        Assert.Equal(0, v.Patch);
        Assert.Equal("beta.1", v.PreRelease);
    }

    [Fact]
    public void SemanticVersion_Parse_ThrowsOnInvalidFormat() =>
        Assert.Throws<FormatException>(() => SemanticVersion.Parse("not-a-version"));

    [Fact]
    public void SemanticVersion_Equality_SameVersion_IsEqual()
    {
        var a = SemanticVersion.Parse("2.0.0");
        var b = SemanticVersion.Parse("2.0.0");
        Assert.Equal(a, b);
    }

    [Fact]
    public void SemanticVersion_ToString_ReturnsString()
    {
        Assert.Equal("1.2.3", SemanticVersion.Parse("1.2.3").ToString());
        Assert.Equal("1.0.0-beta.1", SemanticVersion.Parse("1.0.0-beta.1").ToString());
    }

    [Fact]
    public void SemanticVersion_Comparison_OlderIsLess()
    {
        var older = SemanticVersion.Parse("1.0.0");
        var newer = SemanticVersion.Parse("2.0.0");
        Assert.True(older.CompareTo(newer) < 0);
    }
}
