using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Fakes;
using Xunit;

namespace Ferret.Indexing.Tests;

/// <summary>Verifies incremental indexing behaviour in <see cref="IndexPipeline"/>.</summary>
public sealed class IndexPipelineIncrementalTests
{
    [Fact]
    public async Task RunAsync_SecondRun_SkipsUnchangedAssets()
    {
        // Arrange: pre-populate JsonIndexStateStore with the fingerprint of our fake asset
        var lastModified = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const long sizeBytes = 1024L;
        var assetId = new AssetId("file:///workspace/file.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(lastModified, sizeBytes);

        var tempPath = Path.Combine(Path.GetTempPath(), $"ferret-inc-test-{Guid.NewGuid():N}.json");
        try
        {
            var seedStore = new JsonIndexStateStore(tempPath);
            await seedStore.SetFingerprintAsync(assetId, fingerprint);
            await seedStore.SaveAsync();

            // Reload from disk to simulate a fresh process
            var store = new JsonIndexStateStore(tempPath);

            var engine = new FakeIndexEngine();
            var asset = new AssetDescriptor
            {
                Id = assetId,
                ConnectorId = new ConnectorId("file"),
                InstanceId = new ConnectorInstanceId("default"),
                Kind = AssetKind.File,
                CanonicalUri = new Uri("file:///workspace/file.cs"),
                DisplayName = "file.cs",
                LastModified = lastModified,
                SizeBytes = sizeBytes,
            };

            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([asset]))]),
                new FakeParserDispatcher(),
                engine,
                new FakeEventBus(),
                store);

            // Act
            var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

            // Assert: asset skipped (fingerprint unchanged)
            Assert.Empty(engine.WrittenDocuments);
            Assert.Equal(1, result.DocumentsSkipped);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task RunAsync_ForceRebuild_ClearsStateStoreAndReindexesAll()
    {
        // Arrange: pre-populate state store
        var assetId = new AssetId("file:///workspace/rebuild.cs");
        var lastModified = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const long sizeBytes = 100L;
        var fingerprint = AssetFingerprint.CreateLightweight(lastModified, sizeBytes);

        var tempPath = Path.Combine(Path.GetTempPath(), $"ferret-rebuild-test-{Guid.NewGuid():N}.json");
        try
        {
            var seedStore = new JsonIndexStateStore(tempPath);
            await seedStore.SetFingerprintAsync(assetId, fingerprint);
            await seedStore.SaveAsync();

            var store = new JsonIndexStateStore(tempPath);
            var engine = new FakeIndexEngine();
            var asset = new AssetDescriptor
            {
                Id = assetId,
                ConnectorId = new ConnectorId("file"),
                InstanceId = new ConnectorInstanceId("default"),
                Kind = AssetKind.File,
                CanonicalUri = new Uri("file:///workspace/rebuild.cs"),
                DisplayName = "rebuild.cs",
                LastModified = lastModified,
                SizeBytes = sizeBytes,
            };

            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([asset]))]),
                new FakeParserDispatcher(),
                engine,
                new FakeEventBus(),
                store);

            // Act: ForceRebuild clears state store; asset re-processed regardless of prior fingerprint
            var result = await pipeline.RunAsync(
                WorkspaceId.Create("test"),
                new IndexPipelineOptions { ForceRebuild = true });

            // Key: engine was cleared and asset was re-processed (not skipped due to fingerprint)
            Assert.Equal(1, engine.ClearCount);
            Assert.Equal(1, result.AssetsProcessed);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task RunAsync_StaleAsset_RemovedFromStateStore()
    {
        // Arrange: state store contains an asset that is no longer discovered
        var staleId = new AssetId("file:///workspace/deleted.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 50L);

        var tempPath = Path.Combine(Path.GetTempPath(), $"ferret-stale-test-{Guid.NewGuid():N}.json");
        try
        {
            var seedStore = new JsonIndexStateStore(tempPath);
            await seedStore.SetFingerprintAsync(staleId, fingerprint);
            await seedStore.SaveAsync();

            var store = new JsonIndexStateStore(tempPath);

            // Pipeline discovers no assets -- stale entry should be cleaned up
            var pipeline = new IndexPipeline(
                new FakeConnectorManager([]),
                new FakeParserDispatcher(),
                new FakeIndexEngine(),
                new FakeEventBus(),
                store);

            await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

            // Verify stale entry removed from store
            var remaining = await store.GetAllKeysAsync();
            Assert.Empty(remaining);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // -- Helpers --

    private static ConnectorRuntime MakeRuntime(IConnector connector) =>
        new()
        {
            Instance = new ConnectorInstance
            {
                Id = new ConnectorInstanceId("test"),
                ConnectorType = new ConnectorId("filesystem"),
                DisplayName = "Test",
            },
            Connector = connector,
            Status = new ConnectorStatus
            {
                ConnectorId = new ConnectorId("filesystem"),
                InstanceId = new ConnectorInstanceId("test"),
                IsActive = true,
                Health = ConnectorHealth.Connected(DateTimeOffset.UtcNow),
            },
        };

    // Inner fake implementing IConnector + IAssetSource + IAssetReader
    private sealed class FakeConnectorWithReader : IConnector, IAssetSource, IAssetReader
    {
        private readonly List<AssetDescriptor> _assets;

        internal FakeConnectorWithReader(IEnumerable<AssetDescriptor> assets)
        {
            _assets = assets.ToList();
        }

        public ConnectorType ConnectorType => ConnectorType.Filesystem;

        public ConnectorMetadata Metadata { get; } = ConnectorMetadata.Create(
            "test", "Test Connector", "Test", ConnectorType.Filesystem, "1.0");

        public ConnectorIoCapabilities Capabilities { get; } = ConnectorIoCapabilities.ReadOnly();

        public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));

        public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

        public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
            AssetDiscoveryOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var asset in _assets)
            {
                ct.ThrowIfCancellationRequested();
                yield return asset;
                await Task.Yield();
            }
        }

        public Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes("sample content")));
        }
    }
}
