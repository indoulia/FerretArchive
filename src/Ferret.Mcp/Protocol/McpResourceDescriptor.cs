#pragma warning disable CA1056 // MCP resource URIs use custom schemes (workspace://, index://), not HTTP
namespace Ferret.Mcp.Protocol;

/// <summary>Metadata that describes an MCP resource.</summary>
public sealed record McpResourceDescriptor
{
    /// <summary>Gets the resource URI (e.g. "workspace://status").</summary>
    public required string ResourceUri { get; init; }

    /// <summary>Gets the resource name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the human-readable description shown to AI hosts.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the MIME type of the resource content.</summary>
    public string MimeType { get; init; } = "application/json";
}
