# FEP-004-SPEC-F11.3.1 — Contribution Outcome Recording

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F11.3.1 |
| **Capability** | [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) |
| **Epic** | E11.3 — Partial-Success Transparency |
| **Feature** | F11.3.1 — Contribution Outcome Recording |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-11 — Federation](../epics/FEP-003-EPIC-CAP-11-Federation.md) · [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Honest disclosure of a partial cross-workspace result is impossible without first knowing, for each contributing workspace, exactly what happened. Contribution Outcome Recording exists to record — per workspace, per composition — whether that workspace succeeded, was denied, was stale, or was absent, providing the factual basis Partial Composition Disclosure (F11.3.2) depends on — directly satisfying this Feature's Completion Criteria that every contributing workspace's outcome for a given cross-workspace request is recorded and retrievable.

## 3. Scope

- Recording, for each workspace within a resolved Federation Scope, its Contribution Outcome for a given Cross-Workspace Composition: succeeded, denied, stale, or absent.
- Associating each recorded outcome with the specific cross-workspace request and composition it pertains to.
- Making recorded outcomes retrievable after the composition has completed.

## 4. Out of Scope

- Resolving the Federation Scope itself — that is F11.1.1, which determines which workspaces are candidates for a recorded outcome in the first place.
- Composing the contributions into a result — that is F11.2.1; this Feature records what happened during composition, it does not perform composition.
- Reconciling relevance or ranking across contributions — that is F11.2.2, unrelated to outcome recording.
- Disclosing recorded outcomes to the consumer — that is F11.3.2 (Partial Composition Disclosure), which consumes the record this Feature produces but is a distinct responsibility.
- Determining why a workspace was denied or stale at the source — that determination belongs to each individual workspace's own Access Control & Policy or Context Maintenance capability; this Feature records the outcome as reported, it does not diagnose it.
- Any acquisition, organization, maintenance, or access-control decision within a contributing workspace, per FEP-002-CAP-11 §3.

## 5. Engineering Requirements

1. For every workspace named in a request's resolved Federation Scope, a Contribution Outcome must be recorded for that composition, with no workspace in scope left unrecorded.
2. Each recorded outcome must classify the workspace's contribution as exactly one of: succeeded, denied, stale, or absent.
3. A recorded outcome must be associated unambiguously with the specific cross-workspace request and composition instance it describes.
4. A recorded outcome must be retrievable after the composition it describes has completed, independent of whether the composition succeeded fully or only partially.
5. Recording must not alter, suppress, or reinterpret the outcome a contributing workspace itself reported — a denial reported by a workspace must be recorded as a denial, not silently reclassified as absence.
6. The absence of an expected contribution, with no reported reason, must itself be recorded as a distinct outcome ("absent"), never conflated with "denied" or "stale."

## 6. Inputs

- The resolved Federation Scope for a cross-workspace request (F11.1.1).
- Each contributing workspace's reported result for the composition attempt: success, denial, staleness, or non-response.

## 7. Outputs

- A Contribution Outcome record, per workspace, per composition, classifying each workspace as succeeded, denied, stale, or absent.

## 8. Preconditions

- A Federation Scope has already been resolved for the request (F11.1.1).
- A Cross-Workspace Composition attempt (F11.2.1) has been made, or is in progress, such that per-workspace outcomes exist to record.

## 9. Postconditions

- Every workspace in the resolved Federation Scope has an associated, retrievable Contribution Outcome for the composition.
- No workspace's actual outcome (success, denial, staleness, absence) has been altered or lost in recording.
- The recorded outcomes are available to be consumed by Partial Composition Disclosure (F11.3.2) or any other authorized capability.

## 10. Dependencies

**Capability dependencies.** Depends on each participating workspace's own Access Control & Policy and Context Maintenance capabilities to produce the denial/staleness signals being recorded, per FEP-002-CAP-11 §7.

**Epic dependencies.** Depends on E11.2 (Cross-Workspace Composition), since there is nothing to record an outcome for before composition is attempted, per FEP-003-EPIC-CAP-11 §5.

**Feature dependencies.** F11.2.1 (Cross-Workspace Context Composition), per the E11.3 Features table (FEP-003-EPIC-CAP-11 §3).

**External dependencies.** Identity & access systems (FEP-001 §6), indirectly, insofar as a "denied" outcome originates from a contributing workspace's own access decision, which in turn may rely on such systems; this Feature does not itself consult them.

## 11. Constraints

**Business constraints.** Recorded outcomes must reflect what each contributing workspace actually reported, never a summarized or optimistic approximation that could mask a denial or staleness as success (extension of FEP-002-CAP-11 §8, Business, applied to honesty of record rather than access).

**Product constraints.** A recorded outcome must remain attributable to its specific contributing workspace and composition instance, consistent with the traceability guarantee in FEP-002-CAP-11 §8, Product.

**Context integrity constraints.** Recording must surface partial success honestly, consistent with Product Principle P5, rather than allowing an incomplete composition to be recorded as though every workspace succeeded (FEP-002-CAP-11 §8, Context integrity).

**Trust constraints.** Per Product Principle P2 (Provenance is mandatory, not optional), a Contribution Outcome is itself a form of provenance about the composition process and must be preserved with the same rigor as content provenance.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), this Feature must not absorb the responsibility of disclosing outcomes to the consumer — that remains F11.3.2's responsibility.

## 12. Acceptance Criteria

1. For a composition involving N workspaces in the resolved Federation Scope, exactly N Contribution Outcome records exist, one per workspace.
2. Each recorded outcome is classified as exactly one of succeeded, denied, stale, or absent — never left unclassified or classified as more than one.
3. A workspace that reports a denial has its outcome recorded as "denied," never as "absent" or "succeeded."
4. A workspace that fails to respond within the composition attempt, with no explicit denial or staleness signal, is recorded as "absent."
5. Recorded outcomes for a given composition remain retrievable after the composition attempt has completed, regardless of overall composition success or partiality.

## 13. Validation Requirements

- That every workspace in a resolved Federation Scope has exactly one recorded outcome per composition attempt.
- That recorded outcomes match what each contributing workspace actually reported, with no reclassification or loss of information.
- That "absent" is never used as a substitute classification for an actual reported denial or staleness signal, and vice versa.
- That recorded outcomes remain retrievable after the composition process has finished.

## 14. Failure Conditions

- **Silent partial composition** (FEP-002-CAP-11 §10) — a cross-workspace result quietly omits a workspace that failed, denied, or was stale, without a corresponding recorded outcome. Expected behavior: this must never occur; every workspace in scope must have a recorded outcome, and its absence from the result must be traceable to that outcome, not silently unexplained (Product Principle P5).
- **Outcome misclassification** — a workspace's actual result (e.g., denial) is recorded under the wrong category (e.g., absence), obscuring the true reason for exclusion. Expected behavior: misclassification must be detectable and correctable; the record must never be treated as authoritative if it conflicts with what the workspace actually reported.

## 15. Traceability

Product Vision (Mission: infrastructure that delivers trustworthy engineering context) → Goals G4 (Trustworthy context) → Product Principles P2 (Provenance is mandatory), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-11 (Federation) → Epic E11.3 (Partial-Success Transparency) → Feature F11.3.1 (Contribution Outcome Recording).

## 16. Future Considerations

- As Federation matures beyond Generation 3, richer outcome categories or diagnostic detail may become relevant, per FEP-002-CAP-11 §11, but are deferred until real multi-workspace failure patterns are observed.
- This Feature's detailed design remains provisional, per FEP-003-EPIC-CAP-11 §7, pending real multi-workspace use cases that would clarify what outcome granularity is actually needed.
