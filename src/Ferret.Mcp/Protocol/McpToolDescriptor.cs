namespace Ferret.Mcp.Protocol;

/// <summary>Metadata that describes an MCP tool.</summary>
public sealed record McpToolDescriptor
{
    /// <summary>Gets the tool name (snake_case).</summary>
    public required string Name { get; init; }

    /// <summary>Gets the human-readable description shown to AI hosts.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the JSON Schema for the tool's input arguments, or <see langword="null"/> if the tool takes no arguments.</summary>
    public string? InputSchemaJson { get; init; }
}
