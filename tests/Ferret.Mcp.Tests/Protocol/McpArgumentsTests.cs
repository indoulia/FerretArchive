using Ferret.Mcp.Protocol;

using Xunit;

namespace Ferret.Mcp.Tests.Protocol;

public sealed class McpArgumentsTests
{
    [Fact]
    public void GetString_ExistingKey_ReturnsValue()
    {
        var args = McpArguments.From(("key", "value"));
        Assert.Equal("value", args.GetString("key"));
    }

    [Fact]
    public void GetString_MissingKey_ReturnsNull()
    {
        var args = McpArguments.Empty;
        Assert.Null(args.GetString("missing"));
    }

    [Fact]
    public void GetRequiredString_MissingKey_Throws()
    {
        var args = McpArguments.Empty;
        Assert.Throws<InvalidOperationException>(() => args.GetRequiredString("required"));
    }

    [Fact]
    public void TryGetInt32_ValidInteger_ReturnsTrueAndValue()
    {
        var args = McpArguments.From(("count", "42"));
        Assert.True(args.TryGetInt32("count", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetInt32_MissingKey_ReturnsFalse()
    {
        var args = McpArguments.Empty;
        Assert.False(args.TryGetInt32("missing", out _));
    }
}
