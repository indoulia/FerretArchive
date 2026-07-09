# FEP-004-SPEC-F04.1.2 — Structural & Scope Change Consumption

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F04.1.2 |
| **Capability** | [Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Epic** | E04.1 — Change Detection |
| **Feature** | F04.1.2 — Structural & Scope Change Consumption |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-04 — Context Maintenance](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md); [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists to define how Context Maintenance consumes structural change signals from Context Organization and scope change signals from Workspace Definition, so that freshness judgments reflect source-level change and structure-level change alike, and so that scope changes are never missed — the Feature's stated Objective and Product Outcome.

## 3. Scope

- Receiving structural change signals emitted by Context Organization (F03.3.1) that indicate newly organized material has changed.
- Receiving scope change signals emitted by Workspace Definition (F01.2.3) that indicate declared scope has changed.
- Correlating each received signal to the context unit(s) or scope it affects, where that correlation is knowable.
- Distinguishing structural change signals from scope change signals, since each carries a different implication for freshness or eligibility.
- Ensuring every received signal is reflected, without loss, in downstream judgment inputs used by Freshness State Tracking and Re-processing Orchestration.

## 4. Out of Scope

- Emitting structural change signals (Context Organization's responsibility, F03.3.1).
- Emitting scope change signals (Workspace Definition's responsibility, F01.2.3).
- Detecting source-level change (F04.1.1).
- Updating or maintaining freshness state itself (F04.2.1).
- Triggering re-acquisition, re-organization, or invalidation (E04.3).
- Re-deriving structure or re-reading a source (explicitly outside Context Maintenance's non-responsibilities).

## 5. Engineering Requirements

1. The capability must be able to receive a structural change signal from Context Organization identifying that newly organized material has changed.
2. The capability must be able to receive a scope change signal from Workspace Definition identifying that declared scope has changed, including addition, removal, or alteration of scope.
3. Every received structural or scope change signal must result in a corresponding, observable update to the affected context's freshness or eligibility judgment inputs.
4. Structural change signals must remain distinguishable from scope change signals throughout consumption, since they carry different downstream implications.
5. No structural or scope change signal may be dropped, incorrectly deduplicated, or missed due to timing relative to other Context Maintenance activity.

## 6. Inputs

- Structural change signals, indicating that organized material has changed.
- Scope change signals, indicating that a workspace's declared scope has changed.

## 7. Outputs

- A consumed, correlated signal identifying the affected context unit(s) or scope, made available to Freshness State Tracking (F04.2.1) and to Re-processing Orchestration & Invalidation (E04.3).

## 8. Preconditions

- Context Organization is capable of detecting and signaling structural change (F03.3.1).
- Workspace Definition is capable of propagating scope changes (F01.2.3).

## 9. Postconditions

- Every structural change signal emitted by Context Organization results in a corresponding update reflected in Context Maintenance's freshness judgment inputs.
- Every scope change signal emitted by Workspace Definition results in a corresponding update reflected in Context Maintenance's eligibility judgment inputs.
- No signal emitted upstream is left unreflected in Context Maintenance's state.

## 10. Dependencies

**Capability dependencies.** Context Organization (source of structural change signals); Workspace Definition (source of scope change signals).

**Epic dependencies.** E03.3 — Structural Change Signaling; E01.2 — Scope Declaration & Configuration.

**Feature dependencies.** F03.3.1 — Structural Change Detection & Signaling; F01.2.3 — Scope Change Propagation.

**External dependencies.** None directly; this Feature consumes signals produced by other Ferret capabilities rather than interacting with external systems itself.

## 11. Constraints

**Business constraints.** None beyond honoring the workspace's own declared scope as the authority for what "in scope" means.

**Product constraints.** Scope changes must never be missed — a signal received and not reflected is a direct violation of this Feature's Product Outcome and of P3.

**Context integrity constraints.** Structural and scope signals must propagate completely into judgment inputs; partial consumption would understate the change that actually occurred (P2).

**Trust constraints.** The correlation of a signal to the context unit(s) or scope it affects must be accurate; a misattributed signal would misrepresent freshness or eligibility to every consumer equally, violating P4 (no privileged consumer receives a differently corrected picture).

**Policy constraints.** None beyond what Workspace Definition and Context Organization already govern for their own signals.

## 12. Acceptance Criteria

1. Every structural change signal emitted by Context Organization produces a corresponding, observable update in this Feature's output.
2. Every scope change signal emitted by Workspace Definition produces a corresponding, observable update in this Feature's output.
3. Structural and scope-derived updates remain distinguishable from one another in the resulting output.
4. No structural or scope change signal observed at its source capability is absent from this Feature's consumed output.

## 13. Validation Requirements

- That signal-to-judgment-input reflection is complete under rapid or concurrent signal arrival.
- That structural and scope signal types are preserved and distinguishable end to end.
- That a scope-removal signal is preserved with sufficient fidelity to support eventual invalidation (F04.3.2) rather than being lost in consumption.

## 14. Failure Conditions

- **Orphaned invalidation (precursor).** A scope change signal indicating removal is missed during consumption, which would leave derived context un-invalidated downstream. Expected behavior: any signal that cannot be consumed or correlated must be surfaced as an explicit gap, never silently ignored.
- **Silent staleness (precursor).** A structural change signal is missed, so affected context ages without its freshness judgment being updated. Expected behavior: the same — surfaced, not silent, per P5.

## 15. Traceability

Product Vision (Mission: maintain context) → Goals G1 (Completeness — scope changes never missed), G2 (Currency of context) → Product Principles P2 (Provenance), P3 (Freshness first-class), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-04 (Context Maintenance) → Epic E04.1 (Change Detection) → Feature F04.1.2 (Structural & Scope Change Consumption).

## 16. Future Considerations

- Reconciling change-signal granularity between Organization's structural change unit and Maintenance's consumption unit, as identified as a risk in the epic, may require future refinement.
- As workspace- and source-specific freshness expectations become more granular (per the capability's future evolution), structural and scope signal consumption may need to carry additional context to support that granularity.
