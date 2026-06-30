# Connect Claude

Ferret exposes your workspace to Claude Desktop and Cursor via MCP (Model Context Protocol).

## Start the MCP server

```bash
ferret serve
```

Output:
```
Ferret MCP server running.
Tools: ferret_search, ferret_read_document, ferret_context, ferret_workspace_status
```

## Claude Desktop

Open the config file:
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

Add:

```json
{
  "mcpServers": {
    "ferret": {
      "command": "ferret",
      "args": ["serve"],
      "cwd": "/absolute/path/to/your/project"
    }
  }
}
```

Restart Claude Desktop. Ferret will appear in the tools panel.

## Cursor

In Cursor → Settings → MCP → Add server:

```json
{
  "name": "ferret",
  "command": "ferret serve",
  "workingDirectory": "/absolute/path/to/your/project"
}
```

## Test

In Claude, type: **"Search my codebase for IIndexPipeline"**

Claude will call `ferret_search` and return grounded results from your workspace.

## Related

- [MCP Reference](../reference/mcp) — all MCP tools and schemas
- [Context Assembly](../architecture/context-assembly) — how context is built
- [Troubleshooting](../troubleshooting) — MCP connection errors
