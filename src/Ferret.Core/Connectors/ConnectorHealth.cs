namespace Ferret.Core.Connectors;

/// <summary>Represents the health status of a connector at a point in time.</summary>
public sealed class ConnectorHealth
{
    private ConnectorHealth(bool isConnected, string? errorMessage, DateTimeOffset checkedAt)
    {
        IsConnected = isConnected;
        ErrorMessage = errorMessage;
        CheckedAt = checkedAt;
    }

    /// <summary>Gets a value indicating whether the connector is currently reachable.</summary>
    public bool IsConnected { get; }

    /// <summary>Gets the error message if the connector is not connected; otherwise <see langword="null"/>.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets the UTC timestamp when this health check was performed.</summary>
    public DateTimeOffset CheckedAt { get; }

    /// <summary>Creates a healthy <see cref="ConnectorHealth"/>.</summary>
    /// <param name="checkedAt">The UTC timestamp when the health check was performed.</param>
    /// <returns>A connected <see cref="ConnectorHealth"/> instance.</returns>
    public static ConnectorHealth Connected(DateTimeOffset checkedAt) => new(true, null, checkedAt);

    /// <summary>Creates an unhealthy <see cref="ConnectorHealth"/>.</summary>
    /// <param name="errorMessage">The error message describing the connection failure.</param>
    /// <param name="checkedAt">The UTC timestamp when the health check was performed.</param>
    /// <returns>A disconnected <see cref="ConnectorHealth"/> instance.</returns>
    public static ConnectorHealth Disconnected(string errorMessage, DateTimeOffset checkedAt) =>
        new(false, errorMessage ?? string.Empty, checkedAt);
}
