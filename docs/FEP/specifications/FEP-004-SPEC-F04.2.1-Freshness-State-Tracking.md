# FEP-004-SPEC-F04.2.1 — Freshness State Tracking

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F04.2.1 |
| **Capability** | [Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Epic** | E04.2 — Freshness Accounting |
| **Feature** | F04.2.1 — Freshness State Tracking |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-04 — Context Maintenance](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md); [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists to define how current, stale, or unknown state and age are tracked for every unit of context, so that Context Assembly has the eligibility information it needs to avoid serving stale context as current — this Feature's stated Objective and Product Outcome.

## 3. Scope

- Maintaining a resolvable freshness state — current, stale, or unknown — for every unit of context.
- Maintaining an associated age (time since last confirmed current) for every unit of context.
- Updating freshness state and age in response to consumed Change Signals (source-level, from F04.1.1; structural or scope, from F04.1.2).
- Ensuring "unknown" is represented as a distinct, honest state, never collapsed into or defaulted to "current."
- Ensuring state and age remain resolvable even when a unit's underlying source is temporarily unreachable.

## 4. Out of Scope

- Applying a workspace-specific freshness expectation or threshold to determine staleness (F04.2.2).
- Detecting change itself, whether source-level or structural/scope (E04.1).
- Triggering re-acquisition or re-organization (F04.3.1).
- Invalidating context that is no longer valid (F04.3.2).
- Deciding what to deliver to a specific consumer request (Context Assembly's responsibility).
- Recording the full provenance lineage of a context unit beyond its freshness fact (Provenance & Attribution's responsibility).

## 5. Engineering Requirements

1. Every unit of context recognized by Ferret must have a resolvable freshness state at all times: current, stale, or unknown.
2. Every unit of context must have an associated age, queryable alongside its state.
3. "Unknown" must be represented as a state distinct from "current" and "stale," and must never be defaulted to "current."
4. Freshness state must update in response to relevant consumed Change Signals without manual intervention.
5. A context unit that has never been checked must resolve to "unknown," not to an assumed default state.
6. Freshness state and age must remain resolvable for a context unit even when its underlying source is temporarily unreachable.

## 6. Inputs

- Change Signals consumed via Source Change Detection (F04.1.1) and Structural & Scope Change Consumption (F04.1.2).
- The existing freshness state and age of a context unit, where previously established.

## 7. Outputs

- A resolvable Freshness State (current, stale, or unknown) and age for every unit of context, exposed to Context Assembly and to Provenance & Attribution.

## 8. Preconditions

- Source Change Detection (F04.1.1) exists and can supply source-level Change Signals.
- Structural & Scope Change Consumption (F04.1.2) exists and can supply structural and scope-derived signals.
- A context unit exists, having been produced by Context Organization, that can be tracked.

## 9. Postconditions

- Any query for a given context unit's freshness returns a resolvable state and age.
- No context unit in a workspace lacks a resolvable freshness state.
- A context unit affected by a consumed Change Signal reflects an updated state.

## 10. Dependencies

**Capability dependencies.** Context Organization (source of the context units being tracked); Context Assembly (consumer of freshness state for eligibility); Provenance & Attribution (consumer of freshness facts for lineage).

**Epic dependencies.** E04.1 — Change Detection.

**Feature dependencies.** F04.1.1 — Source Change Detection; F04.1.2 — Structural & Scope Change Consumption.

**External dependencies.** None directly; freshness state is derived entirely from signals already consumed within Context Maintenance.

## 11. Constraints

**Business constraints.** None specific to this Feature; workspace-specific thresholds are F04.2.2's concern, not this Feature's state representation.

**Product constraints.** Maintenance must never present unconfirmed context as confirmed-current; when currency cannot be determined, the honest state is "unknown," never an assumed default (P3).

**Context integrity constraints.** State and age must be attributable per context unit; approximated or shared state across unrelated units would misrepresent currency.

**Trust constraints.** Freshness facts feed Provenance & Attribution and are visible to every consumer equally; state accuracy is a precondition for trust (P2, P4).

**Policy constraints.** None.

## 12. Acceptance Criteria

1. Every context unit's freshness state resolves to exactly one of current, stale, or unknown at any point in time.
2. A context unit with no prior check resolves to "unknown," never to "current."
3. A relevant consumed Change Signal results in an updated freshness state for the affected context unit within a bounded, observable interval.
4. Age is queryable for any context unit alongside its freshness state.
5. A context unit whose source is currently unreachable is never reported as "current."

## 13. Validation Requirements

- That every context unit in a workspace resolves to a freshness state, with none left unresolved.
- That "unknown" is reachable and distinguishable from "current" and "stale" in practice, not merely in definition.
- That age is measured against last-confirmed-current time accurately.
- That state updates correctly follow consumed Change Signals, including under concurrent signal arrival.

## 14. Failure Conditions

- **Silent staleness.** Context ages past any reasonable expectation without this being reflected anywhere visible. Expected behavior: state must transition to "stale" observably, never remain silently "current" (P3, P5).
- **Freshness blindness (partial).** If no workspace expectation yet exists to compare age against (F04.2.2's concern), state tracking must still report state and age honestly, using "unknown" where appropriate, rather than fabricating currency.

## 15. Traceability

Product Vision (Mission: maintain context currency) → Goals G2 (Currency of context), G4 (Trustworthy context) → Product Principles P3 (Freshness first-class), P2 (Provenance mandatory) → Capability FEP-002-CAP-04 (Context Maintenance) → Epic E04.2 (Freshness Accounting) → Feature F04.2.1 (Freshness State Tracking).

## 16. Future Considerations

- Predictive staleness — anticipating that certain context is likely to age based on historical change patterns — is deferred pending real change-pattern data, and would extend this Feature's state model rather than replace it.
- More granular, source-specific freshness cadences (some sources near-real-time, others daily) are anticipated as a future evolution of the capability, building on the state and age this Feature tracks.
