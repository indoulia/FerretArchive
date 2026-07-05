# 04 — Knowledge Graph Across Workspaces

**Status:** Ready for implementation
**Extends:** ARCH-001 §13.2 (Knowledge Graph Model) — additive only, no existing node/edge type changes

## 1. Updated Schema

Everything in ARCH-001 §13.2 (`SourceSymbol`, `Document`, `Specification`, `ADR`, `Interaction`, `MemoryEntry` and their edges) is unchanged. Additions:

```
New Nodes:
  Workspace        (id, kind, name, schemaVersion, ownerId)

New Edges:
  CONTAINS         (Workspace → SourceSymbol | Document | Specification | ADR)
                    one edge per member repo's existing content — no new content is
                    created, existing nodes just gain an incoming CONTAINS edge from
                    their owning Workspace
  IMPORTS          (Workspace → Workspace)
                    the reference relationship from 03-Cross-Workspace-References.md;
                    carries {mode, pinnedStateHash} as edge properties
```

Every edge crossing a workspace boundary (a federated query result) is tagged with its source `Workspace` node at query time — this is a query-result annotation, not a new persisted edge type. Nothing about `REFERENCES`, `IMPLEMENTS`, `EXTENDS`, etc. changes; a `SourceSymbol` in Workspace B referenced from a query run against Workspace A is still just a `SourceSymbol`, now reachable via `CONTAINS` from B and returned with B's workspace ID attached.

## 2. Query Model Additions

ARCH-001 §13.5 defines four query patterns (symbol lookup, full-text, graph traversal, relationship query). None of the four change shape. What changes is *scope*: a query against a workspace with references now implicitly includes every referenced workspace's graph (03 §2), and every result row gains one field: `sourceWorkspaceId`.

No fifth query pattern is introduced. This is deliberate — see 01-Architecture.md §3: federation is invisible above the storage abstraction.

## 3. Design Rationale

ARCH-001 §13.6 already anticipated this: "the graph can be extended with new node and edge types without replacing the storage format." `Workspace`/`CONTAINS`/`IMPORTS` are exactly that — an extension, not a migration. `IKnowledgeStore` (§19.3) needs no interface change for the local case; only `IFederatedKnowledgeStore` (new, §27.2) needs the fan-out logic, and it composes existing `IKnowledgeStore` instances rather than replacing them.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| Workspace/CONTAINS/IMPORTS are additive graph extensions | Ready for implementation |
| Query patterns unchanged; scope widens via reference resolution, not new query syntax | Ready for implementation |
| Cross-workspace results always carry `sourceWorkspaceId` | Ready for implementation |
