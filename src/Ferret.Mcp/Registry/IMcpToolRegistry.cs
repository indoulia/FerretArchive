using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

/// <summary>Read-only registry of MCP tools, built once at startup.</summary>
public interface IMcpToolRegistry
{
    /// <summary>Returns all registered tool descriptors.</summary>
    /// <returns>All registered descriptors.</returns>
    IReadOnlyList<McpToolDescriptor> GetAll();

    /// <summary>Returns the tool with the given <paramref name="name"/>, or <see langword="null"/> if not registered.</summary>
    /// <param name="name">Tool name (snake_case).</param>
    /// <returns>The matching tool, or <see langword="null"/>.</returns>
    IMcpTool? GetByName(string name);
}
