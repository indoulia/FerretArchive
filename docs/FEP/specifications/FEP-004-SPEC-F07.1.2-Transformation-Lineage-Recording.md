# FEP-004-SPEC-F07.1.2 — Transformation Lineage Recording

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F07.1.2 |
| **Capability** | [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| **Epic** | E07.1 — Lineage Capture |
| **Feature** | F07.1.2 — Transformation Lineage Recording |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-07 — Provenance & Attribution](../epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md) · [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

An origin fact alone only accounts for the moment of acquisition; everything a unit of context becomes afterward — a structured element, an assembled result — would otherwise be untraceable back to that origin. Transformation Lineage Recording exists to extend the lineage chain through every transformation a unit undergoes, so that the chain established by F07.1.1 does not stop at the first step. This directly serves the Feature's Product Outcome: extending the lineage chain through every transformation, not just origin.

## 3. Scope

- Recording a transformation link between a structured element and the raw material Context Organization produced it from.
- Recording a transformation link between an assembled result and the structured element(s) Context Assembly composed it from.
- Recording transformation links for outputs produced from more than one input, capturing every contributing input.
- Recording transformation links at the time each transformation occurs.

## 4. Out of Scope

- Recording the origin fact at acquisition — that is F07.1.1 (Acquisition-Origin Recording), which this Feature's chain builds on.
- Preserving already-recorded transformation links through later re-organization or re-assembly — that is F07.2.1 (Lineage Survivability Across Transformation).
- Making the resulting lineage queryable or summarizing it for a consumer — that is F07.2.2 (Provenance Inspection & Summarization).
- Detecting or reporting a structured element or assembled result that lacks a recorded transformation link — that is F07.3.1 (Provenance Completeness Reporting).
- Performing the structuring itself, or maintaining the structural link Context Organization needs for its own purposes — that is Context Organization's F03.2.2 (Traceability Preservation); this Feature records that structural lineage as part of the provenance chain, it does not produce the structuring.
- Performing the composition itself — that is Context Assembly's F05.3.1 (Context Composition); this Feature records the resulting transformation link, it does not compose.
- Judging the correctness or quality of the structuring or composition performed — an explicit non-responsibility of FEP-002-CAP-07.

## 5. Engineering Requirements

1. Every structured element produced by Context Organization must have a recorded transformation link to the raw material it was produced from.
2. Every assembled result produced by Context Assembly must have a recorded transformation link to the structured element(s) it draws from.
3. A transformation link must be recorded at the time of the transformation it describes, not reconstructed or retrofitted afterward.
4. A transformation link must be traceable, through however many prior transformation steps exist, back to a recorded origin fact (F07.1.1).
5. A transformation link for an output produced from more than one input must represent every contributing input, with none omitted.
6. Transformation link recording must apply uniformly regardless of transformation type (structuring or assembly) or the granularity at which the transformation occurred.
7. A structured element or assembled result produced without a corresponding recorded transformation link must be a detectable, non-normal condition.

## 6. Inputs

- Structural lineage facts from Context Organization — what raw material produced what structure (per F03.2.2).
- Assembly outcome facts from Context Assembly — what structured elements produced what assembled result (per F05.3.1).
- The existing lineage record (origin fact, or prior transformation links) for each input being consumed by a transformation.

## 7. Outputs

- An extended lineage record: transformation links connecting each structured element and assembled result back to its input(s).

## 8. Preconditions

- An origin fact must already exist (F07.1.1) for any raw material entering a transformation, or a prior transformation link must already exist for any structured element being further transformed.
- Context Organization must have produced structural relationship facts (F03.2.2).
- Context Assembly must have produced composition facts (F05.3.1).
- Per FEP-003-EPIC-CAP-07 §5, this Feature's capture work runs concurrently with Organization's and Assembly's own epics, not sequenced strictly after their completion.

## 9. Postconditions

- Every structured element and every assembled result is linked, through recorded transformations, back to origin.
- No transformation output exists as an orphan — disconnected from the input(s) it was produced from.
- The recorded chain's length and shape reflect the unit's actual transformation history.

## 10. Dependencies

**Capability dependencies.** Context Organization and Context Assembly — supply the structural and assembly transformation facts this Feature records as lineage links.

**Epic dependencies.** E03.2 (Relationship Modeling) and E05.3 (Composition & Gap Reporting) — per FEP-003-EPIC-CAP-07 §4; E07.1 (Lineage Capture) itself, as this Feature's predecessor within the epic.

**Feature dependencies.** F07.1.1 (Acquisition-Origin Recording), F03.2.2 (Traceability Preservation), F05.3.1 (Context Composition) — per FEP-003-EPIC-CAP-07 §3, E07.1 Features table.

**External dependencies.** None beyond those already relied on transitively through F07.1.1 (source systems, per FEP-001 §6).

## 11. Constraints

**Business constraints.** Provenance must be mandatory, not opt-in (FEP-002-CAP-07 §8, Business); every transformation — structuring or assembly — must produce a recorded link, with no transformation type exempted.

**Product constraints.** Transformation links must survive every subsequent transformation the unit undergoes (FEP-002-CAP-07 §8, Product); this Feature is responsible for recording each link correctly at the moment of transformation, since preservation (F07.2.1) can only preserve what was correctly recorded here.

**Context integrity constraints.** Attribution must exist at the granularity a consumer needs (FEP-002-CAP-07 §8, Context integrity); links must be recorded per structured element and per assembled result, not aggregated across a batch of transformations.

**Trust constraints.** Per Product Principle P2 (Provenance is mandatory, not optional), a structured element or assembled result without a recorded transformation link is not a conforming deliverable.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), this Feature must not absorb Context Organization's or Context Assembly's responsibility for performing structuring or composition — it records the resulting link, it does not produce the structure or the composition.

