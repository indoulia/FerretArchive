using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.ConnectorPlatform;

/// <summary>
/// Persists <see cref="ConnectorInstance"/> records to <c>.ferret/connectors.json</c>.
/// Writes are atomic: content is written to a temp file, then renamed over the target.
/// When the loaded schema version differs from the current version, the existing file
/// is backed up as <c>connectors.json.bak.{timestamp}</c> before overwriting.
/// </summary>
public sealed class ConnectorInstanceStore : IConnectorInstanceStore
{
    private const string CurrentSchemaVersion = "1.0";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConnectorInstance>> LoadAllAsync(
        WorkspacePath rootPath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        var filePath = GetFilePath(rootPath);
        if (!File.Exists(filePath))
        {
            return [];
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Failed to read connectors.json at '{filePath}'.", ex);
        }

        JsonConnectorsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<JsonConnectorsFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse connectors.json at '{filePath}'. The file may be corrupt.", ex);
        }

        if (file is null)
        {
            return [];
        }

        return file.Instances.Select(ToInstance).ToList();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        WorkspacePath rootPath,
        IReadOnlyList<ConnectorInstance> instances,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rootPath);
        ArgumentNullException.ThrowIfNull(instances);
        var filePath = GetFilePath(rootPath);
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(filePath))
        {
            await BackupIfNeededAsync(filePath, ct).ConfigureAwait(false);
        }

        var file = new JsonConnectorsFile
        {
            SchemaVersion = CurrentSchemaVersion,
            Instances = instances.Select(ToJson).ToList(),
        };

        var json = JsonSerializer.Serialize(file, JsonOptions);
        var tmpPath = filePath + ".tmp";

        await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);
        File.Move(tmpPath, filePath, overwrite: true);
    }

    private static string GetFilePath(WorkspacePath rootPath) =>
        Path.Join(rootPath.FullPath, ".ferret", "connectors.json");

    private static async Task BackupIfNeededAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var existing = JsonSerializer.Deserialize<JsonConnectorsFile>(json, JsonOptions);
            if (existing?.SchemaVersion != CurrentSchemaVersion)
            {
                var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var backupPath = filePath + $".bak.{timestamp}";
                File.Copy(filePath, backupPath, overwrite: false);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception)
#pragma warning restore CA1031
        {
            // Backup is best-effort — never block a save due to backup failure
        }
    }

    private static ConnectorInstance ToInstance(JsonConnectorInstance j) =>
        new()
        {
            Id = new ConnectorInstanceId(j.Id),
            ConnectorType = new ConnectorId(j.ConnectorType),
            DisplayName = j.DisplayName,
            IsEnabled = j.Enabled,
            SchemaVersion = j.SchemaVersion ?? CurrentSchemaVersion,
            Tags = j.Tags ?? [],
            Configuration = j.Configuration is null
                ? ConnectorConfiguration.Empty
                : ConnectorConfiguration.FromDictionary(j.Configuration),
        };

    private static JsonConnectorInstance ToJson(ConnectorInstance i) =>
        new()
        {
            Id = i.Id.Value,
            ConnectorType = i.ConnectorType.Value,
            DisplayName = i.DisplayName,
            Enabled = i.IsEnabled,
            SchemaVersion = i.SchemaVersion,
            Tags = i.Tags.Count > 0 ? i.Tags.ToList() : null,
            Configuration = i.Configuration.AsReadOnlyDictionary().Count > 0
                ? new Dictionary<string, string>(i.Configuration.AsReadOnlyDictionary())
                : null,
        };

    private sealed class JsonConnectorsFile
    {
        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        /// Gets or sets the list of connector instances.
        /// </summary>
        [JsonPropertyName("instances")]
        public List<JsonConnectorInstance> Instances { get; set; } = [];
    }

    private sealed class JsonConnectorInstance
    {
        /// <summary>
        /// Gets or sets the instance identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the connector type identifier.
        /// </summary>
        [JsonPropertyName("connectorType")]
        public string ConnectorType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        [JsonPropertyName("schemaVersion")]
        public string? SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the instance is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        [JsonPropertyName("configuration")]
        public Dictionary<string, string>? Configuration { get; set; }
    }
}
