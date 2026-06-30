namespace Ferret.Mcp.Protocol;

/// <summary>Maps exceptions to <see cref="McpToolResult"/> error results.</summary>
public interface IMcpErrorMapper
{
    /// <summary>Converts <paramref name="ex"/> to an error <see cref="McpToolResult"/>.</summary>
    /// <param name="ex">The exception to map.</param>
    /// <returns>An error <see cref="McpToolResult"/> representing the exception.</returns>
    McpToolResult MapException(Exception ex);
}
