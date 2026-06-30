using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;

using Xunit;

namespace Ferret.Mcp.Tests.Registry;

public sealed class McpResourceRegistryTests
{
    private static FakeResource MakeResource(string uri, string name) => new(uri, name);

    [Fact]
    public void GetAll_ReturnsAllDescriptors()
    {
        var registry = new McpResourceRegistryBuilder()
            .Add(MakeResource("workspace://status", "workspace_status"))
            .Build();

        var all = registry.GetAll();
        Assert.Single(all);
        Assert.Equal("workspace://status", all[0].ResourceUri);
    }

    [Fact]
    public void GetByUri_ExistingResource_ReturnsResource()
    {
        var registry = new McpResourceRegistryBuilder()
            .Add(MakeResource("workspace://status", "workspace_status"))
            .Build();

        Assert.NotNull(registry.GetByUri("workspace://status"));
    }

    [Fact]
    public void GetByUri_MissingResource_ReturnsNull()
    {
        var registry = new McpResourceRegistryBuilder().Build();
        Assert.Null(registry.GetByUri("workspace://none"));
    }

    private sealed class FakeResource(string uri, string name) : IMcpResource
    {
        public McpResourceDescriptor Descriptor { get; } = new()
        {
            ResourceUri = uri,
            Name = name,
            Description = "test",
        };

        public Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct) =>
            Task.FromResult(new McpResourceContent { ResourceUri = resourceUri, MimeType = "application/json", Text = "{}" });
    }
}
