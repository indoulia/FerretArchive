using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Registry;

/// <summary>Fluent builder for constructing an immutable <see cref="IMcpResourceRegistry"/>.</summary>
internal sealed class McpResourceRegistryBuilder
{
    private readonly List<IMcpResource> _resources = [];

    /// <summary>Adds <paramref name="resource"/> to the registry being built.</summary>
    /// <param name="resource">Resource to register.</param>
    /// <returns>This builder for chaining.</returns>
    internal McpResourceRegistryBuilder Add(IMcpResource resource)
    {
        _resources.Add(resource);
        return this;
    }

    /// <summary>Builds and returns the immutable registry.</summary>
    /// <returns>An immutable <see cref="IMcpResourceRegistry"/>.</returns>
    internal IMcpResourceRegistry Build() => new McpResourceRegistry(_resources);
}
