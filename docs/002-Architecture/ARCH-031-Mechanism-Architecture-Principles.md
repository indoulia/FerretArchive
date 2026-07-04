# ARCH-031 — Ferret V2 Mechanism Architecture Principles

| Field | Value |
|---|---|
| **Document ID** | ARCH-031 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — requires a new Architecture Governance Review before this document is treated as frozen, per the same standard AGR-001 §8 applied to every prior addition to the foundation |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines governance rules for future mechanism decisions; it makes no mechanism decision itself that would warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary), ARCH-025 (Artifact Validity Model), ARCH-026 (Persistence Requirements), ARCH-027 (Dependency Resolution Architecture), ARCH-028 (Request Equivalence Architecture), ARCH-029 (Validity Propagation Architecture), ARCH-030 (Dependency Participation Semantics) — collectively, the frozen V2 Foundation this document sits beside |

---

## Purpose

The V2 Foundation — ARCH-023 through ARCH-030, governed by AGR-001 through AGR-004 — is frozen. It answers every conceptual question V2 needs answered: what an AI-derived artifact is, when it is valid, what must persist to check that, how resolution reasons about it, what makes two requests equivalent, how invalidation propagates, and how dependency participation behaves at its edges, including deletion.

None of those documents designs a mechanism. None specifies a storage engine, a cache structure, a key, a hash, a schema, an API, or an algorithm. That is deliberate — V2-ROADMAP-001 Tier 3 (Mechanism-Level Design: RM-07 Persistence Mechanism Design, RM-08 Resolution Mechanism Design, RM-09 V2 Surface Design) is where those decisions belong.

This document is the bridge between the two. It is not a conceptual architecture document — it introduces no new artifact, validity class, dependency shape, or resolution outcome, and it answers no question AGR-001's deferred list left open (all four are closed, per AGR-002, AGR-003, and AGR-004). It is also not an implementation design — it specifies no storage technology, schema, key, hash, or API.

Its sole purpose is to state the rules every future mechanism-level design document must obey in order to faithfully realize the frozen conceptual kernel, without either weakening what the kernel guarantees or re-deciding what the kernel already settled.

---

## Scope

Covers:
- What a mechanism architecture is, and how it differs from conceptual architecture, implementation, and runtime behavior
- Which conceptual guarantees established by ARCH-023 through ARCH-030 a mechanism may never weaken
- Which architectural concepts the frozen kernel defines but deliberately leaves for a mechanism to realize
- The implementation freedom intentionally left open for mechanism documents to exercise
- The class of decision that requires an ADR rather than another ARCH document
- The evidence a mechanism design must provide before it can be approved
- The architectural invariants every mechanism must preserve regardless of the technology it chooses
- What a mechanism document must explicitly avoid doing
- The expected sequence of mechanism documents this document governs

Does not cover:
- Storage engines, schemas, or databases of any kind (SQLite, LMDB, RocksDB, or otherwise)
- APIs of any kind
- Hashes, cache keys, or fingerprint designs
- Serialization formats
- AI provider integrations
- Performance optimizations or performance targets
- Any redefinition of an artifact (ARCH-024), validity class or dependency shape (ARCH-025, ARCH-030), persistence requirement (ARCH-026), resolution outcome (ARCH-027), request-equivalence relation (ARCH-028), or propagation model (ARCH-029)
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every principle, concept, and invariant referenced below is taken as-is from ARCH-023 through ARCH-030 and their governing reviews (AGR-001 through AGR-004). This document introduces no new artifact, validity class, dependency shape, resolution outcome, or component. Where it names a responsibility as belonging to a future mechanism document, that responsibility is one the frozen kernel already left open — never a new one invented here. Ownership follows ARCH-023's Data Ownership principle exactly as already stated.

---

## 1. What Is a Mechanism Architecture?

**A mechanism architecture is the design of *how* a conceptual guarantee already established by the frozen kernel is realized in a concrete, buildable form — while changing nothing about *what* that guarantee is.**

The frozen kernel answers questions of definition and behavior: what validity means (ARCH-025), what must persist for it to be checked (ARCH-026), how resolution reasons about it (ARCH-027), what makes two requests the same (ARCH-028), how invalidation propagates (ARCH-029), and how dependency participation behaves at classification boundaries (ARCH-030). A mechanism architecture answers a different kind of question entirely: given that a dependency's recorded state must persist (ARCH-026), *where and in what structure* does it persist; given that resolution must reach one of three outcomes (ARCH-027 §3), *by what procedure* is a candidate located and its recorded state compared.

