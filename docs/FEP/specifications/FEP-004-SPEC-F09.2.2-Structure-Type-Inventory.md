# FEP-004-SPEC-F09.2.2 — Structure Type Inventory

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.2.2 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.2 — Organization Extension Points |
| **Feature** | F09.2.2 — Structure Type Inventory |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output (added per FEP-003A Required Correction) |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that the set of structure types Context Organization currently knows how to extract or relate is explicit and knowable, satisfying the Feature's objective of maintaining a conceptual, explicit inventory of currently supported structure types — the Product Outcome of making the product's actual organization surface knowable rather than implicit in whatever happens to have been built.

## 3. Scope

- Maintaining a conceptual list of every structure type currently supported by Context Organization.
- Recording when a new structure type is admitted and added to the inventory.
- Recording when a previously supported structure type is retired or removed from the inventory.
- Keeping the inventory current as a living artifact, not a one-time deliverable.

## 4. Out of Scope

- Defining the extension point a structure type is described and evaluated against — owned by Structure Type Extension Point Definition (F09.2.1).
- Deciding whether a proposed structure type should be admitted — owned by Extension Admission Criteria (F09.4.1).
- Extracting instances of a given, already-supported structure type from acquired content — owned by Entity Extraction (F03.1.1) and Relationship Identification (F03.2.1); this Feature inventories structure *types*, not extracted structure *instances*.
- Maintaining the analogous inventories of source types or consumer types — owned by Source Type Inventory (F09.1.2) and Consumer Type Inventory (F09.3.2).

## 5. Engineering Requirements

1. The inventory must list every structure type currently supported by Context Organization.
2. The inventory must be updated to add a structure type once that type has been admitted (per F09.4.1).
3. The inventory must be updated to remove or mark retired a structure type that ceases to be supported.
4. The inventory must be maintained continuously, not produced as a one-time snapshot.
5. The inventory must be reviewable by a contributor without requiring inspection of Organization's internal implementation.

## 6. Inputs

- The outcome of an extension admission decision (F09.4.1) for a proposed structure type.
- A decision to retire a previously supported structure type.

## 7. Outputs

- A current, explicit inventory of supported structure types.

## 8. Preconditions

- The Structure Type Extension Point Definition (F09.2.1) exists, so that inventoried structure types are each ones that were described and evaluated against a common point.

## 9. Postconditions

- The inventory accurately reflects every structure type currently supported.
- The inventory is updated whenever a structure type is admitted or retired.

## 10. Dependencies

**Capability dependencies.** Context Organization — the capability whose supported structure types are inventoried.

**Epic dependencies.** E09.2 — Organization Extension Points.

**Feature dependencies.** F09.2.1 — Structure Type Extension Point Definition (prerequisite, per epic file §3).

**External dependencies.** None — structure types are an internal organizational concept, not tied to any specific external system category.

## 11. Constraints

**Business constraints.** The inventory must never represent a structure type as supported unless it has actually been admitted through the extension point and admission criteria (capability §8, business constraint).

**Product constraints.** Maintaining the inventory must not become proportionally harder as the number of supported structure types grows (capability §8, product constraint).

**Context integrity constraints.** The inventory itself must be accurate and current — an inventory that lags reality misrepresents the product's actual organization surface, undermining Completeness of context (G1).

**Trust constraints.** None beyond the general requirement that inventory entries be attributable to when a structure type was admitted or retired (Product Principle P2).

**Policy constraints.** None beyond honoring, not re-deciding, admission outcomes produced by Extension Admission Criteria (F09.4.1).

## 12. Acceptance Criteria

1. The inventory lists every structure type currently supported by Context Organization, with none omitted.
2. The inventory contains no structure type that has not been admitted through the defined extension point and admission criteria.
3. When a new structure type is admitted, the inventory reflects it without requiring inspection of Organization's implementation.
4. When a supported structure type is retired, the inventory reflects its removal or retired status.

## 13. Validation Requirements

- That the inventory's contents match the set of structure types Organization actually supports at a given point in time.
- That an admission event and a retirement event are each reflected in the inventory.

## 14. Failure Conditions

- **Inventory drift** (epic §7): the inventory falls out of date relative to what Organization actually supports — this must be detectable by comparing the inventory against Organization's current behavior, and the discrepancy must be surfaced, never left as silently inaccurate documentation (Product Principle P5).
- **Unbounded extension surface** (capability §10): a structure type appears in the inventory without having passed through admission criteria — this must be treated as a reportable inconsistency, not accepted as valid.

## 15. Traceability

Product Vision (Mission) → G1 (Completeness of context), G5 (Extensible acquisition and delivery) → Product Principles P1, P5 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.2 (Organization Extension Points) → Feature F09.2.2 (Structure Type Inventory).

## 16. Future Considerations

- Expansion of the inventory's scope as recognized structure categories grow (capability §11).
- A more formal, evaluable process for proposing and admitting new structure types, feeding directly into how this inventory grows (epic §8; capability §11).
- Treating inventory currency itself as a monitored property once Observability & Health's foundational epics are in place, guarding against the inventory-drift risk named in the epic's risk register (epic §7).
