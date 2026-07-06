# 25 — Multi-Workspace Dogfooding Sprint (WIP-033, at scale)

**Status:** Complete — dogfooding + benchmarking only, no production code shipped.
**Purpose:** `24-WIP-033-Scope-Classifier-Discovery.md` validated the `fts5vocab` mechanism but could
say nothing about value at scale — every dogfooding session to date (`17`, `22`, `23`) exercised
exactly 1–2 references. This sprint builds real, representative multi-reference workspaces, measures
actual fan-out cost and WIP-030/031 cache effectiveness at that scale, and simulates the Scope
Classifier against real indexes to determine whether it would meaningfully help — per the founder's
brief: "treat WIP-033 as validated but not yet justified, pending evidence from larger-scale
dogfooding." This is that evidence.

---

## 1. Setup — Representative Multi-Reference Workspace

**24 throwaway reference repos**, each a real git repo (not synthetic content) built from a distinct
module of this project's own source tree (`Ferret.Core`, `Ferret.Search`, `Ferret.Providers.Ollama`,
`Ferret.Telemetry`, `Ferret.Mcp`, etc.) — real C# source, real namespaces, real distinct vocabulary
per module, ranging from 2 to 271 files each (build artifacts stripped before indexing/committing).
One additional throwaway repo (`Ferret.Cli`, 114 files) served as the **hub**'s own member repo.

**2 real, already-dogfooded large repos** — `ferret-platform` (this checkout, 2,542 indexed docs) and
`indoulia-foundation` (386 indexed docs) — were added as *additional* references partway through, to
test whether reference **size** diversity (not just count) changes the picture, since the 24 throwaway
modules are all small.

**Result: `dogfood-hub` with 26 references** (24 small throwaway + 2 real large), the largest reference
count ever dogfooded on this project by an order of magnitude over the 1–2 references in every prior
session. All references are floating (unpinned), matching real dogfooded usage to date.

Every repo was indexed with the real `ferret index` and registered with the real `ferret workspaces
create/add-repo/add-reference` commands via this branch's own `Release` build — no shortcuts, no
direct file manipulation. Correctness was confirmed the same way `17`/`22`/`23` did: `ferret workspaces
query dogfood-hub "ollama" --limit 10` and `"workspace" --limit 10` both fanned out correctly and
returned citations tagged with the correct source workspace across all 26+1 sources.

