using Ferret.Core.Abstractions;
using Ferret.Core.Enumerations;

namespace Ferret.Core.Workspace;

/// <summary>The result of a workspace health check, containing an overall status and per-checker results.</summary>
public sealed class WorkspaceHealthReport
{
    private WorkspaceHealthReport(WorkspaceContext context, HealthCheckDepth depth, HealthStatus overall, IReadOnlyList<HealthCheckResult> checks, DateTimeOffset checkedAt)
    {
        Context = context;
        Depth = depth;
        Overall = overall;
        Checks = checks;
        CheckedAt = checkedAt;
    }

    /// <summary>Gets the workspace context that was checked.</summary>
    public WorkspaceContext Context { get; }

    /// <summary>Gets the depth at which the health check was performed.</summary>
    public HealthCheckDepth Depth { get; }

    /// <summary>Gets the overall health status — the worst status across all individual checks.</summary>
    public HealthStatus Overall { get; }

    /// <summary>Gets the individual health check results.</summary>
    public IReadOnlyList<HealthCheckResult> Checks { get; }

    /// <summary>Gets the UTC timestamp when the health check was performed.</summary>
    public DateTimeOffset CheckedAt { get; }

    /// <summary>Creates a new <see cref="WorkspaceHealthReport"/>.</summary>
    /// <param name="context">The workspace context that was checked.</param>
    /// <param name="depth">The depth of the check.</param>
    /// <param name="overall">The overall health status.</param>
    /// <param name="checks">The individual check results.</param>
    /// <param name="checkedAt">The time the check was performed.</param>
    /// <returns>A new <see cref="WorkspaceHealthReport"/> instance.</returns>
    public static WorkspaceHealthReport Create(WorkspaceContext context, HealthCheckDepth depth, HealthStatus overall, IEnumerable<HealthCheckResult> checks, DateTimeOffset checkedAt)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(checks);

        return new WorkspaceHealthReport(context, depth, overall, checks.ToList().AsReadOnly(), checkedAt);
    }
}
