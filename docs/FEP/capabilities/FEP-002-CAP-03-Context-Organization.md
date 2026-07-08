# FEP-002-CAP-03 — Context Organization

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-03 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.3 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

Raw acquired material is bytes, not context. Context Organization exists to turn "we have the content" into "we understand what this is and how it relates to everything else" — the step that makes structure, relationship, and meaning available for everything downstream.

## 2. Responsibilities

- Extract meaningful entities from raw acquired material — a decision, a component, a person, a requirement — at a conceptual level.
- Identify relationships between extracted entities, and between entities and the Acquisition Units they were derived from.
- Produce structured context that Assembly can select from and Maintenance can track the freshness of.
- Maintain internal consistency as new material is organized, recognizing when a newly organized entity is the same as one already known.
- Preserve traceability from every structured element back to the raw material it was derived from, for Provenance & Attribution.

## 3. Non-Responsibilities

- Must never acquire raw material itself — it consumes what Context Acquisition produces.
- Must never decide what is current or stale — that belongs to Context Maintenance; Organization structures whatever it is given, whenever it is given.
- Must never select or rank structured context for a specific request — that belongs to Context Assembly.
- Must never bias its structuring toward the anticipated needs of one consumer over another — doing so would blur into Assembly and violate Product Principle P4.

## 4. Inputs

- Raw acquired material from Context Acquisition.
- Previously organized structure, used to recognize continuity rather than treating every acquisition as wholly new.

## 5. Outputs

- Structured context: entities and their relationships, traceable back to source material.
- Structural change signals — a new entity, a changed entity, an added or broken relationship — for Context Maintenance and Provenance & Attribution.

## 6. Context Objects

- **Entity** — a conceptually meaningful thing extracted from raw material: a component, a decision, a person, a requirement.
- **Relationship** — a conceptual link between two entities, or between an entity and the raw material it derives from.
- **Structured Context Unit** — the organized, related output for a given scope of raw material, ready for Assembly to draw upon.

## 7. Relationships

Consumes raw material from Context Acquisition. Supplies structured context to Context Assembly. Reports structural change to Context Maintenance, which uses it to judge freshness. Supplies lineage to Provenance & Attribution.

## 8. Constraints

- **Business.** Structuring must stay generic — it organizes what the content actually says, not what any one anticipated consumer wants to hear.
- **Product.** Structuring should be idempotent in spirit: re-organizing the same raw material should not silently produce a materially different structure absent a traceable change in the source.
- **Context integrity.** Every structured element must remain traceable to the raw material that produced it — structure that cannot be traced back to a source is indistinguishable from fabrication.

## 9. Success Criteria

- Structured context correctly reflects the relationships actually present in the raw material.
- A structured entity can always be traced back to the acquisition event(s) that produced it.
- Structuring keeps pace with acquisition rather than becoming a bottleneck that leaves material un-organized indefinitely.

## 10. Failure Modes

- **Entity fragmentation** — the same real-world thing is represented as multiple disconnected entities because continuity wasn't recognized across acquisitions.
- **Entity conflation** — two distinct things are merged into one entity, corrupting the relationships built on top of it.
- **Untraceable structure** — a structured element exists with no recoverable link to source material, directly violating Product Principle P2.
- **Consumer-biased structuring** — structure quietly starts encoding assumptions useful to one class of consumer, narrowing what Assembly can do for others (FEP-001 §8, Risk 5).

## 11. Future Evolution

Deeper relationship modeling as acquired source categories grow. Cross-workspace entity recognition as Federation matures — recognizing that an entity in one workspace is the same as one in another. Increasing sophistication in recognizing continuity and change in entities over time, feeding richer signals to Maintenance.
