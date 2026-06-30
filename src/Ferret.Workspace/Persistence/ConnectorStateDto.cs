using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

/// <summary>Serialization model for per-connector state in state.json.</summary>
internal sealed class ConnectorStateDto
{
    /// <summary>Gets or sets a value indicating whether the connector is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the timestamp of the last successful sync.</summary>
    [JsonPropertyName("lastSyncAt")]
    public DateTimeOffset? LastSyncAt { get; set; }
}
