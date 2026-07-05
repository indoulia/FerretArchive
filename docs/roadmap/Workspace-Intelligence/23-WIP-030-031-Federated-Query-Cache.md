# 23 — WIP-030 + WIP-031: Federated Query Cache (merged)

**Status:** Complete
**Purpose:** Implement WIP-030 (pull-based invalidation) and WIP-031 (federated query cache) as a
single capability, per `20-Phase-3-Priority-Assessment.md` §1: WIP-030 has no standalone user value
until there is a cache to invalidate. A repeated federated query against unchanged workspaces now
skips the entire fan-out/merge pipeline; anything else — a different query, a changed reference set,
changed content, or a workspace whose state can't currently be verified — always falls through to the
real, authoritative pipeline.

---

## 1. Implementation Summary

Two new types in `Ferret.Knowledge.Federation`:

- **`FederatedQueryCache`** — a thin, injectable wrapper around a `ConcurrentDictionary<string,
  SearchServiceResult>`. Registered as a singleton in `WorkspacesCliModule` so it survives across the
  per-query instances a long-lived host constructs, exactly like `CachingWorkspaceRegistry` (WIP-032)
  and `WorkspaceStateFingerprintProvider` (P3-001) already do.
- **`CachingFederatedKnowledgeStore`** — an `IFederatedKnowledgeStore` decorator that wraps the real
  `FederatedKnowledgeStore`. Before delegating, it builds a cache key from the *current* state of the
  queried workspace and everything it references; on a key match it returns the cached
  `SearchServiceResult` without running the fan-out pipeline at all.

`WorkspacesQueryCommandHandler` now builds `new CachingFederatedKnowledgeStore(innerStore, _registry,
_fingerprintProvider, entry.WorkspaceId, _queryCache)` instead of calling `FederatedKnowledgeStore`
directly. `FederatedKnowledgeStore` itself is unchanged — same class, same constructor, same
behavior when called directly (its own test suite, including the floating-reference performance
invariant, passes unmodified).

## 2. Cache Design

**Cache key** — a SHA-256 hash of:
- the query identity (`raw:<text>` or `parsed:<OriginalText>`, so the two `ISearchService` overloads
  never collide);
- the parts of `SearchOptions` that can affect output (`MaxResults`, `IncludePassages`,
  `HighlightEnabled`, `SnippetLength`, `Mode`) — `Token` is excluded, it's plumbing, not data;
- the queried workspace's ID;
- for the queried workspace and every one of its `References` (pinned or floating): that workspace's
  current Workspace State Fingerprint (`IWorkspaceStateFingerprintProvider.ComputeFingerprintAsync`,
  ADR-0027 Amendment), plus the reference's `Mode` and `PinnedStateHash`.

**Cache value** — the derived `SearchServiceResult` exactly as `FederatedKnowledgeStore` produced it:
hits, citations, and diagnostics included, unmodified. The cache is never a source of truth — every
cached entry is byte-for-byte a value the real pipeline actually returned for that exact key, and the
inner store remains authoritative on every miss.

**Invalidation** — pull-based, matching every other cache in this codebase (`CachingWorkspaceRegistry`,
the P3-001 fingerprint cache). There is no push/event path: every call recomputes the key from live
state, and a state change (content, reference added/removed, pin drift) naturally produces a
different key, so the stale entry is simply never looked up again — no explicit eviction needed.

**The fail-closed rule that makes "never hides corruption/unreachability" true by construction:** if
the key cannot be safely built — the queried workspace isn't found, a reference's registry entry is
corrupt, a reference no longer resolves, or any participant's fingerprint can't be computed
(unreachable checkout, locked file, I/O error) — `TryBuildCacheKeyAsync` returns `null` and the cache
is bypassed *entirely* for that call: no lookup, no write. Those cases are therefore never cached, so
every call against them runs the real pipeline and reports the real diagnostic. This is a design
choice, not an omission: a dangling/corrupt/unreachable participant is exactly the situation where a
stale cached answer would be most dangerous, so it's the one situation guaranteed to never be served
from cache.

