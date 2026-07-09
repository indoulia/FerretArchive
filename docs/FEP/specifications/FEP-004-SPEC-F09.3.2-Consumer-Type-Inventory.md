# FEP-004-SPEC-F09.3.2 — Consumer Type Inventory

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.3.2 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.3 — Delivery Extension Points |
| **Feature** | F09.3.2 — Consumer Type Inventory |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output (renumbered from F09.2.2 per FEP-003A) |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that the set of consumer types Context Delivery currently knows how to serve is explicit and knowable, satisfying the Feature's objective of maintaining a conceptual, explicit inventory of currently supported consumer types — the Product Outcome of making the product's actual delivery surface knowable rather than implicit in whatever happens to have been built.

## 3. Scope

- Maintaining a conceptual list of every consumer type currently supported by Context Delivery.
- Recording when a new consumer type is admitted and added to the inventory.
- Recording when a previously supported consumer type is retired or removed from the inventory.
- Keeping the inventory current as a living artifact, not a one-time deliverable.

## 4. Out of Scope

- Defining the extension point a consumer type is described and evaluated against — owned by Consumer Type Extension Point Definition (F09.3.1).
- Deciding whether a proposed consumer type should be admitted — owned by Extension Admission Criteria (F09.4.1).
- Serving or presenting context to any specific consumer instance of an already-supported consumer type — owned by Context Delivery (FEP-002-CAP-06); this Feature inventories consumer *types*, not individual served consumers.
- Maintaining the analogous inventories of source types or structure types — owned by Source Type Inventory (F09.1.2) and Structure Type Inventory (F09.2.2).

## 5. Engineering Requirements

1. The inventory must list every consumer type currently supported by Context Delivery.
2. The inventory must be updated to add a consumer type once that type has been admitted (per F09.4.1).
3. The inventory must be updated to remove or mark retired a consumer type that ceases to be supported.
4. The inventory must be maintained continuously, not produced as a one-time snapshot.
5. The inventory must be reviewable by a contributor without requiring inspection of Delivery's internal implementation.

## 6. Inputs

- The outcome of an extension admission decision (F09.4.1) for a proposed consumer type.
- A decision to retire a previously supported consumer type.

## 7. Outputs

- A current, explicit inventory of supported consumer types.

## 8. Preconditions

- The Consumer Type Extension Point Definition (F09.3.1) exists, so that inventoried consumer types are each ones that were described and evaluated against a common point.

## 9. Postconditions

- The inventory accurately reflects every consumer type currently supported.
- The inventory is updated whenever a consumer type is admitted or retired.

## 10. Dependencies

**Capability dependencies.** Context Delivery — the capability whose supported consumer types are inventoried.

**Epic dependencies.** E09.3 — Delivery Extension Points.

**Feature dependencies.** F09.3.1 — Consumer Type Extension Point Definition (prerequisite, per epic file §3).

**External dependencies.** None beyond the consumer systems already named conceptually in F09.3.1; this Feature records categories, not systems.

## 11. Constraints

**Business constraints.** The inventory must never represent a consumer type as supported unless it has actually been admitted through the extension point and admission criteria (capability §8, business constraint).

**Product constraints.** Maintaining the inventory must not become proportionally harder as the number of supported consumer types grows (capability §8, product constraint).

**Context integrity constraints.** The inventory itself must be accurate and current — an inventory that lags reality misrepresents the product's actual delivery surface.

**Trust constraints.** Inventory entries must be attributable to when a consumer type was admitted or retired (Product Principle P2), and the inventory must not imply preferential treatment of any listed consumer type over another (Product Principle P4).

**Policy constraints.** None beyond honoring, not re-deciding, admission outcomes produced by Extension Admission Criteria (F09.4.1).

## 12. Acceptance Criteria

1. The inventory lists every consumer type currently supported by Context Delivery, with none omitted.
2. The inventory contains no consumer type that has not been admitted through the defined extension point and admission criteria.
3. When a new consumer type is admitted, the inventory reflects it without requiring inspection of Delivery's implementation.
4. When a supported consumer type is retired, the inventory reflects its removal or retired status.

## 13. Validation Requirements

- That the inventory's contents match the set of consumer types Delivery actually supports at a given point in time.
- That an admission event and a retirement event are each reflected in the inventory.

## 14. Failure Conditions

- **Inventory drift** (epic §7): the inventory falls out of date relative to what Delivery actually supports — this must be detectable by comparing the inventory against Delivery's current behavior, and the discrepancy must be surfaced, never left as silently inaccurate documentation (Product Principle P5).
- **Unbounded extension surface** (capability §10): a consumer type appears in the inventory without having passed through admission criteria — this must be treated as a reportable inconsistency, not accepted as valid.

## 15. Traceability

Product Vision (Mission) → G3 (Consumer neutrality), G5 (Extensible acquisition and delivery) → Product Principles P1, P4, P5 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.3 (Delivery Extension Points) → Feature F09.3.2 (Consumer Type Inventory).

## 16. Future Considerations

- Expansion of the inventory's scope as recognized consumer categories grow (capability §11).
- A more formal, evaluable process for proposing and admitting new consumer types, feeding directly into how this inventory grows (epic §8; capability §11).
- Treating inventory currency itself as a monitored property once Observability & Health's foundational epics are in place, guarding against the inventory-drift risk named in the epic's risk register (epic §7).
