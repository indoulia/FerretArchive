# Context

Context Assembly builds an AI-ready `ContextPackage` from your workspace. Unlike raw search results, a context package is deduplicated, filtered, expanded, and token-budgeted — ready to include directly in an AI prompt.

## When to use context vs search

| Use Case | Tool |
|---|---|
| Find specific files or symbols | `ferret search` / `ferret_search` |
| Provide background to an AI assistant | `ferret_context` |
| Understand a system component | `ferret_context` |
| Script-driven result processing | `ferret search --json` |

## How it works

`ferret_context` runs the Context Assembly pipeline:

1. **Search** — BM25 search for the query
2. **Deduplicate** — one document per source file
3. **Filter** — remove binary, generated, and excluded files
4. **Expand** — add callers/callees and related documents
5. **Token Budget** — trim to fit within the configured limit
6. **Format** — wrap in an XML envelope with metadata

See [Context Assembly Architecture](../architecture/context-assembly) for the full pipeline diagram.

## Using from Claude

When Ferret is connected as an MCP server, Claude can call `ferret_context` automatically:

> "Explain how the search flow works in this codebase"

Claude calls `ferret_context("how does search flow work")` and receives a token-budgeted package containing the most relevant files.

## Token Budget

The default token budget is 8,000 tokens. Configure in `ferret.config.json`:

```json
{
  "context": {
    "defaultTokenBudget": 12000
  }
}
```

You can also pass `token_budget` per-call via the MCP tool:

```json
{
  "tool": "ferret_context",
  "arguments": {
    "query": "how does file watching work",
    "token_budget": 4000
  }
}
```

## Filtering Generated Files

By default, `*.generated.cs`, `*.designer.cs`, and `bin/`/`obj/` files are excluded from context packages even if they are indexed. Disable this:

```json
{
  "context": {
    "filterGeneratedFiles": false
  }
}
```

## Related

- [Context Assembly Architecture](../architecture/context-assembly) — pipeline diagram
- [MCP Reference](../reference/mcp) — `ferret_context` tool schema
- [Why Context Assembly?](../design/why-context-assembly) — design rationale