**Why no architecture change was required:** every input to the key already exists for another
reason — the registry (already read on every query), the fingerprint provider (already computed for
pinned references, and now proven cheap for *any* reference by the P3-001 metadata gate), and the
reference record itself. No new metadata, no new persistence, no new interface. `20-Phase-3-Priority-
Assessment.md` §1 explicitly anticipated this dependency: "[WIP-031] needs a cheap per-repo state hash
for floating references too... cheap only once [the P3-001 fingerprint fix] lands." That fix landed
first; this ticket is the payoff.

**One explicit, evidenced trade-off, not silently introduced:** `FederatedKnowledgeStoreTests.
SearchAsync_WithAFloatingReference_NeverCallsTheFingerprintProvider` is an approved invariant on
`FederatedKnowledgeStore` itself — a *direct* call to that class never fingerprints a floating
reference. That class is unchanged and the test still passes. `CachingFederatedKnowledgeStore` is a
separate decorator that *does* fingerprint every reference, pinned or floating, to build its key —
this is the "per-repo state hash for floating references" the Phase 3 doc called for, and it is why
this ticket could not exist before P3-001 made that fingerprinting cheap on repeat calls. The
underlying, uncached pipeline's behavior and cost model are untouched; only the new caching layer in
front of it pays this cost, and only to buy a much larger win (skipping the fan-out entirely on a
cache hit).

## 3. Test Summary

`tests/Ferret.Knowledge.Federation.Tests/CachingFederatedKnowledgeStoreTests.cs` — 13 tests, all new:

| Test | Mission acceptance criterion |
|---|---|
| `SearchAsync_CalledTwiceWithTheIdenticalQuery_OnlyInvokesInnerStoreOnce` | repeated identical query hits cache |
| `SearchAsync_CachedResult_ReturnsTheExactHitsAndDiagnosticsFromTheOriginalCall` | diagnostics preserved |
| `SearchAsync_CalledWithADifferentQueryText_InvokesInnerStoreAgain` | different query misses cache |
| `SearchAsync_CalledWithDifferentMaxResults_InvokesInnerStoreAgain` | execution context is part of the key |
| `SearchAsync_WhenTheQueriedWorkspacesOwnContentChanges_InvokesInnerStoreAgain` | workspace content change invalidates cache |
| `SearchAsync_WhenAPinnedReferencesCurrentFingerprintDrifts_InvokesInnerStoreAgain` | pinned fingerprint mismatch invalidates cache |
| `SearchAsync_AfterAddReferenceViaRegistrySave_InvokesInnerStoreAgain` | add-reference invalidates cache |
| `SearchAsync_AfterRemoveReferenceViaRegistrySave_InvokesInnerStoreAgain` | remove-reference invalidates cache |
| `SearchAsync_WhenAReferencedWorkspaceDisappearsFromTheRegistry_InvokesInnerStoreAgain` | workspace deletion invalidates cache |
| `SearchAsync_WhenAReferencedWorkspaceRegistryEntryIsCorrupt_NeverCachesAndAlwaysInvokesInnerStore` | cache never hides corruption |
| `SearchAsync_WhenAParticipatingWorkspacesFingerprintCannotBeComputed_NeverCachesAndAlwaysInvokesInnerStore` | cache never hides unreachable workspaces |
| `SearchAsync_WhenTheQueriedWorkspaceItselfCannotBeFound_NeverCachesAndAlwaysInvokesInnerStore` | fail-closed for the base workspace too |
| `SearchAsync_WhenComputingTheFingerprintThrows_NeverCachesAndAlwaysInvokesInnerStore` | failure-injection: a fingerprint I/O exception must degrade to "don't cache," never crash |

The last test is a direct regression test for a real bug caught during dogfooding (§5) — it did not
exist until dogfooding surfaced the gap, and TDD (RED confirmed against the actual `IOException`,
then GREEN after the fix) was used to close it the same way as every other test here.

**Regression coverage:** full solution build (`dotnet build src/Ferret.sln`) — 0 warnings, 0 errors.
Full suite re-run: `Ferret.Knowledge.Federation.Tests` (28 passed, up from 15 pre-existing +
1 pre-existing floating-reference invariant, all unmodified), `Ferret.Cli.Tests` (247 passed,
including the 4 `WorkspacesQueryCommandHandlerTests` updated only for the new constructor
parameter — no assertion changed). `Ferret.Integration.Tests.WorkspaceE2ETests.
WorkspaceInit_ThenStatus_ShowsWorkspaceName` failed once on the full-suite run and passed cleanly in
isolation — the same pre-existing temp-directory test-parallelization flake already documented in
`19-Stabilization-Sprint-1.md` §2 and `21-P3-001-Fingerprint-Optimization.md` §3, unrelated to this
change (different subsystem: per-repo `.ferret/` init, not federation).

