#pragma warning disable CA1054 // MCP resource URIs use custom schemes (workspace://, index://), not HTTP
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

/// <summary>Read-only registry of MCP resources, built once at startup.</summary>
public interface IMcpResourceRegistry
{
    /// <summary>Returns all registered resource descriptors.</summary>
    /// <returns>All registered descriptors.</returns>
    IReadOnlyList<McpResourceDescriptor> GetAll();

    /// <summary>Returns the resource with the given <paramref name="resourceUri"/>, or <see langword="null"/> if not registered.</summary>
    /// <param name="resourceUri">Resource URI (e.g. "workspace://status").</param>
    /// <returns>The matching resource, or <see langword="null"/>.</returns>
    IMcpResource? GetByUri(string resourceUri);
}
