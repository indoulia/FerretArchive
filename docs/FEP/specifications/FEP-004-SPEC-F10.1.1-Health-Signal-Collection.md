# FEP-004-SPEC-F10.1.1 — Health Signal Collection

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F10.1.1 |
| **Capability** | [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) |
| **Epic** | E10.1 — State Collection |
| **Feature** | F10.1.1 — Health Signal Collection |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-10 — Observability & Health](../epics/FEP-003-EPIC-CAP-10-Observability-Health.md) · [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

No other capability's state can be reported on until it is collected. Health Signal Collection exists to define and collect the health signal each other capability already reports about itself, providing the raw material every later Observability & Health Feature depends on (FEP-003-EPIC-CAP-10 §3, F10.1.1 Objective and Product Outcome).

## 3. Scope

- Defining what constitutes a Health Signal: a conceptual unit of state a capability reports about itself (FEP-002-CAP-10 §6, Context Objects).
- Collecting the already-defined reporting outputs from every other capability that produces one: coverage and gap reporting from Context Acquisition, freshness state from Context Maintenance, assembly gap reporting from Context Assembly, delivery outcome reporting from Context Delivery, provenance completeness reporting from Provenance & Attribution, and decision recording from Access Control & Policy.
- Ensuring a Health Signal is collectible without Observability needing to interrogate any other capability's internal state beyond what that capability's own defined reporting output already exposes.

## 4. Out of Scope

- Aggregating collected signals into a whole-system state picture — that is F10.1.2, a Feature this one feeds.
- Generating a Health Report from aggregated state — that is F10.2.1.
- Classifying a collected signal as genuine failure versus expected, policy-driven gap — that is F10.2.2.
- Routing collected signals to an external observability sink — that is F10.3.1.
- Producing the reporting outputs this Feature collects — each is owned by its originating capability's own Feature (e.g., Coverage & Gap Reporting, Freshness State Tracking, Assembly Gap Reporting, delivery outcome reporting, Provenance Completeness Reporting, Decision Recording & Audit Surfacing), not by this Feature.
- Taking any corrective or remedial action based on what a collected signal indicates — always out of scope for this capability (FEP-002-CAP-10 §3, Non-Responsibilities); this Feature reports, it never remediates.
- Becoming a second store of context substance — collection concerns state signals about capabilities, never the context content those capabilities acquire, organize, or deliver.

## 5. Engineering Requirements

1. A Health Signal must be definable as a conceptual unit of state that a capability reports about itself, independent of the reporting capability's internal implementation.
2. Every other reporting-producing capability's already-defined reporting output must have a corresponding, collectible Health Signal here.
3. Collection must not require Observability to inspect any other capability's internal state beyond what that capability's own defined reporting output exposes.
4. A capability's absence of a reported Health Signal must be distinguishable from that capability reporting a clean, healthy state — an unreported signal is never equivalent to a healthy one.
5. Collection must not alter, filter, or reinterpret the substance of a reporting output as it is collected.
6. Collection must occur without requiring any reporting capability to be blocked on, or made to wait for, Observability, per FEP-001 §4 and FEP-002-CAP-10 §3 (Non-Responsibilities).

## 6. Inputs

- Reporting outputs already defined by other capabilities' own Features: coverage and gap reporting (Context Acquisition), freshness state tracking (Context Maintenance), assembly gap reporting (Context Assembly), delivery outcome reporting (Context Delivery), provenance completeness reporting (Provenance & Attribution), and decision recording and audit surfacing (Access Control & Policy) (FEP-002-CAP-10 §4, Inputs).

## 7. Outputs

- A collected set of Health Signals, one per reporting capability, available for aggregation (FEP-002-CAP-10 §6, Health Signal).

## 8. Preconditions

- Each contributing capability's own reporting-producing Feature must already exist and be actively producing its defined reporting output (FEP-003-EPIC-CAP-10 §4, Prerequisite Features and Epics).

## 9. Postconditions

- Every other reporting-producing capability's defined reporting output has a corresponding Health Signal collected here and available for aggregation.
- A capability that has not reported is observably distinct, within collected state, from one reporting a clean state.

## 10. Dependencies

**Capability dependencies.** Depends on every reporting-producing capability's own output existing: Context Acquisition, Context Maintenance, Context Assembly, Context Delivery, Provenance & Attribution, and Access Control & Policy. None of those capabilities depend functionally on this Feature (FEP-001 §4).

**Epic dependencies.** Per Global cross-capability Epic dependencies (FEP-003 Global Output 3), E10.1 depends on reporting outputs from E02.3, E04.2, E05.3, E06.1/E06.3, E07.3, and E08.3.

**Feature dependencies.** F02.3.2 (Coverage & Gap Reporting), F04.2.1 (Freshness State Tracking), F05.3.2 (Assembly Gap Reporting), F06.1.1/F06.1.2/F06.3.1/F06.3.2 (delivery outcome reporting), F07.3.1 (Provenance Completeness Reporting), F08.3.1 (Decision Recording & Audit Surfacing) — per the E10.1 Features table, F10.1.1 depends directly on all of these.

**External dependencies.** None directly. This Feature does not itself read from source systems, identity & access systems, or observability sinks (FEP-001 §6) — it collects only what other capabilities have already defined as their reporting output.

## 11. Constraints

**Business constraints.** Observability must never be gated behind the same access model that gates content in a way that leaves operators unable to diagnose problems (FEP-002-CAP-10 §8, Business).

**Product constraints.** Health reporting must be honest about degradation (FEP-002-CAP-10 §8, Product); collection must not filter out or soften a negative signal on its way into the collected set.

**Context integrity constraints.** Distinguishing "this capability is unhealthy" from "this capability is healthy but reporting an expected, policy-driven gap" (FEP-002-CAP-10 §8, Context integrity) depends on collection preserving enough fidelity for that later distinction to be possible — even though the classification itself belongs to F10.2.2.

**Trust constraints.** Per Product Principle P5 (Degrade by scope, not by silent omission), an uncollected signal must be observable as a gap in collected state, never silently treated as equivalent to a healthy signal.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), collection reads only what other capabilities already define as their reporting output; it must never reach into another capability's internals in a way that blurs that capability's ownership of its own reporting.

