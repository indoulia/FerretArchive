# ADR-0016 — Integration Platform Architecture

**Status:** Accepted  
**Date:** 2026-06-28  
**Sprint:** 11

## Context

Ferret needs to expose its platform capabilities to external AI hosts (Claude Code, Claude Desktop, Cursor). Adding MCP directly risks treating it as the architecture rather than an adapter.

## Decision

Adopt the **Ferret Host Architecture Pattern**: `Capabilities → Platform Services → Hosts → Protocols`.

**8+1 Architectural Principles:**

1. **Platform services are host-independent.** `ISearchService`, `IDocumentService`, `IIndexEngine`, and `IWorkspaceContext` know nothing about MCP, REST, or any other protocol.
2. **Hosts are adapters.** A Host translates a protocol request into a platform service call. It owns translation; it does not own logic.
3. **One integration technology per package.** `Ferret.Mcp` contains only MCP. A future `Ferret.Rest` would be separate.
4. **External SDKs are quarantined.** SDK types from `ModelContextProtocol` are confined to `Transport/Stdio/`. Nothing outside that folder imports SDK namespaces.
5. **Contracts are Ferret-owned.** `IMcpTool`, `IMcpResource`, `McpArguments`, `McpToolResult` are Ferret types. The SDK adapter translates between Ferret contracts and SDK wire types.
6. **Capabilities are host-independent.** Adding a new Host does not change platform service interfaces.
7. **Protocol translation is one-to-one.** Each SDK request maps to exactly one Ferret call. Adapters do not aggregate, cache, or orchestrate.
8. **Hosts are launchers, not owners.** `ServeCliModule` starts the runtime; it does not own or embed it. `ferret serve` is a launcher, not a server.

**Principle 9 (Platform First):** When evaluating any feature request, ask "does this belong in the Platform (usable by all hosts) or in the Host (specific to one protocol)?" Default to Platform.

## Consequences

- `Ferret.Application` layer deferred until Sprint 12/13 (see ADR-0018).
- All future hosts (REST, Web UI, Agent) follow the same adapter pattern.
- Architecture tests enforce that no SDK types leak outside `Transport/Stdio/`.

## Milestone

M3 — Multi-Host Platform checkpoint after Sprint 11.
