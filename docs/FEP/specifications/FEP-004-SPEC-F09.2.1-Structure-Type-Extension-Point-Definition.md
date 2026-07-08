# FEP-004-SPEC-F09.2.1 — Structure Type Extension Point Definition

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.2.1 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.2 — Organization Extension Points |
| **Feature** | F09.2.1 — Structure Type Extension Point Definition |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output (added per FEP-003A Required Correction) |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that Context Organization has a stable, conceptual point at which a new structure type can attach, satisfying the Feature's objective of defining that point without requiring Organization itself to be redesigned each time a genuinely new kind of structure needs to be extracted or related — the Product Outcome of enabling structural diversity to grow, and the organization-side mechanism by which Product Goal G5 (Extensible acquisition and delivery) is made real. FEP-001 §2.9 assigns Extensibility this exact surface ("new kinds of structure, for Organization"); this Feature restores it as a first-class Engineering Specification per the Required Correction in [FEP-003A](../reviews/FEP-003A-Engineering-Program-Review.md).

## 3. Scope

- Defining, conceptually, the single point at Context Organization's boundary where a new structure type can be attached.
- Describing what a structure type must conceptually supply or satisfy to be evaluated against that point.
- Documenting the extension point so it can be applied consistently to any future proposed structure type.
- Confirming the extension point's definition does not presuppose or hard-code any single existing structure type (e.g., a specific entity or relationship shape).

## 4. Out of Scope

- Deciding whether a specific proposed structure type should be admitted — owned by Extension Admission Criteria (F09.4.1).
- Enumerating or maintaining the list of currently supported structure types — owned by Structure Type Inventory (F09.2.2).
- Performing organization (entity extraction, relationship modeling, or structural change signaling) for any structure type, existing or new — owned by Context Organization (FEP-002-CAP-03); acquiring, organizing, assembling, or delivering context is a Non-Responsibility of Extensibility itself (capability §3; FEP-001 Non-Goals).
- Defining the analogous extension point for source types — owned by Source Type Extension Point Definition (F09.1.1) — or for consumer types — owned by Consumer Type Extension Point Definition (F09.3.1).
- Redesigning or altering Context Organization's, Maintenance's, or Assembly's existing responsibilities.

## 5. Engineering Requirements

1. The extension point must be described independently of any specific structure type, existing or proposed.
2. The extension point must state what a structure type must conceptually supply — such as what it extracts or how it relates entities — in order to be evaluated against it.
3. The extension point must be positioned at Context Organization's boundary such that attaching a new structure type never requires changing Organization's, Maintenance's, or Assembly's defined responsibilities.
4. The extension point's documentation must be sufficient for a future proposed structure type to be described and evaluated against it without further clarification of the point itself.
5. The extension point must not incorporate or presume satisfaction of provenance obligations — those are evaluated separately, by Extension Admission Criteria (F09.4.1).

## 6. Inputs

- The current, stable definition of Context Organization's responsibilities and boundary.
- Conceptual descriptions of existing structure types (entities, relationships), used to confirm the extension point generalizes beyond any single one of them.

## 7. Outputs

- A documented, conceptual extension point at Context Organization's boundary.

## 8. Preconditions

- Context Organization's Entity Extraction epic (E03.1) exists in stable form, since this extension point is defined against Organization's boundary as currently understood (epic file §4).

## 9. Postconditions

- Context Organization has a documented point at which a new structure type can be described and evaluated.
- No change to Organization's, Maintenance's, or Assembly's defined responsibilities is required in order to describe a new structure type against that point.

## 10. Dependencies

**Capability dependencies.** Context Organization — supplies the boundary this extension point attaches to.

**Epic dependencies.** E03.1 — Entity Extraction (must be stable, per epic file §4).

**Feature dependencies.** None within this capability; this Feature is the prerequisite foundation for Structure Type Inventory (F09.2.2) within E09.2.

**External dependencies.** None — structure types are an internal organizational concept, not tied to any specific external system category.

## 11. Constraints

**Business constraints.** The extension point must be additive; it must never require compromising Context Organization's boundary as defined in FEP-001 (capability §8).

**Product constraints.** The cost of describing a new structure type against this point must not grow with the number of structure types already supported (capability §8).

**Context integrity constraints.** The extension point must not allow a new structure type, once admitted, to bypass Entity Extraction's, Relationship Identification's, or Traceability Preservation's obligations.

**Trust constraints.** The extension point's definition must not itself satisfy or waive Provenance & Attribution obligations (Product Principle P2) — satisfaction is confirmed later, by Extension Admission Criteria (F09.4.1).

**Policy constraints.** The extension point must respect capability boundaries rather than team boundaries (Product Principle P6).

## 12. Acceptance Criteria

1. The extension point is described without reference to any single specific structure type.
2. At least one existing structure type and one hypothetical new structure type can each be described against the extension point using the same definition.
3. Describing a new structure type against the extension point requires no change to Organization's, Maintenance's, or Assembly's documented responsibilities.
4. The extension point's documentation is sufficient for a contributor unfamiliar with it to describe a new structure type against it without further clarification.

## 13. Validation Requirements

- That the extension point's definition remains unchanged when a second, differently-shaped structure type is described against it.
- That no other capability's documented responsibilities require modification when a new structure type is described against the extension point.

## 14. Failure Conditions

- **Special-casing** (capability §10): a structure type is added by altering Maintenance's or Assembly's responsibilities instead of going through this extension point — the system must make this deviation visible as a documented boundary violation, never absorb it silently (Product Principle P5).
- **Extension point rot** (capability §10): the extension point is defined but never actually exercised, so real structure-type additions bypass it in practice — this must be detectable by comparing any newly supported structure type against the documented extension point.

## 15. Traceability

Product Vision (Mission) → G5 (Extensible acquisition and delivery) → Product Principles P1, P5, P6 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.2 (Organization Extension Points) → Feature F09.2.1 (Structure Type Extension Point Definition).

## 16. Future Considerations

- A more formal, evaluable process for proposing new structure types as the ecosystem around Ferret grows, toward FEP-001's Generation 4 / Ecosystem (capability §11).
- Revisiting this extension point's shape once Organization has handled more than one real structure type, to confirm it reflects a genuine pattern rather than a single example (epic §7).
- Extending this point to support federation-aware structure types once Federation is underway (capability §11; epic §8).
