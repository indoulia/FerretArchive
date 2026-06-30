using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing.Tests.Fakes;
using Xunit;

namespace Ferret.Indexing.Tests;

/// <summary>Verifies <see cref="IndexPipeline"/> correctly uses <see cref="IConnectorManager"/> + <see cref="ConnectorRuntime"/> (S4 correction).</summary>
public sealed class IndexPipelineConnectorManagerTests
{
    /// <summary>Pipeline with FakeConnectorManager indexes one document end-to-end.</summary>
    [Fact]
    public async Task Pipeline_Receives_FakeConnectorManager_And_Accesses_Connector()
    {
        var asset = new AssetDescriptor
        {
            Id = new AssetId("filesystem:///src/a.txt"),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///src/a.txt"),
            DisplayName = "a.txt",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/plain",
        };
        var fakeConnector = new FakeConnectorFull(
            [asset],
            _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content")));
        var manager = new FakeConnectorManager([MakeRuntime(fakeConnector)]);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(new Document
        {
            Id = DocumentId.From(a.Id),
            SourceAssetId = a.Id,
            ConnectorId = a.ConnectorId,
            InstanceId = a.InstanceId,
            MediaType = "text/plain",
            Kind = DocumentKind.Unknown,
            PlainText = "content",
            ProducedAt = DateTimeOffset.UtcNow,
        }));
        var engine = new FakeIndexEngine();
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(manager, dispatcher, engine, bus);

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(1, result.DocumentsIndexed);
    }

    /// <summary>Connector that is not IAssetSource: zero assets discovered.</summary>
    [Fact]
    public async Task Pipeline_Skips_Runtime_Where_Connector_Is_Not_IAssetSource()
    {
        var plain = new FakePlainConnector();
        var manager = new FakeConnectorManager([MakeRuntime(plain)]);
        var dispatcher = new FakeParserDispatcher();
        var engine = new FakeIndexEngine();
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(manager, dispatcher, engine, bus);

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(0, result.AssetsDiscovered);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    // ── Inner fakes ──────────────────────────────────────────────────────────

    private sealed class FakeConnectorFull : IConnector, IAssetSource, IAssetReader
    {
        private readonly List<AssetDescriptor> _assets;
        private readonly Func<AssetDescriptor, Stream>? _streamFactory;

        internal FakeConnectorFull(
            IEnumerable<AssetDescriptor> assets,
            Func<AssetDescriptor, Stream>? streamFactory = null)
        {
            _assets = assets.ToList();
            _streamFactory = streamFactory;
        }

        public ConnectorType ConnectorType => ConnectorType.Filesystem;

        public ConnectorMetadata Metadata { get; } = ConnectorMetadata.Create(
            "full", "Full Connector", "Test", ConnectorType.Filesystem, "1.0");

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
            var stream = _streamFactory?.Invoke(asset)
                ?? new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
            return Task.FromResult(stream);
        }
    }

    private sealed class FakePlainConnector : IConnector
    {
        public ConnectorType ConnectorType => ConnectorType.Custom;

        public ConnectorMetadata Metadata { get; } = ConnectorMetadata.Create(
            "plain", "plain", "plain", ConnectorType.Custom, "1.0");

        public ConnectorIoCapabilities Capabilities { get; } = ConnectorIoCapabilities.ReadOnly();

        public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));

        public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
