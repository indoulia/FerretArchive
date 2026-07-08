# FEP-002-CAP-08 — Access Control & Policy

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-08 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.8 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

"Context is the product" cannot mean "context is available to anyone who asks." Access Control & Policy exists to ensure context is only delivered to consumers permitted to see it, consistent with the policies a workspace and its sources declare.

## 2. Responsibilities

- Evaluate whether a given consumer, with a given asserted identity, is permitted to receive a given piece of context.
- Apply policies declared at the workspace level, and, where relevant, at the source or context-unit level.
- Distinguish between permission outcomes — permitted, denied, partially permitted — so Assembly and Delivery can act on the distinction rather than a binary pass or fail.
- Make policy decisions consistent and repeatable for the same consumer, context, and policy state.
- Surface policy state for audit: what was permitted, to whom, and under what policy.

## 3. Non-Responsibilities

- Must never establish or issue identity itself — it consumes identity assertions from external systems (FEP-001 §6) and does not become an identity provider.
- Must never decide what context exists or is relevant — it only gates access to what Assembly has already determined is relevant.
- Must never store or become the system of record for the content it protects — it protects content owned elsewhere in the model.

## 4. Inputs

- Policy declarations from Workspace Definition, and, conceptually, from source-level or context-level policy where finer granularity is declared.
- Identity assertions from external identity systems.
- The specific context and requester combination a permission decision is needed for.

## 5. Outputs

- Permission decisions — permitted, denied, or partially permitted — consumed by Context Assembly and Context Delivery.
- Auditable policy-decision records.

## 6. Context Objects

- **Policy** — a conceptual rule governing which asserted identities may access which context.
- **Permission Decision** — the conceptual outcome of evaluating a policy against a specific request and identity.
- **Policy Scope** — the conceptual level at which a policy applies: workspace-wide, source-specific, or context-unit-specific.

## 7. Relationships

Consumes policy declarations from Workspace Definition and identity assertions from external identity systems (FEP-001 §6). Gates Context Assembly's selection and Context Delivery's hand-off. Reports decisions to Provenance & Attribution and to Observability & Health.

## 8. Constraints

- **Business.** Policy evaluation must be consistent — the same consumer, context, and policy state must yield the same decision every time, or the product cannot be trusted for compliance-sensitive use.
- **Product.** Denial must be an explicit, recorded outcome, never an implicit side effect of context simply not being assembled.
- **Context integrity.** Partial permission — for example, permitted to know something exists but not its full content — must be a distinguishable outcome where a workspace's policy calls for that distinction, not collapsed into a binary allow or deny.

## 9. Success Criteria

- No context reaches a consumer without an explicit, evaluated permission decision behind it.
- Policy decisions are auditable after the fact: who was permitted or denied what, and under which policy.
- Policy evaluation remains consistent under repeated, identical requests.

## 10. Failure Modes

- **Silent over-permission** — a policy gap defaults to allow rather than deny, leaking context to consumers who should not have received it.
- **Silent under-permission** — an overly conservative default denies legitimate consumers without an explainable reason, undermining trust in the system's usefulness.
- **Policy/identity mismatch** — a stale or mismatched identity assertion causes decisions to be made against the wrong identity.
- **Unauditable decisions** — permission decisions are made but not recorded, making it impossible to answer "who could see this, and why" after the fact.

## 11. Future Evolution

Finer-grained policy scopes as source and consumer diversity grow. Cross-workspace policy reconciliation as Federation matures — deciding how a permission granted in one workspace interacts with a related workspace's policy. Increasingly rich permission outcomes, such as "permitted to know this exists, not its content," as enterprise use cases demand more nuanced governance.
