# 10 — Usage Ledger

**Status:** Ready for implementation, pending ADR-0028 sign-off on retention
**Extends:** ARCH-001 §19 (Storage Strategy) — the ledger is a new Storage Area, not a new subsystem

## 1. Why a Ledger and Not Counters

Counters answer "how many." They can't answer "which workspace, which developer, at what knowledge state, did this happen" after the fact, and they can't be re-aggregated a new way without having recorded the underlying event. An append-only event ledger can be replayed into any future aggregate (09-Analytics.md) or, later, into billing — without having to have predicted the exact rollup shape in advance. This is the same reasoning ARCH-001 already applies to knowledge state (§13.4: record the hash, don't just record "index updated").

## 2. Event Schema

```json
{
  "eventId": "evt_...",
  "timestamp": "2026-07-05T00:00:00Z",
  "workspaceId": "ws_...",
  "actorId": "user_... | agent_...",
  "eventType": "workspace.created | workspace.shared | repo.added | repo.removed |
                workspace.imported | workspace.indexed | query.executed |
                context.assembled | cache.hit | cache.miss",
  "knowledgeStateHashRef": "sha256:... | null",
  "payload": { "...": "event-type-specific fields, e.g. query text, duration_ms, tokens" }
}
```

This directly implements the event list in the founder's brief (Objective 9) — every example given (`Workspace Created`, `Files Indexed`, `Query Executed`, `Cache Hit`, etc.) is one `eventType` value, not a separate table or schema.

## 3. Storage Backend

Added to §19.2's Storage Areas table:

| Area | Location | Version Controlled | Default Implementation |
|---|---|---|---|
| **Usage Ledger** | `.ai/ledger/` (local), registry-level rollup for shared workspaces (13-Storage.md) | No (gitignored) | Append-only file-based log, same pattern as the default `IKnowledgeStore` |

The ledger sits behind its own narrow interface (`IUsageLedger`: `Append(event)`, `Query(filter, timeRange)`) — the same abstraction pattern as `IKnowledgeStore` (§19.3), so a pluggable backend (SQLite, a hosted ledger service for Ferret Hub) can replace the default without changing callers. This is the same escape hatch §19.5 already provides for the knowledge store; the ledger reuses the pattern rather than inventing a second one.

## 4. Aggregation Model

Aggregation (09-Analytics.md) runs as scheduled rollup jobs over the raw event stream, writing derived summary rows back into the same store under a separate namespace — raw events are never mutated. This is what makes replay possible: if a rollup definition changes, it recomputes from raw events rather than needing a schema migration on historical data.

## 5. Retention Strategy (ADR-0028)

**Recommendation, pending Founder sign-off:**

| Data | Retention |
|---|---|
| Raw events | 90 days rolling |
| Daily/weekly rollups | Indefinite (small, aggregate-only, no per-query detail) |

Raw-event retention trades off storage growth against replay flexibility (§4). 90 days covers every dashboard window in 11-Dashboard.md; it does not cover long-term audit/compliance retention, which FUTURE-002 §22 already scopes as a V3, RBAC-adjacent concern (audit logging for AI operations, deferred).

## 6. Decision Log

| Decision | Outcome |
|---|---|
| Immutable, append-only event ledger (not counters) | Ready for implementation |
| `IUsageLedger` abstraction mirrors `IKnowledgeStore`'s pluggable-backend pattern | Ready for implementation |
| Rollups are derived, never mutate raw events | Ready for implementation |
| Raw event retention window (90 days proposed) | Requires Founder decision — ADR-0028 |
