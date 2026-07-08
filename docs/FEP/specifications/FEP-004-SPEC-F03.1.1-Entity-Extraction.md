# FEP-004-SPEC-F03.1.1 — Entity Extraction

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F03.1.1 |
| **Capability** | [FEP-002-CAP-03 — Context Organization](../capabilities/FEP-002-CAP-03-Context-Organization.md) |
| **Epic** | E03.1 — Entity Extraction |
| **Feature** | F03.1.1 — Entity Extraction |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-03 — Context Organization](../epics/FEP-003-EPIC-CAP-03-Context-Organization.md); [FEP-002-CAP-03 — Context Organization](../capabilities/FEP-002-CAP-03-Context-Organization.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Raw acquired material is bytes, not context. This specification exists to define the first act of turning material into context: recognizing that a given Acquisition Unit contains one or more meaningful things — a component, a decision, a person, a requirement — so that everything downstream (relating, maintaining, assembling, delivering) has something structured to operate on. Without this Feature, Context Organization has nothing to relate, signal change on, or hand to Assembly.

## 3. Scope

- Recognizing entities present within a single given Acquisition Unit's raw material, at a conceptual level (component, decision, person, requirement, and other categories as they become supported).
- Establishing, at the moment of extraction, a resolvable link from each extracted entity to the Acquisition Unit(s) it was derived from.
- Producing extraction output that is generic — reflecting what the raw material actually contains, independent of any anticipated consumer.

## 4. Out of Scope

- Recognizing whether a newly extracted entity is the same as a previously known entity (continuity recognition) — belongs to F03.1.2.
- Identifying relationships between entities — belongs to F03.2.1.
- The durable, system-wide guarantee that traceability is preserved across all structured elements over time, including through continuity merges — belongs to F03.2.2. This Feature is responsible only for establishing the link at extraction time.
- Deciding whether extracted material is current or stale — belongs to Context Maintenance (Non-Goal boundary; FEP-001 §2.4/§2.3).
- Selecting or ranking entities for a specific request — belongs to Context Assembly.
- Discovering or reading the raw material itself — belongs to Context Acquisition.
- Reasoning about, evaluating, or generating conclusions from an entity's content — explicitly a Non-Goal of Ferret (FEP-001 §1.3).

## 5. Engineering Requirements

1. The system shall identify entities present in a given Acquisition Unit's raw material.
2. Each identified entity shall represent a single conceptually meaningful thing (e.g., a component, a decision, a person, a requirement).
3. Entity identification shall be based solely on the content of the raw material, independent of any anticipated consumer of the resulting context.
4. Every extracted entity shall carry a resolvable link to the Acquisition Unit(s) it was derived from at the moment of extraction.
5. Re-extracting from unchanged raw material shall not produce a materially different set of entities absent a traceable change in the source.
6. Entity extraction shall not be limited to a fixed, closed set of entity categories in a way that precludes recognizing new categories as they become supported.

## 6. Inputs

- Raw acquired material belonging to a single Acquisition Unit.
- The set of entity categories currently recognized as conceptually meaningful.

## 7. Outputs

- A set of extracted entities, each representing one conceptually meaningful thing found in the raw material.
- For each extracted entity, an associated link identifying the Acquisition Unit(s) it was derived from.

## 8. Preconditions

- The raw material for the Acquisition Unit has already been faithfully read and preserved (F02.2.1 — Faithful Content Reading).
- The Acquisition Unit falls within a workspace's declared scope.

## 9. Postconditions

- Every entity actually present in the raw material is represented as a discrete extracted entity.
- Every extracted entity is traceable to the Acquisition Unit(s) it came from.
- No entity exists in the output that is not supported by the raw material.

## 10. Dependencies

**Capability dependencies:** Context Acquisition (upstream source of raw material).

**Epic dependencies:** E02.2 — Content Reading & Preservation.

**Feature dependencies:** F02.2.1 — Faithful Content Reading (prerequisite; this Feature has no dependency on any other Feature within Context Organization).

**External dependencies:** Source systems (indirectly, via Context Acquisition) — this Feature has no direct interaction with source systems.

## 11. Constraints

**Business constraints:** Extraction must stay generic — it organizes what the content actually says, not what any one anticipated consumer wants to hear (capability §8, Business).

**Product constraints:** Extraction should be idempotent in spirit — re-organizing unchanged raw material should not silently produce a materially different set of entities (capability §8, Product).

**Context integrity constraints:** Every extracted entity must remain traceable to the raw material that produced it (capability §8, Context integrity); ties to Product Principle P2.

**Trust constraints:** An entity must reflect only what the raw material actually expresses — reflects Product Principle P1 (context over computation): improve what is known, do not infer beyond the source.

**Policy constraints:** None beyond access constraints already resolved upstream by Context Acquisition and Access Control & Policy.

## 12. Acceptance Criteria

1. For an Acquisition Unit containing entities of supported categories, extraction produces exactly the entities actually present, with no fabricated entities.
2. Every extracted entity has a resolvable link back to its source Acquisition Unit.
3. Re-running extraction on unchanged raw material yields the same set of entities.
4. Extraction output for a given Acquisition Unit does not vary based on which consumer will eventually query the resulting context.

## 13. Validation Requirements

- Coverage of known entities against fixed raw-material fixtures.
- Resolvability of the traceability link for every produced entity.
- Stability of extraction output across repeated runs on unchanged input.
- Absence of consumer-specific variance in extraction output.

## 14. Failure Conditions

1. **Untraceable extraction attempt.** An entity would be extracted but no resolvable link to its source Acquisition Unit can be established. The entity must not be persisted as structured context; the gap must be visibly signaled (per P5), not silently dropped or given a fabricated link.
2. **Ambiguous or unrecognizable raw material.** Content does not clearly resolve to any supported entity category. No entity is fabricated; the material's non-extraction remains observable rather than silent.
3. **Consumer-biased extraction.** Extraction outcome varies based on an anticipated consumer rather than raw-material content alone. This is treated as a violation of Product Principle P4 and must be corrected to consumer-neutral behavior.

## 15. Traceability

Product Vision (Mission: turning raw material into organized context) → Goals G1 (Completeness), G4 (Trustworthy context) → Product Principles P1, P2, P4 → Capability FEP-002-CAP-03 (Context Organization) → Epic E03.1 (Entity Extraction) → Feature F03.1.1 (Entity Extraction).

## 16. Future Considerations

- Deeper relationship modeling and richer entity categories as acquired source categories grow (capability §11, Future Evolution).
- The definition of "meaningful entity" may require refinement as more source categories are acquired (epic §7, Risks — entity model instability).
