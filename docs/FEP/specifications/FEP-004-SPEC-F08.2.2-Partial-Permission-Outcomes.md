# FEP-004-SPEC-F08.2.2 — Partial Permission Outcomes

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F08.2.2 |
| **Capability** | [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) |
| **Epic** | E08.2 — Permission Evaluation |
| **Feature** | F08.2.2 — Partial Permission Outcomes |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-08 — Access Control & Policy](../epics/FEP-003-EPIC-CAP-08-Access-Control-Policy.md) · [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A binary permitted-or-denied outcome cannot express governance requirements such as "may know this exists, not its content." Partial Permission Outcomes exists to satisfy F08.2.2's objective — supporting a "partially permitted" outcome distinct from binary allow/deny, where policy calls for it — delivering the product outcome of nuanced governance that a purely binary evaluation model cannot provide.

## 3. Scope

- Producing a "partially permitted" evaluation outcome, distinguishable from both "permitted" and "denied," when the applicable policy declares a requirement for partial treatment.
- Ensuring a partial outcome specifies what is permitted and what is not, consistent with what the governing policy declared.
- Guaranteeing that partial outcomes are produced only where policy explicitly calls for them, and are as deterministic as any binary outcome.

## 4. Out of Scope

- The baseline evaluation mechanics that determine permitted versus denied — that is F08.2.1 (Permission Evaluation Engine), which this Feature extends rather than replaces.
- Declaring a policy, or the specific mechanism by which a policy states it requires partial treatment — that is F08.1.1 (Policy Declaration) and F08.1.2 (Policy Scope Granularity).
- Recording a partial decision or making it auditable after the fact — that is F08.3.1 (Decision Recording & Audit Surfacing).
- Defining what "existence-only" visibility looks like in delivered content, or how Context Assembly or Context Delivery present a partial outcome to a consumer — that remains those capabilities' responsibility; this Feature only produces the decision they must honor.
- Establishing or issuing consumer identity — per FEP-001 §5.2/§6, identity is consumed from external systems, never issued by Ferret.

## 5. Engineering Requirements

1. Where the applicable policy declares a requirement for partial permission, evaluation must be capable of producing a "partially permitted" outcome distinguishable from both "permitted" and "denied."
2. A partial outcome must specify what is permitted and what is not (for example, existence without content), consistent with what the governing policy declared.
3. Partial-permission evaluation must be deterministic on the same basis as binary evaluation: identical identity, context, and policy state must always produce the identical partial outcome.
4. A policy that does not declare a partial-permission requirement must never produce a partial outcome.
5. Consumers of the decision (Context Assembly, Context Delivery) must be able to distinguish a partial outcome from a binary outcome without inferring it indirectly.

## 6. Inputs

- A permission evaluation request, as in F08.2.1, where the applicable resolved policy declares a partial-permission requirement.

## 7. Outputs

- A partial-permission decision, distinguishable from permitted and denied, describing what is and is not permitted for the given request.

## 8. Preconditions

- F08.2.1 (Permission Evaluation Engine) must exist and function — a partial outcome is a variant of the same evaluation act, not an independent one.
- The applicable policy for the governance target must have declared a requirement for partial treatment (a concern of F08.1.1/F08.1.2 at declaration time).

## 9. Postconditions

- A context request governed by a partial-permission policy yields a decision that Context Assembly and Context Delivery can act on distinctly from a full permit or a full deny.
- No partial outcome is produced for a request governed only by a binary policy.

## 10. Dependencies

**Capability dependencies.** None beyond what F08.2.1 already depends on.

**Epic dependencies.** E08.1 (Policy Definition & Scope) — transitively, since the underlying policy must be able to declare a partial-permission requirement before this Feature can produce that outcome.

**Feature dependencies.** F08.2.1 (Permission Evaluation Engine) — the explicit prerequisite Feature per the epic file's E08.2 Features table.

**External dependencies.** An identity & access system category (FEP-001 §6), the same external dependency as F08.2.1, since this Feature evaluates the same identity assertions.

## 11. Constraints

**Business constraints.** The consistency constraint governing binary evaluation (FEP-002-CAP-08 §8, Business) applies equally to partial outcomes: identical inputs must always yield the identical partial outcome.

**Product constraints.** A partial outcome must be an explicit, recorded-eligible outcome in its own right, never an implicit blend or approximation of permit and deny.

**Context integrity constraints.** Partial permission — for example, permitted to know something exists but not its full content — must be a distinguishable outcome where a workspace's policy calls for that distinction, not collapsed into a binary allow or deny (FEP-002-CAP-08 §8, Context integrity). This is the defining constraint of this Feature.

**Trust constraints.** Per Product Principle P4 (No privileged consumer), partial-outcome logic must apply uniformly regardless of which consumer or capability triggered the request.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), this Feature does not decide how partial visibility is presented in delivered content — it only produces the decision Context Assembly and Context Delivery must honor.

## 12. Acceptance Criteria

1. A policy declared to require partial permission produces, for a qualifying request, an outcome distinguishable from both full permit and full deny.
2. A policy not declaring a partial-permission requirement never produces a partial outcome for any request.
3. Identical identity, context, and policy state yield the identical partial outcome across repeated evaluations.
4. A partial outcome specifies, at minimum, what is permitted and what is not, consistent with the governing policy's declaration.

## 13. Validation Requirements

- That partial outcomes arise only from policies that explicitly declare the requirement.
- That partial-outcome determinism holds under repeated identical inputs.
- That partial outcomes remain distinguishable from binary outcomes at every point they are consumed downstream.

## 14. Failure Conditions

- **Partial outcome collapsed into binary.** A policy calling for partial permission instead yields a plain allow or deny. Expected behavior: this is a failure state; per Product Principle P5, the system must surface it as a defect, never treat the collapsed binary outcome as an acceptable substitute.
- **Ambiguous partial outcome.** A partial decision is produced without specifying what is and is not permitted. Expected behavior: this must be treated as an incomplete decision, not a valid partial outcome — per Product Principle P5, incompleteness must be visible, never silently accepted downstream as complete.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G3 (Consumer neutrality — nuanced outcomes serve varied governance needs without favoring one consumer's shape of interaction), G4 (Trustworthy context) → Product Principles P4 (No privileged consumer), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-08 (Access Control & Policy) → Epic E08.2 (Permission Evaluation) → Feature F08.2.2 (Partial Permission Outcomes).

## 16. Future Considerations

- Richer, more nuanced permission outcomes beyond binary and simple partial, deferred pending real enterprise governance requirements (FEP-003-EPIC-CAP-08 §8).
- Increasingly rich permission outcomes as enterprise use cases demand more nuanced governance (FEP-002-CAP-08 §11).
