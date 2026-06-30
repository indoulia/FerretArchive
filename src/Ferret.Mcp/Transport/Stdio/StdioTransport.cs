using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Ferret.Mcp.Transport.Stdio;

/// <summary>MCP transport that communicates over standard input/output.</summary>
public sealed class StdioTransport : IMcpTransport
{
    private static readonly Implementation FerretServerInfo = new() { Name = "Ferret", Version = "0.11.0" };

    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Initializes a new instance of the <see cref="StdioTransport"/> class.</summary>
    /// <param name="loggerFactory">Logger factory for SDK logging.</param>
    public StdioTransport(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public McpTransportDescriptor Descriptor { get; } = new()
    {
        Name = "stdio",
        Description = "MCP transport over standard input/output (stdio).",
    };

    /// <inheritdoc/>
    public async Task RunAsync(IMcpToolRegistry tools, IMcpResourceRegistry resources, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(resources);

        var errorMapper = new McpErrorMapper();
        var options = BuildOptions(tools, resources, errorMapper);

        var transport = new StdioServerTransport(options, _loggerFactory);
#pragma warning disable CA2007 // IAsyncDisposable.DisposeAsync does not support ConfigureAwait
        await using (transport)
        {
            var server = McpServer.Create(transport, options, _loggerFactory);
            await using (server)
            {
                await server.RunAsync(ct).ConfigureAwait(false);
            }
        }
#pragma warning restore CA2007
    }

    private static McpServerOptions BuildOptions(
        IMcpToolRegistry tools,
        IMcpResourceRegistry resources,
        IMcpErrorMapper errorMapper)
    {
        return new McpServerOptions
        {
            ServerInfo = FerretServerInfo,
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability(),
                Resources = new ResourcesCapability(),
            },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = SdkToolAdapter.CreateListHandler(tools),
                CallToolHandler = SdkToolAdapter.CreateCallHandler(tools, errorMapper),
                ListResourcesHandler = SdkResourceAdapter.CreateListHandler(resources),
                ReadResourceHandler = SdkResourceAdapter.CreateReadHandler(resources),
            },
        };
    }
}
