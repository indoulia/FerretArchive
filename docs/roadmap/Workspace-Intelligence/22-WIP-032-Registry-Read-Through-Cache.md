# 22 — WIP-032: Registry Read-Through Cache

**Status:** Complete
**Purpose:** Close out WIP-032 as scoped in `20-Phase-3-Priority-Assessment.md` §1/§2 — cache
`IWorkspaceRegistry.ResolveAsync` reads (not a reference-graph walk; no such walk exists at query
time) so that `FederatedKnowledgeStore`'s per-query, per-member-repo, per-direct-reference registry
file-open + JSON-parse isn't repeated by a long-lived host that resolves the same workspace ID more
than once in one process.

---

## 1. Implementation Summary

`CachingWorkspaceRegistry` (`src/Ferret.Workspace.Graph/CachingWorkspaceRegistry.cs`) is a new
`IWorkspaceRegistry` decorator that wraps another `IWorkspaceRegistry` (in practice,
`FileWorkspaceRegistry`) and caches the result of `ResolveAsync` per `Guid` workspace ID, including
a cached `null` for a workspace that doesn't exist. `WorkspacesCliModule.ConfigureServices`
(`src/Ferret.Cli/Commands/Workspaces/WorkspacesCliModule.cs`) now registers the CLI's singleton
`IWorkspaceRegistry` as `new CachingWorkspaceRegistry(new FileWorkspaceRegistry(root))` instead of
the bare `FileWorkspaceRegistry`. No other production code path changed — `FederatedKnowledgeStore`,
`WorkspaceStateFingerprintProvider`, and every other `IWorkspaceRegistry` consumer are unaware the
decorator exists; they see the same interface as before.

---

## 2. Cache Design

- **Key:** `Guid WorkspaceId`, the same identity `IWorkspaceRegistry.ResolveAsync` is already keyed
  on — no new identity concept introduced.
- **Invalidation:** write-through on `SaveAsync`, the registry's only mutation path. Every
  `workspaces` CLI command that changes an entry (`add-repo`, `remove-repo`, `add-reference`,
  `remove-reference`, `pin-reference`, `unpin-reference`) already funnels through `SaveAsync` for the
  one entry it modifies, so the decorator only needs to intercept that single method to stay correct.
  `SaveAsync` refreshes the cache entry **only if that ID was already cached** in this process — a
  save is never itself used to warm an entry that was never resolved, so a workspace this process
  has never read costs exactly one future `ResolveAsync` read-through, same as if the cache didn't
  exist. `ListAsync` passes straight through, uncached — it is not on the federated query hot path
  (`FederatedKnowledgeStore` never calls it) and its own directory scan already dominates whatever a
  per-entry cache could save.
- **Failure handling:** an exception from the wrapped registry (`WorkspaceRegistryCorruptException`)
  is never cached — it propagates on every call, so a corrupt manifest fails exactly as it did before
  this cache existed, every time, rather than being papered over by a cached error or a cached
  fallback value.
- **Why no architecture change was needed:** this is a pure decorator over an existing interface —
  no new interface, no new persistence, no new DI surface beyond the existing registration site. The
  precedent for an in-memory, per-process, singleton-lifetime cache field was already established one
  optimization earlier by `WorkspaceStateFingerprintProvider` (`21-P3-001-Fingerprint-Optimization.md`
  §2), which is already registered `AddSingleton` in the same `WorkspacesCliModule`. WIP-032 reuses
  that exact pattern (in-memory `ConcurrentDictionary` field, singleton lifetime, write-through on the
  single mutation path) rather than inventing a new one. The registry has exactly one mutation path
  (`SaveAsync`) and exactly one read path relevant to caching (`ResolveAsync`) — there was no
  multi-writer or multi-path invalidation problem to design around.

---

## 3. Test Summary

Task 1 added 8 tests to `CachingWorkspaceRegistryTests`
(`tests/Ferret.Workspace.Graph.Tests/CachingWorkspaceRegistryTests.cs`), all passing:

| Test | Proves |
|---|---|
| `ResolveAsync_CalledTwiceForSameId_OnlyReadsInnerRegistryOnce` | Repeated lookup for the same ID hits the inner registry once, not twice — the core caching behavior |
| `ResolveAsync_AfterSaveAsyncOnTheCache_ReturnsUpdatedEntryWithoutReadingInnerRegistryAgain` | A save through the cache itself updates the cached value without a redundant read |
| `ResolveAsync_AfterAddReferenceViaSaveAsync_ReturnsEntryWithTheNewReference` | `add-reference`'s save path correctly invalidates/refreshes a previously-resolved entry |
| `ResolveAsync_AfterRemoveReferenceViaSaveAsync_ReturnsEntryWithoutTheRemovedReference` | `remove-reference`'s save path correctly invalidates/refreshes a previously-resolved entry |
| `ResolveAsync_WhenInnerRegistryThrowsCorruptException_PropagatesEveryTimeAndNeverCaches` | A corrupt manifest's exception is never cached — fails closed on every call |
| `ResolveAsync_WhenWorkspaceDoesNotExist_ReturnsNullEachTimeAndCachesTheMiss` | A negative lookup (`null`) is itself cached, and the inner registry is read only once for a nonexistent ID |
| `ResolveAsync_ViaAFreshInstance_DoesNotReuseAPreviousInstancesCache` | The cache is genuinely per-process/per-instance — a fresh `CachingWorkspaceRegistry` never sees a prior instance's in-memory state (only the durable on-disk write survives) |
| `ListAsync_PassesThroughToInnerRegistryUncached` | `ListAsync` is confirmed uncached, matching the documented design |

