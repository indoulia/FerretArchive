using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

/// <summary>Immutable registry of MCP resources, built once at startup.</summary>
internal sealed class McpResourceRegistry : IMcpResourceRegistry
{
    private readonly IReadOnlyList<McpResourceDescriptor> _descriptors;
    private readonly Dictionary<string, IMcpResource> _byUri;

    internal McpResourceRegistry(IEnumerable<IMcpResource> resources)
    {
        var list = resources.ToList();
        _descriptors = list.Select(r => r.Descriptor).ToList();
        _byUri = list.ToDictionary(r => r.Descriptor.ResourceUri, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public IReadOnlyList<McpResourceDescriptor> GetAll() => _descriptors;

    /// <inheritdoc/>
    public IMcpResource? GetByUri(string resourceUri) =>
        _byUri.TryGetValue(resourceUri, out var resource) ? resource : null;
}
