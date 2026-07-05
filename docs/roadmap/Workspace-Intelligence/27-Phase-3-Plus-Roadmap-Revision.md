# 27 — Phase 3+ Roadmap Revision

**Status:** Complete — analysis only, no implementation, no architecture change, no code written.
**Purpose:** Workspace Intelligence Core (Phase 0–2, the Vertical Slice, and Stabilization Sprint 1) is
merged and stable on `main` (PR #31). Per the Founder's 2026-07-05 direction, this document re-prioritizes
everything from `Backlog/backlog.md`'s Phase 3 onward (WIP-033+) using the implementation evidence,
benchmarks, and dogfooding gathered in `20`–`26` — not the backlog's original ordering or the design
docs' intent. Every recommendation below is evidence-backed; where no evidence exists yet, that is
stated as the reason for deferral, not papered over.

---

## 1. What's Already Shipped But Not Yet on `main`

Not a backlog item — an operational fact this review surfaced by checking `git log`. P3-001, WIP-032,
WIP-030/031, and P3-002 are all **implemented, tested, and dogfood-validated** (docs `21`, `22`, `23`,
`26`), but live only on `feature/wip-032-registry-read-through-cache` and
`feature/wip-030-031-federated-query-cache` — `main` is still at `9402823` (P3-001 only). PR #32
(WIP-032 → `main`) and PR #33 (WIP-030/031 + P3-002 → PR #32) are open and stacked.

**Recommendation: merge PR #32 then PR #33 immediately.** This is zero engineering effort (already
done), zero risk (already dogfooded at both R=2 and R=26 scale, including the regression found and
fixed at R=26), and every day it isn't merged is a day the ~1000x registry-cache and ~30-35x
query-cache speedups exist only in a benchmark writeup, not in anything a real user or the MCP server
actually runs. Nothing below this point should be scheduled ahead of this merge.

---

## 2. Item-by-Item Disposition

