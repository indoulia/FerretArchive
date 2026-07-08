# FEP-003-EPIC-CAP-09 — Engineering Program: Extensibility

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-09 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-09 — Extensibility](../capabilities/FEP-002-CAP-09-Extensibility.md) |
| **Status** | Draft — Prompt 3 output (amended per FEP-003A) |
| **Last Updated** | 2026-07-08 |

---

> **Amendment (2026-07-08).** §2–§7 below insert E09.2 — Organization Extension Points, and renumber the former E09.2 (Delivery Extension Points) to E09.3 and the former E09.3 (Extension Governance) to E09.4, restoring the Organization extension surface FEP-001 §2.9 assigns to this capability. Corrected per the Required Correction in [FEP-003A — Engineering Program Review & Freeze](../reviews/FEP-003A-Engineering-Program-Review.md).

## 1. Capability Summary

Extensibility keeps the capability model open to new sources and consumers without requiring the core capabilities to be redesigned every time the world outside Ferret changes. It defines extension points; it never acquires, organizes, assembles, or delivers context itself.

## 2. Engineering Epics

### E09.1 — Acquisition Extension Points

- **Purpose.** Define where new source types attach.
- **Scope.** Defining and documenting the extension point at Context Acquisition's boundary; maintaining a source-type inventory.
- **Success Definition.** A new source type can be evaluated and added without altering Acquisition's, Organization's, Maintenance's, or Assembly's defined responsibilities.

### E09.2 — Organization Extension Points

- **Purpose.** Define where new structure types attach.
- **Scope.** Defining and documenting the extension point at Context Organization's boundary; maintaining a structure-type inventory.
- **Success Definition.** A new structure type can be evaluated and added without altering Organization's, Maintenance's, or Assembly's defined responsibilities.

### E09.3 — Delivery Extension Points

- **Purpose.** Define where new consumer types attach.
- **Scope.** Defining and documenting the extension point at Context Delivery's boundary; maintaining a consumer-type inventory.
- **Success Definition.** A new consumer type can be evaluated and added without altering Assembly's or Delivery's defined responsibilities.

### E09.4 — Extension Governance

- **Purpose.** Ensure new extensions are evaluated for fit and trust-capability compliance before admission.
- **Scope.** Defining admission criteria a proposed extension must meet, including satisfying Provenance and Access Control obligations.
- **Success Definition.** No extension is admitted that bypasses provenance or access control obligations.

## 3. Features

### E09.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F09.1.1 — Source Type Extension Point Definition | Define the conceptual point at which a new source type attaches to Context Acquisition. | Enables source diversity to grow without redesigning Acquisition. | E02.1 stable | A new source type can be described against this extension point without changing Acquisition's own responsibilities. |
| F09.1.2 — Source Type Inventory | Maintain a conceptual, explicit inventory of currently supported source types. | Makes the product's actual acquisition surface knowable rather than implicit. | F09.1.1 | The inventory accurately reflects every source type currently supported and is updated when that changes. |

### E09.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F09.2.1 — Structure Type Extension Point Definition | Define the conceptual point at which a new structure type attaches to Context Organization. | Enables structural diversity to grow without redesigning Organization. | E03.1 stable | A new structure type can be described against this extension point without changing Organization's own responsibilities. |
| F09.2.2 — Structure Type Inventory | Maintain a conceptual, explicit inventory of currently supported structure types. | Makes the product's actual organization surface knowable rather than implicit. | F09.2.1 | The inventory accurately reflects every structure type currently supported and is updated when that changes. |

### E09.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F09.3.1 — Consumer Type Extension Point Definition | Define the conceptual point at which a new consumer type attaches to Context Delivery. | Enables consumer diversity to grow without redesigning Delivery. | E06.1 stable | A new consumer type can be described against this extension point without changing Delivery's own responsibilities. |
| F09.3.2 — Consumer Type Inventory | Maintain a conceptual, explicit inventory of currently supported consumer types. | Makes the product's actual delivery surface knowable rather than implicit. | F09.3.1 | The inventory accurately reflects every consumer type currently supported. |

### E09.4 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F09.4.1 — Extension Admission Criteria | Define the criteria a proposed source, structure, or consumer type must meet to be admitted, including trust-capability compliance. | Prevents the "trust bypass" failure mode. | F09.1.1, F09.2.1, F09.3.1, F07.3.1, F08.1.1 | A proposed extension can be checked against explicit, written criteria, and a deliberately non-compliant proposal is correctly rejected. |

## 4. Engineering Dependencies

- **Prerequisite Features.** None strictly blocking, but meaningfully depends on E02.1, E03.1, and E06.1 existing in stable form, and on E07.3/E08.1 for governance.
- **Prerequisite Epics.** E02.1 (Source Discovery), E03.1 (Entity Extraction), E06.1 (Consumer-Fit Presentation).
- **Prerequisite Capabilities.** Context Acquisition, Context Organization, Context Delivery — loosely; Extensibility can be planned in parallel with these, but its extension points cannot be finalized until the capabilities they extend are stable.

## 5. Execution Order

1. **E09.1**, **E09.2**, and **E09.3** can proceed in parallel, since they extend different capabilities.
2. **E09.4** — sequenced last, since it depends on all three extension points existing and on Provenance and Access Control having their own foundational epics in place to check against.

## 6. Capability Completion Gates

- **Functional completeness.** At least one new source type, one new structure type, and one new consumer type can each be described and evaluated purely against the defined extension points, without touching any other capability's defined responsibilities.
- **Validation readiness.** A deliberately non-compliant proposed extension is correctly rejected by Extension Admission Criteria.
- **Documentation readiness.** All three inventories and the admission criteria are documented clearly enough for a future contributor to self-serve an evaluation.
- **Review completion.** FEP-002-CAP-09's non-responsibilities (no acquiring/organizing/assembling/delivering, no special-casing) confirmed unviolated.

## 7. Risks

- **Extension points defined too early are guesses.** Defining these extension points before Acquisition, Organization, and Delivery have each handled more than one real source, structure, or consumer type risks extension points shaped around a single example rather than a genuine pattern.
- **Governance criteria without enforcement teeth.** If Extension Admission Criteria is planned as a checklist with no defined consequence for failing it, the trust-bypass risk it exists to prevent is not actually mitigated at the planning level.
- **Inventory drift.** Both inventories are only useful if kept current; planning them as one-time deliverables rather than living artifacts risks them becoming inaccurate documentation rather than a real reflection of the product's extensibility surface.

## 8. Deferred Work

- A formal third-party extension process — deferred to Generation 4 / Ecosystem.
- Federation-aware extension points — deferred until Federation itself is underway.
