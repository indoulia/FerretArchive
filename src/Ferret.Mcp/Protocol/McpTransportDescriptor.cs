namespace Ferret.Mcp.Protocol;

/// <summary>Metadata that describes an MCP transport.</summary>
public sealed record McpTransportDescriptor
{
    /// <summary>Gets the transport name (e.g. "stdio").</summary>
    public required string Name { get; init; }

    /// <summary>Gets the human-readable description.</summary>
    public required string Description { get; init; }
}
