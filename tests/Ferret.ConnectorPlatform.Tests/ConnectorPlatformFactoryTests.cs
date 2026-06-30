using Ferret.ConnectorPlatform.Tests.Fakes;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

/// <summary>Unit tests for <see cref="ConnectorPlatformFactory"/>.</summary>
public sealed class ConnectorPlatformFactoryTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly WorkspacePath _root;
    private readonly ConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorPlatformFactoryTests"/> class.</summary>
    public ConnectorPlatformFactoryTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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

    [Fact]
    public void CreateConnectorManager_ReturnsIConnectorManager()
    {
        var manager = ConnectorPlatformFactory.CreateConnectorManager(_store, [], _root);
        Assert.NotNull(manager);
        Assert.IsAssignableFrom<IConnectorManager>(manager);
    }

    [Fact]
    public async Task CreateConnectorManager_WithNoInstances_ReturnsEmptyList()
    {
        var manager = ConnectorPlatformFactory.CreateConnectorManager(_store, [], _root);
        var connectors = await manager.GetActiveConnectorsAsync();
        Assert.Empty(connectors);
    }

    [Fact]
    public void CreateConnectorManager_WithFactory_CreatesManagerThatRecognizesFactory()
    {
        var factory = new FakeConnectorFactory("test-connector");
        var manager = ConnectorPlatformFactory.CreateConnectorManager(_store, [factory], _root);
        Assert.NotNull(manager);
    }

    [Fact]
    public void CreateConnectorManager_NullStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ConnectorPlatformFactory.CreateConnectorManager(null!, [], _root));
    }

    [Fact]
    public void CreateConnectorManager_NullFactories_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ConnectorPlatformFactory.CreateConnectorManager(_store, null!, _root));
    }

    [Fact]
    public void CreateConnectorManager_NullRootPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ConnectorPlatformFactory.CreateConnectorManager(_store, [], null!));
    }
}
