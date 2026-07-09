# FEP-004-SPEC-F06.3.1 — Access-Gated Delivery

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F06.3.1 |
| **Capability** | [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) |
| **Epic** | E06.3 — Access-Respecting Hand-off |
| **Feature** | F06.3.1 — Access-Gated Delivery |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-06 — Context Delivery](../epics/FEP-003-EPIC-CAP-06-Context-Delivery.md) · [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Assembly may select context that is, in principle, eligible; but the permission decision governing a specific consumer's specific request is made by Access Control & Policy, and time passes between selection and hand-off. Access-Gated Delivery exists to close that gap: to ensure nothing reaches a consumer at the point of hand-off unless Access Control & Policy has actually permitted it for that consumer's request.

## 3. Scope

- Checking Access Control & Policy's permission decision for a given consumer and request at the moment of hand-off.
- Gating hand-off so that denied context is withheld from the consumer.
- Ensuring the gating check is applied to every hand-off, regardless of delivery surface (F06.1.1) or delivery mode (one-off or subscription notification, F06.2.2).

## 4. Out of Scope

- Making the permission decision itself — that is F08.2.1 (Permission Evaluation Engine), owned by Access Control & Policy; this Feature consumes that decision, it does not produce it.
- Distinguishing a denial from an absence for the consumer — that is F06.3.2 (Denial/Absence Disambiguation), which builds on this Feature's gating outcome.
- Selecting the delivery surface — that is F06.1.1 (Delivery Surface Selection), a precondition of this Feature.
- Preserving fidelity of what is ultimately delivered — that is F06.1.2 (Fidelity-Preserving Presentation), applied to whatever survives this Feature's gate.
- Determining what context was eligible for selection in the first place (freshness/permission-aware selection) — that is Context Assembly's F05.2.1 (Eligibility-Respecting Selection); this Feature is a second, independent enforcement point at hand-off, not a replacement for Assembly's own eligibility check.

## 5. Engineering Requirements

1. Every hand-off of assembled context to a consumer must be preceded by a check against Access Control & Policy's permission decision for that specific consumer and request.
2. Context for which the permission decision is "denied" must never reach the consumer through any delivery surface or delivery mode.
3. The gating check must be applied identically regardless of whether the hand-off is a one-off response or a subscription notification (F06.2.2).
4. A change in the underlying permission decision between Assembly's selection and the moment of hand-off must be reflected in the gating outcome at hand-off time, not the possibly stale state at selection time.
5. Gating must not require or depend on Context Assembly having made its own eligibility determination correctly — this Feature is an independent enforcement point, not solely reliant on upstream correctness.

## 6. Inputs

- Assembled context ready for hand-off, from Context Assembly.
- Access Control & Policy's permission decision for the requesting consumer and the request in question (F08.2.1 — Permission Evaluation Engine).

## 7. Outputs

- Gated context: the subset of assembled context actually permitted to reach the consumer.
- A recorded outcome of denial, for any content withheld by this gate.

## 8. Preconditions

- A delivery surface has been selected for the request (F06.1.1 — Delivery Surface Selection).
- Access Control & Policy has evaluated and can supply a permission decision for the consumer and request (F08.2.1 — Permission Evaluation Engine).

## 9. Postconditions

- A consumer never receives context that Access Control & Policy did not permit for them, regardless of what Assembly selected.
- Every instance of withheld content at hand-off corresponds to an actual permission denial, not an assembly-time omission being mistaken for one.

## 10. Dependencies

**Capability dependencies.** Access Control & Policy (source of the permission decision this Feature enforces).

**Epic dependencies.** E08.2 (Permission Evaluation) — per FEP-003-EPIC-CAP-06 §4, Prerequisite Epics.

**Feature dependencies.** F06.1.1 (Delivery Surface Selection), F08.2.1 (Permission Evaluation Engine) — per the E06.3 Features table.

**External dependencies.** Identity & access systems (FEP-001 §6) — the category of external system whose assertions ultimately inform the permission decision this Feature enforces, consumed indirectly via Access Control & Policy.

## 11. Constraints

**Business constraints.** Access restrictions must be enforced without exception at the point of hand-off, regardless of any upstream selection behavior (FEP-002-CAP-06 §9, Success Criteria).

**Product constraints.** Delivery must never grant access beyond what Access Control & Policy has determined (FEP-002-CAP-06 §3, Non-Responsibility 3).

**Context integrity constraints.** A gap between Assembly and Delivery must never result in access leakage — the enforcement point represented by this Feature exists precisely to close that gap (FEP-002-CAP-06 §10, Failure Modes — "Access leakage").

**Trust constraints.** Per P4, gating must apply identically to every consumer type; no consumer type may bypass or receive relaxed gating.

**Policy constraints.** Per P6, this Feature must not itself evaluate policy or identity — it consumes Access Control & Policy's decision as given.

## 12. Acceptance Criteria

1. A consumer denied access to specific context by Access Control & Policy never receives that context through any delivery surface.
2. A change in permission decision occurring after Assembly's selection but before hand-off is reflected correctly in the gating outcome.
3. Gating is applied identically to one-off deliveries and subscription notifications.
4. Every instance of withheld content at hand-off is traceable to an actual permission denial, verifiable against the permission decision recorded by Access Control & Policy.
5. No delivery surface or delivery mode exists that bypasses this gating check.

## 13. Validation Requirements

- That no simulated denied-access scenario results in the consumer receiving the denied content, across every supported delivery surface and mode.
- That gating reflects the permission decision current at hand-off time, not at selection time.
- That every gating outcome is traceable to a specific permission decision.

## 14. Failure Conditions

- **Access leakage.** A consumer receives context Access Control & Policy had not permitted, due to a gap between Assembly and Delivery (FEP-002-CAP-06 §10). Expected behavior: this must never occur; where a near-miss is detected, it must be surfaced as a defect requiring correction, not tolerated as a rare exception.
- **Over-withholding.** Content is withheld from a consumer despite an actual permission decision to allow it. Expected behavior: distinguishable from a genuine denial and surfaced as a gating defect, since it degrades completeness (G1) without cause.

## 15. Traceability

Product Vision (Mission: delivers engineering context to any human, AI system, or engineering tool that needs it) → Goal G4 (Trustworthy context) → Product Principles P4 (No privileged consumer), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-06 (Context Delivery) → Epic E06.3 (Access-Respecting Hand-off) → Feature F06.3.1 (Access-Gated Delivery).

## 16. Future Considerations

- Delivery patterns spanning federated workspaces, once Federation matures, must preserve this Feature's gating guarantee within each participating workspace (FEP-002-CAP-06 §11).
- As Extensibility's E09.2 (Delivery Extension Points) introduces new consumer types, each new delivery surface must be verified to route through this same gating point without exception.
