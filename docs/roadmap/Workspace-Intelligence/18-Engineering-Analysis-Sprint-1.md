# 18 — Engineering Analysis: Founder Dogfooding Sprint 1

**Status:** Complete — analysis only, no implementation
**Purpose:** Convert `17-Dogfooding-Sprint-1.md` (the sole evidentiary input) into a classified, risk-ranked, executable engineering action plan, per the Garuda Engineering Playbook's governance-classification discipline. No code was changed to produce this document.

---

## 1. Executive Summary

| Dimension | Verdict |
|---|---|
| **Architecture** | **Validated, with one weakened assumption.** Every structural claim tested under real, adversarial conditions held: zero index duplication, live federation with no copying, path-independent identity (confirmed via a genuine cross-machine `git clone`), and correct citations in 100% of trials. One assumption — that an unavailable reference "degrades" a query rather than breaking it (ADR-0027) — is weakened by evidence: it holds for a missing/unindexed repo, and is contradicted for a permission-denied one, which crashes instead of degrading. |
| **Product direction** | **Validated.** The dogfooding journal produced a real capability with zero equivalent in today's Ferret (a cross-repo answer, correctly cited, that a single-repo `ferret search` cannot produce at all). No evidence surfaced suggests the wrong problem is being solved. |
| **Engineering quality** | **Not yet ready for expansion.** Test coverage (44 + 221 + 7 + 3 = 275 tests passing) did not catch the two highest-severity findings — an unhandled-exception crash under a permission failure, and a silent, undetectable degradation on an unreachable reference — because the tests validated `IsSuccess`/`Hits.Count`, not real I/O failure or human-observable output. This is a test-strategy gap, not a volume gap. |

---

## 2. Finding Classification

Every finding from `17-Dogfooding-Sprint-1.md` §2, classified into exactly one category. Positive/confirming observations (zero duplication, correct citations, portable identity) are evidentiary support for §3 below, not defects, and are not forced into this table.

