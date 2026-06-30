namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when a workspace must be upgraded before it can be used.</summary>
public sealed class WorkspaceUpgradeRequiredException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeRequiredException"/> class.</summary>
    public WorkspaceUpgradeRequiredException()
        : base("The workspace must be upgraded before use.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeRequiredException"/> class for a specific workspace.</summary>
    /// <param name="workspaceId">The identifier of the workspace that requires upgrading.</param>
    public WorkspaceUpgradeRequiredException(string workspaceId)
        : base($"Workspace '{workspaceId}' must be upgraded before use.")
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeRequiredException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceUpgradeRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Gets the identifier of the workspace that requires upgrading.</summary>
    public string WorkspaceId { get; }
}
