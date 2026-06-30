using System.Text.Json;

using Ferret.Core.Connectors;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Resources;

/// <summary>MCP resource that lists registered Ferret connectors.</summary>
public sealed class ConnectorsResource : IMcpResource
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IConnectorRegistry _connectorRegistry;

    /// <summary>Initializes a new instance of the <see cref="ConnectorsResource"/> class.</summary>
    /// <param name="connectorRegistry">Registry of registered connectors.</param>
    public ConnectorsResource(IConnectorRegistry connectorRegistry)
    {
        ArgumentNullException.ThrowIfNull(connectorRegistry);
        _connectorRegistry = connectorRegistry;
    }

    /// <inheritdoc/>
    public McpResourceDescriptor Descriptor { get; } = new()
    {
        ResourceUri = "workspace://connectors",
        Name = "connectors",
        Description = "Registered Ferret connectors and their capabilities.",
    };

    /// <inheritdoc/>
    public Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(resourceUri);

        var connectors = _connectorRegistry.GetAll()
            .Select(d => new
            {
                id = d.Id.Value,
                name = d.Metadata.Name,
                connectorType = d.Metadata.ConnectorType.ToString(),
            })
            .ToList();

        var text = JsonSerializer.Serialize(connectors, JsonOptions);
        return Task.FromResult(new McpResourceContent
        {
            ResourceUri = resourceUri,
            MimeType = "application/json",
            Text = text,
        });
    }
}
