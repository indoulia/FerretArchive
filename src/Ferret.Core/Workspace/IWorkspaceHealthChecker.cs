using Ferret.Core.Abstractions;

namespace Ferret.Core.Workspace;

/// <summary>Performs a single named health check against an open workspace.</summary>
public interface IWorkspaceHealthChecker
{
    /// <summary>Gets the unique name of this health checker.</summary>
    string Name { get; }

    /// <summary>Gets the minimum check depth at which this checker runs.</summary>
    HealthCheckDepth Depth { get; }

    /// <summary>Runs the health check against the given workspace context.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="HealthCheckResult"/>.</returns>
    Task<HealthCheckResult> CheckAsync(WorkspaceContext context, CancellationToken ct = default);
}
