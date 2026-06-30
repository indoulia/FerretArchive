# Why MCP Before REST?

Ferret's primary integration surface is the Model Context Protocol (MCP), not a REST API. We built `ferret serve` before we built any HTTP endpoint. This surprised some early reviewers.

## MCP is where AI assistants already live

Claude Desktop, Cursor, and VS Code with GitHub Copilot all speak MCP natively. When you add Ferret as an MCP server, your AI assistant can immediately call `ferret_search` and `ferret_context` without any glue code, API client, or authentication token.

A REST API would require a second integration step: someone has to write a plugin, an extension, or an AI tool wrapper that bridges HTTP to the AI assistant's tool protocol. MCP eliminates that step.

## REST is for humans (and machines that aren't AI)

REST APIs are excellent for human-operated workflows: CI scripts, dashboards, integrations with other services. They are the right choice when the consumer is deterministic and needs structured data.

AI assistants are not REST clients. They use tool-calling protocols where the AI decides which tool to call and what arguments to pass. MCP is that protocol. REST is not.

## What we gave up

Building REST first would have given us a simpler server implementation and a more testable interface. We addressed this by designing `IMcpTool` as a clean interface and testing tools as units before wiring up the MCP runtime.

REST will come in a post-RC1 sprint for users who need programmatic non-AI access.

## Related

- [MCP Runtime Architecture](../architecture/mcp-runtime) — how the server works
- [MCP Reference](../reference/mcp) — all MCP tools
