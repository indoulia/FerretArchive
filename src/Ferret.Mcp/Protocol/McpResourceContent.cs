#pragma warning disable CA1056 // MCP resource URIs use custom schemes (workspace://, index://), not HTTP
namespace Ferret.Mcp.Protocol;

/// <summary>Content returned when an MCP resource is read.</summary>
public sealed record McpResourceContent
{
    /// <summary>Gets the resource URI that was read.</summary>
    public required string ResourceUri { get; init; }

    /// <summary>Gets the MIME type of the content.</summary>
    public required string MimeType { get; init; }

    /// <summary>Gets the text body.</summary>
    public required string Text { get; init; }
}
