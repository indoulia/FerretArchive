using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing.Tests.Fakes;

using Xunit;

namespace Ferret.Indexing.Tests;

/// <summary>Verifies <see cref="IndexPipeline.RunSingleAssetAsync"/> — the O(1)-per-change reindex
/// path used by watch mode (issue #17) instead of a full <see cref="IndexPipeline.RunAsync"/>
/// corpus walk per file-system-watcher event.</summary>
public sealed class IndexPipelineSingleAssetTests
{
    [Fact]
    public async Task RunSingleAssetAsync_ChangedAsset_IndexesIt()
    {
        var assetId = new AssetId("filesystem:///a.cs");
        var asset = new AssetDescriptor
        {
            Id = assetId,
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("default"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///a.cs"),
            DisplayName = "a.cs",
            LastModified = DateTimeOffset.UtcNow,
            SizeBytes = 10,
        };

        var tempPath = Path.Join(Path.GetTempPath(), $"ferret-single-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonIndexStateStore(tempPath);
            var engine = new FakeIndexEngine();
            var dispatcher = new FakeParserDispatcher();
            dispatcher.SetResult(a => ParseResult<Document>.Success(MakeDocument(a)));
            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([asset]))]),
                dispatcher,
                engine,
                new FakeEventBus(),
                store);

            var result = await pipeline.RunSingleAssetAsync(WorkspaceId.Create("test"), assetId);

            Assert.Equal(1, result.DocumentsIndexed);
            Assert.Single(engine.WrittenDocuments);
            Assert.NotNull(await store.GetFingerprintAsync(assetId));
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
    public async Task RunSingleAssetAsync_UnchangedFingerprint_SkipsIt()
    {
        var lastModified = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const long sizeBytes = 1024L;
        var assetId = new AssetId("filesystem:///unchanged.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(lastModified, sizeBytes);
        var asset = new AssetDescriptor
        {
            Id = assetId,
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("default"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///unchanged.cs"),
            DisplayName = "unchanged.cs",
            LastModified = lastModified,
            SizeBytes = sizeBytes,
        };

        var tempPath = Path.Join(Path.GetTempPath(), $"ferret-single-unchanged-{Guid.NewGuid():N}.json");
        try
        {
            var seedStore = new JsonIndexStateStore(tempPath);
            await seedStore.SetFingerprintAsync(assetId, fingerprint);
            await seedStore.SaveAsync();
            var store = new JsonIndexStateStore(tempPath);

            var engine = new FakeIndexEngine();
            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([asset]))]),
                new FakeParserDispatcher(),
                engine,
                new FakeEventBus(),
                store);

            var result = await pipeline.RunSingleAssetAsync(WorkspaceId.Create("test"), assetId);

            Assert.Equal(1, result.DocumentsSkipped);
            Assert.Empty(engine.WrittenDocuments);
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
    public async Task RunSingleAssetAsync_AssetNoLongerResolvable_DeletesFromEngineAndStateStore()
    {
        var assetId = new AssetId("filesystem:///deleted.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 50L);

        var tempPath = Path.Join(Path.GetTempPath(), $"ferret-single-deleted-{Guid.NewGuid():N}.json");
        try
        {
            var seedStore = new JsonIndexStateStore(tempPath);
            await seedStore.SetFingerprintAsync(assetId, fingerprint);
            await seedStore.SaveAsync();
            var store = new JsonIndexStateStore(tempPath);

            var engine = new FakeIndexEngine();

            // No connector resolves this asset -- it no longer exists (or moved out of scope).
            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([]))]),
                new FakeParserDispatcher(),
                engine,
                new FakeEventBus(),
                store);

            await pipeline.RunSingleAssetAsync(WorkspaceId.Create("test"), assetId);

            Assert.Contains(DocumentId.From(assetId), engine.DeletedDocumentIds);
            Assert.Null(await store.GetFingerprintAsync(assetId));
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
    public async Task RunSingleAssetAsync_ResolvedAssetIsDirectory_IsNoOp()
    {
        var assetId = new AssetId("filesystem:///src");
        var asset = new AssetDescriptor
        {
            Id = assetId,
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("default"),
            Kind = AssetKind.Directory,
            CanonicalUri = new Uri("filesystem:///src"),
            DisplayName = "src",
            LastModified = DateTimeOffset.UtcNow,
        };

        var tempPath = Path.Join(Path.GetTempPath(), $"ferret-single-dir-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonIndexStateStore(tempPath);
            var engine = new FakeIndexEngine();
            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([asset]))]),
                new FakeParserDispatcher(),
                engine,
                new FakeEventBus(),
                store);

            var result = await pipeline.RunSingleAssetAsync(WorkspaceId.Create("test"), assetId);

            Assert.Equal(0, result.DocumentsIndexed);
            Assert.Equal(0, result.DocumentsSkipped);
            Assert.Empty(engine.WrittenDocuments);
            Assert.Empty(engine.DeletedDocumentIds);
            Assert.Null(await store.GetFingerprintAsync(assetId));
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
    public async Task RunSingleAssetAsync_DoesNotTouchUnrelatedStateStoreEntries()
    {
        // No global stale sweep: a single-asset run must not remove or otherwise
        // touch entries for assets it wasn't asked about.
        var targetId = new AssetId("filesystem:///target.cs");
        var unrelatedId = new AssetId("filesystem:///unrelated.cs");
        var unrelatedFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 5L);
        var asset = new AssetDescriptor
        {
            Id = targetId,
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("default"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///target.cs"),
            DisplayName = "target.cs",
            LastModified = DateTimeOffset.UtcNow,
            SizeBytes = 10,
        };

        var tempPath = Path.Join(Path.GetTempPath(), $"ferret-single-unrelated-{Guid.NewGuid():N}.json");
        try
        {
            var seedStore = new JsonIndexStateStore(tempPath);
            await seedStore.SetFingerprintAsync(unrelatedId, unrelatedFingerprint);
            await seedStore.SaveAsync();
            var store = new JsonIndexStateStore(tempPath);

            var pipeline = new IndexPipeline(
                new FakeConnectorManager([MakeRuntime(new FakeConnectorWithReader([asset]))]),
                new FakeParserDispatcher(),
                new FakeIndexEngine(),
                new FakeEventBus(),
                store);

            await pipeline.RunSingleAssetAsync(WorkspaceId.Create("test"), targetId);

            Assert.NotNull(await store.GetFingerprintAsync(unrelatedId));
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
    public async Task RunSingleAssetAsync_DefaultInterfaceImplementation_DelegatesToRunAsync()
    {
        // Proves the IIndexPipeline.RunSingleAssetAsync default (delegates to RunAsync) is a
        // genuinely non-breaking addition per ADR-0012 rule 2 -- a bare implementation that only
        // provides RunAsync still behaves correctly when called via the new member.
        var bare = new BareIndexPipeline();
        IIndexPipeline pipeline = bare;

        var result = await pipeline.RunSingleAssetAsync(WorkspaceId.Create("test"), new AssetId("filesystem:///x.cs"));

        Assert.Equal(1, bare.RunAsyncCallCount);
        Assert.NotNull(result);
    }

    private static Document MakeDocument(AssetDescriptor asset) => new()
    {
        Id = DocumentId.From(asset.Id),
        SourceAssetId = asset.Id,
        ConnectorId = asset.ConnectorId,
        InstanceId = asset.InstanceId,
        MediaType = asset.MediaType ?? "text/plain",
        Kind = DocumentKind.Prose,
        PlainText = "Hello from " + asset.DisplayName,
        ProducedAt = DateTimeOffset.UtcNow,
    };

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

    // Local fake implementing IConnector + IAssetSource + IAssetReader, with a real TryGetAsync
    // (matching FakeConnectorWithReader in IndexPipelineIncrementalTests.cs, plus single-asset lookup).
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

        public Task<AssetDescriptor?> TryGetAsync(AssetId assetId, CancellationToken ct = default) =>
            Task.FromResult(_assets.FirstOrDefault(a => a.Id == assetId));
    }

    private sealed class BareIndexPipeline : IIndexPipeline
    {
        internal int RunAsyncCallCount { get; private set; }

        public Task<IndexResult> RunAsync(WorkspaceId workspaceId, IndexPipelineOptions options, CancellationToken ct = default)
        {
            RunAsyncCallCount++;
            return Task.FromResult(new IndexResult
            {
                AssetsDiscovered = 0,
                AssetsProcessed = 0,
                DocumentsIndexed = 0,
                DocumentsSkipped = 0,
                Failures = 0,
                Warnings = 0,
                Duration = TimeSpan.Zero,
            });
        }
    }
}
