using Ferret.Core.Results;

namespace Ferret.Core.Workspace;

/// <summary>Provides the primary workspace lifecycle operations: initialise, load, health-check, upgrade, validate, and change-detect.</summary>
public interface IWorkspaceEngine
{
    /// <summary>Initialises a new workspace at the given root path.</summary>
    /// <param name="rootPath">The directory to initialise as a workspace.</param>
    /// <param name="options">Optional options for the initialisation.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="WorkspaceInitResult"/>.</returns>
    Task<WorkspaceInitResult> InitialiseAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default);

    /// <summary>Loads an existing workspace from the given root path.</summary>
    /// <param name="rootPath">The workspace root directory.</param>
    /// <param name="options">Optional options for the load.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="WorkspaceContext"/>.</returns>
    Task<WorkspaceContext> LoadAsync(WorkspacePath rootPath, WorkspaceOptions? options = null, CancellationToken ct = default);

    /// <summary>Runs a health check against the given workspace context.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="depth">The depth of the health check.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="WorkspaceHealthReport"/>.</returns>
    Task<WorkspaceHealthReport> GetHealthAsync(WorkspaceContext context, HealthCheckDepth depth = HealthCheckDepth.Quick, CancellationToken ct = default);

    /// <summary>Detects files changed since the last index operation.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="Changeset"/>.</returns>
    Task<Changeset> GetChangesetAsync(WorkspaceContext context, CancellationToken ct = default);

    /// <summary>Upgrades the workspace schema to the current platform version.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="WorkspaceUpgradeResult"/>.</returns>
    Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext context, CancellationToken ct = default);

    /// <summary>Validates the workspace configuration and structure.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="ValidationResult"/>.</returns>
    Task<ValidationResult> ValidateAsync(WorkspaceContext context, CancellationToken ct = default);
}
