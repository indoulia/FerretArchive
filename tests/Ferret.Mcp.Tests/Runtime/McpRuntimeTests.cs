using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;
using Ferret.Mcp.Runtime;

using Xunit;

namespace Ferret.Mcp.Tests.Runtime;

public sealed class McpRuntimeTests
{
    [Fact]
    public async Task RunAsync_PassesToolsAndResourcesToTransport()
    {
        var tool = new FakeTool("ping");
        var resource = new FakeResource("workspace://test");
        var transport = new CapturingTransport();
        var sut = new McpRuntime([tool], [resource], transport);

        await sut.RunAsync(CancellationToken.None);

        Assert.NotNull(transport.CapturedTools);
        Assert.NotNull(transport.CapturedResources);
        Assert.NotNull(transport.CapturedTools!.GetByName("ping"));
        Assert.NotNull(transport.CapturedResources!.GetByUri("workspace://test"));
    }

    [Fact]
    public async Task RunAsync_EmptyCollections_RunsWithoutError()
    {
        var transport = new CapturingTransport();
        var sut = new McpRuntime([], [], transport);

        await sut.RunAsync(CancellationToken.None);

        Assert.NotNull(transport.CapturedTools);
        Assert.Empty(transport.CapturedTools!.GetAll());
    }

    private sealed class FakeTool(string name) : IMcpTool
    {
        public McpToolDescriptor Descriptor { get; } = new() { Name = name, Description = name };

        public Task<McpToolResult> ExecuteAsync(McpArguments args, CancellationToken ct) =>
            Task.FromResult(McpToolResult.Success("ok"));
    }

    private sealed class FakeResource(string uri) : IMcpResource
    {
        public McpResourceDescriptor Descriptor { get; } = new() { ResourceUri = uri, Name = uri, Description = uri };

        public Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct) =>
            Task.FromResult(new McpResourceContent { ResourceUri = resourceUri, MimeType = "text/plain", Text = string.Empty });
    }

    private sealed class CapturingTransport : IMcpTransport
    {
        public McpTransportDescriptor Descriptor { get; } = new() { Name = "fake", Description = "fake" };

        public IMcpToolRegistry? CapturedTools { get; private set; }

        public IMcpResourceRegistry? CapturedResources { get; private set; }

        public Task RunAsync(IMcpToolRegistry tools, IMcpResourceRegistry resources, CancellationToken ct)
        {
            CapturedTools = tools;
            CapturedResources = resources;
            return Task.CompletedTask;
        }
    }
}
