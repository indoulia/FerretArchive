# DOGFOOD-001 — Ferret Dogfooding Plan

| Field | Value |
|---|---|
| **Document ID** | DOGFOOD-001 |
| **Version** | 1.0 |
| **Status** | Closed (2026-07-05) — see Closure Note below |
| **Owner** | Ferret Project |
| **Date** | 2026-06-30 |
| **Period** | 4–8 weeks from v0.15.0 publication |
| **Supersedes** | `docs/DOGFOOD.md` (RC1 internal-usability task log) for the post-release period |
| **Related** | ARCH-022 (Distribution Platform); v0.15.0 release notes |

---

## Purpose

This is the authoritative guide for the dogfooding period that follows the
v0.15.0 Distribution Platform release. The goal is to drive Ferret toward
General Availability using **evidence from real daily use** rather than new
feature work.

For the duration of this plan, **no new implementation milestone is planned or
started.** Work is limited to: using Ferret, recording evidence, and fixing bugs
the dogfooding surfaces. New capabilities are captured as backlog candidates and
deferred until the GA Readiness gate (Phase 7) is met and this document is
formally closed.

## Scope

- **In scope:** installing Ferret only via the published channel (`npm install -g @indoulia/ferret`), using it for real engineering and AI-assisted work, across multiple repositories, machines, operating systems, and AI hosts; collecting performance baselines; and triaging/fixing the bugs found.
- **Out of scope:** new features, architectural changes, and new platform layers. If dogfooding proves a capability is *missing* (not broken), record it as a GA-blocking gap only if it prevents the success criteria below; otherwise it is post-GA backlog.

## How to use this document

1. Install Ferret from the published npm package — never from source — for all dogfooding. Using a source build invalidates that day's distribution evidence.
2. Each working day, fill in one **Daily Log** entry (template at the end).
3. File every defect with a severity from the **Bug Severity Rubric** and reference its ID in the daily log.
4. At the end of each week, evaluate progress against the current phase's **success criteria** and decide whether to advance.
5. Phases 1–5 run largely in parallel during weeks 1–6; Phase 6 (benchmarks) is collected throughout; Phase 7 (GA Readiness) is the closing gate.

## Cadence & duration

- **Minimum:** 4 weeks of active use. **Target:** 6 weeks. **Maximum:** 8 weeks before a go/no-go decision is forced.
- **Minimum active-use days:** 20 logged days across the period.
- A week "counts" only if Ferret was used on ≥3 days that week.

---

## Bug Severity Rubric

| Severity | Definition | Examples | Response | GA impact |
|---|---|---|---|---|
| **Critical** | Data loss, security exposure, or a failure that breaks a previously working system. | Uninstall deletes a `.ferret` workspace or user data; install corrupts an existing install; checksum bypassed; crash that loses index state; remote code/credential exposure. | Stop dogfooding the affected path. Fix immediately, before any other work. | **Hard blocker.** Zero open Criticals required for GA. |
| **High** | A core workflow is broken or unreliable, even if a workaround exists; major performance regression. | `ferret index` fails on a common repo; `ferret search` returns wrong/empty results for valid queries; MCP server fails to register tools with a supported host; install fails on a supported OS. | Fix before GA. Target resolution within the same week it is found. | **Blocker.** Zero open Highs required for GA. |
| **Medium** | Degraded experience or edge-case failure with a clear workaround; confusing but non-fatal behavior. | Unclear error messages; slow on very large files; an uncommon file type not parsed; a flag behaves unexpectedly. | Batch-fix during the period. Must be triaged (accepted/deferred) before GA. | **Soft.** ≤5 open Mediums at GA, each with a documented decision. |
| **Low** | Cosmetic or polish; documentation gaps; minor inconsistency. | Help-text typo; log noise; formatting; non-blocking docs omission. | Backlog. Fix opportunistically. | None. Tracked, not gating. |

Every filed bug records: ID, date, severity, phase, machine/OS, Ferret version, repro steps, and (for Critical/High) whether a working install was harmed.

---

## Phase 1 — Personal Workflow

**Objective:** Ferret becomes the primary way you navigate and understand code in your own day-to-day work, replacing ad-hoc file reads and grep.

**Activities:** `ferret init` + `ferret index` your main working repo; use `ferret search` and `ferret watch` for real navigation/comprehension tasks; run `ferret doctor` when something looks off.

**Measurable success criteria:**
- ≥ 50 real navigation/comprehension tasks completed using Ferret as the primary source across the period.
- ≥ 90% of those tasks answered **without a workaround** (no falling back to manual file reads or external grep).
- Median `ferret search` latency ≤ 500 ms on the primary repo; p95 ≤ 1.5 s.
- `ferret watch` reflects an edited file in search results within ≤ 5 s of save.
- Zero Critical or High bugs open against core local commands at phase exit.

**Exit:** All criteria met for one full week.

---

## Phase 2 — Daily AI Workflow

