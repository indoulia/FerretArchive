# 16 — Vertical Slice Validation (Architecture Validation Ledger)

**Status:** Complete — WIP-SLICE-1/2 implemented, tested, dogfooded
**Purpose:** record whether the "Two Workspaces, One Cross-Repo Answer" vertical slice (`Backlog/backlog.md`) actually validated the milestone's central architectural bet, per `15-Execution-Plan.md` §6.

## 1. What Was Built

| Item | Where |
|---|---|
| `WorkspaceReference` + `WorkspaceRegistryEntry.References` (schemaVersion 1.0→1.1, additive) | `src/Ferret.Workspace.Graph/WorkspaceReference.cs`, `WorkspaceRegistryEntry.cs`, `FileWorkspaceRegistry.cs` |
| `ReferenceGraph.WouldCreateCycle` (DAG enforcement) | `src/Ferret.Workspace.Graph/ReferenceGraph.cs` |
| `ferret workspaces add-reference` | `src/Ferret.Cli/Commands/Workspaces/WorkspacesAddReferenceCommandHandler.cs` |
| `IFederatedKnowledgeStore` / `FederatedKnowledgeStore` (fan-out, merge, source tagging, graceful degradation) | `src/Ferret.Knowledge.Federation/` (new module, deps: `Ferret.Core`, `Ferret.Workspace.Graph` only) |
| `IRepoSearchServiceFactory` (abstraction) / `RepoSearchServiceFactory` (concrete, BM25-backed) | `src/Ferret.Knowledge.Federation/IRepoSearchServiceFactory.cs`, `src/Ferret.Cli/Commands/Workspaces/RepoSearchServiceFactory.cs` |
| `ferret workspaces query <workspace> <text>` | `src/Ferret.Cli/Commands/Workspaces/WorkspacesQueryCommandHandler.cs` |
| `SearchHit.SourceWorkspaceId` (citation tagging) | `src/Ferret.Core/Search/SearchHit.cs` |

**Deliberate deviation from the doc set, made explicit rather than silently absorbed:** `IKnowledgeStore` (ARCH-001 §27.2) was never implemented as a literal interface in this codebase — the real storage abstraction is `ISearchService`/`ISearchProvider`. `IFederatedKnowledgeStore` is defined as `ISearchService` with no added members, which is what "implements the same shape as `IKnowledgeStore`" (01-Architecture.md §3) means concretely here. No new query API, no new pattern — matches the doc's intent exactly, just against the interface that actually exists.

`Ferret knowledge query` (as named in 01/12) does not exist as a literal command; the real single-repo query surface is `ferret search`, which is hardwired to one CWD-resolved `IWorkspaceContext`. Making it workspace-aware is new infrastructure outside this slice's scope, so the query surface for the slice is `ferret workspaces query` — additive, zero risk to `ferret search`.

## 2. Test Coverage

| Layer | Count | What it proves |
|---|---|---|
| `Ferret.Workspace.Graph.Tests` (References + cycle detection) | 44 (6 new) | Schema additive/backward-compatible; DAG enforcement (direct + transitive cycles, diamond shared-dependency non-cycle) |
| `Ferret.Cli.Tests` (add-reference, query) | 221 (12 new) | CLI validation, error messages, cycle rejection at the command layer |
| `Ferret.Knowledge.Federation.Tests` | 7 | Fan-out, merge-by-score, source tagging, one-repo-unavailable degradation, zero cross-contamination between unrelated workspaces |
| `Ferret.Integration.Tests` (`WorkspaceFederationE2ETests`) | 3 | **The actual vertical slice**, with two real repos, real SQLite FTS5 indexes built by the real `ferret index` pipeline, and a real `FederatedKnowledgeStore` — see §3 |

Full solution: `dotnet build`/`dotnet test` on `src/Ferret.sln` — 0 warnings, 0 errors, 0 failing tests across all 61 projects, including `Ferret.Architecture.Tests` (dependency-direction rules unaffected).

## 3. Vertical Slice Evidence

`WorkspaceFederationE2ETests.FederatedQuery_AcrossTwoIndependentlyIndexedRepos_ReturnsCitedCrossRepoAnswer_WithNoDuplicatedIndexContent`:
- Two real temp directories, each with real content, each indexed independently via the real `ferret index` pipeline (real SQLite FTS5 databases on disk).
- Repo A's content references a symbol ("TokenValidator") it never defines; repo B defines it. Neither repo alone can answer a query for it.
- Workspace A references Workspace B via `add-reference`.
- A single federated query for "TokenValidator" returns exactly 2 hits, one tagged with A's workspace ID, one with B's — proving fan-out, merge, and citation.
- Asserted directly: repo A's own directory contains zero trace of repo B's content; repo B's own index file exists exactly once, only under repo B.

`FederatedQuery_WhenReferencedRepoIndexIsMissing_StillAnswersFromTheAvailableRepo`: repo B is never indexed; the query still succeeds with repo A's hit only — one repo unavailable does not corrupt the other.

