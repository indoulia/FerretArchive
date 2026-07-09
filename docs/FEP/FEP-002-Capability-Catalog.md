# FEP-002 — Ferret Capability Catalog

| Field | Value |
|---|---|
| **Document ID** | FEP-002 |
| **Version** | 1.0 |
| **Status** | Draft — Prompt 2 output |
| **Program** | Ferret Engineering Program (FEP) |
| **Authoritative Source** | [FEP-001 — Product Architecture](FEP-001-Product-Architecture.md) |
| **Last Updated** | 2026-07-08 |

---

## Purpose and Standing

FEP-001 is complete and frozen. Its product vision, capability hierarchy, capability boundaries, and product principles are authoritative and are not modified, restated with different meaning, or reopened by this document. This document exists to expand each of the eleven capabilities FEP-001 defined into a complete, independently understandable capability definition.

This is still Product Architecture. It is not Engineering Design and not Implementation Planning. No capability definition in this catalog introduces an API, a class, a database, a protocol, a storage mechanism, or any other implementation decision. Every "Context Object" identified below is a concept the product reasons about, not a schema. Where an implementation-adjacent term appears (for example, in a constraint or failure mode), it is there to name a product-level concern, not to prescribe how that concern is addressed in code.

Each capability's full definition lives in its own file under [`capabilities/`](capabilities/), so that each one is independently readable without needing this index or FEP-001 open at the same time — though both remain the definitive context for interpreting any individual entry.

---

## Catalog

| # | Capability | Group (FEP-001 §3) | Detail |
|---|---|---|---|
| 1 | Workspace Definition | Foundation | [FEP-002-CAP-01](capabilities/FEP-002-CAP-01-Workspace-Definition.md) |
| 2 | Context Acquisition | Context Supply Chain | [FEP-002-CAP-02](capabilities/FEP-002-CAP-02-Context-Acquisition.md) |
| 3 | Context Organization | Context Supply Chain | [FEP-002-CAP-03](capabilities/FEP-002-CAP-03-Context-Organization.md) |
| 4 | Context Maintenance | Context Supply Chain | [FEP-002-CAP-04](capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| 5 | Context Assembly | Context Supply Chain | [FEP-002-CAP-05](capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| 6 | Context Delivery | Context Supply Chain | [FEP-002-CAP-06](capabilities/FEP-002-CAP-06-Context-Delivery.md) |
| 7 | Provenance & Attribution | Trust Capabilities | [FEP-002-CAP-07](capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| 8 | Access Control & Policy | Trust Capabilities | [FEP-002-CAP-08](capabilities/FEP-002-CAP-08-Access-Control-Policy.md) |
| 9 | Extensibility | Platform Capabilities | [FEP-002-CAP-09](capabilities/FEP-002-CAP-09-Extensibility.md) |
| 10 | Observability & Health | Platform Capabilities | [FEP-002-CAP-10](capabilities/FEP-002-CAP-10-Observability-Health.md) |
| 11 | Federation | Scale Capability | [FEP-002-CAP-11](capabilities/FEP-002-CAP-11-Federation.md) |

Each detail document follows the same eleven-section structure: Purpose, Responsibilities, Non-Responsibilities, Inputs, Outputs, Context Objects, Relationships, Constraints, Success Criteria, Failure Modes, and Future Evolution.

---

## Review

This catalog was checked against FEP-001 on the following points before being recorded as complete:

- **Responsibilities do not overlap.** Every capability's Responsibilities section is mirrored by a Non-Responsibilities section in every capability it borders, so that each boundary is stated from both sides — for example, Context Organization's Non-Responsibilities explicitly disclaims ranking for a request, and Context Assembly's Responsibilities explicitly claims it.
- **Capability boundaries remain consistent with FEP-001.** Every Purpose and Responsibilities section is a direct elaboration of the corresponding capability's entry in FEP-001 §2; none introduces a responsibility FEP-001 did not already assign, and none narrows a responsibility FEP-001 already assigned.
- **No implementation decisions were introduced.** No detail document names a technology, a data structure, a protocol, or a storage mechanism. Inputs, Outputs, and Context Objects are described only at the level of what information exists conceptually, per the prompt's instruction.
- **No engineering design was performed.** No detail document specifies how a capability does its work internally — only what it is responsible for, what crosses its boundary, and how it relates to its neighbors.
- **No APIs or storage were defined.** Confirmed by inspection of every Inputs/Outputs section: each describes information conceptually ("a request describing what context is needed") rather than as a contract or shape.
- **Context OS principles remain intact.** Every capability's Constraints section traces back to one or more of FEP-001's Product Principles (P1–P6) or Goals (G1–G6), and Product Boundary discipline (FEP-001 §5) is preserved — no capability definition here assigns Ferret a responsibility FEP-001 placed outside the product (reasoning, generation, process enforcement, action-taking).

---

## Deliverable Boundary

This catalog is the authoritative capability reference for all future Ferret engineering planning. It does not proceed into Epics, Features, or Engineering Specifications — those are later, separate FEP prompts, populating [`epics/`](epics/) and [`specifications/`](specifications/) respectively, once issued.

---

## Cross References

| Document | Relationship |
|---|---|
| [FEP-001-Product-Architecture.md](FEP-001-Product-Architecture.md) | Authoritative source for every capability's boundary, hierarchy, and dependency relationships expanded here |
| [FEP-000-Roadmap.md](FEP-000-Roadmap.md) | Program roadmap recording this document as the Prompt 2 output |

---

## Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | 2026-07-08 | Initial Capability Catalog — FEP Prompt 2 output; all eleven FEP-001 capabilities expanded |
