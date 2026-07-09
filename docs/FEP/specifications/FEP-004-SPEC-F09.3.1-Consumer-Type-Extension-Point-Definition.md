# FEP-004-SPEC-F09.3.1 — Consumer Type Extension Point Definition

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.3.1 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.3 — Delivery Extension Points |
| **Feature** | F09.3.1 — Consumer Type Extension Point Definition |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output (renumbered from F09.2.1 per FEP-003A) |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that Context Delivery has a stable, conceptual point at which a new consumer type can attach, satisfying the Feature's objective of defining that point without requiring Delivery itself to be redesigned each time a new kind of consumer emerges — the Product Outcome of enabling consumer diversity to grow, and the delivery-side mechanism by which Product Goal G5 (Extensible acquisition and delivery) and Product Principle P4 (No privileged consumer) are made real together.

## 3. Scope

- Defining, conceptually, the single point at Context Delivery's boundary where a new consumer type can be attached.
- Describing what a consumer type must conceptually supply or satisfy to be evaluated against that point.
- Documenting the extension point so it can be applied consistently to any future proposed consumer type.
- Confirming the extension point's definition does not presuppose or hard-code any single existing consumer type, preserving No Privileged Consumer (Product Principle P4).

## 4. Out of Scope

- Deciding whether a specific proposed consumer type should be admitted — owned by Extension Admission Criteria (F09.4.1).
- Enumerating or maintaining the list of currently supported consumer types — owned by Consumer Type Inventory (F09.3.2).
- Performing delivery (assembly, presentation, or access gating) for any consumer type, existing or new — owned by Context Delivery (FEP-002-CAP-06) and Context Assembly (FEP-002-CAP-05); acquiring, organizing, assembling, or delivering context is a Non-Responsibility of Extensibility itself (capability §3; FEP-001 Non-Goals).
- Defining the analogous extension point for source types — owned by Source Type Extension Point Definition (F09.1.1) — or for structure types — owned by Structure Type Extension Point Definition (F09.2.1).
- Redesigning or altering Context Assembly's or Context Delivery's existing responsibilities.

## 5. Engineering Requirements

1. The extension point must be described independently of any specific consumer type, existing or proposed.
2. The extension point must state what a consumer type must conceptually supply — such as the fit or presentation it requires — in order to be evaluated against it.
3. The extension point must be positioned at Context Delivery's boundary such that attaching a new consumer type never requires changing Assembly's or Delivery's defined responsibilities.
4. The extension point's documentation must be sufficient for a future proposed consumer type to be described and evaluated against it without further clarification of the point itself.
5. The extension point must not incorporate or presume satisfaction of access-control obligations — those are evaluated separately, by Extension Admission Criteria (F09.4.1).

## 6. Inputs

- The current, stable definition of Context Delivery's responsibilities and boundary.
- Conceptual descriptions of existing consumer types, used to confirm the extension point generalizes beyond any single one of them.

## 7. Outputs

- A documented, conceptual extension point at Context Delivery's boundary.

## 8. Preconditions

- Context Delivery's Consumer-Fit Presentation epic (E06.1) exists in stable form, since this extension point is defined against Delivery's boundary as currently understood (Global Output 3; epic §4).

## 9. Postconditions

- Context Delivery has a documented point at which a new consumer type can be described and evaluated.
- No change to Assembly's or Delivery's defined responsibilities is required in order to describe a new consumer type against that point.

## 10. Dependencies

**Capability dependencies.** Context Delivery — supplies the boundary this extension point attaches to; Context Assembly — indirectly affected, since Delivery presents what Assembly produces.

**Epic dependencies.** E06.1 — Consumer-Fit Presentation (must be stable, per epic file §4 and Global Output 3).

**Feature dependencies.** None within this capability; this Feature is the prerequisite foundation for Consumer Type Inventory (F09.3.2) within E09.3.

**External dependencies.** Consumer systems and human or AI consumers, as the general category of external party a future consumer type would represent — referenced only conceptually, not as any specific system.

## 11. Constraints

**Business constraints.** The extension point must be additive; it must never require compromising Context Delivery's boundary as defined in FEP-001 (capability §8).

**Product constraints.** The cost of describing a new consumer type against this point must not grow with the number of consumer types already supported (capability §8).

**Context integrity constraints.** The extension point must not allow a new consumer type, once admitted, to receive context that bypasses Assembly's completeness or currency obligations.

**Trust constraints.** The extension point's definition must not itself satisfy or waive Access Control & Policy obligations — satisfaction is confirmed later, by Extension Admission Criteria (F09.4.1).

**Policy constraints.** The extension point must respect capability boundaries rather than team boundaries (Product Principle P6), and must not create a structurally privileged consumer type (Product Principle P4).

## 12. Acceptance Criteria

1. The extension point is described without reference to any single specific consumer type.
2. At least one existing consumer type and one hypothetical new consumer type can each be described against the extension point using the same definition.
3. Describing a new consumer type against the extension point requires no change to Assembly's or Delivery's documented responsibilities.
4. The extension point's documentation is sufficient for a contributor unfamiliar with it to describe a new consumer type against it without further clarification.

## 13. Validation Requirements

- That the extension point's definition remains unchanged when a second, differently-shaped consumer type is described against it.
- That no other capability's documented responsibilities require modification when a new consumer type is described against the extension point.

## 14. Failure Conditions

- **Special-casing** (capability §10): a consumer type is added by altering Assembly's or Delivery's responsibilities instead of going through this extension point — the system must make this deviation visible as a documented boundary violation, never absorb it silently (Product Principle P5).
- **Extension point rot** (capability §10): the extension point is defined but never actually exercised, so real consumer-type additions bypass it in practice — this must be detectable by comparing any newly supported consumer type against the documented extension point.

## 15. Traceability

Product Vision (Mission) → G3 (Consumer neutrality), G5 (Extensible acquisition and delivery) → Product Principles P1, P4, P5, P6 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.3 (Delivery Extension Points) → Feature F09.3.1 (Consumer Type Extension Point Definition).

## 16. Future Considerations

- A more formal, evaluable process for proposing new consumer types as the ecosystem around Ferret grows, toward FEP-001's Generation 4 / Ecosystem (capability §11).
- Revisiting this extension point's shape once Delivery has handled more than one real consumer type, to confirm it reflects a genuine pattern rather than a single example (epic §7).
- Extending this point to support federation-aware consumer types once Federation is underway (capability §11; epic §8).
