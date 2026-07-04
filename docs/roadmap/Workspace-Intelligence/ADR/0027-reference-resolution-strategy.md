# ADR-0027 — Reference Resolution Strategy

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-05 |
| **Deciders** | Founder |
| **Milestone** | Workspace Intelligence Platform, Phase 2 |
| **Supersedes** | — |

---

## Context

When Workspace A references Workspace B, B's content must be queryable from A. Two architectural strategies exist: materialize (copy or re-index B's content into A) or federate (query B's existing index live, in place). The Founder brief states this requirement explicitly and without qualification: "No duplication. No re-indexing. References only. The imported workspace should behave like a dependency."

## Decision

We will implement references as **live federated queries** via a new `IFederatedKnowledgeStore` that composes the existing `IKnowledgeStore` of every referenced workspace, per ARCH-001 §27.2. No content is copied or re-indexed. See `../03-Cross-Workspace-References.md` for the full mechanism, versioning/pinning, and conflict-handling rules.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Materialize (copy referenced content into the importing workspace's own index) | Directly violates the stated requirement; also doubles storage and creates a second, independently-staling copy of the same knowledge |
| Periodic re-index/sync of referenced content | Same duplication problem as materialization, just on a delay; adds a sync scheduler with no benefit over live federation given ARCH-001 already provides the extension point for the latter |

## Consequences

### Positive
- Zero duplicated storage; a referenced workspace's content is always current, not synced-as-of-some-time
- Reuses ARCH-001 §27.2's already-designed extension point exactly — no new architecture, only implementation
- Cross-workspace queries automatically reflect access-control changes on the referenced workspace immediately (no stale copy to revoke)

### Negative
- Federated query latency depends on the referenced workspace's store being reachable; an offline/unavailable referenced workspace degrades that portion of a query rather than serving stale cached data (unless pinned, see `../03-Cross-Workspace-References.md` §3)

### Neutral / Risks
- Performance at scale depends on the Scope Narrowing and Caching work in `../05-Context-Optimization.md` and `../07-Caching.md` landing in the same milestone — federation without those is correct but not fast
