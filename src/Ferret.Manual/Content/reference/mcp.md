# MCP Reference

Ferret exposes four MCP tools when running as `ferret serve`. All tools use the stdio transport (JSON-RPC over stdin/stdout).

## ferret_search

Search the workspace index for documents matching a query.

### Input Schema

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string",
      "description": "Full-text search query"
    },
    "max_results": {
      "type": "integer",
      "description": "Maximum results to return (default: 10)"
    }
  },
  "required": ["query"]
}
```

### Output

Plain text listing of results with scores and snippets.

### Example

```json
{
  "tool": "ferret_search",
  "arguments": {
    "query": "IIndexPipeline",
    "max_results": 5
  }
}
```

Response:
```
Found 3 result(s) for: IIndexPipeline

[1] src/Ferret.Core/Indexing/IIndexPipeline.cs
    URI: filesystem:///src/Ferret.Core/Indexing/IIndexPipeline.cs
    Score: 0.940
    Orchestrates a complete discover → parse → index pipeline run.

[2] src/Ferret.Indexing/IndexPipeline.cs
    URI: filesystem:///src/Ferret.Indexing/IndexPipeline.cs
    Score: 0.870
    public sealed class IndexPipeline : IIndexPipeline
```

---

## ferret_read_document

Read the full content of a document by its canonical URI.

### Input Schema

```json
{
  "type": "object",
  "properties": {
    "uri": {
      "type": "string",
      "description": "Canonical URI of the document (from ferret_search results)"
    }
  },
  "required": ["uri"]
}
```

### Output

Full document content as plain text.

### Example

```json
{
  "tool": "ferret_read_document",
  "arguments": {
    "uri": "filesystem:///src/Ferret.Core/Indexing/IIndexPipeline.cs"
  }
}
```

---

## ferret_context

Assemble a context package for a query. Runs the full Context Assembly pipeline: search → deduplicate → filter → expand → token budget → format.

### Input Schema

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string",
      "description": "Natural-language or keyword query"
    },
    "token_budget": {
      "type": "integer",
      "description": "Maximum tokens in the assembled context (default: 8000)"
    }
  },
  "required": ["query"]
}
```

### Output

An XML-wrapped context package containing the most relevant documents within the token budget.

### Example

```json
{
  "tool": "ferret_context",
  "arguments": {
    "query": "how does file watching work",
    "token_budget": 4000
  }
}
```

---

## ferret_workspace_status

Return the current status of the workspace: document count, last indexed time, connector health.

### Input Schema

```json
{
  "type": "object",
  "properties": {}
}
```

No input required.

### Output

Plain text workspace status summary.

### Example

```json
{
  "tool": "ferret_workspace_status",
  "arguments": {}
}
```

Response:
```
Workspace: my-project
Documents: 1,231
Last indexed: 2026-06-29T10:00:00Z
Connectors:
  filesystem:default — healthy (1,247 assets)
```

## Related

- [Connect Claude](../getting-started/connect-claude) — wiring MCP to Claude Desktop
- [MCP Runtime Architecture](../architecture/mcp-runtime) — how the server works
- [Context Assembly](../architecture/context-assembly) — what ferret_context does
