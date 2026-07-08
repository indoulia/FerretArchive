# FEP-003-EPIC-CAP-04 — Engineering Program: Context Maintenance

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-04 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-04 — Context Maintenance](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Context Maintenance keeps organized context current as sources change: detecting change, tracking freshness, triggering re-processing, and invalidating what is no longer valid. It never re-reads or re-derives content itself, and it never presents unconfirmed context as current.

## 2. Engineering Epics

### E04.1 — Change Detection

- **Purpose.** Detect that a source or its derived structure may have changed.
- **Scope.** Consuming change signals from sources and from Organization; recognizing workspace scope changes.
- **Success Definition.** A real change is reliably detected within the workspace's declared expectations.

### E04.2 — Freshness Accounting

- **Purpose.** Track and expose the freshness of every unit of context.
- **Scope.** Maintaining current/stale/unknown state and age per context unit; honoring workspace-specific freshness expectations.
- **Success Definition.** The freshness of any context unit is knowable at any time, and never defaults to "assumed current."

### E04.3 — Re-processing Orchestration & Invalidation

- **Purpose.** Trigger re-acquisition/re-organization on detected change, and invalidate context that is no longer valid.
- **Scope.** Triggering Acquisition and Organization; invalidating context on removal or scope change; ensuring complete propagation.
- **Success Definition.** Detected change reliably results in re-processing or invalidation, with no orphaned, ghost context left assemblable.

## 3. Features

### E04.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F04.1.1 — Source Change Detection | Detect that a source may have changed, via push notification or polling. | Provides the trigger Acquisition needs to re-read a source. | F02.1.2 | A change at a source is detected within the workspace's declared freshness expectation. |
| F04.1.2 — Structural & Scope Change Consumption | Consume structural change signals from Organization and scope change signals from Workspace Definition. | Freshness judgments reflect source-level and structure-level change; scope changes are never missed. | F03.3.1, F01.2.3 | Every structural or scope change signal is reflected in an updated freshness or eligibility judgment. |

### E04.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F04.2.1 — Freshness State Tracking | Track current/stale/unknown state and age for every context unit. | Assembly has the eligibility information it needs to avoid serving stale context as current. | F04.1.1, F04.1.2 | Every context unit's freshness state is resolvable; "unknown" is a distinct, honest state. |
| F04.2.2 — Workspace-Specific Freshness Expectations | Apply a workspace's declared freshness expectation when judging staleness. | Freshness judgments respect what each workspace has actually asked for. | F04.2.1, F01.2.2 | Two workspaces with different declared expectations produce different staleness judgments for equivalent change patterns. |

### E04.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F04.3.1 — Re-acquisition & Re-organization Triggering | Trigger Context Acquisition and Context Organization when change is detected. | The pipeline stays current without manual re-processing. | F04.1.1, F04.1.2 | A detected change reliably results in the affected Acquisition Unit and its derived structure being re-processed. |
| F04.3.2 — Invalidation Propagation | Invalidate context that is no longer valid, ensuring completeness. | Removed or out-of-scope context cannot be mistakenly assembled as current. | F04.2.1, F01.2.3 | A removed source or out-of-scope change results in complete invalidation, excluded from Assembly's eligible set. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F02.1.2, F03.3.1, F01.2.2, F01.2.3.
- **Prerequisite Epics.** E02.1 (Source Discovery), E03.3 (Structural Change Signaling), E01.2 (Scope Declaration & Configuration).
- **Prerequisite Capabilities.** Workspace Definition, Context Acquisition, Context Organization.

## 5. Execution Order

1. **E04.1** — nothing can be judged stale before change is detected.
2. **E04.2** — depends on change detection to have a basis for state.
3. **E04.3** — depends on both prior epics, since triggering and invalidation both act on detected change and tracked freshness.

## 6. Capability Completion Gates

- **Functional completeness.** Every context unit in a workspace has a resolvable, honest freshness state at all times.
- **Validation readiness.** A simulated source removal results in complete invalidation of its derived context, with no orphaned context remaining assemblable.
- **Documentation readiness.** The current/stale/unknown distinction and each workspace's freshness expectation are documented clearly enough for Assembly's authors to correctly implement eligibility checks.
- **Review completion.** FEP-002-CAP-04's non-responsibilities (no re-reading, no re-deriving, no delivery decisions) confirmed unviolated.

## 7. Risks

- **Change-signal granularity mismatch.** Planning change detection without agreeing what granularity of change matters risks epics that don't compose cleanly with Acquisition's and Organization's own granularity assumptions.
- **Freshness expectation underspecification.** If workspace-specific expectations aren't scoped concretely, "honor the workspace's expectation" risks becoming an unfalsifiable requirement.
- **Invalidation completeness is easy to under-scope.** "Ensure invalidation propagates completely" touches every capability that has ever produced derived context from a given source; under-scoping it risks a completion gate that can't actually be verified.

## 8. Deferred Work

- Predictive staleness (anticipating likely-to-go-stale context before a change signal arrives) — deferred pending real change-pattern data.
- Cross-workspace freshness reconciliation — deferred until Federation is underway.
