using Ferret.Core.Primitives;

namespace Ferret.Core.Workspace;

/// <summary>Provides workspace context to subsystems that need root path, ID, and layout.
/// Registered as a singleton in the CLI composition root. All commands and modules that
/// need workspace location consume this interface — never call <c>Directory.GetCurrentDirectory()</c>
/// or read <c>workspace.json</c> directly.</summary>
public interface IWorkspaceContext
{
    /// <summary>Gets the workspace unique identifier.</summary>
    WorkspaceId WorkspaceId { get; }

    /// <summary>Gets the workspace root path.</summary>
    WorkspacePath WorkspaceRoot { get; }

    // Reserved: WorkspaceMetadata Metadata { get; }
}
