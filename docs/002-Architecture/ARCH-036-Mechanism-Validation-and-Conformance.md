# ARCH-036 — Ferret V2 Mechanism Validation and Conformance

| Field | Value |
|---|---|
| **Document ID** | ARCH-036 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — not scheduled by V2-ROADMAP-001; requires the same governance V2-ROADMAP-001 §7 assigns to a Tier 3 Mechanism-Level Design document |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines no test framework, tool, or technology; it defines what conformance evidence must show, not how evidence is produced |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-031 (Mechanism Architecture Principles) §7 — the document-level evidentiary standard this document extends to implementations; ARCH-032, ARCH-033, ARCH-034 (the mechanism documents an implementation must conform to); ARCH-035 (Mechanism Interaction Model) — the composed behavior an implementation must also conform to |
| **Governed By** | ARCH-031 (Mechanism Architecture Principles) |
| **Roadmap Item** | None directly — related to, but distinct from, V2-ROADMAP-001 Tier 2 item RM-06 (Benchmarking Architecture, not yet written) and Tier 4 item RM-10 (V2 Sprint Specification & Plan); this document's relationship to both is stated in §6 |

---

## Purpose

ARCH-031 §7 defines what evidence a *mechanism document* must provide before a Standard Architecture Review may accept it. That evidentiary standard governs documents — ARCH-032, ARCH-033, ARCH-034, and ARCH-035 were each written to satisfy it, and each includes a "Conformance With ARCH-031" section demonstrating that it does.

Nothing yet defines what evidence an *implementation* of those documents must provide before it may be considered to conform to them. That is this document's sole purpose: to state, at the same non-technology-specific register ARCH-031 already uses, what conformance means once persistence, resolution, and surface integration exist as running code rather than as architecture text, and what category of evidence demonstrates it — without naming a test framework, a CI tool, a language, or a verification technology of any kind.

Every statement in this document answers **how conformance to the already-written mechanism documents is demonstrated**. None answers what those mechanism documents, or the conceptual kernel beneath them, should require.

---

## Scope

Covers:
- What it means for an implementation to conform to a mechanism document (§1)
- The categories of evidence conformance requires, mapped to the guarantees and invariants ARCH-032, ARCH-033, and ARCH-034 already state (§2)
- Who reviews conformance evidence, using the review types this repository already has (§3)
- What happens when an implementation cannot conform without weakening a guarantee (§4)
- How conformance (correctness) is distinct from benchmarking (performance), and where the line falls relative to V2-ROADMAP-001's Tier 2 RM-06 item (§5)
- How conformance review relates to V2-ROADMAP-001's Tier 4 RM-10 (§6)

