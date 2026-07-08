# FEP-003 — Ferret Engineering Program

| Field | Value |
|---|---|
| **Document ID** | FEP-003 |
| **Version** | 1.1 |
| **Status** | Draft — Prompt 3 output (amended per FEP-003A) |
| **Program** | Ferret Engineering Program (FEP) |
| **Authoritative Sources** | [FEP-001 — Product Architecture](FEP-001-Product-Architecture.md), [FEP-002 — Capability Catalog](FEP-002-Capability-Catalog.md) |
| **Last Updated** | 2026-07-08 |

---

> **Amendment (2026-07-08).** The Capability Program Index, epic/feature totals, Global Output 1 (Phase 6), and Global Output 3 below reflect the insertion of E09.2 — Organization Extension Points into [FEP-003-EPIC-CAP-09](epics/FEP-003-EPIC-CAP-09-Extensibility.md), restoring the Organization extension surface FEP-001 §2.9 assigns to Extensibility. Corrected per the Required Correction in [FEP-003A — Engineering Program Review & Freeze](reviews/FEP-003A-Engineering-Program-Review.md).

## Purpose and Standing

FEP-001 and FEP-002 are complete and frozen. This document does not modify either; it treats both as authoritative and converts every capability FEP-002 defined into a structured Engineering Program — the complete set of Epics and Features that realize each capability, their dependencies, a recommended execution order, completion gates, planning-level risks, and deferred work.

This is still Product Architecture, one level more concrete. It is not Engineering Design and not Implementation Planning. No Epic or Feature in this program specifies an API, a class, a database, a protocol, storage, deployment, or a technology or programming-language choice. Every Feature describes an objective and a product outcome — what becomes true of Ferret's behavior — never a mechanism for achieving it. This program ends before Engineering Specification design begins; that is a future, separate FEP prompt.

Per-capability detail lives in [`epics/`](epics/), one file per capability, each following the same structure: Capability Summary, Engineering Epics, Features, Engineering Dependencies, Execution Order, Capability Completion Gates, Risks, and Deferred Work. This document provides the index and the four Global Outputs that only make sense once every capability's program is visible at once.

---

## Capability Program Index

| # | Capability | Epics | Detail |
|---|---|---|---|
| 1 | Workspace Definition | E01.1 Workspace Identity & Lifecycle · E01.2 Scope Declaration & Configuration · E01.3 Workspace Relationships | [FEP-003-EPIC-CAP-01](epics/FEP-003-EPIC-CAP-01-Workspace-Definition.md) |
| 2 | Context Acquisition | E02.1 Source Discovery · E02.2 Content Reading & Preservation · E02.3 Acquisition Event Recording & Reporting | [FEP-003-EPIC-CAP-02](epics/FEP-003-EPIC-CAP-02-Context-Acquisition.md) |
| 3 | Context Organization | E03.1 Entity Extraction · E03.2 Relationship Modeling · E03.3 Structural Change Signaling | [FEP-003-EPIC-CAP-03](epics/FEP-003-EPIC-CAP-03-Context-Organization.md) |
| 4 | Context Maintenance | E04.1 Change Detection · E04.2 Freshness Accounting · E04.3 Re-processing Orchestration & Invalidation | [FEP-003-EPIC-CAP-04](epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md) |
| 5 | Context Assembly | E05.1 Request Interpretation · E05.2 Selection & Ranking · E05.3 Composition & Gap Reporting | [FEP-003-EPIC-CAP-05](epics/FEP-003-EPIC-CAP-05-Context-Assembly.md) |
| 6 | Context Delivery | E06.1 Consumer-Fit Presentation · E06.2 Subscription & Notification · E06.3 Access-Respecting Hand-off | [FEP-003-EPIC-CAP-06](epics/FEP-003-EPIC-CAP-06-Context-Delivery.md) |
| 7 | Provenance & Attribution | E07.1 Lineage Capture · E07.2 Lineage Preservation & Query · E07.3 Provenance Completeness Assurance | [FEP-003-EPIC-CAP-07](epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md) |
| 8 | Access Control & Policy | E08.1 Policy Definition & Scope · E08.2 Permission Evaluation · E08.3 Decision Auditability | [FEP-003-EPIC-CAP-08](epics/FEP-003-EPIC-CAP-08-Access-Control-Policy.md) |
| 9 | Extensibility | E09.1 Acquisition Extension Points · E09.2 Organization Extension Points · E09.3 Delivery Extension Points · E09.4 Extension Governance | [FEP-003-EPIC-CAP-09](epics/FEP-003-EPIC-CAP-09-Extensibility.md) |
| 10 | Observability & Health | E10.1 State Collection · E10.2 Health Reporting & Distinction · E10.3 External Routing | [FEP-003-EPIC-CAP-10](epics/FEP-003-EPIC-CAP-10-Observability-Health.md) |
| 11 | Federation | E11.1 Federation Scope Resolution · E11.2 Cross-Workspace Composition · E11.3 Partial-Success Transparency | [FEP-003-EPIC-CAP-11](epics/FEP-003-EPIC-CAP-11-Federation.md) |

