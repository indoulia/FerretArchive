# FEP-004-SPEC-F08.2.1 — Permission Evaluation Engine

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F08.2.1 |
| **Capability** | [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) |
| **Epic** | E08.2 — Permission Evaluation |
| **Feature** | F08.2.1 — Permission Evaluation Engine |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-08 — Access Control & Policy](../epics/FEP-003-EPIC-CAP-08-Access-Control-Policy.md) · [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A declared, scoped policy has no effect on what is delivered until something actually evaluates a consumer's asserted identity against it for a specific request. The Permission Evaluation Engine exists to do exactly that, satisfying F08.2.1's objective — evaluating a consumer's asserted identity against applicable policy for a given context request — and delivering its product outcome: the permission decision that Context Assembly and Context Delivery depend on before either may act.

## 3. Scope

- Evaluating a specific consumer's asserted identity against the applicable policy already resolved (per F08.1.1, F08.1.2) for a specific context request.
- Producing a decision — at minimum, permitted or denied — for that request.
- Guaranteeing that identical identity, context request, and policy state always produce the identical decision.
- Ensuring every context request that reaches Context Assembly or Context Delivery is subject to an evaluated decision, with no bypass path.

## 4. Out of Scope

- Declaring a policy, or resolving which policy applies among overlapping granularities — that is F08.1.1 and F08.1.2 respectively; this Feature consumes an already-resolved applicable policy as input.
- Producing a distinguishable "partially permitted" outcome — that is F08.2.2 (Partial Permission Outcomes), which extends this Feature's outcome space.
- Recording the decision or making it auditable after the fact — that is F08.3.1 (Decision Recording & Audit Surfacing).
- Validating, issuing, refreshing, or otherwise establishing the identity assertion being evaluated — per FEP-001 §5.2/§6, identity is consumed as an externally supplied fact from an identity & access system; this Feature never issues or authenticates identity.
- Deciding what context exists or is relevant to the request being evaluated — that remains Context Assembly's responsibility (FEP-002-CAP-08 §3, Non-Responsibilities); evaluation gates access to what Assembly has already determined is relevant, it does not determine relevance itself.
- Storing or becoming the system of record for the content whose access is being evaluated (FEP-002-CAP-08 §3, Non-Responsibilities).

## 5. Engineering Requirements

1. Evaluation must take as input a consumer's asserted identity, the specific context or request in question, and the applicable policy already resolved for that governance target.
2. Evaluation must produce exactly one of the defined outcomes — at minimum, permitted or denied — for any given input combination.
3. Identical inputs (same identity assertion, same context request, same policy state) must always produce the identical decision, regardless of evaluation order, timing, or any prior evaluation.
4. Evaluation must treat the identity assertion it receives as an externally supplied fact — it must not validate, issue, or modify it.
5. Every context request that reaches Context Assembly or Context Delivery must have been subject to evaluation; no request path may bypass it.
6. A genuine change in policy state or identity assertion between two evaluations must be capable of producing a different decision, while identical state continues to yield identical decisions.
7. A context request for which no policy explicitly applies must still receive an explicit decision per a documented default, never an undefined or absent outcome.

## 6. Inputs

- A consumer's asserted identity, sourced from an external identity & access system.
- The specific context or request a decision is needed for.
- The applicable resolved policy for that governance target (F08.1.1, F08.1.2).

## 7. Outputs

- A permission decision — at minimum, permitted or denied — for the given identity, context, and policy combination.

## 8. Preconditions

- A resolvable policy must exist for the governance target in question, at its declared granularity (F08.1.1, F08.1.2).
- An identity assertion must be available from an external identity & access system (FEP-001 §6) for the consumer making the request.

## 9. Postconditions

- Every context request that reached evaluation has an associated, evaluated decision.
- Context Assembly and Context Delivery can act on a decision, rather than proceeding without one.
- No context has been delivered without an explicit, evaluated permission decision behind it (FEP-002-CAP-08 §9, Success Criteria).

## 10. Dependencies

**Capability dependencies.** Workspace Definition — indirectly, via the policy this Feature evaluates against, which in turn depends on workspace configuration.

**Epic dependencies.** E08.1 (Policy Definition & Scope) — must be functioning before evaluation can occur, since there is nothing to evaluate against otherwise (FEP-003-EPIC-CAP-08 §5, Execution Order).

**Feature dependencies.** F08.1.2 (Policy Scope Granularity) — the explicit prerequisite Feature per the epic file's E08.2 Features table, ensuring a single applicable policy is resolvable before evaluation runs.

**External dependencies.** An identity & access system category (FEP-001 §6) supplying identity assertions — explicitly outside Ferret's own engineering program (FEP-003-EPIC-CAP-08 §4, §7).

## 11. Constraints

**Business constraints.** Policy evaluation must be consistent — the same consumer, context, and policy state must yield the same decision every time, or the product cannot be trusted for compliance-sensitive use (FEP-002-CAP-08 §8, Business). This is the defining constraint of this Feature.

**Product constraints.** Denial must be an explicit, recorded outcome, never an implicit side effect of context simply not being assembled (FEP-002-CAP-08 §8, Product).

**Context integrity constraints.** The evaluation outcome must be unambiguous for every request evaluated — an undecided or partially-computed state must never be treated as if it were a completed decision.

**Trust constraints.** Per Product Principle P4 (No privileged consumer), evaluation logic must apply identically regardless of which consumer or capability triggered the request.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), evaluation must not decide what context exists or is relevant — that decision remains Context Assembly's, per FEP-002-CAP-08 §3.

