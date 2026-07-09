# FEP-002-CAP-10 — Observability & Health

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-10 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.10 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

A system that must be trusted blindly is not trustworthy. Observability & Health exists to make the actual state of every other capability knowable at any time, so that consumers and operators are never left simply hoping the rest of the model is working.

## 2. Responsibilities

- Collect and expose state from every other capability: what has been acquired, how current it is, what has been organized, what has been assembled and delivered, and what access decisions have been made.
- Surface degradation or failure in any capability — acquisition gaps, unexpectedly stale context, assembly unable to satisfy a request, policy misconfiguration — in an inspectable way.
- Provide the basis for judging whether the product goals in FEP-001 §1.2 are actually being met in practice, not just in principle.
- Support external routing of this state to observability sinks a deployer chooses to use (FEP-001 §6), without mandating any particular one.

## 3. Non-Responsibilities

- Must never take corrective action itself — it reports, it does not remediate; remediation is a decision made by whoever operates Ferret, or by a future capability not yet defined.
- Must never become a second source of truth for context — it reports on the state of context and capabilities, it does not hold or serve context itself.
- Must never be a prerequisite for any other capability to function — every other capability must work correctly even if Observability & Health itself is degraded, per FEP-001 §4.

## 4. Inputs

- State signals from every other capability: coverage and gaps from Acquisition, structural throughput from Organization, freshness and invalidation from Maintenance, assembly outcomes and gaps from Assembly, delivery outcomes from Delivery, decision records from Access Control & Policy, and lineage completeness from Provenance.

## 5. Outputs

- Inspectable state and health reports covering the whole capability model.
- Signals suitable for routing to external observability sinks, where a deployer chooses to do so.

## 6. Context Objects

- **Health Signal** — a conceptual unit of state a capability reports about itself.
- **Health Report** — a conceptual aggregation of health signals into a picture of the system's current state, at the level of a workspace or the whole product.

## 7. Relationships

Reads state from every other capability without those capabilities depending on it functionally, per FEP-001 §4. Its reports are the primary evidence for whether Product Goals G1 through G6 are being met.

## 8. Constraints

- **Business.** Observability must never be gated behind the same access model that gates content in a way that leaves operators unable to diagnose problems — operational visibility and content access are different concerns.
- **Product.** Health reporting must be honest about degradation; it must never present a degraded capability as healthy for the sake of a simpler status signal.
- **Context integrity.** Observability must distinguish "this capability is unhealthy" from "this capability is healthy but reporting an expected, policy-driven gap," such as an out-of-scope source — conflating the two would create false alarms that erode trust in the reporting itself.

## 9. Success Criteria

- Any capability's current state and any degradation within it is discoverable without needing to interrogate the capability directly.
- Health reporting accurately distinguishes real failure from expected, policy-driven gaps.
- The product's fulfillment of its own stated goals can be evidenced from what Observability & Health reports.

## 10. Failure Modes

- **Blind spots** — a capability fails to report state, leaving a gap in the health picture that looks like "everything is fine" rather than "unknown."
- **Alarm fatigue** — expected, policy-driven gaps are reported indistinguishably from genuine failures, causing real problems to be lost in noise.
- **False health** — a capability reports itself healthy despite degraded behavior because its self-reporting doesn't actually reflect what it's failing to do.
- **Observability as a bottleneck** — a poorly bounded implementation becomes something other capabilities functionally wait on, violating its own non-responsibility.

## 11. Future Evolution

Increasingly rich health reporting as the capability model grows — more source types, more consumer types, Federation. Historical health trends becoming a context object in their own right, such as a source's history of coverage gaps, bounded carefully so this remains reporting rather than Ferret reasoning about its sources. Health reporting scoped across federated workspaces once Federation matures, aggregating without losing per-workspace attribution.
