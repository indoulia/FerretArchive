# FEP-004-SPEC-F04.3.1 — Re-acquisition & Re-organization Triggering

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F04.3.1 |
| **Capability** | [Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Epic** | E04.3 — Re-processing Orchestration & Invalidation |
| **Feature** | F04.3.1 — Re-acquisition & Re-organization Triggering |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-04 — Context Maintenance](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md); [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists to define how Context Acquisition and Context Organization are triggered when change is detected, so that the pipeline stays current without manual re-processing — this Feature's stated Objective and Product Outcome.

## 3. Scope

- Recognizing that a consumed Change Signal (source-level, from F04.1.1; structural or scope, from F04.1.2) warrants re-processing.
- Issuing a trigger to Context Acquisition to re-read the specific affected source.
- Issuing a trigger to Context Organization to re-derive structure for the specific affected material once it has been re-acquired.
- Scoping triggers to the specific affected source or Acquisition Unit implicated by the underlying signal.
- Ensuring a detected, relevant change reliably results in the affected Acquisition Unit and its derived structure being re-processed.

## 4. Out of Scope

- Performing the re-reading of a source (Context Acquisition's responsibility; Context Maintenance must never itself re-read a source).
- Performing the re-derivation of structure (Context Organization's responsibility; Context Maintenance must never itself re-derive structure).
- Detecting the underlying change itself (E04.1).
- Tracking freshness state or age (E04.2).
- Invalidating context that is no longer valid (F04.3.2).
- Deciding what becomes eligible for delivery to a consumer (Context Assembly's responsibility).

## 5. Engineering Requirements

1. A consumed Change Signal that indicates re-processing is warranted must result in a trigger issued to Context Acquisition for the specific affected source.
2. A trigger to Context Organization for re-derivation must follow once the corresponding re-acquisition has produced updated material.
3. Triggers must be scoped to the specific affected source or Acquisition Unit implicated by the underlying signal, rather than defaulting to a workspace-wide re-processing sweep, unless the signal itself is workspace-scoped.
4. Every relevant detected change must reliably result in a corresponding trigger; no relevant change may be left untriggered.
5. Triggering activity must be reportable to Observability & Health, including trigger volume and outcome.
6. This Feature must remain functionally distinct from Context Acquisition and Context Organization — it issues triggers, it does not perform the work those triggers request.

## 6. Inputs

- Consumed Change Signals (from F04.1.1 and F04.1.2) identified as warranting re-processing.

## 7. Outputs

- A re-acquisition trigger directed at Context Acquisition, scoped to the affected source.
- A re-organization trigger directed at Context Organization, scoped to the affected derived material.

## 8. Preconditions

- Source Change Detection (F04.1.1) and Structural & Scope Change Consumption (F04.1.2) exist and supply consumable signals.
- Context Acquisition and Context Organization exist as capabilities that can be triggered.

## 9. Postconditions

- The affected Acquisition Unit is re-processed by Context Acquisition following every relevant detected change.
- The corresponding derived structure is re-processed by Context Organization following successful re-acquisition.
- The pipeline's context reflects the underlying change without requiring manual intervention.

## 10. Dependencies

**Capability dependencies.** Context Acquisition (target of the re-acquisition trigger); Context Organization (target of the re-organization trigger).

**Epic dependencies.** E04.1 — Change Detection; E04.2 — Freshness Accounting; E01.2 — Scope Declaration & Configuration.

**Feature dependencies.** F04.1.1 — Source Change Detection; F04.1.2 — Structural & Scope Change Consumption.

**External dependencies.** Source systems (the category ultimately re-read as a result of this Feature's trigger, via Context Acquisition).

## 11. Constraints

**Business constraints.** None specific to this Feature beyond the urgency already implied by a workspace's freshness expectation (resolved elsewhere, in F04.2.2).

**Product constraints.** Triggering must not become excessive; unbounded triggering starves the pipeline and must be avoided or, where it occurs, made visible (P5).

**Context integrity constraints.** Triggers must target the correct, specific Acquisition Unit or derived structure; a mistargeted trigger would refresh the wrong context or leave the actually-affected context unrefreshed.

**Trust constraints.** Triggered re-processing must be attributable to the Change Signal that caused it, preserving lineage (P2).

**Policy constraints.** None.

## 12. Acceptance Criteria

1. Every consumed Change Signal that warrants re-processing results in a re-acquisition trigger to Context Acquisition for the specific affected source.
2. Every successful re-acquisition resulting from such a trigger is followed by a re-organization trigger to Context Organization for the affected material.
3. No Change Signal that warrants re-processing is left without a corresponding trigger.
4. Triggering activity is reportable and inspectable via Observability & Health.
5. Triggers scoped to a specific source or unit do not expand into workspace-wide re-processing beyond what the underlying signal justified.

## 13. Validation Requirements

- That trigger issuance is reliable against a representative set of detected changes, with no missed triggers.
- That trigger scoping is correct — neither over-broad nor under-scoped relative to the originating signal.
- That this Feature does not itself perform acquisition or organization work, only issues triggers for others to perform it.
- That trigger volume is reportable in a form suitable for detecting change-storm conditions.

## 14. Failure Conditions

- **Change storms.** Overly sensitive detection triggers excessive re-acquisition or re-organization, starving the pipeline. Expected behavior: trigger volume must be observable so the condition is visible, rather than silently degrading pipeline throughput (P5).
- **Silent staleness (missed-trigger variant).** A warranted trigger is missed. Expected behavior: the miss must eventually be detectable — for example, via freshness state remaining stale despite an apparent processing cycle — never permanently silent.

## 15. Traceability

Product Vision (Mission: maintain context) → Goals G2 (Currency of context), G6 (Operable at repository scale and beyond) → Product Principles P1 (Context over computation — triggering exists to keep context accurate, not to compute conclusions), P3 (Freshness first-class), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-04 (Context Maintenance) → Epic E04.3 (Re-processing Orchestration & Invalidation) → Feature F04.3.1 (Re-acquisition & Re-organization Triggering).

## 16. Future Considerations

- Reconciling change-signal granularity with Acquisition's and Organization's own units of work, as identified as a risk in the epic, may refine how finely triggers are scoped.
- Predictive triggering ahead of confirmed change signals is deferred pending real change-pattern data, and would extend this Feature's triggering logic without changing its current, reactive scope.
