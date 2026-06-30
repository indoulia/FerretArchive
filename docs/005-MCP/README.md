# 005 — MCP (Model Context Protocol)

Design documents for Ferret MCP server and client implementations.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added)_ | | |

---

## Overview

Ferret provides first-class MCP support:

- **MCP Client** — connects to external MCP servers (tools, data sources, resources)
- **MCP Server** — exposes Ferret capabilities to MCP-compatible hosts (Claude Desktop, etc.)

---

## Protocol Reference

- MCP Specification: [modelcontextprotocol.io](https://modelcontextprotocol.io)
- Supported transports: `stdio`, `HTTP/SSE`, `WebSocket` (to be confirmed — see ADR backlog)

---

## Template

Use [docs/templates/mcp.md](../templates/mcp.md) for MCP server/client design documents.
