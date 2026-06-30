namespace Ferret.Core.Connectors;

/// <summary>Descriptive metadata for a context source connector.</summary>
public sealed class ConnectorMetadata
{
    private ConnectorMetadata(string id, string name, string description, ConnectorType connectorType, string version)
    {
        Id = id;
        Name = name;
        Description = description;
        ConnectorType = connectorType;
        Version = version;
    }

    /// <summary>Gets the unique connector identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the human-readable connector name.</summary>
    public string Name { get; }

    /// <summary>Gets the connector description.</summary>
    public string Description { get; }

    /// <summary>Gets the connector type category.</summary>
    public ConnectorType ConnectorType { get; }

    /// <summary>Gets the connector version string.</summary>
    public string Version { get; }

    /// <summary>Creates a new <see cref="ConnectorMetadata"/> instance.</summary>
    /// <param name="id">The unique connector identifier.</param>
    /// <param name="name">The human-readable connector name.</param>
    /// <param name="description">The connector description.</param>
    /// <param name="connectorType">The connector type category.</param>
    /// <param name="version">The connector version string.</param>
    /// <returns>A new <see cref="ConnectorMetadata"/> instance.</returns>
    public static ConnectorMetadata Create(string id, string name, string description, ConnectorType connectorType, string version) =>
        new(id ?? string.Empty, name ?? string.Empty, description ?? string.Empty, connectorType, version ?? string.Empty);
}
