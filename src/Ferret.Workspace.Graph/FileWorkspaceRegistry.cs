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
/// WIP-011 adds one more fail-closed gate on top of WIP-010's malformed-JSON handling: a manifest
/// whose <c>schemaVersion</c> is not <see cref="SupportedSchemaVersion"/> is not reachable through
/// any declared migration path (ARCH-001 §12.4) — none exists yet, since v1.0 is the only schema
/// version that has ever shipped — so it fails the same way a malformed manifest does, rather than
/// being silently accepted or guessed at.
/// </summary>
public sealed class FileWorkspaceRegistry : IWorkspaceRegistry
{
    /// <summary>The default schema version, written when <see cref="WorkspaceRegistryEntry.References"/> is empty.</summary>
    public const string SupportedSchemaVersion = "1.0";

    /// <summary>The schema version written when <see cref="WorkspaceRegistryEntry.References"/> is non-empty (WIP-SLICE-2, additive per ARCH-001 §12.4 — no separate migration code, since the envelope already tolerates the extra field).</summary>
    public const string ReferencesSchemaVersion = "1.1";

    private const string ManifestFileName = "workspace.json";

    private static readonly HashSet<string> ReachableSchemaVersions = [SupportedSchemaVersion, ReferencesSchemaVersion];

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

    /// <inheritdoc/>
    public Task RemoveAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(GetManifestPath(workspaceId));
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        return Task.CompletedTask;
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

        if (!ReachableSchemaVersions.Contains(envelope.SchemaVersion))
        {
            // ARCH-001 §12.4: "Validates that the current schema version is reachable ... through
            // the declared migration path." No migration path is declared to or from any version
            // other than the ones in ReachableSchemaVersions yet, so nothing else is reachable —
            // fail closed, same disposition as malformed JSON, per ADR-0026.
            throw new WorkspaceRegistryCorruptException(
                manifestPath,
                $"schemaVersion '{envelope.SchemaVersion}' is not reachable — this reader only supports '{string.Join("', '", ReachableSchemaVersions)}' and no other migration path is declared");
        }

        return ToEntry(envelope);
    }

    private static WorkspaceRegistryEntry ToEntry(JsonWorkspaceRegistryEntryEnvelope envelope) => new()
    {
        WorkspaceId = envelope.WorkspaceId,
        Name = envelope.Name,
        SchemaVersion = envelope.SchemaVersion,
        Kind = envelope.Kind,
        Members = new WorkspaceMembers
        {
            Repos = envelope.Members.Repos
                .Select(r => new RepoMember { Remote = r.Remote, LocalPath = r.LocalPath })
                .ToList(),
            Documents = envelope.Members.Documents
                .Select(d => new DocumentMember { Path = d.Path, Type = d.Type })
                .ToList(),
        },
        References = (envelope.References ?? [])
            .Select(r => new WorkspaceReference { WorkspaceId = r.WorkspaceId, Mode = r.Mode, PinnedStateHash = r.PinnedStateHash })
            .ToList(),
    };

    private static JsonWorkspaceRegistryEntryEnvelope ToEnvelope(WorkspaceRegistryEntry entry) => new()
    {
        SchemaVersion = entry.SchemaVersion,
        WorkspaceId = entry.WorkspaceId,
        Name = entry.Name,
        Kind = entry.Kind,
        Members = new JsonWorkspaceMembers
        {
            Repos = entry.Members.Repos
                .Select(r => new JsonRepoMember { Remote = r.Remote, LocalPath = r.LocalPath })
                .ToList(),
            Documents = entry.Members.Documents
                .Select(d => new JsonDocumentMember { Path = d.Path, Type = d.Type })
                .ToList(),
        },
        References = entry.References.Count == 0
            ? null
            : entry.References
                .Select(r => new JsonWorkspaceReference { WorkspaceId = r.WorkspaceId, Mode = r.Mode, PinnedStateHash = r.PinnedStateHash })
                .ToList(),
    };

    private string GetManifestPath(Guid workspaceId) =>
        Path.Join(_rootDirectory, workspaceId.ToString("N"), ManifestFileName);

    /// <summary>
    /// On-disk shape of a <see cref="WorkspaceRegistryEntry"/>. Carries an explicit schema version
    /// so a future milestone can introduce version-gated reads without a wire-format redesign —
    /// the same convention <c>Ferret.Persistence.FileDependencyStateStore</c> already establishes
    /// in this codebase. <see cref="Members"/> defaults to an empty instance so a manifest written
    /// before WIP-011 (with no "members" property at all) still deserializes correctly.
    /// </summary>
    private sealed class JsonWorkspaceRegistryEntryEnvelope
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = FileWorkspaceRegistry.SupportedSchemaVersion;

        [JsonPropertyName("workspaceId")]
        public Guid WorkspaceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "personal";

        [JsonPropertyName("members")]
        public JsonWorkspaceMembers Members { get; set; } = new();

        /// <summary>Gets or sets the on-disk v1.1 field (<c>02-Workspace-Model.md</c> §3). Null (and so omitted by
        /// <see cref="SerializerOptions"/>'s <c>WhenWritingNull</c> policy) for an entry with no references, keeping
        /// v1.0 output byte-identical to pre-WIP-SLICE-2 output; absent-on-disk deserializes back to null here too.</summary>
        [JsonPropertyName("references")]
        public List<JsonWorkspaceReference>? References { get; set; }
    }

    /// <summary>On-disk shape of the "members" object (<c>02-Workspace-Model.md</c> §3).</summary>
    private sealed class JsonWorkspaceMembers
    {
        [JsonPropertyName("repos")]
        public List<JsonRepoMember> Repos { get; set; } = [];

        [JsonPropertyName("documents")]
        public List<JsonDocumentMember> Documents { get; set; } = [];
    }

    /// <summary>On-disk shape of one <see cref="RepoMember"/>.</summary>
    private sealed class JsonRepoMember
    {
        [JsonPropertyName("remote")]
        public string Remote { get; set; } = string.Empty;

        [JsonPropertyName("localPath")]
        public string? LocalPath { get; set; }
    }

    /// <summary>On-disk shape of one <see cref="DocumentMember"/>.</summary>
    private sealed class JsonDocumentMember
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>On-disk shape of one <see cref="WorkspaceReference"/>.</summary>
    private sealed class JsonWorkspaceReference
    {
        [JsonPropertyName("workspaceId")]
        public Guid WorkspaceId { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "read-only";

        [JsonPropertyName("pinnedStateHash")]
        public string? PinnedStateHash { get; set; }
    }
}
