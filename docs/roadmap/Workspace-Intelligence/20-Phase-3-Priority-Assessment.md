# 20 — Phase 3 Priority Assessment

**Status:** Complete — analysis only, no implementation, no architecture change.
**Purpose:** Determine the smallest Phase 3 work that delivers the largest real-world improvement, using implementation evidence from `FederatedKnowledgeStore.cs`, `WorkspaceStateFingerprintProvider.cs`, `WorkspaceReference.cs`, and `IndexStats.cs` — not just the design docs (`05`–`07`) or the existing backlog order.

---

## 1. Deliverable 1 — Phase 3 Execution Plan

| WIP | Purpose | Current status | Dependencies | Effort | Risk | User impact |
|---|---|---|---|---|---|---|
| **WIP-030** Pull-based invalidation | Invalidate a cached federated result when a referenced workspace's content changes | **Already ~90% built**, just not generalized. `FederatedKnowledgeStore.ResolveSourcesAsync` already does exactly this hash-mismatch check today, but only for *pinned* references (via `IWorkspaceStateFingerprintProvider`). Floating references have no cache to invalidate yet, so there's nothing to build for them until WIP-031 exists. | None architecturally; only meaningful once WIP-031 exists | Trivial as standalone | Low | None directly observable until paired with WIP-031 — recommend **merging into WIP-031**, not shipping separately |
| **WIP-031** Federated query cache | Skip full fan-out on a repeat query against unchanged workspaces | Not implemented. No caching exists anywhere in `FederatedKnowledgeStore` today — every query re-resolves the registry, re-fans-out to every source, and (for pinned refs) re-hashes the entire referenced repo | Needs a cheap per-repo state hash for *floating* references too (not just pinned) | Medium — cache mechanics are simple; the hard part is a cheap hash for the key (see §2) | **Medium** — if the cache key reuses today's `WorkspaceStateFingerprintProvider` as-is, computing the key costs as much as just running the query, making this a net loss | High for repeat-query workloads (MCP tool loops, interactive dogfooding) — but only after the fingerprint-cost fix in §2 |
| **WIP-032** Reference topology cache | Avoid re-walking the reference graph on every query | Not implemented, but **the doc's premise doesn't match the code**: `ResolveSourcesAsync` only reads *direct* references (one level), not a transitive graph walk — that walk only happens at reference-*creation* time (`ReferenceGraph` cycle detection), never at query time. What actually repeats per query is N+1 `IWorkspaceRegistry.ResolveAsync` calls (one per member repo, one per direct reference) — a file-open + JSON-parse each | None | **Low** — this is really "cache registry-entry reads," not "cache a graph walk" | Low | Medium — scales with reference count, but each read is cheap on its own |
| **WIP-033** Scope Classifier | Skip fan-out to references unlikely to be relevant | Not implemented, and its stated prerequisite (a per-workspace "index manifest" of keywords/symbols) **doesn't exist anywhere in the codebase** — see Deliverable 4 | A cheap scope signal to classify against (doesn't exist yet — build or reuse, see §4) | Medium — the heuristic is small, producing what it classifies against is the real work | Medium — a false-negative silently excludes a relevant workspace, which is worse than today's "always fan out" | High *at scale*, but limited value while dogfooded workspaces have only 1–2 references (17/18's evidence) |
| **WIP-034** Compressor | Shrink cross-workspace results before packing | Not implemented; explicitly consumes WIP-033's intent classification output (`05-Context-Optimization.md` §3) — strictly sequenced after it | WIP-033 | Medium | Low-medium — wrong compression silently drops needed detail | High, but compounds with WIP-033 — low standalone value |
| **WIP-035** Context assembly cache | Cache full assembled context, not just raw hits | Not implemented; its cache key includes "scope-classified workspace set" (`07-Caching.md`) — cannot land before WIP-033 | WIP-033, and transitively WIP-031's hash-cheapness fix | Low once WIP-031/033 exist | Low | Medium — mainly benefits repeated identical queries in one session |

### Recommended order (not the existing WIP-030…035 numbering)

