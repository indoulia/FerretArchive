using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.Tests.Fakes;

internal sealed class FakeConnectorFactory : IConnectorFactory
{
    internal FakeConnectorFactory(string id, params ConnectorCapability[] capabilities)
    {
        ConnectorId = new ConnectorId(id);
        Descriptor = new ConnectorDescriptor
        {
            Id = ConnectorId,
            Metadata = ConnectorMetadata.Create(id, id, $"{id} connector", ConnectorType.Custom, "1.0"),
            Capabilities = capabilities,
            SupportedPlatforms = ["Linux", "macOS", "Windows"],
        };
    }

    public ConnectorId ConnectorId { get; }

    public ConnectorDescriptor Descriptor { get; }

    public IConnector Create(ConnectorInstance instance) =>
        throw new NotImplementedException("FakeConnectorFactory does not create connectors.");
}