A mechanism document is itself still architecture, not code. It specifies component boundaries, responsibilities, and guarantees at the level of a design — not a class diagram, a schema migration, or a pull request. The distinction from conceptual architecture is one of subject matter (how vs. what), not one of rigor or abstraction level.

---

## 2. Mechanism Architecture vs. Conceptual Architecture, Implementation, and Runtime Behavior

These four layers are distinct, and a document written at one layer must not silently make decisions that belong to another.

| Layer | Answers | Example (this series) | Governing artifact |
|---|---|---|---|
| **Conceptual architecture** | What does this concept mean, and what must always be true of it? | "Validity is defined by dependency stability, not output reproducibility" (ARCH-025 §1) | ARCH-023 through ARCH-030 |
| **Mechanism architecture** | How is this concept realized in a concrete, buildable design, without changing what it means? | "Dependency state is recorded in a structure with these properties, consulted by this procedure" | This document governs; RM-07, RM-08, RM-09 (V2-ROADMAP-001 §5) produce |
| **Implementation** | What code, in what language, with what class and method structure, realizes the mechanism? | A specific `.NET` class implementing the persistence mechanism RM-07 specifies | Source code, reviewed via normal PR process (V2-ROADMAP-001 §6, RM-10) |
| **Runtime behavior** | What actually happens when this code executes against a real repository? | A specific resolution call returning `Satisfied` for a specific file | Observed at execution time; verified by tests and, eventually, benchmarks (V2-ROADMAP-001 §4, RM-06) |

A conceptual architecture document must never specify a mechanism (ARCH-023 §4 and its successors already enforce this). A mechanism document must never specify implementation-level detail (a specific class name, a specific line of code) — that is Tier 4 (V2-ROADMAP-001 §6). Runtime behavior is never architecture at all; it is what architecture, once implemented, is verified against.

The practical test for which layer a statement belongs to: **if changing the statement would change what "valid," "satisfied," or "the same request" *means*, it is conceptual and requires a governance review to touch. If changing it would only change *how* those meanings are realized, it is mechanism-level and belongs in a Tier 3 document. If changing it would only change what a specific function does internally without altering the mechanism's external behavior, it is implementation.**

---

## 3. Conceptual Guarantees a Mechanism May Never Weaken

The following are drawn directly from the frozen kernel's Closed Architectural Decisions (AGR-001 §6) and the architectural guarantees ARCH-025, ARCH-027, ARCH-029, and ARCH-030 already state. A mechanism document may implement any of these in whatever concrete form it chooses; it may never implement a form that produces a materially weaker guarantee than the one stated here.

| Guarantee | Statement | Source |
|---|---|---|
| Core V2 Principle | Reuse every valid artifact already produced; recompute only the minimum invalidated portion | ARCH-023 §5 |
| Dependency stability over output reproducibility | Validity is determined solely by whether an artifact's recorded dependencies have changed, never by whether recomputation would reproduce the same output | ARCH-025 §1; AGR-001 §6.6 |
| Deterministic evaluation | Given identical dependency state, whether an artifact is valid, and what a resolution outcome is, is computed deterministically — never inferred, sampled, or approximated | ARCH-023 §5 (AG-004); ARCH-027 §5 |
| Fail-closed | An Indeterminate outcome, or an artifact with a real but unrecorded dependency, is never treated as Satisfied or valid | ARCH-025 §8; ARCH-026 §7; ARCH-027 §5, §6; AGR-001 §6.7 |
| Minimum-invalidation | Invalidation and a Not-satisfied outcome are always scoped to the minimum affected candidate the dependency graph supports, never broadened by category, ownership, or proximity | ARCH-025 §5; ARCH-027 §5 |
| No cross-engine inference / Data Ownership | No engine's artifacts are resolved or invalidated using another engine's internal state; each engine's own dependency signals are the only basis | ARCH-023 §5; ARCH-025 §7; ARCH-027 §2 |
| Resolution is not retrieval | Whatever mechanism locates a candidate artifact, the reasoning that determines whether it still satisfies a request remains the decision procedure ARCH-027 defines — a mechanism may not blur the two into one undifferentiated operation | ARCH-027 §1; AGR-001 §6.9 |
| Exact request equivalence | Two requests are the same only under ARCH-028's contract-level equivalence relation; a mechanism may not substitute a looser or fuzzier notion of "similar enough" | ARCH-028 |
| Transitive, point-in-time propagation consistency | Multi-artifact dependency chains combine outcomes per ARCH-029's rule, evaluated at the moment of each check; a mechanism may not cache or short-circuit this in a way that produces a stale combination | ARCH-029 |
| Deletion is unconditional and irreversible per dependency instance | A mechanism may never treat a recreated target as satisfying a dependency on a deleted one | ARCH-030 §2, §7 |
| No new source of truth | A mechanism maintains no independent record of "what's valid" that could diverge from what the owning V1 component already knows | ARCH-023 §4; ARCH-027 §5 |
| The eight approved component names are the only owning-component vocabulary | A mechanism introduces no new owning component or engine | AGR-001 §6.2 |

