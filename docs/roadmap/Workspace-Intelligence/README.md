# Workspace Intelligence Platform — Roadmap Index

Milestone: Ferret v2.0, first milestone after Dogfooding. Full context: `00-Vision.md`.

## Reading Order

| # | Doc | Answers |
|---|---|---|
| 00 | Vision | What / Why |
| 01 | Architecture | How (system-level) |
| 02 | Workspace Model | How (data model) |
| 03 | Cross-Workspace References | How (the "no duplication" requirement) |
| 04 | Knowledge Graph | How (schema additions) |
| 05 | Context Optimization | How (the differentiator) |
| 06 | Incremental Indexing | How (stays fast as things change) |
| 07 | Caching | How (stays fast under load) |
| 08 | Telemetry | How (observability) |
| 09 | Analytics | How (what's measured) |
| 10 | Usage Ledger | How (event storage) |
| 11 | Dashboard | How (what's shown) |
| 12 | API | How (surface area) |
| 13 | Storage | How / decision gate |
| 14 | Migration | How existing users are unaffected |
| 15 | Execution Plan | In what order |
| 16 | Vertical Slice Validation | Did the architecture hold — evidence, not opinion |
| 17 | Founder Dogfooding Sprint 1 | Real-repo dogfooding findings, friction log, stabilization recommendation |
| 18 | Engineering Analysis: Sprint 1 | Classified findings, architecture verdicts, ranked stabilization plan |
| 19 | Stabilization Sprint 1 | Implementation, real failure-injection evidence, reliability verdict |
| 20 | Phase 3 Priority Assessment | What order (Phase 3 is analysis-only so far — no implementation) |
| 21 | P3-001: Fingerprint Optimization | First Phase 3 implementation — the fingerprint re-hash cost fix, with benchmark evidence |
| 22 | WIP-032: Registry Read-Through Cache | Phase 3 implementation — registry resolve cache, with benchmark evidence |
| 23 | WIP-030/031: Federated Query Cache | Phase 3 implementation — federated query cache, with benchmark evidence |
| 24 | WIP-033: Scope Classifier Discovery | Feasibility investigation only — not yet implemented |
| 25 | Multi-Workspace Dogfooding Sprint | Real-scale (R=26) evidence for WIP-033 and WIP-030/031 |
| 26 | P3-002: Query Cache Regression | Root-cause and fix for a regression `25` found in WIP-030/031 |
| 27 | Phase 3+ Roadmap Revision | Evidence-based re-prioritization of everything from WIP-033 onward |
| 28 | Phase 3+ Roadmap Adversarial Review | Ground-truth check of `27` against live `git`/`gh` state — two corrections |
| 29 | Ferret v2 Release Master Plan | The executable plan from today to the v2.0 release tag |
| 30 | Epic 5 — Ferret v2 Release Execution | `29` restated as implementation stories with acceptance criteria, gates, and exit criteria — the canonical execution document from here to the release tag |

`ADR/` — the four ADRs; only ADR-0029 (Phase 5, optional for v2.0) still gates anything. `Backlog/` — the ordered ticket list, including the recommended Phase 1 vertical slice. `../Future/Deferred-Scope.md` — what's explicitly cut from v1 and why.

## Every Open Decision, In One Place

**Updated 2026-07-05 (implementation-readiness review):** ADR-0028 downgraded from a Founder gate to an implementation detail — see that ADR for why. **Updated 2026-07-05 (ADR-0026 finalization review):** ADR-0026 itself is now fully specified (identity rules, atomicity/failure handling, sharing-compatibility) — the only thing left is the Founder's sign-off, not any remaining design work. **Updated 2026-07-06 (T10, `30-Epic-5-Ferret-v2-Release-Execution.md`):** ADR-0026 accepted by the Founder as specified — Phase 0 gate closed.

| Decision | Where | Status |
|---|---|---|
| Workspace registry model (identity-based local registry) | ADR-0026 | **Accepted** (Founder, 2026-07-06) — as specified, no override |
| v1 sharing/permission scope (4 roles, recommended) | ADR-0029 | **Requires Founder decision — optional for v2.0, deliberately left open for v2.1; blocks Phase 5 start only** |
| Usage ledger raw-event retention (90 days) | ADR-0028 | Accepted (default) — ships as configurable, no Founder sign-off needed to proceed |
| Reference resolution strategy (live federation, not copy) | ADR-0027 | Accepted — non-negotiable requirement, not a founder choice point |
| Everything else in 00–15 | — | Ready for implementation |

**ADR-0026 is closed — nothing is blocked on Phase 0 anymore. ADR-0029 remains open by choice, blocking Phase 5 start only, not Phase 1 or v2.0.** See `15-Execution-Plan.md` §5.

## What's Deliberately Not Here

Full enterprise RBAC, org-wide analytics/billing, Ferret Hub/cloud sync, and 100K-repository scale work are cut from this milestone. See `../Future/Deferred-Scope.md` for what and why — each entry names the specific FUTURE-002 open question or V3 deferral it's consistent with, so cutting it now isn't a new decision, it's honoring one already made.
