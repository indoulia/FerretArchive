#pragma warning disable CA1054 // MCP resource URIs use custom schemes (workspace://, index://), not HTTP
namespace Ferret.Mcp.Protocol;

/// <summary>Ferret-owned contract for an MCP resource implementation.</summary>
public interface IMcpResource
{
    /// <summary>Gets the descriptor that describes this resource to AI hosts.</summary>
    McpResourceDescriptor Descriptor { get; }

    /// <summary>Reads the resource content for the given URI.</summary>
    /// <param name="resourceUri">The resource URI being requested (e.g. "workspace://status").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resource content.</returns>
    Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct);
}