A mechanism design that cannot satisfy one of these without altering it has found a conceptual gap, not a mechanism decision — per §6 below, that requires escalation, not a workaround.

---

## 4. Architectural Concepts That Become Mechanism Responsibilities

The frozen kernel deliberately defines each of the following concepts without specifying how it is realized. Realizing them is the explicit job of the mechanism documents this ARCH governs.

| Concept | What the kernel defines (fixed) | What a mechanism must decide (open) | Mechanism document |
|---|---|---|---|
| **Dependency State** | What must persist for validity to be evaluated deterministically, and which existing component owns it (ARCH-026) | Where and in what structure this state is actually stored, subject to ARCH-026's constraints (repository-local, owned by the existing component, additive not parallel) | RM-07 (Persistence Mechanism Design) |
| **Request Equivalence** | What makes two requests the same, as an exact, contract-level relation (ARCH-028) | How that relation is actually computed and compared at request time | RM-08 (Resolution Mechanism Design) |
| **Artifact Validity** | The four validity classes, five dependency shapes, their applicability matrix, and deletion semantics (ARCH-025, ARCH-030) | How a validity determination is actually evaluated and represented at runtime | RM-07, RM-08 (jointly — persistence supplies the state; resolution supplies the evaluation) |
| **Dependency Resolution** | The three possible outcomes and the guarantees resolution must uphold (ARCH-027) | The actual decision procedure, including how a candidate is checked against persisted state | RM-08 (Resolution Mechanism Design) |

No mechanism document may treat any row's "fixed" column as open for renegotiation. Each mechanism document's job is bounded entirely to its "open" column.

---

## 5. Implementation Freedom Intentionally Left to Mechanism Documents

Within the invariants of §3 and the responsibilities of §4, mechanism documents have full latitude over:

- Storage technology and structure (a file, an embedded database, an in-memory structure — any of these, provided persistence remains repository-local and owned by the existing component per ARCH-026)
- Key design, hashing scheme, and fingerprinting approach
- Serialization format
- The specific algorithm or data structure used to evaluate a resolution outcome or check request equivalence
- API surface and shape, where a surface is needed (RM-09)
- Performance characteristics and optimization strategy, subject only to not weakening a §3 guarantee to achieve them

This freedom is intentional, not a gap to be closed by this document. ARCH-023 through ARCH-030 were written so that more than one mechanism could satisfy them; naming a single mechanism here would foreclose that freedom without cause.

---

## 6. Which Decisions Require an ADR Instead of Another ARCH

This series has not needed an ADR to date because no document in it has yet made a mechanism decision — every "Related ADRs" field through ARCH-030 states exactly that. That changes starting with RM-07.

- **An ARCH document** (or an amendment to one, reviewed via a new AGR) is required when a decision changes what a concept *means* — a new validity class, a new dependency shape, a redefined resolution outcome, a different equivalence relation. This is conceptual architecture, and per §2, it binds every future document, not just the mechanism series.
- **An ADR is required** when a mechanism document makes a specific, consequential technology or design choice within the freedom §5 grants — for example, choosing a specific storage technology, a specific hashing algorithm, or a specific serialization format for dependency state. These are exactly the category of decision ARCH-023 §4 and every subsequent conceptual document have deferred as "not yet warranting an ADR" precisely because no such choice had been made yet.
- **The test:** if reversing the decision would require a new governance review because it would change a guarantee in §3, it is an ARCH-level (or AGR-level) decision. If reversing it would only require a new ADR and an implementation change, because §3's guarantees would hold either way, it belongs in an ADR referenced by the mechanism document that made it.

Each Tier 3 mechanism document (RM-07, RM-08, RM-09) is expected to record its consequential technology choices as one or more ADRs, cross-referenced from the mechanism document itself — the mechanism document states the design and the constraints it satisfies; the ADR records the specific choice and its rationale.

