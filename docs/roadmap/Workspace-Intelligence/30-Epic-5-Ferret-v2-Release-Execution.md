# 30 — Epic 5: Ferret v2 Release Execution

**Status:** Ready for execution — no design work remains; this Epic converts `29-Ferret-v2-Release-Master-Plan.md`
into implementation stories with acceptance criteria, dependencies, and gates.
**Governing document:** `29-Ferret-v2-Release-Master-Plan.md`. This Epic does not amend, re-derive, or
reopen anything in `00`–`29` — it is `29`'s content reshaped into stories so engineering can execute
directly from it without producing another planning document.
**Scope discipline:** every story below traces to a task already named in `29` §2, which itself traces
only to `00`–`28`, the four ADRs, `Backlog/backlog.md`, and `Future/Deferred-Scope.md`. No story,
risk, gate, or deferral in this document is new. Where this Epic adds anything beyond `29`, it is
narrowly the mechanical Story/DoD/effort structure the Founder requested — never new scope.

---

## 1. Executive Summary

**Current repository state (re-verified live, this session, against `29`'s claims — all still hold):**

- Current branch `feature/wip-030-031-federated-query-cache` carries commit `d3f7335`
  ("perf(federation): eliminate floating-reference cache key regression") — still unpushed, still on
  zero remote branches, exactly as `29` describes.
- `docs/roadmap/Workspace-Intelligence/README.md` is modified but uncommitted (8 lines — the ADR-0026/
  ADR-0028 decision-table update).
- `24`, `25`, `27`, `28`, `29` (this Epic's own governing document), and
  `docs/archive/superpowers/plans/2026-07-05-wip-032-registry-read-through-cache.md` remain untracked working-tree
  files.
- ADR-0026 status field still reads "Proposed — finalized for Founder approval." ADR-0029 still reads
  "Proposed — requires Founder decision." Neither has moved since `29` was written.

**Implementation status:** Phases 0–2 are done and on `main` (PR #29, PR #31; 1,466 tests passing per
`19`). Phase 3 is partially on `main`: P3-001 and WIP-032 merged (PR #32); WIP-030/031 and the P3-002
regression fix are still stranded on a feature branch, merged into the wrong base (PR #33's
`baseRefName` is `feature/wip-032-registry-read-through-cache`, not `main`). WIP-033 is discovery-only
(Python-simulated, zero real C# implementation, zero connection-pooling infrastructure in the repo to
build on). Phases 4–5 are correctly un-started and out of v2.0 scope.

**Remaining work:** fourteen tasks (`29` §2, `T1`–`T14` below), of which four are release-blocking
(`T1`, `T10`, `T12`, `T13`), five are parallelizable engineering (`T2`–`T5`, `T9`), one is a conditional
high-risk implementation gate (`T6`, which conditionally gates `T7`/`T8`), and two are governance/
documentation follow-ons (`T11`, `T14`).

**Release objective:** tag `v2.0.0` once Gates A–G and I (§6) pass and Gate H's dogfooding pass shows
zero new Critical/High findings — per `29` §9's Go/No-Go criteria, unchanged here.

---

## 2. Epic Goal

**Business objective.** Ship the Founder's "Ferret v2.0" — the Workspace Intelligence Platform milestone
— by correctly landing already-built, already-dogfooded work that is currently either stranded off
`main` or blocked on a pure governance decision, not by building new capability.

**Engineering objective.** Close every item in `29` §2 (`T1`–`T14`) in dependency order, keep `main`
green (0 regressions against the 1,466-test baseline) at every merge, and resolve `T6`'s WIP-033
real-value gate in either direction — merged-and-validated or explicitly deferred — before cutting the
release candidate.

**Success criteria.** Identical to `29` §9's Go criteria: Gates A–G and I pass, plus Gate H's dogfooding
pass returns zero new Critical/High findings. No new success criterion is introduced by this Epic.

---

## 3. Epic Scope

Reproduced exactly from `29` §8 — no addition, no reprioritization.

### Included (v2.0 — must ship)

- `T1`: WIP-030/031 + P3-002 fix landed on `main`
- `T2`: WIP-013 (auto-migration wrapper)
- `T3`: WIP-014 (MCP `workspace_list`)
- `T4`: WIP-040, rescoped (structured `ILogger` events, not a `Meter`/OpenTelemetry buildout)
- `T5`: WIP-036 (cross-repo ranking normalization)
- `T9`: WIP-037 (`workspace remove`/bulk cleanup)
- `T10`: ADR-0026 Founder decision, recorded
- `T12`/`T13`/`T14`: documentation reconciliation
- `T6` (WIP-033): included **only if** its real-C# prototype validates at R=26 scale before the tag;
  otherwise moves to v2.1 per Gate E's explicit either/or

### Not Included (out of v2.0 scope, no v2.0 dependency on them)

- Phase 4 (WIP-041–044: Usage Ledger, retention default, analytics rollups, dashboard CLI) — deferred
  pending real multi-user usage evidence that does not exist today
- Phase 5 (WIP-050/051/052: Sharing/RBAC enforcement) — deferred pending ADR-0029 decision **and** real
  multi-user/org demand
- ADR-0029 closure — optional for v2.0 (`T11`); blocks only Phase 5

### Deferred to v2.1

- WIP-033 (Scope Classifier), if not already shipped in v2.0
- WIP-034 (Compressor) and WIP-035 (Context assembly cache), once WIP-033 has merged and shown real
  (non-simulated) value
- Cross-reference conflict resolution (automatic) — `03` §4, explicitly "not resolved automatically in
  v1," listed in `Future/Deferred-Scope.md`
- ADR-0029 Founder decision, if not already made in v2.0
- WIP-041–044, only if real multi-user usage evidence has materialized by then; otherwise this bundle
  moves to v3 unchanged

### Deferred to v3

All items already named in `Future/Deferred-Scope.md`, unchanged:

- Full five-role sharing model (adds AI Agent role) + invitation flows beyond direct user-ID grants +
  audit history — WIP-050/051/052 and successors, gated on ADR-0029 **and** real multi-user/org demand
- Organization-level / cross-org sharing
- Cost/billing infrastructure (dollar-based reporting), gated on FUTURE-002 Q2
- Ferret Hub / cloud sync
- Enterprise scale beyond current targets (100K repos/workspaces)
- Semantic/vector retrieval layer for the Context Optimization Engine, deferred to FUTURE-002 §22

---

## 4. Stories

Every story below is `29` §2's corresponding task (`T1`–`T14`), unchanged in substance. Effort is
restated on the required XS/S/M/L scale; where `29` used a different notation (e.g. "S–M," "High,"
"Unassessed"), the mapping is noted.

### T1 — Ship WIP-030/031 + P3-002 fix to `main`

- **Objective:** Land the federated query cache and its regression fix on `main` via a correctly-based
  PR, since PR #33 merged into a stale feature branch instead of `main`.
- **Repository evidence:** `29` §1, §2; `28` §2 (`gh pr view 33` → `baseRefName:
  "feature/wip-032-registry-read-through-cache"`; `git branch -r --contains d3f7335` → empty); commit
  `d3f7335` confirmed present on the local branch, zero remote branches, this session.
- **Dependencies:** None. Code, tests, and dogfooding are already complete (`23`, `26`).
- **Deliverables:** `d3f7335` pushed; new PR `feature/wip-030-031-federated-query-cache` → `main`
  opened and merged (bypassing the stale intermediate branch).
- **Acceptance criteria:** `git merge-tree` shows zero conflicts before merge (already confirmed, `28`
  §2); `git merge-base --is-ancestor d3f7335 origin/main` exits 0 after merge.
- **Definition of Done:** Merged to `main`; `dotnet test src/Ferret.sln --configuration Release` passes
  with 0 failures, 0 regressions against the 1,466-test baseline.
- **Estimated effort:** S.

### T2 — WIP-013: auto-migration wrapper

- **Objective:** Zero-action wrapping of every existing `.ai/workspace.json` into a `kind: "personal"`
  registry entry.
- **Repository evidence:** `14-Migration.md`; `Backlog/backlog.md` WIP-013, unchecked, "quick win: ships
  with WIP-010–012, no separate release."
- **Dependencies:** WIP-010, WIP-011 (both done).
- **Deliverables:** Migration wrapper invoked transparently on any `Ferret workspace` command against an
  un-migrated checkout.
- **Acceptance criteria:** Existing single-repo integration test suite passes unmodified
  (`14-Migration.md` §2's invariant); failure path falls back to no-registry behavior per §3, never
  blocks the underlying command.
- **Definition of Done:** Merged to `main`; dogfooded against an already-migrated dogfooding-branch
  checkout with identical behavior/output to the pre-migration baseline; backlog checkbox checked (feeds
  `T12`).
- **Estimated effort:** S.

### T3 — WIP-014: MCP `workspace_list` tool

- **Objective:** MCP parity for WIP-012's CLI `list` command.
- **Repository evidence:** `12-API.md` §3; `Backlog/backlog.md` WIP-014, unchecked.
- **Dependencies:** WIP-012 (done).
- **Deliverables:** MCP tool exposing workspace membership enumeration.
- **Acceptance criteria:** Tool output matches CLI `list` output for the same workspace.
- **Definition of Done:** Merged to `main`; exercised via the existing MCP client used for other
  knowledge tools (no new client needed); backlog checkbox checked.
- **Estimated effort:** S.

### T4 — WIP-040 (rescoped): structured `ILogger` events on the federation/cache path

- **Objective:** Emit cache hit/miss, per-query duration, and per-source skip events via plain
  structured logging — not a `Meter`/OpenTelemetry pipeline — on `FederatedKnowledgeStore` and
  `CachingFederatedKnowledgeStore`.
- **Repository evidence:** `28` §2 confirms `Ferret.Telemetry` is an empty stub
  (`internal static class TelemetryModule {}`), referenced by zero projects, with zero
  `Meter`/`ActivitySource`/`Counter`/`ILogger` usage anywhere in `src/`, including zero `ILogger` calls
  in `FederatedKnowledgeStore.RunAsync`/`ResolveSourcesAsync`/`Merge` or
  `CachingFederatedKnowledgeStore.SearchAsync`. `28` §3.2 rescoped this task from a metrics-pipeline
  buildout to structured logging because no consumer exists for real metrics yet.
- **Dependencies:** None blocking. Softly de-risks `T5`/`T6` (a human can `grep`/tail these events to
  catch a repeat of `26`'s regression faster) — not a data dependency (`28` §6).
- **Deliverables:** Structured log events for cache hit/miss, per-query duration, per-source skip on the
  federation/cache path.
- **Acceptance criteria:** Events are observable via standard logging inspection (`grep`/tail) on a live
  federated query; no new metrics library or DI wiring introduced.
- **Definition of Done:** Merged to `main`; `dotnet test` green; backlog entry for WIP-040 updated to
  reflect the rescoped (logging-only) delivery.
- **Estimated effort:** M — mapped up from `29`'s "S–M" because no existing logging pattern exists on
  this path to extend (`28` §2, §3.2).

### T5 — WIP-036 (new, per `27`, unchanged by `28`): cross-repo BM25 ranking normalization

- **Objective:** Fix `FederatedKnowledgeStore.Merge`'s flat, uncalibrated cross-source ranking, which
  favors large corpora over more-relevant small ones.
- **Repository evidence:** Confirmed live at `FederatedKnowledgeStore.cs:93-97`
  (`.OrderByDescending(hit => hit.Score)` over raw per-source BM25 scores;
  `Bm25SearchProvider.cs:133`, `Score = (float)-rank`) — a live quality defect observed at `dogfood-hub`
  R=26 scale (`18`, `20`, `27`, re-confirmed by `28` §2, §3.3).
- **Dependencies:** None.
- **Deliverables:** A chosen normalization strategy (z-score, min-max, or rank-based — none pre-selected
  by any prior document) applied in `FederatedKnowledgeStore.Merge`.
- **Acceptance criteria:** Regression-tested against `FederatedKnowledgeStore`'s existing `Merge` test
  suite; no change in behavior for single-source queries.
- **Definition of Done:** Merged to `main`; `dotnet test` green; a documented rationale for the chosen
  normalization strategy (this is the one open design micro-decision `28` §7 flags — "unexplored in any
  doc").
- **Estimated effort:** M.

### T6 — WIP-033: pooled-connection Scope Classifier, real-C#-validated

- **Objective:** Build and validate, in real C# against `dogfood-hub` at R=26 scale, the pooled-connection
  Scope Classifier design that `25` validated only via Python simulation.
- **Repository evidence:** `24` proved the naive (non-pooled) shape is not a net win at scale. `25`
  showed a pooled shape is, but only in simulation. `28` §2 confirms zero `ScopeClassifier`/`fts5vocab`
  code exists yet, and confirms the repo has **zero connection-pooling infrastructure anywhere**
  (`SqliteConnectionPool`/`PooledConnection`/`ConnectionPool` — zero matches repo-wide); `Bm25SearchProvider.cs:77-80`
  still opens a fresh `SqliteConnection` per call, so there is nothing to extend from.
- **Dependencies:** Ideally after `T4` (soft — a human notices problems faster with logging in place;
  not a hard block, per `28` §6). Hard prerequisite for `T7`/`T8`.
- **Deliverables:** A throwaway/TDD real-C# pooled prototype; a re-run of `25`'s R=26 `dogfood-hub`
  benchmark against it.
- **Acceptance criteria:** Either (a) the prototype confirms a real (non-simulated) win at R=26 scale and
  merges, or (b) it does not, and WIP-033 (and therefore WIP-034/035) is explicitly marked deferred to
  v2.1 in `Backlog/backlog.md` — not left in an ambiguous "still investigating" state. Either outcome
  satisfies Gate E.
- **Definition of Done:** Gate E resolved in either direction, recorded in `Backlog/backlog.md`.
- **Estimated effort:** L — `29` rates this the plan's only **High** implementation-risk item, because
  connection-pooling infrastructure must be built from scratch with no precedent anywhere in the repo
  (`28` §3.4, §7).

### T7 — WIP-034: Compressor (conditional, v2.1 candidate unless T6 lands early and cleanly)

- **Objective:** Post-Scorer compression of federated results, consuming WIP-033's scope-classified
  output.
- **Repository evidence:** `20` §1 calls this "low standalone value" without WIP-033; `28` §7: "no design
  work has started."
- **Dependencies:** Hard: `T6` merged **and** its real (non-simulated) value confirmed.
- **Deliverables:** Design and implementation of the Compressor, scoped only after `T6` resolves.
- **Acceptance criteria:** Not defined until `T6`'s outcome is known — no design work exists to derive
  criteria from yet (`28` §7).
- **Definition of Done:** Included in v2.0 only if `T6` lands early and cleanly within the release
  window; otherwise moves to v2.1 per `29` §8, with that disposition recorded in `Backlog/backlog.md`.
- **Estimated effort:** L (provisional — `29` marks this "Unassessed"; sized to the L bucket pending
  `T6`'s outcome, since it is gated on new-from-scratch design work).

### T8 — WIP-035: Context assembly cache (conditional, same gate as T7)

- **Objective:** Cache context assembly keyed on WIP-033's scope-classified workspace set.
- **Repository evidence:** `20` §1 — cache key depends on WIP-033's output.
- **Dependencies:** Same hard dependency as `T7`: `T6` merged and validated.
- **Deliverables:** Design and implementation of the context assembly cache, scoped only after `T6`
  resolves.
- **Acceptance criteria:** Not defined until `T6`'s outcome is known.
- **Definition of Done:** Same disposition rule as `T7` — v2.0 only if `T6` lands early and cleanly;
  otherwise v2.1.
- **Estimated effort:** L (provisional, same basis as `T7`).

### T9 — WIP-037: `workspace remove`/bulk-cleanup command

- **Objective:** Give developers a way to remove throwaway workspace registry entries.
- **Repository evidence:** `28` §2 confirms `WorkspacesCliModule.cs:26-100` registers
  `create/list/show/add-repo/remove-repo/add-reference/remove-reference/pin-reference/unpin-reference/query`
  — no `remove-workspace`/`delete`/`prune`. Real, repeated dogfooding friction: 25+ throwaway workspace
  entries left permanently registered with no removal path (`22`, `23`, `25`).
- **Dependencies:** None; mirrors the existing `remove-repo`/`remove-reference` command shape.
- **Deliverables:** A `workspace remove` (or equivalent bulk-cleanup) CLI command.
- **Acceptance criteria:** Mirrors `remove-repo`/`remove-reference`'s existing test and UX pattern; does
  not preempt `T5`/`T6` (opportunistic, not blocking).
- **Definition of Done:** Merged to `main`; `dotnet test` green; backlog checkbox checked.
- **Estimated effort:** S.

### T10 — Close ADR-0026 (Founder decision)

- **Objective:** Record the Founder's Accept/override decision on the identity-based local registry
  model. This is the Phase 0 gate; the design it gates is already implemented and merged, but the
  decision itself has never been recorded.
- **Repository evidence:** ADR-0026's status field, read live this session: "Proposed — finalized for
  Founder approval (2026-07-05 finalization review closed identity/failure-mode/sharing-compatibility
  gaps found in the original draft)." `Backlog/backlog.md` WIP-001 is still unchecked.
- **Dependencies:** None — pure decision, no remaining design questions (`README.md` §"Every Open
  Decision, In One Place").
- **Deliverables:** ADR-0026 status field updated to Accepted, with the Founder-attributed decision
  recorded; `Backlog/backlog.md` WIP-001 checked.
- **Acceptance criteria:** ADR-0026 has a Founder-attributed decision on record; WIP-001 is checked.
- **Definition of Done:** Both artifacts updated and committed to `main`.
- **Estimated effort:** XS — a recorded decision, no engineering.

### T11 — Decide ADR-0029 (v1 sharing scope) — optional for v2.0

- **Objective:** Record the Founder's Accept/override decision on the four-role sharing model, if there
  is appetite to close this gate now.
- **Repository evidence:** ADR-0029's status field, read live this session: "Proposed — requires Founder
  decision." Blocks only Phase 5 (WIP-050/051/052), which is out of v2.0 scope regardless.
- **Dependencies:** None.
- **Deliverables:** ADR-0029 status field updated, if decided.
- **Acceptance criteria:** If decided, ADR-0029 carries a Founder-attributed decision. If not decided,
  no v2.0 gate is affected.
- **Definition of Done:** Not required for v2.0 tag. Should not be left indefinitely open past v2.1
  (`29` §8).
- **Estimated effort:** XS.

### T12 — Reconcile `Backlog/backlog.md` checkboxes

- **Objective:** Bring the backlog's checkboxes in line with actually-shipped state for
  WIP-021/022/023/030/031/032, which `22`/`23`/`26` show implemented but the backlog file still shows
  several unchecked.
- **Repository evidence:** `Backlog/backlog.md` current state (read this session) vs. `22`, `23`, `26`.
- **Dependencies:** None.
- **Deliverables:** Updated `Backlog/backlog.md` with accurate checkbox state for every shipped item,
  plus `T2`/`T3`/`T4`/`T5`/`T9`/`T10` as they land.
- **Acceptance criteria:** No checkbox drift between the backlog and actual `main` state — the exact
  failure mode `28` corrected for the PR #32/#33 case.
- **Definition of Done:** Committed to `main`; verified by direct comparison against `git log`/`gh pr
  list` ground truth, not by trusting the file's own prior state.
- **Estimated effort:** S.

### T13 — Commit the currently-untracked working-tree docs

- **Objective:** Land `24`, `25`, `27`, `28`, `29`, this Epic (`30`), and
  `docs/archive/superpowers/plans/2026-07-05-wip-032-registry-read-through-cache.md` on `main`.
- **Repository evidence:** `git status` this session confirms all of the above remain untracked. These
  are accepted, cited-as-authoritative documents that exist only as working-tree files today — the exact
  loss-on-machine-failure/`git clean` risk ADR-0025 was written to flag.
- **Dependencies:** None.
- **Deliverables:** A commit (or branch + PR, per repository convention) landing all listed files on
  `main`.
- **Acceptance criteria:** `git log` on `main` shows each file committed; no untracked accepted-planning
  file remains in the working tree afterward.
- **Definition of Done:** Merged to `main`.
- **Estimated effort:** S.

### T14 — Update `README.md`'s decision table

- **Objective:** Reflect ADR-0026/ADR-0029's closed status (once `T10`, and optionally `T11`, land) in
  `docs/roadmap/Workspace-Intelligence/README.md`'s "Every Open Decision, In One Place" table.
- **Repository evidence:** README currently (this session) still shows both ADRs open, with a prior
  partial update already staged (8-line uncommitted diff) for the 2026-07-05 review notes.
- **Dependencies:** `T10` (hard); `T11` (soft — only if decided).
- **Deliverables:** Updated decision table rows for ADR-0026 (and ADR-0029, if decided).
- **Acceptance criteria:** Table accurately reflects each ADR's actual status; no stale "open" row
  remains for a closed ADR.
- **Definition of Done:** Committed to `main` as part of, or immediately after, `T13`.
- **Estimated effort:** XS.

---

## 5. Dependency Graph

Reproduced and merged from `29` §3 and `28` §6 — no new dependency introduced.

```mermaid
flowchart TD
    T1["T1: Ship WIP-030/031+P3-002\nto main (new PR)"]
    T2["T2: WIP-013\nauto-migration"]
    T3["T3: WIP-014\nMCP tool"]
    T4["T4: WIP-040\nILogger events"]
    T5["T5: WIP-036\nranking normalization"]
    T6["T6: WIP-033\nreal-C# pooled prototype"]
    T7["T7: WIP-034\nCompressor"]
    T8["T8: WIP-035\nContext cache"]
    T9["T9: WIP-037\nworkspace remove"]
    T10["T10: Close ADR-0026\n(Founder)"]
    T11["T11: Decide ADR-0029\n(Founder, optional)"]
    T12["T12: Reconcile backlog.md"]
    T13["T13: Commit untracked docs"]
    T14["T14: Update README\ndecision table"]
    Tag["v2.0 tag"]

    T1 --> Tag
    T2 -.parallel, no dep.-> Tag
    T3 -.parallel, no dep.-> Tag
    T4 -.soft, de-risks.-> T6
    T4 -.soft, de-risks.-> T5
    T5 -.parallel, no dep.-> Tag
    T6 -->|hard, conditional| T7
    T6 -->|hard, conditional| T8
    T7 -.-> Tag
    T8 -.-> Tag
    T9 -.opportunistic.-> Tag
    T10 --> Tag
    T10 --> T14
    T11 -.optional, no v2.0 dep.-> T14
    T12 --> Tag
    T13 --> Tag
    T14 --> Tag
```

**Hard dependencies:** `T7`, `T8` ← `T6` (both explicitly consume its output, `20` §1, unchanged by
`27`/`28`). `T14` ← `T10` (cannot update the decision table until the decision exists).

**Soft dependencies:** `T4` → `T5`, `T6` — de-risks discovery of a future regression (per `26`'s own
history) but is not a data dependency; `T5`/`T6` do not read anything `T4` produces (`28` §6).

**Optional/conditional dependencies:** `T11` → `T14` — only if `T11` is decided during this release
window; otherwise `T14` proceeds on `T10` alone.

**Critical path:** `T1` → `T10`/`T12`/`T13` (parallel, all release-blocking, no dependency on each other)
→ Gate check → tag. `T2`, `T3`, `T4`, `T5`, `T9` are parallelizable off this path. `T6` is the only
item whose outcome branches the release (conditional inclusion of `T7`/`T8`), and it does not block the
tag either way, since Gate E accepts either resolution.

---

## 6. Release Gates

Reproduced from `29` §4, restructured with purpose/evidence/verification/pass-fail per gate.

### Gate A — Phase 3 shipped set on `main`

- **Purpose:** Prevent the exact failure `28` found — a proven cache regression silently absent from
  `main` because its fix landed on the wrong branch.
- **Required evidence:** `main` contains WIP-030/031 and the P3-002 fix commit.
- **Verification method:** `git merge-base --is-ancestor d3f7335 origin/main` (or the equivalent commit
  after `T1`'s new PR merges).
- **Pass/fail:** Exit code 0 = pass.

### Gate B — Implementation complete

- **Purpose:** Confirm every non-conditional engineering task has actually landed, not just been coded.
- **Required evidence:** `T1`–`T5`, `T9` merged to `main`; `T6` either merged or explicitly deferred (no
  "in progress" state at tag time).
- **Verification method:** Each corresponding WIP ticket checked off in `Backlog/backlog.md` (post-`T12`).
- **Pass/fail:** All required checkboxes checked = pass.

### Gate C — Tests passing

- **Purpose:** No regression against the established baseline.
- **Required evidence:** `dotnet test src/Ferret.sln --configuration Release` — 0 failed, 0 regressions
  vs. the 1,466-test baseline (`19`).
- **Verification method:** CI run on the release commit.
- **Pass/fail:** 0 failures and test count ≥ baseline = pass.

### Gate D — CI green

- **Purpose:** Independent confirmation beyond local test runs.
- **Required evidence:** `.github/workflows/ci.yml` passes on the exact commit being tagged.
- **Verification method:** GitHub Actions run status.
- **Pass/fail:** All required CI jobs green = pass.

### Gate E — WIP-033 real-value gate resolved, either direction

- **Purpose:** Prevent an ambiguous "still investigating" state from reaching the tag.
- **Required evidence:** Either (a) a real-C# pooled prototype re-validates `25`'s numbers at R=26 scale
  and merges, or (b) WIP-033 is explicitly deferred to v2.1 in `Backlog/backlog.md`.
- **Verification method:** Prototype benchmark output (if a) or an explicit backlog entry (if b).
- **Pass/fail:** Either (a) or (b) recorded = pass; anything ambiguous = fail.

### Gate F — Governance gates closed

- **Purpose:** Ensure the Phase 0 Founder gate is not left open under a release that already shipped the
  design it gates (ADR-0025's core lesson).
- **Required evidence:** ADR-0026 status = Accepted, with a Founder-attributed decision recorded;
  `Backlog/backlog.md` WIP-001 checked.
- **Verification method:** Read ADR-0026's status field; read the WIP-001 checkbox.
- **Pass/fail:** Both true = pass.

### Gate G — Documentation synchronized

- **Purpose:** Prevent the project's own source of truth from drifting from shipped reality.
- **Required evidence:** `T12`, `T13`, `T14` complete.
- **Verification method:** `git log` shows the previously-untracked docs committed; backlog checkboxes
  match shipped state; README decision table current.
- **Pass/fail:** All three tasks' Definition of Done met = pass.

### Gate H — Dogfooding complete

- **Purpose:** Final real-usage confirmation on the exact commit being tagged, not an earlier one.
- **Required evidence:** One final dogfooding pass on the exact `main` state slated for tag, at ≥R=26
  scale, with zero new Critical/High findings.
- **Verification method:** Session log, same format as `17`/`25`.
- **Pass/fail:** Zero new Critical/High findings = pass.

### Gate I — Release notes drafted

- **Purpose:** Ensure the release is documented for consumers at ship time.
- **Required evidence:** CHANGELOG/release notes entry summarizing what v2.0 ships.
- **Verification method:** File present in the release PR.
- **Pass/fail:** File present and accurate = pass.

**v2.0 does not require:** ADR-0029 closure (`T11`, blocks only Phase 5), or any Phase 4/5 evidence
(Usage Ledger, Analytics, Dashboard, Sharing/RBAC) — none of it is in this release.

---

## 7. Risk Register

Reproduced from `28` §7 (the only risk assessment already accepted for this program), restructured into
description/likelihood/impact/mitigation/owner. No new risk is introduced.

| # | Description | Likelihood | Impact | Mitigation | Owner |
|---|---|---|---|---|---|
| R1 | `T1` merged via the stale intermediate branch instead of the corrected direct-to-`main` PR, re-shipping the known WIP-030/031 regression (a cache hit 3.1x slower than a fresh query, per `26`) | Low, if this Epic's exact `T1` deliverable (new PR from `feature/wip-030-031-federated-query-cache` directly to `main`) is followed | Critical — ships a known, already-measured performance regression silently | Follow `T1`'s deliverable exactly; do not "finish the job" by merging the stale intermediate branch; `git merge-tree` already confirms the corrected path is conflict-free | Engineering |
| R2 | `T5`'s normalization strategy choice (z-score vs. min-max vs. rank-based) is unexplored in any prior document | Medium | Medium — could ship a normalization that doesn't actually fix the observed quality defect | Regression-test against `FederatedKnowledgeStore`'s existing `Merge` suite; document the chosen strategy's rationale in the PR | Engineering |
| R3 | `T6` (WIP-033) requires building connection-pooling infrastructure from scratch — no precedent anywhere in the repo — plus unvalidated accuracy tuning at production scale | High implementation risk (the plan's only "High" rating, `28` §7) | Medium — a wrong pooling implementation could introduce thread-safety bugs under concurrent MCP-server queries | Build as a throwaway/TDD spike first; require real-C# R=26 re-validation before merge (Gate E); accept explicit v2.1 deferral as a fully valid outcome, not a failure | Engineering |
| R4 | `T7`/`T8` have no design work started and are unassessed for effort | Medium (depends entirely on `T6`'s outcome and timing) | Low for v2.0 (both are already correctly conditional/deferred) | Do not start design work until `T6` resolves; accept v2.1 deferral by default | Engineering |
| R5 | Two Founder governance gates (`T10` mandatory, `T11` optional) remain open independent of code state; tagging without closing `T10` repeats the exact anti-pattern ADR-0025 was written to prevent (a governance decision never recorded even though the work it governs shipped) | Medium — requires a human decision outside engineering's control to schedule | High — Gate F fails, No-Go per `29` §9 | Request the Founder's ADR-0026 decision early in the execution window, in parallel with `T1`–`T9`; it has zero engineering dependency and can happen immediately | Founder / Release Lead |
| R6 | The five currently-untracked accepted documents (`24`, `25`, `27`, `28`, `29`, plus this Epic and the WIP-032 plan doc) exist only in one working tree — loss on machine failure or an accidental `git clean` would erase cited-as-authoritative planning history | Low probability per incident, but the cost compounds with every day left uncommitted | High if it occurs — loss of accepted planning record, the exact risk ADR-0025 flags | Execute `T13` early, independent of the engineering tasks; no dependency blocks it | Documentation |
| R7 | `Backlog/backlog.md` checkbox drift (already observed once, for WIP-021/022/023/030/031/032) recurs if `T12` is done once and not re-checked before tag | Medium | Medium — Gate B/F verification relies on backlog checkboxes being trustworthy | Re-run `T12`'s reconciliation immediately before cutting the release candidate, not only once mid-execution (mirrors Validation Plan step 1's re-run principle) | Documentation |

---

## 8. Validation Strategy

Reproduced from `29` §5, mapped per story — no new validation activity introduced.

| Story | Required tests | CI validation | Dogfooding | Documentation updates |
|---|---|---|---|---|
| T1 | Full `dotnet test src/Ferret.sln --configuration Release` after merge (baseline: 1,466 passing, 0 regressions) | CI green on the merge commit | Already complete (`23`, `26`) — no new session required | `Backlog/backlog.md` (via `T12`) |
| T2 | Existing single-repo integration suite, unmodified, must still pass | CI green | Full existing dogfooding command set against an already-migrated checkout | Backlog checkbox |
| T3 | Tool-output-matches-CLI-output test | CI green | Exercised via existing MCP client | Backlog checkbox |
| T4 | Unit coverage for new log events; no regression in federation path tests | CI green | Observed passively during any subsequent dogfooding session (its purpose is exactly this) | Backlog entry rescoped note |
| T5 | Regression suite against `FederatedKnowledgeStore.Merge`'s existing tests | CI green | Verify improved answer quality on a real multi-source query during the final Gate H pass | Backlog checkbox; PR note on chosen normalization strategy |
| T6 | New unit/integration tests for the pooled prototype | CI green | **Mandatory:** re-run `25`'s R=26 `dogfood-hub` benchmark in real C#, not simulation — the explicit, unresolved condition `27`/`28` both impose | `Backlog/backlog.md` records Gate E's outcome, either direction |
| T7/T8 | Not yet defined — scoped only after `T6` resolves | N/A until scoped | N/A until scoped | Backlog entries updated once (if) started |
| T9 | Mirrors `remove-repo`/`remove-reference` existing test pattern | CI green | Confirmed during Gate H's final smoke pass | Backlog checkbox |
| T10 | N/A — decision, not code | N/A | N/A | ADR-0026 status field; `Backlog/backlog.md` WIP-001 |
| T11 | N/A — decision, not code | N/A | N/A | ADR-0029 status field, if decided |
| T12 | N/A — documentation | N/A | N/A | `Backlog/backlog.md`, verified against `git log`/`gh pr list` ground truth |
| T13 | N/A — documentation | N/A | N/A | Commits `24`, `25`, `27`, `28`, `29`, `30`, and the WIP-032 plan doc to `main` |
| T14 | N/A — documentation | N/A | N/A | `README.md` decision table |

**Mandatory before v2.0 (Gate H, folded into Gate E):**

1. Re-run `28`'s ground-truth method immediately before tagging, not only once at plan-writing time —
   re-check `gh pr view --json baseRefName,mergeCommit` and `git merge-base --is-ancestor` for every task
   in §4, not only `T1`.
2. If `T6` ships, re-validate its real-C# benchmark against `dogfood-hub` at R=26 — not the Python
   simulation from `24`, and not the R=1–2 scale used before `25`.
3. One final dogfooding smoke pass (Gate H) on the release candidate commit, at ≥R=26 scale, covering at
   minimum: a federated query spanning 2+ workspaces, a cache hit/miss cycle on the corrected WIP-030/031
   path, and (if shipped) WIP-033/036/037's user-facing surface.

**Not required for v2.0, flagged for v2.1:** a genuine multi-user dogfooding session (the single
precondition that would change Phase 4/5's deferred status — outside engineering's control to schedule),
and empirical confirmation of WIP-034/035's real value once WIP-033 has accumulated real usage.

**Post-release monitoring:** watch `T4`'s structured `ILogger` events in real usage after v2.0 ships —
their entire purpose is to make a repeat of `26`'s regression visible from normal usage instead of
requiring another dedicated dogfooding sprint to discover.

---

## 9. Execution Order

Reproduced from `29` §7's Release Checklist, organized into the requested execution phases. No step is
reordered or added.

### First commit / first PR

1. Push `d3f7335` on `feature/wip-030-031-federated-query-cache`.
2. Open a new PR: `feature/wip-030-031-federated-query-cache` → `main` (not the stale
   `feature/wip-032-registry-read-through-cache` branch). Confirm `git merge-tree` shows no conflicts
   first.

### Merge sequence

3. Merge that PR. This is `T1` / Gate A.
4. Implement and merge `T2` (WIP-013), `T3` (WIP-014) — independently, each with its own passing test
   run.
5. Implement and merge `T4` (WIP-040, structured `ILogger` events only).
6. Implement and merge `T5` (WIP-036, ranking normalization) — pick a normalization strategy and
   regression-test against `FederatedKnowledgeStore.Merge`'s existing suite.
7. Implement and merge `T9` (WIP-037, `workspace remove`) — opportunistically, not blocking anything
   else.
8. Build the `T6` (WIP-033) real-C# pooled prototype as a throwaway/TDD spike; re-run `25`'s R=26
   `dogfood-hub` benchmark against it.
   - If it confirms a real win at that scale: merge it (`T6`), then start `T7`/`T8` (WIP-034/035).
   - If it does not, or isn't ready in time: mark WIP-033 (and therefore WIP-034/035) explicitly deferred
     to v2.1 in `Backlog/backlog.md`. Either outcome satisfies Gate E.
9. Get the Founder's ADR-0026 decision (`T10`): Accept the identity-based local registry as specified, or
   override. Record it in the ADR and check off `Backlog/backlog.md` WIP-001.
10. (Optional for v2.0) Get the Founder's ADR-0029 decision (`T11`), if there is appetite to close the
    Phase 5 gate now.

### Testing sequence

11. Full `dotnet test src/Ferret.sln --configuration Release` after every merge to `main` (baseline:
    1,466 passing, zero regressions expected). `T6`, if it ships, additionally requires its own R=26
    dogfooding re-run before merge (Gate E), not just unit tests.

### Documentation updates

12. Reconcile `Backlog/backlog.md` checkboxes against actual shipped state (`T12`).
13. Commit and land the currently-untracked docs (`24`, `25`, `27`, `28`, `29`, this Epic `30`, and the
    WIP-032 plan doc) on `main` (`T13`).
14. Update `README.md`'s decision table to reflect closed ADRs (`T14`).

### Release preparation

15. Run the full test suite on the final `main` commit: `dotnet test src/Ferret.sln --configuration
    Release`. Confirm 0 failed, no regression against the 1,466-test baseline. (Gate C)
16. Confirm CI is green on that exact commit (`.github/workflows/ci.yml`). (Gate D)
17. Re-run `28`'s ground-truth check on the final state: for every merged PR in this checklist, confirm
    via `gh pr view --json baseRefName,mergeCommit` that it landed where intended, and via
    `git merge-base --is-ancestor` that every commit this plan depends on is actually an ancestor of the
    commit being tagged.

### Release candidate

18. Run one final dogfooding smoke pass at ≥R=26 scale against the release candidate commit. (Gate H)
19. Write the release notes / CHANGELOG entry for v2.0. (Gate I)

### Final tag

20. Tag the release (`v2.0.0`, following the existing tag pattern — `v0.16.0`, `v0.15.0`, etc.) and run
    whatever the existing `release.yml`/`npm-publish.yml` workflows do for a tagged release, same as the
    last tagged release.
21. Ship.

---

## 10. Exit Criteria

### Story Complete

- **Engineering stories (`T1`–`T5`, `T9`):** merged to `main`; `dotnet test src/Ferret.sln
  --configuration Release` passes with 0 failures and 0 regressions against the 1,466-test baseline;
  the story's own Acceptance Criteria (§4) are met; the corresponding `Backlog/backlog.md` checkbox is
  checked.
- **Conditional stories (`T6`, and `T7`/`T8` if triggered):** Gate E resolved in either direction and
  recorded in `Backlog/backlog.md` — merged-and-validated, or explicitly deferred. An ambiguous
  "still investigating" state is not Story Complete.
- **Governance stories (`T10`, `T11`):** the corresponding ADR's status field carries a
  Founder-attributed decision.
- **Documentation stories (`T12`, `T13`, `T14`):** the target file(s) committed to `main` and verified
  against ground truth (`git log`, `gh pr list`), not against the file's own prior claims.

### Epic Complete

- All of `T1`–`T5`, `T9`–`T14` meet their Story Complete criteria.
- `T6` is resolved in either direction (merged-and-validated, or explicitly deferred to v2.1) — not left
  open.
- `T7`/`T8` are either merged (if `T6` landed early and cleanly) or explicitly recorded as v2.1
  candidates in `Backlog/backlog.md`.
- Gates A, B, F, G (§6) all pass.

### Release Candidate

- Epic Complete, plus Gates C, D, and I (§6) pass on the exact commit proposed as the release candidate.
- `28`'s ground-truth re-check (Execution Order step 17) has been re-run against the release candidate
  commit specifically, not against an earlier state, and surfaces no discrepancy.

### Ferret v2.0 Released

- All of Gates A–G and I (§6) pass, **and** Gate H's final dogfooding pass (≥R=26 scale, on the exact
  tagged commit) shows zero new Critical/High findings — the full Go criteria from `29` §9.
- None of `29` §9's No-Go conditions hold at tag time: `main` is not missing WIP-030/031 or the P3-002
  fix; no test regression against the 1,466-test baseline; CI is not red on the release commit; if `T6`
  merged, its real-C# benchmark was actually re-validated at R=26 (not just merged); ADR-0026 is not
  still "Proposed" with WIP-001 unchecked; the final ground-truth re-check surfaces no uncorrected
  discrepancy.
- The `v2.0.0` tag is pushed and the existing release workflow (`release.yml`/`npm-publish.yml`,
  following the `v0.16.0` pattern) completes successfully.

**Explicitly not an exit-blocking condition, for any of the above:** absence of Phase 4 (Usage Ledger/
Analytics/Dashboard) or Phase 5 (Sharing/RBAC). Both are correctly out of v2.0 scope per `27` §2, and no
review accepted by this milestone has ever made either a v2.0 requirement.