34 Epics, 63 Features, across 11 capabilities.

---

## Global Output 1 — Engineering Roadmap

Phases group Epics by when they can *first* deliver incremental value without violating a dependency, not by calendar time or effort. Every phase after Phase 2 is optional in the sense that the product remains usable without it — each adds a distinct axis of value (currency, trust maturity, operability, openness, scale) on top of a working foundation, rather than being a precondition for basic use.

### Phase 1 — Foundation

**Epics:** E01.1, E01.2

**Why first.** Per FEP-001 §4, every other capability depends on workspace identity and declared scope existing. Nothing else can be meaningfully built, let alone tested, without a workspace to act within.

### Phase 2 — Minimal Context Supply Chain (first usable release)

**Epics:** E02.1, E02.2, E03.1, E03.2, E07.1 (built concurrently, not after), E08.1, E08.2 (minimal form), E05.1, E05.2, E05.3, E06.1, E06.3

**Why this grouping.** This is the smallest set of Epics that lets a consumer issue a real request against a declared workspace and receive a relevant, access-respecting, provenance-bearing answer — one source acquired, organized into entities and relationships, selected and composed for a request, delivered through one surface, gated by at least a minimal policy, with lineage captured from the start. Nothing here can be removed without breaking that end-to-end slice; nothing outside it is required to achieve it. This phase is the basis for the Critical Path (Global Output 4).

**Sequencing note.** E07.1 (Lineage Capture) is listed in this phase but must be built *alongside* E02.2, E03.2, and E05.3, not queued after them — every capability document that produces or transforms context names "provenance as an afterthought" as a named risk, and the only way to avoid it structurally is to interleave it here rather than phase it separately.

### Phase 3 — Currency

**Epics:** E02.3 (coverage/gap half), E03.3, E04.1, E04.2, E04.3, E06.2

**Why this grouping.** Phase 2 answers a request against a snapshot. Phase 3 turns that snapshot into a living system: Maintenance depends on Organization's structural signals (E03.3, from Phase 2) and on Acquisition's coverage reporting (E02.3); Subscription (E06.2) depends on Maintenance's re-processing triggers (E04.3) having something to notify about. This phase cannot start before Phase 2's pipeline exists, but does not block Phase 2 from being usable on its own.

### Phase 4 — Trust Maturity

**Epics:** E07.2, E07.3, E08.3, plus maturing E08.1/E08.2 beyond their Phase 2 minimal form (Policy Scope Granularity, Partial Permission Outcomes)

