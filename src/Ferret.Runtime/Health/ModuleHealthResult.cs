using Ferret.Core.Abstractions;

namespace Ferret.Runtime.Health;

/// <summary>
/// Pairs a named check entry with its <see cref="HealthCheckResult"/>.
/// <para>Why: Carries both the check identity and outcome so callers can correlate failures to their source without inspecting the raw result alone.</para>
/// <para>Lifecycle: Created by <see cref="RuntimeHealthService"/> per check invocation; discarded after the owning <see cref="RuntimeHealthReport"/> is consumed.</para>
/// <para>Layer: Ferret.Runtime internal — produced by RuntimeHealthService and exposed only through RuntimeHealthReport.</para>
/// <para>Thread Safety: Immutable — safe to share across threads after construction.</para>
/// </summary>
public sealed class ModuleHealthResult
{
    /// <summary>Initializes a new instance of the <see cref="ModuleHealthResult"/> class.</summary>
    /// <param name="checkName">A human-readable identifier for the health check.</param>
    /// <param name="result">The outcome of the health check.</param>
    public ModuleHealthResult(string checkName, HealthCheckResult result)
    {
        CheckName = checkName;
        Result = result;
    }

    /// <summary>Gets the human-readable identifier for this health check.</summary>
    public string CheckName { get; }

    /// <summary>Gets the outcome of the health check.</summary>
    public HealthCheckResult Result { get; }
}
