# FEP-003-EPIC-CAP-06 — Engineering Program: Context Delivery

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-06 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Context Delivery makes assembled context available to a requesting consumer, in a form appropriate to that consumer, without altering its substance. It does not decide what to include, and it never grants access beyond what Access Control & Policy has determined.

## 2. Engineering Epics

### E06.1 — Consumer-Fit Presentation

- **Purpose.** Present assembled context via a surface appropriate to the consumer.
- **Scope.** Selecting an appropriate delivery surface per consumer shape; preserving fidelity and completeness indications.
- **Success Definition.** Every consumer receives what was assembled for them, undistorted, via a fitting surface.

### E06.2 — Subscription & Notification

- **Purpose.** Support consumers with a standing interest in relevant context.
- **Scope.** Registering a standing interest (Subscription); delivering notification when relevant new or changed context becomes available.
- **Success Definition.** A consumer with a registered standing interest is notified of relevant change without needing to poll.

### E06.3 — Access-Respecting Hand-off

- **Purpose.** Enforce access control at the point of delivery and disambiguate denial from absence.
- **Scope.** Gating delivery on Access Control & Policy's determination; distinguishing denial from "nothing relevant existed."
- **Success Definition.** No consumer ever receives unpermitted context, and denial is never confused with absence.

## 3. Features

### E06.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F06.1.1 — Delivery Surface Selection | Select or provide a delivery surface appropriate to a consumer's declared shape of interaction. | Human, tool, and agent consumers each receive context in a form they can use. | F05.3.1 | Each supported consumer type has a defined, appropriate delivery surface. |
| F06.1.2 — Fidelity-Preserving Presentation | Present assembled context, including completeness indications, without altering substance. | A consumer's trust is not undermined by transformation loss at delivery. | F06.1.1 | Delivered content and its Assembly Gaps are verifiably unchanged in substance from what Assembly produced. |

### E06.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F06.2.1 — Subscription Registration | Allow a consumer to register a standing interest in context matching some description. | Enables proactive delivery rather than only reactive request/response. | F05.1.1 | A registered subscription is resolvable and persists until explicitly withdrawn. |
| F06.2.2 — Change Notification Delivery | Notify a subscribed consumer when relevant context changes. | Keeps a consumer's picture current without manual re-querying. | F06.2.1, F04.3.1 | A change to context matching an active subscription reliably results in a notification. |

### E06.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F06.3.1 — Access-Gated Delivery | Gate hand-off of assembled context on Access Control & Policy's permission decision. | Prevents access leakage between Assembly's selection and the consumer's actual receipt. | F06.1.1, F08.2.1 | A denied consumer never receives the corresponding context, verified against the permission decision. |
| F06.3.2 — Denial/Absence Disambiguation | Ensure a consumer can distinguish "you were denied this" from "nothing relevant existed." | Preserves trust in the system by never conflating restriction with absence. | F06.3.1 | A denial and an absence produce distinguishable outcomes for the consumer in every case. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F05.3.1 (Context Composition), F08.2.1 (Permission Evaluation Engine).
- **Prerequisite Epics.** E05.3 (Composition & Gap Reporting), E08.2 (Permission Evaluation).
- **Prerequisite Capabilities.** Context Assembly, Access Control & Policy.

## 5. Execution Order

1. **E06.1** — the baseline capability to deliver anything at all.
2. **E06.3** — sequenced before subscription support, since even one-off delivery must be access-gated correctly first.
3. **E06.2** — builds on the same delivery surfaces and access-gating already established, extending them to a standing-interest model.

## 6. Capability Completion Gates

- **Functional completeness.** Every supported consumer type can receive assembled context through an appropriate, fidelity-preserving surface, one-off and, once E06.2 lands, via subscription.
- **Validation readiness.** A simulated denial and a simulated absence are verified to be distinguishable to the consumer in every delivery surface.
- **Documentation readiness.** The Delivery Surface and Subscription concepts are documented clearly enough for a new consumer type to be evaluated against Extensibility's extension point without ambiguity.
- **Review completion.** FEP-002-CAP-06's non-responsibilities (no content decisions, no reasoning, no unauthorized access) confirmed unviolated.

## 7. Risks

- **Surface-richness imbalance.** Building a full-featured surface for one consumer type before others risks creating a de facto privileged consumer at the planning level, even if unintentional, violating Product Principle P4.
- **Subscription scope ambiguity.** "A standing interest in context matching some description" is loosely defined at the product level; this epic's features risk being unscoped until Assembly's request-interpretation concepts are stable.
- **Access-gating sequencing risk.** If E06.1 is delivered before E06.3, there is a planning-level temptation to ship a "temporary" ungated delivery path that becomes difficult to retract later.

## 8. Deferred Work

- Delivery patterns spanning federated workspaces — deferred to Federation.
- Expansion of delivery surface diversity beyond initial supported consumer types — deferred to Extensibility's maturity.
