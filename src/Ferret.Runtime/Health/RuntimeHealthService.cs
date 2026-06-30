using Ferret.Core.Abstractions;
using Ferret.Core.Enumerations;

namespace Ferret.Runtime.Health;

/// <summary>
/// Sequentially runs all registered <see cref="IHealthCheck"/> instances and aggregates their results.
/// <para>Why: Provides a single entry point for whole-platform health evaluation, isolating individual check failures so one bad check cannot prevent the others from running.</para>
/// <para>Lifecycle: Created once by RuntimeBuilder.Build() and registered as a DI singleton; lives until the RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime internal — consumed by the host diagnostics pipeline; must not be referenced from Ferret.Core or module assemblies.</para>
/// <para>Thread Safety: Conditionally Thread Safe — CheckAsync may be called concurrently; individual checks are invoked sequentially within a single call.</para>
/// </summary>
internal sealed class RuntimeHealthService
{
    private readonly IReadOnlyList<IHealthCheck> _checks;

    /// <summary>Initializes a new instance of the <see cref="RuntimeHealthService"/> class.</summary>
    /// <param name="checks">The ordered set of health checks to evaluate on each invocation.</param>
    internal RuntimeHealthService(IReadOnlyList<IHealthCheck> checks)
    {
        _checks = checks;
    }

    /// <summary>Runs all checks sequentially and returns a <see cref="RuntimeHealthReport"/>.</summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A report containing per-check results and the aggregated worst-case status.</returns>
    public async Task<RuntimeHealthReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ModuleHealthResult>(_checks.Count);
        var worst = HealthStatus.Healthy;

        for (int i = 0; i < _checks.Count; i++)
        {
            var checkName = $"check-{i}";
            HealthCheckResult result;

            try
            {
                result = await _checks[i].CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // catch broad exception to isolate check failures
            catch (Exception ex)
            {
                result = HealthCheckResult.Unhealthy($"Check '{checkName}' threw an unhandled exception.", ex);
            }
#pragma warning restore CA1031

            if (result.Status > worst)
            {
                worst = result.Status;
            }

            results.Add(new ModuleHealthResult(checkName, result));
        }

        return new RuntimeHealthReport(worst, results);
    }
}
