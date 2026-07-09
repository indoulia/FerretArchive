# FEP-003-EPIC-CAP-08 — Engineering Program: Access Control & Policy

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-08 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Access Control & Policy governs which consumers may access which context, consistent with declared workspace and source policy. It consumes identity from external systems rather than issuing it, and gates access without owning the content it protects.

## 2. Engineering Epics

### E08.1 — Policy Definition & Scope

- **Purpose.** Allow policies to be declared at the right granularity.
- **Scope.** Declaring policy at workspace, source, or context-unit level; representing policy scope explicitly.
- **Success Definition.** A policy's applicability is unambiguous at whatever granularity it was declared.

### E08.2 — Permission Evaluation

- **Purpose.** Evaluate whether a consumer may access given context, consistently and repeatably.
- **Scope.** Evaluating identity assertions against policy; producing permitted/denied/partially-permitted outcomes.
- **Success Definition.** The same consumer, context, and policy state always yields the same decision.

### E08.3 — Decision Auditability

- **Purpose.** Make every permission decision auditable after the fact.
- **Scope.** Recording decisions with enough context to reconstruct who was permitted or denied what, and why.
- **Success Definition.** Any past permission decision can be reconstructed and explained.

## 3. Features

### E08.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F08.1.1 — Policy Declaration | Allow a policy governing access to be declared, referencing Workspace Definition's configuration. | Establishes the rules Permission Evaluation will apply. | F01.2.2 | A declared policy is resolvable and unambiguous in what it governs. |
| F08.1.2 — Policy Scope Granularity | Support policy declared at workspace, source, or context-unit granularity. | Enables fine-grained governance without forcing every policy to be workspace-wide. | F08.1.1 | A finer-granularity policy correctly takes precedence or composes with a coarser one, per a defined, documented precedence rule. |

### E08.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F08.2.1 — Permission Evaluation Engine | Evaluate a consumer's asserted identity against applicable policy for a given context request. | Provides the permission decision Assembly and Delivery depend on. | F08.1.2, external identity assertions (FEP-001 §6) | Identical inputs always produce the identical decision. |
| F08.2.2 — Partial Permission Outcomes | Support a "partially permitted" outcome distinct from binary allow/deny, where policy calls for it. | Enables nuanced governance, e.g., "may know this exists, not its content." | F08.2.1 | A policy declared to require partial permission produces a distinguishable partial outcome. |

### E08.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F08.3.1 — Decision Recording & Audit Surfacing | Record every permission decision and make the record queryable. | Supports compliance and trust requirements depending on after-the-fact auditability. | F08.2.1 | Any permission decision can be retrieved after the fact along with the identity, context, and policy state that produced it. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F01.2.2 (Workspace Configuration Management).
- **Prerequisite Epics.** E01.2 (Scope Declaration & Configuration).
- **Prerequisite Capabilities.** Workspace Definition. This capability also conceptually depends on an external identity system per FEP-001 §6, which is outside Ferret's own engineering program.

## 5. Execution Order

1. **E08.1** — policy must exist before it can be evaluated.
2. **E08.2** — depends on policy existing.
3. **E08.3** — depends on evaluation existing, since there is nothing to audit before decisions are made.

## 6. Capability Completion Gates

- **Functional completeness.** Every context request that reaches Assembly or Delivery is subject to an evaluated permission decision, with no path that bypasses evaluation.
- **Validation readiness.** Repeated identical requests are verified to produce identical decisions; a policy requiring partial permission is verified to produce a distinguishable partial outcome.
- **Documentation readiness.** The precedence rule between workspace-, source-, and context-unit-level policy is documented unambiguously.
- **Review completion.** FEP-002-CAP-08's non-responsibilities (no identity issuance, no relevance decisions, no content storage) confirmed unviolated.

## 7. Risks

- **Identity-system dependency outside the engineering program's control.** Because identity assertion is explicitly external, scoping Permission Evaluation without that interface contract being stable risks rework once it is defined.
- **Precedence-rule disputes.** Without an agreed precedence rule for overlapping policy scopes, Policy Scope Granularity's completion criteria cannot be objectively verified, and Assembly's and Delivery's dependence on a single, unambiguous decision is put at risk.
- **Audit granularity underspecification.** "Enough context to reconstruct a decision" is not bounded by FEP-002; without agreeing what matters for reconstruction, Decision Recording risks scope disagreement late in planning.

## 8. Deferred Work

- Cross-workspace policy reconciliation — deferred to Federation.
- Richer, more nuanced permission outcomes beyond binary and simple partial — deferred pending real enterprise governance requirements.
