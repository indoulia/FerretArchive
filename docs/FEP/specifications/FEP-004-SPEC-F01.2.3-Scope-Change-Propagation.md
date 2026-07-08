# FEP-004-SPEC-F01.2.3 — Scope Change Propagation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F01.2.3 |
| **Capability** | [FEP-002-CAP-01 — Workspace Definition](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) |
| **Epic** | E01.2 — Scope Declaration & Configuration |
| **Feature** | F01.2.3 — Scope Change Propagation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-01 — Workspace Definition](../epics/FEP-003-EPIC-CAP-01-Workspace-Definition.md) · [FEP-002-CAP-01 — Workspace Definition](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A scope declaration that can change silently is worse than one that never changes at all — dependent capabilities would keep acting on a boundary that no longer holds. Scope Change Propagation exists to ensure that changes to declared scope are visible to dependent capabilities, so that newly out-of-scope context can be retired and newly in-scope content can be acquired without a silent gap (FEP-003-EPIC-CAP-01 §3, F01.2.3 Objective and Product Outcome).

## 3. Scope

- Detecting that a workspace's declared scope has changed from a previously resolved state.
- Making a scope change observable to dependent capabilities, specifically Context Acquisition and Context Maintenance, without requiring those capabilities to independently poll for a difference.
- Ensuring a scope change is itself an explicit, recorded event, not an inferred consequence of a later query returning a different answer.

## 4. Out of Scope

- Declaring the scope boundary itself (which source categories, which exclusions) — that is F01.2.1 (Scope Boundary Declaration), a strict precondition of this Feature; this Feature only concerns the visibility of a change to that declaration, not the declaration's content.
- Declaring or resolving workspace-wide configuration — that is F01.2.2, a sibling Feature; configuration changes are a distinct concern from scope changes, and nothing in this Feature extends to configuration.
- Actually acquiring newly in-scope content or retiring newly out-of-scope content — those actions belong to Context Acquisition and Context Maintenance respectively; this Feature only makes the change observable to them.
- Assigning or resolving workspace identity, or tracking lifecycle state — those are F01.1.1 and F01.1.2.
- Declaring or resolving relationships between workspaces — that is F01.3.1 / F01.3.2.
- Deciding how urgently a dependent capability must react to a propagated change — this Feature guarantees observability, not a service-level response time, which is an implementation concern outside this specification's remit.

## 5. Engineering Requirements

1. A change to a workspace's declared scope must be recorded as a discrete, observable event, distinguishable from the scope's prior state.
2. Dependent capabilities must be able to become aware of a scope change without requiring them to repeatedly re-query the full scope declaration on a fixed interval to detect a difference themselves.
3. A scope change event must identify, at minimum, that a change occurred and the workspace it occurred against, sufficient for a dependent capability to determine what newly is, or is no longer, in scope.
4. Propagation of a scope change must not depend on any dependent capability having been active or listening at the exact moment the change occurred — a capability that resumes activity later must still be able to determine that a change happened since it last observed scope.
5. A scope change must never be lost, merged invisibly into a later change, or superseded in a way that hides an intermediate state a dependent capability needed to act on.
6. The propagation mechanism itself must not alter, reinterpret, or filter the content of the scope change — it carries the fact that a change occurred, not a judgment about it.

## 6. Inputs

- A previously resolved scope declaration for a workspace (F01.2.1) against which a new declaration is compared.
- A new or updated scope declaration for that same workspace.

## 7. Outputs

- An observable scope change signal, sufficient for Context Acquisition and Context Maintenance to determine that scope has changed and to resolve the new scope (FEP-002-CAP-01 §8, Context integrity — "a change to declared scope must be visible to Context Maintenance").

## 8. Preconditions

- The workspace must already be declared (F01.1.1) and have a previously resolvable scope declaration (F01.2.1) for a "change" to be meaningful.

## 9. Postconditions

- Context Acquisition and Context Maintenance can determine that a workspace's scope has changed without independently detecting the difference themselves.
- No gap exists between a scope change occurring and that change being observable — newly out-of-scope context becomes eligible for retirement, and newly in-scope content becomes eligible for acquisition, without a silent delay attributable to this Feature.

## 10. Dependencies

**Capability dependencies.** None beyond Workspace Definition itself; this Feature exists specifically to be consumed by Context Acquisition and Context Maintenance, but its own function does not depend on either of them functioning correctly.

**Epic dependencies.** E01.1 (Workspace Identity & Lifecycle), transitively through F01.2.1 — scope must exist and be identified with a workspace before a change to it is meaningful.

**Feature dependencies.** F01.2.1 (Scope Boundary Declaration) — per the E01.2 Features table, F01.2.3 depends directly on F01.2.1; there is nothing to propagate a change to without a prior scope declaration to change from.

**External dependencies.** None. Propagation is a relationship between this capability and other Ferret capabilities (Acquisition, Maintenance); it does not depend on any external system category from FEP-001 §6.

## 11. Constraints

**Business constraints.** Scope changes must originate from the same explicit-declaration discipline as the original scope statement (FEP-002-CAP-01 §8, Business) — a "change" propagated by this Feature must correspond to an actual, explicit re-declaration, not an inferred drift.

**Product constraints.** Propagation must remain scoped to the specific, stable workspace identity whose scope changed (FEP-002-CAP-01 §8, Product) — a change must never be attributable to, or observable against, the wrong workspace.

**Context integrity constraints.** This Feature exists to satisfy the capability's own explicit context-integrity constraint: "a change to declared scope must be visible to Context Maintenance so newly out-of-scope context can be retired and newly in-scope context can be acquired; scope changes cannot be silent" (FEP-002-CAP-01 §8, Context integrity).

**Trust constraints.** Per Product Principle P3 (Freshness is first-class), a scope change that is not promptly observable undermines every downstream capability's ability to state how current its own view of the workspace is.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), this Feature propagates the fact and content of a change; it must not perform the retirement or acquisition actions themselves, which remain owned by Context Maintenance and Context Acquisition respectively.

## 12. Acceptance Criteria

1. Every change to a workspace's declared scope produces an observable event distinct from the scope's prior state.
2. A dependent capability that was inactive at the time of a scope change can still determine, upon resuming, that a change occurred since it last observed the workspace's scope.
3. No scope change is lost or silently superseded such that a dependent capability's understanding of scope skips an intermediate, actionable state.
4. A scope change event identifies the specific workspace it pertains to, unambiguously distinguishing it from changes to any other workspace.
5. The propagated scope change reflects the declared change's actual content, without alteration or omission introduced by the propagation itself.

## 13. Validation Requirements

- That every scope change is observable without requiring a dependent capability to poll the full scope declaration to detect a difference.
- That no scope change event is dropped, merged, or delayed in a way that leaves dependent capabilities unaware of an intermediate scope state they needed to act on.
- That scope change observability holds regardless of whether a dependent capability was active at the moment the change occurred.
- That a scope change is never attributable to the wrong workspace.

## 14. Failure Conditions

- **Silent scope change.** Scope changes without informing Context Maintenance, leaving stale context in place or in-scope content unacquired (FEP-002-CAP-01 §10, Failure Modes: Silent scope change). Expected behavior: per Product Principle P5, this must never be allowed to occur invisibly — if propagation cannot complete or be confirmed, that failure must itself be observable, rather than presenting a workspace's dependents with a stale, unflagged view of scope.
- **Propagation lag treated as success.** A scope change is recorded but not yet observable to a dependent capability, and that gap is not itself surfaced. Expected behavior: the existence of an unpropagated change must be a detectable condition, not indistinguishable from full propagation having completed.

## 15. Traceability

Product Vision (Mission: infrastructure that continuously maintains and delivers current engineering context) → Goals G2 (Currency of context — staleness must be bounded and observable, not silent) → Product Principles P3 (Freshness is first-class), P5 (Degrade by scope, not by silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-01 (Workspace Definition) → Epic E01.2 (Scope Declaration & Configuration) → Feature F01.2.3 (Scope Change Propagation).

## 16. Future Considerations

- As the acquisition surface grows in breadth (FEP-001 §8, Unbounded acquisition surface risk), the volume and frequency of scope changes propagated may grow correspondingly; this Feature's observability guarantee is expected to hold independent of that growth, though the mechanism's practical characteristics are an implementation-track concern, not addressed here.
- Sequencing pressure from downstream capabilities to short-cut scope declaration (FEP-003-EPIC-CAP-01 §7, Risk) is a direct risk to this Feature specifically, since propagation is only as meaningful as the scope declarations it carries.
