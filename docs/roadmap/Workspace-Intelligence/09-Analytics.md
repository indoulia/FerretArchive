# 09 — Analytics

**Status:** Ready for implementation (v1 scope); org/cross-tenant analytics deferred
**Reads from:** 10-Usage-Ledger.md (events), 08-Telemetry.md (metrics)
**Extends:** ARCH-001 §21.4 (Health and Diagnostics) — analytics is the aggregation layer that report was missing

## 1. Scope Rule

Every aggregate defined here must trace back to a specific dashboard need in 11-Dashboard.md. Analytics is a read-only projection of the ledger — it never writes to it, and it computes nothing that isn't displayed somewhere. This is the same anti-speculation rule the founder applied to the doc set applied to the data model.

## 2. v1 Aggregates

| Aggregate | Grouping | Source events |
|---|---|---|
| Files / documents / symbols indexed | per workspace | Index Engine ledger events |
| Query count, avg/p95 latency | per workspace, per developer | `knowledge.query`, `workspace.federated_query` |
| Estimated tokens saved | per workspace | `context.compression.tokens_saved` (08 §1) |
| Cache hit rate | per workspace | `cache.federation.hit` / `.miss` |
| Most-queried symbols / documents | per workspace | Query events, ranked by frequency |
| Index freshness | per workspace | time since last index update per member repo |

Each is a straightforward rollup (count, sum, percentile, or top-N) over ledger events within a time window — no new query engine, computed by 10-Usage-Ledger.md's own store (§4, "aggregation model").

## 3. Explicitly Out of Scope for v1

- **Cross-tenant / organization-wide analytics.** Requires the multi-tenant model FUTURE-002 §22 defers to V3. Rolling up analytics across workspaces owned by different organizations is a privacy and billing question (FUTURE-002 Q2, Q5, Q8), not just an aggregation query — deferred, see `Future/Deferred-Scope.md`.
- **Cost/billing estimates.** "Estimated cost saved" from the original brief requires a pricing model, which requires the billing decision in FUTURE-002 Q2 (still open). v1 reports *tokens* saved, not dollars — a dollar figure can be layered on later without changing what's measured.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| v1 aggregates limited to per-workspace / per-developer rollups | Ready for implementation |
| Every aggregate must map to a dashboard in 11-Dashboard.md — no speculative metrics | Ready for implementation |
| Org-wide cross-tenant analytics | Deferred to future milestone (needs V3 multi-tenant model) |
| Cost/billing estimates (vs. token estimates) | Deferred — blocked on FUTURE-002 Q2 (billing model), Requires Founder decision when unblocked |
