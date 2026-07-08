# FEP-004-SPEC-F05.3.1 — Context Composition

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F05.3.1 |
| **Capability** | [Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Epic** | E05.3 — Composition & Gap Reporting |
| **Feature** | F05.3.1 — Context Composition |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md), [FEP-003-EPIC-CAP-05 — Context Assembly](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md), [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md), [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

## 2. Purpose

Selected and ranked context is not yet a usable result until it has been composed into a single coherent body appropriate to what was asked and to any stated limits. This specification exists to define composition of the final Assembled Context, realizing the Feature's Product Outcome of a usable, request-appropriate body of context ready for Delivery.

## 3. Scope

- Composing the ranked, eligible set from F05.2.2 into a single coherent Assembled Context.
- Applying recognized constraints (from F05.1.2) to the composed result — e.g., limiting what is included to fit a stated scope or size.
- Ensuring the composed result reflects the ranked order: where a constraint requires a cut, higher-ranked context is preferred over lower-ranked context.
- Producing, for every item excluded by the application of a constraint at this stage, the information needed for F05.3.2 to record it as an Assembly Gap.

## 4. Out of Scope

- Interpreting intent or recognizing constraints — that is E05.1, a prerequisite.
- Selecting or ranking context — that is E05.2, a prerequisite.
- Recording and formally reporting Assembly Gaps — that is F05.3.2, which consumes this feature's exclusion information but owns the gap record itself.
- Deciding the form or surface through which the composed result reaches the consumer — that is Context Delivery.
- Any reasoning over, summarization that alters meaning of, or generation of new content from the composed context (FEP-001 Non-Goal).

## 5. Engineering Requirements

1. Composition must produce a single coherent Assembled Context from the ranked, eligible set.
2. Composition must apply any recognized constraint (F05.1.2) to determine what is included, preferring higher-ranked context when a size constraint requires a cut.
3. Composition must never drop relevant, eligible context to fit a constraint without that omission being identifiable as a specific exclusion (feeding F05.3.2).
4. Composition must reflect the ranked selection faithfully: the composed result's inclusion order or emphasis must not contradict the ranking produced by F05.2.2.
5. Composition must produce a result for every request that reached this stage, even when the eligible set is empty, in which case the composed result is explicitly empty rather than absent.
6. Composition output must be structured so Context Delivery can hand it off without altering its substance.

## 6. Inputs

- The ranked, eligible set of structured context from F05.2.2.
- Recognized constraints from F05.1.2.

## 7. Outputs

- An Assembled Context: the composed, coherent body of context responding to the request.
- A set of items excluded specifically at the composition stage (due to constraint application), passed to F05.3.2 for gap recording.

## 8. Preconditions

- F05.2.2 has produced a ranked, eligible set.
- F05.1.2 has produced any recognized constraints applicable to the request.

## 9. Postconditions

- An Assembled Context exists for the request, reflecting the ranked selection within any stated constraint.
- Every constraint-driven exclusion at this stage is identifiable for gap attribution.

## 10. Dependencies

**Capability dependencies.** None beyond Context Assembly itself; composition operates on what E05.1 and E05.2 have already produced.

**Epic dependencies.** E05.1 (Request Interpretation); E05.2 (Selection & Ranking) — both prerequisite epics per the epic file's Execution Order.

**Feature dependencies.** F05.2.2 (Relevance Ranking), F05.1.2 (Constraint Recognition), per the epic file's Dependencies column.

**External dependencies.** None directly; the composed result is handed onward to Context Delivery, which is a separate capability, not an external system.

## 11. Constraints

**Business constraints.** Composition must remain consumer-neutral: the same ranked set and the same stated constraint compose into the same Assembled Context regardless of consumer, per Product Principle P4.

**Product constraints.** Composition must respect stated scope or size constraints without silently dropping relevant context to fit; a real trade-off must surface as an identifiable exclusion, per the capability's Product constraint.

**Context integrity constraints.** Composition must never present a partial result as complete, per Product Principle P5; every exclusion at this stage must be attributable to a specific, recorded reason (the applied constraint).

**Trust constraints.** The composed result must be traceable back to the ranked, eligible set it was built from, preserving the provenance obligation of Product Principle P2 as context passes through this transformation.

**Policy constraints.** Composition must not reintroduce any context excluded earlier for eligibility reasons (F05.2.1); it only ever composes from what was already selected and ranked.

## 12. Acceptance Criteria

1. Given a ranked, eligible set with no stated size or scope constraint, the composed Assembled Context includes all of that set.
2. Given a ranked, eligible set and a stated size constraint smaller than the set, the composed Assembled Context includes the highest-ranked items up to that limit, and the remainder is identifiable as excluded.
3. Given an empty eligible set, the composed Assembled Context is explicitly empty, not absent or undefined.
4. Given the same ranked set and the same stated constraint, composition produces the same Assembled Context on repeated invocation.
5. Given a stated scope constraint, the composed Assembled Context contains no context outside that scope, and any relevant context outside the scope is identifiable as excluded for that reason.

## 13. Validation Requirements

- Validate that composed results respect stated constraints exactly, without under- or over-inclusion.
- Validate that composition preserves the relevance order established by ranking when constraints force a cut.
- Validate that no relevant, eligible context disappears from a composed result without being identifiable as an exclusion.
- Validate reproducibility of composition for identical inputs.

## 14. Failure Conditions

- **Silent truncation** — relevant context is dropped to fit a constraint without any identifiable record of the exclusion: must never occur; every constraint-driven cut must be identifiable for F05.3.2 to record, per Product Principle P5.
- **Ranking disregard** — composition includes lower-ranked context ahead of higher-ranked context under a size constraint: must be treated as a defect against this feature's Completion Criterion.
- **Constraint violation** — the composed result exceeds a stated size limit or includes material outside a stated scope: must never occur; composition must fail toward under-inclusion with a recorded exclusion, not silent over-delivery.

## 15. Traceability

Product Vision (Mission: assemble a specific, relevant, appropriately scoped body of context) → Goals G1 (Completeness), G2 (Currency, inherited from upstream eligibility), G4 (Trustworthy context) → Product Principles P2, P4, P5 → Capability FEP-002-CAP-05 (Context Assembly) → Epic E05.3 (Composition & Gap Reporting) → Feature F05.3.1 (Context Composition).

## 16. Future Considerations

- Cross-workspace composition, deferred to Federation (per epic file §8, Deferred Work, and capability file §11).
- Feedback-informed composition adjustments based on observed downstream use — deferred pending a bounded design that avoids reintroducing reasoning into Ferret's scope (per epic file §8).
