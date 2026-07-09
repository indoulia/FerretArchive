# FEP-004-SPEC-F04.3.2 — Invalidation Propagation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F04.3.2 |
| **Capability** | [Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Epic** | E04.3 — Re-processing Orchestration & Invalidation |
| **Feature** | F04.3.2 — Invalidation Propagation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-04 — Context Maintenance](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md); [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists to define how context that is no longer valid is invalidated, ensuring completeness, so that removed or out-of-scope context can never be mistakenly assembled as current — this Feature's stated Objective and Product Outcome.

## 3. Scope

- Recognizing that a context unit is no longer valid, because its source was removed or its scope changed to exclude it, based on tracked freshness state (F04.2.1) and consumed scope-change signals (F04.1.2).
- Producing an Invalidation record for each such context unit, capturing that it is invalid and why.
- Propagating invalidation to every derived context unit that depended on the invalidated unit, not only the directly affected one.
- Ensuring an invalidated context unit is excluded from the eligible set Context Assembly draws from.
- Ensuring invalidation propagation is complete for a given trigger, with no dependent derived unit left assemblable as though its source were still valid.

## 4. Out of Scope

- Detecting the underlying source removal or scope change itself (F04.1.1 and F04.1.2's responsibility).
- Tracking ordinary freshness state or age for context that remains valid (F04.2.1).
- Triggering re-acquisition or re-organization for content that is still valid but has changed (F04.3.1).
- Deciding what to assemble or deliver from the remaining eligible set (Context Assembly's responsibility).
- Physically deleting or purging stored content — Ferret is not the system of record for what it observes; this Feature marks invalidity, it does not own content lifecycle or storage.

## 5. Engineering Requirements

1. A context unit whose source has been removed, or whose scope has changed to exclude it, must be recognized as no longer valid.
2. An Invalidation record must be produced for every such context unit, capturing that it is invalid and the reason.
3. Invalidation must propagate to every derived context unit that depended on the invalidated unit, not only the unit directly affected by the originating removal or scope change.
4. An invalidated context unit, and every unit derived from it, must be excluded from the eligible set Context Assembly draws from.
5. Invalidation propagation must be complete for a given originating event — no dependent derived unit may remain assemblable as though the source were still valid.
6. Invalidation activity must be reportable to Observability & Health.

## 6. Inputs

- Freshness State for the context unit in question (from F04.2.1).
- Consumed scope-change signals (from F04.1.2).
- Knowledge of which derived context units depend on a given source or unit, as recorded by Context Organization's structural relationships.

## 7. Outputs

- An Invalidation record per affected context unit, including the reason for invalidation.
- An updated exclusion of invalidated units from the eligible set Context Assembly draws from.

## 8. Preconditions

- Freshness State Tracking (F04.2.1) exists and can identify the state of a context unit.
- Scope change consumption (F04.1.2, and transitively F01.2.3) exists and can identify scope-based invalidity.
- Context Organization has recorded the structural relationships needed to identify which derived units depend on a given source or unit.

## 9. Postconditions

- A removed source or out-of-scope change results in complete invalidation of it and everything derived from it.
- No invalidated unit remains present in the eligible set Context Assembly draws from.
- The invalidation and its reason are recorded and inspectable.

## 10. Dependencies

**Capability dependencies.** Context Organization (structural relationships needed for dependency propagation); Context Assembly (consumer of the resulting exclusion); Workspace Definition (authority over declared scope).

**Epic dependencies.** E04.1 — Change Detection; E04.2 — Freshness Accounting; E01.2 — Scope Declaration & Configuration.

**Feature dependencies.** F04.2.1 — Freshness State Tracking; F01.2.3 — Scope Change Propagation.

**External dependencies.** Source systems (the category whose removal or scope exclusion originates the invalidation this Feature reacts to).

## 11. Constraints

**Business constraints.** None beyond honoring the workspace's own declared scope as the authority for validity.

**Product constraints.** Invalidation must propagate completely — a unit invalidated at the source must not remain assemblable as though nothing happened.

**Context integrity constraints.** Propagation must reach every derived dependent of an invalidated unit, not merely the unit directly named by the originating event.

**Trust constraints.** An invalidated unit must never be presented as current or eligible (P3); the reason for invalidation must be preserved for provenance (P2).

**Policy constraints.** None beyond the scope authority already established by Workspace Definition.

## 12. Acceptance Criteria

1. Given a source is removed or falls out of declared scope, every context unit derived from it is marked invalid.
2. No invalidated context unit appears in the eligible set Context Assembly draws from.
3. Every Invalidation record includes an identifiable reason.
4. A simulated source removal results in zero orphaned derived context remaining assemblable.
5. Invalidation activity is reportable and inspectable via Observability & Health.

## 13. Validation Requirements

- That propagation completeness holds across multi-level derived dependencies (a unit derived from a unit derived from the removed source).
- That exclusion from Context Assembly's eligible set is immediate and complete following invalidation.
- That the reason captured in each Invalidation record is accurate and traceable to the originating scope or source change.
- That no invalidation event is silently dropped during propagation.

## 14. Failure Conditions

- **Orphaned invalidation.** A source is removed but its derived context is never invalidated, leaving ghost context assemblable indefinitely. Expected behavior: this must not occur — propagation completeness is this Feature's core acceptance bar, and any detected gap must be surfaced, never left latent (P5).
- **Silent staleness (invalidation variant).** An invalidated unit lingers without a visible record of its invalidation. Expected behavior: every invalidation is recorded and observable, never silent.

## 15. Traceability

Product Vision (Mission: maintain context) → Goals G2 (Currency of context), G4 (Trustworthy context) → Product Principles P2 (Provenance mandatory), P3 (Freshness first-class), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-04 (Context Maintenance) → Epic E04.3 (Re-processing Orchestration & Invalidation) → Feature F04.3.2 (Invalidation Propagation).

## 16. Future Considerations

- Cross-workspace invalidation propagation is anticipated once Federation is underway, extending this Feature's propagation logic across workspace boundaries; deferred until Federation is underway.
- More granular, dependency-graph-aware propagation is anticipated as Context Organization's structural relationships mature, and as Maintenance signals become an input to Federation's cross-workspace freshness reconciliation.
