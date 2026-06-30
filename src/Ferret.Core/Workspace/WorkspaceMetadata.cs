namespace Ferret.Core.Workspace;

/// <summary>Descriptive metadata about a workspace.</summary>
public sealed class WorkspaceMetadata
{
    private WorkspaceMetadata(string name, string description, string schemaVersion, DateTimeOffset createdAt, DateTimeOffset? lastIndexedAt)
    {
        Name = name;
        Description = description;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        LastIndexedAt = lastIndexedAt;
    }

    /// <summary>Gets the human-readable workspace name.</summary>
    public string Name { get; }

    /// <summary>Gets the workspace description.</summary>
    public string Description { get; }

    /// <summary>Gets the workspace configuration schema version (e.g. "1.0").</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the UTC timestamp when the workspace was first initialised.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the UTC timestamp of the last successful index build, or <see langword="null"/> if never indexed.</summary>
    public DateTimeOffset? LastIndexedAt { get; }

    /// <summary>Creates a new <see cref="WorkspaceMetadata"/> instance.</summary>
    /// <param name="name">The workspace name.</param>
    /// <param name="description">The workspace description.</param>
    /// <param name="schemaVersion">The schema version string.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="lastIndexedAt">The last index timestamp, or <see langword="null"/>.</param>
    /// <returns>A new <see cref="WorkspaceMetadata"/> instance.</returns>
    public static WorkspaceMetadata Create(string name, string description, string schemaVersion, DateTimeOffset createdAt, DateTimeOffset? lastIndexedAt = null)
    {
        return new WorkspaceMetadata(
            name ?? string.Empty,
            description ?? string.Empty,
            schemaVersion ?? string.Empty,
            createdAt,
            lastIndexedAt);
    }
}
