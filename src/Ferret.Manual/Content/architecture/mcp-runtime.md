# MCP Runtime

Ferret's MCP server is a stdio-based runtime that exposes workspace capabilities to AI assistants. The architecture follows the Ferret Host Architecture Pattern: capabilities are platform-owned; the MCP layer is an adapter.

## Startup Sequence

```
ferret serve
     │
     ▼
1. DI container built
   - IMcpTool registrations collected (SearchTool, ReadDocumentTool,
     WorkspaceStatusTool, ContextTool)
   - IMcpToolRegistry built from IEnumerable<IMcpTool>
   - Registry is immutable after this point (ADR-0017)
     │
     ▼
2. McpRuntime.RunAsync()
   - Validates registry (empty tool registry = startup error)
   - Initializes StdioTransport
   - Starts reading JSON-RPC messages from stdin
     │
     ▼
3. Request arrives on stdin
   - StdioTransport reads JSON line
   - McpArgumentsFactory deserializes arguments
   - Runtime dispatches to matching IMcpTool
     │
     ▼
4. IMcpTool.ExecuteAsync(arguments, ct)
   - Tool calls platform service (ISearchService, etc.)
   - Returns McpToolResult
     │
     ▼
5. SdkToolAdapter translates McpToolResult → SDK CallToolResult
   - Writes JSON-RPC response to stdout
```

## IMcpTool Interface

```csharp
public interface IMcpTool
{
    McpToolDescriptor Descriptor { get; }
    Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct);
}
```

Tools are registered via DI and collected at startup:

```csharp
services.AddSingleton<IMcpTool, SearchTool>();
services.AddSingleton<IMcpTool, ReadDocumentTool>();
services.AddSingleton<IMcpTool, WorkspaceStatusTool>();
```

## SDK Isolation Boundary

```
┌─────────────────────────────────────────────────┐
│  Ferret.Mcp (above the line)                    │
│  IMcpTool · McpArguments · McpToolResult        │
│  McpRuntime · IMcpToolRegistry                  │
├─────────────────────────────────────────────────┤
│  Transport/Stdio/ (SDK boundary)                │
│  StdioTransport · SdkToolAdapter                │
│  McpArgumentsFactory · McpErrorMapper           │
│  (only files that import ModelContextProtocol.*) │
└─────────────────────────────────────────────────┘
```

No type from `ModelContextProtocol.*` appears outside `Transport/Stdio/`. Architecture tests enforce this.

## Related

- [Why MCP Before REST?](../design/why-mcp) — the design rationale
- [MCP Reference](../reference/mcp) — all tools and schemas
- [Platform Overview](platform-overview) — where MCP fits in the layer stack