## 12. Acceptance Criteria

1. Every structured element has at least one recorded transformation link to the raw material it originated from.
2. Every assembled result has at least one recorded transformation link to the structured element(s) it was composed from.
3. A structured element or assembled result produced from multiple inputs has all contributing inputs represented in its recorded transformation link.
4. Tracing a transformation link from any structured element or assembled result, through however many steps are recorded, reaches a recorded origin fact.
5. A transformation performed without a corresponding recorded link is identifiable as an anomaly, distinct from a correctly linked transformation.

## 13. Validation Requirements

- That transformation-link coverage reaches every structured element and every assembled result.
- That multi-input transformations retain every contributing input in the recorded link.
- That every recorded chain, traced backward, terminates at a recorded origin fact.
- That transformation links are recorded at the time of transformation, not reconstructed after the fact.

## 14. Failure Conditions

- **Broken lineage.** A transformation loses the link back to its input, producing a structured element or assembled result that cannot be attributed (FEP-002-CAP-07 §10). Expected behavior: this must be detectable, never silently presented as if the chain were intact.
- **Provenance as an afterthought.** Transformation lineage capture is retrofitted inconsistently across structuring and assembly, leaving some outputs fully traceable and others not, with no way to tell which (FEP-002-CAP-07 §10). Expected behavior: recording must occur at transformation time as a matter of course, not as a later reconciliation exercise.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G1 (Completeness — every transformation must be within reach, not only origin), G4 (Trustworthy context — a chain broken mid-transformation cannot support a trust judgment) → Product Principles P2 (Provenance is mandatory, not optional), P3 (Freshness is first-class) → Capability FEP-002-CAP-07 (Provenance & Attribution) → Epic E07.1 (Lineage Capture) → Feature F07.1.2 (Transformation Lineage Recording).

## 16. Future Considerations

- Richer transformation-lineage recording as Context Organization's and Context Assembly's own structuring and composition techniques diversify (FEP-002-CAP-07 §11).
- Reconciling granularity assumptions across capabilities' epics, flagged as a structural risk rather than a settled matter (FEP-003-EPIC-CAP-07 §7, Risks) — different capabilities may assume different units of transformation, requiring explicit reconciliation before this Feature's completeness can be fully assured.
