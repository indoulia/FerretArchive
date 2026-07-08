# FEP-004-SPEC-F05.2.2 — Relevance Ranking

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F05.2.2 |
| **Capability** | [Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Epic** | E05.2 — Selection & Ranking |
| **Feature** | F05.2.2 — Relevance Ranking |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md), [FEP-003-EPIC-CAP-05 — Context Assembly](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md), [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md), [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

## 2. Purpose

Once eligible, relevant context has been selected, Assembly must prioritize it so that, under any size constraint, the most useful context is not crowded out by the merely adequate. This specification exists to define ranking of the selected set by relevance, realizing the Feature's Product Outcome that the most useful context is prioritized within any size constraint.

## 3. Scope

- Ordering the set selected by F05.2.1 according to relevance to the interpreted intent (F05.1.1).
- Producing a ranking that is consistent for equivalent requests.
- Making the ranked order available to Composition (F05.3.1) so that, under a size constraint, higher-ranked context is preferred.

## 4. Out of Scope

- Selecting which context is eligible in the first place (freshness, permission) — that is F05.2.1, a prerequisite.
- Applying a size or scope constraint to cut the ranked list down — that is Composition's responsibility (F05.3.1), using recognized constraints from F05.1.2.
- Recording what was excluded as a result of ranking-plus-constraint interaction — that is F05.3.2 (Assembly Gap Reporting).
- Tuning ranking to favor any one consumer's typical request shape (this would violate Product Principle P4, per the epic's identified risk of "relevance ranking scope creep").
- Any acquisition, organization, or maintenance of the context being ranked.

## 5. Engineering Requirements

1. Ranking must order the selected set such that context more relevant to the interpreted intent is prioritized over less relevant context.
2. Ranking must be consistent for equivalent requests: two equivalent requests over unchanged selected sets must produce the same relative order.
3. Ranking must not vary based on which consumer type issued the request, for equivalent requests and equivalent selected sets.
4. Ranking must operate only on the eligible set produced by F05.2.1; it must not reconsider or override eligibility.
5. Ranking must produce an order that is total or at least sufficient to unambiguously determine what is prioritized when a size constraint later limits how much can be included.
6. Ranking behavior must be demonstrable as favoring more relevant context, not merely consistent — consistency alone does not satisfy relevance.

## 6. Inputs

- The eligible, selected set of structured context from F05.2.1.
- The interpreted intent from F05.1.1, as the basis against which relevance is judged.

## 7. Outputs

- A ranked ordering of the selected set, from most to least relevant to the interpreted intent.

## 8. Preconditions

- F05.2.1 has produced a selected set that is already relevant, current, and permitted.

## 9. Postconditions

- The selected set has a defined relevance order available for Composition to consume.
- The order is reproducible for equivalent requests over unchanged input.

## 10. Dependencies

**Capability dependencies.** None beyond Context Assembly itself; ranking operates entirely on what F05.2.1 has already selected.

**Epic dependencies.** E05.2 (Selection & Ranking) — this feature is the second half of that epic, following selection.

**Feature dependencies.** F05.2.1 (Eligibility-Respecting Selection), per the epic file's Dependencies column.

**External dependencies.** None directly.

## 11. Constraints

**Business constraints.** Ranking must remain consumer-neutral: the same selected set, for the same interpreted intent, ranks identically regardless of which consumer issued the request, per Product Principle P4.

**Product constraints.** Ranking must not be allowed to silently expand into request-shape-specific tuning that favors one consumer's typical pattern — an explicit risk identified for this epic ("Relevance ranking scope creep").

**Context integrity constraints.** Ranking must not alter eligibility; it must never cause ineligible context to be reconsidered as eligible through a ranking side effect.

**Trust constraints.** Ranking decisions must be attributable to relevance to the interpreted intent, not to arbitrary or unexplainable ordering, so that Composition's later constraint-driven cuts remain explainable.

**Policy constraints.** None specific to this feature; ranking never revisits permission decisions already made by F05.2.1.

## 12. Acceptance Criteria

1. Given a selected set and an interpreted intent, the ranking demonstrably places more relevant context ahead of less relevant context.
2. Given two equivalent requests over an unchanged selected set, the produced ranking is identical.
3. Given the same request and selected set submitted by two different consumer types, the ranking is identical.
4. Given a selected set, ranking produces an order sufficient to determine, for any prefix length, which items would be included first under a later size constraint.

## 13. Validation Requirements

- Validate that ranking output correlates with independently judged relevance to the interpreted intent.
- Validate ranking consistency across repeated invocations and across consumer types for equivalent inputs.
- Validate that ranking never alters the membership of the eligible set, only its order.
- Validate absence of consumer-shape-specific bias in ranking outcomes over time.

## 14. Failure Conditions

- **Relevance drift** — ranking logic quietly favors the request shape most common from one consumer, disadvantaging others: violates Product Principle P4; must be detected and corrected, and the ranking basis made inspectable.
- **Unranked or arbitrary order** — the selected set is passed to Composition without a defensible relevance order: violates the Completion Criterion that ranking demonstrably favors more relevant context; must be treated as a defect.
- **Eligibility regression through ranking** — ranking logic inadvertently reintroduces or reorders in a way that surfaces ineligible context: must never occur; if detected, must be treated as an Access bypass or Stale leakage failure per F05.2.1's failure conditions.

## 15. Traceability

Product Vision (Mission: assemble the most useful context for a request) → Goals G1 (Completeness within scope), G3 (Consumer neutrality) → Product Principle P4 → Capability FEP-002-CAP-05 (Context Assembly) → Epic E05.2 (Selection & Ranking) → Feature F05.2.2 (Relevance Ranking).

## 16. Future Considerations

- Increasingly sophisticated relevance and ranking logic as the diversity of request shapes grows (per capability file §11, Future Evolution).
- Feedback-informed relevance ranking based on observed downstream use of assembled context — deferred pending a bounded design that does not reintroduce reasoning or generation into Ferret's scope (per epic file §8, Deferred Work).
