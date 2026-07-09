# FEP-004-SPEC-F09.4.1 — Extension Admission Criteria

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.4.1 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.4 — Extension Governance |
| **Feature** | F09.4.1 — Extension Admission Criteria |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output (renumbered from F09.3.1 per FEP-003A) |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that a proposed source, structure, or consumer type can be checked against explicit, written criteria before admission, satisfying the Feature's objective of defining the criteria a proposed extension must meet — including trust-capability compliance — so that the Product Outcome of preventing the "trust bypass" failure mode is achieved, and Product Goal G5 (Extensible acquisition and delivery) never comes at the expense of G4 (Trustworthy context).

## 3. Scope

- Defining the explicit criteria a proposed source type, structure type, or consumer type must satisfy to be admitted.
- Requiring, as part of those criteria, that a proposed source or structure type can satisfy Provenance & Attribution's obligations (per F07.3.1 — Provenance Completeness Reporting) once admitted.
- Requiring, as part of those criteria, that a proposed consumer type can be gated by Access Control & Policy (per F08.1.1 — Policy Declaration) once admitted.
- Defining what it means for a proposed extension to fail admission, and that failure is a definite, checkable outcome rather than a discretionary judgment call.

## 4. Out of Scope

- Defining the extension points a proposed source, structure, or consumer type is described against — owned by Source Type Extension Point Definition (F09.1.1), Structure Type Extension Point Definition (F09.2.1), and Consumer Type Extension Point Definition (F09.3.1).
- Maintaining the inventories of admitted source, structure, or consumer types — owned by Source Type Inventory (F09.1.2), Structure Type Inventory (F09.2.2), and Consumer Type Inventory (F09.3.2).
- Performing the underlying provenance capture or completeness checking itself — owned by Provenance & Attribution (FEP-002-CAP-07); this Feature only requires that a proposed extension be capable of satisfying it.
- Performing the underlying policy declaration or permission evaluation itself — owned by Access Control & Policy (FEP-002-CAP-08); this Feature only requires that a proposed extension be capable of being gated by it.
- Judging whether a proposed extension is otherwise a "good idea" on business or strategic grounds — an explicit Non-Responsibility of Extensibility (capability §3); this Feature evaluates fit and trust-capability compliance only.
- Enforcing consequences for non-compliant extensions that are admitted anyway through some other, undocumented path — governance enforcement outside these criteria is out of scope.

## 5. Engineering Requirements

1. Admission criteria must be written and explicit, not implicit in reviewer judgment.
2. Admission criteria must require that a proposed source type can be described against the Source Type Extension Point Definition (F09.1.1) without altering another capability's responsibilities.
3. Admission criteria must require that a proposed structure type can be described against the Structure Type Extension Point Definition (F09.2.1) without altering another capability's responsibilities.
4. Admission criteria must require that a proposed consumer type can be described against the Consumer Type Extension Point Definition (F09.3.1) without altering another capability's responsibilities.
5. Admission criteria must require that a proposed source or structure type demonstrate it can satisfy Provenance Completeness Reporting (F07.3.1) once admitted.
6. Admission criteria must require that a proposed consumer type demonstrate it can be gated by Policy Declaration (F08.1.1) once admitted.
7. A proposed extension that fails any criterion must be rejected, and the specific failing criterion must be identifiable.
8. Admission criteria must be applicable uniformly to any proposed source, structure, or consumer type, without a criterion that only makes sense for one specific proposal.

## 6. Inputs

- A proposed source type, structure type, or consumer type, described against the relevant extension point (F09.1.1, F09.2.1, or F09.3.1).
- The current obligations defined by Provenance Completeness Reporting (F07.3.1) and Policy Declaration (F08.1.1).

## 7. Outputs

- An admission decision for the proposed extension: admitted or rejected.
- Where rejected, an identification of which criterion or criteria were not met.

## 8. Preconditions

- Source Type Extension Point Definition (F09.1.1), Structure Type Extension Point Definition (F09.2.1), and Consumer Type Extension Point Definition (F09.3.1) all exist, so that a proposal has something concrete to be evaluated against.
- Provenance Completeness Reporting (F07.3.1) and Policy Declaration (F08.1.1) both exist, so that trust-capability compliance can actually be checked rather than merely asserted.

