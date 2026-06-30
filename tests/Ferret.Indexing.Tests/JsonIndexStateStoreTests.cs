using Ferret.Core.Connectors;

namespace Ferret.Indexing.Tests;

public sealed class JsonIndexStateStoreTests : IAsyncDisposable
{
    private readonly string _filePath;
    private readonly JsonIndexStateStore _store;

    public JsonIndexStateStoreTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"ferret-state-test-{Guid.NewGuid():N}.json");
        _store = new JsonIndexStateStore(_filePath);
    }

    [Fact]
    public async Task GetFingerprintAsync_UnknownAsset_ReturnsNull()
    {
        var assetId = new AssetId("file:///unknown.cs");
        var result = await _store.GetFingerprintAsync(assetId);
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAndGet_RoundTrips()
    {
        var assetId = new AssetId("file:///workspace/file.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(
            DateTimeOffset.UtcNow, sizeBytes: 1024);

        await _store.SetFingerprintAsync(assetId, fingerprint);
        var retrieved = await _store.GetFingerprintAsync(assetId);

        Assert.NotNull(retrieved);
        Assert.Equal(fingerprint.Algorithm, retrieved.Algorithm);
        Assert.Equal(fingerprint.Value, retrieved.Value);
    }

    [Fact]
    public async Task SaveAndReload_PersistsToDisk()
    {
        var assetId = new AssetId("file:///workspace/persistent.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), sizeBytes: 512);

        await _store.SetFingerprintAsync(assetId, fingerprint);
        await _store.SaveAsync();

        // Load from the same file path
        var reloaded = new JsonIndexStateStore(_filePath);
        var retrieved = await reloaded.GetFingerprintAsync(assetId);

        Assert.NotNull(retrieved);
        Assert.Equal(fingerprint.Value, retrieved.Value);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntry()
    {
        var assetId = new AssetId("file:///workspace/toremove.cs");
        await _store.SetFingerprintAsync(
            assetId,
            AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100));

        await _store.RemoveAsync(assetId);

        var result = await _store.GetFingerprintAsync(assetId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        await _store.SetFingerprintAsync(
            new AssetId("file:///a.cs"),
            AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1));
        await _store.SetFingerprintAsync(
            new AssetId("file:///b.cs"),
            AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2));

        await _store.ClearAsync();

        var keys = await _store.GetAllKeysAsync();
        Assert.Empty(keys);
    }

    [Fact]
    public async Task GetAllKeysAsync_ReturnsAllSetAssets()
    {
        var id1 = new AssetId("file:///a.cs");
        var id2 = new AssetId("file:///b.cs");
        await _store.SetFingerprintAsync(id1, AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1));
        await _store.SetFingerprintAsync(id2, AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2));

        var keys = await _store.GetAllKeysAsync();
        Assert.Equal(2, keys.Count);
        Assert.Contains(id1, keys);
        Assert.Contains(id2, keys);
    }

    public async ValueTask DisposeAsync()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }

        await ValueTask.CompletedTask;
    }
}
