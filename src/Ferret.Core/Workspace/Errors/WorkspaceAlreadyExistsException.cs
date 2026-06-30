namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when an attempt is made to create a workspace that already exists.</summary>
public sealed class WorkspaceAlreadyExistsException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceAlreadyExistsException"/> class.</summary>
    public WorkspaceAlreadyExistsException()
        : base("The workspace already exists.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAlreadyExistsException"/> class for a specific workspace identifier.</summary>
    /// <param name="workspaceId">The identifier of the workspace that already exists.</param>
    public WorkspaceAlreadyExistsException(string workspaceId)
        : base($"Workspace '{workspaceId}' already exists.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAlreadyExistsException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Gets the identifier of the workspace that already exists.</summary>
    public string WorkspaceId { get; }
}
