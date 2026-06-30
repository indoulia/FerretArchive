using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Events.Indexing;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Fakes;

using Xunit;

namespace Ferret.Indexing.Tests;

/// <summary>Unit tests for <see cref="IndexPipeline"/>.</summary>
public sealed class IndexPipelineTests
{
    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>Pipeline publishes IndexingStartedEvent when run.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Publishes_IndexingStartedEvent()
    {
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(new FakeConnectorManager([]), new FakeParserDispatcher(), new FakeIndexEngine(), bus);

        await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Contains(bus.Published, e => e is IndexingStartedEvent);
    }

    /// <summary>Pipeline publishes IndexingCompletedEvent when run.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Publishes_IndexingCompletedEvent()
    {
        var bus = new FakeEventBus();
        var pipeline = new IndexPipeline(new FakeConnectorManager([]), new FakeParserDispatcher(), new FakeIndexEngine(), bus);

        await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Contains(bus.Published, e => e is IndexingCompletedEvent);
    }

    /// <summary>Zero counts when no connectors are active.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Returns_Zero_Counts_When_No_Connectors()
    {
        var pipeline = new IndexPipeline(new FakeConnectorManager([]), new FakeParserDispatcher(), new FakeIndexEngine(), new FakeEventBus());

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(0, result.AssetsDiscovered);
        Assert.Equal(0, result.DocumentsIndexed);
    }

