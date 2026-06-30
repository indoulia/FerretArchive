using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

/// <summary>Serialization model for state.json — the ContextOS workspace state.</summary>
internal sealed class WorkspaceStateDto
{
    /// <summary>Gets or sets the knowledge graph version counter.</summary>
    [JsonPropertyName("knowledgeVersion")]
    public int KnowledgeVersion { get; set; }

    /// <summary>Gets or sets the graph version counter.</summary>
    [JsonPropertyName("graphVersion")]
    public int GraphVersion { get; set; }

    /// <summary>Gets or sets the timestamp of the last index operation.</summary>
    [JsonPropertyName("lastIndex")]
    public DateTimeOffset? LastIndex { get; set; }

    /// <summary>Gets or sets the per-connector state dictionary.</summary>
    [JsonPropertyName("connectors")]
    public Dictionary<string, ConnectorStateDto>? Connectors { get; set; }

    /// <summary>Gets or sets the workspace statistics.</summary>
    [JsonPropertyName("statistics")]
    public StatisticsDto Statistics { get; set; } = new();
}
