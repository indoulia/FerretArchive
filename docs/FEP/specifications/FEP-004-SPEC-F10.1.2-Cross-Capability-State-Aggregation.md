# FEP-004-SPEC-F10.1.2 — Cross-Capability State Aggregation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F10.1.2 |
| **Capability** | [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) |
| **Epic** | E10.1 — State Collection |
| **Feature** | F10.1.2 — Cross-Capability State Aggregation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-10 — Observability & Health](../epics/FEP-003-EPIC-CAP-10-Observability-Health.md) · [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Collected Health Signals scattered across every other capability are of limited use if an operator must still check each capability separately. Cross-Capability State Aggregation exists to combine individually collected signals into a coherent, whole-system state picture, enabling a single point of inspection (FEP-003-EPIC-CAP-10 §3, F10.1.2 Objective and Product Outcome).

## 3. Scope

- Combining the Health Signals collected under F10.1.1 into a single, coherent, whole-system state picture.
- Ensuring the aggregated picture can be produced from currently collected signals at any time it is requested.
- Preserving each signal's originating-capability attribution within the aggregated picture, so aggregation does not collapse per-capability visibility into an undifferentiated whole.
- Supporting aggregation scoped to a single workspace as well as to the whole product, consistent with the Health Report context object's workspace/whole-product framing (FEP-002-CAP-10 §6).

## 4. Out of Scope

- Collecting the individual Health Signals themselves — that is F10.1.1, a precondition of this Feature.
- Generating the formatted Health Report artifact for operator or consumer consumption — that is F10.2.1, a Feature this one feeds.
- Classifying any aggregated signal as genuine failure versus expected, policy-driven gap — that is F10.2.2.
- Routing the aggregated picture or any downstream report externally — that is F10.3.1.
- Taking corrective action based on the aggregated picture — always out of scope for this capability.
- Becoming a store of context substance — the aggregated picture is state about capabilities, never the context content those capabilities handle.

## 5. Engineering Requirements

1. A whole-system state picture must be producible by combining all currently collected Health Signals at the time it is requested.
2. Each signal's originating capability must remain individually attributable within the aggregated picture.
3. The aggregated picture must reflect currently collected signals only, never a stale or cached combination silently presented as current.
4. Aggregation must support production scoped to a single workspace and scoped to the whole product, wherever signals carry workspace attribution.
5. A capability with no collected signal (a blind spot) must appear in the aggregated picture as an observable absence, never omitted silently from the picture.
6. Aggregation must operate only over already-collected signals (F10.1.1); it must never require any individual capability to be interrogated directly at aggregation time.

## 6. Inputs

- Health Signals collected via F10.1.1, across every reporting-producing capability.

## 7. Outputs

- An aggregated, whole-system state picture combining currently collected Health Signals, attributable per capability and, where applicable, per workspace.

## 8. Preconditions

- F10.1.1 must already be collecting signals from at least the capabilities currently expected to report before an aggregated picture can be considered representative (FEP-003-EPIC-CAP-10 §6, Functional completeness gate).

## 9. Postconditions

- A whole-system state picture can be produced at any time, reflecting the currently collected signals.
- A capability's absence from collection is observable as a gap in the aggregated picture rather than a silent omission.

## 10. Dependencies

**Capability dependencies.** None beyond Observability & Health itself; this Feature consumes F10.1.1's output and does not itself depend on any other capability functioning beyond what F10.1.1 already requires.

**Epic dependencies.** Internal to E10.1 (State Collection) — per Execution Order (FEP-003-EPIC-CAP-10 §5), E10.1 as a whole must exist before anything can be reported, and aggregation is the second step within it.

**Feature dependencies.** F10.1.1 (Health Signal Collection) — per the E10.1 Features table, F10.1.2 depends directly on F10.1.1.

**External dependencies.** None directly. This Feature aggregates only already-collected signals; it does not itself read from source systems, identity & access systems, or observability sinks (FEP-001 §6).

## 11. Constraints

**Business constraints.** Operational visibility in the form of the aggregated picture must remain accessible to operators independent of content-access gating (FEP-002-CAP-10 §8, Business).

**Product constraints.** Aggregation must not present a degraded capability as healthy for the sake of a simpler combined picture (FEP-002-CAP-10 §8, Product); combining signals must preserve degradation, not launder it away.

**Context integrity constraints.** Aggregation must preserve, from the underlying signals into the combined picture, the information needed to later distinguish "capability unhealthy" from "capability healthy but reporting an expected, policy-driven gap" (FEP-002-CAP-10 §8, Context integrity), even though the classification itself belongs to F10.2.2.

**Trust constraints.** Per Product Principle P5 (Degrade by scope, not by silent omission), a capability missing from collection must appear as an observable gap in the aggregate, never silently absorbed into an apparently complete picture.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), aggregation combines already-collected signals; it must never reach into another capability to interrogate it directly at aggregation time.

## 12. Acceptance Criteria

1. Requesting the aggregated state at any time produces a picture reflecting all currently collected Health Signals.
2. Each capability's contribution to the aggregate is individually attributable within it.
3. A capability absent from collection appears as an observable gap in the aggregate, never omitted without trace.
4. The aggregate can be scoped to a single workspace as well as produced for the whole product.
5. Producing the aggregate never requires querying any capability directly at aggregation time.

## 13. Validation Requirements

- That the aggregate reflects all currently collected signals with no silent drops.
- That per-capability attribution survives aggregation.
- That an uncollected capability's absence is detectable within the aggregate.
- That workspace-scoped and whole-product-scoped aggregation each produce internally consistent pictures.

## 14. Failure Conditions

- **Blind spots** (FEP-002-CAP-10 §10, Failure Modes) propagating into the aggregate as false completeness. Expected behavior: an aggregate built from partial collection must expose which capabilities are represented and which are not, never present itself as complete when it is partial.
- **Observability as a bottleneck** (FEP-002-CAP-10 §10) — aggregation implemented such that it blocks or is blocked by another capability's operation. Expected behavior: aggregation must be producible independent of any capability's own operation, per FEP-002-CAP-10 §3 (Non-Responsibilities).

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G1 (Completeness of context — the aggregate must reflect every collected signal, not a subset), G6 (Operable at repository scale and beyond — a single point of inspection is what makes operability at scale practical) → Product Principles P5 (Degrade by scope, not by silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-10 (Observability & Health) → Epic E10.1 (State Collection) → Feature F10.1.2 (Cross-Capability State Aggregation).

## 16. Future Considerations

- Historical health trend analysis, deferred and explicitly bounded to avoid becoming reasoning about sources rather than reporting on them, would build on top of the aggregated state this Feature produces (FEP-003-EPIC-CAP-10 §8, Deferred Work; FEP-002-CAP-10 §11).
- Federation-scoped health aggregation across federated workspaces, aggregating without losing per-workspace attribution, is deferred until Federation matures (FEP-003-EPIC-CAP-10 §8; FEP-002-CAP-10 §11).