**Objective:** Ferret-supplied context measurably improves AI-assisted work in daily use (context assembly, `ferret ask`, and Ferret-backed prompts).

**Activities:** use `ferret ask` and Ferret-assembled context for real questions during coding; compare answers with and without Ferret context on a sample of tasks.

**Measurable success criteria:**
- ≥ 30 AI-assisted tasks completed with Ferret-supplied context.
- On a blind-rated sample of ≥ 15 tasks, Ferret-supplied context rated "helpful or better" on ≥ 80%.
- Context assembly for a typical question completes in ≤ 3 s.
- No task where Ferret context introduced misleading/incorrect information that changed the outcome (any such case is a **High** correctness bug).

**Exit:** Criteria met and any correctness issues resolved.

---

## Phase 3 — Multi-Repository Validation

**Objective:** Ferret works reliably across repositories of varying size, language, and structure — not just its own codebase.

**Activities:** index ≥ 5 distinct repositories spanning ≥ 3 languages and a range of sizes (small < 10k LOC, medium, large > 500k LOC or > 20k files).

**Measurable success criteria:**
- ≥ 5 repositories indexed successfully (`ferret index` exits 0, index is queryable).
- ≥ 3 languages and at least one "large" repo represented.
- 100% of indexed repos return relevant results for ≥ 3 hand-checked queries each.
- Full index of the large repo completes without crash and within a recorded, acceptable time (baseline captured in Phase 6).
- Incremental re-index after a small change is ≤ 20% of the full-index time.

**Exit:** All repos queryable; no open Critical/High from this phase.

---

## Phase 4 — Cross-Machine Installation

**Objective:** The published npm package installs and runs cleanly for a first-time user across supported platforms.

**Activities:** `npm install -g @indoulia/ferret` on clean machines / fresh user profiles across operating systems; run the full lifecycle; then `npm uninstall` and confirm data preservation.

**Measurable success criteria:**
- Successful clean install on **all** supported targets exercised: Windows x64, Linux x64, and at least one macOS (arm64 or x64).
- Each install passes the lifecycle: `ferret --version` → `ferret init` → `ferret index` → `ferret search` → `ferret serve` (starts and accepts a request) → `npm uninstall`.
- **Time-to-first-search** (from `npm install` to first successful `ferret search`) ≤ 5 minutes on each machine.
- SHA256 verification observed to occur on every install (a tampered/short download is rejected — verify once deliberately).
- `npm uninstall` removes the binary and **preserves** every `.ferret` workspace, index, and config on every machine (any violation is **Critical**).
- macOS Gatekeeper behavior matches the documented unsigned-binary guidance (no surprise beyond the documented prompt).

**Exit:** Clean install + lifecycle + safe uninstall verified on every supported target.

---

## Phase 5 — AI Host Validation

**Objective:** Ferret's MCP server is usable from real AI hosts, not just the CLI.

**Activities:** connect `ferret serve` (MCP) to ≥ 2 hosts (e.g., Claude Code and Claude Desktop, plus any additional MCP-capable host available); exercise the exposed tools from within the host.

**Measurable success criteria:**
- MCP server registers and its tools are discovered by ≥ 2 distinct hosts.
- ≥ 20 real tool invocations issued from within a host; ≥ 95% succeed (valid response, no protocol error).
- Tool input validation rejects malformed input with a clear error (no crash).
- A full host session of ≥ 30 minutes runs without the server crashing, hanging, or leaking (memory stable — baseline in Phase 6).

**Exit:** ≥ 2 hosts validated; no open Critical/High from this phase.

---

## Phase 6 — Benchmark Collection

**Objective:** Establish reproducible performance and quality baselines to judge GA readiness and detect future regressions. Collected continuously throughout the period.

**Metrics to capture (record machine spec, OS, Ferret version, repo, and date for each):**

| Metric | How measured | Baseline target (initial) |
|---|---|---|
| Index throughput | files/sec and MB/sec on full index | Recorded per repo; no hard target yet |
| Full-index time | wall-clock for the large repo | Recorded; must not crash |
| Incremental re-index time | wall-clock after a 1-file change | ≤ 20% of full-index time |
| Search latency | median + p95 over ≥ 50 queries | median ≤ 500 ms, p95 ≤ 1.5 s |
| Context assembly time | wall-clock for a typical question | ≤ 3 s |
| Peak memory | during full index and during a 30-min serve session | Recorded; stable, no unbounded growth |
| Index size on disk | `.ferret` size vs source size | Recorded as a ratio |
| Cold-start time | `ferret --version` / first command | ≤ 1 s |

**Measurable success criteria:**
- Every metric above has at least one recorded baseline on a documented machine.
- Search-latency and incremental-reindex targets met on the primary repo.
- No metric shows unbounded growth (memory, index size) over a sustained session.

**Exit:** Baseline table fully populated and committed to the repo.

---

## Phase 7 — GA Readiness

