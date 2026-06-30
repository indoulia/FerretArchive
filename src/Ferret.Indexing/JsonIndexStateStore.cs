using System.Text.Json;

using Ferret.Core.Connectors;
using Ferret.Core.Indexing;

namespace Ferret.Indexing;

/// <summary>JSON-backed state store for incremental indexing fingerprints.
/// Persists to a single JSON file; loads eagerly on construction.</summary>
public sealed class JsonIndexStateStore : IIndexStateStore
{
    private const char Separator = '|';

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly string _filePath;
    private readonly Dictionary<string, string> _state;

    /// <summary>Initializes a new instance of the <see cref="JsonIndexStateStore"/> class.</summary>
    /// <param name="filePath">Absolute path to the JSON state file.</param>
    public JsonIndexStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _state = Load(filePath);
    }

    /// <inheritdoc/>
    public ValueTask<AssetFingerprint?> GetFingerprintAsync(AssetId assetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        if (!_state.TryGetValue(assetId.Value, out var raw))
        {
            return ValueTask.FromResult<AssetFingerprint?>(null);
        }

        var sep = raw.IndexOf(Separator, StringComparison.Ordinal);
        if (sep < 0)
        {
            return ValueTask.FromResult<AssetFingerprint?>(null);
        }

        var algorithm = raw[..sep];
        var value = raw[(sep + 1)..];
        return ValueTask.FromResult<AssetFingerprint?>(new AssetFingerprint(algorithm, value));
    }

    /// <inheritdoc/>
    public Task SetFingerprintAsync(AssetId assetId, AssetFingerprint fingerprint, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        ArgumentNullException.ThrowIfNull(fingerprint);
        _state[assetId.Value] = $"{fingerprint.Algorithm}{Separator}{fingerprint.Value}";
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(AssetId assetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        _state.Remove(assetId.Value);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlySet<AssetId>> GetAllKeysAsync(CancellationToken ct = default)
    {
        var result = _state.Keys
            .Select(k => new AssetId(k))
            .ToHashSet();
        return ValueTask.FromResult<IReadOnlySet<AssetId>>(result);
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default)
    {
        _state.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(_state, SerializerOptions);
        await File.WriteAllTextAsync(_filePath, json, ct).ConfigureAwait(false);
    }

    private static Dictionary<string, string> Load(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
