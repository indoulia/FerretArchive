# FEP-004-SPEC-F11.3.2 — Partial Composition Disclosure

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F11.3.2 |
| **Capability** | [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) |
| **Epic** | E11.3 — Partial-Success Transparency |
| **Feature** | F11.3.2 — Partial Composition Disclosure |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-11 — Federation](../epics/FEP-003-EPIC-CAP-11-Federation.md) · [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Recording Contribution Outcomes (F11.3.1) only matters if a consumer actually learns from them. Partial Composition Disclosure exists to surface recorded Contribution Outcomes to the consumer whenever a cross-workspace result is not fully complete, consistent with Product Principle P5, so that a partial result is never mistaken for a complete one — directly satisfying this Feature's Completion Criteria that a cross-workspace result with one or more non-succeeding contributing workspaces is never presented as complete.

## 3. Scope

- Determining, for a given composed cross-workspace result, whether every workspace in the resolved Federation Scope succeeded, based on the recorded Contribution Outcomes (F11.3.1).
- Attaching an honest, explicit indication of partiality to the composed result whenever at least one contributing workspace did not succeed.
- Making the specific non-succeeding workspace(s) and their recorded outcome (denied, stale, or absent) identifiable to the consumer alongside the composed result.

## 4. Out of Scope

- Recording the underlying Contribution Outcomes themselves — that is F11.3.1, a precondition this Feature consumes but does not perform.
- Composing the cross-workspace result — that is F11.2.1; this Feature discloses partiality about a result, it does not build the result.
- Reconciling relevance or ranking within the result — that is F11.2.2, unrelated to disclosure of partiality.
- Diagnosing or resolving why a workspace was denied, stale, or absent — that remains the responsibility of the individual workspace whose outcome was recorded; disclosure communicates the recorded fact, it does not investigate its cause.
- Any decision about whether a consumer should accept, retry, or act on a partial result — Ferret discloses; deciding what to do with a partial result belongs to the consumer, consistent with FEP-001's Non-Goal of reasoning over context to produce conclusions or actions.

## 5. Engineering Requirements

1. A composed cross-workspace result must be accompanied by a disclosure of completeness status: fully complete, or partial, based strictly on the recorded Contribution Outcomes for that composition.
2. A cross-workspace result must never be presented, implicitly or explicitly, as fully complete when any contributing workspace's recorded outcome is denied, stale, or absent.
3. When a result is partial, the disclosure must identify which contributing workspace(s) did not succeed and each one's recorded outcome category.
4. Disclosure of partiality must be present in every case where partiality exists — it must not depend on the consumer explicitly requesting completeness status.
5. Disclosure must not alter or omit the underlying composed content itself; it is an addition to the result, not a filter over it.
6. Disclosure must be based only on the recorded Contribution Outcomes for the specific composition being disclosed — it must not infer partiality from unrelated or historical outcomes.

## 6. Inputs

- A composed cross-workspace result (F11.2.1).
- The recorded Contribution Outcomes for that composition (F11.3.1).

## 7. Outputs

- A completeness disclosure accompanying the composed result: an explicit statement of whether the result is fully complete or partial, and, when partial, which workspace(s) did not succeed and why (per their recorded outcome category).

## 8. Preconditions

- Contribution Outcomes have already been recorded for the composition (F11.3.1).
- A Cross-Workspace Composition has already been produced for the request (F11.2.1).

## 9. Postconditions

- A consumer receiving a cross-workspace result always knows, without needing to ask separately, whether the result is complete or partial.
- When partial, the consumer can identify which workspace(s) are missing from the result and the recorded reason (denied, stale, or absent).
- A partial result is never indistinguishable, from the consumer's point of view, from a fully complete one.

## 10. Dependencies

**Capability dependencies.** Depends on Federation's own Contribution Outcome recording; indirectly depends on each participating workspace's own capabilities that produce the underlying denial/staleness signals, per FEP-002-CAP-11 §7.

**Epic dependencies.** Depends on E11.2 (Cross-Workspace Composition), since composition must exist before there is anything to disclose as partial, per FEP-003-EPIC-CAP-11 §5.

**Feature dependencies.** F11.3.1 (Contribution Outcome Recording), per the E11.3 Features table (FEP-003-EPIC-CAP-11 §3).

**External dependencies.** None directly; disclosure operates entirely on already-recorded outcomes and the already-composed result.

## 11. Constraints

**Business constraints.** Disclosure must never be suppressible or optional in a way that would let a partial result be delivered as though it were complete — this is the direct product-level enforcement of the Non-Responsibility that Federation must represent honestly when a cross-workspace request cannot be fully satisfied (FEP-002-CAP-11 §2).

**Product constraints.** Disclosure must remain attributable to specific contributing workspaces — a generic "result may be incomplete" notice without naming which workspace(s) failed would not satisfy the attributability guarantee in FEP-002-CAP-11 §8, Product.

**Context integrity constraints.** Federation must surface partial success honestly, consistent with Product Principle P5, rather than presenting a partial composition as a complete one (FEP-002-CAP-11 §8, Context integrity — the constraint this Feature exists specifically to satisfy).

**Trust constraints.** Per Product Principle P4 (No privileged consumer), disclosure must be delivered identically regardless of which consumer (human, AI system, or tool) requested the cross-workspace result.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), this Feature must not absorb the responsibility of determining why a workspace failed to contribute — it discloses the recorded outcome, it does not diagnose or resolve it.

