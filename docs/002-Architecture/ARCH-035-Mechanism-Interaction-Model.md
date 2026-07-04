# ARCH-035 — Ferret V2 Mechanism Interaction Model

| Field | Value |
|---|---|
| **Document ID** | ARCH-035 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — not scheduled by V2-ROADMAP-001; requires the same governance V2-ROADMAP-001 §7 assigns to a Tier 3 Mechanism-Level Design document (Standard Architecture Review, escalating to a new Architecture Governance Review only on a discovered conceptual gap), since it makes no conceptual decision of its own |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document composes existing mechanism documents; it makes no decision an ADR would record |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-027 (Dependency Resolution Architecture); ARCH-032 (Persistence Mechanism Design); ARCH-033 (Dependency Resolution Mechanism Design); ARCH-034 (Surface Integration Mechanism Design) — the three mechanism documents this document composes |
| **Governed By** | ARCH-031 (Mechanism Architecture Principles) |
| **Roadmap Item** | None — this document is not a V2-ROADMAP-001 item. It exists because ARCH-032, ARCH-033, and ARCH-034 each define one mechanism's responsibilities in isolation, and no existing document verifies that the three compose into one coherent whole with no gap or overlap between them |

---

## Purpose

ARCH-032, ARCH-033, and ARCH-034 each realize one mechanism's responsibilities: persistence, resolution, and surface integration, respectively. Each states its own boundary with its neighbors (ARCH-032 §8; ARCH-033 §9–§10; ARCH-034 §8). This document does not restate those boundaries — it verifies that, read together, they compose into exactly one path from "an engine is about to fulfil a request" to "a surface presents a result," with no responsibility assigned twice and none left unassigned.

This is not a fourth mechanism. It defines no responsibility, guarantee, or invariant that ARCH-032, ARCH-033, or ARCH-034 does not already state. Its only content is the composition of what they already state, and the failure/degradation behavior of that composition as a whole — which is a property of the three mechanisms taken together, not a property any one of them can state alone.

Every statement in this document answers **how the three already-defined mechanisms compose**. None answers what any of them, or the conceptual kernel beneath them, should be.

---

## Scope

Covers:
- The single, non-branching sequence of responsibility handoffs from request to surfaced result (§1)
- A responsibility matrix proving no lifecycle stage is owned by more than one mechanism document, and none is unowned (§2)
- Failure and degradation behavior of the composed sequence, at each handoff point (§3)
- What happens when V2 (all three mechanisms) is entirely absent (§4)
- The guarantees this composition preserves that no single mechanism document states alone (§5)

Does not cover, and introduces no new statement of:
- Any responsibility, guarantee, or invariant already stated by ARCH-032, ARCH-033, or ARCH-034 — this document cites, never restates with variation
- Any mechanism, storage, key, schema, or API decision
- Any redefinition of the conceptual kernel (ARCH-023 through ARCH-030) or of ARCH-031
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every responsibility, guarantee, and boundary statement referenced below is taken as-is from ARCH-032, ARCH-033, and ARCH-034, which are themselves taken as-is from ARCH-023 through ARCH-030. This document performs no independent derivation from the conceptual kernel — where it needs a fact about validity, resolution, or persistence, it cites the mechanism document that already states it, never the conceptual kernel directly, since the mechanism documents are what compose here.

---

## 1. The Composed Sequence

For a single request, the composed sequence is:

1. **An owning engine receives a request** and constructs its identity per ARCH-028 §2 (engine responsibility, explicit parameters, ambient scope) — an activity owned entirely by the engine, outside any of the three mechanism documents.
2. **The engine invokes resolution (ARCH-033)**, supplying the request's identity, before producing a new artifact (ARCH-033 §1, "Resolution Responsibility... Report").
3. **Resolution performs retrieval (ARCH-033 §1, §4)**: it asks persistence (ARCH-032) whether a candidate exists whose recorded request identity is equivalent, per ARCH-028 §3, to the current one.
4. **Persistence exposes what it holds (ARCH-032 §5)**: the candidate's recorded dependency state, its durable output if any, its recorded request-identity properties, and whether its record is readable and complete. Persistence performs no comparison and reaches no outcome of its own (ARCH-032 §1, "No persistence mechanism performs resolution").
5. **Resolution performs comparison and combination (ARCH-033 §5)** over what persistence exposed and current dependency state, reaching exactly one outcome: Satisfied, Not-satisfied, or Indeterminate (ARCH-027 §3; ARCH-033 §3).
6. **Resolution reports the outcome to the engine** (ARCH-033 §1, "Report"; ARCH-027 §2) — never to a surface, never to persistence.
7. **The engine decides** what to do with the outcome (ARCH-027 §1; ARCH-023 §9): reuse the candidate if Satisfied, or recompute if Not-satisfied or Indeterminate. This decision belongs to the engine alone — no mechanism document decides it.
8. **If the engine recomputes and the result is a reuse candidate**, the engine invokes persistence (ARCH-032 §1, "Record") to record the new artifact's dependency state, and its own output where the engine chooses to make it durable (ARCH-032 §2.2).
9. **The engine produces its existing surface artifact** (`CommandResult`, `McpToolResult`, `McpResourceContent`, or another from ARCH-024 §7) from whichever artifact resulted — reused or freshly computed — indistinguishably (ARCH-034 §2, §6).
10. **Surface integration presents the result** (ARCH-034 §1, §4) exactly as it would with no V2 mechanism present, except that the artifact behind it may have been reused rather than recomputed.

