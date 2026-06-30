using System.Text.Json.Serialization;

namespace Ferret.Workspace.Persistence;

/// <summary>Serialization model for workspace.json — the ContextOS workspace manifest.</summary>
internal sealed class WorkspaceManifest
{
    /// <summary>Gets or sets the workspace identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspace name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspace description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema version.</summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>Gets or sets the Ferret platform version that created this workspace.</summary>
    [JsonPropertyName("ferretVersion")]
    public string FerretVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the ContextOS version.</summary>
    [JsonPropertyName("contextOsVersion")]
    public string ContextOsVersion { get; set; } = "1.0";

    /// <summary>Gets or sets the creation timestamp.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the workspace type (e.g. "repository").</summary>
    [JsonPropertyName("workspaceType")]
    public string WorkspaceType { get; set; } = "repository";

    /// <summary>Gets or sets the enabled feature flags.</summary>
    [JsonPropertyName("features")]
    public Dictionary<string, bool>? Features { get; set; }

    /// <summary>Gets or sets the list of enabled connector identifiers.</summary>
    [JsonPropertyName("enabledConnectors")]
    public List<string>? EnabledConnectors { get; set; }

    /// <summary>Gets or sets the list of enabled model identifiers.</summary>
    [JsonPropertyName("enabledModels")]
    public List<string>? EnabledModels { get; set; }
}
