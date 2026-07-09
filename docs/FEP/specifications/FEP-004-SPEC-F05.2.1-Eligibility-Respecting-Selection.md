# FEP-004-SPEC-F05.2.1 — Eligibility-Respecting Selection

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F05.2.1 |
| **Capability** | [Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md) |
| **Epic** | E05.2 — Selection & Ranking |
| **Feature** | F05.2.1 — Eligibility-Respecting Selection |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md), [FEP-003-EPIC-CAP-05 — Context Assembly](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md), [FEP-002-CAP-05 — Context Assembly](../capabilities/FEP-002-CAP-05-Context-Assembly.md), [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

## 2. Purpose

Assembly must never surface context that is stale or that the requester is not permitted to see. This specification exists to define how Ferret selects structured context that is both relevant to the interpreted request and eligible — current per Context Maintenance and permitted per Access Control & Policy — realizing the Feature's Product Outcome that Assembly never surfaces stale or unpermitted context.

## 3. Scope

- Selecting candidate structured context from Context Organization's output that is relevant to the interpreted intent (F05.1.1).
- Filtering that candidate set by freshness/eligibility state supplied by Context Maintenance.
- Filtering that candidate set by permission state supplied by Access Control & Policy.
- Producing a selected set containing only context that is simultaneously relevant, current, and permitted.

## 4. Out of Scope

- Interpreting the request's intent — that is F05.1.1, a prerequisite input to this feature.
- Ranking the selected set by relevance — that is F05.2.2 (Relevance Ranking), which operates after this feature.
- Composing the final result or recording exclusions as Assembly Gaps — that is E05.3.
- Determining freshness state itself — that is Context Maintenance's Freshness State Tracking (F04.2.1).
- Determining permission outcomes themselves — that is Access Control & Policy's Permission Evaluation Engine (F08.2.1).
- Acquiring or structuring any new context — Assembly consumes what Organization and Maintenance have already produced (per the capability's Non-Responsibilities).

## 5. Engineering Requirements

1. Selection must only include structured context relevant to the interpreted intent produced by F05.1.1.
2. Selection must exclude any context that Context Maintenance has marked stale or of unknown freshness, unless the workspace's declared freshness expectation permits its inclusion.
3. Selection must exclude any context that Access Control & Policy has not permitted for the requesting consumer.
4. Selection must never include context excluded by freshness or permission checks, even partially.
5. Selection must be able to proceed to a defined, reportable outcome when Maintenance's or Access Control's state for a candidate item is itself unavailable or unresolved, rather than assuming eligibility by default.
6. The selection outcome must be reproducible: an identical request, over unchanged organized context, freshness state, and permission state, must produce an identical selected set.

## 6. Inputs

- The interpreted intent from F05.1.1.
- Structured context from Context Organization.
- Freshness and eligibility state from Context Maintenance (per F04.2.1, Freshness State Tracking).
- Permission state from Access Control & Policy (per F08.2.1, Permission Evaluation Engine).

## 7. Outputs

- A selected set of structured context that is relevant, current, and permitted.
- A record, for context considered but excluded, of which eligibility check (freshness or permission) excluded it, for use by Assembly Gap Reporting (F05.3.2).

## 8. Preconditions

- F05.1.1 has produced an interpreted intent for the request.
- Context Maintenance's freshness state tracking (F04.2.1) exists, at least in minimal form, per Epic E05.2's execution-order dependency.
- Access Control & Policy's permission evaluation (F08.2.1) exists, at least in minimal form, per Epic E05.2's execution-order dependency.

## 9. Postconditions

- The selected set contains no context excluded by freshness or permission.
- Every excluded candidate's exclusion reason (staleness or permission) is retained for downstream gap attribution.

## 10. Dependencies

**Capability dependencies.** Context Organization (source of structured context); Context Maintenance (source of freshness/eligibility state); Access Control & Policy (source of permission state).

**Epic dependencies.** E05.1 (Request Interpretation, prerequisite epic); E04.2 (Freshness Accounting); E08.2 (Permission Evaluation) — per Global Output 3's cross-capability epic dependency and the epic file's Prerequisite Epics.

**Feature dependencies.** F05.1.1 (Request Intent Interpretation); F04.2.1 (Freshness State Tracking); F08.2.1 (Permission Evaluation Engine) — per the epic file's Dependencies column.

**External dependencies.** Identity & access systems (FEP-001 §6) insofar as they underlie the permission assertions Access Control & Policy evaluates — consumed indirectly through F08.2.1, not directly by this feature.

## 11. Constraints

**Business constraints.** Selection logic must be identical for equivalent requests regardless of the requesting consumer, per Product Principle P4 and the capability's Business constraint on consumer neutrality.

**Product constraints.** Selection must not silently drop relevant context to fit a constraint — any exclusion must be attributable to a specific, recorded reason, per the capability's Product constraint.

**Context integrity constraints.** Selection must never treat context Maintenance has marked stale or unknown as though current — a direct instantiation of the capability's Failure Mode "Stale leakage" to be prevented, per Product Principle P3.

**Trust constraints.** Selection must never draw on context the requester was not permitted to see — a direct instantiation of the capability's Failure Mode "Access bypass" to be prevented.

**Policy constraints.** Selection must treat Access Control & Policy's permission decision as authoritative and complete before including any candidate context; it must not substitute its own judgment for a missing permission decision.

## 12. Acceptance Criteria

1. Given a candidate context unit marked stale by Context Maintenance, it does not appear in the selected set.
2. Given a candidate context unit not permitted for the requester by Access Control & Policy, it does not appear in the selected set.
3. Given a candidate context unit that is both current and permitted and relevant to the interpreted intent, it appears in the selected set.
4. Given an identical request under unchanged organized context, freshness state, and permission state, the selected set is identical across repeated invocations.
5. Given a candidate whose freshness or permission state is unresolved, it is excluded from the selected set and recorded as excluded for an unresolved reason, not silently included.

## 13. Validation Requirements

- Validate that no selected item is ever stale, of unknown freshness (absent a workspace exception), or unpermitted.
- Validate that relevant, current, permitted context is not incorrectly excluded.
- Validate reproducibility of selection outcomes under unchanged upstream state.
- Validate that exclusion reasons are correctly attributed to freshness vs. permission causes.

## 14. Failure Conditions

- **Stale leakage** — context Maintenance has marked stale or unknown is selected as though current: must never occur; if detected, selection must be corrected and the incident made observable, per Product Principle P3 and P5.
- **Access bypass** — context the requester was not permitted to see is selected because the interaction with Access Control & Policy was incomplete: must never occur; selection must fail closed (exclude) rather than fail open when permission state is unavailable, per Product Principle P5.
- **Silent truncation** — relevant, eligible context is dropped from selection without a recorded reason: must instead be recorded as an attributable exclusion for gap reporting.

## 15. Traceability

Product Vision (Mission: deliver trustworthy, current context) → Goals G2 (Currency), G4 (Trustworthy context) → Product Principles P3, P4, P5 → Capability FEP-002-CAP-05 (Context Assembly) → Epic E05.2 (Selection & Ranking) → Feature F05.2.1 (Eligibility-Respecting Selection).

## 16. Future Considerations

- Selection spanning multiple workspaces as Federation matures, requiring eligibility judgments that cross workspace boundaries (per capability file §11).
- Resolving the risk of "premature coupling to Access Control's maturity" as that capability's permission evaluation itself matures (per epic file §7, Risks).
