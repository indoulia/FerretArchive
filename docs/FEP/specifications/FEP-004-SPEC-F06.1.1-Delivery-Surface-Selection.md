# FEP-004-SPEC-F06.1.1 — Delivery Surface Selection

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F06.1.1 |
| **Capability** | [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) |
| **Epic** | E06.1 — Consumer-Fit Presentation |
| **Feature** | F06.1.1 — Delivery Surface Selection |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-06 — Context Delivery](../epics/FEP-003-EPIC-CAP-06-Context-Delivery.md) · [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Assembled context that never reaches a consumer through a surface that fits how that consumer actually receives things is unusable to them regardless of how well it was assembled. Delivery Surface Selection exists to ensure that whichever shape of consumer requests context — human, tool, or agent — that consumer receives it through a surface appropriate to that shape, so consumer neutrality does not collapse into "whichever surface happened to be built first."

## 3. Scope

- Determining, per request, which delivery surface is appropriate to the requesting consumer's declared shape of interaction.
- Providing that surface as the channel through which the consumer receives context — conceptually, not as a specific protocol.
- Ensuring every supported consumer type (human-facing, tool-facing, agent-facing) has a defined, appropriate surface available to it.
- Recognizing when a consumer's declared shape does not match any currently supported surface, so that gap is visible rather than silently defaulted.

## 4. Out of Scope

- Preserving fidelity and completeness indications through the surface once selected — that is F06.1.2 (Fidelity-Preserving Presentation).
- Gating delivery on access permission — that is F06.3.1 (Access-Gated Delivery).
- Registering or resolving standing interests / subscriptions — that is F06.2.1 (Subscription Registration) and F06.2.2 (Change Notification Delivery).
- Deciding what context is relevant or complete for the request — that belongs entirely to Context Assembly (F05.3.1 and its epic), per FEP-002-CAP-06 Non-Responsibility 1.
- Reasoning about, acting on, or modifying the content being delivered — an explicit Non-Responsibility of this capability and a Non-Goal of the whole product (FEP-001 §1.3).
- Defining the general extension mechanism by which an entirely new consumer type could be added to the product — that is Extensibility's E09.2 (Delivery Extension Points), which depends on this Feature rather than subsuming it.

## 5. Engineering Requirements

1. A requesting consumer's declared shape of interaction must be resolvable to exactly one appropriate delivery surface among those currently supported.
2. Each supported consumer type (human-facing, tool-facing, agent-facing) must have a defined delivery surface associated with it before that consumer type can be considered supported at all.
3. Surface selection must depend only on the consumer's declared shape of interaction and must not vary based on which specific consumer is asking (Product Principle P4).
4. If a consumer's declared shape of interaction does not match any currently supported surface, that mismatch must be an observable, distinguishable outcome rather than a silent default to an unrelated surface.
5. Adding a new supported consumer type's delivery surface must not require altering how Context Assembly produces assembled context.
6. Surface selection must occur without inspecting or interpreting the substance of the assembled context being delivered.

## 6. Inputs

- Assembled context ready for hand-off, from Context Assembly.
- The requesting consumer's declared shape of interaction.

## 7. Outputs

- A selected, appropriate delivery surface for the request.
- An observable outcome when no supported surface matches the declared shape of interaction.

## 8. Preconditions

- Context Assembly has produced an assembled result available for hand-off (F05.3.1 — Context Composition).
- The requesting consumer has declared, in some form, the shape of interaction it expects.

## 9. Postconditions

- The consumer's request is paired with a delivery surface appropriate to how that consumer receives things.
- Every supported consumer type has a working path from "assembled result exists" to "a fitting surface is available to carry it."
- No consumer type receives a surface selection that differs in kind of care or richness from another purely because of who is asking (P4).

## 10. Dependencies

**Capability dependencies.** Context Assembly (assembled context must exist to be delivered).

**Epic dependencies.** E05.3 (Composition & Gap Reporting) — per FEP-003-EPIC-CAP-06 §4, Prerequisite Epics.

**Feature dependencies.** F05.3.1 (Context Composition) — per the E06.1 Features table.

**External dependencies.** Consumer systems (FEP-001 §6) — the category of external system whose declared shape of interaction this Feature reads, without this Feature defining how that declaration is transmitted.

## 11. Constraints

**Business constraints.** Delivery must not create a de facto privileged consumer by virtue of a richer surface being built for it first (FEP-002-CAP-06 §8, Business; Product Principle P4) — the underlying content available to any permitted consumer must be equivalent regardless of which surface was selected.

**Product constraints.** Selecting a surface must never itself become an occasion to reformat or reinterpret the assembled context (FEP-002-CAP-06 §8, Product) — surface selection is a routing decision, not a transformation.

**Context integrity constraints.** A consumer for whom no appropriate surface exists must be able to tell that this is the case, distinguishable from a request that produced no relevant context.

**Trust constraints.** Per P4, surface selection logic must be uniform across consumers of the same declared shape.

**Policy constraints.** Per P6, this Feature must not absorb fidelity preservation, access gating, or subscription responsibilities that FEP-002-CAP-06 assigns elsewhere.

## 12. Acceptance Criteria

1. Every declared shape of interaction among currently supported consumer types resolves to exactly one delivery surface.
2. Two distinct consumers declaring the same shape of interaction are routed to the same kind of delivery surface.
3. A declared shape of interaction that matches no supported surface produces an observable mismatch outcome, never a silent substitution.
4. Surface selection completes using only the declared shape of interaction and the fact that an assembled result exists — no inspection of the assembled result's content is required to select a surface.
5. Adding a new supported consumer type's surface does not require a change to how Context Assembly composes results.

## 13. Validation Requirements

- That every supported consumer type has an appropriate, defined delivery surface.
- That surface selection is consistent for a given declared shape of interaction across repeated requests.
- That an unsupported declared shape produces a distinguishable, observable outcome.
- That surface selection does not depend on which specific consumer identity is asking, only on declared shape.

## 14. Failure Conditions

- **De facto privileged consumer.** One consumer type's surface becomes functionally richer than others', contradicting P4 (FEP-002-CAP-06 §10). Expected behavior: this imbalance must be detectable at the level of surface definitions, not discovered only after consumers compare experiences.
- **Unmatched declared shape.** A consumer declares a shape of interaction with no defined surface. Expected behavior: the mismatch is surfaced to the requester as an explicit outcome, never defaulted silently to an arbitrary surface (Product Principle P5).

## 15. Traceability

Product Vision (Mission: delivers engineering context to any human, AI system, or engineering tool that needs it) → Goals G3 (Consumer neutrality), G5 (Extensible acquisition and delivery) → Product Principles P4 (No privileged consumer), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-06 (Context Delivery) → Epic E06.1 (Consumer-Fit Presentation) → Feature F06.1.1 (Delivery Surface Selection).

## 16. Future Considerations

- Growth in the diversity of delivery surfaces as new classes of human, agent, and tool consumers emerge (FEP-002-CAP-06 §11).
- This Feature is the attachment point for Extensibility's E09.2 (Delivery Extension Points); how new consumer types are formally on-boarded is deferred to that epic (FEP-003-EPIC-CAP-06 Global Output 3 cross-capability dependency).