**Objective:** Decide go/no-go for GA 1.0 based on the accumulated evidence. This is the closing gate.

**GA Readiness criteria (all must hold):**
- **Usage:** ≥ 20 logged active-use days; ≥ 50 Phase-1 tasks and ≥ 30 Phase-2 AI tasks completed.
- **Reliability:** **0** open Critical bugs, **0** open High bugs.
- **Mediums:** ≤ 5 open Medium bugs, each with a documented accept/defer decision.
- **Workaround rate:** ≥ 90% of Phase-1 tasks answered without a workaround across the full period.
- **Distribution:** clean install + safe uninstall verified on all supported targets (Phase 4); SHA256 verification confirmed.
- **AI hosts:** ≥ 2 hosts validated with ≥ 95% tool-call success (Phase 5).
- **Benchmarks:** full baseline table recorded; latency + incremental-reindex targets met (Phase 6).
- **No data-loss events** of any kind during the entire period.

**Decision outputs:**
- **GO → GA 1.0:** tag and release 1.0; close this document; resume milestone planning.
- **NO-GO → RC2:** only if evidence justifies it (e.g., a Critical class of issue). Scope RC2 strictly to the failing criteria, then re-run the relevant phases.

---

## Daily Log Template

Copy one block per active-use day into a running log (e.g., append to this file under a `## Daily Logs` section, or a per-week file under `docs/dogfood/`).

```
### YYYY-MM-DD
- Machine / OS:            (e.g., Win11 x64 / desktop)
- Ferret version:         (ferret --version)
- Install source:         published npm  (must be npm, not source)
- Phase(s) exercised:     1 / 2 / 3 / 4 / 5 / 6
- Tasks attempted:        N
- Answered w/o workaround: N  (workaround rate: %)
- Workarounds used:       (what + why, or "none")
- Latency observed:       search median __ ms, p95 __ ms; context __ s
- Bugs filed:             [ID severity one-line] ... (or "none")
- Data-loss event:        NO / YES (if YES → Critical, stop and fix)
- Notes / friction:       (free text — anything that slowed you down)
```

### Weekly rollup (end of each week)

```
## Week N (YYYY-MM-DD .. YYYY-MM-DD)
- Active-use days:        N (week counts if >=3)
- Tasks total / no-workaround %:
- New bugs by severity:   C_ H_ M_ L_   | Open by severity: C_ H_ M_ L_
- Phases advanced this week:
- Benchmarks recorded:    (which metrics)
- Decision:               continue / address blockers / force go-no-go (week 8)
```

---

## Issue tracking

- File bugs in the project tracker with the `dogfood` label and the severity.
- Critical/High bugs link back to the daily-log date that found them.
- This document is the source of truth for the period; update the status header to `Closed` with the GA decision when Phase 7 concludes.

---

## Closure Note (2026-07-05)

**This document is closed on a different, narrower basis than the Phase 1–7 GA Readiness plan above — recorded here plainly so the distinction isn't lost.**

What was **not** done: the phased plan's own exit criteria (≥20 logged active-use days, ≥50 Phase-1 tasks, ≥30 Phase-2 AI tasks, 2 AI hosts validated at ≥95% tool-call success, the full Phase 6 benchmark table, a GA go/no-go decision) were never pursued. No entries exist under a `## Daily Logs` section in this file in the format this document itself specifies. Phase 7 was not reached.

What **was** done (2026-07-04 to 2026-07-05, see `docs/dogfooding/2026-07-04-daily-log.md` and `2026-07-05-daily-log.md`): direct, hands-on CLI/MCP dogfooding of Ferret against itself and a real external repository, structured as an evolving series of sessions (bug-hunting → trust validation → reliability validation → confidence confirmation). This surfaced and fixed 10 real defects (see below), then ran a defined regression checklist (`docs/dogfooding/REGRESSION-CHECKLIST.md`) across 3 consecutive ordinary-engineering-work sessions with no new findings, no new implementation fixes required, and no tool other than Ferret used — the closing bar this exercise's later sessions explicitly defined for itself, distinct from the original Phase 7 criteria above.

**Issues found and fixed** (committed to branch `dogfooding`, TDD, full solution suite green throughout): #14, #15, #16, #19, #20, #22, #24, #26, #27, #28.

**Issues found and filed, not fixed** (genuine design decisions or unconfirmed root causes — deliberately not guessed at): #17 (`ferret watch` visibility latency), #18 (`ferret doctor` freshness has no git-branch awareness), #21 (`ContextAssembler` swallows search failures silently), #23 (`--passages` flag is a non-functional no-op), #25 (`ferret status` is a hardcoded stub). #9/#13 (directory-open failures) were already fixed on `main` prior to this exercise, pending an npm release.

**Disposition:** closed as "sufficient evidence gathered to proceed," not as "GA readiness confirmed." If GA readiness against the original Phase 1–7 criteria is still wanted, that remains a separate, not-yet-started body of work.
