# MCP Design — [Server / Client Name]

| Field | Value |
|---|---|
| **Status** | Draft \| Review \| Accepted |
| **Role** | MCP Server \| MCP Client |
| **Protocol Version** | MCP 1.x |
| **Transport** | stdio \| HTTP/SSE \| WebSocket |
| **Author** | [name] |
| **Date** | YYYY-MM-DD |

---

## Purpose

<!--
What does this MCP server/client expose or consume?
One paragraph.
-->

## Capabilities

### Tools Exposed (if server)

| Tool Name | Description | Input Schema | Output Schema |
|---|---|---|---|
| `tool_name` | | `{...}` | `{...}` |

### Resources Exposed (if server)

| Resource URI Pattern | MIME Type | Description |
|---|---|---|
| `ferret://agents/{id}` | `application/json` | |

### Prompts (if server)

| Prompt Name | Description | Arguments |
|---|---|---|
| | | |

### Sampling (if server)

- [ ] This server supports MCP sampling (LLM calls back through the client)

---

## Transport Configuration

### stdio

```json
{
  "mcpServers": {
    "ferret": {
      "command": "dotnet",
      "args": ["run", "--project", "src/Ferret.Mcp.Server"],
      "env": {
        "FERRET_API_KEY": "${FERRET_API_KEY}"
      }
    }
  }
}
```

### HTTP/SSE

```
Base URL:  http://localhost:5200/mcp
SSE path:  /sse
POST path: /message
```

---

## Authentication

<!--
How does the client authenticate to this server?
-->

## Error Handling

| MCP Error Code | Meaning | Retry? |
|---|---|---|
| `-32700` | Parse error | No |
| `-32600` | Invalid request | No |
| `-32601` | Method not found | No |
| `-32602` | Invalid params | No |
| `-32603` | Internal error | Yes (with backoff) |

## Observability

| Signal | What is emitted |
|---|---|
| Logs | Tool call in/out, errors |
| Metrics | `mcp.tool.calls.total`, `mcp.tool.duration_ms` |
| Traces | Span per tool call |

---

_Template version: 1.0 — stored in `/templates/mcp.md`_
