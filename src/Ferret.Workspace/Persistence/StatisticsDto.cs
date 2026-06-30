using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

/// <summary>Serialization model for the statistics sub-object in state.json.</summary>
internal sealed class StatisticsDto
{
    /// <summary>Gets or sets the total number of files in the workspace.</summary>
    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; set; }

    /// <summary>Gets or sets the number of indexed files.</summary>
    [JsonPropertyName("indexedFiles")]
    public int IndexedFiles { get; set; }

    /// <summary>Gets or sets the timestamp of the last index operation.</summary>
    [JsonPropertyName("lastIndexedAt")]
    public DateTimeOffset? LastIndexedAt { get; set; }

    /// <summary>Gets or sets the schema version.</summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";
}
