# 03 — Cross-Workspace References

**Status:** Ready for implementation
**Extends:** ARCH-001 §27.2 (Multi-Repository Federation) — this document is that section's activation
**ADR:** ADR-0027 (Reference Resolution Strategy)

## 1. The Requirement, Restated

"Workspace A imports Workspace B" must behave like a dependency: no duplication, no re-indexing. This is not a design option among several — it's the founder's explicit, non-negotiable requirement, and it's also the cheaper option architecturally, so ADR-0027 is **Ready for implementation**, not a founder decision point.

## 2. Mechanism

A reference is an entry in the importing workspace's manifest (02-Workspace-Model.md §3):

```json
{ "workspaceId": "ws_1a9c...", "mode": "read-only", "pinnedStateHash": null }
```

Resolving it does not copy anything. `Ferret.Knowledge.Federation` (01-Architecture.md §2) holds a live handle to the referenced workspace's existing `IKnowledgeStore`. Every query against the importing workspace fans out to:
1. The importing workspace's own local `IKnowledgeStore` (its member repos)
2. Each referenced workspace's `IKnowledgeStore`, unmodified, queried in place

Results are merged and tagged with their source workspace so a citation always names which workspace an answer came from.

## 3. Versioning and Pinning

`pinnedStateHash` (nullable) controls whether a reference floats or is pinned:

| Value | Behavior |
|---|---|
| `null` (default) | Always queries the referenced workspace's current state — like a floating dependency version |
| A knowledge state hash (§13.4) | Federation refuses to return results if the referenced workspace's current state hash doesn't match — like a lockfile pin. Query fails closed with an explicit "reference out of date" error, not silently stale data |

Pinning reuses the existing knowledge state hash (§13.4) exactly as-is — no new versioning scheme.

## 4. Access Control on a Reference

`mode: "read-only"` is the only mode in v1. A referenced workspace's owner must have granted at least Viewer access (ADR-0029) to the importing workspace's members for the reference to resolve; otherwise queries against that reference return empty results with a permission note, not an error that leaks the referenced workspace's existence.

## 5. Cycle and Conflict Handling

- **Cycles** (A imports B, B imports A): detected at reference-creation time by a graph walk of the importing workspace's own reference list plus the target's. Creating a cycle is rejected outright — not resolved, rejected. Reference graphs must be a DAG.
- **Conflicts** (A and B, both referenced by C, define contradictory information about the same symbol/decision): **not resolved automatically in v1.** Federated results are returned with source-workspace tags and let the caller (or the Context Optimization Engine, 05) see both; no merge, no precedence rule. Building an actual conflict-resolution policy is deferred — see `Future/Deferred-Scope.md`. This mirrors FUTURE-002 Q4, which asks the identical question one layer up (connector conflicts) and leaves it open for the same reason: it's a product decision, not just a technical one, and doesn't block v1 shipping.

## 6. What This Does Not Require

Per ARCH-001 §27.2: "The Domain Layer and Plugin Architecture are already compatible with this extension; only the Knowledge Engine and storage abstraction need new interfaces." Confirmed — nothing in the Specification, Review, or Plugin domains needs to change for references to work.

## 7. Decision Log

| Decision | Outcome |
|---|---|
| References are live/federated, never materialized copies | Ready for implementation — hard requirement |
| Reference graph must be a DAG; cycles rejected at creation | Ready for implementation |
| Pinning reuses existing knowledge state hash, no new scheme | **Corrected** by ADR-0027 amendment (2026-07-05) — pinning uses the Workspace State Fingerprint, a derived value defined in `13-Storage.md` §4 (§13.4), not a pre-existing primitive |
| Cross-reference conflict resolution policy | Deferred to future milestone |
