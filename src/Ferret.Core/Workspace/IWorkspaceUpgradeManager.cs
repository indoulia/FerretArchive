namespace Ferret.Core.Workspace;

/// <summary>Manages workspace schema upgrades between platform versions.</summary>
public interface IWorkspaceUpgradeManager
{
    /// <summary>Returns <see langword="true"/> if the workspace at <paramref name="context"/> requires a schema upgrade.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a boolean value indicating whether an upgrade is required.</returns>
    Task<bool> IsUpgradeRequiredAsync(WorkspaceContext context, CancellationToken ct = default);

    /// <summary>Applies any pending schema migration steps and returns the upgrade result.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="WorkspaceUpgradeResult"/>.</returns>
    Task<WorkspaceUpgradeResult> UpgradeAsync(WorkspaceContext context, CancellationToken ct = default);
}
