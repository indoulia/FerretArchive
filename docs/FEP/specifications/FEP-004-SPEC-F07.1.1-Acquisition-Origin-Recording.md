# FEP-004-SPEC-F07.1.1 — Acquisition-Origin Recording

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F07.1.1 |
| **Capability** | [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| **Epic** | E07.1 — Lineage Capture |
| **Feature** | F07.1.1 — Acquisition-Origin Recording |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-07 — Provenance & Attribution](../epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md) · [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Every lineage chain has to start somewhere. Acquisition-Origin Recording exists to establish that starting point: the moment a unit of context is first acquired, it must acquire, alongside it, a fact recording where it came from and when. Without this root fact, nothing downstream — structuring, assembly, delivery — could ever be traced back to anything, and Provenance & Attribution's entire purpose (turning "Ferret says so" into "here is exactly where this came from") would have no foundation to stand on. This directly serves the Feature's Product Outcome: establishing the root of every lineage chain.

## 3. Scope

- Recording an origin fact for every Acquisition Unit at, or immediately following, the moment it is acquired.
- Ensuring the origin fact captures which source the unit came from, when it was acquired, and the outcome of that acquisition attempt.
- Ensuring an origin fact exists regardless of source category, and regardless of whether the acquisition attempt fully succeeded, partially succeeded, or failed.
- Establishing the origin fact as the root node from which all later transformation lineage for that unit is traced.

## 4. Out of Scope

- Recording lineage for transformations after acquisition (structuring, assembly) — that is F07.1.2 (Transformation Lineage Recording).
- Preserving an origin fact through later re-organization or re-assembly — that is F07.2.1 (Lineage Survivability Across Transformation).
- Producing consumer-facing provenance summaries or making lineage queryable — that is F07.2.2 (Provenance Inspection & Summarization).
- Detecting or reporting units that lack an origin fact — that is F07.3.1 (Provenance Completeness Reporting).
- Performing the acquisition itself, or recording the underlying acquisition event (source identity, time, outcome as a raw fact) — that is Context Acquisition's F02.3.1 (Acquisition Event Recording); this Feature consumes that fact to establish the lineage root, it does not generate it.
- Judging the correctness, quality, or truth of the acquired content — an explicit non-responsibility of FEP-002-CAP-07.
- Establishing the identity of the source system or the acquiring consumer — per FEP-001 §5.2, Ferret consumes identity, it does not issue it.

## 5. Engineering Requirements

1. Every Acquisition Unit must receive an associated origin fact at, or immediately following, its acquisition.
2. An origin fact must record the source the unit came from, the time it was acquired, and the outcome of the acquisition attempt.
3. An origin fact must be recorded for every acquisition outcome — full success, partial success, and failure — not only for clean successes.
4. An origin fact must be recorded regardless of source category, so that no category of source is structurally exempt from lineage capture.
5. Origin fact recording must not depend on organization, assembly, or delivery having occurred for that unit.
6. An origin fact, once recorded, must not be silently altered; any correction to a recorded origin fact must itself be recorded as part of the unit's lineage, not overwrite the original silently.
7. An origin fact must serve as the anchor from which every later transformation-lineage link for that unit is traceable.

## 6. Inputs

- The acquisition-event fact produced by Context Acquisition for a unit (source, time, outcome), per F02.3.1.
- The identity of the Acquisition Unit the event pertains to.

## 7. Outputs

- A recorded origin fact for the unit — the root of that unit's Lineage Record.

## 8. Preconditions

- Context Acquisition must have attempted acquisition of the unit and produced an acquisition-event fact (F02.3.1).
- Per FEP-003-EPIC-CAP-07 §5 (Execution Order), this Feature must be built concurrently with Context Acquisition's own epics, not sequenced strictly after them — origin recording retrofitted after acquisition work is already complete is the named "afterthought" failure mode this Feature exists to avoid.

## 9. Postconditions

- Every acquired unit has an origin fact attached, independent of what happens to it afterward.
- The origin fact is available as the anchor for any later transformation-lineage recording (F07.1.2).
- The absence of an origin fact for an acquired unit is a distinguishable, non-normal state rather than an unremarkable one.

## 10. Dependencies

**Capability dependencies.** Context Acquisition — supplies the acquisition-event facts this Feature records as origin.

**Epic dependencies.** E02.3 (Acquisition Event Recording & Reporting) — per FEP-003-EPIC-CAP-07 §4 (Engineering Dependencies).

**Feature dependencies.** F02.3.1 (Acquisition Event Recording) — per FEP-003-EPIC-CAP-07 §3, E07.1 Features table. No prerequisite Feature within this capability; F07.1.1 is the first Feature in E07.1.

**External dependencies.** Source systems (FEP-001 §6) — the origin an Acquisition Unit is recorded as coming from is a source system Ferret reads from; this Feature records that origin, it does not integrate with the source system itself.

## 11. Constraints

**Business constraints.** Provenance must be mandatory, not opt-in (FEP-002-CAP-07 §8, Business); every Acquisition Unit must receive an origin fact with no exception carved out for any source category or acquisition outcome.

**Product constraints.** An origin fact, as the root of a lineage chain, must be preserved through every later transformation the unit undergoes (FEP-002-CAP-07 §8, Product) — this Feature is responsible for establishing that root correctly, since a flawed root cannot be repaired by later preservation.

**Context integrity constraints.** Attribution must exist at the granularity a consumer actually needs (FEP-002-CAP-07 §8, Context integrity) — origin facts must be recorded per Acquisition Unit, not aggregated at the level of a source or a batch.

**Trust constraints.** Per Product Principle P2 (Provenance is mandatory, not optional), a unit without a recorded origin fact is not a conforming deliverable.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), this Feature must not absorb Context Acquisition's responsibility for performing acquisition or recording the underlying acquisition event — it consumes that event to establish lineage, nothing more.

