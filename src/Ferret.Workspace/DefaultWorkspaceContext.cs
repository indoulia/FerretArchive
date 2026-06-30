using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

namespace Ferret.Workspace;

/// <summary>
/// Default implementation of <see cref="IWorkspaceContext"/>.
/// Built once by the CLI composition root from CWD + workspace.json manifest.
/// Registered as a singleton so all commands share the same workspace identity.
/// </summary>
public sealed class DefaultWorkspaceContext : IWorkspaceContext
{
    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspaceContext"/> class.</summary>
    /// <param name="workspaceId">The workspace identifier read from workspace.json.</param>
    /// <param name="workspaceRoot">The workspace root path (CWD at startup).</param>
    public DefaultWorkspaceContext(WorkspaceId workspaceId, WorkspacePath workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(workspaceRoot);

        WorkspaceId = workspaceId;
        WorkspaceRoot = workspaceRoot;
    }

    /// <inheritdoc/>
    public WorkspaceId WorkspaceId { get; }

    /// <inheritdoc/>
    public WorkspacePath WorkspaceRoot { get; }
}