---

## 7. Evidence Every Mechanism Design Must Provide Before Approval

A mechanism document is not approvable on the strength of internal consistency alone. Before a Tier 3 document (RM-07, RM-08, RM-09) can pass its Standard Architecture Review (V2-ROADMAP-001 §7), it must provide:

1. **A guarantee-by-guarantee trace.** For every row in §3, an explicit statement of how the proposed mechanism preserves it — not merely an assertion that it does.
2. **A responsibility trace.** For the concept(s) it realizes (§4), a statement of exactly which "open" decision it is making, and confirmation that it leaves every "fixed" element of the kernel untouched.
3. **An ownership trace.** Confirmation that the mechanism is owned by the same existing V1 component ARCH-026 §3 and ARCH-025 §7 already assign, and introduces no new owning component (AGR-001 §6.2).
4. **An explicit non-goals section**, naming what the mechanism deliberately does not do, in the same register ARCH-023 through ARCH-030 already use.
5. **A statement of which ADRs it produced (§6)**, if any, with a cross-reference to each.
6. **Confirmation that no Closed Architectural Decision (AGR-001 §6) is contradicted**, and that no conceptual guarantee (§3) is weakened, narrowed, or made conditional by the mechanism's concrete form.

Absent any one of these, the review is incomplete — this mirrors, at the mechanism tier, the same evidentiary standard AGR-001's methodology (§2) already applied at the conceptual tier: no finding, and by extension no approval, rests on a recollection or summary rather than a verified trace against the source text.

---

## 8. Architectural Invariants Every Mechanism Must Preserve

These restate §3's guarantees as invariants a mechanism design is checked against, in the compact form a reviewer applies during evidence review (§7):

| Invariant | What it forbids a mechanism from doing |
|---|---|
| **Fail-closed** | Treating missing, incomplete, or unreadable dependency state as evidence of validity |
| **Repository First** | Introducing any state, structure, or source of truth that does not live in the repository the same way every other V1 component's state already does |
| **Existing ownership** | Assigning a mechanism's data or behavior to any component other than the one ARCH-025 §7 / ARCH-026 §3 already name as owner |
| **Minimum invalidation** | Invalidating, or reporting Not-satisfied for, any artifact beyond the minimum the dependency graph actually implicates |
| **No hidden side effects** | Mutating persisted dependency state, invoking `IModelProvider`, or performing an owning engine's work as a byproduct of resolution or persistence |
| **No silent recomputation** | Falling back to recomputation without the outcome being an explicit, observable Not-satisfied or Indeterminate result the owning engine can see and act on |
| **Deterministic evaluation** | Producing a different validity or resolution outcome for the same request against the same persisted state on two separate evaluations |

A mechanism document that cannot satisfy every row in this table, in its specific concrete design, has not yet met the bar for approval — regardless of how well-engineered, performant, or elegant that design otherwise is.

---

## 9. What Mechanism Documents Must Explicitly Avoid

- **Redefining any conceptual element.** No mechanism document may restate, rename, or subtly narrow a validity class, dependency shape, resolution outcome, equivalence rule, or propagation rule — cite the defining document instead (per the Repository-First Method already established across the series).
- **Reopening a Closed Architectural Decision.** Per AGR-001 §8, this requires a new governance review, never an inline mechanism-document assumption.
- **Introducing a new owning component or engine.** Mechanism design happens within an existing component's ownership boundary (§4, §8); it never creates a ninth approved component.
- **Treating implementation freedom (§5) as license to weaken an invariant (§3, §8).** Freedom of technology choice is not freedom from the guarantees that choice must still satisfy.
- **Drifting into the explicit Out of Scope list (Scope, above) under the guise of "necessary detail."** A mechanism document may reference that a hash, key, or schema will exist; specifying its actual form is exactly the content the document exists to contain, but doing so must not be mistaken for license to also redesign persistence *requirements* (ARCH-026), which remain fixed.
- **Silently assuming an answer to a question this series has not yet settled.** If a mechanism document discovers it cannot proceed without deciding something the kernel left open, it must halt and escalate to a new governance review (V2-ROADMAP-001 §1), exactly as every prior document in this series has been bound to do.
- **Presenting a mechanism decision as though it carries the same authority as a conceptual one.** A mechanism document's Standard Architecture Review (V2-ROADMAP-001 §7) is not equivalent to an Architecture Governance Review; it does not freeze a foundation document and does not close a deferred question.

