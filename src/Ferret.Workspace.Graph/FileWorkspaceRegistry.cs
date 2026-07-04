using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ferret.Workspace.Graph;

/// <summary>
/// Default file-based implementation of <see cref="IWorkspaceRegistry"/> (ADR-0026). Layout is
/// one directory per workspace, <c>&lt;root&gt;/&lt;workspaceId&gt;/workspace.json</c> — a direct,
/// deterministic path for <see cref="ResolveAsync"/>, and a directory scan for
/// <see cref="ListAsync"/> (ADR-0026 "Registry Storage": sufficient at v1 scale, no separate index
/// file). Writes use the same atomic temp-file-then-rename pattern established in this codebase by
/// <c>Ferret.Persistence.FileDependencyStateStore</c> (ADR-0022), reused rather than reinvented.
/// Unlike that store, a corrupt entry is never auto-evicted — see <see cref="WorkspaceRegistryCorruptException"/>.
/// </summary>
public sealed class FileWorkspaceRegistry : IWorkspaceRegistry
{
    private const string ManifestFileName = "workspace.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _rootDirectory;

    /// <summary>Initializes a new instance of the <see cref="FileWorkspaceRegistry"/> class.</summary>
    /// <param name="rootDirectory">Absolute path to the directory under which each workspace's own subdirectory lives, e.g. <c>~/.ferret/workspaces</c>.</param>
    public FileWorkspaceRegistry(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = rootDirectory;
    }

    /// <inheritdoc/>
    public async Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var manifestPath = GetManifestPath(workspaceId);
        return await ReadManifestAsync(manifestPath, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return [];
        }

        var entries = new List<WorkspaceRegistryEntry>();
        foreach (var manifestPath in Directory.EnumerateFiles(_rootDirectory, ManifestFileName, SearchOption.AllDirectories))
        {
            var entry = await ReadManifestAsync(manifestPath, ct).ConfigureAwait(false);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var manifestPath = GetManifestPath(entry.WorkspaceId);
        var dir = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var envelope = ToEnvelope(entry);
        var tmpPath = manifestPath + ".tmp";
        var stream = File.Create(tmpPath);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, envelope, SerializerOptions, ct).ConfigureAwait(false);
        }

        File.Move(tmpPath, manifestPath, overwrite: true);
    }

    private static async Task<WorkspaceRegistryEntry?> ReadManifestAsync(string manifestPath, CancellationToken ct)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        JsonWorkspaceRegistryEntryEnvelope? envelope;
        try
        {
            var stream = File.OpenRead(manifestPath);
            await using (stream.ConfigureAwait(false))
            {
                envelope = await JsonSerializer.DeserializeAsync<JsonWorkspaceRegistryEntryEnvelope>(stream, SerializerOptions, ct).ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            // ADR-0026: fail closed with a clear message, never silently discard or auto-repair —
            // deliberately not evicted, unlike Ferret.Persistence.FileDependencyStateStore's
            // treatment of a corrupt cache record. A workspace registry entry is not recomputable.
            throw new WorkspaceRegistryCorruptException(manifestPath, "manifest content is not valid JSON for the expected shape", ex);
        }

        if (envelope is null)
        {
            throw new WorkspaceRegistryCorruptException(manifestPath, "manifest deserialized to an empty document");
        }

        return ToEntry(envelope);
    }

    private static WorkspaceRegistryEntry ToEntry(JsonWorkspaceRegistryEntryEnvelope envelope) => new()
    {
        WorkspaceId = envelope.WorkspaceId,
        Name = envelope.Name,
        SchemaVersion = envelope.SchemaVersion,
    };

    private static JsonWorkspaceRegistryEntryEnvelope ToEnvelope(WorkspaceRegistryEntry entry) => new()
    {
        SchemaVersion = entry.SchemaVersion,
        WorkspaceId = entry.WorkspaceId,
        Name = entry.Name,
    };

    private string GetManifestPath(Guid workspaceId) =>
        Path.Join(_rootDirectory, workspaceId.ToString("N"), ManifestFileName);

    /// <summary>
    /// On-disk shape of a <see cref="WorkspaceRegistryEntry"/>. Carries an explicit schema version
    /// so a future milestone (WIP-011 onward) can introduce version-gated reads without a
    /// wire-format redesign — the same convention <c>Ferret.Persistence.FileDependencyStateStore</c>
    /// already establishes in this codebase.
    /// </summary>
    private sealed class JsonWorkspaceRegistryEntryEnvelope
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = "1.0";

        [JsonPropertyName("workspaceId")]
        public Guid WorkspaceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
