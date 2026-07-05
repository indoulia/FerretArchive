# 26 — P3-002: Federated Query Cache Regression Investigation & Optimization

**Status:** Complete
**Purpose:** `25-Multi-Workspace-Dogfooding-Sprint.md` Finding 2 discovered that WIP-030/031's
already-shipped `CachingFederatedKnowledgeStore` becomes a net performance *regression* at realistic
reference scale (R=26, size-diverse) — a cache hit measured slower than a fresh, uncached query. This
ticket root-causes that regression with real measurements against the same live 26-reference
`dogfood-hub` environment, implements the smallest correctness-preserving fix, and re-validates.

---

## 1. Root Cause Analysis

**Measured, not speculated**, using a direct-instantiation probe against the real, still-live
`dogfood-hub` registry (`ferret-platform` + `indoulia-foundation` + 24 throwaway modules, 26
references total — the exact environment from `25`), mirroring the benchmark style of `21`/`23`/`25`.

| Cost component | Measured (warm) |
|---|---|
| Registry lookup, all 27 participants (`CachingWorkspaceRegistry`, already warm) | 6–7 ms |
| Fingerprint generation, all 27 participants, metadata-gate warm (P3-001) | 104–130 ms |
| — of which: `ferret-platform` alone (2,644 files after `.git`/`bin`/`obj`/etc. skip) | 106–140 ms |
| — of which: `indoulia-foundation` alone (556–560 files after skip) | 13–15 ms |
| — of which: all 24 small throwaway repos combined | ~10 ms |
| `FederatedQueryCache` dictionary lookup (1,000×) | 0.1–0.2 ms total |
| Fresh, uncached `FederatedKnowledgeStore.SearchAsync` (novel query, warm) | 12–45 ms |
| `CachingFederatedKnowledgeStore.SearchAsync`, cache hit (before fix) | 115–158 ms |

