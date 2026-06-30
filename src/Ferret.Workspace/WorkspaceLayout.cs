namespace Ferret.Workspace;

/// <summary>Canonical file and directory names within a .ferret workspace root.</summary>
internal static class WorkspaceLayout
{
    /// <summary>The name of the workspace root directory.</summary>
    internal const string RootDirectoryName = ".ferret";

    /// <summary>The name of the workspace manifest file.</summary>
    internal const string ManifestFileName = "workspace.json";

    /// <summary>The name of the workspace state file.</summary>
    internal const string StateFileName = "state.json";

    /// <summary>The name of the configuration directory.</summary>
    internal const string ConfigDirectoryName = "config";

    /// <summary>The name of the cache directory.</summary>
    internal const string CacheDirectoryName = "cache";

    /// <summary>The name of the logs directory.</summary>
    internal const string LogsDirectoryName = "logs";

    /// <summary>The name of the plugins directory.</summary>
    internal const string PluginsDirectoryName = "plugins";

    /// <summary>The name of the connectors directory.</summary>
    internal const string ConnectorsDirectoryName = "connectors";

    /// <summary>The name of the indexes directory.</summary>
    internal const string IndexesDirectoryName = "indexes";

    /// <summary>The name of the knowledge directory.</summary>
    internal const string KnowledgeDirectoryName = "knowledge";

    /// <summary>The name of the memory directory.</summary>
    internal const string MemoryDirectoryName = "memory";

    /// <summary>The name of the artifacts directory.</summary>
    internal const string ArtifactsDirectoryName = "artifacts";

    /// <summary>The name of the models directory.</summary>
    internal const string ModelsDirectoryName = "models";

    /// <summary>The name of the snapshots directory.</summary>
    internal const string SnapshotsDirectoryName = "snapshots";

    /// <summary>The name of the telemetry directory.</summary>
    internal const string TelemetryDirectoryName = "telemetry";

    /// <summary>The name of the temp directory.</summary>
    internal const string TempDirectoryName = "temp";

    /// <summary>Flat ordered list — every path relative to .ferret/ root. Parents precede children.</summary>
    internal static readonly IReadOnlyList<string> AllDirectories =
    [
        ConfigDirectoryName,
        CacheDirectoryName,
        LogsDirectoryName,
        PluginsDirectoryName,
        ConnectorsDirectoryName,
        "connectors/git",
        "connectors/jira",
        "connectors/github",
        "connectors/azuredevops",
        "connectors/confluence",
        "connectors/filesystem",
        "connectors/logs",
        IndexesDirectoryName,
        "indexes/semantic",
        "indexes/keyword",
        "indexes/graph",
        KnowledgeDirectoryName,
        "knowledge/entities",
        "knowledge/relationships",
        "knowledge/documents",
        MemoryDirectoryName,
        "memory/working",
        "memory/episodic",
        "memory/longterm",
        ArtifactsDirectoryName,
        ModelsDirectoryName,
        "models/embeddings",
        "models/rerankers",
        "models/llms",
        SnapshotsDirectoryName,
        "snapshots/workspace",
        "snapshots/indexes",
        "snapshots/knowledge",
        TelemetryDirectoryName,
        "telemetry/metrics",
        "telemetry/events",
        "telemetry/diagnostics",
        TempDirectoryName,
    ];

    /// <summary>Empty JSON config files written to config/ on init.</summary>
    internal static readonly IReadOnlyList<string> ConfigFileNames =
    [
        "runtime.json",
        "plugins.json",
        "models.json",
        "connectors.json",
    ];
}
