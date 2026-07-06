namespace Ferret.Cli.Commands.Workspace;

/// <summary>
/// Zero-action wrapping of an existing single-repo checkout into a <c>kind: "personal"</c> workspace
/// registry entry (WIP-013, <c>14-Migration.md</c>).
/// </summary>
internal interface IWorkspaceRegistryAutoMigrator
{
    /// <summary>
    /// Ensures the repo at <paramref name="repoPath"/> has a workspace registry entry, creating a
    /// <c>personal</c> entry with this repo as its sole member if none exists yet. Never throws and
    /// never blocks the calling command — any failure (unresolvable identity, unreadable or
    /// unwritable registry) is swallowed and logged, per <c>14-Migration.md</c> §3's fail-open rule.
    /// </summary>
    /// <param name="repoPath">Absolute path to the repo's root directory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when migration has been attempted (successfully or not).</returns>
    Task EnsureMigratedAsync(string repoPath, CancellationToken ct = default);
}
