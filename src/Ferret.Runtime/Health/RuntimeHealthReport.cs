using Ferret.Core.Enumerations;

namespace Ferret.Runtime.Health;

/// <summary>
/// Aggregates all per-module health results into a single host-level report.
/// <para>Why: Provides a unified view of platform health so operators and diagnostics tooling can determine overall status without inspecting individual checks.</para>
/// <para>Lifecycle: Created by <see cref="RuntimeHealthService.CheckAsync"/> on each invocation; discarded after consumption by the caller.</para>
/// <para>Layer: Ferret.Runtime internal — produced by RuntimeHealthService and surfaced through the health endpoint or diagnostics pipeline.</para>
/// <para>Thread Safety: Immutable — safe to share across threads after construction.</para>
/// </summary>
public sealed class RuntimeHealthReport
{
    /// <summary>Initializes a new instance of the <see cref="RuntimeHealthReport"/> class.</summary>
    /// <param name="overallStatus">The worst <see cref="HealthStatus"/> across all checks.</param>
    /// <param name="results">The ordered list of per-module health results.</param>
    public RuntimeHealthReport(HealthStatus overallStatus, IReadOnlyList<ModuleHealthResult> results)
    {
        OverallStatus = overallStatus;
        Results = results;
    }

    /// <summary>Gets the worst <see cref="HealthStatus"/> observed across all registered checks.</summary>
    public HealthStatus OverallStatus { get; }

    /// <summary>Gets the ordered list of per-module health results produced during the last check run.</summary>
    public IReadOnlyList<ModuleHealthResult> Results { get; }
}
