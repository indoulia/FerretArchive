# 06 — Incremental Indexing Across Workspace References

**Status:** Ready for implementation
**Extends:** ARCH-001 §14 (Index Architecture), §25.3 (Incremental Design)

## 1. The One New Problem

§25.3 already guarantees every index operation works on a changeset, not the full repo, *within one repo's index*. Federation adds exactly one new question: when Repo A (inside Workspace X) changes, what — if anything — needs invalidating in a Workspace Y that references X?

**Answer: nothing needs re-indexing. Something needs invalidating.** Workspace Y never held a copy of X's index (03 §2), so there's nothing in Y to update. What Y's federated-query cache (07-Caching.md) holds is a cache of *query results* keyed on X's knowledge state hash (§13.4) — when X's hash changes, Y's cached results for queries that touched X are stale and are invalidated by hash mismatch, not by a push notification or a re-index job.

## 2. Mechanism

No new indexing pipeline is needed:

1. Repo A changes → Workspace X's own Index Engine runs its existing incremental update (§14.2, unchanged) → X's knowledge state hash changes (§13.4, unchanged)
2. Workspace Y's federation layer (`Ferret.Knowledge.Federation`) checks X's current state hash lazily, at query time, against whatever hash any cached result for X was computed under
3. Mismatch → cache miss → live query against X's current `IKnowledgeStore` → new cache entry keyed on the new hash

This is a pull model, not a push model — Y never needs to know X changed until Y actually queries through the reference again. No new coordination service, no webhook, no polling daemon.

## 3. Why Not Push-Based Invalidation

A push model (X notifies every workspace that references it on every change) requires X to maintain a live list of dependents, turns every commit into a fan-out operation, and doesn't work at all for the air-gapped/offline deployment mode ARCH-001 §16.4 (FUTURE-002) requires as a first-class target. The pull-on-query model degrades to "just query," which always works, including offline against a stale-but-present local checkout.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| No new indexing pipeline for federation; existing §14.2 incremental update is untouched | Ready for implementation |
| Cross-workspace invalidation is pull-based (state-hash mismatch at query time), not push-based | Ready for implementation |
