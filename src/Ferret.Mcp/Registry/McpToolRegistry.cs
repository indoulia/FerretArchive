using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

/// <summary>Immutable registry of MCP tools, built once at startup.</summary>
internal sealed class McpToolRegistry : IMcpToolRegistry
{
    private readonly IReadOnlyList<McpToolDescriptor> _descriptors;
    private readonly Dictionary<string, IMcpTool> _byName;

    internal McpToolRegistry(IEnumerable<IMcpTool> tools)
    {
        var list = tools.ToList();
        _descriptors = list.Select(t => t.Descriptor).ToList();
        _byName = list.ToDictionary(t => t.Descriptor.Name, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public IReadOnlyList<McpToolDescriptor> GetAll() => _descriptors;

    /// <inheritdoc/>
    public IMcpTool? GetByName(string name) =>
        _byName.TryGetValue(name, out var tool) ? tool : null;
}