## 9. Postconditions

- Every admitted source, structure, or consumer type is known to satisfy the extension point it was described against and the relevant trust-capability obligations.
- No extension bypasses provenance or access-control obligations by virtue of having been admitted.

## 10. Dependencies

**Capability dependencies.** Provenance & Attribution — supplies the completeness obligation a proposed source or structure type must satisfy; Access Control & Policy — supplies the policy obligation a proposed consumer type must satisfy.

**Epic dependencies.** E09.1 (Acquisition Extension Points), E09.2 (Organization Extension Points), E09.3 (Delivery Extension Points), E07.3 (Provenance Completeness Assurance), E08.1 (Policy Definition & Scope) — per Global Output 3 and epic file §4.

**Feature dependencies.** F09.1.1 — Source Type Extension Point Definition; F09.2.1 — Structure Type Extension Point Definition; F09.3.1 — Consumer Type Extension Point Definition; F07.3.1 — Provenance Completeness Reporting; F08.1.1 — Policy Declaration (all listed as Dependencies in the epic file §3).

**External dependencies.** None beyond the source, structure, and consumer categories already named conceptually by the extension points this Feature evaluates proposals against.

## 11. Constraints

**Business constraints.** Admission criteria must never be relaxed to accommodate a specific proposal; the criteria govern the proposal, not the reverse (capability §8, business constraint).

**Product constraints.** Applying admission criteria must not become proportionally harder as the number of admitted extensions grows (capability §8, product constraint).

**Context integrity constraints.** An admitted source or structure type must not degrade the completeness or currency guarantees that already-supported types rely on (capability §9).

**Trust constraints.** No extension may be admitted that bypasses Provenance & Attribution's or Access Control & Policy's obligations (capability §8, context integrity constraint; Product Principle P2); admission is the enforcement point for this guarantee.

**Policy constraints.** Admission is a fit-and-compliance evaluation only; it must not extend into deciding engineering process, review, or approval workflows for the proposal's own development (FEP-001 Non-Goals).

## 12. Acceptance Criteria

1. A proposed source, structure, or consumer type that satisfies every admission criterion is admitted.
2. A deliberately non-compliant proposed extension — one that fails at least one criterion — is correctly rejected.
3. A rejection identifies which specific criterion or criteria were not satisfied.
4. No admitted extension can be shown to bypass Provenance Completeness Reporting (F07.3.1) or Policy Declaration (F08.1.1) obligations.
5. The same set of criteria is applied regardless of whether the proposal is a source, structure, or consumer type, adapted only for which extension point and which trust capability applies.

## 13. Validation Requirements

- That a compliant proposed extension is admitted.
- That a proposed extension deliberately missing trust-capability compliance is rejected, with the missing criterion identified.
- That a proposed extension deliberately misdescribed against the wrong extension point is rejected or corrected before admission.

## 14. Failure Conditions

- **Trust bypass** (capability §10): a new extension is admitted despite skipping provenance or access-control obligations — the system must reject the proposal and surface the specific unmet criterion, never admit it silently (Product Principle P5).
- **Governance criteria without enforcement teeth** (epic §7): admission criteria exist only as an unenforced checklist — rejection outcomes must be a real, observable gate on admission, not an advisory note that can be overridden without record.

## 15. Traceability

Product Vision (Mission) → G4 (Trustworthy context), G5 (Extensible acquisition and delivery) → Product Principles P2, P4, P5, P6 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.4 (Extension Governance) → Feature F09.4.1 (Extension Admission Criteria).

## 16. Future Considerations

- A more formal, evaluable process for proposing and admitting new source, structure, and consumer types as the ecosystem around Ferret grows, toward FEP-001's Generation 4 / Ecosystem (capability §11; epic §8).
- Support for third-party-authored extensions once these admission criteria are mature and stable enough to support external proposers (capability §11).
- Extending admission criteria to evaluate federation-aware source, structure, and consumer types once Federation is underway (capability §11; epic §8).
