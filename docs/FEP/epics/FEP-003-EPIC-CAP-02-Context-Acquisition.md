# FEP-003-EPIC-CAP-02 — Engineering Program: Context Acquisition

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-02 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-02 — Context Acquisition](../capabilities/FEP-002-CAP-02-Context-Acquisition.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Context Acquisition discovers and reads engineering-relevant content from sources within a workspace's declared scope, preserving it faithfully for Organization to structure. It does not interpret, filter, or rank what it reads, and it never writes back to a source.

## 2. Engineering Epics

### E02.1 — Source Discovery

- **Purpose.** Discover what exists within a workspace's declared scope.
- **Scope.** Discovering sources matching declared scope; recognizing when a source appears or disappears; tracking reachability.
- **Success Definition.** Acquisition has a current, accurate map of discoverable sources within scope, with reachability known for each.

### E02.2 — Content Reading & Preservation

- **Purpose.** Read source content faithfully, without loss.
- **Scope.** Reading discovered, reachable sources; preserving content without lossy transformation; isolating failures per source.
- **Success Definition.** Raw material handed to Organization faithfully represents the source at acquisition time, and one source's failure never blocks another's.

### E02.3 — Acquisition Event Recording & Reporting

- **Purpose.** Record what happened during acquisition and make gaps visible.
- **Scope.** Attaching acquisition-time facts for Provenance; reporting coverage and gaps to Observability.
- **Success Definition.** Every acquisition attempt is recorded, and coverage gaps are always visible rather than silent.

## 3. Features

### E02.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F02.1.1 — Source Discovery within Scope | Discover sources matching declared scope. | Acquisition knows what it should attempt to read. | F01.2.1 | Every discoverable source within declared scope is known to Acquisition, and no out-of-scope source is included. |
| F02.1.2 — Source Reachability Tracking | Track which known sources are currently reachable versus unreachable. | Enables partial-failure resilience and honest gap reporting. | F02.1.1 | Reachability state is knowable per source, and changes to it are observable. |

### E02.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F02.2.1 — Faithful Content Reading | Read the content of a reachable source without lossy transformation. | Organization receives material it can extract full meaning from. | F02.1.2 | Acquired material is demonstrably a faithful representation of the source content at time of reading. |
| F02.2.2 — Partial-Failure Resilience | Ensure failure to read one source does not prevent reading others. | A workspace's acquisition coverage degrades gracefully rather than catastrophically. | F02.2.1 | A simulated failure in one source's reading has no effect on the successful reading of others in the same workspace. |

### E02.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F02.3.1 — Acquisition Event Recording | Record source identity, acquisition time, and outcome for every acquisition attempt. | Provenance & Attribution has the origin facts it requires. | F02.2.1 | Every Acquisition Unit has an associated, recorded Acquisition Event. |
| F02.3.2 — Coverage & Gap Reporting | Report what was and was not successfully acquired, and why. | Observability & Health can distinguish full from partial coverage, satisfying Product Principle P5. | F02.1.2, F02.2.2 | A coverage report exists for every acquisition cycle, and every gap is attributed to a specific reason. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F01.2.1 (Scope Boundary Declaration).
- **Prerequisite Epics.** E01.2 (Scope Declaration & Configuration).
- **Prerequisite Capabilities.** Workspace Definition.

## 5. Execution Order

1. **E02.1** — nothing can be read before it is discovered.
2. **E02.2** — depends on discovery and reachability tracking.
3. **E02.3** — its recording mechanism should be designed concurrently with E02.2, not bolted on after, per the mandatory-provenance principle; it is listed last only because its completion depends on reading actually occurring to have events to record.

## 6. Capability Completion Gates

- **Functional completeness.** Every declared, reachable source within scope has been discovered, read, and recorded, for at least the source categories a workspace declares.
- **Validation readiness.** A simulated unreachable source produces a visible, attributed gap rather than a silent absence.
- **Documentation readiness.** The distinction between "not yet acquired," "acquired but incomplete," and "declared out of scope" is documented and consistently applied.
- **Review completion.** FEP-002-CAP-02's non-responsibilities (no interpretation, no relevance filtering, no writing to sources) confirmed unviolated.

## 7. Risks

- **Source category breadth outpacing planning.** "Any engineering-relevant source" is open-ended (FEP-001 §8); epics scoped around one source category may need revisiting as new categories are prioritized.
- **Coupling reachability to reading.** If reachability tracking is not planned as distinct from reading, partial-failure resilience becomes harder to reason about later.
- **Provenance retrofit risk.** If acquisition-event recording is planned as an afterthought rather than concurrently with content reading, early-built source categories risk incomplete provenance, contradicting Product Principle P2 at the planning level.

## 8. Deferred Work

- Support for inherently partial or sampled source categories (e.g., very high-volume conversation archives), pending an explicit product stance on acceptable coverage.
- Acquisition scheduling sophistication beyond "read what Maintenance signals," deferred until Maintenance's change-signal patterns are established in practice.
