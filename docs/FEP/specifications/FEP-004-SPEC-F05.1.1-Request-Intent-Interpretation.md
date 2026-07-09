# FEP-004-SPEC-F05.1.1 — Request Intent Interpretation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F05.1.1 |
| **Capability** | [Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Epic** | E05.1 — Request Interpretation |
| **Feature** | F05.1.1 — Request Intent Interpretation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md), [FEP-003-EPIC-CAP-05 — Context Assembly](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md), [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md), [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

## 2. Purpose

Assembly cannot select relevant context until it knows what a request is actually asking for. This specification exists to define what it means for Ferret to correctly and consistently interpret a request's intent, so that the Product Outcome — providing the basis for selecting relevant structured context — is achieved regardless of how the request happened to be phrased or which consumer issued it.

## 3. Scope

- Interpreting the conceptual intent of a request: what subject matter, topic, or engineering concern the request is about.
- Recognizing when two differently phrased requests express the same underlying intent.
- Producing an interpreted-intent representation usable by downstream selection (F05.2.1).
- Applying this interpretation consistently across consumer types, per Product Principle P4.

## 4. Out of Scope

- Recognizing scope or size constraints stated in a request — that is F05.1.2 (Constraint Recognition).
- Selecting or ranking any structured context — that is E05.2.
- Composing a result or reporting gaps — that is E05.3.
- Reasoning about, evaluating, or drawing conclusions from the request's subject matter (FEP-001 Non-Goal: Ferret does not reason about or generate engineering artefacts).
- Establishing the requester's identity or permissions — that is Access Control & Policy.
- Any decision about how a request is transported to Assembly or how a result is delivered back — that is Context Delivery.

## 5. Engineering Requirements

1. Assembly must derive a conceptual intent from a request independent of the literal wording used to express it.
2. Two requests that a reasonable domain reader would judge equivalent must resolve to the same interpreted intent.
3. Interpretation must not vary based on which consumer type (human, AI system, tool) issued the request, for equivalent requests.
4. Interpretation must be deterministic for a given request under unchanged organized context: the same request interpreted twice must yield the same intent.
5. Interpretation must produce a representation stable enough to be consumed by selection (F05.2.1) without further disambiguation by that feature.
6. Cases where a request's intent cannot be resolved (too ambiguous, out of any known subject matter) must be distinguishable from a successfully interpreted intent, not silently defaulted.

## 6. Inputs

- A request, expressed conceptually as a statement of what context is needed.
- The vocabulary and structure of organized context available from Context Organization, insofar as it informs what subject matter can be recognized.

## 7. Outputs

- An interpreted intent: a conceptual representation of what the request is asking for, usable by Selection & Ranking (E05.2).
- An indication that a request's intent could not be resolved, where applicable.

## 8. Preconditions

- A request has been received in a form Assembly can process conceptually.
- Context Organization has produced at least a minimal structured vocabulary of subject matter for the workspace (per FEP-001 §2.3).

## 9. Postconditions

- Every processed request has an associated interpreted intent, or an explicit unresolved-intent indication.
- The interpreted intent is available for use by Selection & Ranking without requiring re-interpretation.

## 10. Dependencies

**Capability dependencies.** Context Organization (supplies the structured vocabulary interpretation draws on).

**Epic dependencies.** None within this capability precede E05.1; E05.1 is the entry point of Context Assembly.

**Feature dependencies.** None within this capability (per the epic file's Dependencies column: "None within this capability").

**External dependencies.** None directly; consumer systems (FEP-001 §6) are the source of requests but their identity/authentication is out of scope here.

## 11. Constraints

**Business constraints.** Interpretation logic must be the same regardless of which consumer issued the request, per Product Principle P4 (No privileged consumer).

**Product constraints.** Interpretation must not encode assumptions specific to one request shape at the expense of others (per the capability's Business constraint on consumer neutrality).

**Context integrity constraints.** Interpretation draws only on already-organized context vocabulary; it must never imply new context has been acquired or structured to resolve intent (per the capability's Non-Responsibility: never acquire or structure new context).

**Trust constraints.** An unresolved intent must be reported as such, not guessed at silently, consistent with Product Principle P5 (Degrade by scope, not by silent omission).

**Policy constraints.** None specific to this feature; permission evaluation is out of scope here and belongs to Access Control & Policy.

## 12. Acceptance Criteria

1. Given two requests phrased differently but expressing the same subject matter, both resolve to the same interpreted intent.
2. Given the same request submitted by two different consumer types, the interpreted intent is identical.
3. Given a request whose subject matter cannot be matched to any organized context vocabulary, the system produces an explicit unresolved-intent indication rather than a fabricated or default intent.
4. Given the same request submitted twice under unchanged organized context, the interpreted intent is identical both times.

## 13. Validation Requirements

- Validate that equivalence classes of differently phrased requests consistently map to the same interpreted intent.
- Validate that interpretation output is independent of consumer type.
- Validate that unresolved intent is surfaced explicitly rather than defaulted.
- Validate determinism of interpretation under repeated identical input and unchanged organized context.

## 14. Failure Conditions

- **Relevance drift at the interpretation stage** — interpretation logic favors the phrasing patterns typical of one consumer, disadvantaging others: must be detected and corrected, never allowed to silently persist, per Product Principle P4.
- **Silent default on ambiguity** — an unresolvable request is silently assigned an arbitrary intent: must instead surface as an explicit unresolved-intent indication, per Product Principle P5.
- **Inconsistent repeated interpretation** — the same request yields different intents across invocations under unchanged context: must be treated as a defect, since it breaks the Completion Criterion of consistent resolution.

## 15. Traceability

Product Vision (Mission: assemble engineering context) → Goals G1 (Completeness), G3 (Consumer neutrality) → Product Principles P1, P4, P5 → Capability FEP-002-CAP-05 (Context Assembly) → Epic E05.1 (Request Interpretation) → Feature F05.1.1 (Request Intent Interpretation).

## 16. Future Considerations

- Increasingly sophisticated relevance and interpretation logic as the diversity of request shapes grows (per capability file §11).
- Feedback-informed refinement of interpretation based on observed downstream use — deferred pending a bounded design that does not reintroduce reasoning into Ferret's scope (per epic file §8).
