# FEP-004-SPEC-F06.2.2 — Change Notification Delivery

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F06.2.2 |
| **Capability** | [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) |
| **Epic** | E06.2 — Subscription & Notification |
| **Feature** | F06.2.2 — Change Notification Delivery |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-06 — Context Delivery](../epics/FEP-003-EPIC-CAP-06-Context-Delivery.md) · [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A Subscription that never results in a notification is indistinguishable from no Subscription at all. Change Notification Delivery exists to make good on the standing interest registered in F06.2.1: when context matching an active Subscription changes, the subscribed consumer must learn of it without having to ask again.

## 3. Scope

- Recognizing that a change to context corresponds to an active Subscription's described standing interest.
- Delivering notification of that change to the subscribed consumer.
- Ensuring notification delivery occurs reliably for every change that matches an active Subscription.

## 4. Out of Scope

- Registering, resolving, or withdrawing Subscriptions — that is F06.2.1 (Subscription Registration), a precondition of this Feature.
- Detecting that a source itself has changed, or re-acquiring and re-organizing content as a result — that is F04.3.1 (Re-acquisition & Re-organization Triggering), owned by Context Maintenance; this Feature consumes the fact that change was already detected and processed, it does not perform that detection itself.
- Selecting the delivery surface used to carry the notification — that is F06.1.1 (Delivery Surface Selection), reused at notification time, not redefined here.
- Gating notification content on access permission — that is F06.3.1 (Access-Gated Delivery), applied to notifications as it is applied to one-off deliveries.
- Deciding what the notification's content should say beyond identifying the relevant change — no reasoning about or evaluation of the change is performed here.

## 5. Engineering Requirements

1. A change to context that matches an active Subscription's described standing interest must be recognized as relevant to that Subscription.
2. Recognition of a relevant change must reliably result in a notification being delivered to the subscribing consumer.
3. Notification delivery must occur through a delivery surface appropriate to the subscribing consumer, consistent with F06.1.1's surface-selection outcome for that consumer.
4. A change that does not match any active Subscription's description must not produce a notification to an unrelated consumer.
5. Notification delivery must not depend on the consumer polling or re-requesting; the initiative to notify lies with this Feature.
6. Failure to deliver a notification for a matching change must be an observable outcome, not a silent drop.

## 6. Inputs

- An active Subscription (F06.2.1) describing a standing interest.
- A signal that context has changed, following re-acquisition and re-organization (F04.3.1 — Re-acquisition & Re-organization Triggering).

## 7. Outputs

- A delivered notification to the subscribing consumer, identifying the relevant change.
- An observable indication when notification delivery for a matching change fails.

## 8. Preconditions

- A Subscription has been registered and is active (F06.2.1 — Subscription Registration).
- A change to context has been detected and the affected material re-acquired/re-organized (F04.3.1 — Re-acquisition & Re-organization Triggering).
- A delivery surface appropriate to the subscribing consumer has been established (F06.1.1 — Delivery Surface Selection).

## 9. Postconditions

- The subscribing consumer is aware of the relevant change without having issued a new request.
- Every change matching an active Subscription results in exactly one corresponding notification outcome (delivered or observably failed) — never silence.

## 10. Dependencies

**Capability dependencies.** Context Maintenance (source of the change-detection and re-processing signal this Feature reacts to); Context Delivery's own F06.1.1 for the notification's carrying surface.

**Epic dependencies.** E04.3 (per FEP-003 Global Output 3: E06.2 depends on E04.3) and E06.1 (Consumer-Fit Presentation, for the notification's delivery surface).

**Feature dependencies.** F06.2.1 (Subscription Registration), F04.3.1 (Re-acquisition & Re-organization Triggering) — per the E06.2 Features table.

**External dependencies.** Change-notification sources (FEP-001 §6) — the category of external system whose change signals ultimately originate the re-acquisition this Feature reacts to, consumed indirectly via F04.3.1.

## 11. Constraints

**Business constraints.** A notification must correspond to an actual, recognized change matching an actual, active Subscription — never a speculative or unconfirmed change.

**Product constraints.** Notification content must be presented with the same fidelity guarantees as a one-off delivery (F06.1.2 applies equally to notifications).

**Context integrity constraints.** A failed notification delivery must be distinguishable from "no relevant change occurred" — silence must never be ambiguous between these two states (Product Principle P5).

**Trust constraints.** Per P4, notification reliability must be equivalent across consumer types; no consumer type's Subscriptions may be treated as more reliably serviced than another's.

**Policy constraints.** Per P6, this Feature must not absorb change-detection responsibility (Context Maintenance) or access-gating responsibility (F06.3.1) that belong elsewhere.

## 12. Acceptance Criteria

1. A change matching an active Subscription's description reliably produces a notification to the subscribing consumer.
2. A change that does not match any active Subscription produces no notification to unrelated consumers.
3. Notification delivery uses a delivery surface consistent with the one selected for that consumer under F06.1.1.
4. A simulated notification-delivery failure for a matching change produces an observable failure outcome rather than silent non-delivery.
5. No consumer waits on polling to learn of a change matching its active Subscription.

## 13. Validation Requirements

- That every change matching an active Subscription is followed by a notification-delivery attempt.
- That notifications are not sent for changes outside the scope of any active Subscription.
- That notification-delivery failure is always observable, never silent.
- That notification fidelity matches one-off delivery fidelity guarantees.

## 14. Failure Conditions

- **Missed notification.** A change matches an active Subscription but no notification is delivered. Expected behavior: this must be detectable as a failure, surfaced for correction, never allowed to pass as though the Subscription had produced nothing relevant (P5).
- **Over-notification.** A change is delivered as a notification to a consumer whose Subscription does not actually match it. Expected behavior: treated as a matching-correctness defect to be surfaced and corrected, since it erodes trust in Subscription accuracy.

## 15. Traceability

Product Vision (Mission: continuously acquires, organizes, maintains ... and delivers engineering context) → Goals G2 (Currency of context), G3 (Consumer neutrality) → Product Principles P3 (Freshness is first-class), P4 (No privileged consumer), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-06 (Context Delivery) → Epic E06.2 (Subscription & Notification) → Feature F06.2.2 (Change Notification Delivery).

## 16. Future Considerations

- Maturation of subscription-based delivery alongside one-off request/response delivery (FEP-002-CAP-06 §11).
- Delivery patterns spanning federated workspaces, once Federation matures, extending notification reach across workspace boundaries while preserving the same reliability and fidelity guarantees within each workspace.
