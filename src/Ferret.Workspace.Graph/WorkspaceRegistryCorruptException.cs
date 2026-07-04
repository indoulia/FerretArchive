namespace Ferret.Workspace.Graph;

/// <summary>
/// Thrown when a workspace registry entry exists on disk but cannot be read as a valid entry
/// (ADR-0026 "Registry Storage" — fail closed with a clear message naming the file and the reason,
/// never silently discard or auto-repair). Unlike <c>Ferret.Persistence.FileDependencyStateStore</c>'s
/// treatment of a corrupt cache record (evicted automatically, since it is cheap to recompute), a
/// workspace registry entry represents real, non-recomputable user configuration (workspace
/// membership, later references and sharing) — so this is a loud failure, not a silent one.
/// </summary>
public sealed class WorkspaceRegistryCorruptException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceRegistryCorruptException"/> class.</summary>
    public WorkspaceRegistryCorruptException()
        : this("(unknown)", "no further detail provided", null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceRegistryCorruptException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    public WorkspaceRegistryCorruptException(string message)
        : base(message)
    {
        FilePath = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceRegistryCorruptException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public WorkspaceRegistryCorruptException(string message, Exception innerException)
        : base(message, innerException)
    {
        FilePath = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceRegistryCorruptException"/> class.</summary>
    /// <param name="filePath">The path of the unreadable registry entry file.</param>
    /// <param name="reason">A human-readable description of why the file could not be read.</param>
    public WorkspaceRegistryCorruptException(string filePath, string reason)
        : this(filePath, reason, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceRegistryCorruptException"/> class.</summary>
    /// <param name="filePath">The path of the unreadable registry entry file.</param>
    /// <param name="reason">A human-readable description of why the file could not be read.</param>
    /// <param name="cause">The underlying exception, if any.</param>
    public WorkspaceRegistryCorruptException(string filePath, string reason, Exception? cause)
        : base($"Workspace registry entry at '{filePath}' is corrupt: {reason}", cause)
    {
        FilePath = filePath;
    }

    /// <summary>Gets the path of the unreadable registry entry file.</summary>
    public string FilePath { get; }
}