---

## Relationship to the Frozen Conceptual Kernel

This document does not join the frozen kernel as an eighth conceptual document. ARCH-023 through ARCH-030 remain the complete conceptual foundation; nothing here amends any of them, and no Closed Architectural Decision is touched. This document instead sits beside the kernel as a standing prerequisite: every mechanism document the roadmap schedules (RM-07, RM-08, RM-09) must satisfy it in addition to, never instead of, the specific conceptual document(s) it implements against.

Where a future document must choose between citing this document and citing ARCH-023 through ARCH-030 directly for a specific guarantee, it cites the originating conceptual document — this document is the rulebook for how to build against that guarantee, not a substitute source of the guarantee itself.

---

## Expected Mechanism Document Sequence

This document governs, but does not itself produce, the following sequence — identical to V2-ROADMAP-001 Tier 3 (§5), restated here only to bind each item to this document's evidentiary requirements (§7) before it may pass its Standard Architecture Review:

1. **RM-07 — Persistence Mechanism Design.** Realizes Dependency State (§4) against ARCH-026's requirements.
2. **RM-08 — Resolution Mechanism Design.** Realizes Request Equivalence and Dependency Resolution (§4) against ARCH-027 and ARCH-028, and depends on RM-07 being complete.
3. **RM-09 — V2 Surface Design.** Realizes any CLI/MCP-facing surface for V2 capabilities, depending on both RM-07 and RM-08 being complete.

This sequence is expected, not binding beyond the dependency order V2-ROADMAP-001 already fixes — a later document may re-scope within it, provided it does not violate this document's invariants (§8) or the roadmap's dependency ordering (V2-ROADMAP-001 §2).

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses every guarantee, principle, and closed decision from ARCH-023 through ARCH-030 and AGR-001 through AGR-004 without modification, as the fixed reference set every mechanism document must be checked against.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behavior to any V1 component. It defines governance rules for future mechanism documents; it performs no mechanism work itself.

**Existing components intentionally unchanged.** All of them. Every ownership assignment, validity class, dependency shape, resolution outcome, equivalence rule, and propagation rule established by the frozen kernel remains exactly as those documents left it.

**New concepts introduced.** One, purely organizational: the distinction between conceptual architecture, mechanism architecture, implementation, and runtime behavior (§2), and the evidentiary standard (§7) and invariant checklist (§8) a mechanism document must satisfy. Neither introduces a new component, artifact, validity concept, or architectural decision — they exist only to ensure the mechanism documents V2-ROADMAP-001 already schedules faithfully realize, rather than silently reinterpret, the kernel those documents build on.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Source of the Core V2 Principle, Data Ownership principle, and the eight approved component names this document treats as invariant (§3, §8) |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Source of the artifact inventory every mechanism document must build against without redefinition |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Source of the validity classes, dependency shapes, and fail-closed/minimum-invalidation principles this document treats as invariant (§3, §4, §8) |
| [ARCH-026](ARCH-026-Persistence-Requirements.md) | Source of the persistence requirements RM-07 (§4, Expected Mechanism Document Sequence) must realize |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Source of the resolution concept, outcomes, and guarantees RM-08 must realize (§3, §4, §8) |
| [ARCH-028](ARCH-028-Request-Equivalence-Architecture.md) | Source of the exact equivalence relation this document treats as invariant (§3, §4) |
| [ARCH-029](ARCH-029-Validity-Propagation-Architecture.md) | Source of the transitive-closure and outcome-combination model this document treats as invariant (§3) |
| [ARCH-030](ARCH-030-Dependency-Participation-Semantics.md) | Source of the deletion-semantics and applicability-matrix invariants this document carries forward (§3) |
| [AGR-001](../Reviews/AGR-001.md) | Source of the Closed Architectural Decisions (§6) this document and every mechanism document must not reopen |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | Sequences RM-07, RM-08, RM-09 as Tier 3 Mechanism-Level Design; this document is their standing prerequisite (Expected Mechanism Document Sequence) |
| Future mechanism documents (RM-07, RM-08, RM-09) | Must cite this document, in addition to their specific conceptual parent(s), as evidence of their evidentiary and invariant compliance |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial draft — establishes mechanism architecture principles as the bridge between the frozen V2 Foundation (ARCH-023–ARCH-030) and Tier 3 mechanism-level design (V2-ROADMAP-001 RM-07–RM-09). Pending governance review. |
