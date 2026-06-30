namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when a workspace cannot be found by its identifier or path.</summary>
public sealed class WorkspaceNotFoundException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotFoundException"/> class.</summary>
    public WorkspaceNotFoundException()
        : base("The workspace was not found.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotFoundException"/> class for a specific workspace identifier.</summary>
    /// <param name="workspaceId">The identifier of the workspace that could not be found.</param>
    public WorkspaceNotFoundException(string workspaceId)
        : base($"Workspace '{workspaceId}' was not found.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotFoundException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Gets the identifier of the workspace that could not be found.</summary>
    public string WorkspaceId { get; }
}
