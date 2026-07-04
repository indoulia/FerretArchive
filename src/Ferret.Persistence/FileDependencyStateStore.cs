using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Ferret.Core.Connectors;

namespace Ferret.Persistence;

/// <summary>
/// Production implementation of <see cref="IDependencyStateStore"/> (S2-2, decided by ADR-0022;
/// wire format decided by ADR-0023; key/lookup structure decided by ADR-0024). Same atomic-write
/// mechanism (temp file + rename) and the same per-record JSON envelope as established in S2-2/
/// S2-3 — S2-4 only changes how a request identity maps to a file location, not how a record is
/// written once that location is known, and not what bytes are written to it. As of S2-4, the
/// constructor argument is a root directory, not a single fixed file: each distinct
/// (engine responsibility, request path) key maps, via <see cref="GetRecordFilePath"/>, to its own
/// file directly and deterministically — never by scanning the directory or consulting a separate
/// index file — so this store can hold more than one record at a time. S2-8: <see cref="GetRecordAsync"/>
/// also classifies every unreadability category this backend can actually produce — malformed
/// wire-format content and filesystem-level I/O failure — and fails closed by returning null for
/// each, the same signal already used for "no record at this key". Callers therefore never observe
/// a storage-technology exception type; the classification stays fully encapsulated here rather
/// than being caught ad hoc at each call site. Dependency capture, dependency chains, comparison
/// logic, and retention remain out of scope — see ADR-0022, ADR-0023, and ADR-0024.
/// </summary>
public sealed class FileDependencyStateStore : IDependencyStateStore
{
    private const string CurrentSchemaVersion = "1.0";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _rootDirectory;

