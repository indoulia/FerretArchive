using Ferret.Mcp.Transport.Stdio;

using Xunit;

namespace Ferret.Mcp.Tests.Transport;

public sealed class McpErrorMapperTests
{
    private readonly McpErrorMapper _sut = new();

    [Fact]
    public void MapException_ArgumentException_ReturnsErrorWithMessage()
    {
        var ex = new ArgumentException("bad param");

        var result = _sut.MapException(ex);

        Assert.True(result.IsError);
        Assert.Contains("bad param", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapException_InvalidOperationException_ReturnsErrorWithMessage()
    {
        var ex = new InvalidOperationException("state mismatch");

        var result = _sut.MapException(ex);

        Assert.True(result.IsError);
        Assert.Contains("state mismatch", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapException_UnknownException_ReturnsGenericError()
    {
        var ex = new NotSupportedException("something went wrong");

        var result = _sut.MapException(ex);

        Assert.True(result.IsError);
        Assert.Contains("something went wrong", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void MapException_Result_IsAlwaysError()
    {
        var result = _sut.MapException(new NotSupportedException("x"));
        Assert.True(result.IsError);
    }
}
