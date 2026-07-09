# FEP-004-SPEC-F04.1.1 — Source Change Detection

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F04.1.1 |
| **Capability** | [Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Epic** | E04.1 — Change Detection |
| **Feature** | F04.1.1 — Source Change Detection |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md); [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md); [FEP-003-EPIC-CAP-04 — Context Maintenance](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md); [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md); [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

This specification exists to define what it means to detect that a source may have changed, via push notification or polling, so that Context Acquisition has the trigger it needs to re-read that source. Its Product Outcome — providing the trigger Acquisition needs — is the sole basis for what this Feature must do; it defines detection, not the response to detection.

## 3. Scope

- Recognizing that a specific, identified source within a workspace's declared scope may have changed.
- Accepting either interaction shape for learning of the change — a source pushing a notification, or Ferret polling — as equally valid, per FEP-001 §6.
- Producing a Change Signal that identifies the affected source and the fact that it may have changed.
- Meeting the detection latency bound a workspace has declared acceptable for that source.
- Making the absence of a detected change, within the declared bound, an observable fact rather than a silent non-event.

## 4. Out of Scope

- Re-reading the source's content (Context Acquisition's responsibility; Context Maintenance must never itself re-read a source).
- Consuming structural change signals from Context Organization or scope change signals from Workspace Definition (F04.1.2).
- Triggering re-acquisition or re-organization as a result of a detected change (F04.3.1).
- Tracking freshness state or age for a context unit (F04.2.1).
- Applying a workspace's freshness expectation to judge staleness (F04.2.2).
- Reasoning about, generating, or evaluating the substance of what changed (outside Ferret entirely, per FEP-001 Non-Goals).
- Deciding what is delivered to a consumer.

## 5. Engineering Requirements

1. The capability must be able to receive or solicit an indication that a specific, identified source may have changed, regardless of whether that indication arrives via push notification or polling.
2. Every detected indication must identify which source, per Workspace Definition's known source identity, it pertains to.
3. Detection must occur within the latency bound the workspace has declared acceptable for that source.
4. A detected change must be recorded as a Change Signal, available for consumption by other Context Maintenance features.
5. The absence of a detected change within the declared bound must itself be an observable fact, never silently equated with "confirmed no change."
6. Detection must depend only on whether change may have occurred, not on interpreting what the change contains.

## 6. Inputs

- A raw indication that a source may have changed (an event, a notification, or the outcome of a check).
- The workspace's declared identity for the source being monitored.
- The workspace's declared freshness expectation for that source, as the bound detection is measured against.

## 7. Outputs

- A Change Signal identifying a specific source that may have changed and when the indication was received.
- An observable record of "checked, no change found" where applicable, distinct from "not yet checked."

## 8. Preconditions

- The source is already known and tracked as reachable or unreachable (F02.1.2 — Source Reachability Tracking).
- The workspace's scope and source identity have already been declared (Workspace Definition).

## 9. Postconditions

- Every actual change at a monitored source produces a corresponding Change Signal within the workspace's declared bound.
- No monitored source's change goes undetected beyond its declared bound without that lateness being observable.

## 10. Dependencies

**Capability dependencies.** Workspace Definition (source identity and freshness expectation); Context Acquisition (source reachability, as the eventual consumer of this Feature's output).

**Epic dependencies.** E02.1 — Source Discovery.

**Feature dependencies.** F02.1.2 — Source Reachability Tracking.

**External dependencies.** Source systems (the category of external system whose change is being detected); change-notification sources (the category of external interaction, push or poll, through which detection occurs).

## 11. Constraints

**Business constraints.** Freshness expectations vary per workspace, per Workspace Definition's configuration; detection honors each workspace's declared bound rather than one fixed standard.

**Product constraints.** Detection must never present an unconfirmed state as "no change occurred" — an unchecked or inconclusive source must resolve honestly, per Product Principle P3.

**Context integrity constraints.** A Change Signal must be scoped strictly to the source it concerns and must not be conflated with structural or scope change (P6 — Maintenance's boundary is a capability boundary, not a convenience grouping).

**Trust constraints.** Detection must not assert that a change happened, or did not happen, beyond what the received indication actually supports (P2).

**Policy constraints.** None beyond the workspace's own declared configuration; no capability-specific access policy applies to detection itself.

## 12. Acceptance Criteria

1. Given a monitored source whose content changes, a Change Signal identifying that source is produced within the workspace's declared freshness expectation window.
2. Given a monitored source whose content does not change, no Change Signal is produced for that source within the same window.
3. Every Change Signal produced is attributable to exactly one identified source.
4. Detection functions correctly whether the source uses a push or a poll interaction shape.
5. When a scheduled or expected check does not run, that omission is itself observable and distinguishable from a confirmed "no change" outcome.

## 13. Validation Requirements

- That detection latency, per source, is measurable against the workspace's declared bound.
- That both push and poll interaction shapes are exercised and produce equivalent Change Signal behavior.
- That an unreachable source (per F02.1.2) is not misreported as "no change" but as an inconclusive/unknown detection outcome.
- That every produced Change Signal correctly identifies its source.

## 14. Failure Conditions

- **Change storms.** Overly sensitive detection produces an excessive volume of Change Signals for a source. Expected behavior: signal volume must remain observable so the condition is visible to Observability & Health, never silently absorbed downstream.
- **Freshness blindness.** The workspace's freshness expectation for a source is undeclared or unclear, leaving no bound to detect against. Expected behavior: this is surfaced as an explicit configuration gap, never resolved by an unstated default.
- **Silent staleness (precursor).** A check that should have run does not run. Expected behavior: this omission must be recorded as an observable, distinct outcome rather than defaulting to an assumed "no change."

## 15. Traceability

Product Vision (Mission: continuously acquire, organize, maintain... context) → Goals G2 (Currency of context), G5 (Extensible acquisition, since either interaction shape is valid) → Product Principles P3 (Freshness is first-class), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-04 (Context Maintenance) → Epic E04.1 (Change Detection) → Feature F04.1.1 (Source Change Detection).

## 16. Future Considerations

- Change-signal granularity may need to be reconciled with how Acquisition and Organization define their own units of change, as the program's execution track proceeds (per the epic's identified risk).
- Predictive staleness — anticipating likely change before a signal arrives — is deferred pending real change-pattern data, and would build on this Feature's detection mechanism without altering its current, reactive scope.
