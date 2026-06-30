# Design Decisions

Every significant choice in Ferret has a reason. This section explains the *why* behind the major design decisions — not as formal ADRs, but as readable explanations of the thinking, the trade-offs, and what we decided against.

These pages exist because architecture without rationale is just code. Understanding why things are the way they are makes it easier to extend, adapt, and challenge them.

## Decisions

- [Why SQLite?](why-sqlite) — why a file-based database, not a server
- [Why BM25 Before Vectors?](why-bm25) — why keyword search ships first
- [Why MCP Before REST?](why-mcp) — why we target AI tools, not HTTP clients
- [Why Providers?](why-providers) — why the AI provider abstraction exists
- [Why Context Assembly?](why-context-assembly) — why search results are not enough
- [Why Platform-First?](why-platform-first) — why we build foundations before features
- [Why Manual, Not Docs?](why-manual) — why this is called The Ferret Manual

## Related

- [Architecture Explorer](../architecture/index) — how the pieces fit together
- [Developer Guide](../developer-guide/index) — how to extend the platform
