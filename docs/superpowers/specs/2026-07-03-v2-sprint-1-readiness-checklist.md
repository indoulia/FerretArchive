# Ferret V2 Sprint 1 — Implementation Readiness Checklist

**Date:** 2026-07-03
**Status:** Gate — must be reviewed before Sprint 1 begins
**Governs:** The vertical-slice sprint defined in `docs/superpowers/plans/2026-07-03-v2-sprint-1-vertical-slice.md`
**Established by:** ADR-0021 (Milestone: Ferret V2 Architecture Baseline v1 Complete)

---

## Purpose

Prevent Sprint 1 from stalling halfway through on an unresolved engineering decision. This is not an architecture document — every row below traces to a mechanism document (ARCH-032 through ARCH-036) or an ADR, and this checklist decides nothing on its own. It only states what must be true before coding starts.

**Key finding of this checklist: Sprint 1 does not need the Storage Backend or Serialization ADRs resolved.** Both can be deferred past Sprint 1 by deliberately building against ARCH-032's persistence abstraction with a disposable, ADR-0001-exempt spike implementation (a single file under a clearly-labeled non-production path), rather than the eventual production store. This is not a shortcut around governance — it is the walking-skeleton pattern, and it is explicitly what ARCH-032 §9's implementation freedom and ADR-0001's "trivial implementation choices do not need an ADR" criterion both already permit.

---

## Readiness Table

| Area | Ready for Sprint 1? | Blocking Decision (Sprint 1) | Ready for Production? | Blocking Decision (Production) | Traces to |
|---|---|---|---|---|---|
| Repository scanning | ✅ | None — reuses existing, real Connector Platform (`Ferret.ConnectorPlatform`, `FilesystemConnector`) | ✅ | None | ARCH-024 §1 |
| Parsing | ✅ | None — reuses existing, real Parser Platform | ✅ | None | ARCH-024 §2 |
| Dependency record model | ✅ | None — a plain data shape per ARCH-032 §2.1/§2.2, no technology commitment | ✅ | None (shape is fixed by ARCH-032; only its physical form is an ADR concern) | ARCH-032 §2 |
| Persistence abstraction (interface) | ✅ | None — an interface has no technology commitment | ✅ | None | ARCH-032 §1 |
| Storage backend | ✅ (spike only) | A disposable, non-production file-based store, chosen as a trivial implementation detail under ADR-0001's exemption — not a Sprint 1 ADR | ❌ | ADR required — Implementation Backlog Epic 1, Feature 1.4 | ARCH-032 §9 |
| Serialization | ✅ (spike only) | Same disposable-choice reasoning as Storage Backend | ❌ | ADR required — Epic 1, Feature 1.5 | ARCH-032 §9 |
| Retention/eviction & upgrade/versioning | N/A for Sprint 1 | A single record, written once, is sufficient to prove the flow — no eviction policy is exercised | ❌ | ADR required — Epic 1, Feature 1.6 | ARCH-032 §3, §9 |
| Corruption/unreadability detection | Partial | Sprint 1 must include at least one deliberately-corrupted-record test proving the Indeterminate path works — full detection strategy is not required | ❌ | ADR required — Epic 1, Feature 1.7 | ARCH-032 §6, §7.1 |
| Concurrency scope | ✅ | Resolved by explicit statement in the Sprint 1 plan's Global Constraints (single-process only) — satisfies ADR-0021 Rule 5 for Sprint 1's scope | ❌ | Requires a decision (ADR or new governance review, depending on what's found) before any multi-process usage ships | ADR-0021 Rule 5 |
| Retrieval (request-equivalence lookup) | ✅ | A linear/direct lookup is acceptable for one record — no index structure needed at this scale | ✅ | Key/lookup structure ADR affects performance only, not Sprint 1's correctness proof | ARCH-033 §4 |
| Comparison / resolution outcome | ✅ | Single dependency shape (source content) is sufficient to exercise Satisfied / Not-satisfied / Indeterminate | ✅ | Algorithm/data-structure ADR is a performance concern, not a Sprint 1 blocker | ARCH-033 §5, §7 |
| Chain combination (multi-artifact) | N/A for Sprint 1 | No derived-artifact dependency (shape 2) exists in a one-file slice | ➖ | Deferred to Epic 2, Feature 2.3 | ARCH-033 §1, ARCH-029 §6 |
| Deletion semantics | Not ready | The deletion-signal-production gap (ARCH-030 §2; ARCH-032 §9) is an unresolved conceptual gap, not an ADR — do not implement a deletion path in Sprint 1 | ❌ | Requires escalation per ADR-0021 Rule 6 before any implementation, not just an ADR | ARCH-030 §2 |
| Surface / CLI output | ✅ | Reuses the existing `CommandResult` pattern (ARCH-024 §7); no new command, flag, or API | ✅ | None — ARCH-034 defines no API | ARCH-034 §1, §2 |
| Benchmark extensions | Deferred | Explicitly out of scope for Sprint 1 — add V2 metrics to the existing benchmark suite after the vertical slice is proven, not before | ➖ | Existing suite (`docs/superpowers/specs/2026-06-30-benchmark-suite-spec.md`) already covers V1; V2 metrics are additive | ADR-0021 (Consequences) |

---

## Exit Criteria — Ready to Start Sprint 1

All of the following must hold before coding begins:

1. Every row marked **✅** or **N/A** above for the "Ready for Sprint 1?" column — no row may be silently skipped.
2. The Concurrency Scope statement is written into the Sprint 1 plan's Global Constraints, not left implicit (ADR-0021 Rule 5).
3. No deletion-path code is planned for Sprint 1 (the one row marked **Not ready** above).
4. The actual, current source of `FilesystemConnector`, `ParserDispatcher`, `Document`, and one existing persisted-state store (e.g. `JsonWorkspaceStore`) is read and verified before any task-level, file-and-code-exact implementation plan is written — this checklist and the milestone-level Sprint 1 plan do not themselves constitute that verification.

## Exit Criteria — Ready to Move Storage/Serialization to Production

Separate from Sprint 1 readiness — do not conflate the two:

1. ADR for Storage Backend accepted (Epic 1, Feature 1.4).
2. ADR for Serialization accepted (Epic 1, Feature 1.5).
3. ADR for Retention/Eviction accepted (Epic 1, Feature 1.6).
4. A corruption-detection mechanism chosen and ADR'd (Epic 1, Feature 1.7).
5. Sprint 1's spike store code is either replaced or explicitly marked superseded — it must never silently become the production path by default.

---

## Related

- [ADR-0021](../../adr/0021-v2-architecture-baseline-complete.md) — establishes this checklist as the required gate
- [ADR-0001](../../adr/0001-use-architecture-decision-records.md) — the triviality exemption this checklist relies on for the Sprint 1 spike store
- [ARCH-032](../../002-Architecture/ARCH-032-Persistence-Mechanism-Design.md), [ARCH-033](../../002-Architecture/ARCH-033-Dependency-Resolution-Mechanism-Design.md), [ARCH-034](../../002-Architecture/ARCH-034-Surface-Integration-Mechanism-Design.md) — mechanism documents this checklist traces every row to
- `docs/superpowers/plans/2026-07-03-v2-sprint-1-vertical-slice.md` — the plan this checklist gates
- `docs/002-Architecture/V2-IMPLEMENTATION-BACKLOG-001.md` — where every ❌ row's ADR is tracked to completion
