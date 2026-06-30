using Ferret.Mcp.Protocol;

using Xunit;

namespace Ferret.Mcp.Tests.Protocol;

public sealed class McpToolResultTests
{
    [Fact]
    public void Success_SetsIsErrorFalse_AndTextContent()
    {
        var result = McpToolResult.Success("hello");
        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal("text", result.Content[0].Type);
        Assert.Equal("hello", result.Content[0].Text);
    }

    [Fact]
    public void Error_SetsIsErrorTrue_AndTextContent()
    {
        var result = McpToolResult.Error("bad input");
        Assert.True(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal("bad input", result.Content[0].Text);
    }
}
