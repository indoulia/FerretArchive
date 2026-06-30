using System.Text.Json;

using Ferret.Mcp.Transport.Stdio;

using Xunit;

namespace Ferret.Mcp.Tests.Transport;

public sealed class McpArgumentsFactoryTests
{
    [Fact]
    public void From_NullDictionary_ReturnsEmpty()
    {
        var result = McpArgumentsFactory.From(null);
        Assert.Equal(string.Empty, result.GetString("anything") is null ? string.Empty : "not-empty");
        Assert.Null(result.GetString("anything"));
    }

    [Fact]
    public void From_EmptyDictionary_ReturnsEmpty()
    {
        var result = McpArgumentsFactory.From(new Dictionary<string, JsonElement>());
        Assert.Null(result.GetString("x"));
    }

    [Fact]
    public void From_StringElement_ExtractsRawString()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonDocument.Parse("\"hello world\"").RootElement,
        };

        var result = McpArgumentsFactory.From(dict);

        Assert.Equal("hello world", result.GetString("query"));
    }

    [Fact]
    public void From_IntElement_ExtractsRawText()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["limit"] = JsonDocument.Parse("42").RootElement,
        };

        var result = McpArgumentsFactory.From(dict);

        Assert.Equal("42", result.GetString("limit"));
    }

    [Fact]
    public void From_MissingKey_ReturnsNull()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["query"] = JsonDocument.Parse("\"test\"").RootElement,
        };

        var result = McpArgumentsFactory.From(dict);

        Assert.Null(result.GetString("missing"));
    }
}