## 12. Acceptance Criteria

1. For each reporting-producing capability, a corresponding Health Signal is collected from its already-defined reporting output.
2. A capability that fails to produce its defined reporting output results in an observably absent Health Signal, distinguishable from a signal reporting a healthy state.
3. The content of a collected Health Signal matches the substance of the originating reporting output without alteration.
4. Collection of a Health Signal never requires the reporting capability to pause, block, or wait on Observability.

## 13. Validation Requirements

- That every reporting-producing capability's defined reporting output yields a collectible Health Signal.
- That an absent signal is verifiably distinct from a signal reporting a healthy state.
- That collected signal content is unaltered relative to its source reporting output.
- That collection never introduces a functional dependency of any reporting capability upon Observability.

## 14. Failure Conditions

- **Blind spots** (FEP-002-CAP-10 §10, Failure Modes) — a capability fails to report, leaving a gap in collected state. Expected behavior: the gap must be observable as "unknown / not reporting," never silently rendered as "healthy."
- **False health** (FEP-002-CAP-10 §10) — a capability's own reporting output states health despite degraded behavior. Expected behavior: collection must preserve whatever fidelity the originating reporting output carries and must not itself introduce any additional obscuring of that signal's accuracy; adjudicating truthfulness beyond that is out of this Feature's scope.

## 15. Traceability

Product Vision (Mission: infrastructure that continuously acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G1 (Completeness of context — every other capability's reporting output must be collected, not just some), G4 (Trustworthy context — collected raw material underlies every later trust evaluation) → Product Principles P5 (Degrade by scope, not by silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-10 (Observability & Health) → Epic E10.1 (State Collection) → Feature F10.1.1 (Health Signal Collection).

## 16. Future Considerations

- Increasingly rich health reporting as the capability model grows — more source types and more consumer types will widen the set of reporting outputs this Feature must be able to collect from (FEP-002-CAP-10 §11).
- Historical health trends becoming a context object in their own right, bounded carefully so this remains reporting rather than Ferret reasoning about its sources (FEP-002-CAP-10 §11; FEP-003-EPIC-CAP-10 §8, Deferred Work).
