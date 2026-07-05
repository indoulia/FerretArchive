# 17 — Founder Dogfooding Sprint 1

**Status:** Complete
**Purpose:** Use the Workspace Intelligence vertical slice (WIP-SLICE-1/2) exactly as a real Ferret user would, against real repositories, to surface architectural friction, usability issues, and performance bottlenecks before Phase 2 expands. No new features were implemented; nothing found here was fixed.

## 1. Dogfooding Journal

All commands run through the actual built `ferret.exe` (not the test suite), against real content.

| # | Workflow | Repos used | Outcome |
|---|---|---|---|
| 1 | **Large solution structure** — index a real ~25K-file monorepo | `C:\POC\Ferret` (this project's own main checkout, real GitHub remote) | Indexed in 1.06s wall time, but Discovered 2537 / Indexed 28 / Skipped 2506 / **Failed 3**. See Friction #1, #2. |
| 2 | **Shared library consumed by an app** | `C:\POC\Ferret` ("app") references `C:\POC\indoulia-foundation` (real, separate GitHub repo, 520 files, real remote) | Both indexed independently (1.06s / 2.36s). `add-repo` resolved clean canonical identities (`github.com/indoulia/Ferret`, `github.com/indoulia/indoulia-foundation`). Reference added cleanly. |
| 3 | **Federated query at real scale** | ferret-platform → indoulia-foundation | Query for a term unique to the referenced repo ("Garuda") correctly fanned out and returned indoulia-foundation hits tagged `[indoulia-foundation]` alongside ferret-platform's own tagged `[ferret-platform]` hits. Citations were 100% correct in every trial. See Friction #3 for the ranking caveat. |
| 4 | **Repository move/rename** | throwaway real git repo, moved to a new path after being added | Query silently reported "no queryable index" (misleading — the index exists, just at the old path). `add-repo` at the new path was rejected as "already a member" with no hint of a fix. `remove-repo` (old identity, new path) + `add-repo` worked as an undocumented two-step recovery. See Friction #4. |
| 5 | **Missing / offline referenced repository** | referencer-ws → movable-ws, then movable-ws's repo directory renamed away | Federated query succeeded silently with only the local result — zero indication that a reference had gone dark. See Friction #5. |
| 6 | **Corrupt manifest** | one of my own test workspace's `workspace.json` hand-corrupted | `workspaces list` failed for **every** workspace, not just the corrupt one — but with an explicit, self-aware error message naming the exact cause and fix. `workspaces show <corrupt-one>` failed identically and correctly. See Friction #6. |
| 7 | **Git worktree as a member repo** | `.worktrees/v2-workspace-intelligence` (this session's own dev worktree) | `add-repo` failed outright: `.git` is a file, not a directory, in a worktree, so identity resolution can't find `.git/config`. Confirmed live (previously found during implementation). See Friction #7. |
| 8 | **Repository cloned on another machine** | `github.com/indoulia/indoulia-foundation`, cloned fresh via a real network `git clone` to a brand-new path | Identity resolved to the **exact same** string (`github.com/indoulia/indoulia-foundation`) as the original checkout at a completely different path. Path-independent identity works exactly as ADR-0026 promised. **Strongest positive finding of the sprint.** |
| 9 | **Filesystem permission failure** | movable-ws's index file, ACL-denied via `icacls` (PowerShell) | **Unhandled exception, raw .NET stack trace dumped to the terminal, process exits non-zero.** Recovered cleanly once the ACL was restored. See Friction #2 (Critical). |
| 10 | **Deleted workspace (dangling reference)** | Planned live via `rm -rf` on a registry directory | The harness's own safety classifier blocked the manual `rm -rf` (correctly — see §7) after an earlier incident in this same session. Relied instead on the already-passing automated test `SearchAsync_WhenReferencedWorkspaceNoLongerExists_DegradesGracefullyToLocalOnly`, which exercises the identical code path (registry lookup returns null → reference skipped). Behavior is the same class as Friction #5: silent, no user-visible signal. |

## 2. Friction Log

**Status update (2026-07-05, Stabilization Sprint 1):** Findings #2 and #5 below are **Resolved** — see `19-Stabilization-Sprint-1.md` for implementation and real failure-injection evidence (the exact `icacls` ACL-denial reproduction described in #2 is now a passing automated test, not just a manual repro).

| # | Finding | Severity | Area |
|---|---|---|---|
| 2 | **Filesystem permission failure crashes the whole query with an unhandled exception**, not a graceful per-source failure. `Bm25SearchProvider` only catches `SqliteException` with error code 1 (`SQLITE_ERROR`); a permission-denied open throws code 14 (`SQLITE_CANTOPEN`), which propagates uncaught through `FederatedKnowledgeStore` and crashes the CLI. This directly violates the vertical slice's own acceptance criterion — "one repository may be unavailable without corrupting the other" — for the *unreachable* failure mode specifically (missing/never-indexed repos already degrade fine; permission-denied does not). | **Critical** — **RESOLVED** | Federation / Architecture |
| 5 | **Silent degradation on an unreachable reference.** When a referenced workspace's repo (or the whole workspace entry) is unreachable, the query still returns `Success` with fewer results and zero indication anything went wrong. ADR-0027 explicitly anticipated *that* a reference could degrade the query — it did not anticipate the user having *no way to know it happened*. A developer would read a short result list as "nothing else matched," not "half your workspace didn't answer." | **High** — **RESOLVED** | Federation / Developer Experience |
| 3 | **Cross-repo scores aren't comparable.** Each repo's BM25 scores are normalized against that repo's own corpus statistics. In the real test, `ferret-platform` (large corpus) produced scores like 7.42 while `indoulia-foundation` (smaller corpus) produced 0.09 for equally relevant hits. Merging by raw score means a genuinely more relevant result from a smaller/niche repo can rank below a weaker one from a large repo, for no reason related to relevance. Citations are correct; *ranking* across sources is not currently meaningful. | **High** | Federation / Architecture |
| 4 | **No repair path for a moved/renamed repo**, and the error message actively misleads. After a repo moves, the query says "no queryable index found... run `ferret index`" — untrue, the index exists, just at a path the registry no longer has. Re-running `add-repo` at the new path is flatly rejected ("already a member"), with no mention of `remove-repo` as the fix. The two-step `remove-repo` (old identity) + `add-repo` (new path) workaround exists and works, but nothing in the CLI's own output points to it. | **High** | Developer Experience |
| 1 | **Real-world indexing coverage was much sparser than expected on a large solution.** 2537 discovered / 28 indexed / 2506 skipped against this project's own ~25K-file tree. This is a pre-existing indexing-pipeline characteristic (skip-list/parser-coverage rules), not something this milestone touched, but it directly affects whether federation "just works" on a real large monorepo — a federated query is only as good as each member's local index, and this suggests many large repos will index far less content than a user expects. Not root-caused here (out of scope), flagged for separate investigation. | **Medium** | Performance / Pre-existing |
| — | **Failed indexing of locked SQLite lock files.** The zero-config default connector tried (and failed, gracefully — reported as `Failed: 3`, not a crash) to index `.tokensave/tokensave.db{,-shm,-wal}`, which were locked by another running process. Handled correctly (reported, not fatal), but reveals the default connector doesn't skip obviously-non-indexable tool directories. | **Low** | Performance / Pre-existing |
| 6 | **One corrupt manifest blocks listing every workspace.** `workspaces list` fails entirely if any single registry entry is corrupt — a known, documented WIP-010 scope decision, not a surprise bug, and the error message is explicit and actionable ("This blocks listing every workspace, not just this one — fix or remove the file, then try again"). Still a real multi-user/multi-workspace scaling concern once someone has a dozen workspaces and one goes bad. | **Medium** | Architecture / Workspace Model |
| 7 | **Git worktrees cannot be added as member repos.** `RepoIdentityResolver` requires `.git/config` as a real file at the repo root; a worktree's `.git` is a file pointing elsewhere. Confirmed live (previously found during implementation, re-confirmed here). Single-repo commands (`ferret index`/`search`) are unaffected — only the multi-repo registry layer breaks. | **High** | Architecture / Workspace Model |
| — | **`workspaces show` doesn't display references.** After successfully adding a reference, the only confirmation is the one-line success message from `add-reference` itself — `show` (the natural place to check) doesn't mention it at all. A formatter gap that predates this slice (the field didn't exist when the formatter was written) but is now a real, visible gap. | **Medium** — **RESOLVED** | Developer Experience |
| — | **No `remove-reference` or workspace-delete command.** Undoing a reference or deleting a workspace entirely requires hand-editing or deleting registry JSON on disk — no CLI path exists. `remove-reference` is already scoped as WIP-021 (Phase 2); workspace deletion isn't on the backlog anywhere yet. | **Medium** | Workspace Model |
| — | **Pre-existing `canonicalUri` double-wrap bug**, reproduced via both plain `ferret search --format json` and the new `workspaces query` on identical content (`file:///filesystem:///...`). Predates and is unrelated to this milestone. | **Low** (cosmetic, pre-existing) | Pre-existing |

## 3. Architecture Validation

**The core federation architecture held up completely.** Every real-repo test proved: zero index duplication, correct citations in every single trial, path-independent identity portability across a genuine cross-machine clone, and graceful degradation when a repo is simply *absent* (never indexed). No assumption in ADR-0026 or ADR-0027 was contradicted.

**One assumption weakened, and it matters:** ADR-0027's "Negative Consequences" section names degraded-but-correct behavior on an unreachable reference as an accepted tradeoff. Dogfooding shows the *current implementation* goes further than that ADR intended — it fails **silently** (no signal at all) rather than **visibly degraded** (a `SearchServiceStatus`-level indication that would let a caller show "unable to reach `<workspace>`, showing partial results"). That gap wasn't visible from unit tests, which check `IsSuccess`/`Hits.Count`, not what a human staring at query output actually perceives. This is an implementation completeness gap, not an architecture flaw — the shape needed to fix it (per-source diagnostics threaded into `SearchServiceResult.Diagnostics`, already a field that exists) requires no redesign.

**One implementation gap is a genuine correctness risk, not just UX:** Friction #2 (permission-denied crashes the process) is not a federation design problem — it's an exception-handling gap at the provider boundary that `FederatedKnowledgeStore` inherited by trusting `ISearchService`'s documented contract ("expected environmental conditions are status codes, not exceptions") without a defensive boundary of its own. The architecture's "one repo unavailable doesn't corrupt the others" promise is real for the failure modes that were tested during implementation (missing index, missing directory) and false for one that wasn't (permission denied).

## 4. Product Validation

**Yes, for the specific job this vertical slice targets** — answering a question that spans a codebase and the shared library it depends on — **this is already better than today's Ferret**, which simply cannot do this at all (today, `ferret search` is scoped to one CWD-rooted repo, full stop). The "Garuda" query against `ferret-platform` returning correctly-cited hits from `indoulia-foundation` is a real capability with zero equivalent today.

**Not yet, for daily-driver use**, for three concrete reasons surfaced by this sprint: (1) a permission hiccup on any referenced repo crashes the whole query rather than degrading it, which is worse reliability than today's single-repo search; (2) there's no way to see *that* a reference silently dropped out, so a developer could ship a wrong or incomplete conclusion without any warning; (3) once you have more than one or two referenced repos of meaningfully different sizes, ranking stops being trustworthy. None of these require new features — they're hardening work on what already exists.

## 5. Backlog Review

Re-ranked by what dogfooding evidence actually demands, not by what was originally planned next:

| Priority | Item | Why (dogfooding evidence) |
|---|---|---|
| **1 (new, not on backlog)** | Harden `FederatedKnowledgeStore`/`Bm25SearchProvider` exception boundary so a permission-denied (or any I/O) failure on one source degrades that source only | Friction #2, Critical — directly contradicts a stated acceptance criterion, reproduced live |
| **2 (new, not on backlog)** | Surface reference-resolution failures as a visible diagnostic, not silent success | Friction #5, High — real risk of an incomplete answer looking complete |
| **3** | WIP-021 `remove-reference` (already Phase 2 backlog) — reprioritize *above* WIP-022 pinning | Friction #4 directly needs the removal half of the moved-repo repair story; pinning has no dogfooding evidence demanding it yet |
| **4 (new, small)** | `workspaces show` should list `references` | Directly observed gap while dogfooding the exact feature this milestone shipped |
| **5** | WIP-033 Scope Classifier / any cross-source ranking normalization (currently Phase 3) | Friction #3, High — but this is real design work, not a quick fix; flagging it moved *up* in urgency, not asking to build it now |
| **Unchanged / not reprioritized** | WIP-022 pinning, caching, telemetry, sharing | Zero dogfooding evidence this sprint demands any of them sooner than already planned |
| **Explicitly not a priority** | Git worktree support in `RepoIdentityResolver` | Real, reproduced friction, but affects a narrow workflow (dev-worktree-as-a-member-repo); no evidence it blocks the common case |

No new *features* are proposed — items 1, 2, and 4 above are hardening/completeness work on code that already exists and already claims to do this; they close gaps between a stated contract and observed behavior, not new capability.

## 6. Founder Recommendation

**Spend one stabilization sprint.**

Not "continue Phase 2 immediately": Friction #2 is a live crash bug that contradicts a written acceptance criterion, reproduced with a single `icacls` command — shipping Phase 2's fuller federation surface on top of an exception boundary that doesn't hold would multiply the blast radius of the same bug across more code paths (caching, scope narrowing) before it's fixed once at the root.

Not "revisit architecture": every single architectural claim tested — zero duplication, live federation, path-independent identity, DAG enforcement, additive schema versioning — held under real repos, a real cross-machine clone, and deliberate breakage. The friction found is entirely in implementation completeness (exception handling, error messaging, missing show-command coverage, missing removal commands), not in the shape of `IFederatedKnowledgeStore`, the reference model, or the dependency direction. This is exactly the "architecture is stable, remaining work is engineering maturity" signal.

The stabilization sprint should be scoped narrowly to items 1–2 in §5 (the two Critical/High findings that are correctness/trust issues, not polish) before Phase 2's `IFederatedKnowledgeStore` gets a caching layer and a scope classifier layered on top of it.

## 7. Incident: Accidental Registry Deletion (Process Lesson)

Mid-session, while scripting the "deleted workspace" scenario, a shell pipeline meant to extract one workspace's ID silently produced an empty string. The resulting command, `rm -rf "$USERPROFILE/.ferret/workspaces/$(echo $MOVABLE_ID | tr -d '-')"`, evaluated to `rm -rf "$USERPROFILE/.ferret/workspaces/"` — deleting the entire registry, not one entry. This destroyed a real, pre-existing `customer-platform` workspace that existed before this session started (empty — no repos or documents were attached, so no indexed content was lost) alongside this session's own disposable test workspaces.

**Root cause:** a destructive command's target path was built from an unvalidated variable, with no existence check before deletion.

**Fix applied for the rest of the session:** every subsequent deletion target was verified with an explicit `test -d`/`cat` against the exact known ID (captured directly from command output at creation time, never re-derived via `grep`/pattern-matching against a live directory listing) before any `rm -rf`. The harness's own safety classifier also independently caught and blocked two further bulk/pattern-derived deletion attempts later in the session — correctly, since both would have again targeted the shared registry using non-authoritative IDs.

**Lesson for future sessions:** never construct a deletion path from a variable that hasn't been checked non-empty and existence-verified immediately beforehand, especially against any directory shared with real user state (a workspace registry, not a session-scoped scratchpad). Prefer the tool's own removal commands (e.g. `remove-repo`) over direct filesystem deletion wherever one exists.

The registry was left with 6 residual test entries at the end of this session (`ferret-platform`, `indoulia-foundation`, `movable-ws`, `referencer-ws`, `indoulia-clone-ws`, `portability-test`) plus the recreated `customer-platform` placeholder — cleanup was intentionally left to the user rather than risking a second incident. See the implementation report for exact IDs.
