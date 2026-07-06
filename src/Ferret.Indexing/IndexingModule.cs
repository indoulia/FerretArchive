using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Events;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Indexing;

/// <summary>Registers Ferret.Indexing services into a <see cref="IServiceCollection"/>.
/// Callers must separately register <see cref="IIndexEngine"/>, <see cref="IParserDispatcher"/>,
/// <see cref="IEventBus"/>, <see cref="IConnectorManager"/>, and <see cref="IWorkspaceContext"/>.</summary>
public static class IndexingModule
{
    /// <summary>Registers <see cref="IIndexPipeline"/> as a singleton backed by <see cref="IndexPipeline"/>
    /// and <see cref="IIndexStateStore"/> as a singleton backed by <see cref="JsonIndexStateStore"/>.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IIndexStateStore>(sp =>
        {
            var workspace = sp.GetRequiredService<IWorkspaceContext>();
            var stateFilePath = Path.Join(
                workspace.WorkspaceRoot.FullPath,
                WorkspaceLayout.RootDirectoryName,
                IndexLayout.StateFileName);
            return new JsonIndexStateStore(stateFilePath);
        });

        services.AddSingleton<IIndexPipeline>(sp =>
        {
            var connectorManager = sp.GetRequiredService<IConnectorManager>();
            var dispatcher = sp.GetRequiredService<IParserDispatcher>();
            var engine = sp.GetRequiredService<IIndexEngine>();
            var bus = sp.GetRequiredService<IEventBus>();
            var stateStore = sp.GetRequiredService<IIndexStateStore>();
            var workspace = sp.GetRequiredService<IWorkspaceContext>();
            return new IndexPipeline(connectorManager, dispatcher, engine, bus, stateStore, workspace.WorkspaceRoot.FullPath);
        });

        return services;
    }
}