No step above is stated for the first time in this document — each is a direct citation of a responsibility ARCH-032, ARCH-033, or ARCH-034 already assigns. This document's contribution is the sequence and the seams between steps (§2, §3), not the steps themselves.

---

## 2. Responsibility Matrix

Every lifecycle stage in the composed sequence (§1) is owned by exactly one mechanism document — this table exists to make that exhaustively verifiable, not to introduce new assignments.

| Stage | Owned by | Not owned by |
|---|---|---|
| Constructing request identity | The engine itself (ARCH-028 §2) | No mechanism document — this precedes all three |
| Retrieval (locating a candidate) | ARCH-033 §1, §4 | Never ARCH-032 (persistence exposes; it does not locate) or ARCH-034 (has no calling relationship to resolution, ARCH-034 §8) |
| Exposing persisted state | ARCH-032 §5 | Never ARCH-033 (resolution consumes what is exposed; it does not itself hold or expose persisted state) |
| Comparison and combination | ARCH-033 §5 | Never ARCH-032 ("no persistence mechanism performs resolution," ARCH-032 §1) |
| Reporting an outcome | ARCH-033 §1, "Report" | Never surfaced directly by ARCH-034 (ARCH-034 §8: "surface integration has no visibility into... how resolution reached its outcome") |
| Deciding reuse vs. recompute | The engine itself (ARCH-023 §9; ARCH-027 §1) | No mechanism document — all three explicitly disclaim this decision (ARCH-032 §1; ARCH-033 §6; ARCH-034 §3) |
| Recording a new artifact's state | ARCH-032 §1, "Record" | Never ARCH-033 (§6: "resolution does not persist anything") or ARCH-034 |
| Producing the surface artifact | The engine itself, using ARCH-034's invariants | ARCH-034 states what must remain true of this; it does not perform it on the engine's behalf (ARCH-034 §3) |
| Presenting the result | ARCH-034 §1, §4 | Never ARCH-032 or ARCH-033 (ARCH-033 §10: "resolution... makes no assumption about... how or whether its outcome ever reaches a CLI or MCP surface") |

No cell in this table is empty and no stage appears in more than one "Owned by" cell. Where a stage is owned by "the engine itself," that is unchanged from ARCH-023's Data Ownership principle and is not a gap this document needs to fill — the three mechanism documents exist precisely to define what happens on either side of the engine's own, un-mechanized decisions, not to replace them.

---

## 3. Failure and Degradation Behavior

Each seam in the composed sequence (§1) has a defined failure behavior, and every one of them degrades toward the same place: full recomputation, exactly as if no V2 mechanism existed.

| Failure point | What happens | Why (citation) |
|---|---|---|
| Persistence cannot expose a complete or readable record for a candidate | Resolution reports Indeterminate | ARCH-032 §5 (integrity signal); ARCH-033 §3, §8.1 |
| Retrieval finds no candidate with an equivalent request identity | Resolution reports Not-satisfied, by default | ARCH-027 §4; ARCH-033 §3, §4 |
| A dependency in the candidate's chain has changed, or a chain link's target was deleted | Resolution reports Not-satisfied | ARCH-025 §4, §5; ARCH-030 §2; ARCH-033 §5 |
| Resolution itself cannot be reached or fails to complete | The engine proceeds exactly as it would with no resolution outcome at all — the pre-V2 baseline (ARCH-023 §6: "V1 does not require V2 to function") | ARCH-023 §6; ARCH-027 §6 |
| The engine recomputes but cannot record the result (persistence unavailable) | The engine's output to the surface is unaffected — recording failure only forfeits a future reuse opportunity, never the current result | ARCH-026 §7 ("degrades to recomputation, never to incorrect reuse"); ARCH-032 §6 |
| Surface integration receives a freshly computed, rather than reused, artifact | No different behavior than today, with no V2 mechanism present at all | ARCH-034 §2 (indistinguishable output) |

**No failure at any seam ever produces an incorrect result.** Every failure mode above resolves to either Not-satisfied or Indeterminate, both of which the engine treats identically to "no candidate was ever available" — the composed sequence has no failure path that reaches the surface as anything other than the same result full recomputation would have produced. This is not a new guarantee; it is the necessary consequence of ARCH-032 §6, ARCH-033 §7, and ARCH-034 §6 each independently upholding fail-closed at their own seam.

---

## 4. Total Absence of V2

Where none of the three mechanisms is present at all — no persistence, no resolution, no surface integration beyond what already exists — the composed sequence reduces to steps 1, 7 (always "recompute," since there was never anything to consult), 9, and 10 of §1: the engine receives a request, computes its result the way it always has, and presents it through its existing surface. This is the same behavior ARCH-023 §6 already requires ("V1 does not require V2 to function... V2's absence... never prevents the engine from proceeding via its pre-V2 baseline behaviour"). This document adds nothing to that guarantee; it shows that the composed three-mechanism sequence degrades to exactly that baseline, one seam at a time, rather than all at once — which is what makes the individual failure rows in §3 sufficient to prove the whole.

