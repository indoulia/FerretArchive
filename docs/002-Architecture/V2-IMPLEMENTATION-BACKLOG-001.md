# V2-IMPLEMENTATION-BACKLOG-001 — Ferret V2 Implementation Backlog

| Field | Value |
|---|---|
| **Document ID** | V2-IMPLEMENTATION-BACKLOG-001 |
| **Version** | 1.0 |
| **Status** | Active |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Governing Milestone** | [ADR-0021](../adr/0021-v2-architecture-baseline-complete.md) — Ferret V2 Architecture Baseline v1 Complete |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |

---

## Purpose

Per ADR-0021, the V2 program has transitioned from architecture-primary to implementation-primary work. This backlog is the delivery-side counterpart to V2-ROADMAP-001: where that document sequenced *architecture* work by conceptual dependency, this document sequences *implementation* work by the same discipline, mapping every epic, feature, and task back to the mechanism document or ADR that governs it. No item here may be implemented without a traceable architectural basis — an item with no ARCH/ADR citation is not ready for a sprint.

This document does not redesign anything ARCH-023 through ARCH-036 already state. It is a work-tracking artifact, not architecture.

---

## Priority Legend

- **P0** — Required for Sprint 1 (the vertical-slice proof)
- **P1** — Required before any production rollout
- **P2** — Required for enterprise scale or later hardening, not before

---

## Epic 1 — Persistence Mechanism (realizes ARCH-032)

| # | Feature/Task | Priority | Traces to | ADR dependency | Target |
|---|---|---|---|---|---|
| 1.1 | Dependency record model (source-content shape, Class A only, minimal request-identity) | P0 | ARCH-032 §2.1, §2.2, §2.3 | None | Sprint 1, Milestone 3 |
| 1.2 | Persistence abstraction (`IDependencyStateStore`-shaped interface) | P0 | ARCH-032 §1 | None | Sprint 1, Milestone 4 |
| 1.3 | Spike store implementation (disposable, non-production) | P0 | ARCH-032 §9 | None — ADR-0001 triviality exemption | Sprint 1, Milestone 4 |
| 1.4 | Production storage backend | P1 | ARCH-032 §9 | **ADR required** | Sprint 2+ |
| 1.5 | Production serialization format | P1 | ARCH-032 §9 | **ADR required** | Sprint 2+ |
| 1.6 | Retention/eviction policy for superseded records | P1 | ARCH-032 §3, §9 | **ADR required** | Sprint 2–3 |
| 1.7 | Corruption/unreadability detection mechanism | P1 | ARCH-032 §6, §7.1, §9 | **ADR required** | Sprint 2–3 |
| 1.8 | Configuration/registration dependency capture (shape 4 — parser version, connector config) | P1 | ARCH-032 §2.1; ARCH-026 §3 (currently unmet for any component) | None — extends existing gap closure, not a new decision | Sprint 2+ |
| 1.9 | Deletion-representability (structural capacity only, not detection) | P2 | ARCH-032 §2.1, §7.8 | None for the structural capacity; detection itself is blocked (see Epic 2, Feature 2.4) | Sprint 3+ |

## Epic 2 — Dependency Resolution Mechanism (realizes ARCH-033)

| # | Feature/Task | Priority | Traces to | ADR dependency | Target |
|---|---|---|---|---|---|
| 2.1 | Retrieval — request-equivalence lookup (linear, single record) | P0 | ARCH-033 §4, §1 | None | Sprint 1, Milestone 5 |
| 2.2 | Comparison procedure — single dependency shape | P0 | ARCH-033 §5, §3 | None | Sprint 1, Milestone 5 |
| 2.3 | Chain combination (multi-artifact, e.g. `ContextPackage` depending on `SearchResult`) | P1 | ARCH-033 §1, §5; ARCH-029 §6 | None architecturally; needs Epic 1's shape-4 capture (1.8) first for realistic chains | Sprint 2+ |
| 2.4 | Deletion detection and handling | **Blocked** | ARCH-030 §2; ARCH-032 §9 | **Not an ADR** — unresolved conceptual gap; requires escalation per ADR-0021 Rule 6 before scheduling | Not scheduled |
| 2.5 | Key/lookup structure (index, not linear scan) | P1 | ARCH-033 §11; V2-ROADMAP-001 §5 (assigned wholly to this epic, not Epic 1) | **ADR required** | Sprint 2+ |
| 2.6 | Comparison/combination algorithm and data structure | P1 | ARCH-033 §11 | **ADR required** | Sprint 2+ |