1. **Un-numbered fix: make `WorkspaceStateFingerprintProvider` cheap.** Not a new WIP — a targeted fix to existing code, no architecture change. See §2. Highest leverage, smallest change, zero dependencies.
2. **WIP-032**, scoped as a registry-entry read-through cache (not a graph-topology cache — no graph walk exists yet to cache). No dependencies.
3. **WIP-030 + WIP-031 merged into one ticket** — cheap only once step 1 lands.
4. **WIP-033**, after building the minimal scope signal in §4 (reusing the existing SQLite FTS5 vocabulary — no new persistence).
5. **WIP-034** — strictly after WIP-033.
6. **WIP-035** — strictly after WIP-033, benefits from WIP-031.

**Why this order beats the backlog's:** dogfooding evidence (`17`/`18`) shows real workspaces today have 1–2 references, not many. The fingerprint re-hash cost (step 1) hits *every* query on *any* workspace with even one pinned reference, today — a bigger, more universal win right now than Scope Classifier, which only pays off once reference counts grow past what's actually been dogfooded.

---

## 2. Deliverable 2 — Performance Assessment

**Current complexity**, confirmed by reading `FederatedKnowledgeStore.RunAsync`/`ResolveSourcesAsync` directly (not inferred from docs):

- **Registry resolution:** O(1 + R) file reads + JSON parses per query (own entry + each direct reference), every query, uncached.
- **Pinned-reference fingerprint check:** for each pinned reference, `WorkspaceStateFingerprintProvider.ComputeFingerprintAsync` does a full filesystem walk of the referenced repo and a SHA-256 hash of *every file's content*, combined into one digest — **on every single query**, not just when content might have changed. This is O(total bytes in the pinned repo) per query, independent of the query itself.
- **Fan-out:** O(1 + R) independent search-service instantiations and BM25 queries via `Task.WhenAll` — always full fan-out, no scope narrowing exists yet.
- **Merge:** O(total hits) sort — cheap, not a bottleneck.

**Expected scaling:** latency scales linearly with reference count (registry reads + fan-out) and, for any pinned reference, with that reference's total file size — independent of reference count. This directly threatens `00-Vision.md` §4's target (p95 ≤ 2x single-repo baseline *regardless of reference count*) once a workspace has more than a couple of references, and a single large pinned reference can make every query slower than the vision target regardless of how few references exist.

**Bottlenecks, ranked:**
1. **`WorkspaceStateFingerprintProvider`'s full content re-hash per query** — confirmed by code, the single largest and most surprising cost; no telemetry currently wraps it.
2. **Per-query registry file I/O** — one JSON file per member repo and per reference, every query; cheap individually, compounds with reference count.
3. **Unconditional full fan-out** — cost scales with total references, not relevant references.
4. **Possible per-query connection/service construction** (`CreateForRepo` per source per query) — pattern suggests this, but the factory's implementation wasn't read this pass; flag as unconfirmed, verify before treating as a fix target.

**Safe optimization opportunities (no architecture change):**
- In-process, invalidation-aware cache over `IWorkspaceRegistry.ResolveAsync` — this is exactly what `07-Caching.md` already pre-approves as the topology cache layer.
- Cache the pinned-reference fingerprint itself (short TTL or filesystem-watch invalidation) instead of recomputing on every call — turns O(queries × repo size) into O(repo size) amortized, with zero interface change.
- *If* the index engine already tracks a per-file content hash for its own incremental updates, reuse it instead of a from-scratch re-hash — **unverified this pass** (`ContentHash.cs` exists but no usage was found in `Ferret.Indexing`), treat as "investigate," not "confirmed shovel-ready."

