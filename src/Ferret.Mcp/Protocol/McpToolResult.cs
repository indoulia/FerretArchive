namespace Ferret.Mcp.Protocol;

/// <summary>Result returned by an <see cref="IMcpTool"/> invocation.</summary>
public sealed record McpToolResult
{
    /// <summary>Gets the content items.</summary>
    public required IReadOnlyList<McpContent> Content { get; init; }

    /// <summary>Gets a value indicating whether this result represents an error.</summary>
    public bool IsError { get; init; }

    /// <summary>Gets a value indicating whether this result is successful.</summary>
    public bool IsSuccess => !IsError;

    /// <summary>Creates a successful result with text content.</summary>
    /// <param name="text">Result text.</param>
    /// <returns>A successful <see cref="McpToolResult"/>.</returns>
    public static McpToolResult Success(string text) =>
        new() { Content = [McpContent.FromText(text)], IsError = false };

    /// <summary>Creates an error result with a message.</summary>
    /// <param name="message">Error message.</param>
    /// <returns>An error <see cref="McpToolResult"/>.</returns>
    public static McpToolResult Error(string message) =>
        new() { Content = [McpContent.FromText(message)], IsError = true };
}