## 12. Acceptance Criteria

1. Every unit for which Context Acquisition reports a successful acquisition has an origin fact recording source, time, and outcome.
2. Every unit for which Context Acquisition reports a partial or failed acquisition attempt still has an origin fact reflecting that outcome.
3. An origin fact's recorded source and time match the acquisition-event fact produced by Context Acquisition for the same unit.
4. An Acquisition Unit that lacks an origin fact is identifiable as such, distinct from units that have one.
5. An origin fact, once recorded, is retrievable unchanged prior to any subsequent transformation of the unit.

## 13. Validation Requirements

- That origin-fact coverage reaches every Acquisition Unit, across every source category and every acquisition outcome.
- That origin facts are recorded at per-unit granularity, not aggregated at a coarser level.
- That origin-fact recording does not depend on, or wait for, organization, assembly, or delivery.
- That a recorded origin fact remains unaltered except through an explicitly recorded correction.

## 14. Failure Conditions

- **Provenance as an afterthought.** Origin capture is retrofitted inconsistently, leaving some acquired units with an origin fact and others without, with no way to tell which (FEP-002-CAP-07 §10). Expected behavior: the absence of an origin fact must be a detectable, non-normal state rather than indistinguishable from a unit that was never examined.
- **Coarse attribution.** An origin fact is recorded at the level of a source or batch rather than the individual unit, leaving it too broad to support an actual trust judgment (FEP-002-CAP-07 §10). Expected behavior: this must be treated as a non-conforming recording, not accepted as sufficient.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G1 (Completeness — no unit can be traced if its origin was never recorded), G4 (Trustworthy context — origin is the first fact a consumer needs to judge trust) → Product Principles P2 (Provenance is mandatory, not optional), P3 (Freshness is first-class — acquisition time is a freshness-relevant fact) → Capability FEP-002-CAP-07 (Provenance & Attribution) → Epic E07.1 (Lineage Capture) → Feature F07.1.1 (Acquisition-Origin Recording).

## 16. Future Considerations

- Richer origin facts as source categories diversify beyond what is currently anticipated (FEP-002-CAP-07 §11).
- Origin recording becoming interpretable across workspace boundaries as Federation matures, without this Feature's per-unit guarantees changing (FEP-002-CAP-07 §11).
