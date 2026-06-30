namespace Ferret.Mcp.Protocol;

/// <summary>Ferret-owned contract for an MCP tool implementation.</summary>
public interface IMcpTool
{
    /// <summary>Gets the descriptor that describes this tool to AI hosts.</summary>
    McpToolDescriptor Descriptor { get; }

    /// <summary>Executes the tool with the given arguments.</summary>
    /// <param name="arguments">Parsed invocation arguments.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The tool execution result.</returns>
    Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct);
}