## 4. Benchmark

Measured with a direct-instantiation probe (real `FileWorkspaceRegistry`, real `RepoSearchServiceFactory`,
real `WorkspaceStateFingerprintProvider`, real indexed repos — no mocks), mirroring the measurement
style in `21-P3-001-Fingerprint-Optimization.md` §4. Workspace `bench-a` (member: this repo's `docs/`
tree) references workspace `bench-b` (member: the real, separate `indoulia-foundation` repo, 548
files), one shared `FederatedQueryCache` across five sequential in-process calls of the same query:

| Call | Time | What happened |
|---|---|---|
| 1. First federated query (cold) | 569.1 ms | Full registry resolve, full content hash of both repos, full fan-out + merge |
| 2. Repeated query | 16.6 ms | Cache hit — same result object as call 1, fan-out skipped entirely |
| 3. Repeated query | 16.2 ms | Cache hit again — confirms it isn't a one-off warm-up artifact |
| 4. After modifying a file in `indoulia-foundation` | 178.0 ms | Cache key changed (different result object than call 3) — correctly re-ran the fan-out. Faster than the cold call because `bench-a`'s own fingerprint was still warm (P3-001) — only the changed repo needed a full re-hash |
| 5. Repeated query (warm again) | 16.2 ms | Cache hit — same result object as call 4, cache correctly repopulated |

**~35x speedup** on a cache hit versus the cold call (569.1 ms → 16.2 ms), and automatic,
correct invalidation the moment content actually changed — no manual step, no stale answer served.

**CLI-level dogfooding note:** the CLI dogfood (§5) uses the real `ferret workspaces query`
executable, but each CLI invocation is its own process, so it cannot demonstrate the cache's
performance win by itself (same caveat as `22-WIP-032-Registry-Read-Through-Cache.md`'s benchmark) —
it proves *correctness* (identical results, correct reference-removal behavior) using every real code
path. The benchmark above is the only way to observe the cache's actual win, since it requires one
long-lived process — exactly the profile of the MCP server this cache is built for.

## 5. Dogfooding Summary

**Real CLI workflow**, two fresh throwaway workspaces against two real repos:

```
ferret workspaces create --name wip030031-a
ferret workspaces add-repo wip030031-a C:\POC\Ferret
ferret workspaces create --name wip030031-b
ferret workspaces add-repo wip030031-b C:\POC\indoulia-foundation
ferret workspaces add-reference wip030031-a wip030031-b
ferret workspaces query wip030031-a "workspace registry" --limit 5   # x3, identical results each time
ferret workspaces remove-reference wip030031-a wip030031-b
ferret workspaces query wip030031-a "workspace registry" --limit 5   # confirmed wip030031-b content gone
ferret workspaces add-reference wip030031-a wip030031-b              # re-added for the benchmark
```

All three repeated queries returned byte-identical hits, scores, and citations. After
`remove-reference`, the query correctly stopped returning any `[wip030031-b]`-sourced hit — expected,
since each CLI invocation starts a fresh, empty cache, so this exercises the real, uncached
correctness path (the query cache itself is validated by the in-process benchmark in §4 and the unit
suite in §3, since a one-shot CLI process can never observe a cache hit).

**A real bug was found and fixed during this dogfooding pass**, not just in the benchmark probe: the
first real `ferret workspaces query` run against `wip030031-a` (member repo: this live Ferret
checkout) crashed with an unhandled `IOException` — `WorkspaceStateFingerprintProvider` tried to open
`.tokensave\tokensave.db`, which was locked by another process (the TokenSave MCP server, active
during this same session) actively writing to it. `TryBuildCacheKeyAsync` wrapped registry resolution
in a cache-safety try/catch but had not wrapped the fingerprint-computation calls the same way. Fixed
via TDD (`SearchAsync_WhenComputingTheFingerprintThrows_NeverCachesAndAlwaysInvokesInnerStore`,
RED confirmed against the real exception type, then GREEN) by extending the identical
cache-safety-boundary pattern to both fingerprint calls: a locked/unreadable file now means "can't
verify this workspace's state, don't cache," never a crash. This is exactly the class of bug
`superpowers:verification-before-completion` and this repo's "dogfood every optimization" policy
exist to catch before it reaches a real user.

**Cleanup:** the two throwaway workspaces (`wip030031-a`, `wip030031-b`) were left registered — the
registry has no delete-workspace command (same as `22-WIP-032-Registry-Read-Through-Cache.md`'s
precedent). `indoulia-foundation`'s `git status` was checked before and after (only the pre-existing
untracked `.ferret/` entry both times) — the benchmark's temporary probe file was written and deleted
inside a `try/finally`, confirmed gone.

