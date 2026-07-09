# FEP-004-SPEC-F02.1.2 — Source Reachability Tracking

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F02.1.2 |
| **Capability** | [Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md) |
| **Epic** | E02.1 — Source Discovery |
| **Feature** | F02.1.2 — Source Reachability Tracking |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-02 — Context Acquisition](../epics/FEP-003-EPIC-CAP-02-Context-Acquisition.md)<br>[FEP-002-CAP-02 — Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Knowing a source exists is not the same as knowing it can be read right now. This specification exists to track, per discovered source, whether it is currently reachable, enabling partial-failure resilience and honest gap reporting elsewhere in the capability, per the Feature's stated objective and product outcome.

## 3. Scope

- Determining a current reachability state (reachable or unreachable) for every discovered Source.
- Maintaining reachability state per individual Source, independent of any other Source.
- Detecting and making observable any transition in a Source's reachability state.

## 4. Out of Scope

- Discovering sources in the first place — owned by Source Discovery within Scope (F02.1.1).
- Reading the content of a source — owned by Faithful Content Reading (F02.2.1).
- Deciding how to respond to unreachability (e.g., retry timing or scheduling) — belongs to Context Maintenance, not Acquisition.
- Producing the coverage and gap report itself — owned by Coverage & Gap Reporting (F02.3.2), which consumes reachability state as an input.
- Recording acquisition events — owned by Acquisition Event Recording (F02.3.1).

## 5. Engineering Requirements

1. For every discovered Source, Acquisition must be able to determine a current reachability state.
2. Reachability state must be tracked per individual Source, independent of the state of any other Source.
3. A transition in a Source's reachability state must be detectable and observable to other parts of the capability.
4. Determining reachability must not require the full content of the Source to be read.
5. Reachability state must be made available to inform Partial-Failure Resilience (F02.2.2) and Coverage & Gap Reporting (F02.3.2).

## 6. Inputs

- The discovered Source inventory produced by Source Discovery within Scope (F02.1.1).
- The prior reachability state for each Source, where one exists, for detecting transitions.

## 7. Outputs

- A current reachability state for every discovered Source.
- Signals marking a transition in a Source's reachability state.

## 8. Preconditions

- Source Discovery within Scope (F02.1.1) has produced a Source inventory for the workspace.

## 9. Postconditions

- Every Source in the discovered inventory has a known, current reachability state.
- Any change in a Source's reachability since the prior check is observable to the rest of Acquisition.

## 10. Dependencies

**Capability dependencies.** Workspace Definition — indirectly, via the scope that bounds which sources are tracked.

**Epic dependencies.** E02.1 — Source Discovery.

**Feature dependencies.** F02.1.1 — Source Discovery within Scope (prerequisite, per epic file §3).

**External dependencies.** Source systems, as the population whose reachability is being assessed; connectivity/network condition, described only conceptually as a state that may vary independently per source.

## 11. Constraints

**Business constraints.** Reachability tracking applies only to Sources already established as within declared scope.

**Product constraints.** Acquisition must be resilient to partial failure; assessing one Source's reachability must not block or delay assessment of any other Source's reachability (capability §8).

**Context integrity constraints.** Reachability state must reflect the true current condition of the Source, not a stale or assumed state.

**Trust constraints.** Reachability state must be attributable to a point in time so consumers can judge its currency (Product Principle P3).

**Policy constraints.** None beyond the scope constraints already established by Workspace Definition.

## 12. Acceptance Criteria

1. Every discovered Source has an associated, current reachability state.
2. A change in a Source's reachability state (reachable to unreachable, or the reverse) is detectable across successive checks.
3. Assessing the reachability of one Source has no dependency on, or effect on, assessing another Source's reachability.
4. Reachability state for every Source is available for consumption by the reading process and by coverage reporting.

## 13. Validation Requirements

- That a current reachability state exists for every discovered Source.
- That an artificially unreachable source is correctly reflected as unreachable.
- That a reachability check against one Source does not block, delay, or alter the outcome of a concurrent check against another Source.

## 14. Failure Conditions

- **Silent gaps** (capability §10): a Source becomes unreachable but no reachability change is recorded — the resulting state change must be surfaced, never left silent, per Product Principle P5.
- **Acquisition storms** (capability §10): reachability checks against a single Source are excessively frequent — the tracking approach must respect the Source's own constraints, without prescribing a specific check frequency.

## 15. Traceability

Product Vision (Mission) → G2 (Currency of context), G4 (Trustworthy context) → Product Principles P3, P5 → Capability FEP-002-CAP-02 (Context Acquisition) → Epic E02.1 (Source Discovery) → Feature F02.1.2 (Source Reachability Tracking).

## 16. Future Considerations

- The epic's risk that coupling reachability tracking too tightly to reading will make partial-failure resilience harder to reason about later (epic §7) — reachability should remain conceptually distinct from reading as both evolve.
- More precise coverage states building on reachability history (capability §11).
