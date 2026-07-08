# FEP-003-EPIC-CAP-05 — Engineering Program: Context Assembly

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-05 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Context Assembly composes the specific, relevant, appropriately-scoped context that answers a given request — selecting, ranking, and composing organized, eligible context, and honestly reporting any exclusions. It never acquires, organizes, or delivers context itself.

## 2. Engineering Epics

### E05.1 — Request Interpretation

- **Purpose.** Interpret what a request is actually asking for.
- **Scope.** Interpreting intent and stated constraints (scope, size) from a request, conceptually.
- **Success Definition.** A request's intent and constraints are correctly and consistently interpreted regardless of which consumer issued it.

### E05.2 — Selection & Ranking

- **Purpose.** Select relevant, eligible structured context and rank it by relevance.
- **Scope.** Selecting from Organization's structured context, respecting Maintenance's eligibility and Access Control & Policy's permission; ranking by relevance.
- **Success Definition.** Selected and ranked context is demonstrably relevant, current, and permitted.

### E05.3 — Composition & Gap Reporting

- **Purpose.** Compose a coherent result and report any exclusions honestly.
- **Scope.** Composing the final assembled context; recording Assembly Gaps for relevant-but-excluded material.
- **Success Definition.** Every assembled result is either complete relative to what's eligible, or its incompleteness is explicitly recorded and attributable.

## 3. Features

### E05.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F05.1.1 — Request Intent Interpretation | Interpret what context a request is asking for. | Provides the basis for selecting relevant structured context. | None within this capability. | Two equivalent requests, differently phrased, resolve to the same interpreted intent. |
| F05.1.2 — Constraint Recognition | Recognize constraints a request states about scope or size. | Enables Assembly to respect stated limits rather than silently ignoring or over-delivering. | F05.1.1 | A stated constraint is consistently applied, and its effect on the result is attributable back to it. |

### E05.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F05.2.1 — Eligibility-Respecting Selection | Select structured context that is both relevant and eligible (current, permitted). | Assembly never surfaces stale or unpermitted context. | F05.1.1, F04.2.1, F08.2.1 | No context excluded by freshness or permission ever appears in a selected set. |
| F05.2.2 — Relevance Ranking | Rank selected context by relevance to the interpreted request. | The most useful context is prioritized within any size constraint. | F05.2.1 | Ranking is consistent for equivalent requests and demonstrably favors more relevant context. |

### E05.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F05.3.1 — Context Composition | Compose selected, ranked context into a coherent Assembled Context. | A usable, request-appropriate body of context ready for Delivery. | F05.2.2, F05.1.2 | The composed result respects stated constraints and reflects the ranked selection. |
| F05.3.2 — Assembly Gap Reporting | Record what relevant context was excluded, and why. | Satisfies Product Principle P5 — no partial result presented as complete. | F05.3.1 | Every exclusion in a composed result has an attributable, recorded reason. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F04.2.1 (Freshness State Tracking), F08.2.1 (Permission Evaluation Engine).
- **Prerequisite Epics.** E04.2 (Freshness Accounting), E08.2 (Permission Evaluation).
- **Prerequisite Capabilities.** Context Organization, Context Maintenance, Access Control & Policy.

## 5. Execution Order

1. **E05.1** — nothing can be selected before intent is understood.
2. **E05.2** — depends on interpretation, and on Maintenance's freshness state and Access Control's permission decisions already existing, at least in minimal form.
3. **E05.3** — depends on selection and ranking being complete.

## 6. Capability Completion Gates

- **Functional completeness.** Every request type Ferret is expected to support produces a demonstrably relevant, eligible, composed result.
- **Validation readiness.** A request that would include stale or unpermitted context, absent Assembly's checks, is verified to correctly exclude it and report why.
- **Documentation readiness.** The distinction between Assembled Context and an Assembly Gap is documented well enough for Delivery's authors to preserve both faithfully.
- **Review completion.** FEP-002-CAP-05's non-responsibilities (no new acquisition/organization, no delivery-shape decisions, no access bypass) confirmed unviolated.

## 7. Risks

- **Premature coupling to Access Control's maturity.** Eligibility-respecting selection cannot be meaningfully completed until Access Control & Policy's permission evaluation exists even minimally; planning Assembly in isolation risks a completion gate that cannot actually be exercised.
- **Relevance ranking scope creep.** "Rank by relevance" can silently expand into request-shape-specific tuning that favors one consumer's typical pattern, violating Product Principle P4 at the planning level.
- **Constraint interpretation ambiguity.** Without a concrete, agreed notion of what a "stated constraint" can be, completion criteria for constraint-related features risk being unverifiable.

## 8. Deferred Work

- Feedback-informed assembly (using observed downstream use to inform future relevance) — deferred pending a bounded design that does not reintroduce reasoning into Ferret's scope.
- Cross-workspace assembly — deferred to Federation.