**Deliberately leave unchanged:**
- The pull-based (not push-based) invalidation model — correct for the offline/air-gapped requirement; fix the *cost* of the pull, not the model.
- Per-source fan-out as independent, fail-isolated tasks (Stabilization Sprint 1's exception boundary) — do not introduce shared state or early-exit between sources when optimizing.
- Fail-closed pinned-reference behavior (ADR-0027 Amendment) — cache the fingerprint *computation*, never the *comparison result* in a way that could serve stale content.

---

## 3. Deliverable 3 — Cross-workspace Ranking Assessment

**Current behavior**, confirmed in `FederatedKnowledgeStore.Merge`: each source scores independently within its own corpus statistics (BM25 IDF/avg-doc-length are corpus-local), then results are merged with a flat `OrderByDescending(hit => hit.Score)` — no normalization. This matches `18-Engineering-Analysis-Sprint-1.md`'s finding that cross-repo BM25 scores aren't comparable, classified as a backlog gap, not a broken promise.

**Is normalization required before Context Optimization? No.**
- The **Scope Classifier (WIP-033)** runs *before* any Scorer/BM25 step in the pipeline (`Scope Classifier → Planner → … → Relevance Scorer`, `05-Context-Optimization.md`). Its heuristic is keyword/symbol-name matching, not score comparison — it has no data dependency on ranking normalization.
- The **Compressor (WIP-034)** runs post-Scorer but decides compression from the query's intent classification, not from comparing score magnitudes across sources.
- Neither ticket touches score comparability. This confirms — and sharpens — the Engineering Analysis Sprint 1 disposition: normalization isn't just "somewhere in Phase 3," it's decoupled from *both* WIP-033 and WIP-034 specifically.

**Where it does matter, today, independent of Phase 3:** the existing `Merge` ordering already decides what the (also-existing) ARCH-001 §13.3 greedy Token Packer sees first. A large, established corpus will systematically out-score a small, actually-more-relevant referenced workspace for reasons unrelated to relevance (different avg-doc-length/term-frequency baselines) — a live ranking-quality issue today, that Phase 3 neither creates nor fixes.

**Recommendation:** track normalization as its own independent ticket, not gating or belonging to Phase 3's Context Optimization work — consistent with, and slightly more specific than, the disposition already recorded in `18-Engineering-Analysis-Sprint-1.md` §5.

---

## 4. Deliverable 4 — Scope Classifier Design Review

**Question:** Can workspace references already provide enough information to narrow scope before querying every workspace?

**Answer: No.** Confirmed by reading the types directly:
- `WorkspaceReference` carries exactly `WorkspaceId`, `Mode` (always `"read-only"` in v1), and `PinnedStateHash` — no name, description, tag, or content signal.
- `IndexStats` (the only existing per-workspace summary) carries `DocumentCount`, `TotalChars`, `LastIndexedAt`, `IndexSizeBytes` — none of it correlates with "does this workspace mention X."

`05-Context-Optimization.md`'s premise ("match against each workspace's index manifest") describes an artifact that must be built — it doesn't exist to be reused as-is.

**Smallest implementation that stays within existing architecture and constraints (no new persistence):** don't build a new manifest file. Each per-repo keyword index is already backed by SQLite FTS5, which exposes its own vocabulary as a queryable virtual table (`fts5vocab`) for free — no new schema, no new write path. The Scope Classifier becomes: for each referenced workspace, run a cheap vocabulary-membership check against its already-existing index for the query's terms — far cheaper than a real BM25 query (no scoring, just presence), with zero new persistence.

**Caveat:** this still opens a connection to each referenced workspace's index per query — the same cost concern flagged in §2 (bottleneck #4). The classifier's own cost must stay meaningfully cheaper than the query it's avoiding, which argues for pairing this with the §2/WIP-032 registry-and-connection caching work rather than building it in isolation.

---

## 5. Strategic Answer

**Smallest work, largest real-world improvement:** the un-numbered `WorkspaceStateFingerprintProvider` cost fix (§2) plus a registry-entry read-through cache (WIP-032, scoped as in §1) — neither is a new architecture surface, neither has a dependency chain, and together they fix the one confirmed, universal, already-live cost (full-repo re-hashing on every query to any workspace with a pinned reference) that hits real dogfooded usage *today*, before Scope Classifier/Compressor's scale benefits become relevant at the reference counts actually observed.
