# FEP-002-CAP-07 — Provenance & Attribution

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-07 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.7 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

Ferret does not assert that its context is correct — it asserts where that context came from and what happened to it, so a consumer can judge trust for themselves. Provenance & Attribution exists to make that origin and history knowable for every unit of context Ferret holds, turning "Ferret says so" into "here is exactly where this came from."

## 2. Responsibilities

- Record the origin of every unit of context: which source it came from, and when it was acquired.
- Record the lineage of every transformation a unit of context undergoes — acquisition, organization, and inclusion in an assembly.
- Record freshness facts supplied by Context Maintenance alongside the content they describe.
- Make provenance and lineage queryable and inspectable in their own right, not only as a silent internal record.
- Preserve provenance through restructuring, re-organization, or re-assembly — lineage must survive transformation.

## 3. Non-Responsibilities

- Must never judge the correctness, quality, or truth of the content whose lineage it tracks — it tracks origin, not veracity.
- Must never itself acquire, organize, assemble, or deliver context — it attaches to those capabilities rather than performing their work.
- Must never gate access to context — that belongs to Access Control & Policy, even though the two are often consulted together.

## 4. Inputs

- Acquisition facts from Context Acquisition: source, time, outcome.
- Structural lineage from Context Organization: what raw material produced what structure.
- Freshness facts from Context Maintenance.
- Assembly and delivery outcome facts from Context Assembly and Context Delivery.

## 5. Outputs

- Lineage records attached to, or queryable alongside, any unit of context.
- Provenance summaries suitable for a consumer's own trust evaluation — for example, which source a unit came from, when it was acquired, how it was structured, and when it was last confirmed current.

## 6. Context Objects

- **Lineage Record** — the conceptual chain of origin and transformation for a unit of context.
- **Attribution** — the conceptual association between a unit of context and the source(s) and process(es) that produced it.

## 7. Relationships

Attaches to every stage of the Context Supply Chain (FEP-001 §4) rather than sitting between two of them. Is consulted by Context Assembly and Context Delivery when a consumer requests, or is owed, provenance alongside content. Reports its own completeness to Observability & Health.

## 8. Constraints

- **Business.** Provenance must be mandatory, not opt-in, for any capability that produces or transforms context, per Product Principle P2; a capability that cannot supply provenance facts is not a conforming implementation.
- **Product.** Provenance records must survive every transformation context passes through; a broken lineage chain is a defect, not an acceptable gap.
- **Context integrity.** Attribution must exist at the granularity a consumer actually needs to evaluate trust; attribution that only exists at too coarse a level fails the purpose.

## 9. Success Criteria

- Any unit of delivered context can be traced back to its origin and the transformations it underwent.
- No context exists in the system without an attached, complete lineage record.
- Consumers can use provenance to make their own trust judgments without needing Ferret to vouch for correctness.

## 10. Failure Modes

- **Broken lineage** — a transformation loses the link back to origin, producing context that cannot be attributed.
- **Coarse attribution** — provenance exists but at a granularity too broad to support an actual trust judgment.
- **Provenance as an afterthought** — lineage capture is retrofitted inconsistently, leaving some context fully traceable and other context not, with no way to tell which is which.
- **Conflated trust and correctness** — provenance is misread, by Ferret or by a consumer-facing surface, as a correctness guarantee rather than an origin record.

## 11. Future Evolution

Richer provenance summaries as source categories and transformation steps diversify. Provenance becoming a first-class input to Federation, where lineage must remain interpretable across workspace boundaries. A possible future capability, not committed here, for consumers to query trust-relevant patterns across provenance records in aggregate — without Ferret itself judging correctness.
