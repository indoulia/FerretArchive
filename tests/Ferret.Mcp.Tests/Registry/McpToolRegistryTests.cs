using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;

using Xunit;

namespace Ferret.Mcp.Tests.Registry;

public sealed class McpToolRegistryTests
{
    private static FakeTool MakeTool(string name) => new(name);

    [Fact]
    public void GetAll_ReturnsAllDescriptors()
    {
        var registry = new McpToolRegistryBuilder()
            .Add(MakeTool("search"))
            .Add(MakeTool("read_document"))
            .Build();

        var all = registry.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, d => d.Name == "search");
        Assert.Contains(all, d => d.Name == "read_document");
    }

    [Fact]
    public void GetByName_ExistingTool_ReturnsTool()
    {
        var registry = new McpToolRegistryBuilder().Add(MakeTool("search")).Build();
        Assert.NotNull(registry.GetByName("search"));
    }

    [Fact]
    public void GetByName_MissingTool_ReturnsNull()
    {
        var registry = new McpToolRegistryBuilder().Build();
        Assert.Null(registry.GetByName("not_found"));
    }

    private sealed class FakeTool(string name) : IMcpTool
    {
        public McpToolDescriptor Descriptor { get; } = new() { Name = name, Description = "test" };

        public Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct) =>
            Task.FromResult(McpToolResult.Success("ok"));
    }
}
