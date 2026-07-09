# FEP-004-SPEC-F03.3.1 — Structural Change Detection & Signaling

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F03.3.1 |
| **Capability** | [FEP-002-CAP-03 — Context Organization](../capabilities/FEP-002-CAP-03-Context-Organization.md) |
| **Epic** | E03.3 — Structural Change Signaling |
| **Feature** | F03.3.1 — Structural Change Detection & Signaling |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-03 — Context Organization](../epics/FEP-003-EPIC-CAP-03-Context-Organization.md); [FEP-002-CAP-03 — Context Organization](../capabilities/FEP-002-CAP-03-Context-Organization.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Context Maintenance judges freshness, but it cannot judge what it is never told about. This specification exists to define how Context Organization detects structural change — a new entity, a changed entity, an added relationship, a broken relationship — resulting from newly organized material, and reliably signals that change onward, without itself deciding what should happen as a result.

## 3. Scope

- Detecting that organizing newly acquired material produced a new entity, a change to an existing entity, a newly identified relationship, or a relationship that no longer holds.
- Emitting a corresponding signal for each detected structural change.
- Making structural change signals available to Context Maintenance and Provenance & Attribution.

## 4. Out of Scope

- Deciding whether the changed structure is current or stale, or what to do in response to the change (invalidation, re-acquisition, re-organization) — belongs entirely to Context Maintenance, per the epic's explicit purpose ("without Organization deciding what to do about it").
- The mechanics of extracting entities (F03.1.1), recognizing their continuity (F03.1.2), or identifying relationships (F03.2.1) — this Feature consumes their outcomes to detect change, it does not perform them.
- Guaranteeing the durable traceability link of structured elements — belongs to F03.2.2.
- Selecting or ranking changed context for a specific consumer request — belongs to Context Assembly.

## 5. Engineering Requirements

1. The system shall detect when organizing material results in a new entity that did not previously exist.
2. The system shall detect when organizing material results in a change to an existing entity, as recognized by entity continuity recognition (F03.1.2).
3. The system shall detect when organizing material results in a new relationship between entities.
4. The system shall detect when a previously represented relationship no longer holds.
5. For every detected structural change, the system shall emit a corresponding signal describing the nature of the change.
6. A structural change signal shall not itself make or imply any freshness or staleness judgment about the change.
7. Every structural change detected shall produce exactly one corresponding signal — no missed changes, and no duplicate signals for the same change.

## 6. Inputs

- Newly organized entities and their continuity status (from F03.1.2).
- Newly identified relationships and their status (from F03.2.1).
- The prior state of the Structured Context Unit, for comparison.

## 7. Outputs

- Structural change signals — one per new entity, changed entity, added relationship, or broken relationship — made available to Context Maintenance and Provenance & Attribution.

## 8. Preconditions

- Entity extraction (F03.1.1), continuity recognition (F03.1.2), and relationship identification (F03.2.1) have completed for the newly acquired material.
- A prior structured state exists for comparison (may be empty on first organization of a workspace).

## 9. Postconditions

- Every structural change arising from the newly organized material has a corresponding signal available to Context Maintenance and Provenance & Attribution.
- Context Organization has taken no action based on the detected change beyond signaling it.

## 10. Dependencies

**Capability dependencies:** Context Maintenance and Provenance & Attribution (downstream signal consumers).

**Epic dependencies:** E03.1 — Entity Extraction; E03.2 — Relationship Modeling (both prerequisite, per epic §5 Execution Order — this epic signals changes to entities and relationships that must already be modeled).

**Feature dependencies:** F03.1.2 — Entity Continuity Recognition; F03.2.1 — Relationship Identification.

**External dependencies:** Change-notification sources (indirectly — this Feature detects change resulting from organization, while the upstream trigger for re-acquisition and re-organization is a Context Maintenance / Context Acquisition concern per FEP-001 §6).

## 11. Constraints

**Business constraints:** Signaling must remain neutral — describing what changed, not prescribing a response (capability §2, Non-Responsibilities — Organization must never decide what is current or stale).

**Product constraints:** Every real structural change must produce a signal; no change may be silently absorbed (capability §9, Success Criteria).

**Context integrity constraints:** Signals must accurately reflect the actual structural change that occurred, not an approximation.

**Trust constraints:** A signal must not overstate or understate the change that actually occurred, consistent with Product Principle P1.

**Policy constraints:** Context Maintenance and Provenance & Attribution must be able to receive the same signal without Organization differentiating between them, per Product Principle P4 (no privileged consumer).

## 12. Acceptance Criteria

1. Every new entity produced by organizing material generates a corresponding "new entity" signal.
2. Every recognized change to an existing entity generates a corresponding "changed entity" signal.
3. Every newly identified relationship generates a corresponding "added relationship" signal.
4. Every relationship that no longer holds generates a corresponding "broken relationship" signal.
5. No structural change signal asserts or implies a freshness or staleness conclusion.
6. Context Maintenance and Provenance & Attribution can each independently receive and consume the same signal without differentiated treatment by Organization.

## 13. Validation Requirements

- Completeness testing: every engineered structural change produces a corresponding signal.
- Accuracy testing: signal content matches the actual change that occurred.
- Non-duplication testing: no structural change produces more than one signal.
- Consumer-neutral delivery testing across Context Maintenance and Provenance & Attribution.

## 14. Failure Conditions

1. **Missed structural change.** A real change occurs but no signal is produced. Context Maintenance's freshness judgment becomes based on incomplete information; this must be treated as a defect and surfaced via Observability & Health, not left undetected.
2. **False structural change signal.** A signal is emitted for a change that did not actually occur. This must be corrected; downstream consumers must never receive a signal describing a change unsupported by the actual organized material.
3. **Signal starvation under high acquisition volume.** Structural change detection cannot keep pace with the rate of newly organized material. This degraded state must be visibly reported, per Product Principle P5, not silently queued indefinitely with no observable backlog.

## 15. Traceability

Product Vision (Mission: keeping context current by making change visible to the capabilities that maintain it) → Goal G2 (Currency of context) → Product Principles P1, P3, P4, P5 → Capability FEP-002-CAP-03 (Context Organization) → Epic E03.3 (Structural Change Signaling) → Feature F03.3.1 (Structural Change Detection & Signaling).

## 16. Future Considerations

- Increasing sophistication in recognizing continuity and change in entities over time, feeding richer signals to Maintenance (capability §11, Future Evolution).
- Signal granularity may need to evolve alongside deeper relationship modeling tied to source categories not yet acquired (epic §8, Deferred Work).
