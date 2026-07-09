# FEP-004-SPEC-F10.2.1 — Health Report Generation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F10.2.1 |
| **Capability** | [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) |
| **Epic** | E10.2 — Health Reporting & Distinction |
| **Feature** | F10.2.1 — Health Report Generation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-10 — Observability & Health](../epics/FEP-003-EPIC-CAP-10-Observability-Health.md) · [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

An aggregated state picture is only useful to an operator or consumer once it can be inspected as a discrete, point-in-time statement. Health Report Generation exists to generate a Health Report from the currently aggregated state, giving operators and consumers a way to evaluate whether the product's own stated goals are actually being met in practice (FEP-003-EPIC-CAP-10 §3, F10.2.1 Objective and Product Outcome; FEP-002-CAP-10 §2, Responsibilities).

## 3. Scope

- Generating a Health Report — a point-in-time, inspectable representation — from the currently aggregated whole-system state (F10.1.2).
- Ensuring a generated Health Report accurately reflects the underlying aggregated state at the moment of its generation.
- Making the generated Health Report the artifact by which operators and consumers can evaluate whether Product Goals G1–G6 are being met in practice, without the report itself asserting a conclusion about whether they are.

## 4. Out of Scope

- Collecting individual Health Signals (F10.1.1) or aggregating them into a whole-system picture (F10.1.2) — this Feature consumes the aggregated state, it does not produce it.
- Classifying whether a signal within a generated report represents genuine failure or an expected, policy-driven gap — that is F10.2.2, though this Feature's report must preserve the information that classification needs.
- Routing a generated report to an external observability sink — that is F10.3.1.
- Taking corrective or remedial action based on report contents — always out of scope for this capability.
- Determining, amending, or enforcing what the product's own goals should be — a Health Report evidences fulfillment of Goals G1–G6 as defined in FEP-001; it does not define or alter them.

## 5. Engineering Requirements

1. A Health Report must be generatable on demand from the currently aggregated whole-system state.
2. A generated Health Report must accurately reflect the aggregated state exactly as it existed at the moment of generation — never a state materially before or after that moment.
3. A Health Report must preserve the per-capability attribution inherited from the aggregated state it is generated from.
4. A Health Report must be structured so it can serve as evidence for whether each of Product Goals G1–G6 is being met, without itself asserting a conclusion about whether they are met.
5. Generating a Health Report must not alter, suppress, or reinterpret the aggregated state it draws from.
6. It must be possible to determine, from a generated Health Report, which capabilities contributed a signal and which did not, as of the report's generation time.

## 6. Inputs

- The currently aggregated whole-system state picture (F10.1.2).

## 7. Outputs

- A generated Health Report: an inspectable, point-in-time representation of system state suitable for operator or consumer evaluation (FEP-002-CAP-10 §6, Health Report; §5, Outputs).

## 8. Preconditions

- F10.1.2's aggregated state must be producible before a Health Report can be generated from it.

## 9. Postconditions

- A Health Report exists that accurately reflects the aggregated state at its generation time.
- An operator or consumer can inspect the generated report to evaluate the product's fulfillment of its own goals without needing to interrogate any individual capability directly.

## 10. Dependencies

**Capability dependencies.** None beyond Observability & Health itself.

**Epic dependencies.** E10.1 (State Collection) — per Execution Order (FEP-003-EPIC-CAP-10 §5), E10.2 depends on collection (and, transitively, aggregation) already existing.

**Feature dependencies.** F10.1.2 (Cross-Capability State Aggregation) — per the E10.2 Features table, F10.2.1 depends directly on F10.1.2.

**External dependencies.** None directly. Observability sinks (FEP-001 §6) are not consumed here — a generated report is the artifact F10.3.1 later makes available for optional external routing; this Feature does not itself route anything.

## 11. Constraints

**Business constraints.** Report accessibility must not be gated behind the same access model that gates content access, in a way that would leave operators unable to diagnose problems (FEP-002-CAP-10 §8, Business).

**Product constraints.** A generated Health Report must never present a degraded capability as healthy for the sake of a simpler status signal (FEP-002-CAP-10 §8, Product) — this is the central constraint on generation.

**Context integrity constraints.** A generated report must preserve, from the aggregated state it draws from, the information needed to distinguish real failure from an expected, policy-driven gap (FEP-002-CAP-10 §8, Context integrity); the classification itself is F10.2.2's responsibility, but generation must not flatten that distinction out of the report.

**Trust constraints.** Per Product Principle P5 (Degrade by scope, not by silent omission), a report generated from a partial aggregated state must show that it is partial, never present partial coverage as complete.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), report generation reports on other capabilities' state; it does not adjudicate, alter, or act on their behavior.

## 12. Acceptance Criteria

1. A Health Report can be generated on demand and reflects the aggregated state current as of the moment of generation.
2. A generated report attributes each represented signal to its originating capability.
3. A capability absent from the aggregated state at generation time is observable as absent in the generated report, never silently omitted.
4. Two reports generated at different times, following a change in the underlying aggregated state, differ in a way traceable to that change.
5. A generated report never states a degraded capability as healthy.

## 13. Validation Requirements

- That report generation accurately mirrors the aggregated state at generation time, with no drift or unacknowledged staleness.
- That per-capability attribution and per-capability absence are both preserved into the generated report.
- That report content never launders a degraded underlying state into an apparently healthy one.
- That report generation is independently repeatable, and repeated generations reflect any intervening state changes.

## 14. Failure Conditions

- **False health** (FEP-002-CAP-10 §10, Failure Modes) — a report states health despite underlying degradation, because the underlying self-reporting fed into the aggregate did not reflect actual behavior. Expected behavior at this Feature's boundary: generation must faithfully carry forward whatever the aggregated state indicates, without adding false confidence of its own; if the aggregated state shows degradation, the report must show it.
- **Report generated from stale or incomplete aggregated state, presented as complete.** Expected behavior: the report must reflect exactly what was aggregated at generation time and must expose any incompleteness present in that aggregate, never paper over it.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G1 (Completeness of context), G4 (Trustworthy context — a report is how trust in the product's own goal-fulfillment is evidenced) → Product Principles P2 (Provenance is mandatory, not optional — a report should carry forward attribution), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-10 (Observability & Health) → Epic E10.2 (Health Reporting & Distinction) → Feature F10.2.1 (Health Report Generation).

## 16. Future Considerations

- Increasingly rich health reporting as the capability model grows, covering more source types and more consumer types (FEP-002-CAP-10 §11).
- Historical health trend analysis, deferred and explicitly bounded so it remains reporting rather than reasoning about sources, would extend report generation to include trend data over time (FEP-003-EPIC-CAP-10 §8, Deferred Work; FEP-002-CAP-10 §11).
