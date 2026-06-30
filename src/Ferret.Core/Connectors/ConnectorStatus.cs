namespace Ferret.Core.Connectors;

/// <summary>Current runtime state of a connector instance. Never used as configuration.</summary>
public sealed record ConnectorStatus
{
    /// <summary>Gets the connector type identifier.</summary>
    public required ConnectorId ConnectorId { get; init; }

    /// <summary>Gets the workspace-scoped instance identifier.</summary>
    public required ConnectorInstanceId InstanceId { get; init; }

    /// <summary>Gets a value indicating whether the connector is currently active.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Gets the current connector health.</summary>
    public required ConnectorHealth Health { get; init; }

    /// <summary>Gets the time of the last successful sync, or null if never synced.</summary>
    public DateTimeOffset? LastSyncAt { get; init; }

    /// <summary>Gets the current error message, if any.</summary>
    public string? CurrentError { get; init; }
}
