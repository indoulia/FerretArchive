namespace Ferret.Workspace.Graph;

/// <summary>
/// Thrown when a local path given to <see cref="RepoIdentityResolver"/> cannot be resolved to a
/// repo identity — the path doesn't exist, or it isn't a git repository. Mirrors
/// <see cref="WorkspaceRegistryCorruptException"/>'s shape: a clear, actionable message naming the
/// path and the reason, not a generic exception.
/// </summary>
public sealed class RepoIdentityResolutionException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RepoIdentityResolutionException"/> class.</summary>
    public RepoIdentityResolutionException()
        : this("(unknown)", "no further detail provided", null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RepoIdentityResolutionException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    public RepoIdentityResolutionException(string message)
        : base(message)
    {
        RepoPath = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="RepoIdentityResolutionException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public RepoIdentityResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
        RepoPath = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="RepoIdentityResolutionException"/> class.</summary>
    /// <param name="repoPath">The path that could not be resolved.</param>
    /// <param name="reason">A human-readable description of why.</param>
    public RepoIdentityResolutionException(string repoPath, string reason)
        : this(repoPath, reason, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RepoIdentityResolutionException"/> class.</summary>
    /// <param name="repoPath">The path that could not be resolved.</param>
    /// <param name="reason">A human-readable description of why.</param>
    /// <param name="cause">The underlying exception, if any.</param>
    public RepoIdentityResolutionException(string repoPath, string reason, Exception? cause)
        : base($"Cannot resolve a repo identity for '{repoPath}': {reason}", cause)
    {
        RepoPath = repoPath;
    }

    /// <summary>Gets the path that could not be resolved.</summary>
    public string RepoPath { get; }
}
