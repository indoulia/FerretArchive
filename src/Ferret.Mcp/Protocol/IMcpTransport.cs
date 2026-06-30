using Ferret.Mcp.Registry;

namespace Ferret.Mcp.Protocol;

/// <summary>Ferret-owned contract for an MCP transport (e.g. stdio).</summary>
public interface IMcpTransport
{
    /// <summary>Gets the descriptor that identifies this transport.</summary>
    McpTransportDescriptor Descriptor { get; }

    /// <summary>Starts the transport and serves requests until <paramref name="ct"/> is cancelled.</summary>
    /// <param name="tools">Registry of available tools.</param>
    /// <param name="resources">Registry of available resources.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the transport shuts down.</returns>
    Task RunAsync(IMcpToolRegistry tools, IMcpResourceRegistry resources, CancellationToken ct);
}
