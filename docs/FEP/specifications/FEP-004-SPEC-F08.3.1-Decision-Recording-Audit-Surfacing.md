# FEP-004-SPEC-F08.3.1 — Decision Recording & Audit Surfacing

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F08.3.1 |
| **Capability** | [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) |
| **Epic** | E08.3 — Decision Auditability |
| **Feature** | F08.3.1 — Decision Recording & Audit Surfacing |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-08 — Access Control & Policy](../epics/FEP-003-EPIC-CAP-08-Access-Control-Policy.md) · [FEP-002-CAP-08 — Access Control & Policy](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A permission decision that leaves no trace cannot support compliance or trust once the moment it was made has passed. Decision Recording & Audit Surfacing exists to satisfy F08.3.1's objective — recording every permission decision and making the record queryable — delivering the product outcome the epic and capability both name directly: supporting compliance and trust requirements that depend on after-the-fact auditability.

## 3. Scope

- Recording every permission decision produced by Permission Evaluation (F08.2.1, F08.2.2), including permitted, denied, and partially permitted outcomes.
- Capturing, for each recorded decision, the identity involved, the context/request evaluated, and the policy state applied.
- Making recorded decisions retrievable and queryable after the fact, independent of later changes to policy or identity.
- Preserving the historical accuracy of a recorded decision against later, unrelated policy or identity changes.

## 4. Out of Scope

- Making the permission decision itself — that is F08.2.1 (Permission Evaluation Engine) and F08.2.2 (Partial Permission Outcomes); this Feature only records decisions already produced elsewhere.
- Declaring policy, or resolving which policy applies — that is F08.1.1 (Policy Declaration) and F08.1.2 (Policy Scope Granularity).
- General observability of other capabilities' internal state — that is Observability & Health, a distinct capability; this Feature's records are specific to permission decisions, not general system state, though it reports decisions onward in the same spirit (FEP-002-CAP-08 §7, Relationships).
- Establishing or issuing the identity captured in a decision record — per FEP-001 §5.2/§6, identity is consumed from external systems and is never issued by Ferret; this Feature records what identity was asserted, it does not authenticate it.
- Enforcing policy or altering a decision's outcome — this Feature only records outcomes already produced; it has no authority to change what was decided.

## 5. Engineering Requirements

1. Every permission decision produced by Permission Evaluation — permitted, denied, or partially permitted — must be recorded.
2. Each record must capture, at minimum, the identity involved, the context/request evaluated, the policy state applied, and the resulting decision.
3. A recorded decision must remain retrievable after the fact, independent of whether the underlying policy or identity has since changed.
4. Records must be queryable such that "who was permitted or denied what, and under what policy" can be answered for any past decision.
5. Recording must occur for every decision without exception — no decision may be evaluated without a corresponding record.
6. A recorded decision must not become alterable in a way that changes the historical account of what was decided — a later change to policy or identity must not retroactively rewrite what was recorded for a past decision.

## 6. Inputs

- A completed permission decision from Permission Evaluation (F08.2.1, F08.2.2), including the identity, context/request, and applicable policy state that produced it.

## 7. Outputs

- A retrievable, queryable record of the decision, sufficient to answer who was permitted, denied, or partially permitted what, and under what policy, after the fact.

## 8. Preconditions

- F08.2.1 (Permission Evaluation Engine) must exist and be producing decisions — there is nothing to record before decisions are made (FEP-003-EPIC-CAP-08 §5, Execution Order).

## 9. Postconditions

- Any past permission decision can be reconstructed and explained.
- Compliance and trust questions about historical access can be answered without relying on memory or inference.
- Policy decisions remain auditable after the fact: who was permitted or denied what, and under which policy (FEP-002-CAP-08 §9, Success Criteria).

## 10. Dependencies

**Capability dependencies.** Provenance & Attribution — this Feature reports decisions onward to it, per FEP-002-CAP-08 §7 (Relationships), though Decision Recording owns the permission-decision record itself rather than delegating it.

**Epic dependencies.** E08.2 (Permission Evaluation) — must precede this epic, since there is nothing to audit before decisions are made (FEP-003-EPIC-CAP-08 §5, Execution Order).

**Feature dependencies.** F08.2.1 (Permission Evaluation Engine) — the explicit prerequisite Feature per the epic file's E08.3 Features table.

**External dependencies.** None directly — recording depends on the internal decision output of F08.2.1/F08.2.2, not on any external system.

## 11. Constraints

**Business constraints.** Decisions must be recorded with enough context to reconstruct who was permitted or denied what, and why (FEP-002-CAP-08 §9, Success Criteria, rendered as a recording obligation).

**Product constraints.** Denial must be an explicit, recorded outcome, never an implicit side effect of context simply not being assembled (FEP-002-CAP-08 §8, Product) — this Feature is the mechanism by which that explicitness becomes durable and retrievable.

**Context integrity constraints.** A partially permitted outcome (F08.2.2) must be recorded distinguishably, never collapsed to a binary outcome within the record.

**Trust constraints.** Per Product Principle P2 (Provenance is mandatory, not optional), the decision record is itself a form of provenance over the act of gating context, and must be preserved with the same rigor as provenance over context content.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), this Feature must not absorb Permission Evaluation's decision-making role — it records what was already decided, it does not decide.

## 12. Acceptance Criteria

1. Every decision produced by Permission Evaluation has a corresponding record retrievable after the fact.
2. A retrieved record identifies the identity, the context/request, the policy state, and the decision outcome — including partial outcomes where applicable — for that decision.
3. A change to policy or identity after a decision was recorded does not alter the retrievable record of that past decision.
4. No decision exists without an associated record — every evaluated decision is discoverable via query.

## 13. Validation Requirements

- That no evaluated decision is missing a corresponding record.
- That records remain accurate and unaltered by later, unrelated policy or identity changes.
- That a query for a past decision returns enough detail to answer who was permitted, denied, or partially permitted what, and under which policy.

## 14. Failure Conditions

- **Unauditable decisions.** A permission decision is made but not recorded, making it impossible to answer "who could see this, and why" after the fact (FEP-002-CAP-08 §10, Failure Modes). Expected behavior: this must be treated as a detectable defect — a decision without a record must be visible as a gap, never silently absent (Product Principle P5).
- **Record/decision drift.** A record no longer accurately reflects the decision it was created for, for example because a later policy or identity change bleeds into the historical record. Expected behavior: this must be detectable and surfaced, never silently presented as the original decision.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goal G4 (Trustworthy context — a consumer can only trust the access-gating behind delivered context if that gating is itself auditable) → Product Principles P2 (Provenance is mandatory, not optional), P5 (Degrade by scope, not silent omission) → Capability FEP-002-CAP-08 (Access Control & Policy) → Epic E08.3 (Decision Auditability) → Feature F08.3.1 (Decision Recording & Audit Surfacing).

## 16. Future Considerations

- Resolving what "enough context to reconstruct a decision" bounds in practice, flagged as a scope-disagreement risk pending further definition (FEP-003-EPIC-CAP-08 §7, Risks — Audit granularity underspecification).
- Cross-workspace audit reconciliation as Federation matures, affecting what a decision record must capture when a request spans more than one workspace (FEP-002-CAP-08 §11).
- Correspondingly richer record detail as permission outcomes themselves grow richer beyond binary and simple partial (FEP-003-EPIC-CAP-08 §8; FEP-002-CAP-08 §11).
