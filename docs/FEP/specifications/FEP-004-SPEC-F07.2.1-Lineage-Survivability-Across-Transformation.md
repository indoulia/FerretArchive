# FEP-004-SPEC-F07.2.1 — Lineage Survivability Across Transformation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F07.2.1 |
| **Capability** | [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| **Epic** | E07.2 — Lineage Preservation & Query |
| **Feature** | F07.2.1 — Lineage Survivability Across Transformation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-07 — Provenance & Attribution](../epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md) · [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Context is not static — Context Maintenance triggers re-organization and re-assembly as sources change, and each such cycle is another transformation capable of breaking a lineage chain that was correctly recorded the first time. Lineage Survivability Across Transformation exists to guarantee that a re-organization or re-assembly of already-organized content never severs the lineage chain built by F07.1.1 and F07.1.2. This directly serves the Feature's Product Outcome: long-lived context remains fully attributable through many Maintenance cycles, not only through its first pass.

## 3. Scope

- Ensuring a re-organization or re-assembly of previously organized or assembled content extends, rather than replaces or erases, that content's existing lineage.
- Ensuring the resulting lineage, after any number of successive re-organization or re-assembly cycles, retains an unbroken path back to the unit's original acquisition.
- Making a would-be break in the lineage chain, caused by a re-processing cycle, a detectable condition rather than a silent one.
- Keeping prior lineage states inspectable after a superseding transformation, rather than folding them away.

## 4. Out of Scope

- Recording a transformation link for the first time a unit is structured or assembled — that is F07.1.2 (Transformation Lineage Recording), which this Feature's preservation guarantee builds on.
- Making lineage queryable or producing consumer-facing provenance summaries — that is F07.2.2 (Provenance Inspection & Summarization).
- Detecting or reporting a unit whose lineage is incomplete — that is F07.3.1 (Provenance Completeness Reporting).
- Deciding when re-processing occurs, or performing the re-organization or re-assembly itself — that is Context Maintenance's F04.3.1 (Re-acquisition & Re-organization Triggering) and Context Organization's/Context Assembly's own re-processing work; this Feature only guarantees that whatever re-processing occurs does not break lineage.
- Judging whether the re-organized or re-assembled content is more correct, better structured, or higher quality than before — an explicit non-responsibility of FEP-002-CAP-07.

## 5. Engineering Requirements

1. When existing structured or assembled content is re-organized or re-assembled, the resulting lineage must retain an unbroken path back to the unit's original acquisition.
2. A re-organization or re-assembly event must produce a new transformation link that references the unit's prior lineage state, not one that discards it.
3. Lineage preservation must hold across an unbounded number of successive re-organization or re-assembly cycles for the same unit.
4. A re-processing cycle that would break the lineage chain must be detected and must not be allowed to complete as though lineage were intact.
5. Preservation must hold independent of what triggered the re-processing — freshness expiry, detected source change, or any other Context Maintenance trigger.
6. A unit's prior lineage state must remain inspectable after a superseding transformation, not merely absorbed into the new state without a trace.

## 6. Inputs

- The existing lineage record for a unit entering a re-organization or re-assembly cycle.
- The re-processing trigger fact from Context Maintenance (per F04.3.1).
- The new transformation fact produced by Context Organization or Context Assembly for the re-processing event.

## 7. Outputs

- An extended, unbroken lineage record spanning original acquisition through every subsequent re-organization or re-assembly.
- A retained, inspectable record of the unit's prior lineage state(s).

## 8. Preconditions

- The unit must already have a transformation lineage recorded (F07.1.2) before any re-processing cycle occurs.
- Context Maintenance must have triggered, or be capable of triggering, a re-processing cycle for the unit (F04.3.1).

## 9. Postconditions

- After any re-organization or re-assembly, the unit's lineage traces continuously back to its original acquisition.
- No re-processing cycle leaves the lineage chain with an undetected gap.
- Historical lineage states for the unit remain inspectable after being superseded.

## 10. Dependencies

**Capability dependencies.** Context Maintenance — supplies the re-processing trigger this Feature must preserve lineage across; Context Organization and Context Assembly — perform the re-processing itself.

**Epic dependencies.** E07.1 (Lineage Capture) — preservation presupposes an initial lineage exists to be preserved; E04.3 (Re-processing Orchestration & Invalidation) — per FEP-003-Global-Output-3, E07.2 depends on E04.3.

**Feature dependencies.** F07.1.2 (Transformation Lineage Recording), F04.3.1 (Re-acquisition & Re-organization Triggering) — per FEP-003-EPIC-CAP-07 §3, E07.2 Features table.

**External dependencies.** Change-notification sources (FEP-001 §6) — indirectly, as the ultimate trigger behind Context Maintenance's re-processing cycles; this Feature does not itself observe change, it only guarantees lineage survives whatever re-processing results from it.

## 11. Constraints

**Business constraints.** Provenance must be mandatory, not opt-in (FEP-002-CAP-07 §8, Business); a re-processing cycle must never be permitted to leave a unit without lineage, since that would be equivalent to the provenance obligation lapsing partway through the unit's life.

**Product constraints.** Provenance records must survive every transformation a unit passes through, and a broken lineage chain is a defect, not an acceptable gap (FEP-002-CAP-07 §8, Product) — this constraint is this Feature's central obligation, applied specifically to re-processing transformations rather than first-pass ones.

**Context integrity constraints.** Attribution must exist at the granularity a consumer needs (FEP-002-CAP-07 §8, Context integrity); preservation must maintain that granularity across re-processing rather than collapsing prior detail into a coarser summary.

**Trust constraints.** Per Product Principle P3 (Freshness is first-class, not assumed), a re-processed unit's lineage must reflect that it has, in fact, been re-processed — preservation is not a license to make re-processing invisible.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), this Feature must not absorb Context Maintenance's responsibility for deciding when to trigger re-processing, nor Context Organization's or Context Assembly's responsibility for performing it.

## 12. Acceptance Criteria

1. A simulated re-organization of previously organized material preserves an unbroken lineage chain to original acquisition.
2. A simulated re-assembly of previously assembled content preserves an unbroken lineage chain to original acquisition.
3. Repeating a re-organization or re-assembly cycle multiple times in succession on the same unit preserves the chain at every step, not only the first.
4. A re-processing cycle that would break the chain is detected before, or at, the point the break would occur, rather than being discovered only later.
5. A unit's lineage state prior to its most recent re-processing cycle remains retrievable after that cycle completes.

## 13. Validation Requirements

- That lineage survives an arbitrary number of successive re-processing cycles, not only a single cycle.
- That lineage survival holds regardless of which Context Maintenance trigger initiated the re-processing.
- That a break in the chain, were one to occur, would be detected rather than passing unnoticed.
- That prior lineage states remain distinguishable and retrievable after being superseded.

## 14. Failure Conditions

- **Broken lineage.** A re-organization or re-assembly loses the link back to the unit's prior lineage state, producing content that can no longer be attributed to its original acquisition (FEP-002-CAP-07 §10). Expected behavior: this must be detected and surfaced, per Product Principle P5, never allowed to present as an intact chain.
- **Provenance as an afterthought.** Re-processing cycles are handled inconsistently — some preserving lineage, others not — with no way to tell which units retain a full chain (FEP-002-CAP-07 §10). Expected behavior: preservation must be uniform across every re-processing cycle; any inconsistency must itself be a detectable, reportable condition.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G2 (Currency of context — re-processing exists to keep context current, and must not cost it its lineage in the process), G4 (Trustworthy context — long-lived context is only as trustworthy as its surviving chain) → Product Principles P2 (Provenance is mandatory, not optional), P3 (Freshness is first-class), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-07 (Provenance & Attribution) → Epic E07.2 (Lineage Preservation & Query) → Feature F07.2.1 (Lineage Survivability Across Transformation).

## 16. Future Considerations

- Preservation guarantees extending to Federation-spanning re-processing, where a unit's re-organization or re-assembly may eventually cross workspace boundaries — deferred to Federation (FEP-002-CAP-07 §11; FEP-003-EPIC-CAP-07 §8).
- Revisiting completeness-verification cost as the actual diversity and frequency of re-processing cycles becomes known (FEP-003-EPIC-CAP-07 §7, Risks).
