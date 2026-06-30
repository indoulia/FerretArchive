using Ferret.Core.Primitives;

namespace Ferret.Core.Workspace;

/// <summary>Represents an open workspace — the root path, identity, metadata, and runtime capabilities.</summary>
public sealed class WorkspaceContext
{
    private WorkspaceContext(WorkspacePath rootPath, WorkspaceId id, WorkspaceMetadata metadata, WorkspaceCapabilities capabilities)
    {
        RootPath = rootPath;
        Id = id;
        Metadata = metadata;
        Capabilities = capabilities;
    }

    /// <summary>Gets the absolute path to the workspace root directory.</summary>
    public WorkspacePath RootPath { get; }

    /// <summary>Gets the unique workspace identifier.</summary>
    public WorkspaceId Id { get; }

    /// <summary>Gets the workspace metadata.</summary>
    public WorkspaceMetadata Metadata { get; }

    /// <summary>Gets the runtime capabilities of this workspace.</summary>
    public WorkspaceCapabilities Capabilities { get; }

    /// <summary>Creates a new <see cref="WorkspaceContext"/>.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="id">The workspace identifier.</param>
    /// <param name="metadata">The workspace metadata.</param>
    /// <param name="capabilities">The workspace runtime capabilities.</param>
    /// <returns>A new <see cref="WorkspaceContext"/> instance.</returns>
    public static WorkspaceContext Create(WorkspacePath rootPath, WorkspaceId id, WorkspaceMetadata metadata, WorkspaceCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(capabilities);

        return new WorkspaceContext(rootPath, id, metadata, capabilities);
    }
}
