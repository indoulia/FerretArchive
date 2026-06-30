namespace Ferret.Core.Connectors;

/// <summary>
/// Stored configuration for a single connector instance.
/// Represents a user-named, persisted connector binding (e.g. "workspace" → filesystem at ".").
/// Part of the Metadata → Descriptor → Instance → Status / Runtime pattern.
/// </summary>
public sealed record ConnectorInstance
{
    /// <summary>Gets the workspace-scoped instance identifier.</summary>
    public required ConnectorInstanceId Id { get; init; }

    /// <summary>Gets the connector type identifier (e.g. "filesystem").</summary>
    public required ConnectorId ConnectorType { get; init; }

    /// <summary>Gets the human-readable display name for this instance.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets a value indicating whether this instance is enabled. Default: true.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Gets the schema version for migration purposes. Default: "1.0".</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Gets the optional tags associated with this instance.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets the connector-type-specific configuration values for this instance.</summary>
    public ConnectorConfiguration Configuration { get; init; } = ConnectorConfiguration.Empty;

    // Reserved: ConnectorPolicy? Policy — read-only, bandwidth limits, security constraints
    // Reserved: string? ProfileId — credential sharing via ConnectorProfile
}
