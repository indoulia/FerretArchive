namespace Ferret.Core.Workspace;

/// <summary>Detects files changed since the last successful index operation.</summary>
public interface IWorkspaceChangeDetector
{
    /// <summary>Computes the set of changes since the last index for the given workspace context.</summary>
    /// <param name="context">The open workspace context.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that resolves to a <see cref="Changeset"/>.</returns>
    Task<Changeset> DetectChangesAsync(WorkspaceContext context, CancellationToken ct = default);
}
