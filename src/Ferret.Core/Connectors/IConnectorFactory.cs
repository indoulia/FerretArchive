namespace Ferret.Core.Connectors;

/// <summary>Creates connector instances and exposes the static descriptor for registration.</summary>
public interface IConnectorFactory
{
    /// <summary>Gets the connector type identifier this factory produces.</summary>
    ConnectorId ConnectorId { get; }

    /// <summary>Gets the static descriptor for the connector type this factory produces.</summary>
    ConnectorDescriptor Descriptor { get; }

    /// <summary>Creates a configured connector from a stored instance record.
    /// The factory reads <see cref="ConnectorInstance.Configuration"/> to populate its
    /// connector-type-specific configuration object.</summary>
    /// <param name="instance">The stored instance configuration.</param>
    /// <returns>A connector ready for use.</returns>
    IConnector Create(ConnectorInstance instance);
}
