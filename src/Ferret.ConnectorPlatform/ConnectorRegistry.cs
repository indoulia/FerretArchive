using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform;

/// <summary>Immutable registry of connector descriptors. Built once via RegistryBuilder.</summary>
internal sealed class ConnectorRegistry : IConnectorRegistry
{
    private readonly IReadOnlyDictionary<ConnectorId, ConnectorDescriptor> _descriptors;

    internal ConnectorRegistry(IReadOnlyDictionary<ConnectorId, ConnectorDescriptor> descriptors) =>
        _descriptors = descriptors;

    /// <inheritdoc/>
    public IReadOnlyList<ConnectorDescriptor> GetAll() => [.. _descriptors.Values];

    /// <inheritdoc/>
    public ConnectorDescriptor? GetById(ConnectorId id) =>
        _descriptors.GetValueOrDefault(id);

    /// <inheritdoc/>
    public bool IsRegistered(ConnectorId id) => _descriptors.ContainsKey(id);

    /// <inheritdoc/>
    public IReadOnlyList<ConnectorDescriptor> GetByCapability(ConnectorCapability capability) =>
        [.. _descriptors.Values.Where(d => d.Capabilities.Contains(capability))];
}
