namespace Ferret.Core.Connectors;

/// <summary>Read-only registry of all discovered (DI-registered) connector descriptors.</summary>
public interface IConnectorRegistry
{
    /// <summary>Returns all registered connector descriptors.</summary>
    /// <returns>A read-only list of all registered connector descriptors.</returns>
    IReadOnlyList<ConnectorDescriptor> GetAll();

    /// <summary>Returns the descriptor for the given connector ID, or null if not registered.</summary>
    /// <param name="id">The connector ID to look up.</param>
    /// <returns>The connector descriptor if found; otherwise null.</returns>
    ConnectorDescriptor? GetById(ConnectorId id);

    /// <summary>Returns true if a connector with the given ID is registered.</summary>
    /// <param name="id">The connector ID to check.</param>
    /// <returns>True if a connector with the given ID is registered; otherwise false.</returns>
    bool IsRegistered(ConnectorId id);

    /// <summary>Returns all connectors that declare the given capability.</summary>
    /// <param name="capability">The capability to filter by.</param>
    /// <returns>A read-only list of connector descriptors that declare the given capability.</returns>
    IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability);
}
