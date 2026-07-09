# FEP-003-EPIC-CAP-03 — Engineering Program: Context Organization

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-03 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-03 — Context Organization](../capabilities/FEP-002-CAP-03-Context-Organization.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Context Organization converts raw acquired material into structured, related, queryable context — extracting entities and relationships and preserving traceability back to source. It does not decide freshness or relevance; it structures whatever it is given.

## 2. Engineering Epics

### E03.1 — Entity Extraction

- **Purpose.** Identify meaningful entities within raw acquired material.
- **Scope.** Recognizing entities (components, decisions, people, requirements) from raw material; recognizing continuity of an entity across acquisitions.
- **Success Definition.** Entities present in raw material are consistently and correctly recognized as the same entity across time, without fragmentation or conflation.

### E03.2 — Relationship Modeling

- **Purpose.** Identify relationships between entities and between entities and their source material.
- **Scope.** Relationship identification between entities; traceability from structure to raw material.
- **Success Definition.** Relationships present in raw material are represented, and every structured element is traceable to source.

### E03.3 — Structural Change Signaling

- **Purpose.** Report changes in structure to Maintenance and Provenance.
- **Scope.** Detecting and signaling when an entity appears, changes, or a relationship is added or broken.
- **Success Definition.** Every structural change is signaled to dependent capabilities without Organization deciding what to do about it.

## 3. Features

### E03.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F03.1.1 — Entity Extraction | Recognize meaningful entities within a given Acquisition Unit. | Raw material becomes structured entities Assembly can eventually draw upon. | F02.2.1 | Entities present in raw material are extracted, each traceable to its source Acquisition Unit. |
| F03.1.2 — Entity Continuity Recognition | Recognize when a newly extracted entity is the same as one already known. | Prevents entity fragmentation as sources are re-acquired over time. | F03.1.1 | Re-acquiring unchanged or lightly changed material does not produce duplicate entities for the same real-world thing. |

### E03.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F03.2.1 — Relationship Identification | Identify relationships between recognized entities. | Structured context reflects how things relate, enabling coherent Assembly. | F03.1.1, F03.1.2 | Relationships actually present in raw material are represented as such. |
| F03.2.2 — Traceability Preservation | Preserve a link from every structured element back to its raw material. | Satisfies Provenance & Attribution's dependency on structural lineage. | F03.1.1, F03.2.1 | No structured element exists without a resolvable link to its originating Acquisition Unit(s). |

### E03.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F03.3.1 — Structural Change Detection & Signaling | Detect and signal structural change resulting from newly organized material. | Context Maintenance can judge freshness based on actual structural change. | F03.1.2, F03.2.1 | A structural change (new/changed entity, added/broken relationship) reliably produces a corresponding signal. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F02.2.1 (Faithful Content Reading).
- **Prerequisite Epics.** E02.2 (Content Reading & Preservation).
- **Prerequisite Capabilities.** Context Acquisition.

## 5. Execution Order

1. **E03.1** — nothing can be related before entities exist.
2. **E03.2** — depends on entities and their continuity being recognized.
3. **E03.3** — depends on both prior epics, since it signals changes to entities and relationships that must already be modeled.

## 6. Capability Completion Gates

- **Functional completeness.** Entities and relationships actually present in acquired material are represented in structured context for every supported source category.
- **Validation readiness.** Re-acquiring the same or lightly modified material does not fragment or conflate entities.
- **Documentation readiness.** The distinction between an entity, a relationship, and a Structured Context Unit is documented clearly enough for Assembly's authors to consume without ambiguity.
- **Review completion.** FEP-002-CAP-03's non-responsibilities (no freshness decisions, no request-specific ranking, no consumer-biased structuring) confirmed unviolated.

## 7. Risks

- **Entity model instability.** "Meaningful entity" has no fixed, closed definition; early planning may under- or over-scope it, requiring rework as more source categories are acquired.
- **Continuity recognition complexity underestimated at planning time.** Recognizing "this is the same entity as before" is deceptively easy to state and historically hard to scope correctly; treating it as a small feature risks it silently absorbing most of the epic's real difficulty.
- **Consumer bias creeping into planning.** If relationship-modeling features are drafted with a specific anticipated consumer in mind, this violates the capability's boundary before any implementation even begins.

## 8. Deferred Work

- Cross-workspace entity recognition — deferred until Federation is underway.
- Deeper relationship modeling tied to source categories not yet acquired — deferred until those categories are prioritized in Context Acquisition.
