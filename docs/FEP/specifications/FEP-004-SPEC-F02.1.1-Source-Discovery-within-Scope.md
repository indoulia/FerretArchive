# FEP-004-SPEC-F02.1.1 — Source Discovery within Scope

### 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F02.1.1 |
| **Capability** | [Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md) |
| **Epic** | E02.1 — Source Discovery |
| **Feature** | F02.1.1 — Source Discovery within Scope |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md)<br>[FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md)<br>[FEP-003-EPIC-CAP-02 — Context Acquisition](../epics/FEP-003-EPIC-CAP-02-Context-Acquisition.md)<br>[FEP-002-CAP-02 — Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md)<br>[FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Nothing can be read that has not first been found. This specification exists so that Acquisition builds and maintains an accurate map of what exists within a workspace's declared scope, satisfying the Feature's objective of discovering sources matching declared scope, so that Acquisition knows what it should attempt to read.

## 3. Scope

- Enumerating sources that exist within the boundary of a workspace's declared scope, across every source category that scope includes.
- Recognizing when a source that matches declared scope newly appears.
- Recognizing when a previously discovered source disappears or ceases to exist.
- Producing and maintaining a current inventory of discovered Sources for use by the rest of Context Acquisition.

## 4. Out of Scope

- Deciding what counts as in-scope — owned by Workspace Definition (FEP-001 §2.1, capability §3 non-responsibilities).
- Reading the content of a discovered source — owned by Faithful Content Reading (F02.2.1).
- Tracking whether a discovered source is currently reachable — owned by Source Reachability Tracking (F02.1.2).
- Interpreting, structuring, or judging relevance of anything found — owned by Context Organization and Context Assembly respectively (FEP-001 Non-Goals; capability §3).
- Writing to, modifying, or acting on any source (FEP-001 Non-Goals).
- Reporting coverage and gaps — owned by Coverage & Gap Reporting (F02.3.2), which consumes this Feature's output.

## 5. Engineering Requirements

1. Acquisition must enumerate all sources that exist within the boundary of a workspace's declared scope, for each source category the scope includes.
2. Acquisition must exclude any source lying outside the declared scope boundary from its discovered inventory.
3. Discovery must be repeatable so that sources appearing within scope after an initial discovery are detected on a subsequent pass.
4. Discovery must detect when a previously discovered source disappears from scope or ceases to exist.
5. Every discovered source must be recorded as a discrete, identifiable Source usable by subsequent acquisition steps.
6. The discovered inventory must be available for downstream reachability tracking (F02.1.2) and coverage reporting (F02.3.2).

## 6. Inputs

- The resolved scope declaration for a workspace, describing which source categories and boundaries apply.
- The prior discovery inventory, where one exists, for the purpose of detecting appearance and disappearance.

## 7. Outputs

- A current inventory of discovered Sources within declared scope.
- Signals indicating that a Source has newly appeared or has disappeared since the prior discovery pass.

## 8. Preconditions

- A workspace has a resolved, declared scope (F01.2.1 — Scope Boundary Declaration, within E01.2 — Scope Declaration & Configuration).

## 9. Postconditions

- Acquisition holds a current, accurate inventory of Sources believed to exist within the workspace's declared scope.
- No Source lying outside declared scope appears in that inventory.

## 10. Dependencies

**Capability dependencies.** Workspace Definition — supplies the scope boundary this Feature discovers within.

**Epic dependencies.** E01.2 — Scope Declaration & Configuration.

**Feature dependencies.** F01.2.1 — Scope Boundary Declaration (prerequisite, per epic file §4).

**External dependencies.** Source systems (version control, document stores, issue trackers, communication archives, and any other declared source category), as the population being discovered against.

## 11. Constraints

**Business constraints.** Discovery must never exceed the scope Workspace Definition declares; discovering out-of-scope content is a policy violation, not a bonus (capability §8).

**Product constraints.** Discovery must operate independently across source categories, so that difficulty discovering one category does not prevent discovery of others.

**Context integrity constraints.** The discovered inventory must accurately reflect what exists within scope — no fabricated, duplicated, or omitted sources.

**Trust constraints.** Discovery results must be attributable to a point in time so that later Acquisition Event Recording (F02.3.1) can associate facts with a specific discovery pass (Product Principle P2).

**Policy constraints.** None beyond scope adherence, which is enforced by Workspace Definition and honored, not re-decided, by this Feature.

## 12. Acceptance Criteria

1. Given a workspace's declared scope, every source that exists and matches that scope is present in Acquisition's discovered inventory.
2. No source lying outside declared scope appears in the discovered inventory.
3. When a new source matching scope appears, a subsequent discovery pass detects and adds it.
4. When a previously discovered source disappears or ceases to exist, a subsequent discovery pass detects and flags its removal.
5. The discovered inventory is consumable by Source Reachability Tracking (F02.1.2) without further transformation.

## 13. Validation Requirements

- That discovery output for a known scope contains exactly the expected sources, with no omissions and no additions.
- That every discovered source can be shown to fall within the declared scope boundary.
- That appearance and disappearance of a source are each detectable by comparing two successive discovery passes.

## 14. Failure Conditions

- **Scope creep** (capability §10): a source outside declared scope is discovered — the system must exclude it from the inventory and make the exclusion visible, never silently include it.
- **Silent gaps** (capability §10): a source exists within scope but is not discovered — once detected, this must surface as a reportable gap (feeding F02.3.2), never remain hidden, per Product Principle P5.

## 15. Traceability

Product Vision (Mission) → G1 (Completeness of context), G6 (Operable at repository scale and beyond) → Product Principles P1, P5, P6 → Capability FEP-002-CAP-02 (Context Acquisition) → Epic E02.1 (Source Discovery) → Feature F02.1.1 (Source Discovery within Scope).

## 16. Future Considerations

- Expansion of recognized source categories as consumer needs grow (capability §11; FEP-001 Open Question 6).
- More precise discovery-state reporting distinguishing "not yet discovered" from "declared out of scope" (capability §11).
- Revisiting discovery scoping as new source categories are prioritized, per the epic's risk that source-category breadth may outpace planning (epic §7).