**Cleanup note:** per established precedent (no `remove-workspace`/`remove-repo`-at-scale command
exists — `22`/`23`'s own documented limitation), the 25 new throwaway workspace entries
(`dogfood-hub` + 24 `dogfood-Ferret.*`) were left registered in `~/.ferret/workspaces`, and the two
references added to `dogfood-hub` (pointing at `ferret-platform`/`indoulia-foundation`) were left in
place. Neither `ferret-platform` nor `indoulia-foundation`'s own manifests were modified — a
reference is stored only on the referencing workspace's entry.

## 2. Measurement — Real Fan-Out Cost at Scale

Measured with a direct-instantiation probe (real `FileWorkspaceRegistry`/`CachingWorkspaceRegistry`,
real `WorkspaceStateFingerprintProvider`, real `RepoSearchServiceFactory`/`Bm25SearchProvider`, real
`FederatedKnowledgeStore`/`CachingFederatedKnowledgeStore`, pointed at the real `~/.ferret/workspaces`
registry) — mirroring the exact benchmark style of `21`/`22`/`23`, run as a throwaway, uncommitted xUnit
test (deleted at the end of this session, never part of the permanent suite).

### At R=24 (small throwaway modules only)

| Measurement | Result |
|---|---|
| Cold (registry + fingerprint cold, first-ever call) | 211.3 ms |
| Avg. novel-query fan-out, warm registry+fingerprint (10 distinct terms) | **8.55 ms** (min 6.0, max 14.2) |
| Novel no-hit query (matches nothing anywhere) | 4.7–6.3 ms |

### At R=26 (24 small + 2 real large repos mixed in)

| Measurement | Result |
|---|---|
| Cold | 176.6 ms |
| Avg. novel-query fan-out, warm registry+fingerprint (same 10 terms) | **24.12 ms** (min 14.7, max 44.4) |
| WIP-030/031 cache: first call through the decorator | 219.3 ms |
| WIP-030/031 cache: repeat 1 / repeat 2 (same query) | **103.9 ms / 109.7 ms** |
| Novel no-hit query | 5.0–6.5 ms |

**Finding 1 — reference count alone is cheap; reference *size* diversity is what actually costs.**
Going from R=24 (all small) to R=26 (+2 large, real repos) roughly **tripled** the average per-query
fan-out cost (8.55 ms → 24.12 ms), even though reference count only grew by 2. `20-Phase-3-Priority-
Assessment.md`'s framing ("cost scales with total references") is incomplete on its own — cost scales
with total *matched, ranked, and merged* content, which correlates with reference *corpus size*, not
just reference *count*. This matters directly for WIP-033: a classifier that only reduces *R* helps
less than one that specifically identifies and skips *large, irrelevant* corpora.

**Finding 2 — the WIP-030/031 query cache can become a net negative at this scale, for a specific,
identifiable reason.** The "cached" repeat calls (103.9/109.7 ms) were **slower than a fresh, uncached
novel query** (24.12 ms avg) at the same R. Per `23-WIP-030-031-Federated-Query-Cache.md` §2's own
documented design, `CachingFederatedKnowledgeStore` must fingerprint *every* reference — pinned or
floating — to build its cache key, even on a hit. At R=26 with two real, large repos in the reference
set, that per-call fingerprint metadata-scan (a real filesystem directory walk per repo, every single
call, cache hit or miss — P3-001 only skips the *content hash*, never the *directory walk* that decides
whether a re-hash is needed) now costs more than just running the query fresh, because the query itself
got cheap enough (§Finding 1's small-repo case) that there's very little left for the cache to save.
This is a **new finding, not previously dogfooded at this reference-count/size combination** — worth a
follow-up ticket independent of WIP-033, since it means the existing, already-shipped WIP-030/031 cache
needs its own re-validation at realistic scale, not just at R=2.

## 3. Measurement — Scope Classifier Simulation (accuracy + cost, at the same R=26)

Simulated (not implemented) against all 26 real reference indexes plus the hub, using the same
`fts5vocab` mechanism and document-frequency filter (>30% of docs → treat as too common, fail open to
"include") validated in `24`. Ground truth for two distinctive terms (`ollama`, `telemetry`) was taken
from real `ferret workspaces query dogfood-hub "<term>" --limit 200` output, tagging exactly which
sources contributed real hits.

**Accuracy: zero false negatives across both terms.** For `ollama`, the classifier correctly flagged
22/27 sources `EXCLUDE` and never excluded any of the 5 real contributors (`dogfood-hub`,
`dogfood-Ferret.Core`, `dogfood-Ferret.Configuration.AI`, `dogfood-Ferret.Providers.Ollama`,
`ferret-platform`). For `telemetry`, 22/27 `EXCLUDE`, zero false negatives against the 5 real
contributors. This confirms `24`'s accuracy analysis directly against ground truth, not just vocabulary
inspection — the frequency-filtered membership check is a safe, accurate signal for distinctive terms
at this scale.

**Cost — the naive implementation is not obviously a win; a pooled one plausibly is:**

| Classifier design | Cost per query (26 refs) | vs. R=26 fan-out cost (24.12 ms avg) |
|---|---|---|
| Naive: fresh connection + vocab table per reference, per query | **17.13 ms** | Comparable to the query it's replacing — a thin, unreliable margin |
| Pooled: one persistent connection + vocab table per reference, reused across the process (same singleton pattern as `CachingWorkspaceRegistry`/P3-001) | **2.71 ms** (+ 14.6 ms one-time setup, amortized) | Clearly cheaper — a real margin |

**Finding 3 — the "smallest implementation" sketched in `20`/`24` (one connection per reference, per
query) is the wrong shape at this scale.** A naive classifier would cost almost as much as the fan-out
it's trying to avoid, for the same root cause as Finding 2: per-reference connection overhead, paid
fresh every call, is the dominant cost once the underlying search itself is cheap. The mechanism only
pays off if it reuses the same "build once, keep warm for the process" pattern already established by
every other Phase 3 optimization (P3-001, WIP-032, WIP-030/031) — which was not in either prior
document's implementation sketch.

## 4. Synthesis — Does WIP-033 Meaningfully Improve Latency at This Scale?

**Not as originally sketched. Plausibly yes, with a specific, now-evidenced design correction.**

- At R=24 (small, uniform references), the entire federated query already costs ~8.55 ms once
  warm — there is effectively nothing left for a classifier to save, and a naive classifier
  (17 ms simulated) would make things *worse*.
- At R=26 (adding 2 large, realistic references), the query costs ~24 ms — closer to a threshold
  where skipping large, irrelevant sources could matter, but a **naive** classifier's overhead
  (17 ms) still eats most of the available savings.
- A **pooled** classifier (2.71 ms/query, measured) would leave a real margin (24.12 ms → an estimated
  low-teens ms after skipping ~20 of 26 irrelevant sources, based on Finding 1's per-source cost
  distribution) — but this number comes from a Python/SQLite simulation against real index files, not
  from an actual C# implementation, and doesn't yet account for connection lifecycle management,
  thread-safety under concurrent queries, or GC/memory cost of holding 26+ open connections for a
  long-lived process (the MCP server scenario this whole optimization line targets).
- **No false negatives were observed** in this session's ground-truth check — the accuracy risk flagged
  in `24` §3 did not materialize for the two terms tested, though two terms is not exhaustive coverage.

## 5. Recommendation

**Re-sequence, not implement yet, and not reject.** Of the four options: not "implement exactly as
planned" (the naive design this session tested is not a net win at measured scale); not "reject" (the
mechanism is accurate — zero false negatives — and a pooled design shows a real, measured margin); not
yet "implement with minor adjustments" either, because the adjustment required (connection pooling) is
not minor enough to skip validating in real C#, not a Python simulation, before committing to
production code.

**Concrete next step, smaller than a full WIP-033 implementation:** build a throwaway (or Task-1-only,
TDD, deletable-if-it-fails) prototype of a **pooled** `ScopeClassifier` — one long-lived connection +
`fts5vocab` table per referenced workspace, matching `CachingWorkspaceRegistry`'s singleton lifetime
pattern — and re-run this exact benchmark (R=26, mixed small/large references) end-to-end through the
real `FederatedKnowledgeStore` before deciding to merge it. This sprint's numbers (2.71 ms pooled vs.
17.13 ms naive vs. 24.12 ms of fan-out to skip) make a real implementation attempt look justified for
the first time — but only the pooled shape, and only re-validated in the real code path, not
extrapolated from this session's simulation.

**Independent finding to route separately:** Finding 2 (WIP-030/031's cache turning net-negative at
R=26 with size-diverse references) is a regression risk in *already-shipped* code, discovered by this
sprint's larger-scale dogfooding, not a WIP-033 concern. Recommend a follow-up investigation on
`CachingFederatedKnowledgeStore`'s own fingerprint-based key-building cost at realistic scale,
independent of whatever happens with WIP-033.

This is the outcome the founder's brief anticipated: the roadmap adapting to real measurement rather
than being followed mechanically. WIP-033's core assumption (reuse `fts5vocab`) remains validated; its
originally sketched cost model does not survive contact with a representative reference count and
size mix — and neither, newly, does WIP-030/031's.
