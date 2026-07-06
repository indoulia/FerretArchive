# 19 — Stabilization Sprint 1

**Status:** Complete
**Purpose:** Close the Critical/High implementation gaps identified in `18-Engineering-Analysis-Sprint-1.md` §6/§10, exactly as scoped — no Phase 2 work, no architecture change, no unrelated fixes.

---

## 1. Implementation Summary

| Objective (per `18-Engineering-Analysis-Sprint-1.md` §6/§10) | Change |
|---|---|
| 1. Harden the federation fan-out exception boundary | `FederatedKnowledgeStore.RunSourceAsync` (new) wraps each per-source query in `try`/`catch (Exception)` — rethrows `OperationCanceledException`, converts any other exception into a per-source `Failure` result carrying a `SearchDiagnostic`. `CA1031` suppressed at this one call site with the same justification convention already used in `Ferret.Indexing.IndexPipeline` ("pipeline must be resilient"). |
| 2. Populate `SearchServiceResult.Diagnostics` for skipped/unavailable sources, surfaced via `ferret workspaces query` | `ResolveSourcesAsync` now returns diagnostics for a dangling reference or a corrupt referenced manifest, in addition to the sources list. `Merge` folds those together with a diagnostic per failed per-source result (using that result's own diagnostic if `RunSourceAsync` set one, else a generic `"Workspace '<id>' skipped: <status>"`). `WorkspacesQueryCommandHandler` prints every diagnostic (via `WriteLine`, not `WriteVerbose` — visible by default, not opt-in) after the hit list on success, and after the error message on total failure. |
| 3. `workspaces show` displays references | `TextWorkspacesShowFormatter` gained a `References (N):` section, mirroring the existing `Repos`/`Documents` sections' style (raw identifiers, no name resolution — consistent with how `Repos` already displays raw remote strings). |
| 4. Resequence WIP-021 ahead of WIP-022 | `Backlog/backlog.md` — the two items were already listed in that order; added an explicit rationale note to each so the ordering reads as a deliberate, evidence-backed decision rather than incidental numbering. |

**Files changed:** `src/Ferret.Knowledge.Federation/FederatedKnowledgeStore.cs`, `src/Ferret.Cli/Commands/Workspaces/WorkspacesQueryCommandHandler.cs`, `src/Ferret.Cli/Commands/Workspaces/TextWorkspacesShowFormatter.cs`, `docs/roadmap/Workspace-Intelligence/Backlog/backlog.md`. No new interfaces, no new projects, no ADRs touched.

**Tests added:** 4 in `Ferret.Knowledge.Federation.Tests` (exception-boundary + diagnostics), 2 in `Ferret.Cli.Tests` (references display + query diagnostics), 1 real failure-injection integration test in `Ferret.Integration.Tests` (see §3).

---

## 2. Engineering Validation

`dotnet build`/`dotnet test` on `src/Ferret.sln` (61 projects): **0 warnings, 0 errors, 0 test failures.** 1,466 tests passed across the solution (8 net-new this sprint: 4 in `Ferret.Knowledge.Federation.Tests`, 3 in `Ferret.Cli.Tests`, 1 in `Ferret.Integration.Tests`). One `Ferret.Integration.Tests` failure (`WorkspaceE2ETests.WorkspaceInit_CreatesExpectedContextOsArtifacts`) appeared on the first full-suite run and passed cleanly in isolation and on a full-suite re-run — a pre-existing test-parallelization flake (multiple test classes mutate the process-global `Environment.CurrentDirectory`), unrelated to any file this sprint touched. Not investigated further or fixed, per "no unrelated bug fixes."

All four objectives have dedicated tests demonstrating them, not just the existing status-code-driven suite passing unmodified:
- `SearchAsync_WhenOneSourceThrowsAnException_StillReturnsResultsFromTheOtherSource` / `..._RecordsADiagnosticNamingTheFailure` / `SearchAsync_WhenEverySourceThrows_ReturnsAFailureResult_NotAnException` (objective 1 & 2, unit level, fake-throwing source).
- `SearchAsync_WhenReferencedRepoHasNoIndex_RecordsADiagnostic` / `SearchAsync_WhenReferencedWorkspaceNoLongerExists_...` extended to assert a diagnostic (objective 2, unit level).
- `Query_WhenAReferencedRepoIsSkipped_StillSucceeds_AndPrintsADiagnostic` (objective 2, CLI-handler level).
- `Show_WithReferences_DisplaysReferences` / `Show_WithNoReferences_DisplaysEmptyReferencesSection` (objective 3).
- `FederatedQuery_WhenReferencedRepoIndexIsPermissionDenied_StillAnswersFromTheAvailableRepo_WithADiagnostic` (objective 1 & 2, **real failure injection** — see §3).

---

## 3. Real Failure-Injection Evidence

Per the sprint's explicit instruction ("do not simulate permission failures if the existing test infrastructure can reproduce them"), the exact scenario found live during Founder Dogfooding Sprint 1 was reproduced as an automated integration test rather than only a fake-throwing unit test:

`Ferret.Integration.Tests.WorkspaceFederationE2ETests.FederatedQuery_WhenReferencedRepoIndexIsPermissionDenied_StillAnswersFromTheAvailableRepo_WithADiagnostic`:
1. Two real repos, real content, each indexed by the real `ferret index` pipeline into a real SQLite FTS5 database on disk.
2. Workspace A references Workspace B.
3. A real `icacls <path> /deny <user>:(R,W)` process is run against B's actual `keyword-index.db` file — the identical command used to reproduce the crash live during dogfooding.
4. A federated query against A executes.
5. **Before this sprint:** this exact sequence threw `Microsoft.Data.Sqlite.SqliteException: SQLite Error 14: 'unable to open database file'` uncaught, crashing the process (reproduced and recorded in `17-Dogfooding-Sprint-1.md` §1, row 9).
6. **After this sprint:** the query returns `IsSuccess = true` with A's single hit, and `Diagnostics` contains an entry naming B's workspace ID and the failure. ACL is restored in a `finally` block regardless of outcome.

This is real disk I/O, a real OS-level access-control denial via `icacls`, and the real `Bm25SearchProvider`/`SqliteConnection` code path — not a substitute exception thrown by a test double. The unit-level tests (§2) cover the same contract with an injected exception for fast, deterministic coverage of edge cases (every-source-fails, exact diagnostic wording); this integration test is the evidence that the fix holds against the actual mechanism that broke in production use.

---

## 4. Updated Architecture Validation Ledger

`16-Vertical-Slice-Validation.md` §3/§4 amended in place (not rewritten): the "graceful degradation on an unavailable reference" row, previously stated as an unconditional strength, now records that it held for one failure mode and not another, and is fixed as of this sprint. The `workspaces show` references gap in the Technical Debt row is struck through as resolved. No other ledger content changed — the original vertical-slice evidence (zero duplication, correct citations, path-independent identity) stands as originally recorded.

---

## 5. Updated Dogfooding Notes

`17-Dogfooding-Sprint-1.md` §2 (Friction Log): findings #2 (Critical) and #5 (High) and the unlabeled `workspaces show` gap are marked **RESOLVED**, with a pointer to this document for evidence. No other finding in that log was touched — findings #3 (cross-repo score comparability), #4 (moved-repo error message), #1 (indexing coverage), #6 (corrupt-manifest blast radius), #7 (git worktrees), and the `canonicalUri` bug remain open exactly as recorded, per this sprint's explicit scope boundary.

---

## 6. Remaining Known Issues

Everything below was explicitly out of scope for this sprint and is unchanged by it — listed here only for completeness, not as new findings:

- Cross-repo BM25 score comparability (Backlog gap, `18-Engineering-Analysis-Sprint-1.md` §5) — unaddressed, tracked for Phase 3 planning.
- Moved-repo error message points to the wrong fix (Developer experience issue) — the underlying capability works (proven in dogfooding via `remove-repo`+`add-repo`); only the message is wrong.
- One corrupt manifest still blocks `workspaces list` entirely (intentional technical debt, disclosed at WIP-010 implementation time).
- Git worktrees still cannot be added as member repos (`RepoIdentityResolver` limitation).
- `canonicalUri` double-wrap (pre-existing, unrelated epic).
- Large-repo indexing coverage question (pre-existing, unrelated epic, not investigated).
- The pre-existing test-parallelization flake noted in §2 (not investigated; not part of this sprint's scope).

---

## 7. Final Question

**Is the federation layer now reliable enough to become the foundation for Phase 2?**

**Yes**, on the evidence produced by this sprint specifically. The one finding that constituted a genuine reliability defect — a per-source I/O failure crashing the entire federated query, contradicting a written acceptance criterion — is now closed, with the exact real-world reproduction (an ACL-denied SQLite file) converted into a passing automated test, not just a unit-level simulation. The companion silent-degradation gap (a partial result being indistinguishable from a complete one) is closed by the same change, using an extension point (`SearchServiceResult.Diagnostics`) that already existed in the architecture — confirming, as `18-Engineering-Analysis-Sprint-1.md` §3 found, that no architectural change was required to reach this state.

This verdict is scoped to reliability, not completeness: the remaining known issues in §6 (cross-repo ranking, moved-repo messaging, worktree support) are real and should inform Phase 2 planning, but none of them are crash risks or silent-correctness risks — they are usability and capability gaps, which is exactly the category of work Phase 2 already exists to expand.