**Root cause:** `CachingFederatedKnowledgeStore.TryBuildCacheKeyAsync` calls
`IWorkspaceStateFingerprintProvider.ComputeFingerprintAsync` for the queried workspace **and every
one of its references, pinned or floating** — on *every* call, cache hit or miss (`23-WIP-030-031-
Federated-Query-Cache.md` §2's own documented design). `WorkspaceStateFingerprintProvider`'s P3-001
metadata-gate only skips the expensive **content hash** when nothing changed; it never skips the
**directory walk** that decides whether a re-hash is needed — that walk is O(files-after-ignore) and
runs unconditionally, every call. At R=26 with two real, large repos in the reference set, that
walk — done for a reference the real, uncached `FederatedKnowledgeStore` would *never* fingerprint at
all (`FederatedKnowledgeStoreTests.SearchAsync_WithAFloatingReference_NeverCallsTheFingerprintProvider`
is a locked invariant on that class) — costs more than the query it's meant to short-circuit.

Two hypotheses were tested and **disproven** before landing on the fix:

- **Parallelizing** the 27 sequential fingerprint calls (`Task.WhenAll` instead of a serial
  `foreach`-`await`) was measured at 111–125 ms vs. 104–130 ms serial — no meaningful improvement,
  because one participant (`ferret-platform`, ~106–140 ms alone) dominates the critical path;
  Amdahl's law limits any win from overlapping the other 26 participants' much-cheaper walks.
- **Raw enumeration speed** was checked directly: `Directory.EnumerateFiles` over `ferret-platform`'s
  *entire* tree (no ignore-filtering, 26,541 entries incl. `.git`, `.worktrees`) took 353–495 ms, while
  the real, ignore-aware walk (2,644 files after skipping `.git`/`.ferret`/`bin`/`obj`/`node_modules`/
  `packages`/`.svn`/`.hg`) took 106–140 ms. This is genuinely O(files-after-skip) cost, not an
  accidental inefficiency in one call site — confirming the walk itself, not something adjacent to it,
  is the bottleneck.

## 2. Implementation Summary

**The smallest change that preserves correctness: separate floating- from pinned-reference handling
in the cache key**, per the mission's own candidate list.

- A **pinned** reference already needs its full, portable, content-based Workspace State Fingerprint
  for drift detection — this is a pre-existing cost the real, uncached pipeline already pays for a
  pinned reference (`FederatedKnowledgeStore.ResolveSourcesAsync` fingerprints it too). Untouched.
- A **floating** reference has no drift/portability requirement. All the cache needs to know is "would
  a fresh query see different results" — and that is entirely determined by what the reference's
  **keyword index** currently contains, never by raw filesystem content that was never indexed (an
  edit to a file that hasn't been re-indexed cannot change what a query returns either way, from a
  *fresh* query or a cached one).

Added `IWorkspaceStateFingerprintProvider.ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry, ct)`
— a new, additive interface member (no existing signature changed). The concrete implementation
(`WorkspaceStateFingerprintProvider`) computes it as a single `FileInfo` stat per member repo against
the already-standardized `.ferret/indexes/keyword/keyword-index.db` path
(`WorkspaceLayout.RootDirectoryName` + `IndexLayout.*`, both pre-existing, already-shared conventions —
no new metadata invented) — O(1) per repo, not O(files). Missing index ⇒ `null` ⇒ fail closed, same
disposition as an unreachable checkout for the real fingerprint.

`CachingFederatedKnowledgeStore.TryBuildCacheKeyAsync` now branches per reference:
`reference.PinnedStateHash is not null` ⇒ `ComputeFingerprintAsync` (unchanged); otherwise ⇒
`ComputeIndexChangeSignalAsync` (new, cheap path). The queried workspace's own fingerprint is
untouched (it was never the measured bottleneck — 3–5 ms even for the hub's own content).

**Why this preserves every constraint:**
- **No cache redesign.** Same key shape, same fail-closed-on-null/exception behavior, same
  invalidation model (pull-based, no push path). Only *which* signal a floating reference contributes
  changed.
- **No fingerprint redesign.** `IWorkspaceStateFingerprintProvider.ComputeFingerprintAsync` and its
  ADR-0027 Amendment semantics (hash every file, portable, no persistence) are byte-for-byte unchanged
  and still used for every case that needs them (own content, pinned references).
- **Pinning correctness fully preserved.** A pinned reference's drift-check is untouched — same method
  call, same inputs, same test coverage (`SearchAsync_WhenAPinnedReferencesCurrentFingerprintDrifts_
  InvokesInnerStoreAgain` passes unmodified).
- **Fail-closed preserved, arguably tightened.** A floating reference with no index yet now correctly
  bypasses the cache (previously it would have gotten a — expensive but valid — real fingerprint; now
  it fails closed *and* fast).
- **Reuses existing architecture only.** `IndexLayout`/`WorkspaceLayout` are pre-existing, shared path
  conventions already used by `IndexCommandHandler`, `ServeCliModule`, `CoreCliModule`, and
  `IndexCliModule` — `Ferret.Knowledge.Federation` already references `Ferret.Core` (where they live),
  so no new project reference was needed.

## 3. Benchmark

Same direct-instantiation probe, same live `dogfood-hub` (R=26), before and after:

| Call | Before | After |
|---|---|---|
| Registry lookup, 27 participants (warm) | 6.5 ms | 6.5 ms (unchanged) |
| First `CachingFederatedKnowledgeStore.SearchAsync` (cold key, miss → real query) | 190–280 ms | 88–241 ms |
| **Repeat `CachingFederatedKnowledgeStore.SearchAsync` (cache hit, warm)** | **115–158 ms** | **3.6–4.7 ms** |
| Direct `FederatedKnowledgeStore.SearchAsync` (uncached, novel query, warm) | 12–45 ms | 12–16 ms (unchanged — real pipeline untouched) |

**Cache hit: ~115–158 ms → ~4 ms, a ~30–35× speedup.** More importantly, the regression's defining
symptom is reversed: a cache hit was **3.1× slower** than a fresh query before this fix (137.7 ms vs.
44.5 ms in one paired run) and is now **~3× faster** (4.1 ms vs. 12.3 ms in the equivalent paired run
after) — the mission's stated success criterion.

After a real content change (dogfooding, §5): first query after reindex correctly falls through to
the real pipeline (cache miss, as expected — same as before this fix); repeat queries after that are
cache hits again at the same ~4 ms.

## 4. Test Summary

**`tests/Ferret.Knowledge.Federation.Tests/CachingFederatedKnowledgeStoreTests.cs`** — all 13
pre-existing tests pass unmodified (the fake fingerprint provider's new
`ComputeIndexChangeSignalAsync` defaults to mirroring whatever was registered via `Register`, so no
existing test needed to change). 5 new tests:

| Test | Proves |
|---|---|
| `SearchAsync_WhenAFloatingReferencesIndexChangeSignalChanges_InvokesInnerStoreAgain` | floating-reference content change still invalidates the cache (was previously untested — no equivalent existed before P3-002) |
| `SearchAsync_WhenAFloatingReferencesIndexChangeSignalCannotBeComputed_NeverCachesAndAlwaysInvokesInnerStore` | fail-closed when no index exists yet, even if the (unused) expensive fingerprint *would* resolve |
| `SearchAsync_ForAFloatingReference_UsesTheCheapIndexChangeSignalNotTheFullFingerprint` | direct regression guard: proves the fix is actually wired in (floating references never call `ComputeFingerprintAsync`) |
| `SearchAsync_ForAPinnedReference_StillUsesTheFullFingerprintNotTheCheapSignal` | inverse guard: pinned semantics/drift-detection untouched |

**`tests/Ferret.Cli.Tests/Commands/Workspaces/WorkspaceStateFingerprintProviderTests.cs`** — 5 new
tests for the real `ComputeIndexChangeSignalAsync` implementation: no-index-yet → null (fail closed);
same value across repeat calls with no reindex; different value after a real reindex; **unchanged**
value when a source file is edited but never reindexed (proves the signal deliberately tracks the
index, not the filesystem); null when the repo checkout is unreachable.

**Regression coverage:** full solution build (`dotnet build src/Ferret.sln`) — 0 warnings, 0 errors.
`dotnet format --verify-no-changes` — clean. Full suite re-run: 1,371 tests across the solution, all
passing except the same pre-existing temp-directory test-parallelization flake documented in
`19-Stabilization-Sprint-1.md` §2, `21` §3, and `23` §3 (`Ferret.Integration.Tests`, passes cleanly in
isolation, unrelated subsystem). `Ferret.Knowledge.Federation.Tests`: 28 → 32. `Ferret.Cli.Tests`:
247 → 252.

## 5. Dogfooding Summary

**Real CLI correctness**, against the live `dogfood-hub` (26 references):

```
ferret workspaces query dogfood-hub "ollama" --limit 10      # x3, byte-identical results each time
ferret workspaces query dogfood-hub "telemetry" --limit 5    # multi-source fan-out confirmed:
                                                               # dogfood-Ferret.Core + ferret-platform
                                                               # both contributed real hits
```

**Real end-to-end invalidation**, through the actual production code path (no fakes, no mocks): a
throwaway probe queried `CachingFederatedKnowledgeStore` for a unique, guaranteed-novel term against
the real `dogfood-hub` (0 hits, cached); added a new file to the real, live `dogfood-Ferret.Telemetry`
floating reference containing that unique term; ran the real `ferret index` against that repo (which
rewrote its real `.ferret/indexes/keyword/keyword-index.db`); queried the *same* `CachingFederatedKnowledgeStore`
instance again and got **1 hit** — the cache correctly detected the floating reference's real content
change via the new cheap signal and fell through to the real pipeline, exactly as designed. Probe file
and reindex were cleaned up in a `finally` block; `git status` on the reference repo showed only the
same pre-existing untracked `.ferret/` entry before and after, nothing left behind.

**Remaining limitation, same as `23` §5 and `25` §2:** a one-shot CLI process can never observe a
cache *hit* (a fresh cache every process) — cache-hit performance is only observable via a
direct-instantiation probe or a genuinely long-lived host (the MCP server this cache targets). This is
a pre-existing, documented property of the benchmark methodology, not a limitation introduced here.

**Cleanup:** both throwaway probes used for this investigation
(`P3002BenchmarkProbe.cs`, `P3002DogfoodInvalidationProbe.cs`) were deleted before this ticket closed,
per the established precedent in `21`, `23`, and `25` — their numbers are captured in §1/§3/§5 above.

## 6. What Implementation Taught Us

- **P3-001's own stated assumption ("the walk is cheap, only the hash needs gating") was correct at
  R≤2 and wrong at R=26 with size-diverse references** — the exact kind of assumption this codebase's
  Standing Engineering Policy asks to be re-validated by evidence rather than trusted indefinitely.
- **Parallelizing independent I/O doesn't help when one participant dominates the critical path.**
  Tested and measured before implementing (per `systematic-debugging`'s "test minimally, verify before
  continuing") — worth stating plainly since it was the first, more obvious hypothesis and it failed.
- **A cache's own key-construction cost model does not have to match the thing it's caching.** The
  federated query's *output* is a pure function of each participant's indexed state, not its raw
  filesystem state — so the cache's invalidation signal only needs to track the index, which is
  strictly cheaper to check than the filesystem it was built from, without losing any correctness the
  cache actually needs (as opposed to correctness the *pinning* feature needs, which is a different,
  stricter requirement this fix deliberately left alone).
- **A missing test can hide behind a correct-by-accident implementation.** No test asserted that a
  floating reference's content change invalidates the cache before this ticket — the old implementation
  happened to satisfy it (by over-fingerprinting), so the gap was invisible until the fix's own
  regression-guard tests were written.
- **Reusing a path-convention constant class (`IndexLayout`/`WorkspaceLayout`) already shared by four
  other call sites was cheaper and safer than inventing a new abstraction** — no new project reference,
  no new persisted state, no new DI wiring beyond one interface method.

## 7. Architecture Validation

- **Architecture upheld?** Yes. No persistence introduced, no push invalidation, no change to
  `IFederatedKnowledgeStore`, the registry, or the portable Workspace State Fingerprint's semantics.
  `FederatedKnowledgeStore` (the real, uncached pipeline) is byte-for-byte unchanged; its own test
  suite, including the floating-reference performance invariant, passes unmodified.
- **Any assumption invalidated?** One, narrowly: P3-001's implicit assumption that the metadata-gate's
  directory walk is cheap enough to run unconditionally on every call no longer holds at R≥~20 with a
  size-diverse reference set. This ticket does not change P3-001's own fix (the content-hash gate) —
  it changes what `CachingFederatedKnowledgeStore` asks the fingerprint provider to compute for a
  floating reference in the first place.
- **New ADR required?** No. Additive interface method over already-approved primitives (ADR-0026
  registry, ADR-0027 Amendment fingerprint, the pre-existing `IndexLayout`/`WorkspaceLayout`
  conventions) — same category as P3-001 and WIP-030/031, neither of which required one.
- **Technical debt introduced?** One documented, narrow trade-off: `IWorkspaceStateFingerprintProvider`
  now has two methods with an implicit caller contract (`ComputeIndexChangeSignalAsync` is only ever
  correct as a cache-validity signal, never as a substitute for the real fingerprint) — captured in the
  new method's XML doc remarks so a future caller cannot reach for it by name alone without reading
  why it exists.

**Next step, per the mission's own gate:** with this regression resolved and evidenced, WIP-033
(Scope Classifier) may now proceed per `25` §5's recommendation (pooled design, re-validated in real
C# against the real `FederatedKnowledgeStore`, not the Python/SQLite simulation).