These 8 tests map directly onto the mission's acceptance criteria: repeated-lookup caching,
save-triggered invalidation for both add-reference and remove-reference, fail-closed behavior on a
corrupt entry, correct negative-lookup caching, no cross-process cache leakage, and an explicit
sanity check that the uncached `ListAsync` path was a deliberate scope decision, not an oversight.

`dotnet build src/Ferret.sln -c Release`: 0 warnings, 0 errors (verified in this session — see §5).

---

## 4. Benchmark

Mechanism (mirroring `21-P3-001-Fingerprint-Optimization.md` §4's direct-instantiation-and-timing
style, not a permanent automated perf test): a throwaway console probe, written under the session
scratchpad and never committed, project-referencing
`src/Ferret.Workspace.Graph/Ferret.Workspace.Graph.csproj` directly. It points a real
`FileWorkspaceRegistry` at the live `~/.ferret/workspaces` root (the same registry used for
dogfooding in §5 below — the exact `ferret-platform` / `indoulia-foundation` pair, so the measured
manifests are real, not synthetic) and times, in one process:

1. A baseline loop: 500 iterations × 2 `ResolveAsync` calls (one per workspace ID), against a bare
   uncached `FileWorkspaceRegistry` — always a fresh file-open + JSON-parse.
2. A single cold call per ID against a fresh `CachingWorkspaceRegistry` (first resolution, a cache
   miss — pays the same file-open + JSON-parse cost as the baseline, plus a dictionary insert).
3. A warm loop: the same 500×2 `ResolveAsync` calls against the now-populated
   `CachingWorkspaceRegistry` — every call a cache hit.

Run three times to rule out a JIT/warm-up artifact:

| Run | Baseline (uncached) avg/call | Cold miss (1st call, ferret-platform) | Cold miss (1st call, indoulia-foundation) | Warm (cached) avg/call | Speedup (avg/call) |
|---|---|---|---|---|---|
| 1 | 0.27923 ms | 3.387 ms | 0.449 ms | 0.00029 ms | 955.6x |
| 2 | 0.31120 ms | 3.034 ms | 0.447 ms | 0.00031 ms | 1013.7x |
| 3 | 0.26460 ms | 3.299 ms | 0.583 ms | 0.00031 ms | 841.3x |

A single isolated warm call (post-population) measured `0.000`–`0.001 ms` — a dictionary lookup, no
I/O, no allocation beyond the lookup itself.

**Reading these numbers honestly:** the absolute per-call cost of the uncached path (~0.27–0.31 ms) is
small in isolation — this registry file is tiny and local-disk cached by the OS after the first touch,
so this is not the multi-hundred-millisecond cost the fingerprint provider had (`21`'s 251–291 ms cold
walk). What the cache removes is a **per-query, per-reference, repeated fixed cost** that scales
linearly with reference count and query count in a long-lived process — at 500 repeated queries
against 2 IDs, that's the difference between ~280 ms and ~0.3 ms of registry I/O across the run,
consistent with `20-Phase-3-Priority-Assessment.md` §2's characterization of this as "cheap
individually, compounds with reference count" rather than the single dominant cost fixed by the prior
optimization.

---

## 5. Dogfooding — Same Live Registration Used in `17-Dogfooding-Sprint-1.md` / `21`

Reused the real, still-live pair: workspace `ferret-platform` (`C:\POC\Ferret`, this repo) with an
existing (unpinned, floating) reference to workspace `indoulia-foundation`
(`C:\POC\indoulia-foundation`, a real separate repo). No throwaway workspaces were created. Built this
branch's own CLI (`dotnet build src/Ferret.sln -c Release`, 0 warnings / 0 errors) and invoked
`src\Ferret.Cli\bin\Release\net9.0\ferret.exe` directly, since the globally-installed `ferret` on PATH
is the older published v0.16.0.

**Correctness across repeated fresh-process queries** (each `ferret workspaces query` invocation is
its own process, so this proves identical results/diagnostics per invocation, not the in-process
cache's performance win — that's §4):

```
ferret.exe workspaces query ferret-platform "federated" --limit 10
```

Run three times back-to-back; all three produced byte-identical output, e.g. the top hit every time:

```
[ferret-platform] filesystem:///.worktrees/v2-workspace-intelligence/src/Ferret.Knowledge.Federation/IFederatedKnowledgeStore.cs (score: 8.26)
```

