using Ferret.Core.Connectors;
using Ferret.Mcp.Resources;
using Xunit;

namespace Ferret.Mcp.Tests.Resources;

public sealed class ConnectorsResourceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsJsonWithConnectorList()
    {
        var registry = new FakeConnectorRegistry([MakeDescriptor("filesystem", "Filesystem")]);
        var sut = new ConnectorsResource(registry);

        var content = await sut.ReadAsync("workspace://connectors", CancellationToken.None);

        Assert.Equal("workspace://connectors", content.ResourceUri);
        Assert.Contains("filesystem", content.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Descriptor_HasCorrectUri()
    {
        var sut = new ConnectorsResource(new FakeConnectorRegistry([]));
        Assert.Equal("workspace://connectors", sut.Descriptor.ResourceUri);
    }

    private static ConnectorDescriptor MakeDescriptor(string id, string name) => new()
    {
        Id = new ConnectorId(id),
        Metadata = ConnectorMetadata.Create(id, name, string.Empty, ConnectorType.Filesystem, "v1"),
        Capabilities = [],
    };

    private sealed class FakeConnectorRegistry(IReadOnlyList<ConnectorDescriptor> descriptors) : IConnectorRegistry
    {
        public IReadOnlyList<ConnectorDescriptor> GetAll() => descriptors;

        public ConnectorDescriptor? GetById(ConnectorId id) => null;

        public bool IsRegistered(ConnectorId id) => false;

        public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) => [];
    }
}
