namespace Ferret.Core.Connectors;

/// <summary>Contract for all ContextOS context source connectors.</summary>
public interface IConnector
{
    /// <summary>Gets the connector type category.</summary>
    ConnectorType ConnectorType { get; }

    /// <summary>Gets the connector metadata.</summary>
    ConnectorMetadata Metadata { get; }

    /// <summary>Gets the connector's declared I/O capabilities.</summary>
    ConnectorIoCapabilities Capabilities { get; }

    /// <summary>Returns the current health of this connector.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ConnectorHealth"/> describing the current state of the connector.</returns>
    Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default);

    /// <summary>Establishes a connection and returns an active session.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An <see cref="IConnectorSession"/> that must be disposed when done.</returns>
    Task<IConnectorSession> ConnectAsync(CancellationToken ct = default);

    /// <summary>Closes the connection to the underlying source.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the connection is closed.</returns>
    Task DisconnectAsync(CancellationToken ct = default);
}
