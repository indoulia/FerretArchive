namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when an operation attempts to access a path outside the workspace root.</summary>
public sealed class WorkspacePathTraversalException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspacePathTraversalException"/> class.</summary>
    public WorkspacePathTraversalException()
        : base("A path traversal attempt was detected.")
    {
        AttemptedPath = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspacePathTraversalException"/> class for a specific attempted path.</summary>
    /// <param name="attemptedPath">The path string that was attempted.</param>
    public WorkspacePathTraversalException(string attemptedPath)
        : base($"Path traversal attempt detected: '{attemptedPath}' is outside the workspace root.")
    {
        AttemptedPath = attemptedPath;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspacePathTraversalException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the path traversal attempt.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspacePathTraversalException(string message, Exception innerException)
        : base(message, innerException)
    {
        AttemptedPath = string.Empty;
    }

    /// <summary>Gets the path string that was attempted.</summary>
    public string AttemptedPath { get; }
}
