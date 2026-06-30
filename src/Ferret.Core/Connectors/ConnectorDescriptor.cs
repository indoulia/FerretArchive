namespace Ferret.Core.Connectors;

/// <summary>Static descriptor for a registered connector type. Immutable — no public setters.</summary>
public sealed record ConnectorDescriptor
{
    /// <summary>Gets the stable connector type identifier.</summary>
    public required ConnectorId Id { get; init; }

    /// <summary>Gets the connector metadata (name, description, version).</summary>
    public required ConnectorMetadata Metadata { get; init; }

    /// <summary>Gets the capabilities this connector declares.</summary>
    public required IReadOnlyList<ConnectorCapability> Capabilities { get; init; }

    /// <summary>Gets the OS platforms this connector supports (e.g. "Linux", "macOS", "Windows").</summary>
    public IReadOnlyList<string> SupportedPlatforms { get; init; } = [];

    /// <summary>Gets an optional URI pointing to documentation for this connector.</summary>
    public Uri? DocumentationUri { get; init; }
}
