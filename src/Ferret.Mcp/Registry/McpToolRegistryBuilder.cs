using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

/// <summary>Fluent builder for constructing an immutable <see cref="IMcpToolRegistry"/>.</summary>
internal sealed class McpToolRegistryBuilder
{
    private readonly List<IMcpTool> _tools = [];

    /// <summary>Adds <paramref name="tool"/> to the registry being built.</summary>
    /// <param name="tool">Tool to register.</param>
    /// <returns>This builder for chaining.</returns>
    internal McpToolRegistryBuilder Add(IMcpTool tool)
    {
        _tools.Add(tool);
        return this;
    }

    /// <summary>Builds and returns the immutable registry.</summary>
    /// <returns>An immutable <see cref="IMcpToolRegistry"/>.</returns>
    internal IMcpToolRegistry Build() => new McpToolRegistry(_tools);
}
