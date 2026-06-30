namespace Ferret.Core.Workspace;

/// <summary>Quantitative statistics about a workspace's index and file state.</summary>
public sealed class WorkspaceStatistics
{
    private WorkspaceStatistics(int totalFiles, int indexedFiles, DateTimeOffset lastIndexed, string schemaVersion)
    {
        TotalFiles = totalFiles;
        IndexedFiles = indexedFiles;
        LastIndexed = lastIndexed;
        SchemaVersion = schemaVersion;
    }

    /// <summary>Gets the total number of files in the workspace.</summary>
    public int TotalFiles { get; }

    /// <summary>Gets the number of files currently in the index.</summary>
    public int IndexedFiles { get; }

    /// <summary>Gets the UTC timestamp of the last successful index operation.</summary>
    public DateTimeOffset LastIndexed { get; }

    /// <summary>Gets the workspace schema version at the time these statistics were recorded.</summary>
    public string SchemaVersion { get; }

    /// <summary>Creates a new <see cref="WorkspaceStatistics"/> instance.</summary>
    /// <param name="totalFiles">Total file count.</param>
    /// <param name="indexedFiles">Indexed file count.</param>
    /// <param name="lastIndexed">Last index timestamp.</param>
    /// <param name="schemaVersion">Schema version string.</param>
    /// <returns>A new <see cref="WorkspaceStatistics"/> instance.</returns>
    public static WorkspaceStatistics Create(int totalFiles, int indexedFiles, DateTimeOffset lastIndexed, string schemaVersion)
    {
        return new WorkspaceStatistics(totalFiles, indexedFiles, lastIndexed, schemaVersion ?? string.Empty);
    }
}
