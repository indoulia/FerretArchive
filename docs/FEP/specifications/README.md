# FEP Specifications

Engineering Specifications produced by [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md), one per Feature defined in [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md). Every specification is implementation-independent — it defines WHAT must be engineered, never HOW — consistent with the standing FEP program constraint (no implementation, code, APIs, or runtime decisions until AEF reaches GA; see [FEP-000-Roadmap.md](../FEP-000-Roadmap.md)).

This document is the authoritative index: every row below corresponds to exactly one Feature and exactly one specification file. 63 specifications, matching FEP-003 v1.1's 63 Features exactly (Extensibility corrected per [FEP-003A](../reviews/FEP-003A-Engineering-Program-Review.md) — see Capability 09 below).

---

## Capability 01 — Workspace Definition

Source: [FEP-002-CAP-01](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) · [FEP-003-EPIC-CAP-01](../epics/FEP-003-EPIC-CAP-01-Workspace-Definition.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F01.1.1 | Workspace Declaration | E01.1 — Workspace Identity & Lifecycle | [FEP-004-SPEC-F01.1.1](FEP-004-SPEC-F01.1.1-Workspace-Declaration.md) |
| F01.1.2 | Workspace Lifecycle State Tracking | E01.1 — Workspace Identity & Lifecycle | [FEP-004-SPEC-F01.1.2](FEP-004-SPEC-F01.1.2-Workspace-Lifecycle-State-Tracking.md) |
| F01.2.1 | Scope Boundary Declaration | E01.2 — Scope Declaration & Configuration | [FEP-004-SPEC-F01.2.1](FEP-004-SPEC-F01.2.1-Scope-Boundary-Declaration.md) |
| F01.2.2 | Workspace Configuration Management | E01.2 — Scope Declaration & Configuration | [FEP-004-SPEC-F01.2.2](FEP-004-SPEC-F01.2.2-Workspace-Configuration-Management.md) |
| F01.2.3 | Scope Change Propagation | E01.2 — Scope Declaration & Configuration | [FEP-004-SPEC-F01.2.3](FEP-004-SPEC-F01.2.3-Scope-Change-Propagation.md) |
| F01.3.1 | Relationship Declaration | E01.3 — Workspace Relationships | [FEP-004-SPEC-F01.3.1](FEP-004-SPEC-F01.3.1-Relationship-Declaration.md) |
| F01.3.2 | Relationship Type Model | E01.3 — Workspace Relationships | [FEP-004-SPEC-F01.3.2](FEP-004-SPEC-F01.3.2-Relationship-Type-Model.md) |

## Capability 02 — Context Acquisition

Source: [FEP-002-CAP-02](../capabilities/FEP-002-CAP-02-Context-Acquisition.md) · [FEP-003-EPIC-CAP-02](../epics/FEP-003-EPIC-CAP-02-Context-Acquisition.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F02.1.1 | Source Discovery within Scope | E02.1 — Source Discovery | [FEP-004-SPEC-F02.1.1](FEP-004-SPEC-F02.1.1-Source-Discovery-within-Scope.md) |
| F02.1.2 | Source Reachability Tracking | E02.1 — Source Discovery | [FEP-004-SPEC-F02.1.2](FEP-004-SPEC-F02.1.2-Source-Reachability-Tracking.md) |
| F02.2.1 | Faithful Content Reading | E02.2 — Content Reading & Preservation | [FEP-004-SPEC-F02.2.1](FEP-004-SPEC-F02.2.1-Faithful-Content-Reading.md) |
| F02.2.2 | Partial-Failure Resilience | E02.2 — Content Reading & Preservation | [FEP-004-SPEC-F02.2.2](FEP-004-SPEC-F02.2.2-Partial-Failure-Resilience.md) |
| F02.3.1 | Acquisition Event Recording | E02.3 — Acquisition Event Recording & Reporting | [FEP-004-SPEC-F02.3.1](FEP-004-SPEC-F02.3.1-Acquisition-Event-Recording.md) |
| F02.3.2 | Coverage & Gap Reporting | E02.3 — Acquisition Event Recording & Reporting | [FEP-004-SPEC-F02.3.2](FEP-004-SPEC-F02.3.2-Coverage-and-Gap-Reporting.md) |

## Capability 03 — Context Organization

Source: [FEP-002-CAP-03](../capabilities/FEP-002-CAP-03-Context-Organization.md) · [FEP-003-EPIC-CAP-03](../epics/FEP-003-EPIC-CAP-03-Context-Organization.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F03.1.1 | Entity Extraction | E03.1 — Entity Extraction | [FEP-004-SPEC-F03.1.1](FEP-004-SPEC-F03.1.1-Entity-Extraction.md) |
| F03.1.2 | Entity Continuity Recognition | E03.1 — Entity Extraction | [FEP-004-SPEC-F03.1.2](FEP-004-SPEC-F03.1.2-Entity-Continuity-Recognition.md) |
| F03.2.1 | Relationship Identification | E03.2 — Relationship Modeling | [FEP-004-SPEC-F03.2.1](FEP-004-SPEC-F03.2.1-Relationship-Identification.md) |
| F03.2.2 | Traceability Preservation | E03.2 — Relationship Modeling | [FEP-004-SPEC-F03.2.2](FEP-004-SPEC-F03.2.2-Traceability-Preservation.md) |
| F03.3.1 | Structural Change Detection & Signaling | E03.3 — Structural Change Signaling | [FEP-004-SPEC-F03.3.1](FEP-004-SPEC-F03.3.1-Structural-Change-Detection-Signaling.md) |

## Capability 04 — Context Maintenance

Source: [FEP-002-CAP-04](../capabilities/FEP-002-CAP-04-Context-Maintenance.md) · [FEP-003-EPIC-CAP-04](../epics/FEP-003-EPIC-CAP-04-Context-Maintenance.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F04.1.1 | Source Change Detection | E04.1 — Change Detection | [FEP-004-SPEC-F04.1.1](FEP-004-SPEC-F04.1.1-Source-Change-Detection.md) |
| F04.1.2 | Structural & Scope Change Consumption | E04.1 — Change Detection | [FEP-004-SPEC-F04.1.2](FEP-004-SPEC-F04.1.2-Structural-Scope-Change-Consumption.md) |
| F04.2.1 | Freshness State Tracking | E04.2 — Freshness Accounting | [FEP-004-SPEC-F04.2.1](FEP-004-SPEC-F04.2.1-Freshness-State-Tracking.md) |
| F04.2.2 | Workspace-Specific Freshness Expectations | E04.2 — Freshness Accounting | [FEP-004-SPEC-F04.2.2](FEP-004-SPEC-F04.2.2-Workspace-Specific-Freshness-Expectations.md) |
| F04.3.1 | Re-acquisition & Re-organization Triggering | E04.3 — Re-processing Orchestration & Invalidation | [FEP-004-SPEC-F04.3.1](FEP-004-SPEC-F04.3.1-Re-acquisition-Re-organization-Triggering.md) |
| F04.3.2 | Invalidation Propagation | E04.3 — Re-processing Orchestration & Invalidation | [FEP-004-SPEC-F04.3.2](FEP-004-SPEC-F04.3.2-Invalidation-Propagation.md) |

## Capability 05 — Context Assembly

Source: [FEP-002-CAP-05](../capabilities/FEP-002-CAP-05-Context-Assembly.md) · [FEP-003-EPIC-CAP-05](../epics/FEP-003-EPIC-CAP-05-Context-Assembly.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F05.1.1 | Request Intent Interpretation | E05.1 — Request Interpretation | [FEP-004-SPEC-F05.1.1](FEP-004-SPEC-F05.1.1-Request-Intent-Interpretation.md) |
| F05.1.2 | Constraint Recognition | E05.1 — Request Interpretation | [FEP-004-SPEC-F05.1.2](FEP-004-SPEC-F05.1.2-Constraint-Recognition.md) |
| F05.2.1 | Eligibility-Respecting Selection | E05.2 — Selection & Ranking | [FEP-004-SPEC-F05.2.1](FEP-004-SPEC-F05.2.1-Eligibility-Respecting-Selection.md) |
| F05.2.2 | Relevance Ranking | E05.2 — Selection & Ranking | [FEP-004-SPEC-F05.2.2](FEP-004-SPEC-F05.2.2-Relevance-Ranking.md) |
| F05.3.1 | Context Composition | E05.3 — Composition & Gap Reporting | [FEP-004-SPEC-F05.3.1](FEP-004-SPEC-F05.3.1-Context-Composition.md) |
| F05.3.2 | Assembly Gap Reporting | E05.3 — Composition & Gap Reporting | [FEP-004-SPEC-F05.3.2](FEP-004-SPEC-F05.3.2-Assembly-Gap-Reporting.md) |

## Capability 06 — Context Delivery

Source: [FEP-002-CAP-06](../capabilities/FEP-002-CAP-06-Context-Delivery.md) · [FEP-003-EPIC-CAP-06](../epics/FEP-003-EPIC-CAP-06-Context-Delivery.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F06.1.1 | Delivery Surface Selection | E06.1 — Consumer-Fit Presentation | [FEP-004-SPEC-F06.1.1](FEP-004-SPEC-F06.1.1-Delivery-Surface-Selection.md) |
| F06.1.2 | Fidelity-Preserving Presentation | E06.1 — Consumer-Fit Presentation | [FEP-004-SPEC-F06.1.2](FEP-004-SPEC-F06.1.2-Fidelity-Preserving-Presentation.md) |
| F06.2.1 | Subscription Registration | E06.2 — Subscription & Notification | [FEP-004-SPEC-F06.2.1](FEP-004-SPEC-F06.2.1-Subscription-Registration.md) |
| F06.2.2 | Change Notification Delivery | E06.2 — Subscription & Notification | [FEP-004-SPEC-F06.2.2](FEP-004-SPEC-F06.2.2-Change-Notification-Delivery.md) |
| F06.3.1 | Access-Gated Delivery | E06.3 — Access-Respecting Hand-off | [FEP-004-SPEC-F06.3.1](FEP-004-SPEC-F06.3.1-Access-Gated-Delivery.md) |
| F06.3.2 | Denial/Absence Disambiguation | E06.3 — Access-Respecting Hand-off | [FEP-004-SPEC-F06.3.2](FEP-004-SPEC-F06.3.2-Denial-Absence-Disambiguation.md) |

## Capability 07 — Provenance & Attribution

Source: [FEP-002-CAP-07](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) · [FEP-003-EPIC-CAP-07](../epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F07.1.1 | Acquisition-Origin Recording | E07.1 — Lineage Capture | [FEP-004-SPEC-F07.1.1](FEP-004-SPEC-F07.1.1-Acquisition-Origin-Recording.md) |
| F07.1.2 | Transformation Lineage Recording | E07.1 — Lineage Capture | [FEP-004-SPEC-F07.1.2](FEP-004-SPEC-F07.1.2-Transformation-Lineage-Recording.md) |
| F07.2.1 | Lineage Survivability Across Transformation | E07.2 — Lineage Preservation & Query | [FEP-004-SPEC-F07.2.1](FEP-004-SPEC-F07.2.1-Lineage-Survivability-Across-Transformation.md) |
| F07.2.2 | Provenance Inspection & Summarization | E07.2 — Lineage Preservation & Query | [FEP-004-SPEC-F07.2.2](FEP-004-SPEC-F07.2.2-Provenance-Inspection-Summarization.md) |
| F07.3.1 | Provenance Completeness Reporting | E07.3 — Provenance Completeness Assurance | [FEP-004-SPEC-F07.3.1](FEP-004-SPEC-F07.3.1-Provenance-Completeness-Reporting.md) |

## Capability 08 — Access Control & Policy

Source: [FEP-002-CAP-08](../capabilities/FEP-002-CAP-08-Access-Control-Policy.md) · [FEP-003-EPIC-CAP-08](../epics/FEP-003-EPIC-CAP-08-Access-Control-Policy.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F08.1.1 | Policy Declaration | E08.1 — Policy Definition & Scope | [FEP-004-SPEC-F08.1.1](FEP-004-SPEC-F08.1.1-Policy-Declaration.md) |
| F08.1.2 | Policy Scope Granularity | E08.1 — Policy Definition & Scope | [FEP-004-SPEC-F08.1.2](FEP-004-SPEC-F08.1.2-Policy-Scope-Granularity.md) |
| F08.2.1 | Permission Evaluation Engine | E08.2 — Permission Evaluation | [FEP-004-SPEC-F08.2.1](FEP-004-SPEC-F08.2.1-Permission-Evaluation-Engine.md) |
| F08.2.2 | Partial Permission Outcomes | E08.2 — Permission Evaluation | [FEP-004-SPEC-F08.2.2](FEP-004-SPEC-F08.2.2-Partial-Permission-Outcomes.md) |
| F08.3.1 | Decision Recording & Audit Surfacing | E08.3 — Decision Auditability | [FEP-004-SPEC-F08.3.1](FEP-004-SPEC-F08.3.1-Decision-Recording-Audit-Surfacing.md) |

## Capability 09 — Extensibility

Source: [FEP-002-CAP-09](../capabilities/FEP-002-CAP-09-Extensibility.md) · [FEP-003-EPIC-CAP-09](../epics/FEP-003-EPIC-CAP-09-Extensibility.md)

Amended per [FEP-003A](../reviews/FEP-003A-Engineering-Program-Review.md)'s Required Correction: E09.2 — Organization Extension Points was inserted to restore the Organization extension surface FEP-001 §2.9 assigns; the former E09.2 (Delivery) and E09.3 (Governance) were renumbered to E09.3 and E09.4.

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F09.1.1 | Source Type Extension Point Definition | E09.1 — Acquisition Extension Points | [FEP-004-SPEC-F09.1.1](FEP-004-SPEC-F09.1.1-Source-Type-Extension-Point-Definition.md) |
| F09.1.2 | Source Type Inventory | E09.1 — Acquisition Extension Points | [FEP-004-SPEC-F09.1.2](FEP-004-SPEC-F09.1.2-Source-Type-Inventory.md) |
| F09.2.1 | Structure Type Extension Point Definition | E09.2 — Organization Extension Points | [FEP-004-SPEC-F09.2.1](FEP-004-SPEC-F09.2.1-Structure-Type-Extension-Point-Definition.md) |
| F09.2.2 | Structure Type Inventory | E09.2 — Organization Extension Points | [FEP-004-SPEC-F09.2.2](FEP-004-SPEC-F09.2.2-Structure-Type-Inventory.md) |
| F09.3.1 | Consumer Type Extension Point Definition | E09.3 — Delivery Extension Points | [FEP-004-SPEC-F09.3.1](FEP-004-SPEC-F09.3.1-Consumer-Type-Extension-Point-Definition.md) |
| F09.3.2 | Consumer Type Inventory | E09.3 — Delivery Extension Points | [FEP-004-SPEC-F09.3.2](FEP-004-SPEC-F09.3.2-Consumer-Type-Inventory.md) |
| F09.4.1 | Extension Admission Criteria | E09.4 — Extension Governance | [FEP-004-SPEC-F09.4.1](FEP-004-SPEC-F09.4.1-Extension-Admission-Criteria.md) |

## Capability 10 — Observability & Health

Source: [FEP-002-CAP-10](../capabilities/FEP-002-CAP-10-Observability-Health.md) · [FEP-003-EPIC-CAP-10](../epics/FEP-003-EPIC-CAP-10-Observability-Health.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F10.1.1 | Health Signal Collection | E10.1 — State Collection | [FEP-004-SPEC-F10.1.1](FEP-004-SPEC-F10.1.1-Health-Signal-Collection.md) |
| F10.1.2 | Cross-Capability State Aggregation | E10.1 — State Collection | [FEP-004-SPEC-F10.1.2](FEP-004-SPEC-F10.1.2-Cross-Capability-State-Aggregation.md) |
| F10.2.1 | Health Report Generation | E10.2 — Health Reporting & Distinction | [FEP-004-SPEC-F10.2.1](FEP-004-SPEC-F10.2.1-Health-Report-Generation.md) |
| F10.2.2 | Expected-Gap vs. Failure Distinction | E10.2 — Health Reporting & Distinction | [FEP-004-SPEC-F10.2.2](FEP-004-SPEC-F10.2.2-Expected-Gap-vs-Failure-Distinction.md) |
| F10.3.1 | Observability Sink Routing | E10.3 — External Routing | [FEP-004-SPEC-F10.3.1](FEP-004-SPEC-F10.3.1-Observability-Sink-Routing.md) |

## Capability 11 — Federation

Source: [FEP-002-CAP-11](../capabilities/FEP-002-CAP-11-Federation.md) · [FEP-003-EPIC-CAP-11](../epics/FEP-003-EPIC-CAP-11-Federation.md)

| Feature | Title | Epic | Specification |
|---|---|---|---|
| F11.1.1 | Federation Scope Determination | E11.1 — Federation Scope Resolution | [FEP-004-SPEC-F11.1.1](FEP-004-SPEC-F11.1.1-Federation-Scope-Determination.md) |
| F11.2.1 | Cross-Workspace Context Composition | E11.2 — Cross-Workspace Composition | [FEP-004-SPEC-F11.2.1](FEP-004-SPEC-F11.2.1-Cross-Workspace-Context-Composition.md) |
| F11.2.2 | Cross-Workspace Relevance Reconciliation | E11.2 — Cross-Workspace Composition | [FEP-004-SPEC-F11.2.2](FEP-004-SPEC-F11.2.2-Cross-Workspace-Relevance-Reconciliation.md) |
| F11.3.1 | Contribution Outcome Recording | E11.3 — Partial-Success Transparency | [FEP-004-SPEC-F11.3.1](FEP-004-SPEC-F11.3.1-Contribution-Outcome-Recording.md) |
| F11.3.2 | Partial Composition Disclosure | E11.3 — Partial-Success Transparency | [FEP-004-SPEC-F11.3.2](FEP-004-SPEC-F11.3.2-Partial-Composition-Disclosure.md) |

---

## Totals

| Capability | Specifications |
|---|---|
| 01 Workspace Definition | 7 |
| 02 Context Acquisition | 6 |
| 03 Context Organization | 5 |
| 04 Context Maintenance | 6 |
| 05 Context Assembly | 6 |
| 06 Context Delivery | 6 |
| 07 Provenance & Attribution | 5 |
| 08 Access Control & Policy | 5 |
| 09 Extensibility | 7 |
| 10 Observability & Health | 5 |
| 11 Federation | 5 |
| **Total** | **63** |
