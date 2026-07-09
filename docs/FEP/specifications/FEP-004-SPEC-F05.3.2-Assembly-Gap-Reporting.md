# FEP-004-SPEC-F05.3.2 — Assembly Gap Reporting

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F05.3.2 |
| **Capability** | [Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Epic** | E05.3 — Composition & Gap Reporting |
| **Feature** | F05.3.2 — Assembly Gap Reporting |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md), [FEP-003-EPIC-CAP-05 — Context Assembly](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md), [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md), [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

## 2. Purpose

An Assembled Context that omits relevant material without saying so misrepresents itself as complete. This specification exists to define how Ferret records what relevant context was excluded from a composed result, and why, satisfying Product Principle P5 — no partial result is ever presented as complete — which is this Feature's stated Product Outcome.

## 3. Scope

- Recording an Assembly Gap for every unit of relevant context excluded from the Assembled Context produced by F05.3.1.
- Attributing each Assembly Gap to a specific, recorded reason: staleness, access restriction, absence, or a stated constraint.
- Making the set of Assembly Gaps available alongside the Assembled Context as part of Assembly's output.
- Covering exclusions that originate at any upstream stage of Assembly (eligibility exclusion in F05.2.1, constraint-driven exclusion in F05.3.1) insofar as they represent relevant-but-excluded material.

## 4. Out of Scope

- Determining eligibility itself (freshness, permission) — that is F05.2.1's responsibility; this feature only records the resulting exclusion and its reason.
- Composing the Assembled Context itself — that is F05.3.1, a prerequisite.
- Deciding how gaps are presented to a consumer — that is Context Delivery's responsibility, once Assembly hands off both the Assembled Context and its gaps.
- Judging whether an exclusion was the "right" trade-off — Assembly reports gaps; it does not evaluate or justify them beyond stating the recorded reason (no reasoning over the result, per FEP-001 Non-Goals).
- Recording provenance/lineage for the context that was included — that is Provenance & Attribution's cross-cutting responsibility, distinct from gap reporting for excluded material.

## 5. Engineering Requirements

1. Every unit of context that was relevant to the interpreted intent but excluded from the composed result must have a corresponding Assembly Gap recorded.
2. Each Assembly Gap must carry a specific, attributable reason: staleness, access restriction, absence (context did not exist or was never organized), or a stated constraint.
3. Assembly Gap Reporting must never allow a composed result to be delivered as complete when relevant material was excluded — completeness or the presence of gaps must be explicit and observable together.
4. Assembly Gaps must be reported consistently: an identical exclusion under identical circumstances must always be recorded with the same reason.
5. The absence of any Assembly Gap must positively indicate that the composed result is complete relative to what was eligible, not merely that no gap was recorded.
6. Assembly Gap Reporting must draw only on exclusion information already produced by upstream stages (F05.2.1, F05.3.1); it must not itself judge relevance or eligibility.

## 6. Inputs

- The Assembled Context and the record of items excluded during composition, from F05.3.1.
- The record of items excluded during eligibility-respecting selection, from F05.2.1.

## 7. Outputs

- A set of Assembly Gaps, each identifying excluded, relevant context and its specific exclusion reason.
- An implicit or explicit completeness signal: whether the Assembled Context is complete relative to the eligible, relevant set (no gaps) or incomplete (one or more gaps recorded).

## 8. Preconditions

- F05.3.1 has produced a composed Assembled Context and identified any constraint-driven exclusions.
- F05.2.1 has identified any eligibility-driven exclusions (staleness, permission) during selection.

## 9. Postconditions

- Every exclusion of relevant context, from any stage of Assembly, is represented as an attributable Assembly Gap.
- A consumer of Assembly's output can determine, without additional inference, whether the result is complete or where it falls short and why.

## 10. Dependencies

**Capability dependencies.** Indirectly, Context Maintenance and Access Control & Policy, insofar as their exclusion reasons (staleness, permission) must be preserved through F05.2.1 for accurate gap attribution.

**Epic dependencies.** E05.1 (Request Interpretation) and E05.2 (Selection & Ranking) — prerequisite epics whose exclusion information this feature draws on, per the epic file's Execution Order.

**Feature dependencies.** F05.3.1 (Context Composition), per the epic file's Dependencies column.

**External dependencies.** None directly.

## 11. Constraints

**Business constraints.** Gap reporting must apply the same attribution logic regardless of which consumer receives the result, per Product Principle P4.

**Product constraints.** Every exclusion from an assembly must be explainable by a specific, recorded reason: staleness, access, absence, or a stated constraint, per the capability's Success Criteria.

**Context integrity constraints.** Assembly must never present a partial result as complete; this is the central constraint this Feature exists to satisfy, per Product Principle P5 and the capability's Context integrity constraint.

**Trust constraints.** Gap records must be specific enough to be independently attributable — a generic or unexplained "excluded" state does not satisfy this Feature's requirement.

**Policy constraints.** Gap reporting must not itself disclose content the requester was not permitted to see; recording "excluded due to access restriction" must not leak the excluded content's substance beyond what policy allows to be known.

## 12. Acceptance Criteria

1. Given an Assembled Context with one or more constraint-driven exclusions, each exclusion has a corresponding Assembly Gap attributing it to that constraint.
2. Given an Assembled Context with one or more eligibility-driven exclusions (staleness or permission), each has a corresponding Assembly Gap attributing it to the correct specific reason.
3. Given an Assembled Context with no exclusions, no Assembly Gap is recorded, and the result is identifiable as complete relative to the eligible set.
4. Given a permission-driven exclusion, the recorded gap states the reason category without disclosing the excluded content itself beyond what policy permits.
5. Given identical exclusion circumstances across two requests, the recorded gap reason is the same in both cases.

## 13. Validation Requirements

- Validate that every exclusion identified upstream (F05.2.1, F05.3.1) results in exactly one attributable Assembly Gap.
- Validate that no Assembled Context is ever delivered without an accompanying completeness signal (gaps present or explicitly none).
- Validate that gap reason categories (staleness, access, absence, stated constraint) are applied correctly and consistently.
- Validate that access-restricted gaps do not leak restricted content through the gap record itself.

## 14. Failure Conditions

- **Silent truncation** — an exclusion occurs upstream but no corresponding Assembly Gap is recorded: must never occur; this is the primary failure mode this Feature exists to prevent, per Product Principle P5.
- **Unattributable gap** — a gap is recorded without a specific, recognizable reason: must be treated as a defect, since the capability's Success Criteria require every exclusion to be explainable by a specific reason.
- **Access bypass via gap disclosure** — a gap record for a permission-driven exclusion reveals more about the excluded content than policy permits: must never occur; the gap must state only the reason category.

## 15. Traceability

Product Vision (Mission: deliver trustworthy context) → Goals G2 (Currency), G4 (Trustworthy context) → Product Principles P2, P4, P5 → Capability FEP-002-CAP-05 (Context Assembly) → Epic E05.3 (Composition & Gap Reporting) → Feature F05.3.2 (Assembly Gap Reporting).

## 16. Future Considerations

- As relevance and ranking logic grow more sophisticated, gap reasons may need to become more granular to remain specific and attributable (per capability file §11, Future Evolution).
- Cross-workspace Assembly Gaps as Federation matures, where an exclusion's reason may originate in a different workspace than the one being queried (per epic file §8, Deferred Work: cross-workspace assembly deferred to Federation).
