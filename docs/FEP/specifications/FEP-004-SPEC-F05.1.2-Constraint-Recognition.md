# FEP-004-SPEC-F05.1.2 — Constraint Recognition

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F05.1.2 |
| **Capability** | [Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Epic** | E05.1 — Request Interpretation |
| **Feature** | F05.1.2 — Constraint Recognition |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md), [FEP-003-EPIC-CAP-05 — Context Assembly](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md), [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md), [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

## 2. Purpose

A request may state limits on scope or size that Assembly must honor. This specification exists to define how Ferret recognizes such stated constraints, so that Assembly can respect stated limits rather than silently ignoring or over-delivering — the Feature's stated Product Outcome.

## 3. Scope

- Recognizing constraints a request explicitly states about scope (what subject areas are in or out) or size (how much context is wanted).
- Producing a conceptual record of each recognized constraint, attributable back to the request that stated it.
- Making recognized constraints available to Composition & Gap Reporting (E05.3) so their effect on the result can be traced.

## 4. Out of Scope

- Interpreting the request's underlying subject-matter intent — that is F05.1.1 (Request Intent Interpretation), a prerequisite.
- Enforcing or applying the constraint during selection, ranking, or composition — that is E05.2 and F05.3.1's responsibility to honor what this feature recognizes.
- Recording Assembly Gaps caused by a constraint's effect on the result — that is F05.3.2.
- Inferring constraints the request did not actually state (implicit or assumed limits are not "recognition").
- Any decision about consumer-side presentation size or shape — that is Context Delivery.

## 5. Engineering Requirements

1. Assembly must recognize scope-type constraints (e.g., limiting the subject areas a result should draw from) when a request states them.
2. Assembly must recognize size-type constraints (e.g., limiting how much context a result should contain) when a request states them.
3. Each recognized constraint must be recorded in a form that allows its later effect on a composed result to be attributed back to it.
4. A request that states no constraint must be distinguishable from one whose constraint could not be recognized.
5. Constraint recognition must be applied consistently for equivalent constraints regardless of phrasing or consumer type.
6. Recognized constraints must be passed forward intact to Composition (F05.3.1) without being altered or dropped before use.

## 6. Inputs

- A request, expressed conceptually, including any statements of scope or size limitation it contains.
- The interpreted intent produced by F05.1.1, for context in which a constraint is being applied.

## 7. Outputs

- A set of recognized constraints, each attributable to the request that stated it.
- An indication when no constraint was stated, or when a stated constraint could not be recognized.

## 8. Preconditions

- F05.1.1 has produced an interpreted intent for the request (a constraint is recognized in relation to what is being asked for).

## 9. Postconditions

- Every request's stated constraints, if any, are recorded and available to downstream composition.
- No stated constraint is lost or silently dropped before reaching composition.

## 10. Dependencies

**Capability dependencies.** None beyond Context Assembly itself; this feature operates entirely on the request as interpreted.

**Epic dependencies.** E05.1 (Request Interpretation) — this feature is part of that epic and follows its intent-interpretation step.

**Feature dependencies.** F05.1.1 (Request Intent Interpretation), per the epic file's Dependencies column.

**External dependencies.** None; constraints originate from consumer-issued requests (FEP-001 §6, Consumer systems), not from any external system requiring separate integration.

## 11. Constraints

**Business constraints.** Constraint recognition must apply the same logic to equivalent stated constraints regardless of the issuing consumer, per Product Principle P4.

**Product constraints.** A recognized constraint must be respected downstream without being silently ignored or overridden, per the capability's Product constraint on respecting stated scope or size limits.

**Context integrity constraints.** Recognizing a constraint must never itself cause context to be dropped without record — recognition only records the constraint; its enforcement effect must later surface as an attributable Assembly Gap where relevant (P5).

**Trust constraints.** An unrecognizable stated constraint must be reported as such, not silently discarded, per Product Principle P5.

**Policy constraints.** None specific to this feature.

## 12. Acceptance Criteria

1. Given a request stating a scope constraint, the constraint is recognized and recorded, attributable to the request.
2. Given a request stating a size constraint, the constraint is recognized and recorded, attributable to the request.
3. Given a request stating no constraint, the system records that no constraint was stated, distinct from a recognition failure.
4. Given a request whose stated constraint cannot be recognized, the system records an explicit recognition failure rather than silently proceeding as if no constraint existed.
5. Given two equivalently phrased constraints from different consumer types, both are recognized identically.

## 13. Validation Requirements

- Validate that stated scope and size constraints are correctly recognized across varied phrasings.
- Validate that unstated constraints are not fabricated.
- Validate that unrecognizable constraints are surfaced explicitly rather than silently ignored.
- Validate that recognized constraints reach Composition (F05.3.1) unaltered.

## 14. Failure Conditions

- **Silent truncation risk at the source** — a constraint is recognized but its downstream enforcement drops relevant context without recording why: the recognition record must ensure this can be traced back and reported as an Assembly Gap by F05.3.2, per Product Principle P5.
- **Constraint misattribution** — a recognized constraint cannot be tied back to the request that stated it: violates the Completion Criterion that a constraint's effect is attributable, and must be treated as a defect.
- **Consumer-specific constraint handling** — equivalent constraints recognized differently based on consumer type: violates Product Principle P4 and must be corrected.

## 15. Traceability

Product Vision (Mission: assemble context appropriate to a request's constraints) → Goals G3 (Consumer neutrality) → Product Principles P4, P5 → Capability FEP-002-CAP-05 (Context Assembly) → Epic E05.1 (Request Interpretation) → Feature F05.1.2 (Constraint Recognition).

## 16. Future Considerations

- A more concrete, agreed notion of what a "stated constraint" can be, to reduce ambiguity risk in completion verification (per epic file §7, Risks).
- Expansion of recognizable constraint types as request shapes diversify (per capability file §11, Future Evolution).
