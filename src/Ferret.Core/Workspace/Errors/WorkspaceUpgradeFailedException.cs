namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when a workspace upgrade attempt fails.</summary>
public sealed class WorkspaceUpgradeFailedException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeFailedException"/> class.</summary>
    public WorkspaceUpgradeFailedException()
        : base("Workspace upgrade failed.")
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeFailedException"/> class with a message.</summary>
    /// <param name="message">A message describing the upgrade failure.</param>
    public WorkspaceUpgradeFailedException(string message)
        : base(message)
    {
        WorkspaceId = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceUpgradeFailedException"/> class with workspace and inner exception.</summary>
    /// <param name="workspaceId">The identifier of the workspace whose upgrade failed.</param>
    /// <param name="innerException">The exception that caused the upgrade to fail.</param>
    public WorkspaceUpgradeFailedException(string workspaceId, Exception innerException)
        : base($"Upgrade failed for workspace '{workspaceId}'.", innerException)
    {
        WorkspaceId = workspaceId;
    }

    /// <summary>Gets the identifier of the workspace whose upgrade failed.</summary>
    public string WorkspaceId { get; }
}
