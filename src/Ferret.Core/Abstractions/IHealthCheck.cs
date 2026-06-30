namespace Ferret.Core.Abstractions;

/// <summary>Enables a component to report its own health status.</summary>
public interface IHealthCheck
{
    /// <summary>Checks the health of this component asynchronously.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that resolves to a <see cref="HealthCheckResult"/>.</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