    /// <summary>Connector implementing IAssetSource but not IAssetReader: asset discovered, skipped.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Skips_Connectors_Without_IAssetReader()
    {
        var asset = MakeAsset("readme.md");
        var connector = new FakeConnectorSourceOnly([asset]);
        var engine = new FakeIndexEngine();
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector)]),
            new FakeParserDispatcher(),
            engine,
            new FakeEventBus());

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(1, result.AssetsDiscovered);
        Assert.Equal(0, result.AssetsProcessed);
        Assert.Equal(1, result.DocumentsSkipped);
        Assert.Empty(engine.WrittenDocuments);
    }

    /// <summary>Successful parse → document indexed.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Indexes_Document_When_Parse_Succeeds()
    {
        var asset = MakeAsset("readme.md", "text/markdown");
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(MakeDocument(a)));
        var engine = new FakeIndexEngine();
        var connector = new FakeConnectorWithReader([asset]);
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector)]),
            dispatcher,
            engine,
            new FakeEventBus());

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(1, result.DocumentsIndexed);
        Assert.Single(engine.WrittenDocuments);
    }

    /// <summary>Unsupported media type → skipped.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Skips_Unsupported_MediaType()
    {
        var asset = MakeAsset("binary.xyz", "application/octet-stream");
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Unsupported(a.MediaType ?? "application/octet-stream"));
        var engine = new FakeIndexEngine();
        var connector = new FakeConnectorWithReader([asset]);
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector)]),
            dispatcher,
            engine,
            new FakeEventBus());

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(0, result.DocumentsIndexed);
        Assert.Equal(1, result.DocumentsSkipped);
        Assert.Empty(engine.WrittenDocuments);
    }

    /// <summary>Parse failure → failure counted.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Counts_Parse_Failures()
    {
        var asset = MakeAsset("broken.md", "text/markdown");
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(_ => ParseResult<Document>.Failed("Parse error"));
        var engine = new FakeIndexEngine();
        var connector = new FakeConnectorWithReader([asset]);
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector)]),
            dispatcher,
            engine,
            new FakeEventBus());

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(1, result.Failures);
        Assert.Single(result.FailureMessages);
        Assert.Empty(engine.WrittenDocuments);
    }

    /// <summary>DocumentDiscoveredEvent published for each discovered asset.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Publishes_DocumentDiscoveredEvent_Per_Asset()
    {
        var assets = new[]
        {
            MakeAsset("file1.md"),
            MakeAsset("file2.md"),
        };
        var bus = new FakeEventBus();
        var connector = new FakeConnectorWithReader(assets);
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(MakeDocument(a)));
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector)]),
            dispatcher,
            new FakeIndexEngine(),
            bus);

        await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        var discovered = bus.Published.OfType<DocumentDiscoveredEvent>().ToList();
        Assert.Equal(2, discovered.Count);
    }

    /// <summary>ForceRebuild calls ClearAsync once.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_ForceRebuild_Calls_ClearAsync()
    {
        var engine = new FakeIndexEngine();
        var pipeline = new IndexPipeline(new FakeConnectorManager([]), new FakeParserDispatcher(), engine, new FakeEventBus());

        await pipeline.RunAsync(WorkspaceId.Create("test"), new IndexPipelineOptions { ForceRebuild = true });

        Assert.Equal(1, engine.ClearCount);
    }

    /// <summary>Without ForceRebuild, ClearAsync is not called.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_No_ForceRebuild_Does_Not_Call_ClearAsync()
    {
        var engine = new FakeIndexEngine();
        var pipeline = new IndexPipeline(new FakeConnectorManager([]), new FakeParserDispatcher(), engine, new FakeEventBus());

        await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(0, engine.ClearCount);
    }

    /// <summary>Multiple connectors aggregate results correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RunAsync_Multiple_Connectors_Aggregates_Results()
    {
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(a => ParseResult<Document>.Success(MakeDocument(a)));
        var engine = new FakeIndexEngine();

        var connector1 = new FakeConnectorWithReader([MakeAsset("file1.md")]);
        var connector2 = new FakeConnectorWithReader([MakeAsset("file2.md"), MakeAsset("file3.md")]);

        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector1), MakeRuntime(connector2)]),
            dispatcher,
            engine,
            new FakeEventBus());

        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        Assert.Equal(3, result.AssetsDiscovered);
        Assert.Equal(3, result.DocumentsIndexed);
        Assert.Equal(3, engine.WrittenDocuments.Count);
    }

    /// <summary>OperationCanceledException propagates out of RunAsync.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OperationCanceledException_Propagates()
    {
        using var cts = new CancellationTokenSource();
        var dispatcher = new FakeParserDispatcher();
        dispatcher.SetResult(_ =>
        {
            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return ParseResult<Document>.Failed("unreachable");
        });

        var asset = MakeAsset("file.md", "text/markdown");
        var connector = new FakeConnectorWithReader([asset]);
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([MakeRuntime(connector)]),
            dispatcher,
            new FakeIndexEngine(),
            new FakeEventBus());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default, cts.Token));
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

    private static AssetDescriptor MakeAsset(string name, string mediaType = "text/plain") => new()
    {
        Id = new AssetId($"filesystem:///{name}"),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri($"filesystem:///{name}"),
        DisplayName = name,
        LastModified = DateTimeOffset.UtcNow,
        MediaType = mediaType,
    };

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

    // ── Inner fakes ──────────────────────────────────────────────────────────

    private sealed class FakeConnectorWithReader : IConnector, IAssetSource, IAssetReader
    {
        private readonly List<AssetDescriptor> _assets;
        private readonly Func<AssetDescriptor, Stream>? _streamFactory;

        internal FakeConnectorWithReader(
            IEnumerable<AssetDescriptor> assets,
            Func<AssetDescriptor, Stream>? streamFactory = null)
        {
            _assets = assets.ToList();
            _streamFactory = streamFactory;
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
            var stream = _streamFactory?.Invoke(asset)
                ?? new MemoryStream(System.Text.Encoding.UTF8.GetBytes("sample content"));
            return Task.FromResult(stream);
        }
    }

    private sealed class FakeConnectorSourceOnly : IConnector, IAssetSource
    {
        private readonly List<AssetDescriptor> _assets;

        internal FakeConnectorSourceOnly(IEnumerable<AssetDescriptor> assets)
        {
            _assets = assets.ToList();
        }

        public ConnectorType ConnectorType => ConnectorType.Filesystem;

        public ConnectorMetadata Metadata { get; } = ConnectorMetadata.Create(
            "source-only", "Source Only", "Test", ConnectorType.Filesystem, "1.0");

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
    }
}
