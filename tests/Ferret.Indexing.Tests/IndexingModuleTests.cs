using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Fakes;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Ferret.Indexing.Tests;

/// <summary>Unit tests for <see cref="IndexingModule"/>.</summary>
public sealed class IndexingModuleTests
{
    /// <summary>IIndexPipeline resolves as singleton after all dependencies are registered.</summary>
    [Fact]
    public void ConfigureServices_Registers_IIndexPipeline_As_Singleton()
    {
        var services = new ServiceCollection();

        // Register all IndexPipeline constructor dependencies
        services.AddSingleton<IIndexEngine, FakeIndexEngine>();
        services.AddSingleton<IParserDispatcher, FakeParserDispatcher>();
        services.AddSingleton<IEventBus, FakeEventBus>();
        services.AddSingleton<IConnectorManager>(new FakeConnectorManager([]));
        services.AddSingleton<Ferret.Core.Workspace.IWorkspaceContext>(new FakeWorkspaceContext());

        // Register the module
        IndexingModule.ConfigureServices(services);

        using var sp = services.BuildServiceProvider();
        var pipeline = sp.GetService<IIndexPipeline>();

        Assert.NotNull(pipeline);
    }

    /// <summary>IIndexPipeline is registered as singleton — same instance returned twice.</summary>
    [Fact]
    public void ConfigureServices_IIndexPipeline_Is_Singleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIndexEngine, FakeIndexEngine>();
        services.AddSingleton<IParserDispatcher, FakeParserDispatcher>();
        services.AddSingleton<IEventBus, FakeEventBus>();
        services.AddSingleton<IConnectorManager>(new FakeConnectorManager([]));
        services.AddSingleton<Ferret.Core.Workspace.IWorkspaceContext>(new FakeWorkspaceContext());

        IndexingModule.ConfigureServices(services);

        using var sp = services.BuildServiceProvider();
        var p1 = sp.GetRequiredService<IIndexPipeline>();
        var p2 = sp.GetRequiredService<IIndexPipeline>();

        Assert.Same(p1, p2);
    }

    /// <summary>IIndexEngine is not registered by IndexingModule.</summary>
    [Fact]
    public void IIndexEngine_Is_Not_Registered_By_IndexingModule()
    {
        var services = new ServiceCollection();

        IndexingModule.ConfigureServices(services);

        using var sp = services.BuildServiceProvider();
        var engine = sp.GetService<IIndexEngine>();

        Assert.Null(engine);
    }

    /// <summary>Pipeline resolves even when connector manager has no active connectors.</summary>
    [Fact]
    public void ConfigureServices_Accepts_Empty_ConnectorManager()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIndexEngine, FakeIndexEngine>();
        services.AddSingleton<IParserDispatcher, FakeParserDispatcher>();
        services.AddSingleton<IEventBus, FakeEventBus>();
        services.AddSingleton<IConnectorManager>(new FakeConnectorManager([]));
        services.AddSingleton<Ferret.Core.Workspace.IWorkspaceContext>(new FakeWorkspaceContext());

        // No active connectors — pipeline should still resolve
        IndexingModule.ConfigureServices(services);

        using var sp = services.BuildServiceProvider();
        var pipeline = sp.GetRequiredService<IIndexPipeline>();

        Assert.NotNull(pipeline);
    }

    /// <summary>ConfigureServices returns the same IServiceCollection for chaining.</summary>
    [Fact]
    public void ConfigureServices_Returns_Same_ServiceCollection()
    {
        var services = new ServiceCollection();
        var returned = IndexingModule.ConfigureServices(services);

        Assert.Same(services, returned);
    }

    /// <summary>FakeConnectorManager returns empty by default.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task FakeConnectorManager_GetActiveConnectorsAsync_Returns_Empty_By_Default()
    {
        var manager = new FakeConnectorManager([]);
        var runtimes = await manager.GetActiveConnectorsAsync();
        Assert.Empty(runtimes);
    }

    /// <summary>FakeAssetSourceReader implements both source and reader interfaces.</summary>
    [Fact]
    public void FakeAssetSourceReader_Implements_Both_Interfaces()
    {
        var reader = new FakeAssetSourceReader([]);
        Assert.IsAssignableFrom<IAssetSource>(reader);
        Assert.IsAssignableFrom<IAssetReader>(reader);
    }
}