Does not cover, and will not decide:
- Any test framework, assertion library, or CI tool
- Any specific test case, fixture, or dataset
- Any coverage threshold, performance target, or benchmark metric (ARCH-031 §2's layer distinction places these at the Runtime Behavior layer, verified against, not defined by, this document)
- Any redefinition of a guarantee or invariant already stated by ARCH-023 through ARCH-030, ARCH-032, ARCH-033, or ARCH-034
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every guarantee and invariant referenced below is taken as-is from ARCH-032, ARCH-033, and ARCH-034 (and, through them, from ARCH-023 through ARCH-030). This document introduces no new guarantee, invariant, or responsibility for any mechanism to satisfy — it defines only how satisfaction of what those documents already require is demonstrated once code exists. Review types cited (`AR-`, ordinary PR review) are taken as-is from `docs/Reviews/README.md` and V2-ROADMAP-001 §6–§7; this document introduces no new review type.

---

## 1. What Conformance Means

**An implementation conforms to a mechanism document when its observable behavior satisfies every guarantee and invariant that document states, regardless of the technology, data structure, or algorithm used to produce that behavior.**

This follows directly from ARCH-031 §1's definition of a mechanism architecture as realizing a conceptual guarantee "in a concrete, buildable form... while changing nothing about *what* that guarantee is." Conformance is the confirmation, once the concrete form exists, that nothing was in fact changed. It is judged entirely by observable behavior — what a persistence read returns, what a resolution call outputs, what a surface presents — never by inspecting whether a particular technology was used, since ARCH-032 §9, ARCH-033 §11, and ARCH-034 §9 each leave technology open by design.

Conformance is not correctness in the everyday sense of "produces the right answer for a typical input." It is specifically: does the implementation uphold the *guarantee*, including under the failure and edge conditions the guarantee exists to govern (an unreadable persisted record, a deleted dependency, a request with no equivalent candidate). A mechanism that behaves correctly for ordinary inputs but silently treats an unreadable record as valid does not conform, regardless of how well it performs otherwise.

---

## 2. Conformance Evidence Categories

Evidence is organized around the guarantees and invariants the mechanism documents already state — this table adds no new guarantee; it groups existing ones into categories of evidence an implementation must be able to produce.

| Evidence category | Demonstrates | Source guarantees/invariants |
|---|---|---|
| **Fail-closed behavior** | The implementation never treats missing, incomplete, corrupted, or unreadable state as valid or Satisfied | ARCH-032 §6 ("Fail-closed"), §7.1; ARCH-033 §7 ("Fail-closed"), §8.1 |
| **Determinism** | The same inputs and the same recorded/current state always produce the same output, across repeated invocations | ARCH-032 §6 ("Reconstructible in principle"), §7.7; ARCH-033 §7 ("Determinism"), §8 |
| **Ownership boundaries** | No mechanism reads or writes another component's domain state beyond what that component's own mechanism document exposes | ARCH-032 §7.3; ARCH-033 §2, §9–§10; ARCH-034 §8 |
| **Minimality** | An invalidation or Not-satisfied outcome is scoped to the specific candidate/dependency affected, never broadened | ARCH-032 §7.4; ARCH-033 §7 ("Minimality") |
| **No hidden side effects** | Reading, comparing, or presenting never mutates persisted state or triggers recomputation on its own | ARCH-032 §7.5; ARCH-033 §8.3; ARCH-034 §7.2 |
| **No precomputed or cross-call verdicts** | No component stores or reuses a validity/resolution outcome computed at an earlier point in time in place of a fresh check | ARCH-032 §4, §7.6; ARCH-033 §5 step 6, §8.6 |
| **Exact equivalence only** | No component treats a non-equivalent request as a candidate, by ranking, similarity, or any partial match | ARCH-033 §4, §7, §8.5 |
| **Deletion and matrix semantics** | A deleted dependency is unconditionally Not-satisfied with no comparison performed; no class/shape combination the ARCH-030 §5 matrix excludes is ever constructed | ARCH-032 §7.8–§7.11; ARCH-033 §5 steps 2–4, §8.7–§8.8 |
| **Indistinguishable output** | A reused artifact and a freshly computed one produce identical surface content | ARCH-034 §2, §6, §7.3 |
| **Baseline degradation** | Every failure at every mechanism seam resolves to the same result full recomputation would have produced, per ARCH-035 §3 | ARCH-035 §3, §5 |

An implementation is not required to produce evidence for a category that cannot arise in its concrete design (for example, a design with no notion of a "superseded" record in the sense ARCH-032 §3 describes would have no separate evidence to offer beyond what fail-closed and determinism already cover) — but it must be able to state, for each row above, either the evidence or the reason the row does not apply to its design, rather than silently omitting a row.

---

## 3. Verification Responsibilities

This document introduces no new review type. Conformance is verified using the review types this repository already has:

- **At the mechanism-document tier** (ARCH-032, ARCH-033, ARCH-034, ARCH-035 themselves), conformance to ARCH-031 is already demonstrated by each document's own "Conformance With ARCH-031" section, verified by a Standard Architecture Review per V2-ROADMAP-001 §7.
- **At the implementation tier**, conformance to §2's evidence categories is verified through the same process V2-ROADMAP-001 §6 already assigns to Tier 4 (Implementation Planning): "Normal PR / code review; no architecture governance review required unless implementation surfaces a conceptual gap." This document does not elevate that review requirement — it states what a normal PR/code review for a V2 mechanism implementation must specifically check, within the review process that already governs all other Ferret code.
- **Escalation** follows the same path every prior document in this series uses: where an implementer or reviewer finds that no design can satisfy a §2 evidence category without contradicting ARCH-032, ARCH-033, or ARCH-034, or without exposing a gap in the conceptual kernel those documents realize, that is a conceptual or mechanism gap, not an implementation detail to route around — it must halt and escalate to a new Architecture Governance Review (V2-ROADMAP-001 §1; ARCH-031 §9), exactly as ARCH-032 §9 already requires for the deletion-signal-production gap it records.

---

## 4. When an Implementation Cannot Conform

If a candidate implementation design cannot satisfy a §2 evidence category without weakening a guarantee ARCH-032, ARCH-033, or ARCH-034 states, three responses are available, in this order of preference:

1. **Choose a different concrete design** that satisfies the guarantee — since ARCH-032 §9, ARCH-033 §11, and ARCH-034 §9 each leave substantial technology freedom, this is expected to resolve the overwhelming majority of such cases.
2. **Escalate to determine whether the guarantee itself rests on an unresolved conceptual gap** (for example, the deletion-signal-production gap ARCH-032 §9 and ARCH-030 §2 already record) — in which case the correct response is a new Architecture Governance Review addressing the gap, not an implementation that quietly ignores the guarantee.
3. **Never** ship an implementation that fails a §2 evidence category while presenting itself as conformant. A non-conformant implementation is not a lesser mechanism realization — per ARCH-031 §1, it is a different, unauthorized architecture, and per this document's own §1, it does not conform regardless of how well it otherwise performs.

---

## 5. Conformance vs. Benchmarking

Conformance and benchmarking answer different questions and are not substitutes for each other:

- **Conformance** (this document) asks: does the implementation uphold the guarantees ARCH-032, ARCH-033, and ARCH-034 already state? This is a pass/fail property, evaluated against fixed, already-frozen guarantees.
- **Benchmarking** (V2-ROADMAP-001 Tier 2, RM-06 — "Benchmarking Architecture," not yet written) asks: does the implementation, once conformant, actually deliver on the Core V2 Principle's measurable outcome (ARCH-023 §5) — how much recomputation is actually avoided, at what cost? This is a measured, not a binary, property, and per V2-ROADMAP-001 §4's description of RM-06, it is scoped "without naming tools, thresholds, or a benchmarking mechanism," exactly as this document is scoped without naming test tools.

**Conformance is a precondition for benchmarking to mean anything.** A non-conformant implementation that appears to "perform well" by silently skipping fail-closed checks or reusing stale verdicts is not demonstrating the Core V2 Principle — it is demonstrating a different, incorrect system that happens to run fast. This document's evidence categories (§2) are therefore a prerequisite gate RM-06, once written, should assume has already been passed — this document does not itself define what RM-06 measures, and RM-06, once written, does not relieve an implementation of this document's conformance requirement.

---

## 6. Relationship to RM-10 (Implementation Planning)

V2-ROADMAP-001 §6 (Tier 4) schedules RM-10 as "V2 Sprint Specification & Plan," governed by "Normal PR / code review; no architecture governance review required unless implementation surfaces a conceptual gap." This document does not add a governance requirement RM-10 does not already have — it specifies what that already-required PR/code review must check, for the specific case of a V2 mechanism implementation, using §2's evidence categories as the checklist. A sprint specification or plan produced under RM-10 should reference this document's §2 the same way it would reference any other acceptance criterion already established for the work it plans.

---

## Relationship to the Conceptual Kernel

This document adds nothing to the frozen kernel and amends none of ARCH-023 through ARCH-030, ARCH-031, ARCH-032, ARCH-033, or ARCH-034. It extends ARCH-031 §7's document-level evidentiary standard to the implementation tier, using only guarantees and invariants those five documents already state. Where it states a rule not verbatim in any of them — the conformance-precedes-benchmarking ordering (§5) chief among them — it is shown to be a direct corollary of the Core V2 Principle requiring a real guarantee before a real measurement can mean anything, not an independent addition to it.

---

## Interaction With RM-07, RM-08, and RM-09

This document applies uniformly to implementations of all three mechanism documents. It has no entry or exit criteria of its own within V2-ROADMAP-001, since it is not a roadmap item; it should be consulted once any of RM-07, RM-08, or RM-09 begins producing implementation code under RM-10.

---

## Interaction With Future ADRs

This document produces no ADR. It states one requirement for every future ADR realizing ARCH-032, ARCH-033, or ARCH-034: per those documents' own "Interaction With Future ADRs" sections, each ADR must state which guarantee or invariant it upholds and how — this document's §2 is the checklist against which that statement should be verified, both at ADR-acceptance time and again once the ADR is implemented.

---

## Conformance With ARCH-031

| ARCH-031 §7 requirement | Satisfied by |
|---|---|
| Guarantee-by-guarantee trace | §2, organizing every guarantee/invariant from ARCH-032, ARCH-033, and ARCH-034 into ten evidence categories |
| Responsibility trace | §3 (Verification Responsibilities) |
| Ownership trace | §3 — verification follows the same ownership as the existing PR/code review and Standard Architecture Review processes; no new reviewing body introduced |
| Explicit non-goals | Scope ("Does not cover") |
| Statement of ADRs produced | None produced; see Interaction With Future ADRs |
| Confirmation no Closed Architectural Decision is contradicted | See Impact on Existing Architecture, below |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses every guarantee and invariant already stated by ARCH-032, ARCH-033, and ARCH-034, and the review types already established by `docs/Reviews/README.md` and V2-ROADMAP-001 §6–§7, without modification.

**Existing components extended.** None. This document assigns no new review type, no new governance body, and no new gate beyond the PR/code review V2-ROADMAP-001 §6 already requires for Tier 4 work.

**Existing components intentionally unchanged.** All of them. RM-06's scope (V2-ROADMAP-001 §4) and RM-10's governance (V2-ROADMAP-001 §6) are both left exactly as the roadmap already states them; this document only clarifies the boundary between this document's conformance concern and RM-06's benchmarking concern (§5).

**New concepts introduced.** None at the conceptual tier. One organizational corollary — the ten-category conformance-evidence table (§2) — is introduced, derived directly from the guarantees and invariants ARCH-032, ARCH-033, and ARCH-034 already state, exactly as V2-ROADMAP-001's own four-tier classification (§7 of that document) was accepted as "purely organisational."

**Closed Architectural Decisions.** All nine (AGR-001 §6) checked individually against this document's text; none is contradicted, narrowed, or reinterpreted.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Source of the Core V2 Principle §5 uses to distinguish conformance from benchmarking |
| [ARCH-030 §2](ARCH-030-Dependency-Participation-Semantics.md) | Source of the deletion-signal-production gap cited as the escalation example in §3 |
| [ARCH-031](ARCH-031-Mechanism-Architecture-Principles.md) | Parent — the document-level evidentiary standard (§7) this document extends to implementations |
| [ARCH-032](ARCH-032-Persistence-Mechanism-Design.md) | Source of persistence-side guarantees/invariants organized in §2 |
| [ARCH-033](ARCH-033-Dependency-Resolution-Mechanism-Design.md) | Source of resolution-side guarantees/invariants organized in §2 |
| [ARCH-034](ARCH-034-Surface-Integration-Mechanism-Design.md) | Source of surface-side guarantees/invariants organized in §2 |
| [ARCH-035](ARCH-035-Mechanism-Interaction-Model.md) | Source of the composed baseline-degradation guarantee organized in §2 |
| [AGR-001](../Reviews/AGR-001.md) | Source of the nine Closed Architectural Decisions confirmed unaffected (Impact, above) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-06 (Benchmarking Architecture, not yet written) and RM-10 (Implementation Planning) — this document's relationship to each is stated in §5, §6 |
| `docs/Reviews/README.md` | Source of the existing review-type taxonomy (`AR-`, etc.) this document reuses without adding to |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Mechanism Validation and Conformance — extends ARCH-031's document-level evidentiary standard to implementations of ARCH-032, ARCH-033, and ARCH-034, and distinguishes conformance from benchmarking (RM-06). Not a V2-ROADMAP-001 item. Pending Standard Architecture Review. |