**Invalidation check (remove-reference):**

```
ferret.exe workspaces remove-reference ferret-platform indoulia-foundation
```

output: `Workspace 'ferret-platform' no longer references 'indoulia-foundation'.` — confirmed against
the on-disk manifest, whose `references` array was removed entirely. A query for a term known to
score high specifically in `indoulia-foundation`'s content (`"workspace"` — before removal, its top 5
hits were exclusively `indoulia-foundation` paths such as
`docs/governance/garuda/implementation-readiness/Engineering-Workspace-Blueprint.md`) was re-run after
the removal:

```
ferret.exe workspaces query ferret-platform "workspace" --limit 10
```

All 10 results after removal were `[ferret-platform]`-sourced only — zero `indoulia-foundation` hits,
confirming the reference removal was correctly observed on the very next query (a fresh process, so
this exercises `FileWorkspaceRegistry` directly, not the in-process cache — but it confirms the
underlying `SaveAsync`/`ResolveAsync` contract the cache write-through relies on is intact).

**Restoration:** the reference was re-added immediately after —
`ferret.exe workspaces add-reference ferret-platform indoulia-foundation` — restoring
`ferret-platform`'s `workspace.json` to be byte-identical to its pre-session content (verified by
direct file comparison: same `schemaVersion: "1.1"`, same single `references` entry for
`indoulia-foundation`'s ID, no `pinnedStateHash` — it was floating before and is floating now).
`indoulia-foundation`'s manifest was never written to and is unchanged. `git status` in
`C:\POC\Ferret` at the end of this session shows nothing added or modified by this work beyond the new
doc file itself.

---

## 6. What Implementation Taught Us

- **Registry I/O was measurable but never the dominant cost — confirmed, not assumed.** §4's numbers
  (sub-millisecond baseline per call) confirm `20-Phase-3-Priority-Assessment.md` §2's own ranking:
  registry resolution was bottleneck #2, an order of magnitude cheaper *per call* than the fingerprint
  provider's full-repo re-hash (bottleneck #1, fixed in `21`). This cache's win is about **eliminating
  repetition across a long session**, not fixing an expensive single call — the opposite shape of the
  fingerprint fix.
- **The CLI itself never sees this speedup.** Identical to the fingerprint provider's own lesson
  (`21` §5): each `ferret workspaces query` invocation is a fresh process, so the singleton cache never
  survives past one command. The measured ~1000x per-call speedup in §4 only pays off inside a
  long-lived host — the MCP server or any future interactive/loop consumer that resolves the same
  workspace ID repeatedly in one process, exactly the audience `20-Phase-3-Priority-Assessment.md` §1
  named for this ticket.
- **Negative-cache correctness mattered as much as positive-cache correctness.** A `null` result
  (workspace not found) is cached identically to a real entry — verified both by the dedicated unit
  test and implicitly by every dogfooding query resolving `indoulia-foundation`'s ID cleanly. Missing
  this would have meant every reference to a *removed* workspace kept re-hitting disk forever, which
  is the opposite of the intended savings.
- **Write-through only refreshes what was already read, deliberately.** The `SaveAsync` behavior of
  never warming a cache entry purely from a write (only from a prior `ResolveAsync`) was easy to get
  backwards; the existing unit test suite (`ResolveAsync_AfterSaveAsyncOnTheCache_...`) locks in the
  intended asymmetry rather than relying on this doc to describe it correctly after the fact.
- **No corpus-scale surprise.** With only one reference in the real dogfooded `ferret-platform`
  workspace, this session couldn't observe the "compounds with reference count" scaling
  `20-Phase-3-Priority-Assessment.md` §2 predicted — the benchmark's 2-ID loop is a proxy for that
  scaling, not a live measurement of it at real multi-reference scale, which hasn't been dogfooded yet.

---

## 7. Architecture Validation

**Architecture upheld: yes, explicitly and without qualification.** No ADR was touched, no new
interface was introduced, and `FederatedKnowledgeStore`'s call sites are byte-for-byte unchanged —
the decorator is invisible to every existing consumer of `IWorkspaceRegistry`. The one assumption from
`20-Phase-3-Priority-Assessment.md` §1 that was directly tested and held exactly as predicted: WIP-032
is "really 'cache registry-entry reads,' not 'cache a graph walk'" — confirmed again in this session,
since `FederatedKnowledgeStore.ResolveSourcesAsync` was re-confirmed (by the dogfooding queries
resolving both the member repo and its one direct reference) to only ever call `ResolveAsync` for
direct references, never walk a transitive graph at query time. No assumption was invalidated. No new
ADR is required — none was required by the plan, and nothing discovered during implementation,
benchmarking, or dogfooding changed that. No technical debt was introduced: the cache is a small,
fully-tested decorator with no persisted state, no configuration surface, and no failure mode beyond
"propagate the inner registry's exception," which is the same failure mode the codebase already had.