| Item | Evidence | Disposition |
|---|---|---|
| **WIP-033** Scope Classifier | `24` proved the `fts5vocab` mechanism is real, accurate (zero false negatives at R=26, doc `25` §3), and cheap — but only in a **pooled**-connection shape (2.71 ms/query) measured via Python/SQLite simulation, not real C#. The **naive** shape sketched in `20`/`24` (fresh connection per reference per query, 17.13 ms) is not a net win at measured scale. `25`'s own recommendation is explicit: "re-sequence, not implement yet" pending a real-C# pooled prototype re-validated against the live `FederatedKnowledgeStore`. | **Implement now, but only the pooled design, and only after a throwaway/TDD prototype re-validates `25`'s numbers in real C# against `dogfood-hub` (R=26) before merging.** This supersedes `24`'s weaker "implement with minor adjustments" verdict — `25` is newer evidence at a scale `24` never tested. |
| **WIP-034** Compressor | No implementation evidence exists; `20`'s own analysis (§1) says it strictly consumes WIP-033's intent-classification output and has "low standalone value." Nothing in `24`/`25`/`26` changes this — it was never simulated or benchmarked. | **Defer**, sequencing unchanged (after WIP-033). Do not start until WIP-033 actually merges and its real (not simulated) value is confirmed — building a compressor for a classifier that might still change shape is premature. |
| **WIP-035** Context assembly cache | Same as WIP-034 — no evidence gathered, and its cache key explicitly depends on WIP-033's "scope-classified workspace set" (`20` §1). WIP-031 (a co-dependency) is done, pending the `main` merge in §1. | **Defer**, same gate as WIP-034. |
| **Cross-repo ranking normalization** *(new item — propose **WIP-036**; currently untracked in `backlog.md`)* | Flagged twice as a live gap (`18` §2/§5, `20` §3) and **freshly confirmed at real scale**: `25`'s R=26 mixed-corpus setup is exactly the condition where uncalibrated per-source BM25 (`FederatedKnowledgeStore.Merge`'s flat `OrderByDescending`) systematically favors a large corpus over a small, more-relevant one — this is happening in the dogfooded environment today, independent of whether WIP-033/034 ever ship. `20` §3 already concluded it's decoupled from both Context Optimization tickets, so it isn't blocked by anything. | **Implement now, ahead of WIP-033/034.** It is a demonstrated, live quality defect at the same real scale (`dogfood-hub`, R=26) that motivated re-sequencing WIP-033, has no dependency on anything unmerged, and — unlike WIP-033 — needs no further validation sprint to justify starting; the defect is already proven, only the fix is unbuilt. |
| **`workspace remove` / bulk-cleanup command** *(new item — propose **WIP-037**; currently untracked)* | Surfaced three times now as a real operational gap, not a hypothetical: `22`/`23` and `25` (25 throwaway workspace entries left permanently registered in `~/.ferret/workspaces` because no such command exists). Purely a dogfooding/testing-ergonomics gap so far — no end-user complaint, no design doc reference. | **Implement as a small, low-priority backlog item.** Cheap (mirrors existing `remove-repo`/`remove-reference` command shape), unblocks future at-scale dogfooding sessions from leaving permanent registry cruft, and needs no design work — but it is genuinely low priority; do not let it preempt WIP-036 or the WIP-033 prototype. |
| **WIP-040** Telemetry metrics (`cache.federation.{hit,miss}`, `workspace.federated_query.duration`, etc.) | `20` §2 explicitly noted "no telemetry currently wraps" the fingerprint bottleneck that P3-001 later fixed, and `26`'s entire regression (a cache hit slower than a cache miss) was found only because a dedicated dogfooding+benchmarking *sprint* was run — not because any running system surfaced it. The backlog already tags this a "quick win... independent of Phase 2 landing." | **Promote — implement now, ahead of WIP-033.** This is the highest-leverage low-effort item on the whole remaining backlog: had `cache.federation.hit`/`.duration` existed before `25`, the P3-002 regression would have been visible from real usage instead of requiring a purpose-built dogfooding sprint to discover. Every future Phase 3 optimization (including the WIP-033 prototype in the item above) benefits from this existing first. |
| **WIP-041** Usage Ledger sink | No implementation evidence; requires new design surface (`10-Usage-Ledger.md`) not yet touched by any dogfooding session. All dogfooding to date is single-developer, not multi-user — there is no usage pattern yet for a ledger to meaningfully aggregate. | **Defer as a bundle with WIP-042/043/044** (below). `13-Storage.md`'s backend-swappable design means deferring costs nothing architecturally — this was a deliberate design property, not an accident (`Deferred-Scope.md`'s billing entry states the same thing for the ledger schema). |
| **WIP-042** Ship ADR-0028 retention default | Trivial once WIP-041 exists; ADR-0028 itself was already downgraded from a Founder gate to an implementation detail (`README.md`, 2026-07-05 review). No evidence changes this. | **Defer with WIP-041** — there's no ledger to retain data from yet. |
| **WIP-043** Analytics rollups | Depends on WIP-041. No usage data exists to analyze — every "benchmark" produced so far (`21`–`26`) came from throwaway direct-instantiation probes, not from ledger-derived analytics, and that pattern has worked fine for every optimization decision made to date. | **Defer with WIP-041.** Building analytics for data that doesn't exist yet, ahead of a real multi-user signal, is exactly the speculative work the Founder's original directive ruled out. |
| **WIP-044** Dashboard CLI | Depends on WIP-043. Same reasoning. | **Defer with WIP-041–043** as one bundle. |
| **WIP-050** `sharing` field + `workspace share` | Gated on ADR-0029 (still open per `README.md`), and — more importantly — **zero dogfooding evidence of a sharing need exists**: every session (`17`, `22`, `23`, `25`, `26`) has been single-developer, single-registry. `Deferred-Scope.md`'s RBAC entry already frames the "pick this up when" condition as real multi-user/org demand, which hasn't materialized. | **Defer**, pending both the ADR-0029 decision *and* an actual multi-user dogfooding scenario — neither precondition is close to being met by anything observed so far. |
| **WIP-051** Permission check on reference resolution | Depends on WIP-050 existing to have anything to check. | **Defer with WIP-050.** |
| **WIP-052** Four-role enforcement | Same — ADR-0029-gated, no usage evidence. | **Defer with WIP-050/051** as one bundle. |