## 12. Acceptance Criteria

1. Given identical identity assertion, context request, and policy state, repeated evaluations produce the identical decision without exception.
2. Every context request observed reaching Context Assembly or Context Delivery has a corresponding evaluated decision preceding it.
3. A context request for which no policy explicitly applies still receives an explicit decision per the documented default, never an absence of decision.
4. The identity used in a decision is traceable to the original externally supplied assertion, with no alteration or reissuance by this Feature.

## 13. Validation Requirements

- That decision determinism holds across repeated identical inputs, including across time.
- That no request path exists that reaches Context Assembly or Context Delivery without a preceding evaluation.
- That a policy gap (no explicit applicable policy) resolves to the documented default deterministically.
- That evaluation's output is independent of which consumer or capability issued the request (Product Principle P4).

## 14. Failure Conditions

- **Silent over-permission.** A policy gap defaults to allow rather than deny, leaking context to consumers who should not have received it (FEP-002-CAP-08 §10, Failure Modes). Expected behavior: this must never occur — the documented default must be deny, and any deviation is a detectable failure, never a silent leak (Product Principle P5).
- **Silent under-permission.** An overly conservative default denies legitimate consumers without an explainable reason, undermining trust in the system's usefulness (FEP-002-CAP-08 §10). Expected behavior: a denial must always be explainable via the decision record (F08.3.1), even though recording itself is out of this Feature's scope.
- **Policy/identity mismatch.** A stale or mismatched identity assertion causes a decision to be made against the wrong identity (FEP-002-CAP-08 §10). Expected behavior: this must be detectable and surfaced — an affected decision must never be treated as valid, and the mismatch must not be resolved silently.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goal G4 (Trustworthy context — a consumer can only trust delivered context if access to it was properly and consistently gated) → Product Principles P1 (Context over computation — evaluation is a factual gate, not a judgment call), P4 (No privileged consumer), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-08 (Access Control & Policy) → Epic E08.2 (Permission Evaluation) → Feature F08.2.1 (Permission Evaluation Engine).

## 16. Future Considerations

- Richer, more nuanced permission outcomes beyond binary and simple partial, deferred pending real enterprise governance requirements (FEP-003-EPIC-CAP-08 §8; FEP-002-CAP-08 §11).
- Cross-workspace policy reconciliation as Federation matures — deciding how a permission granted in one workspace interacts with a related workspace's policy — will affect what "applicable policy" means as an input to this Feature (FEP-002-CAP-08 §11).
