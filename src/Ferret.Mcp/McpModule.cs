using Ferret.Mcp.Protocol;
using Ferret.Mcp.Resources;
using Ferret.Mcp.Runtime;
using Ferret.Mcp.Tools;
using Ferret.Mcp.Transport.Stdio;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ferret.Mcp;

/// <summary>Registers Ferret.Mcp services into a <see cref="IServiceCollection"/>.</summary>
public static class McpModule
{
    /// <summary>Registers MCP tools, resources, transport, and runtime as singletons.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMcpTool, SearchTool>();
        services.AddSingleton<IMcpTool, ReadDocumentTool>();
        services.AddSingleton<IMcpTool, WorkspaceStatusTool>();
        services.AddSingleton<IMcpTool, ContextTool>();
        services.AddSingleton<IMcpTool, WorkspaceListTool>();

        services.AddSingleton<IMcpResource, WorkspaceStatusResource>();
        services.AddSingleton<IMcpResource, IndexStatsResource>();
        services.AddSingleton<IMcpResource, ConnectorsResource>();

        services.TryAddSingleton<IMcpTransport, StdioTransport>();
        services.TryAddSingleton<IMcpRuntime, McpRuntime>();

        return services;
    }
}
