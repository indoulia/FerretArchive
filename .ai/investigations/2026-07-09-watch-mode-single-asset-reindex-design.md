# Design — Single-Asset Incremental Reindex for `ferret watch` (Issue #17)

| Field | Value |
|---|---|
| **Status** | Design — Pending Independent Engineering Review |
| **Date** | 2026-07-09 |
| **Source issue** | Dogfooding #17 (`docs/archive/dogfooding/2026-07-06-daily-log.md`), root-caused with measured evidence: 23.22s from save to searchable vs. a documented ≤5s target. |
| **Scope** | `Ferret.Core` (`IAssetSource`, `IIndexPipeline`), `Ferret.Indexing` (`IndexPipeline`), `Ferret.Connectors.Filesystem` (`FilesystemConnector`), `Ferret.Cli` (`WatchCommandHandler`). |

This is a scoped bug-fix design, not a V2 Workspace-Intelligence-Platform architecture document — it does not use the `ARCH-NNN`/`FEAT-NNN` numbering (that series belongs to the V2 program's own governed roadmap, `V2-ROADMAP-001-Architecture-Program.md`, which this fix is not part of), and it does not fabricate an ID from that registry.

## Problem

`WatchCommandHandler.ProcessChangesAsync` (`src/Ferret.Cli/Commands/Watch/WatchCommandHandler.cs:140`) calls `IIndexPipeline.RunAsync(...)` for every debounced batch of file-system-watcher events, regardless of how many files actually changed. `IndexPipeline.RunAsync` (`src/Ferret.Indexing/IndexPipeline.cs:96`) always performs a full `IAssetSource.DiscoverAsync` walk of the entire workspace and computes/compares a fingerprint for every discovered asset — `IndexPipelineOptions.ForceRebuild = false` only skips the `ClearAsync` calls at lines 65-69, it does not skip the discovery walk. So a one-file edit still costs O(corpus), which is the measured 23.22s.

## Goal

Reindex exactly the asset(s) the watcher reported changed, in roughly O(1) work per changed file, while preserving every existing pipeline guarantee: fingerprint-based skip, the filesystem connector's skip-dir/ignore-provider filtering, state-store consistency, and existing domain-event publication.

## Design

**1. `IAssetSource` gains a single-asset lookup** (`src/Ferret.Core/Connectors/IAssetSource.cs`):

```csharp
/// <summary>Resolves a single asset by its canonical Id without a full discovery walk.
/// Returns null if the asset no longer exists or is excluded by this source's ignore policy.</summary>
Task<AssetDescriptor?> TryGetAsync(AssetId assetId, CancellationToken ct = default);
```

`FilesystemConnector` implements it by mapping the canonical URI back to an absolute path (the same unescape+join logic `OpenAsync` already uses), then statting that one path. It must apply the *same* eligibility rules `WalkDirectoryAsync` applies (`HardcodedSkipDirs` check on every ancestor segment, plus `IgnoreProvider.ShouldIgnore`) — refactored into one shared private static helper so the two paths cannot silently diverge and start disagreeing about what's indexable. `IAssetSource` currently has exactly one implementor in the codebase (confirmed by grep), so this additive interface member has a blast radius of one class.

**2. `IIndexPipeline` gains a single-asset entry point** (`src/Ferret.Core/Indexing/IIndexPipeline.cs`):

```csharp
/// <summary>Reindexes a single already-known-changed asset without a full corpus discovery walk.
/// Removes it from the index and state store if it no longer resolves (deleted, moved out of scope, or newly ignored).</summary>
Task<IndexResult> RunSingleAssetAsync(WorkspaceId workspaceId, AssetId assetId, CancellationToken ct = default);
```

`IndexPipeline` implements it by iterating active connector runtimes (same enumeration `RunAsync` already does) and calling `TryGetAsync` on the first one that returns non-null. The per-asset work itself (fingerprint compare → `OpenAsync` → `DispatchAsync` → `WriteAsync` → `SetFingerprintAsync` → event publication) is extracted from `RunAsync`'s inner loop into a shared private method so `RunAsync` and `RunSingleAssetAsync` cannot drift apart. If no connector resolves the asset, it is deleted from `IIndexEngine` and removed from `IIndexStateStore` — the same cleanup `RunAsync`'s stale-asset sweep performs, just scoped to one asset instead of a set difference over the whole corpus. `RunSingleAssetAsync` intentionally does **not** run the global stale-sweep (`GetAllKeysAsync().Except(seenAssets)`) — that stays O(corpus) by nature and is out of scope here; the existing full `ferret index` path remains the periodic safety net for corpus-wide drift (e.g. files deleted while `ferret watch` wasn't running).

**3. `WatchCommandHandler.ProcessChangesAsync` changes** (`src/Ferret.Cli/Commands/Watch/WatchCommandHandler.cs`):

- Modifications: replace the single `_pipeline.RunAsync(...)` call for the whole batch with one `_pipeline.RunSingleAssetAsync(workspaceId, assetId, ct)` call per distinct changed path. Debounced batches are consistently single-file in the dogfooding-log evidence, so a loop is simpler and easier to test than a new batch-shaped API.
- Deletions: keep the existing `_engine.DeleteAsync(docId, ct)` call, and **add** `_stateStore.RemoveAsync(assetId, ct)` alongside it. This is a real, closely-related gap this change surfaces: today, a deleted file's fingerprint is left in the state store, silently relying on some *future* full `RunAsync`'s stale-sweep to clean it up. Once the hot path for watch mode stops calling `RunAsync` at all, that reliance becomes indefinite for a long-running `ferret watch` session — this closes it now rather than leaving a newly-introduced leak. Requires injecting `IIndexStateStore` into `WatchCommandHandler` (currently only `IIndexPipeline`/`IIndexEngine`/`IWorkspaceContext`/`ILogger`).

## Alternatives considered

- **Cache the previous `DiscoverAsync` listing and diff in memory.** Rejected — still O(corpus) per watch event; moves the cost, doesn't remove it.
- **A connector-native push/change-feed model.** Rejected as out of scope — a much larger redesign with no supporting evidence in the backlog that it's needed now; the measured problem is fully explained by the full-walk-per-event pattern.
- **A batch-shaped `RunSingleAssetAsync(IReadOnlyList<AssetId>)`.** Deferred, not rejected — would save a few redundant connector-manager lookups for multi-file batches, but the dogfooding-log evidence shows debounced batches are consistently small (single-file edits), so the simpler single-asset signature is preferred for now. Can be added later without breaking this design.

## Architecture-conformance check (ADR-0030)

No new Core→Workspace/Search dependency, and no vocabulary leak: `IAssetSource` and `IIndexPipeline` both already live in `Ferret.Core`; the new members use only existing Core vocabulary (`AssetId`, `AssetDescriptor`, `WorkspaceId`). `IIndexStateStore` is also already `Ferret.Core.Indexing`, so `WatchCommandHandler` (which already depends on `Ferret.Core.Indexing`) gains no new project dependency.

## Independent Engineering Review — findings and resolutions

An independent adversarial review (2026-07-09) verified this design against the current source and found 7 real issues. Resolutions, incorporated into this design before implementation:

1. **ADR-0012 (Milestone 1 Platform Foundation Freeze) applies and was not checked.** `Ferret.Core` and `Ferret.Cli` are frozen: "No breaking changes ... without a superseding ADR" but "Bug fixes and non-breaking additions are allowed without an ADR" (ADR-0012 rule 2). **Resolution:** both new interface members are added as C# default interface methods, not required abstract members — `IAssetSource.TryGetAsync` defaults to `Task.FromResult<AssetDescriptor?>(null)` ("this source has nothing at that Id" — behaviorally correct for any source that hasn't implemented real single-lookup), and `IIndexPipeline.RunSingleAssetAsync` defaults to delegating to `RunAsync(workspaceId, IndexPipelineOptions.Default, ct)` (falls back to exact previous behavior). Neither existing implementor needs to change to keep compiling or behaving as before — this is a non-breaking addition per rule 2, no superseding ADR needed. `WatchCommandHandler`'s constructor gains a required `IIndexStateStore` parameter, but the class is `internal sealed`, not part of `Ferret.Cli`'s public surface, and DI auto-resolves the added dependency (already registered, consumed by `IndexPipeline`) — not a breaking change under rule 1.
2. **"Blast radius: one class" was wrong — 5 `IAssetSource` and 2 `IIndexPipeline` test-double implementors exist.** With the default-interface-method approach in (1), none of them need to change to keep compiling; they simply inherit the default (no-op / delegate-to-RunAsync) behavior. Resolution folded into the Implementation Plan: only the tests that specifically exercise the new single-asset path need new/updated fakes; the rest are untouched.
3. **`WatchCommandHandlerTests.cs` (2 call sites) break at compile time regardless — adding a constructor parameter isn't a DIM concern.** Both `new WatchCommandHandler(pipeline, engine, workspaceContext, logger)` sites must be updated to pass a fake `IIndexStateStore`. Folded into the Implementation Plan as an explicit step.
4. **Directory-created events must not reach per-asset processing as if they were documents.** `RunSingleAssetAsync`'s shared per-asset helper re-applies the exact `asset.Kind != AssetKind.File` guard `RunAsync` already has at `IndexPipeline.cs:101-104`, and applies it *before* any not-found/delete-cleanup branch: a resolved-but-non-File descriptor is a plain no-op (never indexed, so nothing to delete), distinct from a null descriptor (genuinely gone → delete+remove-from-state-store).
5. **Multi-connector-instance `AssetId` collision — pre-existing ambiguity, not solved or worsened here.** The canonical `filesystem:///relative/path` URI carries no instance discriminator, so two connector instances rooted at different paths can theoretically mint the same `AssetId`. `RunAsync` already has an equivalent, differently-shaped ambiguity today (multiple connectors' `DiscoverAsync` results for a colliding Id are each processed as encountered, i.e. last-write-wins across connectors); `RunSingleAssetAsync`'s "first connector to resolve it wins" is a different arbitrary rule over the same pre-existing gap in the `AssetId` scheme. Fixing the scheme itself is out of scope for this latency fix — noted here as a carried-over, not introduced, limitation.
6. **`BuildDocumentId` doesn't expose the intermediate `AssetId`, which the new state-store cleanup needs.** Resolution: split into `BuildAssetId(string absolutePath) : AssetId` (the existing logic) plus `BuildDocumentId` calling it (`DocumentId.From(BuildAssetId(path))`); the deletion branch uses both. Folded into the Implementation Plan.
7. **Fire-and-forget concurrency (`_ = ProcessChangesAsync(...)`) is pre-existing, not introduced or worsened.** Two overlapping debounce windows on the same path could already race today via `RunAsync`; scoping the hot path to a single asset narrows (does not widen) the blast radius of that pre-existing race. Not addressed here — out of scope for this fix, noted for future reference.

## Post-implementation Engineering Review — findings and resolutions

A second independent review, run against the implemented diff (not just the design), found two real bugs and two accepted tradeoffs:

1. **Real bug, fixed** — `FilesystemConnector.TryGetAsync` checked the ignore provider against the leaf asset only, not its ancestor directories. `WalkDirectoryAsync` checks every ancestor directory descriptor as it walks and never descends into an ignored one, so a `.ferretignore` pattern like `vendor` correctly hides `src/vendor/lib.js` from a full walk but `TryGetAsync` for that same path returned it anyway — exactly the single-path/full-walk divergence this design commits to preventing. Fixed by walking every ancestor directory from the leaf's parent up to the workspace root and checking `IgnoreProvider.ShouldIgnore` on each, in addition to the leaf. Covered by a new test (`TryGetAsync_FileUnderAncestorDirectoryIgnoredByFerretIgnore_ReturnsNull`).
2. **Real bug, fixed** — `WatchCommandHandler`'s new `_stateStore.RemoveAsync` call on deletion mutated only in-memory state; nothing flushed it to disk for a deletion-only batch (modifications get saved because `RunSingleAssetAsync` calls `SaveAsync` internally, but deletions bypass the pipeline entirely). A process exit right after a deletion-only batch would lose the removal. Fixed by calling `_stateStore.SaveAsync` once after the deletion loop. Covered by a new assertion in `ExecuteAsync_FileDeleted_RemovesFromEngineAndStateStore`.
3. **Accepted tradeoff, not changed** — `RunSingleAssetAsync` calls `_stateStore.SaveAsync` on every invocation; `WatchCommandHandler` now calls it once per changed path in a batch, versus one `SaveAsync` per whole batch under the old full-`RunAsync` call. This multiplies state-store persistence I/O by batch size for a multi-file batch. Not changed because: (a) the dogfooding-log evidence this fix is based on shows debounced batches are consistently single-file, so the multiplier is typically 1; (b) making each call self-durable (matching `RunAsync`'s own always-save behaviour) is a correctness property worth a small, bounded I/O cost over batching saves at the `WatchCommandHandler` layer, which would weaken that guarantee for any other future caller of `RunSingleAssetAsync`.
4. **Documentation correction, not a code change** — this document's Goal section originally said the fix preserves "existing domain-event publication" without qualification. In fact `RunSingleAssetAsync` deliberately does not publish `IndexingStartedEvent`/`IndexingCompletedEvent` (unlike `RunAsync`) — publishing a "corpus indexing started/completed" pair for a single-file watch save would misrepresent what happened to any future subscriber keying off `IsRebuild`/corpus-level semantics. Verified no current subscriber is affected: `ConsoleIndexEventSink` (the only consumer of these two events) is constructed inline only by `IndexCommandHandler` when `--verbose` is passed, and is not wired into `ferret watch` at all. Per-asset events (`DocumentDiscoveredEvent`, `DocumentIndexedEvent`, `DocumentSkippedEvent`, `DocumentParsingFailedEvent`) are preserved and published exactly as `RunAsync` does. The Goal statement above should be read as "existing per-asset domain-event publication," not the two corpus-level lifecycle events.

## Test plan (for the Implementation phase)

- `Ferret.Connectors.Filesystem.Tests`: `TryGetAsync` returns the correct descriptor for an existing eligible file; returns `null` for a non-existent path; returns `null` for a path under a hardcoded-skip directory; returns `null` for a path excluded by `IgnoreProvider`; returns the descriptor (not `null`) for a directory path — callers, not the connector, decide whether a directory is processable.
- `Ferret.Core.Tests` (or wherever `IAssetSource`'s own contract is exercised, if anywhere): default `TryGetAsync` on a bare interface implementation returns `null` — proves the DIM default is non-breaking.
- `Ferret.Indexing.Tests`: `RunSingleAssetAsync` indexes a changed asset; skips an asset whose fingerprint is unchanged; is a no-op (no delete, no state-store write) when the resolved descriptor's `Kind != AssetKind.File`; deletes+removes-from-state-store an asset that no longer resolves at all; does not touch unrelated state-store entries (no global sweep); default `RunSingleAssetAsync` on a bare `IIndexPipeline` implementation delegates to `RunAsync`.
- `Ferret.Cli.Tests`: update both existing `WatchCommandHandlerTests.cs` constructor call sites to pass a new `FakeWatchStateStore : IIndexStateStore`; `ProcessChangesAsync` calls `RunSingleAssetAsync` once per distinct changed path instead of `RunAsync`; a deletion calls both `IIndexEngine.DeleteAsync` and `IIndexStateStore.RemoveAsync` with the same asset's `DocumentId`/`AssetId`.
