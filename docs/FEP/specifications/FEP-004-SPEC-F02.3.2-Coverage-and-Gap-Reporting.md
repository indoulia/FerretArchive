# FEP-004-SPEC-F02.3.2 — Coverage & Gap Reporting

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F02.3.2 |
| **Capability** | [Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md) |
| **Epic** | E02.3 — Acquisition Event Recording & Reporting |
| **Feature** | F02.3.2 — Coverage & Gap Reporting |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-02 — Context Acquisition](../epics/FEP-003-EPIC-CAP-02-Context-Acquisition.md)<br>[FEP-002-CAP-02 — Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A partial picture presented as complete is worse than an acknowledged gap. This specification exists so that what was and was not successfully acquired — and why — is reported for every acquisition cycle, so that Observability & Health can distinguish full from partial coverage, satisfying Product Principle P5, per the Feature's objective and product outcome.

## 3. Scope

- Producing a coverage report for every acquisition cycle.
- Distinguishing, for the workspace's declared scope, what was successfully acquired from what was not.
- Attributing every gap in the report to a specific, identifiable reason.
- Distinguishing "not yet acquired," "acquired but incomplete," and "declared out of scope" as separate, consistently applied categories.
- Making the coverage report available to Observability & Health.

## 4. Out of Scope

- Recording individual acquisition attempts — owned by Acquisition Event Recording (F02.3.1), whose output this Feature aggregates.
- Determining reachability — owned by Source Reachability Tracking (F02.1.2).
- Performing reads or isolating read failures — owned by Faithful Content Reading (F02.2.1) and Partial-Failure Resilience (F02.2.2).
- Remediating any reported gap — Observability & Health reports and does not remediate (FEP-001 §2.10), and Acquisition itself never takes corrective action on its own initiative (capability §3 non-responsibilities).

## 5. Engineering Requirements

1. A coverage report must be produced for every acquisition cycle.
2. The coverage report must distinguish, across the workspace's declared scope, what was successfully acquired from what was not.
3. Every gap identified in the coverage report must be attributed to a specific, identifiable reason (for example: unreachable, out of scope, or failed during reading).
4. The coverage report must be made available to Observability & Health.
5. The coverage report must consistently represent the distinction between "not yet acquired," "acquired but incomplete," and "declared out of scope."

## 6. Inputs

- The discovered Source inventory, from Source Discovery within Scope (F02.1.1).
- Reachability state, from Source Reachability Tracking (F02.1.2).
- Reading outcomes, from Faithful Content Reading (F02.2.1) and Partial-Failure Resilience (F02.2.2).
- Acquisition Events, from Acquisition Event Recording (F02.3.1).

## 7. Outputs

- A coverage and gap report, produced per acquisition cycle, for Observability & Health.

## 8. Preconditions

- An acquisition cycle — comprising discovery, reachability assessment, and reading attempts — has completed or reached a defined checkpoint.

## 9. Postconditions

- Observability & Health can determine, for the cycle, the full extent of coverage achieved and the attributed reason for every shortfall.

## 10. Dependencies

**Capability dependencies.** Observability & Health — the consumer of this Feature's output.

**Epic dependencies.** E02.1 — Source Discovery; E02.2 — Content Reading & Preservation (both supply the underlying state this Feature reports on).

**Feature dependencies.** F02.1.2 — Source Reachability Tracking, F02.2.2 — Partial-Failure Resilience (explicit dependencies per epic file §3); F02.3.1 — Acquisition Event Recording (supplies the event data this report aggregates).

**External dependencies.** None beyond those already established for discovery and reading; this Feature introduces no new external system.

## 11. Constraints

**Business constraints.** None beyond those already governing acquisition scope.

**Product constraints.** Gap attribution must be specific enough to be informative — a generic "failed" category does not satisfy the Feature's completion criteria.

**Context integrity constraints.** Reported coverage must reflect the true, current state of acquisition for the cycle being reported, not an optimistic or stale summary.

**Trust constraints.** Coverage gaps must never be silently absent from the report (Product Principle P5, primary driver); the report must honestly represent partial coverage (Goal G4).

**Policy constraints.** None.

## 12. Acceptance Criteria

1. A coverage report exists for every completed acquisition cycle.
2. Every Source within declared scope is accounted for in the report, either as acquired or as a gap with an attributed reason.
3. No gap entry in the report lacks an attributed reason.
4. The report distinguishes "not yet acquired," "acquired but incomplete," and "declared out of scope" as separate categories.

## 13. Validation Requirements

- That every Source in a workspace's declared scope appears in exactly one coverage category per report.
- That every gap entry carries a non-empty, specific reason.
- That a simulated unreachable Source produces a visible, attributed gap in the report.

## 14. Failure Conditions

- **Silent gaps** (capability §10, primary): the coverage report omits a Source or presents a gap without attribution — this violates Product Principle P5 and must never occur; if detected, it is itself a reportable defect.
- **Misattributed gaps**: a gap's recorded reason does not match its true cause (for example, an unreachable Source misreported as out of scope) — this compromises the report's honesty and must be avoided.

## 15. Traceability

Product Vision (Mission) → G2 (Currency of context), G4 (Trustworthy context) → Product Principles P3, P5 → Capability FEP-002-CAP-02 (Context Acquisition) → Epic E02.3 (Acquisition Event Recording & Reporting) → Feature F02.3.2 (Coverage & Gap Reporting).

## 16. Future Considerations

- Increasingly precise coverage reporting, further distinguishing gap categories as acquisition matures (capability §11).
- An explicit product stance on acceptable coverage for inherently partial or sampled source categories, such as very high-volume conversation archives, before their gap semantics can be fully defined (capability §11; epic §8 deferred work).
