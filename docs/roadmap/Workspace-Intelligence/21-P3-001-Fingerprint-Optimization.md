# 21 — P3-001: Workspace State Fingerprint Optimization

**Status:** Complete
**Purpose:** Fix the un-numbered cost identified as highest-leverage in `20-Phase-3-Priority-Assessment.md` §1/§2 — `WorkspaceStateFingerprintProvider` re-hashing a pinned reference's entire repo content on every single query — without changing ADR-0027, pinning semantics, fail-closed behavior, federation, or the registry.

---

## 1. Architecture Validation — Is Reuse Possible?

**Question:** Does the existing indexing pipeline already compute enough information to derive the Workspace State Fingerprint, so the fingerprint provider can reuse it instead of re-walking and re-hashing file content?

**Answer: No — reuse of an existing hash as the fingerprint itself is not possible, for two independent reasons, both confirmed by reading the code directly (not inferred):**

1. **The only existing per-file signal is not content-based.** `IndexPipeline.RunAsync` (`src/Ferret.Indexing/IndexPipeline.cs:125`) already tracks a per-asset fingerprint for incremental indexing — `AssetFingerprint.CreateLightweight(asset.LastModified, asset.SizeBytes)` — persisted via `IIndexStateStore`. This is mtime+size, explicitly documented on `AssetFingerprint` itself as "no I/O required." The ADR-0027 Amendment requires the Workspace State Fingerprint to be **portable** — "must not depend on local filesystem metadata (mtime, absolute paths)" (invariant #3) — precisely because a fresh clone/checkout resets mtimes. `WorkspaceStateFingerprintProviderTests.ComputeFingerprintAsync_SameIdentityAndContentAtADifferentCheckoutPathAndMtime_ReturnsTheSameValue` already locks this in. Reusing the mtime-based incremental signal as the fingerprint would break that invariant outright.
2. **The SQLite document index doesn't cover the same file set.** `SqliteKeywordIndexEngine` only stores `Document` rows for assets that were successfully discovered, parsed, and indexed — `IndexPipeline` explicitly skips unsupported media types, empty content, and parse failures before ever calling `_engine.WriteAsync`. The current (and correct) fingerprint semantics hash **every file** in a member repo, indexable or not (binaries, config, `.gitignore`, anything). Deriving the fingerprint from indexed documents instead would silently narrow what counts as "the workspace changed," which is a fail-closed correctness regression for pinned references, not a safe optimization.

**Conclusion:** no existing artifact can serve as the fingerprint's source of truth. The fingerprint must keep deriving from a real, full, content-based hash. The optimization target is therefore *how often* that hash is recomputed, not *what* it's computed from.

---

## 2. Optimization Implemented

`WorkspaceStateFingerprintProvider` (`src/Ferret.Cli/Commands/Workspaces/WorkspaceStateFingerprintProvider.cs`) gained an in-memory, per-process, metadata-gated cache:

1. Every call still enumerates the repo's files via `FilesystemConnector.DiscoverAsync` (a directory walk — no content read), exactly as before.
2. From that enumeration, a **metadata signature** is built from `(CanonicalUri, SizeBytes, LastModified)` per file, hashed with SHA-256 — the same size+mtime heuristic `IndexPipeline` already uses for its own incremental skip check, applied here only as a cheap change-detector, never as the fingerprint's value.
3. If the metadata signature matches the last one computed for that exact local path, the previously-computed **content digest** is returned directly — no file is opened, no byte is read or hashed.
4. On any mismatch (or first call for that path), the full per-file open+SHA-256 content hash runs exactly as it did before, and both signatures are cached.

**Why this respects every stated constraint:**
- **No architecture change, no invented persistence.** The cache is a private `ConcurrentDictionary` field, in-memory only, lost on process exit — not a new storage subsystem, no migration, nothing written to disk.
- **ADR-0027 Amendment invariant #1 ("computed, not stored") holds.** Every call still performs a real computation (the directory walk + metadata check); the cache only skips the expensive sub-step (content hashing) when that computation proves nothing changed.
- **Portability is unaffected.** The cache key is the local path; two different checkouts of identical content are always independent cache misses that each get a fresh, full content hash — the existing "same content, different path/mtime → same fingerprint" test (`WorkspaceStateFingerprintProviderTests`) passes unmodified.
- **Fail-closed behavior, federation, and the registry are untouched** — same interface, same `null`-on-unreachable-repo behavior, same call sites in `FederatedKnowledgeStore`.

**Known trade-off, stated explicitly (not hidden):** the metadata gate uses size+mtime, the same heuristic `git`, `make`, and this codebase's own incremental indexer already rely on. A pathological edit that preserves both a file's size and its mtime exactly would go undetected until some other file in the repo changes. This is an accepted, industry-standard trade-off for the win in the common case, not a silent weakening — content hashing itself is unchanged and still runs in full whenever the gate trips.

---

## 3. Tests

Added to `WorkspaceStateFingerprintProviderTests` (all passing, alongside the 4 pre-existing tests which pass unmodified):

| Test | Proves |
|---|---|
| `ComputeFingerprintAsync_CalledTwiceOnUnchangedContent_ReturnsTheSameValue` (pre-existing) | Identical content → identical fingerprint |
| `ComputeFingerprintAsync_SameIdentityAndContentAtADifferentCheckoutPathAndMtime_ReturnsTheSameValue` (pre-existing) | Cloned repository → identical fingerprint |
| `ComputeFingerprintAsync_WhenFileContentChanges_ReturnsADifferentValue` (pre-existing) | Changed content → changed fingerprint |
| `ComputeFingerprintAsync_CalledTwiceOnUnchangedContent_SkipsRecomputingContentHash` (new) | Repeated query avoids unnecessary recomputation — asserts `ContentDigestComputationCount == 1` after two calls |
| `ComputeFingerprintAsync_WhenFileContentChanges_RecomputesContentHash` (new) | Cache correctly invalidates — asserts the count increments to 2 |
| `ComputeFingerprintAsync_WhenRepoLocalPathIsUnreachable_ReturnsNull` (pre-existing) | Fail-closed path unaffected |

`ContentDigestComputationCount` is an `internal`-only counter (test observability, `InternalsVisibleTo("Ferret.Cli.Tests")` already existed) — not public API surface.

`dotnet build src/Ferret.sln`: 0 warnings, 0 errors. `dotnet format --verify-no-changes`: clean. Full suites re-run: `Ferret.Cli.Tests` (247 passed), `Ferret.Knowledge.Federation.Tests` (15 passed), `Ferret.Indexing.Tests` (51 passed, 1 pre-existing skip). `Ferret.Integration.Tests.WorkspaceE2ETests` showed one failure on a full-suite run and passed cleanly in isolation — the same pre-existing temp-directory test-parallelization flake already documented in `19-Stabilization-Sprint-1.md` §2, unrelated to this change (different subsystem: per-repo `.ferret/` init, not workspace federation).

---

## 4. Dogfooding — Same Pinned-Reference Workflow as `17-Dogfooding-Sprint-1.md`

Reused the real, still-live registration from Founder Dogfooding Sprint 1 (`17-Dogfooding-Sprint-1.md` §Scenario 2): workspace `ferret-platform` (`C:\POC\Ferret`) referencing workspace `indoulia-foundation` (`C:\POC\indoulia-foundation`, a real separate repo, 548 files). Ran:

```
ferret workspaces pin-reference ferret-platform indoulia-foundation
```

then exercised `ComputeFingerprintAsync` against the real pinned repo directly (same code path `FederatedKnowledgeStore.ResolveSourcesAsync` calls on every query with a pinned reference), measuring:

| Scenario | Time | Notes |
|---|---|---|
| First query (cold) | 251–291 ms | Full walk + open + SHA-256 every file, as before |
| Repeated query (warm, unchanged) | 13–17 ms | Metadata scan only — **~15–20x faster**, zero files opened |
| Repeated query (warm, unchanged) | 13–16 ms | Confirms it isn't a one-off JIT/cache-warming artifact |
| After a real file added to the repo | 216 ms | Correctly detected as changed, fell back to full re-hash, produced a different fingerprint |

`ContentDigestComputationCount` was 1 after three identical queries and 2 after the real content change — hashing happened exactly when it needed to, and not once more.

State was left as found: reference `unpin-reference`d back to floating afterward, and the temporary probe file used to trigger the "modified" scenario was deleted (`git status` in `indoulia-foundation` shows the same pre-existing untracked entries as before, nothing added by this session).

---

## 5. Implementation Lessons

- **The "obvious" reuse (an existing hash) was a trap, not a shortcut.** Both candidate reuse sources looked promising from the doc level (`05-Context-Optimization.md`'s premise, `IIndexStateStore`'s existence) and both failed for reasons only visible by reading the actual code and the actual invariant text in the ADR amendment — not by re-reading the design docs harder. Confirming "reuse is impossible" took the same rigor as building the fix.
- **The real bottleneck was byte-hashing, not enumeration.** The directory walk (`DiscoverAsync`) was never the expensive part — it's a stat-only scan. Keeping it unconditional (run every call) while gating only the content-read step kept the change small and the invariant ("computed, not stored") intact, rather than trying to cache the walk itself.
- **The singleton registration mattered as much as the code change.** `WorkspaceStateFingerprintProvider` was already `AddSingleton` in `WorkspacesCliModule` before this work — without that, an in-memory cache field would reset every call and do nothing. The optimization is a no-op for the one-shot `ferret workspaces query` CLI process (each invocation is a fresh process, cache never survives past it) and pays off specifically for long-lived hosts issuing repeated queries in one process — the MCP server, per `20-Phase-3-Priority-Assessment.md`'s own framing of who benefits ("MCP tool loops, interactive dogfooding"). Worth stating plainly rather than implying a CLI-visible speedup that doesn't exist.
- **Confirmed the size/mtime heuristic is already a codebase precedent**, not a new risk introduced here — `IndexPipeline` relies on the identical signal for its own incremental-indexing skip check. Extending it to gate the pinned-reference cache introduces no new class of correctness risk beyond one already accepted elsewhere in this codebase.
