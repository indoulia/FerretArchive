namespace Ferret.Core.Connectors;

/// <summary>
/// Activates, caches, and vends connector runtimes for the process lifetime.
/// Only <c>ConnectorManager</c> implements this — no other subsystem constructs connectors directly.
/// </summary>
public interface IConnectorManager
{
    /// <summary>Returns all active (enabled) connector runtimes.
    /// Results are process-scoped cached — the same <see cref="ConnectorRuntime"/> instance
    /// is returned across calls for the same instance ID.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that yields the list of active connector runtimes.</returns>
    Task<IReadOnlyList<ConnectorRuntime>> GetActiveConnectorsAsync(CancellationToken ct = default);

    /// <summary>Returns the stored instance configuration for the given ID, or null if not found.</summary>
    /// <param name="id">The workspace-scoped instance identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that yields the connector instance, or null if not found.</returns>
    Task<ConnectorInstance?> GetInstanceAsync(ConnectorInstanceId id, CancellationToken ct = default);

    // Reserved: Task ReconnectAsync(ConnectorInstanceId id, CancellationToken ct = default);
    // Reserved: Task<ConnectorHealth> CheckHealthAsync(ConnectorInstanceId id, CancellationToken ct = default);
}
