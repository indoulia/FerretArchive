# FEP-004-SPEC-F07.2.2 — Provenance Inspection & Summarization

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F07.2.2 |
| **Capability** | [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) |
| **Epic** | E07.2 — Lineage Preservation & Query |
| **Feature** | F07.2.2 — Provenance Inspection & Summarization |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-07 — Provenance & Attribution](../epics/FEP-003-EPIC-CAP-07-Provenance-Attribution.md) · [FEP-002-CAP-07 — Provenance & Attribution](../capabilities/FEP-002-CAP-07-Provenance-Attribution.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A lineage record that only exists internally, never surfaced, cannot serve the reason Provenance & Attribution exists: letting a consumer judge trust for themselves. Provenance Inspection & Summarization exists to make the lineage captured by F07.1.1/F07.1.2 and preserved by F07.2.1 actually reachable — as a full record for direct inspection, or as a summary suited to a consumer's own trust evaluation. This directly serves the Feature's Product Outcome: consumers can evaluate trust without asking Ferret to vouch for correctness.

## 3. Scope

- Retrieving a provenance summary for any unit of context that has a recorded lineage.
- Ensuring a provenance summary reflects the unit's full recorded lineage at the time of retrieval — source, acquisition time, and structuring/assembly history.
- Incorporating freshness facts recorded by Context Maintenance into the summary, so it states how current the unit's content is understood to be.
- Making the full, underlying lineage record directly inspectable, not only available as a pre-composed summary.
- Ensuring inspection and summarization are available equally to any consumer, regardless of that consumer's shape of interaction.

## 4. Out of Scope

- Recording origin or transformation facts — that is F07.1.1 (Acquisition-Origin Recording) and F07.1.2 (Transformation Lineage Recording), whose output this Feature reads but does not produce.
- Guaranteeing that lineage survives re-organization or re-assembly — that is F07.2.1 (Lineage Survivability Across Transformation); this Feature summarizes whatever lineage state currently exists, it does not guarantee that state's continuity.
- Detecting or reporting a unit whose lineage is incomplete — that is F07.3.1 (Provenance Completeness Reporting).
- Deciding who is permitted to see a given unit's provenance — that belongs to Access Control & Policy, an explicit non-responsibility of FEP-002-CAP-07 even though the two are often consulted together.
- Assembling or delivering the underlying context content itself — that is Context Assembly's and Context Delivery's own responsibility; this Feature is consulted alongside them, per FEP-002-CAP-07 §7, but does not perform assembly or delivery.
- Asserting or implying that a summarized unit's content is correct, accurate, or of good quality — an explicit non-responsibility of FEP-002-CAP-07.

## 5. Engineering Requirements

1. A provenance summary must be retrievable for any unit of context that has a recorded lineage.
2. A provenance summary must reflect the unit's full recorded lineage at the time of retrieval, including its origin and every recorded transformation.
3. A provenance summary must state, at minimum, which source(s) the unit originated from, when it was acquired, and how it was subsequently structured or assembled.
4. A provenance summary must incorporate the most recent freshness fact recorded by Context Maintenance for the unit.
5. The full lineage record underlying any summary must be directly inspectable on its own, for a consumer or operator that needs the underlying detail rather than a summary.
6. Provenance inspection and summarization must be available equally to any consumer, without favoring one consumer's shape of interaction over another.
7. Requesting a provenance summary or inspecting a lineage record must never alter the underlying lineage record.

## 6. Inputs

- The recorded lineage record (origin fact and transformation links) for a unit, as captured per F07.1.1/F07.1.2 and preserved per F07.2.1.
- The most recent freshness fact recorded by Context Maintenance for the unit.
- A request, from a consumer or operator, for a unit's provenance.

## 7. Outputs

- A provenance summary for the requested unit.
- The full, directly inspectable lineage record for the requested unit.

## 8. Preconditions

- The unit must already have a recorded lineage (F07.1.2) for there to be anything to summarize or inspect.

## 9. Postconditions

- Any consumer requesting provenance for a unit receives a summary or full record reflecting that unit's actual recorded history.
- No consumer's request for provenance alters the unit's recorded lineage.
- A summary's freshness statement reflects the most recently recorded freshness fact at the time of the request.

## 10. Dependencies

**Capability dependencies.** Context Maintenance — supplies freshness facts incorporated into summaries; Context Assembly and Context Delivery — consult this Feature when a consumer requests, or is owed, provenance alongside content (FEP-002-CAP-07 §7).

**Epic dependencies.** E07.1 (Lineage Capture) — summarization presupposes capture already exists.

**Feature dependencies.** F07.1.2 (Transformation Lineage Recording) — per FEP-003-EPIC-CAP-07 §3, E07.2 Features table; transitively, F07.1.1 (Acquisition-Origin Recording), since origin is part of the full lineage a summary must reflect.

**External dependencies.** Consumer systems (FEP-001 §6) — the human, AI agent, or tool issuing the provenance request.

## 11. Constraints

**Business constraints.** Provenance must be mandatory, not opt-in (FEP-002-CAP-07 §8, Business); inspection and summarization must be available for any unit with recorded lineage, not withheld or gated as an optional add-on.

**Product constraints.** Provenance records must survive transformation (FEP-002-CAP-07 §8, Product); a summary must reflect the unit's current, post-transformation lineage state, not a stale snapshot taken before later transformations occurred.

**Context integrity constraints.** Attribution must exist at the granularity a consumer actually needs (FEP-002-CAP-07 §8, Context integrity); a summary must expose that granularity rather than collapsing fine-grained lineage into an uninformative aggregate.

**Trust constraints.** Per the capability's core non-responsibility, a summary must communicate origin, not correctness — its wording and structure must not be read, by Ferret or by a consumer-facing surface, as a correctness guarantee (FEP-002-CAP-07 §10, Conflated trust and correctness). Per Product Principle P4 (No privileged consumer), summaries must be equally available regardless of consumer shape.

**Policy constraints.** Per FEP-002-CAP-07 §3 (Non-Responsibilities), this Feature must not itself gate access to provenance or to content — that decision belongs to Access Control & Policy.

## 12. Acceptance Criteria

1. A provenance summary requested for a unit with recorded lineage is returned and includes its source(s), acquisition time, and structuring/assembly history.
2. A provenance summary's freshness statement matches the most recently recorded freshness fact for that unit at the time of the request.
3. The full lineage record underlying a summary is separately retrievable in full, not only in summarized form.
4. Requests for the same unit's provenance from consumers of different interaction shapes return summaries reflecting identical underlying lineage.
5. Retrieving a provenance summary or a full lineage record produces no change to the unit's recorded lineage.

## 13. Validation Requirements

- That summary content is complete relative to the underlying lineage record it is drawn from.
- That a summary's freshness statement is current as of the time of the request.
- That retrieval is consumer-neutral — identical underlying lineage produces consistent summaries regardless of who asks.
- That no read operation (summary or full inspection) mutates the underlying lineage record.
- That summarization and inspection are available for every unit with recorded lineage, with no undocumented exception.

## 14. Failure Conditions

- **Conflated trust and correctness.** A provenance summary, or its presentation, is read as a correctness guarantee rather than an origin record (FEP-002-CAP-07 §10). Expected behavior: this must be treated as a defect in the summary's construction or presentation, never accepted as harmless framing.
- **Coarse attribution.** A summary cannot support an actual trust judgment because the underlying lineage was recorded at too broad a granularity (FEP-002-CAP-07 §10). Expected behavior: this limitation must be visible in the summary itself, per Product Principle P5, never masked as though the summary were fully informative.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires, organizes, maintains, assembles, and delivers engineering context) → Goals G3 (Consumer neutrality — provenance access must not favor one consumer shape), G4 (Trustworthy context — this Feature is the point at which trustworthiness becomes actionable for a consumer) → Product Principles P1 (Context over computation — Ferret exposes the record, it does not conclude on the consumer's behalf), P4 (No privileged consumer) → Capability FEP-002-CAP-07 (Provenance & Attribution) → Epic E07.2 (Lineage Preservation & Query) → Feature F07.2.2 (Provenance Inspection & Summarization).

## 16. Future Considerations

- Richer provenance summaries as source categories and transformation steps diversify (FEP-002-CAP-07 §11).
- A possible future capability for consumers to query trust-relevant patterns across provenance records in aggregate, without Ferret itself judging correctness — deferred, not committed (FEP-002-CAP-07 §11; FEP-003-EPIC-CAP-07 §8).
- Summaries remaining interpretable across workspace boundaries as Federation matures — deferred to Federation (FEP-002-CAP-07 §11; FEP-003-EPIC-CAP-07 §8).
