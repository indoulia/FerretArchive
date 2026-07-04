# 11 — Dashboard

**Status:** Ready for implementation (Developer, Workspace views); Organization view deferred
**Reads from:** 09-Analytics.md aggregates only — never queries the raw ledger directly

## 1. v1 Views

| View | Audience | Shows |
|---|---|---|
| **Developer** | Individual contributor | Their query count/latency, workspaces they're a member of, tokens saved on their queries |
| **Workspace** | Workspace owner/admin | Repos/docs/symbols indexed, reference list and health (is each reference resolving, is any pinned reference stale), query volume, cache hit rate, index freshness per member repo |

Both map directly to aggregates already defined in 09-Analytics.md §2 — no dashboard here requires a metric that doc doesn't already produce.

## 2. Organization View — Deferred

The founder's brief asks for a Developer/Workspace/Organization three-tier dashboard set. Organization-level aggregation requires the multi-tenant/org model that FUTURE-002 §22 defers to V3, so an Organization view has nothing correct to show in v1 — building it now would mean displaying either fabricated rollups or a single-org special case that gets thrown away. Tracked in `Future/Deferred-Scope.md`.

## 3. Implementation Note

Delivered as `Ferret dashboard` CLI output (table format, consistent with §23.3 Output Formats) plus a JSON export for any future web view — no new UI framework decision is required for v1; a hosted/web dashboard is a Ferret Hub (V3) concern (FUTURE-002 §16.6), not this milestone's.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| v1 ships Developer and Workspace views only | Ready for implementation |
| Dashboards read only from Analytics aggregates, never raw ledger | Ready for implementation |
| Organization view | Deferred to future milestone (needs V3 multi-tenant model) |
| Hosted/web dashboard UI | Deferred — CLI table/JSON output is sufficient for v1 |