**Why this grouping.** Phase 2 ships mandatory, minimal provenance and access control — enough to be honest, not yet enough to be fully queryable, fully granular, or fully auditable. Phase 4 matures both trust capabilities once the pipeline has run long enough (through Phase 3's Maintenance cycles) to have real lineage and decision history to mature against.

### Phase 5 — Operability

**Epics:** E10.1, E10.2, E10.3

**Why this grouping.** Observability depends on other capabilities' reporting outputs existing (available from Phase 2 onward) but nothing depends on Observability (FEP-001 §4). It can therefore be built in parallel with Phases 3 and 4 rather than gating either.

### Phase 6 — Openness

**Epics:** E09.1, E09.2, E09.3, E09.4

**Why this grouping.** Extensibility's own risk register warns against defining extension points before there is more than one real source or consumer type to generalize from. Sequencing this phase after Phase 2 (and ideally after Phase 3 has proven the pipeline handles change) gives at least one mature source type and one mature consumer type to extract a genuine pattern from, rather than guessing.

### Phase 7 — Scale

**Epics:** E01.3 (cheap to build early since it has no other prerequisite, but has no consumer until now), E11.1, E11.2, E11.3

**Why this grouping.** Federation depends on the entire capability model already being satisfied within at least two individual workspaces (FEP-001 §4). It is necessarily last, and its own risk register notes that detailed planning here is inherently provisional until real multi-workspace use cases exist.

### Roadmap Summary

| Phase | Theme | Unblocks |
|---|---|---|
| 1 | Foundation | Everything |
| 2 | Minimal Context Supply Chain | First usable release |
| 3 | Currency | Long-lived, self-updating context |
| 4 | Trust Maturity | Full provenance/audit for compliance-sensitive use |
| 5 | Operability | Diagnosability at scale (parallel to 3–4) |
| 6 | Openness | Third-party and new-category growth |
| 7 | Scale | Multi-workspace consumers |

---

## Global Output 2 — Capability Dependency Graph

This restates FEP-001 §4 at the granularity this program needs — which capability's Epics cannot begin until which other capability has delivered *something* usable, not full completion.

```
Workspace Definition
        │
        ▼
Context Acquisition ──────► Context Organization ──────► Context Maintenance
        │                           │                            │
        │                           ▼                            │
        │                   Context Assembly ◄───────────────────┘
        │                           │
        │                           ▼
        │                   Context Delivery
        │                           │
        ▼                           ▼
   Provenance & Attribution (interleaved with every stage above, per Risk in FEP-003-EPIC-CAP-07)
                                    │
                                    ▼
                          Access Control & Policy (gates Assembly & Delivery; informed by Workspace Definition)

Extensibility          — depends on Acquisition and Delivery each being stable enough to extract an extension point from
Observability & Health — depends on every capability's reporting output; nothing depends on it
Federation             — depends on the entire model above, satisfied per workspace, more than once
```

No capability dependency introduced here contradicts FEP-001 §4; this is the same graph, annotated with which Epics carry each dependency in practice (see Global Output 3).

---

## Global Output 3 — Epic Dependency Graph

Cross-capability Epic dependencies only; within-capability sequencing is given in each capability's own Execution Order (§5 of each `epics/FEP-003-EPIC-CAP-NN` document).

| Epic | Depends On (cross-capability) |
|---|---|
| E02.1 Source Discovery | E01.2 |
| E03.1 Entity Extraction | E02.2 |
| E04.1 Change Detection | E02.1, E03.3 |
| E04.2 Freshness Accounting | E04.1, E01.2 |
| E04.3 Re-processing Orchestration & Invalidation | E04.1, E04.2, E01.2 |
| E05.2 Selection & Ranking | E04.2, E08.2 |
| E06.1 Consumer-Fit Presentation | E05.3 |
| E06.2 Subscription & Notification | E05.1 (concept reuse), E04.3 |
| E06.3 Access-Respecting Hand-off | E06.1, E08.2 |
| E07.1 Lineage Capture | E02.3, E03.2, E05.3 (interleaved, not sequential — see Phase 2 note above) |
| E07.2 Lineage Preservation & Query | E07.1, E04.3 |
| E07.3 Provenance Completeness Assurance | E07.2 |
| E08.1 Policy Definition & Scope | E01.2 |
| E09.1 Acquisition Extension Points | E02.1 |
| E09.2 Organization Extension Points | E03.1 |
| E09.3 Delivery Extension Points | E06.1 |
| E09.4 Extension Governance | E09.1, E09.2, E09.3, E07.3, E08.1 |
| E10.1 State Collection | Reporting outputs from E02.3, E04.2, E05.3, E06.1/E06.3, E07.3, E08.3 |
| E11.1 Federation Scope Resolution | E01.3 |
| E11.2 Cross-Workspace Composition | E11.1, E05.3 (per participating workspace) |
| E11.3 Partial-Success Transparency | E11.2 |

Epics not listed here (E01.1, E01.2, E01.3, E02.2, E02.3, E03.2, E03.3, E05.1, E05.3, E08.2, E10.2, E10.3) have no cross-capability dependency beyond what is already implied by the chain above, or are the target rather than the source of a listed dependency.

---

## Global Output 4 — Critical Path

The minimum engineering path to a first usable Ferret release — a consumer can issue a request against a declared workspace and receive a relevant, current-at-the-time, access-respecting, provenance-bearing answer:

```
E01.1 → E01.2 → E02.1 → E02.2 → E03.1 → E03.2 → E08.1 → E08.2 → E05.1 → E05.2 → E05.3 → E06.1 → E06.3
                                    ↑
                    E07.1 (Lineage Capture) interleaved from E02.2 onward, not a separate step
```

**Justification.** Every Epic on this path is a hard prerequisite for the next, per the Epic Dependency Graph above:
- Identity and scope (E01.1, E01.2) must exist before anything can be acquired.
- Acquisition (E02.1, E02.2) must exist before there is raw material to organize.
- Organization (E03.1, E03.2) must exist before there is structured context to select from.
- Minimal policy (E08.1, E08.2) must exist before Delivery can be honestly access-gated — Delivery's own non-responsibilities forbid an "ungated for now" shortcut, even at minimum viable scope.
- Assembly (E05.1–E05.3) must exist before there is a composed result to deliver.
- Delivery (E06.1, E06.3) is the last step — presenting the result, access-gated.

**What is deliberately off the Critical Path.** Context Maintenance (Phase 3), full Provenance querying and audit (Phase 4), Observability (Phase 5), Extensibility (Phase 6), and Federation (Phase 7) are not required for the first demonstration of end-to-end value. Their absence means the first release is a snapshot, not yet self-updating, not yet fully auditable, not yet diagnosable, not yet open to new sources or consumers, and confined to one workspace — each a known, named limitation, not a silent one. Provenance is the one exception treated as concurrent rather than deferred: FEP-002-CAP-07 states plainly that mandatory provenance built after the fact is close to unrecoverable, so E07.1 rides alongside the Critical Path rather than after it, even though it is not itself gating any single Epic transition above.

---

## Review

Verified before this program was recorded as complete:

- **Every Capability has complete Epics.** All eleven `epics/FEP-003-EPIC-CAP-NN` documents define exactly three Epics each, with Purpose, Scope, and Success Definition for each.
- **Every Epic has complete Features.** Every Epic's Features table specifies Objective, Product Outcome, Dependencies, and Completion Criteria for every Feature; none were left as placeholders.
- **Dependencies are consistent.** Every cross-capability dependency named in a capability document's §4 (Engineering Dependencies) appears in the Epic Dependency Graph above, and no dependency points to an Epic or Feature that does not exist in this program.
- **Capability boundaries remain unchanged.** No Epic or Feature assigns a capability a responsibility beyond what FEP-002 defined; several Risk entries (e.g., in CAP-05, CAP-09) exist specifically to flag where planning-level scope creep toward another capability's responsibility is a live risk, which is itself evidence the boundary was actively checked, not assumed.
- **No implementation decisions were introduced.** No Feature specifies a mechanism, technology, data structure, protocol, or storage approach; every Completion Criterion is phrased as an observable product behavior.
- **The roadmap is internally consistent.** Every phase in the Engineering Roadmap only contains Epics whose cross-capability dependencies (Global Output 3) are satisfied by an earlier or the same phase; no phase depends on a later one.
- **The Engineering Program can be executed incrementally.** Phase 2 alone constitutes a complete, usable, honestly-scoped release (per the Critical Path); every subsequent phase is additive value, not a precondition for the product to function at all.

---

## Deliverable Boundary

This document, together with the eleven `epics/FEP-003-EPIC-CAP-NN` documents, is the authoritative execution roadmap for Ferret's engineering work once AEF reaches General Availability and implementation is separately authorized. It does not proceed into Engineering Specifications — that is a future, separate FEP prompt, populating [`specifications/`](specifications/) once issued.

---

## Cross References

| Document | Relationship |
|---|---|
| [FEP-001-Product-Architecture.md](FEP-001-Product-Architecture.md) | Authoritative product vision, capability model, and dependency structure this program executes against |
| [FEP-002-Capability-Catalog.md](FEP-002-Capability-Catalog.md) | Authoritative per-capability responsibility, boundary, and constraint definitions each Epic here traces back to |
| [FEP-000-Roadmap.md](FEP-000-Roadmap.md) | Program roadmap recording this document as the Prompt 3 output |

---

## Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | 2026-07-08 | Initial Engineering Program — FEP Prompt 3 output; all eleven capabilities expanded into Epics and Features, plus the four Global Outputs |
| 1.1 | 2026-07-08 | Extensibility corrected per FEP-003A Required Correction: inserted E09.2 — Organization Extension Points (restoring the Organization extension surface FEP-001 §2.9 assigns), renumbered former E09.2 → E09.3 and E09.3 → E09.4; totals updated to 34 Epics, 63 Features |
