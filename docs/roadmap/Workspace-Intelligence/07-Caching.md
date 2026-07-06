# 07 — Caching Architecture

**Status:** Ready for implementation
**Extends:** ARCH-001 §19 (Storage Strategy), §25 (Scalability Strategy)

## 1. Three Cache Layers, All Keyed on Existing State Hashes

No new hashing or fingerprinting scheme is introduced anywhere in this milestone — every cache below is keyed on the knowledge state hash that already exists (§13.4). This is the single rule that keeps caching correct without new invalidation machinery.

| Layer | Cache key | Invalidated when | New? |
|---|---|---|---|
| Per-repo knowledge index | content hash (existing, §14.3) | file content changes | No — exists today |
| Federated query result | tuple of (query, state hash of every workspace touched) | any touched workspace's state hash changes | Yes — new, backs 06-Incremental-Indexing.md §2 |
| Workspace reference topology | workspace manifest version (02 §3 `schemaVersion` + reference list) | a reference is added/removed/re-pinned | Yes — new, cheap: this is a small list, not a graph traversal, so it's invalidated far less often than query results |
| Context assembly output | (query, scope-classified workspace set, all involved state hashes) | any involved state hash changes, or scope classification changes | Yes — new, sits in front of 05-Context-Optimization.md's pipeline |

## 2. Why a Separate Topology Cache

Resolving "which workspaces does this workspace transitively reference" requires a graph walk (03 §5, cycle detection). Doing that walk on every query would be wasted work — the reference list changes rarely (a human edits it), while queries happen constantly. The topology cache means the walk happens once per manifest version, not once per query.

## 3. Storage Backend

All three new cache layers are additional entries in the existing Storage Areas table (§19.2), using the existing `.ai/cache/` area (gitignored, no version control) — not a new storage subsystem:

| Area | Location | Version Controlled |
|---|---|---|
| Federated query cache | `.ai/cache/federation/` | No |
| Workspace topology cache | `.ai/cache/workspace-graph/` | No |
| Context assembly cache | `.ai/cache/context/` | No |

Cache eviction policy (size-bounded LRU vs TTL) is an implementation detail left to the execution phase — it does not gate this milestone's design and is marked accordingly.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| All new caches key on existing knowledge state hashes; no new hash scheme | Ready for implementation |
| Reference topology is cached separately from query results | Ready for implementation |
| New caches live under the existing `.ai/cache/` storage area | Ready for implementation |
| Eviction policy (LRU size bound vs TTL) | Deferred to execution phase — implementation detail, not a design gate |
