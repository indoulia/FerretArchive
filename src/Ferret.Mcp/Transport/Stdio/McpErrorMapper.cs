using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Transport.Stdio;

/// <summary>Maps .NET exceptions to <see cref="McpToolResult"/> error responses.</summary>
internal sealed class McpErrorMapper : IMcpErrorMapper
{
    /// <inheritdoc/>
    public McpToolResult MapException(Exception ex) => ex switch
    {
        ArgumentException argEx => McpToolResult.Error($"Invalid argument: {argEx.Message}"),
        InvalidOperationException opEx => McpToolResult.Error($"Operation not valid: {opEx.Message}"),
        _ => McpToolResult.Error($"An unexpected error occurred: {ex.Message}"),
    };
}
