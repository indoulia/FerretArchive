namespace Ferret.Mcp.Protocol;

/// <summary>A single content item in an MCP tool result or resource response.</summary>
public sealed record McpContent
{
    /// <summary>Gets the content type (e.g. "text").</summary>
    public required string Type { get; init; }

    /// <summary>Gets the text body, if any.</summary>
    public string? Text { get; init; }

    /// <summary>Creates a plain-text content item.</summary>
    /// <param name="text">Text body.</param>
    /// <returns>A new text content item.</returns>
    public static McpContent FromText(string text) => new() { Type = "text", Text = text };
}
