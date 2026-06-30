namespace Ferret.Core.Connectors;

/// <summary>
/// Runtime wrapper for an active connector: stored instance configuration + live connector + current status.
/// Only <c>ConnectorManager</c> creates and disposes <see cref="ConnectorRuntime"/> instances (ADR-0014 Principle 10).
/// Pipelines receive <see cref="ConnectorRuntime"/> from the manager and never construct connectors directly.
/// </summary>
public sealed record ConnectorRuntime
{
    /// <summary>Gets the stored instance configuration.</summary>
    public required ConnectorInstance Instance { get; init; }

    /// <summary>Gets the live connector.</summary>
    public required IConnector Connector { get; init; }

    /// <summary>Gets the current runtime status.</summary>
    public required ConnectorStatus Status { get; init; }

    // Reserved: IConnectorSession Session — active session (post-ConnectAsync)
}
