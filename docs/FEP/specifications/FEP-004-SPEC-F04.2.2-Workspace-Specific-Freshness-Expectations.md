# FEP-004-SPEC-F04.2.2 — Workspace-Specific Freshness Expectations

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F04.2.2 |
| **Capability** | [Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Epic** | E04.2 — Freshness Accounting |
| **Feature** | F04.2.2 — Workspace-Specific Freshness Expectations |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-04 — Context Maintenance](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md); [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists to define how a workspace's declared freshness expectation is applied when judging staleness, so that freshness judgments respect what each workspace has actually asked for rather than one fixed standard applied everywhere — this Feature's stated Objective and Product Outcome.

## 3. Scope

- Resolving a workspace's declared freshness expectation for a specific source or scope, as configured via Workspace Definition.
- Applying the resolved expectation as the threshold against which a context unit's tracked age (F04.2.1) is judged stale versus current.
- Producing different staleness judgments for different workspaces subjected to equivalent underlying change patterns, in proportion to their differing declared expectations.
- Resolving the explicit condition of "no expectation declared" when a workspace has not configured one.
- Reflecting a change to a workspace's declared expectation in that workspace's subsequent staleness judgments.

## 4. Out of Scope

- Declaring, storing, or configuring the freshness expectation itself (Workspace Definition's responsibility, F01.2.2).
- Tracking the raw freshness state and age of a context unit (F04.2.1).
- Detecting change of any kind (E04.1).
- Triggering re-acquisition, re-organization, or invalidation (E04.3).
- Defining a product-wide default expectation intended to override or substitute for a workspace's own declaration.

## 5. Engineering Requirements

1. The capability must be able to resolve the freshness expectation a specific workspace has declared for a specific source or scope.
2. Staleness judgments must be computed by applying the resolved workspace expectation to a context unit's tracked age, not a single fixed threshold applied uniformly.
3. Two workspaces with different declared expectations, subjected to equivalent change patterns, must produce different staleness judgments.
4. When a workspace has not declared an expectation, the capability must resolve this as an explicit "no expectation declared" condition rather than substituting an unstated default.
5. A change to a workspace's declared expectation must be reflected in that workspace's subsequent staleness judgments without requiring re-processing of other, unrelated workspaces.

## 6. Inputs

- A context unit's tracked freshness state and age (from F04.2.1).
- The workspace's declared freshness expectation, or its explicit absence (from Workspace Definition / F01.2.2).

## 7. Outputs

- A staleness judgment (current or stale, given the specific workspace's expectation) for a context unit, supplementing the raw state produced by F04.2.1.

## 8. Preconditions

- Freshness State Tracking (F04.2.1) exists and produces resolvable age per context unit.
- Workspace Configuration Management (F01.2.2) exists and can resolve a declared expectation, or its explicit absence, for the workspace in question.

## 9. Postconditions

- Every staleness judgment made for a context unit reflects the specific workspace's declared expectation.
- No workspace's staleness judgment is silently governed by another workspace's expectation or by an undeclared, product-wide default.

## 10. Dependencies

**Capability dependencies.** Workspace Definition (source of the declared freshness expectation).

**Epic dependencies.** E01.2 — Scope Declaration & Configuration; E04.1 — Change Detection (via this capability's own E04.2 prerequisite chain).

**Feature dependencies.** F04.2.1 — Freshness State Tracking; F01.2.2 — Workspace Configuration Management.

**External dependencies.** None directly.

## 11. Constraints

**Business constraints.** Freshness expectations vary per workspace, per Workspace Definition's configuration; this Feature honors each workspace's declared expectation rather than applying one fixed standard everywhere.

**Product constraints.** When a workspace's expectation is undeclared, the honest judgment is "no expectation declared," never an assumed "current" (P3).

**Context integrity constraints.** The expectation applied to a given workspace's context units must be that workspace's own current declaration, never a mismatched, stale, or borrowed copy.

**Trust constraints.** Since staleness judgments feed Assembly's eligibility decisions and are visible to every consumer, correct expectation resolution is required so no consumer sees a differently and incorrectly computed judgment for the same workspace (P4).

**Policy constraints.** None beyond the workspace's own configured expectation.

## 12. Acceptance Criteria

1. Given two workspaces with distinct declared freshness expectations and identical tracked age for a comparable context unit, the resulting staleness judgments differ in proportion to the differing expectations.
2. Given a workspace with no declared expectation, the staleness judgment resolves to an explicit "no expectation declared" condition, not "current."
3. A change to one workspace's declared expectation is reflected in that workspace's subsequent staleness judgments without altering another workspace's judgments.
4. Every staleness judgment is traceable to the specific declared expectation that was applied to produce it.

## 13. Validation Requirements

- That judgment differentiation is demonstrable across at least two distinct declared expectations against equivalent age/change patterns.
- That the undeclared-expectation path resolves honestly and is distinguishable from both "current" and "stale."
- That expectation-change propagation is correctly timed and isolated to the workspace whose declaration changed.

## 14. Failure Conditions

- **Freshness blindness.** A workspace's freshness expectation is undeclared or unclear, leaving no standard to check against. Expected behavior: the judgment surfaces this explicitly rather than defaulting silently to "current" or to a fixed global threshold (P5).
- **Silent staleness (misapplication variant).** The wrong, or a stale copy of, a workspace's expectation is applied. Expected behavior: any mismatch between the resolved and the intended current expectation must be detectable, never silently accepted as correct.

## 15. Traceability

Product Vision (Mission: maintain context) → Goals G2 (Currency of context), G3 (Consumer neutrality — no workspace's declared expectation overrides another's) → Product Principles P3 (Freshness first-class), P4 (No privileged consumer), P6 (Capability boundaries) → Capability FEP-002-CAP-04 (Context Maintenance) → Epic E04.2 (Freshness Accounting) → Feature F04.2.2 (Workspace-Specific Freshness Expectations).

## 16. Future Considerations

- Workspace- and source-specific freshness expectations are anticipated to become more granular over time — some sources treated as near-real-time, others acceptable at a daily cadence — building on this Feature's resolution mechanism.
- Predictive staleness, anticipated as future evolution, would apply against a workspace's declared expectation rather than replace the need for one.
