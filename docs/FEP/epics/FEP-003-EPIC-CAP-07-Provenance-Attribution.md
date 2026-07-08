# FEP-003-EPIC-CAP-07 — Engineering Program: Provenance & Attribution

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-07 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Provenance & Attribution records, for every unit of context, where it came from and what happened to it — attaching to every stage of the Context Supply Chain rather than sitting at one point in it. It never judges correctness, only origin and history.

## 2. Engineering Epics

### E07.1 — Lineage Capture

- **Purpose.** Record the origin and every transformation a unit of context undergoes.
- **Scope.** Capturing acquisition-origin facts and structural/assembly transformation facts.
- **Success Definition.** Every unit of context has a lineage record from the moment it is first acquired.

### E07.2 — Lineage Preservation & Query

- **Purpose.** Ensure lineage survives transformation and is inspectable.
- **Scope.** Preserving lineage through restructuring, re-organization, and re-assembly; making lineage and provenance summaries queryable.
- **Success Definition.** Lineage is never lost through a transformation, and any consumer or operator can inspect it directly.

### E07.3 — Provenance Completeness Assurance

- **Purpose.** Guarantee provenance is mandatory and complete, not opt-in.
- **Scope.** Verifying every context-producing or transforming capability supplies lineage facts; reporting completeness to Observability.
- **Success Definition.** No context unit exists in the system without a complete, attached lineage record.

## 3. Features

### E07.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F07.1.1 — Acquisition-Origin Recording | Record the source, time, and outcome of acquisition for every Acquisition Unit. | Establishes the root of every lineage chain. | F02.3.1 | Every Acquisition Unit has a recorded origin fact. |
| F07.1.2 — Transformation Lineage Recording | Record how raw material became structured context and how structured context became an assembled result. | Extends the lineage chain through every transformation, not just origin. | F07.1.1, F03.2.2, F05.3.1 | Every structured element and every assembled result has a recorded transformation link back to its inputs. |

### E07.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F07.2.1 — Lineage Survivability Across Transformation | Ensure a re-organization or re-assembly of existing content does not break its existing lineage chain. | Long-lived context remains fully attributable through many Maintenance cycles. | F07.1.2, F04.3.1 | A simulated re-organization of previously organized material preserves an unbroken lineage chain to original acquisition. |
| F07.2.2 — Provenance Inspection & Summarization | Make lineage and origin information queryable and summarizable for a consumer's trust evaluation. | Consumers can evaluate trust without asking Ferret to vouch for correctness. | F07.1.2 | A provenance summary for any delivered context unit is retrievable and reflects its full recorded lineage. |

### E07.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F07.3.1 — Provenance Completeness Reporting | Detect and report any context unit lacking complete lineage. | Makes a provenance gap visible rather than a silent, undetected defect. | F07.2.1, F07.2.2 | A deliberately introduced lineage gap is detected and reported. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F02.3.1, F03.2.2, F05.3.1, F04.3.1.
- **Prerequisite Epics.** E02.3 (Acquisition Event Recording & Reporting), E03.2 (Relationship Modeling), E05.3 (Composition & Gap Reporting).
- **Prerequisite Capabilities.** Context Acquisition, Context Organization, Context Assembly — but see Execution Order below: this is not a strict sequential dependency in practice.

## 5. Execution Order

1. **E07.1** — must be planned and built *concurrently* with Context Acquisition, Organization, and Assembly's own epics, not sequenced after them; provenance retrofitted after the fact is a named failure mode both here and in FEP-001 §8.
2. **E07.2** — depends on capture existing, and on Maintenance's re-processing triggers being in place to verify survivability.
3. **E07.3** — depends on both prior epics; it is a verification/assurance layer over capture and preservation, so it is necessarily last, but should be planned early enough to shape how E07.1's features are scoped.

## 6. Capability Completion Gates

- **Functional completeness.** Every context unit in the system, across every source category and every transformation, has a complete, recorded lineage.
- **Validation readiness.** A deliberately introduced gap is reliably detected by Provenance Completeness Reporting.
- **Documentation readiness.** The distinction between a Lineage Record and an Attribution is documented clearly enough that a consumer-facing surface can present provenance without conflating origin with correctness.
- **Review completion.** FEP-002-CAP-07's non-responsibility — never judging correctness or truth — confirmed unviolated by any completeness or summarization feature.

## 7. Risks

- **Retrofit risk is structural, not incidental.** Because Provenance attaches across every other capability's epics rather than depending on them sequentially, planning it as an independently-timed capability (as this document's per-capability structure implies) risks exactly the afterthought failure mode it exists to prevent; the Engineering Roadmap (FEP-003 Global Outputs) treats E07.1 as interleaved work, not a phase.
- **Granularity disputes.** What counts as "a unit of context" for lineage purposes is not fixed by FEP-001 or FEP-002; different capabilities' epics may assume different granularities, requiring explicit reconciliation before transformation lineage recording can be considered complete.
- **Completeness verification cost.** Proving a negative — "no context exists without lineage" — is harder to bound at the planning level than proving a positive; completeness-reporting criteria may need revisiting once the actual diversity of context-producing epics is known.

## 8. Deferred Work

- Aggregate trust-pattern querying across provenance records — deferred, not committed.
- Federation-spanning lineage interpretation — deferred to Federation.