    /// <summary>Initializes a new instance of the <see cref="FileDependencyStateStore"/> class.</summary>
    /// <param name="rootDirectory">Absolute path to the directory under which this store's keyed record files live, e.g. under <c>.ferret/temp/</c>.</param>
    public FileDependencyStateStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = rootDirectory;
    }

    /// <inheritdoc/>
    public async ValueTask<DependencyRecord?> GetRecordAsync(string engineResponsibility, string requestPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineResponsibility);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPath);

        var recordFilePath = GetRecordFilePath(engineResponsibility, requestPath);
        if (!File.Exists(recordFilePath))
        {
            // S2-9: a temp file is never a valid record by construction (it only ever exists
            // between File.Create and the atomic rename in SetRecordAsync), so finding one here
            // with no completed target means an earlier write crashed before renaming (ADR-0022's
            // noted risk). Safe to discard unconditionally — unlike the target file itself, there
            // is no "not yet safely classified" case to guard against.
            EvictOrphanedTempFile(recordFilePath);
            return null;
        }

        JsonDependencyRecordEnvelope? envelope;
        try
        {
            var stream = File.OpenRead(recordFilePath);
            await using (stream.ConfigureAwait(false))
            {
                envelope = await JsonSerializer.DeserializeAsync<JsonDependencyRecordEnvelope>(stream, SerializerOptions, ct).ConfigureAwait(false);
            }
        }
        catch (JsonException)
        {
            // Malformed content for this backend's chosen wire format (ADR-0023) — the bytes on
            // disk are not a valid envelope. Fail-closed per ARCH-026 §7: unreadable, not absent-but-fine.
            // S2-9: this is a durable, non-transient fact about the bytes on disk (they will never
            // become valid on a later read), so ARCH-026 §5's "discard it" disposition applies —
            // retaining a permanently corrupted file serves no purpose.
            EvictCorruptedFile(recordFilePath);
            return null;
        }
        catch (IOException)
        {
            // The chosen backend's storage medium (ADR-0022, local filesystem) could not produce
            // the bytes at all — e.g. a sharing violation from a concurrent handle. Same fail-closed
            // treatment as malformed content: the record cannot be confirmed, so it is unreadable.
            // S2-9: unlike malformed content, this is not a safe corruption classification — the
            // file may simply be mid-write by another handle — so it is never evicted here.
            return null;
        }

        if (envelope is null || envelope.EngineResponsibility != engineResponsibility || envelope.RequestPath != requestPath)
        {
            return null;
        }

        return ToRecord(envelope);
    }

    /// <inheritdoc/>
    public async Task SetRecordAsync(DependencyRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var recordFilePath = GetRecordFilePath(record.EngineResponsibility, record.RequestPath);
        var dir = Path.GetDirectoryName(recordFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var envelope = ToEnvelope(record);
        var tmpPath = recordFilePath + ".tmp";
        var stream = File.Create(tmpPath);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, envelope, SerializerOptions, ct).ConfigureAwait(false);
        }

        File.Move(tmpPath, recordFilePath, overwrite: true);
    }

    private static DependencyRecord ToRecord(JsonDependencyRecordEnvelope envelope) => new()
    {
        EngineResponsibility = envelope.EngineResponsibility,
        RequestPath = envelope.RequestPath,
        SourceFingerprint = new AssetFingerprint(envelope.SourceFingerprint.Algorithm, envelope.SourceFingerprint.Value),
        PlainText = envelope.PlainText,
        ConfigurationDependency = ToConfigurationDependency(envelope.ConfigurationDependency),
        DependencyChain = ToDependencyChain(envelope.DependencyChain),
    };

    private static JsonDependencyRecordEnvelope ToEnvelope(DependencyRecord record) => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        EngineResponsibility = record.EngineResponsibility,
        RequestPath = record.RequestPath,
        SourceFingerprint = new JsonAssetFingerprint
        {
            Algorithm = record.SourceFingerprint.Algorithm,
            Value = record.SourceFingerprint.Value,
        },
        PlainText = record.PlainText,
        ConfigurationDependency = ToJsonConfigurationDependency(record.ConfigurationDependency),
        DependencyChain = ToJsonDependencyChain(record.DependencyChain),
    };

    private static ConfigurationDependency? ToConfigurationDependency(JsonConfigurationDependency? envelope) =>
        envelope is null
            ? null
            : new ConfigurationDependency
            {
                Parser = ToComponentRegistrationIdentity(envelope.Parser),
                Connector = ToComponentRegistrationIdentity(envelope.Connector),
            };

    private static JsonConfigurationDependency? ToJsonConfigurationDependency(ConfigurationDependency? dependency) =>
        dependency is null
            ? null
            : new JsonConfigurationDependency
            {
                Parser = ToJsonComponentRegistrationIdentity(dependency.Parser),
                Connector = ToJsonComponentRegistrationIdentity(dependency.Connector),
            };

    private static ComponentRegistrationIdentity? ToComponentRegistrationIdentity(JsonComponentRegistrationIdentity? envelope) =>
        envelope is null
            ? null
            : new ComponentRegistrationIdentity { Id = envelope.Id, Version = envelope.Version };

    private static JsonComponentRegistrationIdentity? ToJsonComponentRegistrationIdentity(ComponentRegistrationIdentity? identity) =>
        identity is null
            ? null
            : new JsonComponentRegistrationIdentity { Id = identity.Id, Version = identity.Version };

    private static DependencyChain ToDependencyChain(List<JsonDependencyReference>? envelope) =>
        envelope is null || envelope.Count == 0
            ? DependencyChain.Empty
            : new DependencyChain
            {
                References = envelope
                    .Select(r => new DependencyReference { EngineResponsibility = r.EngineResponsibility, RequestPath = r.RequestPath })
                    .ToList(),
            };

    private static List<JsonDependencyReference> ToJsonDependencyChain(DependencyChain chain) =>
        chain.References
            .Select(r => new JsonDependencyReference { EngineResponsibility = r.EngineResponsibility, RequestPath = r.RequestPath })
            .ToList();

    /// <summary>
    /// S2-9 (ARCH-026 §5's "discard it" disposition): removes a record file whose content S2-8
    /// already classified as durably corrupted (malformed for this backend's wire format). This is
    /// a best-effort cleanup, not a correctness requirement — <see cref="GetRecordAsync"/> already
    /// returned null on its own before this runs. If the delete itself cannot complete (e.g. the
    /// file becomes locked between the failed read and this call), it is left for a future query to
    /// retry rather than surfacing a new failure mode.
    /// </summary>
    private static void EvictCorruptedFile(string recordFilePath)
    {
        try
        {
            File.Delete(recordFilePath);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// S2-9 (ADR-0022's noted risk): removes a stray <c>.tmp</c> file left behind by a
    /// <see cref="SetRecordAsync"/> that crashed after <c>File.Create</c> but before the atomic
    /// rename, discovered the next time its key is queried and found to have no completed record.
    /// A temp file is never a valid persisted record by construction — <see cref="GetRecordAsync"/>
    /// never reads from this path — so, unlike <see cref="EvictCorruptedFile"/>, there is no "not
    /// yet safely classified" case here: deleting it can never discard data that was ever readable.
    /// Best-effort: if another in-flight <see cref="SetRecordAsync"/> holds the file open, the
    /// delete simply fails silently and is retried on a later query.
    /// </summary>
    private static void EvictOrphanedTempFile(string recordFilePath)
    {
        var tmpPath = recordFilePath + ".tmp";
        if (!File.Exists(tmpPath))
        {
            return;
        }

        try
        {
            File.Delete(tmpPath);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// Computes the on-disk location for a request identity (ARCH-028 §2, properties 1-2) as a
    /// direct, deterministic function of the key: a SHA-256 hash of the two identity components,
    /// joined by a NUL separator so no combination of responsibility/path strings can collide by
    /// concatenation alone. Retrieval cost is therefore independent of how many other records this
    /// store holds — there is no directory scan and no separate index file to keep in sync.
    /// </summary>
    private string GetRecordFilePath(string engineResponsibility, string requestPath)
    {
        var keyBytes = Encoding.UTF8.GetBytes(engineResponsibility + '\0' + requestPath);
        var hash = Convert.ToHexStringLower(SHA256.HashData(keyBytes));
        return Path.Join(_rootDirectory, hash + ".json");
    }

    /// <summary>
    /// On-disk shape of a <see cref="DependencyRecord"/> (ADR-0023). Carries an explicit schema
    /// version so a future milestone can introduce version-gated reads without a wire-format
    /// redesign; today every write embeds <see cref="CurrentSchemaVersion"/> and every read
    /// accepts whatever version is present without acting on it.
    /// </summary>
    private sealed class JsonDependencyRecordEnvelope
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = CurrentSchemaVersion;

        [JsonPropertyName("engineResponsibility")]
        public string EngineResponsibility { get; set; } = string.Empty;

        [JsonPropertyName("requestPath")]
        public string RequestPath { get; set; } = string.Empty;

        [JsonPropertyName("sourceFingerprint")]
        public JsonAssetFingerprint SourceFingerprint { get; set; } = new();

        [JsonPropertyName("plainText")]
        public string? PlainText { get; set; }

        /// <summary>Gets or sets the shape-4 dependency (S2-5). Null for records written before S2-5, and for records with no such dependency — a purely additive property, applied without a schema-version bump since this is additive, not breaking.</summary>
        [JsonPropertyName("configurationDependency")]
        public JsonConfigurationDependency? ConfigurationDependency { get; set; }

        /// <summary>Gets or sets the shape-2 (derived-artifact) dependency chain (S2-6), as a flat array of references — never absent in intent, defaulting to empty for records written before S2-6.</summary>
        [JsonPropertyName("dependencyChain")]
        public List<JsonDependencyReference> DependencyChain { get; set; } = [];
    }

    /// <summary>
    /// On-disk shape of an <see cref="AssetFingerprint"/> within a <see cref="JsonDependencyRecordEnvelope"/>.
    /// Kept as an explicit local DTO (rather than serializing <see cref="AssetFingerprint"/>
    /// directly) so this store's wire format never depends on how a `Ferret.Core` domain type
    /// happens to serialize by default.
    /// </summary>
    private sealed class JsonAssetFingerprint
    {
        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>On-disk shape of a <see cref="ConfigurationDependency"/> (S2-5, ARCH-032 §2.1 shape 4).</summary>
    private sealed class JsonConfigurationDependency
    {
        [JsonPropertyName("parser")]
        public JsonComponentRegistrationIdentity? Parser { get; set; }

        [JsonPropertyName("connector")]
        public JsonComponentRegistrationIdentity? Connector { get; set; }
    }

    /// <summary>On-disk shape of a <see cref="ComponentRegistrationIdentity"/> (S2-5).</summary>
    private sealed class JsonComponentRegistrationIdentity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>On-disk shape of one <see cref="DependencyReference"/> within a <see cref="JsonDependencyRecordEnvelope"/>'s dependency chain (S2-6).</summary>
    private sealed class JsonDependencyReference
    {
        [JsonPropertyName("engineResponsibility")]
        public string EngineResponsibility { get; set; } = string.Empty;

        [JsonPropertyName("requestPath")]
        public string RequestPath { get; set; } = string.Empty;
    }
}
