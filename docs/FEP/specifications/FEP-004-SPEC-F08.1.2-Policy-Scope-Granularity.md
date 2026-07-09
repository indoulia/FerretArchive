# FEP-004-SPEC-F08.1.2 — Policy Scope Granularity

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F08.1.2 |
| **Capability** | [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) |
| **Epic** | E08.1 — Policy Definition & Scope |
| **Feature** | F08.1.2 — Policy Scope Granularity |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-08 — Access Control & Policy](../epics/FEP-003-EPIC-CAP-08-Access-Control-Policy.md) · [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Forcing every policy to be workspace-wide would make fine-grained governance impossible without an explosion of workspaces. Policy Scope Granularity exists to satisfy F08.1.2's objective — supporting policy declared at workspace, source, or context-unit granularity — so that fine-grained governance is possible without forcing every rule to apply everywhere, and so that a finer-granularity policy composes with or takes precedence over a coarser one in a way that is unambiguous, satisfying the Feature's product outcome directly.

## 3. Scope

- Supporting exactly three defined granularities at which a policy may be declared: workspace-wide, source-specific, and context-unit-specific.
- Making a policy's declared granularity explicit and retrievable.
- Defining and applying a documented precedence rule that determines the single applicable outcome when policies at different granularities overlap for the same governance target.

## 4. Out of Scope

- The base mechanism of declaring a policy at all, including referencing workspace configuration — that is F08.1.1 (Policy Declaration), which this Feature builds directly on top of.
- Evaluating an identity assertion against the resolved applicable policy — that is F08.2.1 (Permission Evaluation Engine).
- Producing a distinguishable "partially permitted" evaluation outcome — that is F08.2.2 (Partial Permission Outcomes).
- Recording or making auditable which policy applied to a past decision — that is F08.3.1 (Decision Recording & Audit Surfacing).
- Defining what structurally constitutes a "source" or a "context unit" — those definitions belong to Context Acquisition and Context Organization respectively; this Feature only resolves precedence among policies addressed at those levels.
- Establishing or issuing consumer identity — per FEP-001 §5.2/§6, identity is consumed from external systems, never issued by Ferret, and this Feature does not evaluate identity at all.

## 5. Engineering Requirements

1. A policy must be declarable at exactly one of three defined granularities: workspace-wide, source-specific, or context-unit-specific.
2. The granularity at which a policy is declared must be explicit and retrievable, never inferred from context.
3. When two or more policies apply to the same governance target at different granularities, a documented precedence rule must determine a single applicable outcome.
4. The precedence rule must produce a defined, unambiguous outcome for every combination of overlapping granularities, with no combination left undefined.
5. A finer-granularity policy must be able to override, narrow, or compose with a coarser one according to the documented rule, without requiring the coarser policy to be redeclared or modified.
6. Adding or changing a finer-granularity policy must not silently alter the applicable policy for governance targets the finer policy does not cover.

## 6. Inputs

- A declared policy (F08.1.1).
- A stated intended granularity for that policy: workspace-wide, source-specific, or context-unit-specific.
- The set of policies already declared for a workspace, when precedence must be resolved.

## 7. Outputs

- A policy with an explicit, resolvable scope granularity.
- A single, resolvable applicable-policy outcome for a given governance target, even when multiple policies at different granularities could otherwise apply.

## 8. Preconditions

- F08.1.1 (Policy Declaration) must already allow a policy to be declared — granularity is a property of an already-declarable policy, not a precondition for declaration to exist at all.

## 9. Postconditions

- For any governance target, exactly one applicable policy outcome is determinable, regardless of how many policies at different granularities exist for that workspace.
- A finer-granularity policy's effect is observable exactly where it was declared to apply, and nowhere else.

## 10. Dependencies

**Capability dependencies.** Workspace Definition — the source of the workspace- and source-level addressing concepts a policy's granularity is declared against (FEP-003-EPIC-CAP-08 §4).

**Epic dependencies.** E01.2 (Scope Declaration & Configuration) — inherited transitively through F08.1.1's own dependency, since granularity is declared on top of an already-referenced workspace configuration.

**Feature dependencies.** F08.1.1 (Policy Declaration) — the explicit prerequisite Feature per the epic file's E08.1 Features table; a policy must be declarable before its granularity can be constrained.

**External dependencies.** None. Granularity resolution requires no identity assertion or external identity & access system involvement; that category (FEP-001 §6) is relevant only once evaluation (E08.2) occurs.

## 11. Constraints

**Business constraints.** Precedence between overlapping policy scopes must be resolvable without dispute; an unresolved precedence question puts Permission Evaluation's consistency guarantee (FEP-002-CAP-08 §8, Business) at risk before evaluation ever runs (FEP-003-EPIC-CAP-08 §7, Risks — Precedence-rule disputes).

**Product constraints.** A governance target's applicable policy must never be left implicit or undetermined — an undetermined outcome would be indistinguishable from context simply not being assembled, which FEP-002-CAP-08 §8 (Product) forbids.

**Context integrity constraints.** Composition or override between granularities must preserve the ability to later produce a distinguishable partial-permission outcome (F08.2.2) where the applicable policy calls for one — granularity resolution must not collapse or discard that distinction.

**Trust constraints.** Per Product Principle P4 (No privileged consumer), the precedence rule must apply identically regardless of which consumer or capability triggered resolution of the applicable policy.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), this Feature resolves which policy applies; it must not itself decide whether the resolved policy permits, denies, or partially permits — that remains E08.2's responsibility.

## 12. Acceptance Criteria

1. Every declared policy exposes a single, explicit granularity: workspace-wide, source-specific, or context-unit-specific.
2. For any governance target covered by two or more policies at different granularities, applying the documented precedence rule yields exactly one applicable outcome.
3. Declaring a new context-unit-level policy does not change the applicable policy for any context unit it does not cover.
4. Resolving the applicable policy for the same governance target and the same set of declared policies is reproducible: repeated resolution yields the same result every time.

## 13. Validation Requirements

- That every combination of overlapping granularities (workspace with source, workspace with context-unit, source with context-unit, all three together) resolves to exactly one documented outcome.
- That the precedence rule itself is documented and its application is deterministic.
- That no governance target is left with zero determinable applicable-policy state where at least one declared policy could apply.

## 14. Failure Conditions

- **Precedence-rule ambiguity.** A combination of overlapping policy scopes has no documented outcome (FEP-003-EPIC-CAP-08 §7, Risks — Precedence-rule disputes). Expected behavior: this must be treated as a failure to resolve before Permission Evaluation depends on the outcome, and must never be resolved arbitrarily or silently (Product Principle P5).
- **Silent over-permission via granularity conflict.** A conflict between overlapping granularities defaults to the more permissive policy without that being the documented rule (FEP-002-CAP-08 §10, Failure Modes — Silent over-permission). Expected behavior: any default applied must be the explicitly documented rule, never an incidental outcome of resolution order.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goal G1 (Completeness of context — fine-grained policy scoping allows precise governance without leaving ungoverned gaps) → Product Principles P5 (Degrade by scope, not silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-08 (Access Control & Policy) → Epic E08.1 (Policy Definition & Scope) → Feature F08.1.2 (Policy Scope Granularity).

## 16. Future Considerations

- Finer-grained policy scopes as source and consumer diversity grow beyond the three granularities currently defined (FEP-002-CAP-08 §11).
- Cross-workspace precedence reconciliation — deciding how a policy scoped in one workspace interacts with a related workspace's policy — deferred to Federation as it matures (FEP-003-EPIC-CAP-08 §8; FEP-002-CAP-08 §11).