## Epic 3 — Surface Integration (realizes ARCH-034)

| # | Feature/Task | Priority | Traces to | ADR dependency | Target |
|---|---|---|---|---|---|
| 3.1 | CLI output reuse path (existing `CommandResult` pattern, indistinguishable output) | P0 | ARCH-034 §1, §2, §4 | None | Sprint 1, Milestone 6 |
| 3.2 | Optional provenance field ("this result was reused") | P2 | ARCH-034 §2 (strictly additive), §9 | **ADR required only if pursued** — product decision, not currently triggered | Unscheduled |

## Epic 4 — Composition and Conformance (realizes ARCH-035, ARCH-036)

| # | Feature/Task | Priority | Traces to | ADR dependency | Target |
|---|---|---|---|---|---|
| 4.1 | End-to-end vertical-slice test suite validating the composed sequence | P0 | ARCH-035 §1, §2 | None | Sprint 1 |
| 4.2 | Conformance-evidence checklist wired into normal PR review | P0 | ARCH-036 §2, §3 | None — uses existing PR review, no new gate | Sprint 1 onward |
| 4.3 | Full guarantee-by-guarantee conformance trace for the production implementation | P1 | ARCH-036 §1, §2 | None | Before production rollout |

## Epic 5 — Governance Follow-Through (realizes ADR-0021)

| # | Feature/Task | Priority | Traces to | ADR dependency | Target |
|---|---|---|---|---|---|
| 5.1 | Concurrency scope statement | P0 | ADR-0021 Rule 5 | None — stated directly in the Sprint 1 plan's Global Constraints | Done (Sprint 1 plan) |
| 5.2 | Production concurrency/multi-process model | P1 | ADR-0021 Rule 5 (production side) | **ADR or new governance review**, depending on what investigation finds | Before any multi-process usage ships |
| 5.3 | RM-05 — AI Integration Architecture | **Deferred** | ADR-0021 Rule 3 | N/A — a new ARCH document, not an ADR | Triggered only when an AI-derived artifact enters the reuse path |
| 5.4 | Extend existing benchmark suite with V2 metrics (persistence time, resolution/lookup time, recomputation-avoided rate, cold/warm start) | P1 | ADR-0021 Rule 4; `docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md` | None — extension of an already-approved spec | After Sprint 1 proves the flow, before Phase VI benchmarking |
| 5.5 | RM-06 formal ARCH document | **Superseded** | ADR-0021 Rule 4 | N/A | Only if 5.4's extension surfaces a question the existing spec's register can't answer |

---

## Sprint Mapping Summary

| Sprint | Scope |
|---|---|
| **Sprint 1** | Epic 1 (1.1–1.3), Epic 2 (2.1–2.2), Epic 3 (3.1), Epic 4 (4.1–4.2), Epic 5 (5.1) — the vertical slice, per `docs/archive/superpowers/plans/2026-07-03-v2-sprint-1-vertical-slice.md` |
| **Sprint 2+** | Epic 1 (1.4–1.8), Epic 2 (2.3, 2.5–2.6), Epic 5 (5.4) — production hardening, contingent on ADRs landing |
| **Sprint 3+** | Epic 1 (1.9), Epic 4 (4.3), Epic 5 (5.2) — deferred hardening and full conformance |
| **Unscheduled / Blocked** | Epic 2 (2.4 — blocked pending escalation), Epic 3 (3.2 — no trigger yet), Epic 5 (5.3, 5.5 — deferred by ADR-0021) |

---

## Cross References

| Document | Relationship |
|---|---|
| [ADR-0021](../adr/0021-v2-architecture-baseline-complete.md) | Establishes this backlog as the delivery-side counterpart to the architecture program |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | The architecture-sequencing document this backlog parallels for implementation |
| [ARCH-032](ARCH-032-Persistence-Mechanism-Design.md) through [ARCH-036](ARCH-036-Mechanism-Validation-and-Conformance.md) | Source of every architectural citation in Epics 1–4 |
| `docs/archive/superpowers/specs/2026-07-03-v2-sprint-1-readiness-checklist.md` | The gate Sprint 1's scope (P0 items above) must pass |
| `docs/archive/superpowers/plans/2026-07-03-v2-sprint-1-vertical-slice.md` | The milestone plan realizing Sprint 1's P0 items |
| `docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md` | The existing benchmark suite Feature 5.4 extends |
| `docs/adr/README.md` | Where every "ADR required" item in this backlog will be recorded once decided |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial implementation backlog, mapping ARCH-032 through ARCH-036 and ADR-0021 to Epics 1–5. |
