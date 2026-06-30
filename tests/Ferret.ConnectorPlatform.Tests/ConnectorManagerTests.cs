using Ferret.ConnectorPlatform;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

/// <summary>Unit tests for <see cref="ConnectorManager"/>.</summary>
public sealed class ConnectorManagerTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorManagerTests"/> class.</summary>
    public ConnectorManagerTests()
    {
        _tmpDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tmpDir);
        _root = WorkspacePath.Create(_tmpDir);
        _store = new ConnectorInstanceStore();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
        {
            Directory.Delete(_tmpDir, recursive: true);
        }
    }

    /// <summary>No instances → empty result.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Returns_Empty_When_No_Instances()
    {
        using var manager = new ConnectorManager(_store, [], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Empty(runtimes);
    }

    /// <summary>Enabled instance with matching factory → single runtime.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Returns_Runtime_For_Enabled_Instance()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Default",
        };
        await _store.SaveAsync(_root, [instance]);

        var factory = new FakeConnectorManagerFactory("fake");
        using var manager = new ConnectorManager(_store, [factory], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Single(runtimes);
        Assert.Equal("default", runtimes[0].Instance.Id.Value);
    }

    /// <summary>Disabled instances are excluded from the active list.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Skips_Disabled_Instances()
    {
        var enabled = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("enabled"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Enabled",
            IsEnabled = true,
        };
        var disabled = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("disabled"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Disabled",
            IsEnabled = false,
        };
        await _store.SaveAsync(_root, [enabled, disabled]);

        var factory = new FakeConnectorManagerFactory("fake");
        using var manager = new ConnectorManager(_store, [factory], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Single(runtimes);
        Assert.Equal("enabled", runtimes[0].Instance.Id.Value);
    }

    /// <summary>Instances with no matching factory are silently skipped.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Skips_Unknown_ConnectorType()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("x"),
            ConnectorType = new ConnectorId("unknown-type"),
            DisplayName = "X",
        };
        await _store.SaveAsync(_root, [instance]);
        using var manager = new ConnectorManager(_store, [], _root); // no factories

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Empty(runtimes);
    }

    /// <summary>Second call returns the same cached runtime object.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Returns_Same_Cached_Runtime_On_Second_Call()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Default",
        };
        await _store.SaveAsync(_root, [instance]);

        var factory = new FakeConnectorManagerFactory("fake");
        using var manager = new ConnectorManager(_store, [factory], _root);

        var first = await manager.GetActiveConnectorsAsync();
        var second = await manager.GetActiveConnectorsAsync();

        Assert.Same(first[0], second[0]);
    }

    /// <summary>Empty store with a filesystem factory → synthesized default connector rooted at the workspace.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Synthesizes_Default_Filesystem_When_Store_Empty()
    {
        var factory = new FakeConnectorManagerFactory("filesystem");
        using var manager = new ConnectorManager(_store, [factory], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Single(runtimes);
        Assert.Equal("default", runtimes[0].Instance.Id.Value);
        Assert.Equal("filesystem", runtimes[0].Instance.ConnectorType.Value);
        Assert.Equal(_root.FullPath, runtimes[0].Instance.Configuration.GetValue("rootPath"));
    }

    /// <summary>Empty store with no filesystem factory registered → no synthesized default.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetActiveConnectorsAsync_Does_Not_Synthesize_When_No_Filesystem_Factory()
    {
        var factory = new FakeConnectorManagerFactory("fake");
        using var manager = new ConnectorManager(_store, [factory], _root);

        var runtimes = await manager.GetActiveConnectorsAsync();

        Assert.Empty(runtimes);
    }

    /// <summary>Unknown ID returns null.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetInstanceAsync_Returns_Null_For_Unknown_Id()
    {
        using var manager = new ConnectorManager(_store, [], _root);

        var instance = await manager.GetInstanceAsync(new ConnectorInstanceId("nonexistent"));

        Assert.Null(instance);
    }

    /// <summary>Known ID returns matching instance.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetInstanceAsync_Returns_Instance_By_Id()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("my-instance"),
            ConnectorType = new ConnectorId("fake"),
            DisplayName = "Mine",
        };
        await _store.SaveAsync(_root, [instance]);
        using var manager = new ConnectorManager(_store, [], _root);

        var loaded = await manager.GetInstanceAsync(new ConnectorInstanceId("my-instance"));

        Assert.NotNull(loaded);
        Assert.Equal("my-instance", loaded!.Id.Value);
    }

    /// <summary>Dispose does not throw.</summary>
    [Fact]
    public void Dispose_Does_Not_Throw()
    {
        static ConnectorManager Create(IConnectorInstanceStore store, WorkspacePath root) =>
            new(store, [], root);

        var ex = Record.Exception(() =>
        {
            using var manager = Create(_store, _root);
        });
        Assert.Null(ex);
    }

    // ---- Inner fakes ----

    private sealed class FakeConnectorManagerFactory : IConnectorFactory
    {
        internal FakeConnectorManagerFactory(string connectorId)
        {
            ConnectorId = new ConnectorId(connectorId);
            Descriptor = new ConnectorDescriptor
            {
                Id = ConnectorId,
                Metadata = ConnectorMetadata.Create(connectorId, connectorId, $"{connectorId} connector", ConnectorType.Custom, "1.0"),
                Capabilities = [],
                SupportedPlatforms = [],
            };
        }

        public ConnectorId ConnectorId { get; }

        public ConnectorDescriptor Descriptor { get; }

        public IConnector Create(ConnectorInstance instance) =>
            new FakeConnector(instance.Id);
    }

    private sealed class FakeConnector : IConnector
    {
        internal FakeConnector(ConnectorInstanceId id)
        {
            ConnectorType = ConnectorType.Custom;
            Metadata = ConnectorMetadata.Create(id.Value, id.Value, "fake", ConnectorType.Custom, "1.0");
            Capabilities = ConnectorIoCapabilities.ReadOnly();
        }

        public ConnectorType ConnectorType { get; }

        public ConnectorMetadata Metadata { get; }

        public ConnectorIoCapabilities Capabilities { get; }

        public Task<ConnectorHealth> GetHealthAsync(CancellationToken ct = default) =>
            Task.FromResult(ConnectorHealth.Connected(DateTimeOffset.UtcNow));

        public Task<IConnectorSession> ConnectAsync(CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task DisconnectAsync(CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