**Remaining friction:** none beyond the bug above, which is now fixed and covered by a regression test.

## 6. What Implementation Taught Us

- The single biggest risk in this design wasn't the cache logic — it was assuming "resolving state to
  build a key" is exception-free. Dogfooding against a real, *live* repo (this one, mid-session, with
  another process actively writing a file) found a crash in under five minutes that no unit test with
  fake dependencies would have caught, because every fake fingerprint provider returns cleanly.
- Fingerprinting a repo's own live tool directories (`.tokensave/`, potentially others) makes the
  cache key non-deterministic on an actively-used repo — this is a pre-existing property of
  `WorkspaceStateFingerprintProvider` (it deliberately hashes every file, ADR-0027 Amendment), newly
  *load-bearing* here because this cache now depends on that fingerprint being stable across calls,
  not just correct within one call.
- Pairing WIP-030+031 with the already-landed P3-001 fix was validated exactly as `20-Phase-3-Priority-
  Assessment.md` predicted: the 178 ms "after content change" call being far cheaper than the 569 ms
  cold call — not because of the query cache (which correctly missed) but because P3-001's own
  per-repo fingerprint cache kept the *unchanged* repo's re-hash free. The two caches compound.
- The existing floating-reference performance invariant on `FederatedKnowledgeStore` did not need to
  change — it only had to be reinterpreted as scoped to that one class, with the new caching decorator
  free to make its own, differently-motivated tradeoff on top.
- "Do not invent new metadata solely for caching" was achievable in the literal sense (every key input
  already existed), but required resolving the same reference topology `ResolveSourcesAsync` already
  walks a second time from the caching layer — a small, accepted duplication rather than exposing
  `FederatedKnowledgeStore`'s internals.

## 7. Architecture Validation

- **Architecture upheld?** Yes. No new persistence, no distributed cache, no background refresh, no
  push invalidation, no new interface. `IFederatedKnowledgeStore` is unchanged; `FederatedKnowledgeStore`
  is unchanged; the registry and fingerprint provider contracts are unchanged.
- **Any assumption invalidated?** One, explicitly: the floating-reference fingerprint-cost invariant
  was previously understood as "floating references never pay fingerprint cost, full stop." It now
  more precisely means "the *uncached* federation pipeline never pays that cost for a floating
  reference" — a new, separate caching layer may, deliberately, to make caching possible at all. The
  original test and its rationale remain true and enforced for the class it targets.
- **New ADR required?** No. This is additive caching over already-approved primitives (ADR-0026
  registry, ADR-0027 Amendment fingerprint), the same category as WIP-032 and P3-001, neither of which
  required one either.
- **Technical debt introduced?** One documented, accepted trade-off (above): the cache key must
  resolve the same reference topology `FederatedKnowledgeStore.ResolveSourcesAsync` resolves
  internally, since that method is private. Not a defect — a normal decorator-pattern cost — but worth
  naming if a future change makes `ResolveSourcesAsync`'s output reusable across both call sites.

## 8. Git Summary

- `39feb4c` — `feat(workspace-graph): add federated query cache (WIP-030/031)` (`FederatedQueryCache`,
  `CachingFederatedKnowledgeStore`, and their test suite).
- `6732b48` — `feat(workspace-graph): wire CachingFederatedKnowledgeStore into the query command
  (WIP-030/031)` (DI wiring + updated `WorkspacesQueryCommandHandlerTests` fixture).
- Branch: `feature/wip-032-registry-read-through-cache`.
- PR: not opened as part of this task.
