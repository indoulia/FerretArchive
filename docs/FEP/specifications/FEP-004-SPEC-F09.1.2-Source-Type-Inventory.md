# FEP-004-SPEC-F09.1.2 — Source Type Inventory

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F09.1.2 |
| **Capability** | [Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Epic** | E09.1 — Acquisition Extension Points |
| **Feature** | F09.1.2 — Source Type Inventory |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-09 — Extensibility](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)<br>[FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists so that the set of source types Context Acquisition currently knows how to observe is explicit and knowable, satisfying the Feature's objective of maintaining a conceptual, explicit inventory of currently supported source types — the Product Outcome of making the product's actual acquisition surface knowable rather than implicit in whatever happens to have been built.

## 3. Scope

- Maintaining a conceptual list of every source type currently supported by Context Acquisition.
- Recording when a new source type is admitted and added to the inventory.
- Recording when a previously supported source type is retired or removed from the inventory.
- Keeping the inventory current as a living artifact, not a one-time deliverable.

## 4. Out of Scope

- Defining the extension point a source type is described and evaluated against — owned by Source Type Extension Point Definition (F09.1.1).
- Deciding whether a proposed source type should be admitted — owned by Extension Admission Criteria (F09.3.1).
- Discovering instances of sources within a workspace's declared scope for a given, already-supported source type — owned by Source Discovery within Scope (F02.1.1); this Feature inventories source *types*, not discovered source *instances*.
- Maintaining the analogous inventory of consumer types — owned by Consumer Type Inventory (F09.2.2).

## 5. Engineering Requirements

1. The inventory must list every source type currently supported by Context Acquisition.
2. The inventory must be updated to add a source type once that type has been admitted (per F09.3.1).
3. The inventory must be updated to remove or mark retired a source type that ceases to be supported.
4. The inventory must be maintained continuously, not produced as a one-time snapshot.
5. The inventory must be reviewable by a contributor without requiring inspection of Acquisition's internal implementation.

## 6. Inputs

- The outcome of an extension admission decision (F09.3.1) for a proposed source type.
- A decision to retire a previously supported source type.

## 7. Outputs

- A current, explicit inventory of supported source types.

## 8. Preconditions

- The Source Type Extension Point Definition (F09.1.1) exists, so that inventoried source types are each ones that were described and evaluated against a common point.

## 9. Postconditions

- The inventory accurately reflects every source type currently supported.
- The inventory is updated whenever a source type is admitted or retired.

## 10. Dependencies

**Capability dependencies.** Context Acquisition — the capability whose supported source types are inventoried.

**Epic dependencies.** E09.1 — Acquisition Extension Points.

**Feature dependencies.** F09.1.1 — Source Type Extension Point Definition (prerequisite, per epic file §3).

**External dependencies.** None beyond the source systems already named conceptually in F09.1.1; this Feature records categories, not systems.

## 11. Constraints

**Business constraints.** The inventory must never represent a source type as supported unless it has actually been admitted through the extension point and admission criteria (capability §8, business constraint).

**Product constraints.** Maintaining the inventory must not become proportionally harder as the number of supported source types grows (capability §8, product constraint).

**Context integrity constraints.** The inventory itself must be accurate and current — an inventory that lags reality misrepresents the product's actual acquisition surface, undermining Completeness of context (G1).

**Trust constraints.** None beyond the general requirement that inventory entries be attributable to when a source type was admitted or retired (Product Principle P2).

**Policy constraints.** None beyond honoring, not re-deciding, admission outcomes produced by Extension Admission Criteria (F09.3.1).

## 12. Acceptance Criteria

1. The inventory lists every source type currently supported by Context Acquisition, with none omitted.
2. The inventory contains no source type that has not been admitted through the defined extension point and admission criteria.
3. When a new source type is admitted, the inventory reflects it without requiring inspection of Acquisition's implementation.
4. When a supported source type is retired, the inventory reflects its removal or retired status.

## 13. Validation Requirements

- That the inventory's contents match the set of source types Acquisition actually supports at a given point in time.
- That an admission event and a retirement event are each reflected in the inventory.

## 14. Failure Conditions

- **Inventory drift** (epic §7): the inventory falls out of date relative to what Acquisition actually supports — this must be detectable by comparing the inventory against Acquisition's current behavior, and the discrepancy must be surfaced, never left as silently inaccurate documentation (Product Principle P5).
- **Unbounded extension surface** (capability §10): a source type appears in the inventory without having passed through admission criteria — this must be treated as a reportable inconsistency, not accepted as valid.

## 15. Traceability

Product Vision (Mission) → G1 (Completeness of context), G5 (Extensible acquisition and delivery) → Product Principles P1, P5 → Capability FEP-002-CAP-09 (Extensibility) → Epic E09.1 (Acquisition Extension Points) → Feature F09.1.2 (Source Type Inventory).

## 16. Future Considerations

- Expansion of the inventory's scope as recognized source categories grow (capability §11; FEP-001 Open Question 6).
- A more formal, evaluable process for proposing and admitting new source types, feeding directly into how this inventory grows (epic §8; capability §11).
- Treating inventory currency itself as a monitored property once Observability & Health's foundational epics are in place, guarding against the inventory-drift risk named in the epic's risk register (epic §7).