**Retire: none.** No remaining backlog item has evidence indicating it should be dropped outright — every deferred item has a clear, evidence-stated "pick this up when" condition rather than being wrong or obsolete.

**Merge: none beyond what's already merged.** WIP-030+031 were already merged into one ticket during implementation (`23`); no further merge opportunities were found among the remaining items — WIP-034/035's dependency on WIP-033 is a *sequencing* gate, not a *merge*, since each still ships its own independently testable unit of value once its turn comes.

---

## 3. Revised Order

Supersedes `backlog.md`'s Phase 3–5 ordering for everything not yet started. Items already done
(P3-001, WIP-032, WIP-030/031, P3-002) are omitted here; see §1 for their merge status.

1. **Merge PR #32 → PR #33 → `main`.** (§1 — zero new work, unblocks everything else being real.)
2. **WIP-040** — telemetry for the federation/cache path. Quick win, no dependencies, de-risks every item below it.
3. **WIP-036** *(new)* — cross-repo ranking normalization. Live, evidenced defect; no dependencies.
4. **WIP-033** — Scope Classifier, pooled design only, real-C#-validated at R=26 before merging.
5. **WIP-034** — Compressor, only after WIP-033 merges and shows real (not simulated) value.
6. **WIP-035** — Context assembly cache, same gate as WIP-034.
7. **WIP-037** *(new)* — `workspace remove`/bulk cleanup. Low priority; slot in opportunistically, does not block anything above or below it.
8. **WIP-041–044** — Usage Ledger, retention, analytics, dashboard, as one bundle. Defer until real multi-user usage exists.
9. **WIP-050–052** — Sharing/RBAC, as one bundle. Defer until ADR-0029 closes *and* multi-user demand is observed.

## 4. Why This Order Beats the Existing Backlog's

`backlog.md`'s Phase 3–5 ordering was written from design intent (`05`–`11`) before any of Phase 3 had
implementation or dogfooding evidence. Three things evidence changed since then, none of which the
original ordering could have known:

- **WIP-033's cost/benefit case is real but conditional on a design correction (pooled connections) the original ticket never specified** — discovered only by dogfooding at R=26, a scale nothing before `25` had ever tested (`17`/`22`/`23` all used R=1–2).
- **A telemetry gap (WIP-040) is what turned finding the P3-002 regression into a dedicated sprint instead of a dashboard glance** — this promotes WIP-040 ahead of further perf work, not because the backlog undervalued it, but because `26`'s root-cause story is itself the evidence for why it should come first now.
- **A ranking-quality defect (proposed WIP-036) was live and measurable at the same R=26 scale that justified re-sequencing WIP-033** — it was always known (`18`), but had no forcing function to rank it until real mixed-scale evidence existed to compare it against.

Phase 4 (WIP-041–044) and Phase 5 (WIP-050–052) are unchanged in *relative* order but are pushed later
in absolute terms: nothing in any dogfooding session to date (all single-developer) produced evidence
that ledger/analytics/dashboard or sharing/RBAC would deliver real value yet — deferring them costs
nothing architecturally (both are explicitly backend-swappable per `13-Storage.md` and
`Deferred-Scope.md`) and avoids building against a usage pattern that doesn't exist.

## 5. Untouched By This Review

Smaller open items from Stabilization Sprint 1 (`19` §6) — git-worktree identity resolution, the
moved-repo error message, the intentional corrupt-manifest blast-radius debt — are real but were
already correctly classified as low-priority, narrow-blast-radius issues with no bearing on Phase 3+
prioritization; they remain wherever they're currently tracked (backlog or GitHub issues, per the
project's standing bug-tracking convention) and this review found no evidence to re-rank them.