## 12. Acceptance Criteria

1. Every composed cross-workspace result in which at least one contributing workspace's recorded outcome is denied, stale, or absent is accompanied by an explicit partial-completeness disclosure.
2. A cross-workspace result in which every contributing workspace's recorded outcome is "succeeded" is disclosed as fully complete, with no false partiality indication.
3. A partial-completeness disclosure names each non-succeeding contributing workspace and its recorded outcome category.
4. No cross-workspace result is delivered to a consumer without an accompanying completeness disclosure, whether complete or partial.
5. The composed content of the result is identical whether or not a partial-completeness disclosure is attached — disclosure adds information, it does not remove or filter content.

## 13. Validation Requirements

- That every partial composition is disclosed as partial, with no case of a partial result being presented as complete.
- That every fully successful composition is disclosed as complete, with no case of unwarranted partiality being reported.
- That the disclosed non-succeeding workspace(s) and their outcome categories match the underlying recorded Contribution Outcomes exactly.
- That disclosure is present for every cross-workspace result, not only when a consumer explicitly requests completeness information.

## 14. Failure Conditions

- **Silent partial composition presented as complete** (FEP-002-CAP-11 §10) — a result quietly omits a workspace that failed, denied, or was stale, and disclosure fails to flag it. Expected behavior: this is the failure mode this Feature exists to prevent; any such occurrence must itself be treated as a defect, since the requirement is precisely that this never happens (Product Principle P5).
- **Disclosure present but uninformative** — a result is flagged as partial but the specific non-succeeding workspace(s) or their outcome category cannot be identified from the disclosure. Expected behavior: disclosure must be specific enough to identify the affected workspace(s) and outcome category; a generic partiality flag alone does not satisfy this Feature's requirements.

## 15. Traceability

Product Vision (Mission: infrastructure that delivers trustworthy engineering context to any consumer) → Goals G4 (Trustworthy context) → Product Principles P4 (No privileged consumer), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-11 (Federation) → Epic E11.3 (Partial-Success Transparency) → Feature F11.3.2 (Partial Composition Disclosure).

## 16. Future Considerations

- As Federation matures beyond Generation 3, richer disclosure detail (e.g., estimated staleness duration, retry guidance surfaced by the consumer's own tooling) may become relevant, per FEP-002-CAP-11 §11, but is deferred until real multi-workspace use cases exist.
- This Feature's detailed design remains provisional, per FEP-003-EPIC-CAP-11 §7, until real partial-composition scenarios across genuinely federated workspaces are observed.
