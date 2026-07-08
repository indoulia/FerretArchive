# FEP-004-SPEC-F07.3.1 — Provenance Completeness Reporting

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F07.3.1 |
| **Capability** | [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| **Epic** | E07.3 — Provenance Completeness Assurance |
| **Feature** | F07.3.1 — Provenance Completeness Reporting |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-07 — Provenance & Attribution](../epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md) · [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Provenance being "mandatory, not opt-in" is only a real guarantee if a gap in it can actually be found. Provenance Completeness Reporting exists to verify, across every context-producing or transforming capability, that lineage facts genuinely exist and are complete for every unit — and to make any gap visible rather than a silent, undetected defect. This directly serves the Feature's Product Outcome: makes a provenance gap visible rather than a silent, undetected defect.

## 3. Scope

- Verifying that every context-producing or transforming capability supplies lineage facts for the units it produces or transforms.
- Detecting a context unit with an incomplete or missing lineage record — a missing origin fact, a missing transformation link, or a broken chain.
- Reporting a detected completeness gap to Observability & Health, per FEP-002-CAP-07 §7.
- Reporting a positive confirmation of completeness when no gap is found, so that the absence of a reported gap is itself a meaningful signal.
- Reliably detecting a deliberately introduced lineage gap, for validation purposes.

## 4. Out of Scope

- Capturing origin or transformation facts — that is F07.1.1 (Acquisition-Origin Recording) and F07.1.2 (Transformation Lineage Recording), whose output this Feature verifies but does not produce.
- Preserving lineage through re-organization or re-assembly — that is F07.2.1 (Lineage Survivability Across Transformation); this Feature verifies the result of preservation, it does not perform preservation.
- Producing consumer-facing provenance summaries or supporting direct lineage inspection — that is F07.2.2 (Provenance Inspection & Summarization); this Feature's output is a completeness report, not a trust-evaluation summary.
- Remediating or fixing a detected gap — responsibility for the missing fact reverts to whichever capability (Acquisition, Organization, Assembly, Maintenance) should have supplied it; this Feature detects and reports, it does not repair.
- Judging the correctness, quality, or truth of the content whose lineage completeness is being verified — an explicit non-responsibility of FEP-002-CAP-07.
- Operating Observability & Health's own general health-reporting mechanisms beyond supplying this Feature's completeness reports as one input to them.

## 5. Engineering Requirements

1. Every context-producing or transforming capability must be verifiable as supplying lineage facts for every unit it produces or transforms.
2. A context unit with an incomplete or missing lineage record — missing origin, missing transformation link, or a broken chain — must be detected.
3. A detected completeness gap must be reported in a form Observability & Health can consume.
4. Completeness verification must cover every source category and every transformation type in use, not a subset chosen for convenience.
5. A deliberately introduced gap, used for validation purposes, must be detected reliably, with no false negative.
6. The absence of any reported gap must be distinguishable from completeness verification simply not having run.
7. Completeness verification must not alter any lineage record it inspects.

## 6. Inputs

- The full set of recorded lineage records — origin facts, transformation links, and preserved lineage state — across every context unit.
- The inventory of context-producing or transforming capabilities expected to supply lineage facts.

## 7. Outputs

- A completeness report identifying any context unit with an incomplete or missing lineage record.
- A positive confirmation of completeness when verification finds no gap.

## 8. Preconditions

- Lineage capture (F07.1.1, F07.1.2) and preservation (F07.2.1) must already be in place, since this Feature verifies their output rather than producing lineage itself.
- Provenance inspection (F07.2.2) must exist so that a detected gap can be located against an actual, inspectable record.
- Per FEP-003-EPIC-CAP-07 §5, this Feature is a verification and assurance layer over E07.1 and E07.2, and is necessarily last in execution order, though it should be planned early enough to shape how F07.1.1's and F07.1.2's capture work is scoped.

## 9. Postconditions

- Every context unit's lineage-completeness status is known and reported.
- No completeness gap remains undetected and unreported.
- Observability & Health has ongoing visibility into provenance completeness, not a one-time check.

## 10. Dependencies

**Capability dependencies.** Observability & Health — receives this Feature's completeness reports (FEP-002-CAP-07 §7); Context Acquisition, Context Organization, and Context Assembly — the capabilities whose lineage output is being verified for completeness.

**Epic dependencies.** E07.1 (Lineage Capture) and E07.2 (Lineage Preservation & Query) — per FEP-003-EPIC-CAP-07 §5, E07.3 is a verification/assurance layer over both prior epics.

**Feature dependencies.** F07.2.1 (Lineage Survivability Across Transformation) and F07.2.2 (Provenance Inspection & Summarization) — per FEP-003-EPIC-CAP-07 §3, E07.3 Features table.

**External dependencies.** None beyond those already relied on transitively through the lineage-capture and preservation Features this Feature verifies.

## 11. Constraints

**Business constraints.** Provenance must be mandatory, not opt-in (FEP-002-CAP-07 §8, Business); a capability that cannot supply provenance facts is not a conforming implementation, and this Feature is the mechanism that makes such non-conformance visible.

**Product constraints.** A broken lineage chain is a defect, not an acceptable gap (FEP-002-CAP-07 §8, Product); this Feature's reporting must treat every detected gap as a defect requiring visibility, never as tolerable background noise.

**Context integrity constraints.** What counts as "a unit of context" for lineage purposes is not fixed by FEP-001 or FEP-002 (FEP-003-EPIC-CAP-07 §7, Risks — granularity disputes); completeness claims are only meaningful once granularity is reconciled consistently across the capabilities being verified.

**Trust constraints.** Per Product Principle P5 (Degrade by scope, not by silent omission), a detected gap must be reported, never silently absorbed into an apparently complete picture.

**Policy constraints.** Per FEP-003-EPIC-CAP-07 §6 (Review completion gate), this Feature must not itself judge the correctness or truth of content while assessing completeness — it verifies the presence of lineage, not the quality of what that lineage describes.

## 12. Acceptance Criteria

1. A context unit with a complete lineage record is reported as complete.
2. A context unit with a missing origin fact is detected and reported as incomplete.
3. A context unit with a missing transformation link anywhere in its chain is detected and reported as incomplete.
4. A deliberately introduced lineage gap is detected in every trial run against it, with no false negative.
5. A generated completeness report is distinguishable from a state in which no report was generated at all.
6. Running completeness verification produces no change to any lineage record it inspects.

## 13. Validation Requirements

- That completeness verification covers every source category and every transformation type in use.
- That gap detection is reliable against deliberately introduced gaps, with no false negative.
- That verification does not mutate any lineage record it inspects.
- That completeness reports are correctly routed to, and consumable by, Observability & Health.
- That the granularity used for completeness verification is reconciled with the granularity used by lineage capture and preservation, so a completeness claim is meaningful.

## 14. Failure Conditions

- **Provenance as an afterthought.** Lineage capture was retrofitted inconsistently elsewhere in the system, leaving some context fully traceable and other context not, with no way to tell which (FEP-002-CAP-07 §10). Expected behavior: this Feature exists precisely to surface that distinction; a failure to distinguish traceable from non-traceable units is itself a failure of this Feature.
- **Broken lineage left undetected.** A lineage gap exists but completeness reporting fails to surface it. Expected behavior: per Product Principle P5, this must never present as silent completeness; any known limitation in verification coverage must itself be reported, not hidden.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goal G4 (Trustworthy context — a provenance guarantee that cannot be verified is not a guarantee) → Product Principles P2 (Provenance is mandatory, not optional), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-07 (Provenance & Attribution) → Epic E07.3 (Provenance Completeness Assurance) → Feature F07.3.1 (Provenance Completeness Reporting).

## 16. Future Considerations

- Completeness-verification cost and criteria may need revisiting once the actual diversity of context-producing epics across the program is known (FEP-003-EPIC-CAP-07 §7, Risks) — proving the negative "no context exists without lineage" is harder to bound at the planning level than proving a positive.
- A possible future capability for querying trust-relevant patterns across completeness and provenance records in aggregate, without judging correctness — deferred, not committed (FEP-002-CAP-07 §11; FEP-003-EPIC-CAP-07 §8).
