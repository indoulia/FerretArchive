using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform;

/// <summary>Builds an IConnectorRegistry from IConnectorFactory instances. Does not depend on DI.</summary>
public static class RegistryBuilder
{
    /// <summary>Builds an immutable registry from the provided factories.</summary>
    /// <param name="factories">The connector factories to register.</param>
    /// <returns>An immutable <see cref="IConnectorRegistry"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if two factories share the same ConnectorId.</exception>
    public static IConnectorRegistry Build(IEnumerable<IConnectorFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        var dict = new Dictionary<ConnectorId, ConnectorDescriptor>();
        foreach (var factory in factories)
        {
            if (!dict.TryAdd(factory.ConnectorId, factory.Descriptor))
            {
                throw new InvalidOperationException(
                    $"Duplicate connector ID: '{factory.ConnectorId.Value}'. Each connector must have a unique ID.");
            }
        }

        return new ConnectorRegistry(dict);
    }
}