`SingleRepoWorkspace_WithNoReferences_BehavesIdenticallyToPreFederationQuery`: a workspace with one repo and zero references returns the same single hit a pre-federation query would — the 14-Migration.md backward-compatibility invariant holds.

Also dogfooded live through the actual built `ferret.exe` (not just tests) — see `docs/dogfooding/` for the session log.

**Updated 2026-07-05 (Stabilization Sprint 1):** Founder Dogfooding Sprint 1 (`17-Dogfooding-Sprint-1.md`) found that this slice's "one repository may be unavailable without corrupting the other" claim held for a missing/unindexed repo but not for a permission-denied one, which crashed the whole query with an unhandled exception (Critical finding). `FederatedKnowledgeStore`'s fan-out now catches any per-source exception and degrades that source only, and every skipped source (exception, missing index, or a dangling/corrupt reference) is recorded in `SearchServiceResult.Diagnostics` and surfaced by `ferret workspaces query`. Proven with a real ACL-denial integration test (`FederatedQuery_WhenReferencedRepoIndexIsPermissionDenied_StillAnswersFromTheAvailableRepo_WithADiagnostic`), not a simulated exception — see `19-Stabilization-Sprint-1.md` for full evidence. §4's row on graceful degradation is amended accordingly.

## 4. Architecture Validation Ledger

| Question | Answer |
|---|---|
| **Architecture upheld?** | Yes. Live federation (ADR-0027) works exactly as designed: zero copying, zero re-indexing, results merged with source attribution, graceful degradation on an unavailable reference. No redesign was needed. |
| **New ADR required?** | No. Everything built is a direct, unmodified implementation of ADR-0026/ADR-0027 and `01-03`/`12`/`02`-Workspace-Model.md. |
| **Unexpected complexity?** | One: `IKnowledgeStore` doesn't exist in code, so "same shape" had to be resolved against the real `ISearchService` abstraction instead — a naming/mapping exercise, not a design problem (§1 above). |
| **Technical debt?** | (1) No `workspaces remove-reference` / `workspaces delete` command yet — Phase 2 backlog (WIP-021, now reprioritized ahead of WIP-022) already covers `remove-reference`; workspace deletion isn't tracked anywhere yet, worth a ticket. (2) ~~`workspaces show` doesn't display `References`~~ — **fixed in Stabilization Sprint 1.** |
| **Graceful degradation on an unavailable reference?** | **Amended 2026-07-05:** originally claimed unconditionally true; dogfooding evidence showed it held for a missing/unindexed repo and did not hold for a permission-denied one (crashed instead of degrading). Fixed in Stabilization Sprint 1 — see the note above and `19-Stabilization-Sprint-1.md`. |
| **Candidate improvements (not implemented, by design)** | Scope narrowing, compression, caching, pinning, telemetry — all explicitly excluded by the vertical slice's scope; all still land in Phase 2 (full) / Phase 3 per `15-Execution-Plan.md`. |

## 5. Dogfooding Findings (see also `docs/dogfooding/`)

- **Git worktree incompatibility (real bug):** `ferret workspaces add-repo` fails against a git worktree checkout — `RepoIdentityResolver` requires `.git/config` to be a real file at `<repo>/.git/config`, but a worktree's `.git` is itself a file (pointer to the main repo's git dir), not a directory. Reproduced live. Not fixed here (pre-existing WIP-012 code, out of this slice's scope) — worth a follow-up issue.
- **Pre-existing, unrelated bug surfaced:** `canonicalUri` is double-wrapped (`file:///filesystem:///...`) for filesystem-connector hits, reproduced via both `ferret search --format json` and `ferret workspaces query` on the same content. This is a `Bm25SearchProvider`/connector-layer bug that predates and is orthogonal to this milestone — not fixed here, worth filing.
- **`workspaces show` doesn't surface references** — a real usability gap noticed while dogfooding `add-reference`; the success message is the only confirmation a reference was added.
- **No `remove-reference`/undo path yet** — once added, a reference (or a whole workspace) can only be removed by hand-editing/deleting the registry's on-disk JSON; acceptable for this slice (explicitly out of scope), a real gap before this ships broadly.
- **What worked well:** identity resolution's local-identity fallback (ADR-0026) made two freshly-created, remote-less git repos usable immediately with no extra setup; the query output's `[workspace-name] hit` citation format read clearly with zero explanation needed.

## 6. Decision Log

| Decision | Outcome |
|---|---|
| `IFederatedKnowledgeStore` = `ISearchService`, no new members | Validated in code and tests |
| New CLI surface is `ferret workspaces query`, not a change to `ferret search` | Validated — zero regression risk, existing single-repo suite unaffected |
| `IRepoSearchServiceFactory` abstraction keeps `Ferret.Knowledge.Federation`'s only dependencies `Ferret.Core` + `Ferret.Workspace.Graph` | Validated — dependency direction matches 01-Architecture.md §2 exactly |
| Vertical slice validates the hypothesis; Phase 2 (full) can proceed | See §7, Final Question, in the session's implementation report |
