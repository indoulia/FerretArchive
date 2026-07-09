# FEP-004-SPEC-F10.2.2 — Expected-Gap vs. Failure Distinction

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F10.2.2 |
| **Capability** | [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) |
| **Epic** | E10.2 — Health Reporting & Distinction |
| **Feature** | F10.2.2 — Expected-Gap vs. Failure Distinction |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-10 — Observability & Health](../epics/FEP-003-EPIC-CAP-10-Observability-Health.md) · [FEP-002-CAP-10 — Observability & Health](../capabilities/FEP-002-CAP-10-Observability-Health.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A Health Report that cannot tell a genuine failure apart from an expected, policy-driven gap is a report no one can safely act on. Expected-Gap vs. Failure Distinction exists to classify each signal as one or the other, preventing alarm fatigue and false-health failure modes (FEP-003-EPIC-CAP-10 §3, F10.2.2 Objective and Product Outcome).

## 3. Scope

- Classifying each signal represented in a generated Health Report as either a genuine failure or an expected, policy-driven gap.
- Grounding an "expected" classification in an existing, explicit scope or policy declaration already made elsewhere — for example, an out-of-scope source category declared via F01.2.1 (Scope Boundary Declaration) — never in mere frequency or convention.
- Ensuring the classification is itself inspectable, so a consumer of a report can see why a given signal was classified as expected rather than as a failure.

## 4. Out of Scope

- Generating the Health Report whose signals are classified here — that is F10.2.1, a precondition of this Feature.
- Collecting or aggregating the underlying signals — that is F10.1.1 and F10.1.2 respectively.
- Declaring the scope or policy that makes a gap "expected" in the first place — that is owned by the originating capability (for example, Workspace Definition's F01.2.1 for scope); this Feature consumes that declaration to classify a signal, it does not create scope or policy itself.
- Routing classified signals to an external observability sink — that is F10.3.1.
- Taking corrective action on a signal classified as a genuine failure — always out of scope for this capability; classification informs an operator, it never remediates on their behalf.

## 5. Engineering Requirements

1. Every signal represented in a generated Health Report must be classifiable as either a genuine failure or an expected, policy-driven gap — no signal may remain unclassified once a report is generated.
2. A gap must be classifiable as "expected" only when it can be attributed to an existing, explicit scope or policy declaration — never merely because the gap is common, unsurprising, or previously unremarked.
3. A signal that cannot be attributed to an explicit scope or policy declaration must default to being classified as a potential genuine failure, never silently defaulted to "expected."
4. The basis for any "expected gap" classification must be traceable to the specific scope or policy declaration that justifies it.
5. A signal's classification must be revisited if the scope or policy declaration that justified it changes or is withdrawn.
6. A deliberately introduced expected gap must be classifiable as expected, and a deliberately introduced genuine failure must be classifiable as a failure, with the two remaining distinguishable from each other in every case.

## 6. Inputs

- The signals contained within a generated Health Report (F10.2.1).
- The scope or policy declarations capable of justifying a gap as expected — for example, F01.2.1 (Scope Boundary Declaration) — and equivalent policy-declaration outputs from other capabilities where applicable.

## 7. Outputs

- A classification, attached to each signal in a Health Report, of "genuine failure" or "expected, policy-driven gap," together with the declaration that justifies any "expected" classification.

## 8. Preconditions

- A Health Report must already exist (F10.2.1), containing the signals to be classified.
- The scope or policy declaration a classification might rely on (for example, F01.2.1) must already be resolvable.

## 9. Postconditions

- Every signal in a Health Report carries an explicit classification.
- An operator or consumer can distinguish, for any given signal, whether it represents a genuine failure requiring attention or an expected, already-sanctioned gap.

## 10. Dependencies

**Capability dependencies.** Workspace Definition (FEP-002-CAP-01), insofar as its scope declarations are one basis by which a gap can be justified as expected.

**Epic dependencies.** Internal to E10.2 (Health Reporting & Distinction) — follows F10.2.1 within the same Epic; also depends on E01.2 (Scope Declaration & Configuration) as a source of scope-driven expected-gap justification.

**Feature dependencies.** F10.2.1 (Health Report Generation), F01.2.1 (Scope Boundary Declaration) — per the E10.2 Features table, F10.2.2 depends directly on both.

**External dependencies.** None directly. Classification consumes declarations already resolved by other capabilities; it does not itself read from source systems or identity & access systems (FEP-001 §6).

## 11. Constraints

**Business constraints.** None beyond the general observability-accessibility constraint (FEP-002-CAP-10 §8, Business), which this Feature does not extend further.

**Product constraints.** Health reporting must never conflate an expected, policy-driven gap with a genuine failure, nor the reverse (FEP-002-CAP-10 §8, Product) — this is the constraint this Feature exists to satisfy directly.

**Context integrity constraints.** This Feature directly implements the capability's central context-integrity constraint: distinguishing "this capability is unhealthy" from "this capability is healthy but reporting an expected, policy-driven gap" (FEP-002-CAP-10 §8, Context integrity).

**Trust constraints.** Per Product Principle P5 (Degrade by scope, not by silent omission), a signal that cannot be classified with confidence must default toward visibility as a potential failure, never silently toward "expected."

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), classification consumes scope or policy declarations owned by other capabilities; it never itself declares or alters that scope or policy.

## 12. Acceptance Criteria

1. Every signal in a generated Health Report resolves to exactly one classification: genuine failure or expected, policy-driven gap.
2. A deliberately introduced expected gap, backed by an explicit scope or policy declaration, is classified as expected, not as a failure.
3. A deliberately introduced genuine failure, with no supporting scope or policy declaration, is classified as a failure, not as expected.
4. A signal with no attributable scope or policy declaration is never classified as expected by default.
5. Withdrawing or changing the scope or policy declaration that justified an "expected" classification results in the affected signal's classification being revisited.

## 13. Validation Requirements

- That every signal in a report receives exactly one classification, with none left ambiguous.
- That an "expected" classification is always traceable to a specific, existing declaration, never inferred from frequency or convention.
- That a genuine failure and an expected gap remain distinguishable from each other under conditions that deliberately introduce both.
- That classification responds correctly to a change in the underlying scope or policy declaration it depended on.

## 14. Failure Conditions

- **Alarm fatigue** (FEP-002-CAP-10 §10, Failure Modes) — expected, policy-driven gaps reported indistinguishably from genuine failures, burying real problems in noise. Expected behavior: every expected-gap classification must carry a visible, traceable justification distinguishing it from an unclassified or genuine-failure signal.
- **False health via misclassification** — a genuine failure incorrectly classified as an expected gap because of an over-broad or stale policy declaration. Expected behavior: classification must be re-evaluated whenever the justifying declaration changes, and a signal without a current, valid justification must not remain classified as expected.

## 15. Traceability

Product Vision (Mission: infrastructure that continuously acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G4 (Trustworthy context — accurate classification underlies trust in the report), G2 (Currency of context — reclassification on policy change keeps the distinction current) → Product Principles P5 (Degrade by scope, not by silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-10 (Observability & Health) → Epic E10.2 (Health Reporting & Distinction) → Feature F10.2.2 (Expected-Gap vs. Failure Distinction).

## 16. Future Considerations

- Ambiguity in what counts as "expected" without a clear, agreed source for policy-driven expectation is a known risk; a more explicit shared vocabulary for expectation may be needed as more capabilities mature (FEP-003-EPIC-CAP-10 §7, Risks).
- Historical health trend analysis, deferred and bounded to avoid becoming reasoning about sources, could eventually inform classification confidence without this Feature's classification remaining reporting rather than reasoning (FEP-003-EPIC-CAP-10 §8, Deferred Work; FEP-002-CAP-10 §11).