---

## 5. Guarantees This Composition Preserves

These are properties of the sequence as a whole; each rests entirely on guarantees already stated individually by ARCH-032, ARCH-033, or ARCH-034 (cited per row), not on anything newly asserted here.

| Composed guarantee | Rests on |
|---|---|
| No responsibility is exercised twice | §2 (Responsibility Matrix) — each stage has exactly one owner |
| No responsibility is left unexercised | §2 — no stage lacks an owner |
| Every failure degrades to the pre-V2 baseline, never to an incorrect result | §3, §4; ARCH-032 §6, ARCH-033 §7, ARCH-034 §6 (each mechanism's own fail-closed guarantee) |
| The engine's reuse-vs-recompute decision is never made, or preempted, by a mechanism | §1 step 7; ARCH-023 §9; ARCH-027 §1 |
| No mechanism observes or depends on another mechanism's internal state beyond what that mechanism's own document exposes | §2; ARCH-032 §8, ARCH-033 §9–§10, ARCH-034 §8 (each mechanism's own boundary statement) |

---

## Relationship to the Conceptual Kernel

This document adds nothing to the frozen kernel, amends none of ARCH-023 through ARCH-030, and amends none of ARCH-031, ARCH-032, ARCH-033, or ARCH-034. It composes what those documents already state and states nothing they do not. Where it appears to state a new guarantee (§5), each is shown to rest entirely on citations to the mechanism documents it composes, not on independent reasoning about the conceptual kernel.

---

## Interaction With RM-07, RM-08, and RM-09

This document has no entry or exit criteria of its own within V2-ROADMAP-001, because it is not a roadmap item. It should be read after ARCH-032, ARCH-033, and ARCH-034 are each individually complete, and it should be re-verified — not re-derived — whenever any of the three changes in a way that could alter a responsibility boundary, a guarantee, or a failure behavior it cites.

---

## Interaction With Future ADRs

This document produces no ADR and makes no decision an ADR would record. A future ADR realizing any of ARCH-032, ARCH-033, or ARCH-034 should be checked against this document only for one thing: that the ADR's concrete choice does not relocate a responsibility from the cell §2 assigns it to, or introduce a new failure path that reaches a surface as anything other than the pre-V2 baseline (§3).

---

## Conformance With ARCH-031

| ARCH-031 §7 requirement | Satisfied by |
|---|---|
| Guarantee-by-guarantee trace | §5, tracing five composed guarantees to their component mechanisms |
| Responsibility trace | §1 (Composed Sequence), §2 (Responsibility Matrix) |
| Ownership trace | §2 — every "Owned by" and "Not owned by" cell traces to an existing mechanism document |
| Explicit non-goals | Scope ("Does not cover") |
| Statement of ADRs produced | None produced; see Interaction With Future ADRs |
| Confirmation no Closed Architectural Decision is contradicted | See Impact on Existing Architecture, below |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses every responsibility, guarantee, and boundary statement already made by ARCH-032, ARCH-033, and ARCH-034, and, through them, by ARCH-023 through ARCH-030, without modification.

**Existing components extended.** None. This document assigns no new responsibility to any mechanism document or V1 component — it verifies that existing assignments compose without gap or overlap.

**Existing components intentionally unchanged.** All of them. No mechanism document's text is altered by this document's existence.

**New concepts introduced.** None. This document is a composition and verification exercise over three already-written mechanism documents; every table and statement in it is a citation, not an addition.

**Closed Architectural Decisions.** All nine (AGR-001 §6) remain exactly as ARCH-032, ARCH-033, and ARCH-034 already left them; this document introduces no new point of contact with any of them.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Source of the Data Ownership principle and the "V1 does not require V2" guarantee this document's §4 restates by composition, not by independent derivation |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Source of the reuse-vs-recompute ownership rule central to §1 step 7 and §2 |
| [ARCH-031](ARCH-031-Mechanism-Architecture-Principles.md) | Governing document — the evidentiary standard this document is written to satisfy |
| [ARCH-032](ARCH-032-Persistence-Mechanism-Design.md) | Composed document — source of every persistence-side citation in §1–§3 |
| [ARCH-033](ARCH-033-Dependency-Resolution-Mechanism-Design.md) | Composed document — source of every resolution-side citation in §1–§3 |
| [ARCH-034](ARCH-034-Surface-Integration-Mechanism-Design.md) | Composed document — source of every surface-side citation in §1–§3 |
| [AGR-001](../Reviews/AGR-001.md) | Source of the nine Closed Architectural Decisions confirmed unaffected (Impact, above) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | Sequences RM-07, RM-08, RM-09 as the three documents this document composes; this document itself is not a roadmap item |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Mechanism Interaction Model — composes ARCH-032, ARCH-033, and ARCH-034 into one verified sequence with no responsibility gap or overlap. Not a V2-ROADMAP-001 item. Pending Standard Architecture Review. |
