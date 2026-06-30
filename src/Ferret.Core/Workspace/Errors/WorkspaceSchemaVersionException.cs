namespace Ferret.Core.Workspace.Errors;

/// <summary>Thrown when the workspace schema version is incompatible with the current platform version.</summary>
public sealed class WorkspaceSchemaVersionException : WorkspaceException
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class.</summary>
    public WorkspaceSchemaVersionException()
        : base("The workspace schema version is incompatible.")
    {
        WorkspaceId = string.Empty;
        SchemaVersion = string.Empty;
        RequiredVersion = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class with a message.</summary>
    /// <param name="message">A message describing the schema version incompatibility.</param>
    public WorkspaceSchemaVersionException(string message)
        : base(message)
    {
        WorkspaceId = string.Empty;
        SchemaVersion = string.Empty;
        RequiredVersion = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the schema version incompatibility.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public WorkspaceSchemaVersionException(string message, Exception innerException)
        : base(message, innerException)
    {
        WorkspaceId = string.Empty;
        SchemaVersion = string.Empty;
        RequiredVersion = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="WorkspaceSchemaVersionException"/> class with workspace and version details.</summary>
    /// <param name="workspaceId">The identifier of the workspace with the incompatible schema.</param>
    /// <param name="schemaVersion">The schema version found in the workspace.</param>
    /// <param name="requiredVersion">The schema version required by the platform.</param>
    public WorkspaceSchemaVersionException(string workspaceId, string schemaVersion, string requiredVersion)
        : base($"Workspace '{workspaceId}' has schema version '{schemaVersion}' but version '{requiredVersion}' is required.")
    {
        WorkspaceId = workspaceId;
        SchemaVersion = schemaVersion;
        RequiredVersion = requiredVersion;
    }

    /// <summary>Gets the identifier of the workspace.</summary>
    public string WorkspaceId { get; }

    /// <summary>Gets the schema version found in the workspace.</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the schema version required by the platform.</summary>
    public string RequiredVersion { get; }
}
