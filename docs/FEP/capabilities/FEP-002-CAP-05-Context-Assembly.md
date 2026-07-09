# FEP-002-CAP-05 — Context Assembly

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-05 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.5 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

Organized context describes everything Ferret knows in general; it does not, by itself, answer a specific question. Context Assembly exists to turn the general body of current, structured context into the specific, relevant response a particular request actually needs.

## 2. Responsibilities

- Interpret what a request is asking for, at the level of what context would satisfy it.
- Select relevant structured context from what Organization has produced, respecting what Maintenance has marked eligible.
- Rank and prioritize selected context by relevance to the request.
- Compose selected context into a coherent body appropriate to the request's scope and any stated constraints.
- Indicate, alongside the assembled result, where the assembly is incomplete — relevant context existed but was excluded due to staleness, access restriction, or a stated constraint.

## 3. Non-Responsibilities

- Must never acquire or structure new context — it works only with what Organization and Maintenance have already produced.
- Must never decide how the result reaches the consumer — that belongs to Context Delivery.
- Must never bypass Access Control & Policy — it composes from what a given requester is permitted to see, not from everything that exists.
- Must never treat one class of consumer's request shape as more legitimate than another's, per Product Principle P4.

## 4. Inputs

- A request describing what context is needed, expressed conceptually as intent and constraints.
- Structured context from Context Organization.
- Freshness and eligibility state from Context Maintenance.
- Permission state from Access Control & Policy.

## 5. Outputs

- An assembled body of context relevant to the request.
- An indication of completeness or gaps — what was excluded, and why.

## 6. Context Objects

- **Request** — a conceptual expression of what context is being asked for.
- **Assembled Context** — the composed, relevant body of context produced in response to a request.
- **Assembly Gap** — a conceptual record of relevant-but-excluded context and the reason for its exclusion.

## 7. Relationships

Consumes structured context from Context Organization, freshness and eligibility from Context Maintenance, and permission decisions from Access Control & Policy. Supplies its result to Context Delivery. Reports outcomes, including gaps, to Provenance & Attribution and Observability & Health.

## 8. Constraints

- **Business.** Assembly must remain consumer-neutral — the same request, from different consumers with the same permissions, is assembled by the same logic, per Product Principle P4.
- **Product.** Assembly must respect stated scope or size constraints without silently dropping relevant context to fit; a real trade-off must surface as an Assembly Gap.
- **Context integrity.** Assembly must never present a partial result as complete, per Product Principle P5; every exclusion must be attributable to a specific, recorded reason.

## 9. Success Criteria

- The assembled context is demonstrably relevant to what was asked.
- Every exclusion from an assembly is explainable by a specific, recorded reason: staleness, access, absence, or a stated constraint.
- The same class of request produces consistent assembly behavior regardless of which consumer issued it.

## 10. Failure Modes

- **Silent truncation** — relevant context is dropped to fit a constraint without recording that it happened, violating P5.
- **Relevance drift** — assembly logic quietly favors the request shape most common from one consumer, disadvantaging others, violating P4.
- **Stale leakage** — context Maintenance has marked stale or unknown is assembled as though current.
- **Access bypass** — assembly draws on context the requester was not permitted to see because the interaction with Access Control & Policy was incomplete.

## 11. Future Evolution

Increasingly sophisticated relevance and ranking logic as the diversity of request shapes grows. Assembly spanning multiple workspaces as Federation matures, requiring relevance judgments that cross workspace boundaries. Feedback-informed assembly, where a consumer's observable downstream use of assembled context informs future relevance judgments — bounded carefully so as not to reintroduce reasoning or generation into Ferret's own scope.
