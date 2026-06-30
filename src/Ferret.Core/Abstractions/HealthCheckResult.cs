using Ferret.Core.Enumerations;

namespace Ferret.Core.Abstractions;

/// <summary>Represents the result of a health check evaluation.</summary>
public sealed class HealthCheckResult
{
    private HealthCheckResult(HealthStatus status, string description, Exception? exception)
    {
        Status = status;
        Description = description;
        Exception = exception;
    }

    /// <summary>Gets the health status reported by the check.</summary>
    public HealthStatus Status { get; }

    /// <summary>Gets a human-readable description of the health check outcome.</summary>
    public string Description { get; }

    /// <summary>Gets the exception that caused an unhealthy state, or <see langword="null"/>.</summary>
    public Exception? Exception { get; }

    /// <summary>Creates a healthy result.</summary>
    /// <param name="description">A description of the healthy state.</param>
    /// <returns>A healthy <see cref="HealthCheckResult"/>.</returns>
    public static HealthCheckResult Healthy(string description) =>
        new(HealthStatus.Healthy, description, null);

    /// <summary>Creates a degraded result.</summary>
    /// <param name="description">A description of the degraded state.</param>
    /// <returns>A degraded <see cref="HealthCheckResult"/>.</returns>
    public static HealthCheckResult Degraded(string description) =>
        new(HealthStatus.Degraded, description, null);

    /// <summary>Creates an unhealthy result.</summary>
    /// <param name="description">A description of the unhealthy state.</param>
    /// <param name="exception">The exception that caused the unhealthy state.</param>
    /// <returns>An unhealthy <see cref="HealthCheckResult"/>.</returns>
    public static HealthCheckResult Unhealthy(string description, Exception? exception = null) =>
        new(HealthStatus.Unhealthy, description, exception);
}
