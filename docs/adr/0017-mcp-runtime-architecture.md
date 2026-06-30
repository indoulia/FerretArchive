# ADR-0017 — MCP Runtime Architecture

**Status:** Accepted  
**Date:** 2026-06-28  
**Sprint:** 11

## Context

Sprint 11 delivers `Ferret.Mcp`: an MCP stdio runtime. Key decisions about transport isolation, registry design, and SDK boundary must be recorded.

## Decision

1. **Stdio transport only in Sprint 11.** HTTP transport reserved for a future sprint (`Transport/Http/`).
2. **SDK confined to `Transport/Stdio/`.** `McpArgumentsFactory`, `SdkToolAdapter`, `SdkResourceAdapter`, `SdkRuntimeAdapter`, `StdioTransport` are the only files that import `ModelContextProtocol.*` namespaces.
3. **Immutable registries.** `IMcpToolRegistry` and `IMcpResourceRegistry` are built once at startup via internal builders and are never mutated at runtime.
4. **Stateless adapters.** SDK adapter classes (`SdkToolAdapter`, `SdkResourceAdapter`) are stateless static translators. No shared mutable state.
5. **Runtime independence.** `McpRuntime` depends on `IMcpTransport` (Ferret interface), not on SDK types. Swapping the transport does not change the runtime.
6. **Startup validation.** `McpRuntime.RunAsync` validates registries before starting the transport. An empty tool registry is a startup error.
7. **One runtime per process.** `IMcpRuntime` is registered as a singleton. Starting multiple runtimes in one process is unsupported.

## Consequences

- `IMcpTransport` is the seam: everything above it is pure Ferret; everything below `Transport/Stdio/` is SDK.
- Future sprint: `HttpTransport` implements `IMcpTransport` without touching `McpRuntime` or the tool/resource layer.
