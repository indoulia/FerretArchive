using Ferret.Mcp.Protocol;
using Ferret.Mcp.Registry;

namespace Ferret.Mcp.Runtime;

/// <summary>Wires registered tools and resources into the configured transport and runs the MCP server.</summary>
public sealed class McpRuntime : IMcpRuntime
{
    private readonly IEnumerable<IMcpTool> _tools;
    private readonly IEnumerable<IMcpResource> _resources;
    private readonly IMcpTransport _transport;

    /// <summary>Initializes a new instance of the <see cref="McpRuntime"/> class.</summary>
    /// <param name="tools">All registered MCP tools.</param>
    /// <param name="resources">All registered MCP resources.</param>
    /// <param name="transport">Transport to run the MCP server on.</param>
    public McpRuntime(
        IEnumerable<IMcpTool> tools,
        IEnumerable<IMcpResource> resources,
        IMcpTransport transport)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(transport);
        _tools = tools;
        _resources = resources;
        _transport = transport;
    }

    /// <inheritdoc/>
    public Task RunAsync(CancellationToken ct)
    {
        var toolRegistry = BuildToolRegistry();
        var resourceRegistry = BuildResourceRegistry();
        return _transport.RunAsync(toolRegistry, resourceRegistry, ct);
    }

    private IMcpToolRegistry BuildToolRegistry()
    {
        var builder = new McpToolRegistryBuilder();
        foreach (var tool in _tools)
        {
            builder.Add(tool);
        }

        return builder.Build();
    }

    private IMcpResourceRegistry BuildResourceRegistry()
    {
        var builder = new McpResourceRegistryBuilder();
        foreach (var resource in _resources)
        {
            builder.Add(resource);
        }

        return builder.Build();
    }
}
