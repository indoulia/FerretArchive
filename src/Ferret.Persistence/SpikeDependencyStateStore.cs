using System.Text.Json;

namespace Ferret.Persistence;

/// <summary>
/// Sprint 1 disposable spike implementation of <see cref="IDependencyStateStore"/>, mirroring
/// the direct-read/direct-write pattern of <c>JsonWorkspaceStore</c> (no eager load, no in-memory
/// cache, no explicit flush step). Proves ARCH-032 §3's persistence mechanism is implementable
/// for exactly one <see cref="DependencyRecord"/>. Not production storage: no concurrency
/// handling, no locking, no atomic writes, no versioning, and no deletion support.
/// As of S2-2 (ADR-0022), <see cref="FileDependencyStateStore"/> is the composition root's
/// registered implementation — this type is retained only as a reference implementation.
/// </summary>
public sealed class SpikeDependencyStateStore : IDependencyStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    /// <summary>Initializes a new instance of the <see cref="SpikeDependencyStateStore"/> class.</summary>
    /// <param name="filePath">Absolute path to the JSON file backing this store, e.g. under <c>.ferret/temp/</c>.</param>
    public SpikeDependencyStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    /// <inheritdoc/>
    public async ValueTask<DependencyRecord?> GetRecordAsync(string engineResponsibility, string requestPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineResponsibility);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPath);

        if (!File.Exists(_filePath))
        {
            return null;
        }

        DependencyRecord? record;
        var stream = File.OpenRead(_filePath);
        await using (stream.ConfigureAwait(false))
        {
            record = await JsonSerializer.DeserializeAsync<DependencyRecord>(stream, SerializerOptions, ct).ConfigureAwait(false);
        }

        if (record is null || record.EngineResponsibility != engineResponsibility || record.RequestPath != requestPath)
        {
            return null;
        }

        return record;
    }

    /// <inheritdoc/>
    public async Task SetRecordAsync(DependencyRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var stream = File.Create(_filePath);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, record, SerializerOptions, ct).ConfigureAwait(false);
        }
    }
}
