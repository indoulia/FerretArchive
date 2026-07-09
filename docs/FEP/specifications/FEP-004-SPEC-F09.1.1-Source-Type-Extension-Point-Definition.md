# FEP-004-SPEC-F09.1.1 — Source Type Extension Point Definition

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.1.1 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.1 — Acquisition Extension Points |
| **Feature** | F09.1.1 — Source Type Extension Point Definition |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that Context Acquisition has a stable, conceptual point at which a new source type can attach, satisfying the Feature's objective of defining that point without requiring Acquisition itself to be redesigned each time the world outside Ferret changes — the Product Outcome of enabling source diversity to grow, and the acquisition-side mechanism by which Product Goal G5 (Extensible acquisition and delivery) is made real.

## 3. Scope

- Defining, conceptually, the single point at Context Acquisition's boundary where a new source type can be attached.
- Describing what a source type must conceptually supply or satisfy to be evaluated against that point.
- Documenting the extension point so it can be applied consistently to any future proposed source type.
- Confirming the extension point's definition does not presuppose or hard-code any single existing source type.

## 4. Out of Scope

- Deciding whether a specific proposed source type should be admitted — owned by Extension Admission Criteria (F09.3.1).
- Enumerating or maintaining the list of currently supported source types — owned by Source Type Inventory (F09.1.2).
- Performing acquisition (discovery, reading, or event recording) for any source type, existing or new — owned by Context Acquisition (FEP-002-CAP-02); acquiring, organizing, assembling, or delivering context is a Non-Responsibility of Extensibility itself (capability §3; FEP-001 Non-Goals).
- Defining the analogous extension point for consumer types — owned by Consumer Type Extension Point Definition (F09.2.1).
- Redesigning or altering Context Acquisition's, Organization's, Maintenance's, or Assembly's existing responsibilities.

## 5. Engineering Requirements

1. The extension point must be described independently of any specific source type, existing or proposed.
2. The extension point must state what a source type must conceptually supply in order to be evaluated against it.
3. The extension point must be positioned at Context Acquisition's boundary such that attaching a new source type never requires changing Acquisition's, Organization's, Maintenance's, or Assembly's defined responsibilities.
4. The extension point's documentation must be sufficient for a future proposed source type to be described and evaluated against it without further clarification of the point itself.
5. The extension point must not incorporate or presume satisfaction of provenance or access-control obligations — those are evaluated separately, by Extension Admission Criteria (F09.3.1).

## 6. Inputs

- The current, stable definition of Context Acquisition's responsibilities and boundary.
- Conceptual descriptions of existing source types, used to confirm the extension point generalizes beyond any single one of them.

## 7. Outputs

- A documented, conceptual extension point at Context Acquisition's boundary.

## 8. Preconditions

- Context Acquisition's Source Discovery epic (E02.1) exists in stable form, since this extension point is defined against Acquisition's boundary as currently understood (Global Output 3; epic §4).

## 9. Postconditions

- Context Acquisition has a documented point at which a new source type can be described and evaluated.
- No change to Acquisition's, Organization's, Maintenance's, or Assembly's defined responsibilities is required in order to describe a new source type against that point.

## 10. Dependencies

**Capability dependencies.** Context Acquisition — supplies the boundary this extension point attaches to.

**Epic dependencies.** E02.1 — Source Discovery (must be stable, per epic file §4 and Global Output 3).

**Feature dependencies.** None within this capability; this Feature is the prerequisite foundation for Source Type Inventory (F09.1.2) within E09.1.

**External dependencies.** Source systems, as the general category of external system a future source type would represent — referenced only conceptually, not as any specific system.

## 11. Constraints

**Business constraints.** The extension point must be additive; it must never require compromising Context Acquisition's boundary as defined in FEP-001 (capability §8).

**Product constraints.** The cost of describing a new source type against this point must not grow with the number of source types already supported (capability §8).

**Context integrity constraints.** The extension point must not allow a new source type, once admitted, to bypass Faithful Content Reading or Acquisition Event Recording obligations.

**Trust constraints.** The extension point's definition must not itself satisfy or waive Provenance & Attribution obligations (Product Principle P2) — satisfaction is confirmed later, by Extension Admission Criteria (F09.3.1).

**Policy constraints.** The extension point must respect capability boundaries rather than team boundaries (Product Principle P6).

## 12. Acceptance Criteria

1. The extension point is described without reference to any single specific source type.
2. At least one existing source type and one hypothetical new source type can each be described against the extension point using the same definition.
3. Describing a new source type against the extension point requires no change to Acquisition's, Organization's, Maintenance's, or Assembly's documented responsibilities.
4. The extension point's documentation is sufficient for a contributor unfamiliar with it to describe a new source type against it without further clarification.

## 13. Validation Requirements

- That the extension point's definition remains unchanged when a second, differently-shaped source type is described against it.
- That no other capability's documented responsibilities require modification when a new source type is described against the extension point.

## 14. Failure Conditions

- **Special-casing** (capability §10): a source type is added by altering Organization's, Maintenance's, or Assembly's responsibilities instead of going through this extension point — the system must make this deviation visible as a documented boundary violation, never absorb it silently (Product Principle P5).
- **Extension point rot** (capability §10): the extension point is defined but never actually exercised, so real source-type additions bypass it in practice — this must be detectable by comparing any newly supported source type against the documented extension point.

## 15. Traceability

Product Vision (Mission) → G5 (Extensible acquisition and delivery) → Product Principles P1, P5, P6 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.1 (Acquisition Extension Points) → Feature F09.1.1 (Source Type Extension Point Definition).

## 16. Future Considerations

- A more formal, evaluable process for proposing new source types as the ecosystem around Ferret grows, toward FEP-001's Generation 4 / Ecosystem (capability §11).
- Revisiting this extension point's shape once Acquisition has handled more than one real source type, to confirm it reflects a genuine pattern rather than a single example (epic §7).
- Extending this point to support federation-aware source types once Federation is underway (capability §11; epic §8).