| Finding | Classification | Justification |
|---|---|---|
| Permission-denied index file crashes the federated query with an unhandled `SqliteException` | **Reliability issue** | The narrow root cause (`Bm25SearchProvider` catches only `SqliteException` code 1) is a code-level defect, but its *effect* — one repo's I/O failure taking down the entire query rather than degrading — is a violation of a stated resilience guarantee ("one repository may be unavailable without corrupting the other"). Classified by impact, not by line count. |
| Reference resolution fails silently — no diagnostic signal when a referenced workspace is unreachable | **Implementation defect** | `SearchServiceResult.Diagnostics` already exists as a field for exactly this purpose; it is simply never populated on this path. The fix is wiring an existing extension point, not new architecture — a defect of omission, not design. |
| Cross-repo BM25 scores are not comparable (a large corpus and a small corpus produce scores on different scales) | **Backlog gap** | No ADR or design doc ever promised normalized cross-source ranking; `03-Cross-Workspace-References.md` §5 explicitly defers conflict/precedence handling. This is an unaddressed area surfaced by real evidence, not a broken promise — it needs a decision and a ticket, not a fix to something that was claimed to work. |
| Moved/renamed repo produces a misleading error ("run `ferret index`") and `add-repo` rejects the fix with no hint of `remove-repo` | **Developer experience issue** | The underlying capability (identity survives a move) works correctly and a repair path exists; the defect is entirely in what the CLI tells the user, not in what it does. |
| One corrupt workspace manifest blocks `workspaces list` for every workspace | **Backlog gap** | Already a disclosed, intentional WIP-010 scope decision at implementation time (not a surprise defect), with a self-aware, actionable error message. Dogfooding confirmed the cost of that trade-off is real once a user has several workspaces — it needs a future ticket, not urgent remediation. |
| Git worktrees cannot be added as member repos (`.git` is a file, not a directory, in a worktree) | **Implementation defect** | ADR-0026's identity principle (canonicalize the origin remote from git config) is architecturally sound; `RepoIdentityResolver` simply doesn't resolve the worktree indirection. A scoped code fix, not a design change. |
| Zero-config connector attempts (and fails, non-fatally) to index locked tool-database files (`.tokensave/*.db`) | **Developer experience issue** | Reported clearly as a per-file failure, not a crash — correct failure handling, but a default that surprises a user pointing the tool at a real, messy working directory. Pre-existing, outside this epic. |
| Real ~25K-file repo: 2537 discovered, only 28 indexed, 2506 skipped | **Developer experience issue** | Indexing completed without error in ~1s; the surprise is coverage, not speed or crash risk. Root cause was not investigated (out of this epic's scope) — flagged, not diagnosed. |
| `workspaces show` does not display `references` after `add-reference` succeeds | **Developer experience issue** | The data is stored and read back correctly (`workspaces query` proves it); only the display layer is incomplete. |
| No `remove-reference` or workspace-delete command | **Backlog gap** | `remove-reference` is already WIP-021 on the Phase 2 backlog (needs resequencing, see §5); workspace deletion has no ticket anywhere and needs one. |
| `canonicalUri` double-wrap (`file:///filesystem:///...`) | **Implementation defect** | Reproduced via plain `ferret search --format json` on content untouched by this epic — pre-existing, unrelated to Workspace Intelligence, belongs to the base search/indexing epic's backlog, not this one. |
| Accidental deletion of the entire `~/.ferret/workspaces` registry during scripted dogfooding | **Process issue** | Root-caused to an unvalidated variable in a destructive shell command against shared state, not a Ferret defect. Already logged in `17-Dogfooding-Sprint-1.md` §7; restated here for completeness of classification. |

---

## 3. Architecture Validation

Every architecture-level assumption exercised by this dogfooding sprint, per ADR-0026, ADR-0027, and `01-Architecture.md`/`03-Cross-Workspace-References.md`/`04-Knowledge-Graph.md`.

| # | Assumption | Verdict | Why |
|---|---|---|---|
| 1 | Workspace identity is durable and path-independent (ADR-0026: canonicalized origin remote, not path) | **Strengthened** | Validated with harder evidence than the original design review had: a real `git clone` from GitHub to a brand-new path resolved to the identical identity string as the original checkout. A local repo move, recovered via `remove-repo`+`add-repo`, also re-resolved to the same identity. This is the single strongest result of the sprint. |
| 2 | References are live/federated, never copied or re-indexed (ADR-0027) | **Validated** | Zero new files appeared in a referencing repo's directory after any federated query, across every real-repo trial. |
| 3 | Reference graphs must be a DAG; cycles are rejected at creation (ADR-0027 §5) | **Validated** (by prior automated tests — not re-exercised in this dogfooding pass) | No scenario in `17-Dogfooding-Sprint-1.md`'s journal tested cycle rejection live. This verdict rests on the implementation-phase test suite (`ReferenceGraphTests`, `WorkspacesAddReferenceCommandHandlerTests`), not on new dogfooding evidence. Flagged so it is not mistaken for freshly-validated. |
| 4 | An unavailable referenced workspace degrades that portion of a query rather than corrupting the whole result (ADR-0027, "Negative Consequences") | **Weakened** | Holds for one failure mode (a repo that was simply never indexed — confirmed, returns local-only results successfully) and is directly contradicted by another (a permission-denied repo crashes the entire query with an unhandled exception). The ADR's tradeoff is correct in principle; the implementation does not uniformly deliver it. |
| 5 | `IFederatedKnowledgeStore` is indistinguishable from a local query at the API/CLI surface (`01-Architecture.md` §3) | **Weakened**, narrowly | The technical claim (same interface shape, no new query API) is fully validated — the CLI surface genuinely does not change shape. But dogfooding surfaces an unstated corollary problem: because a federated and a degraded-federated result are *also* indistinguishable to the caller, a user cannot tell a complete answer from a silently partial one. The interface-shape claim holds; the transparency assumption riding on it does not. |
| 6 | Registry corruption fails closed and is never auto-repaired (ADR-0026) | **Validated** | Reproduced exactly as designed, with a clear, actionable error message naming the file and the fix. |
| 7 | Additive `schemaVersion` bump (1.0→1.1 for `references`) requires no migration mechanism (`02-Workspace-Model.md` §3) | **Validated** | No schema-related failures occurred anywhere in the sprint; every workspace round-tripped correctly through create/add-repo/add-reference/query. |
| 8 | Any directory with git metadata can serve as a member repo (ADR-0026 Identity Rules) | **Weakened** | The rule ("canonicalize origin from `.git/config`") is sound for a standard checkout and fails for a git worktree, where `.git` is a file, not a directory, pointing elsewhere. The identity *concept* is untouched; its current implementation's coverage of "what counts as a repo" is narrower than real git usage. |

**No assumption was Rejected.** No evidence from this sprint requires an architecture redesign.

---

## 4. Implementation Gap Analysis

Gaps to fix before expanding functionality, ranked by engineering risk (likelihood × impact of shipping Phase 2 on top of them, per the Playbook's risk-register convention).

| Rank | Gap | Risk | Why this rank |
|---|---|---|---|
| 1 | Federation fan-out has no defensive boundary around a per-source I/O exception | **Critical** | Deterministically reproducible with one `icacls` command; crashes the entire query, not just one source; directly contradicts a written acceptance criterion. Phase 2 adds caching and scope-narrowing on top of this same fan-out loop — every new layer multiplies the number of ways this crash can be triggered. |
| 2 | No observable signal when a reference fails to resolve | **High** | Not a crash, but a correctness-of-trust problem: a user can receive a genuinely incomplete answer with no way to know it's incomplete. Left unfixed, Phase 2's caching layer would cache these silent partial results indistinguishably from complete ones. |
| 3 | `RepoIdentityResolver` does not resolve git-worktree indirection | **Medium** | Narrow workflow (using a worktree as a federation member), not a correctness risk — it fails loudly and immediately at `add-repo`, never silently. Does not block the common case. |
| 4 | Moved-repo error message points to the wrong fix | **Medium-low** | A working repair path exists (`remove-repo`+`add-repo`); the defect is discoverability, not capability. |
| 5 | `workspaces show` omits `references` | **Low** | Cosmetic — underlying data and query behavior are both correct; only confirmation-by-inspection is missing. |
| 6 | `canonicalUri` double-wrap | **Low, out of scope** | Pre-existing, reproduced on content untouched by this epic — belongs to a different backlog entirely. |

---

## 5. Backlog Impact

Every reprioritization signal from `17-Dogfooding-Sprint-1.md` §5, resolved to exactly one disposition.

| Signal | Disposition | Justification |
|---|---|---|
| Harden the federation exception boundary | **New backlog item** (top of the stabilization sprint, §6) | A scoped implementation task with a precisely identified fix location (`Bm25SearchProvider` / `FederatedKnowledgeStore`'s fan-out). No architecture decision is required — the fix is narrowing what "expected environmental condition" means at that boundary, consistent with `SearchServiceStatus`'s own documented intent. |
| Surface reference-resolution failure as a visible diagnostic | **New backlog item** | Uses the existing `SearchServiceResult.Diagnostics` field — an extension point that already exists and is already threaded through the CLI's error-rendering path for other statuses. |
| Reorder WIP-021 (`remove-reference`) ahead of WIP-022 (pinning) within Phase 2 | **Replaces existing backlog priority** | Both items already exist on the Phase 2 backlog (`Backlog/backlog.md`); this is a resequencing based on the moved-repo repair story needing removal capability, not a new item. |
| `workspaces show` should list `references` | **New backlog item** (small) | A one-file formatter change with no dependencies; not worth deferring to Phase 2's broader API work. |
| Cross-source ranking normalization | **Not a backlog change now — record as an Engineering Observation** (§7) for Phase 3 planning | The work is already scoped under Phase 3 (WIP-033 Scope Classifier / WIP-034 Compressor), which has a hard dependency on Phase 2 per `15-Execution-Plan.md`'s dependency graph. Evidence elevates its importance; it cannot be pulled forward without breaking that dependency order, and nothing here justifies breaking it. |
| One corrupt manifest blocks `workspaces list` entirely | **Technical debt — Intentional** | Already a disclosed WIP-010 scope decision at implementation time, not a newly discovered defect. Register it formally as intentional technical debt with a future-epic target, rather than treating it as an open bug. |
| `canonicalUri` double-wrap | **Technical debt — Accidental**, tracked outside this backlog | Predates this epic and affects code this epic didn't touch; per the project's standing bug-tracking convention, this becomes a GitHub issue on the base search/indexing surface, not a Workspace Intelligence backlog item. |
| Git worktree identity resolution | **New backlog item**, explicitly low priority | Confirmed, reproducible gap with a narrow blast radius (one workflow, fails loudly, has zero silent-corruption risk). Does not belong in the stabilization sprint (§6); queue behind it. |
| Large-repo indexing coverage (28/2537) | **Not a backlog item on this epic** | Root cause was not investigated and the affected code (indexing/connector pipeline) predates and is outside Workspace Intelligence. Recommend a separate investigation ticket on the indexing epic's own backlog. |

**No finding from this sprint justifies a new ADR.** Every gap identified is closeable by implementation work within already-frozen architecture; none requires reopening ADR-0026 or ADR-0027.

---

## 6. Critical Path — Stabilization Sprint

The smallest sprint that closes the Critical and High implementation gaps from §4 before Phase 2 continues.

**Objectives**
1. A per-source failure in `FederatedKnowledgeStore`'s fan-out (any exception, not only specific `SqliteException` codes) degrades that source only — it never propagates to crash the whole query.
2. When a source is skipped (unreachable repo, unreachable reference, or a caught fan-out exception), that fact is recorded in `SearchServiceResult.Diagnostics` and surfaced by `ferret workspaces query`'s output — a human reading the result can tell it's partial.
3. `ferret workspaces show` lists `references`.
4. `Backlog/backlog.md` resequenced: WIP-021 ahead of WIP-022.

**Backlog items**
- (New) Harden `FederatedKnowledgeStore` fan-out exception boundary.
- (New) Wire skipped-source diagnostics into `SearchServiceResult`/`workspaces query` output.
- (New) `workspaces show` displays `references`.
- (Reorder only, no code) WIP-021 moved ahead of WIP-022 in `Backlog/backlog.md`.

**Expected deliverables**
- Updated `FederatedKnowledgeStore` (and, if the boundary is better placed there, `Bm25SearchProvider`) with an integration test that denies filesystem permission on a real index file (mirroring this sprint's `icacls` reproduction) and asserts the query still succeeds with the other source's results.
- A diagnostic-visibility test: a reference to an unreachable workspace produces output that names the unreachable workspace, not just fewer hits.
- An updated `TextWorkspacesShowFormatter` unit test asserting `references` appear in `show` output.
- `Backlog/backlog.md` diff reflecting the reorder, with a one-line rationale citing this document.

**Exit criteria**
- The exact failure reproduced in this sprint (ACL-denied index file → federated query) no longer throws; it returns a successful, partial, diagnostically-flagged result.
- `dotnet build`/`dotnet test` on `src/Ferret.sln` remains 0 warnings, 0 failures.
- No new architecture surface is introduced (no new interfaces beyond what already exists in `Ferret.Knowledge.Federation`).

**Definition of Done**
- All four objectives above have passing, real-failure-mode tests (not only status-code-driven unit tests) demonstrating them.
- `17`/`18`'s findings that map to this sprint are marked resolved in a follow-up dogfooding note (not written here — this document does not implement or close anything).
- Sponsor/Founder sign-off obtained before Phase 2 backlog items (WIP-020/022/023 full scope) resume.

---

## 7. Engineering Observations

Per the Playbook's definition (§7): findings about *frozen* architecture that don't require reopening it, recorded so they aren't re-derived from scratch later.

- **ADR-0027's degradation tradeoff is correct but underspecified on observability.** The ADR accepts that an unreachable reference degrades a query. It does not say the caller must be told. `SearchServiceResult.Diagnostics` already exists as the right extension point — this observation exists so the stabilization sprint doesn't mistake "add a diagnostic" for "amend ADR-0027." It doesn't need amending; it needs a caller that uses what it already allows.
- **ADR-0026's identity rule assumed `.git` is always a directory.** True for a standard checkout, false for a git worktree. The identity *concept* (canonicalize the origin remote) needs no change; `RepoIdentityResolver`'s file-existence check does.
- **Testing blind spot:** every test written during implementation validated `SearchServiceResult.IsSuccess` and `Hits.Count` — never a real I/O denial, and never "would a human reading this output understand what happened." Both of this sprint's highest-severity findings passed the entire automated test suite (275 tests) undetected for exactly this reason. Future milestone test strategies should explicitly include at least one real-failure-injection test per external-boundary interface (filesystem, network, subprocess), not only status-code-driven unit tests.
- **Assumption that proved correct, more strongly than expected:** identity-based, path-independent workspace membership. The design review anticipated this in principle; dogfooding proved it against a real network clone, which is stronger evidence than any test in the implementation-phase suite provided.
- **Assumption that proved correct:** fail-closed registry corruption handling, including the quality of the resulting error message — this is the kind of thing that's easy to under-invest in and this sprint found it already done well.
- **Process lesson (restated from `17-Dogfooding-Sprint-1.md` §7):** a destructive shell command derived its target path from an unvalidated variable against shared registry state, deleting a pre-existing user workspace. The harness's own safety classifier caught two further attempts of the same class later in the same session — that the *tooling* had to be the backstop, twice, is itself the lesson: verify-then-delete discipline was not reliably self-applied under time pressure.

---

## 8. Playbook Improvements

Recommended only where this sprint's evidence backs the change.

- **Vertical slice validation should include failure-injection as part of its own Definition of Done, not defer it to a separate dogfooding pass.** `16-Vertical-Slice-Validation.md` declared the slice ready for Phase 2 using unit, integration, and live-CLI tests — all of which exercised the happy path and the "repo simply isn't indexed" failure mode, none of which denied a permission or corrupted a manifest under real conditions. Both of this sprint's highest-severity findings would have been caught one sprint earlier if failure-injection (permission denial, moved paths, corrupted registry entries) were a mandatory section of a vertical slice's own validation ledger, not a follow-up activity.
- **The two-stage process (engineering self-validation, then Founder-directed adversarial dogfooding) should be kept as-is** — this sprint is evidence it works, not evidence it's redundant. The first stage's "Complete" verdict was necessary (it proved the architecture) but not sufficient (it didn't find the reliability gap); the second stage is what found it. Collapsing the two stages would have shipped the crash bug into Phase 2 undetected.
- **No change recommended to dogfooding cadence, backlog-reprioritization format, or CI/merge standards** — this sprint surfaced no evidence bearing on any of them.

---

## 9. Execution Recommendation

**Run one stabilization sprint.**

Not *continue immediately to Phase 2*: a reproducible crash under a common, real failure condition (permission denial) directly contradicts a written acceptance criterion; Phase 2 adds a caching layer and a scope classifier on top of the exact fan-out loop where that crash lives, which multiplies its blast radius before it is fixed once at the source.

Not *pause for architectural redesign*: §3 shows every structural claim — zero duplication, live federation, path-independent identity, DAG enforcement, additive schema versioning, fail-closed corruption handling — held under real, adversarial, repository-backed testing. Nothing in this sprint's evidence requires reopening ADR-0026 or ADR-0027, and the rules governing this analysis explicitly prohibit doing so without evidence that isn't present here.

---

## 10. Implementation Readiness

**Yes — Engineering is ready to begin the stabilization sprint immediately.** Every fix location named in §4/§6 is precisely identified against existing code (`FederatedKnowledgeStore`, `Bm25SearchProvider`, `SearchServiceResult.Diagnostics`, `TextWorkspacesShowFormatter`, `Backlog/backlog.md`); none requires a design discussion or an open decision.

**Implementation order:**
1. Harden the fan-out exception boundary (§6, objective 1) — the actual crash-risk fix; blocks nothing else and should land first.
2. Wire skipped-source diagnostics through `SearchServiceResult`/`workspaces query` output (§6, objective 2) — naturally sequenced after #1, since the exception-handling path decided there is what produces the "this source was skipped" signal to surface.
3. `workspaces show` displays `references` (§6, objective 3) — independent, can proceed in parallel with #1/#2.
4. Resequence `Backlog/backlog.md` (§6, objective 4) — a documentation change, no code dependency, can happen at any point.
5. Outside this sprint, file separate tracking (per the project's standing bug-tracking convention) for: git-worktree identity resolution, the `canonicalUri` double-wrap, and the large-repo indexing-coverage question — each belongs to a different backlog than this one and should not block it.
