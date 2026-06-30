namespace Ferret.Core.Indexing;

/// <summary>Conventional paths within the <c>.ferret</c> index directory.
/// Mirrors <see cref="Ferret.Core.Workspace.WorkspaceLayout"/> for the index subsystem.
/// Used by the CLI host (S5) to build the SQLite database path and by
/// <c>IndexCommandHandler</c> to display the resolved path in command output.</summary>
public static class IndexLayout
{
    /// <summary>Subdirectory containing all index databases. Relative to <c>.ferret/</c>.</summary>
    public const string IndexDirectoryName = "indexes";

    /// <summary>Subdirectory for keyword (FTS5) index. Relative to <c>.ferret/indexes/</c>.</summary>
    public const string KeywordDirectoryName = "keyword";

    /// <summary>Filename of the keyword FTS5 database.</summary>
    public const string KeywordDatabaseFileName = "keyword-index.db";

    /// <summary>Filename of the incremental-indexing state file. Stored directly under <c>.ferret/</c>.</summary>
    public const string StateFileName = "index-state.json";

    // Reserved: VectorDirectoryName = "vector"
    // Reserved: AnalyticsDirectoryName = "analytics"
    // Reserved: CacheDirectoryName = "cache"
}
