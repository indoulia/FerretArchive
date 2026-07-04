# 13 — Storage Design

**Status:** Requires Founder decision on registry model (ADR-0026); everything downstream of that choice is Ready for implementation
**Extends:** ARCH-001 §19 (Storage Strategy)

## 1. The Decision (ADR-0026)

This is the load-bearing decision for the entire milestone — 02-Workspace-Model.md, 03-Cross-Workspace-References.md, and 10-Usage-Ledger.md all assume its outcome. Restated plainly:

**Where does a multi-repo workspace's own record live, given no single repo checkout can hold it?**

| Option | Description | Cloud-sync-ready? | Sharing-ready? |
|---|---|---|---|
| A — Path-based | A manifest in a parent directory lists relative paths to repo checkouts | No — paths are meaningless on another machine | No — same reason |
| **B — Identity-based local registry (recommended)** | `~/.ferret/workspaces/<id>/workspace.json`; members addressed by durable identity (git remote, workspace ID), path cached alongside | Yes — the registry entry itself is what would sync | Yes — identity survives being shared to a different machine |
| C — Cloud-hosted registry, day one | Workspace state lives in a Ferret-operated service from the start | Trivially yes | Trivially yes | Breaks local-first/air-gap invariant (FUTURE-002 Condition 3–4); too early, no hosting infra exists |

**Recommendation: B.** It is the only option that satisfies local-first *and* keeps the door open for C later, via the same abstraction swap ARCH-001 already uses for `IKnowledgeStore` (§19.3/19.5) and FUTURE-002 uses for `IMemoryStore` tiers (§13.3: same interface, Local → Shared → Enterprise backend). A is cheaper to build but actively forecloses two of the founder's stated top priorities (shared workspaces, cloud sync) — building A now would itself become the rework this whole exercise is meant to avoid.

## 2. Storage Areas Added (extends §19.2)

| Area | Location | Version Controlled | Default Implementation |
|---|---|---|---|
| Workspace Registry Entry | `~/.ferret/workspaces/<id>/workspace.json` | No (local machine state; shared via explicit `workspace share`, not git) | JSON file |
| Federation / Topology / Context Caches | `.ai/cache/{federation,workspace-graph,context}/` | No | File-based (07-Caching.md) |
| Usage Ledger | `.ai/ledger/` | No | Append-only file-based log (10-Usage-Ledger.md) |

Per-repo storage areas (§19.2 existing table: Knowledge Index, Session Memory, Workspace Config, etc.) are entirely unchanged.

## 3. Storage Abstraction

`IWorkspaceRegistry` (`Resolve(id)`, `List()`, `Save(entry)`) is a new, narrow interface following §19.3's design rule exactly: any compliant backend — local file, or later a hosted service — can serve it. This is the abstraction that keeps option C available without committing to it now (§1).

## 4. Decision Log

| Decision | Outcome |
|---|---|
| Workspace registry model (A/B/C) | **Requires Founder decision — ADR-0026.** Recommendation: B (identity-based local registry) |
| New storage areas follow existing §19.2 table format and `.ai/` conventions where repo-scoped, `~/.ferret/` where machine-scoped | Ready for implementation once ADR-0026 is decided |
| `IWorkspaceRegistry` mirrors `IKnowledgeStore`'s pluggable-backend pattern | Ready for implementation |
