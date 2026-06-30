# Architecture Explorer

Ferret is a layered platform built on the ContextOS foundation. The architecture separates contracts from implementations and hosts from capabilities — making every layer independently testable and replaceable. This section maps each major subsystem.

## Sections

- [Platform Overview](platform-overview) — the full layer stack and package responsibilities
- [Dependency Graph](dependency-graph) — which packages reference which
- [Search Flow](search-flow) — query → BM25 → results
- [Context Assembly](context-assembly) — search results → AI-ready context package
- [AI Flow](ai-flow) — model routing → provider selection → completion
- [MCP Runtime](mcp-runtime) — server startup, tool registration, stdio lifecycle
- [Storage](storage) — `.ferret/` directory layout, SQLite schema
- [Configuration](configuration) — config layering, environment variable overrides
- [Extension Points](extension-points) — the four interfaces every extension implements

## Key Principles

Ferret's architecture is governed by seven platform principles defined in ADR-0013:

1. Capability composition over inheritance
2. Universal asset model (`AssetDescriptor`)
3. Identity → Descriptor → Instance → Status lifecycle
4. Streaming by default (`IAsyncEnumerable<T>`)
5. Normalization before processing
6. Separation of discovery, enrichment, indexing, and knowledge extraction
7. Commands are orchestration, not implementation

All principles are enforced by architecture tests in `Ferret.Architecture.Tests`.

## Related

- [Design Decisions](../design/index) — the reasoning behind the architecture
- [Developer Guide](../developer-guide/index) — how to extend the platform
- [Reference](../reference/architecture) — ADR index
