# ARCH-029 — Ferret V2 Validity Propagation Architecture

| Field | Value |
|---|---|
| **Document ID** | ARCH-029 |
| **Version** | 1.1 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-003) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines a consistency property, not a mechanism; no mechanism decision exists yet to warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-025 (Artifact Validity Model) §5; ARCH-027 (Dependency Resolution Architecture) §3 — the two frozen sections this document amends |
| **Roadmap Item** | [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) RM-02 |
| **Resolves** | [AGR-001](../Reviews/AGR-001.md) §5, Deferred Question F7 (Invalidation Propagation Timing) |

---

## Purpose

This document resolves AGR-001's Deferred Question F7. AGR-001 framed F7 as a timing question — "whether invalidation propagates eagerly... or lazily." That framing is retired here, not answered on its own terms: propagation is not a scheduling problem, because the frozen foundation authorizes no process that could be scheduled. The actual architectural problem is **temporal consistency** — what must be true about the relationship between an upstream change and every downstream artifact's validity, given that nothing in this architecture watches for changes independently of being asked.

This is the second amendment to the frozen V2 Foundation. It is architecture-only: it defines a consistency property, not a mechanism. Per AGR-001 §8, it does not become part of the frozen foundation on its own. It concludes with the specific changes it proposes (§9) and remains a proposal until a new Architecture Governance Review (AGR-003) accepts it.

---

## Scope

Covers:
- When a change to a dependency becomes architecturally observable
- What "propagation" means as an architectural property, distinct from a running process
- Which propagation models the frozen foundation's existing constraints permit
- The invariants any permitted model must preserve
- How propagation relates to artifact validity (ARCH-025) and dependency resolution (ARCH-027)
- What can and cannot be observed while a multi-step validity determination is being made
- The specific amendments this document proposes to ARCH-025 and ARCH-027

Does not cover:
- Background workers, schedulers, queues, event-handler implementations, or polling
- Storage, database, or cache design of any kind
- APIs of any kind
- Performance or timing guarantees — this document makes no claim about how quickly a change is reflected in wall-clock terms
- AGR-001's other deferred items (F6, F9) — each remains separately owned per AGR-001 §5
- Any redefinition of an artifact (ARCH-024), of validity (ARCH-025) beyond the specific amendment in §9, of resolution (ARCH-027) beyond the specific amendment in §9, or of request equivalence (ARCH-028)
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every term this document builds on — artifact, dependency shape, validity class, resolution outcome, request equivalence — is taken as-is from ARCH-024, ARCH-025, ARCH-027, and ARCH-028. No new engine, subsystem, or process is introduced. Where this document resolves an ambiguity ARCH-025 §5 left open, it does so by making explicit what ARCH-023 §9 and ARCH-027 §1 already establish, not by inventing new architecture.

---

## 1. When Does an Upstream Dependency Change Become Architecturally Observable?

**A change becomes architecturally observable at, and only at, the moment an engine performs a check against it — never before, regardless of how much time has passed since the change actually occurred in the repository.**

This follows directly from two things the frozen foundation already establishes, not from anything new: ARCH-023 §9 states V2 has no side effects and never performs an engine's work; ARCH-027 §1 states resolution "does not detect change; it interprets already-detected or already-recorded change." Neither authorizes anything to watch the repository independently of being asked. A change that has occurred but has not yet been checked for has no standing in this architecture — it is real, but not yet architecturally observable.

This directly dissolves the framing AGR-001 F7 used. "Eager" propagation would require something to notice a change the moment it happens, independent of any check — which nothing in the frozen foundation authorizes. "Lazy" propagation, correctly understood, is not a delay before an eventually-run background job; it is the simple fact that observability *is* consultation. There is no third option and no schedule to choose between — this is a consequence of what is already frozen, not a new design decision.

---

## 2. What Is Propagation?

**Propagation is not a process. It is the property that a validity check performed on an artifact correctly accounts for every dependency in that artifact's full chain — including dependencies that are themselves other artifacts (ARCH-025 §3, dependency shape 2) — not merely the dependencies recorded directly on the artifact being checked.**

ARCH-025 §5 already states that invalidating an upstream artifact "must be capable of invalidating" a downstream one "through that recorded edge." It left two things unstated: how far that capability must reach, and at what moment it takes effect. §1 above answers the second (only at check time, never before). §4 below answers the first (the full transitive chain, not one edge). Propagation, as this document defines it, is the combination of both answers — it describes a property a correctly-performed check must have, not a running process that "propagates" anything on its own initiative.

Throughout this document, "chain" means the full set of an artifact's transitive dependencies, not necessarily a linear sequence. ARCH-025 §3 permits an artifact to carry more than one dependency shape at once — `ContextPackage`, for instance, carries an index/knowledge-state dependency (shape 3) alongside a request-scoped one (shape 5) and, indirectly, a derived-artifact dependency (shape 2) through `SearchResult`. Where dependency shape 2 branches — an artifact depending on more than one other artifact — "chain" refers to the whole resulting set, and every invariant and rule in this document (§4, §6) applies across that entire set, not along a single path through it.

---

## 3. What Architectural Propagation Models Are Permitted?

Two conceivable models exist in the abstract. Only one is compatible with what is already frozen.

**Eager (proactive) propagation** — updating a downstream artifact's recorded validity the moment an upstream dependency changes, independent of any request to check it. **Not permitted.** This would require something to run independently of being consulted — directly contradicting ARCH-023 §9 (no side effects, V2 never initiates) and ARCH-027 §1 (resolution interprets already-recorded change; it does not produce it). No such capability exists anywhere in the frozen foundation, and this document does not introduce one — that would be a background worker, explicitly out of scope.

**Consultative (check-time) propagation** — determining a downstream artifact's validity, including its full transitive chain, only at the moment something checks it, per §1. **Permitted, and the only model this architecture recognizes.** This is not a design choice made here; it is the model already implied by ARCH-023 §9, ARCH-025's existing dependency shapes, and ARCH-027 §1, made explicit and given the transitivity requirement (§4) it was previously missing.

---

## 4. What Invariants Must Every Propagation Model Preserve?

These invariants apply to the one permitted model (§3) and would apply to any future one, should the frozen foundation ever be amended to permit another.

**Transitive closure.** Checking an artifact's validity must chase its full dependency chain — a downstream artifact's check is not complete after confirming only its own directly-recorded dependencies; it must also confirm the validity of every artifact those dependencies themselves rest on, however many links deep. The chain terminates, and requires no further checking, at any Class C or Class D artifact it reaches — ARCH-025 §5 already excludes both from participating in invalidation (Class C as source only, Class D exempt entirely), and this invariant does not extend that exclusion. Transitive closure means following every dependency shape 2 edge to its end, not checking nodes ARCH-025 §5 already places outside the invalidation model.

**No stale positive, ever.** A candidate is never reported valid while any link in its transitive chain is itself invalid — regardless of how much or how little has elapsed since that link became invalid. This is a correctness guarantee evaluated fresh at each check, not a bound on how quickly invalidity becomes known.

**No wall-clock guarantee.** This architecture makes no claim about how much time may elapse between a change and a check that would reveal it — there is no such thing as "propagation delay" here, because nothing propagates independent of a check (§1, §2). Any such guarantee would describe a mechanism this document does not define.

**Determinism and order-independence.** Given the same dependency-chain state, checking it must always yield the same outcome, regardless of the order in which links in the chain happen to be evaluated. This extends AG-004 (Deterministic Behaviour) and ARCH-025 §8's deterministic-validity principle to multi-hop chains specifically.

---

## 5. Interaction With Artifact Validity

ARCH-025 §5's minimum-invalidation principle states that invalidation propagates "along dependency edges, not proximity" — correct as far as it goes, but silent on how many edges a validity check must traverse. Without the transitive-closure invariant (§4), "minimum invalidated portion" is ambiguous for any artifact whose dependency shape 2 (derived-artifact dependency, ARCH-025 §3) chains through more than one other artifact: checking only the first edge could under-scope invalidation and report an artifact valid when an indirect ancestor has actually changed.

This document does not change what makes an artifact valid (ARCH-025 §1's dependency-stability definition is untouched). It closes the gap in how far a validity check must look before it may answer the question ARCH-025 §1 poses.

---

## 6. Interaction With Dependency Resolution

ARCH-027 §3 defines three resolution outcomes — Satisfied, Not-satisfied, Indeterminate — for "a given candidate artifact against a given request." This document adds the rule for combining outcomes across a transitive chain, without changing what any individual outcome means:

- A candidate resolves to **Satisfied** only if every link in its transitive dependency chain independently resolves to Satisfied.
- A candidate resolves to **Not-satisfied** if any link in its chain resolves to Not-satisfied — regardless of what any other link resolves to.
- A candidate resolves to **Indeterminate** if no link resolves to Not-satisfied, but at least one link cannot be resolved to Satisfied (per ARCH-026 §7 and ARCH-027 §3's own Indeterminate definition).

This is the same fail-closed ordering ARCH-025 §8, ARCH-026 §7, and ARCH-027 §5 already establish — Not-satisfied outranks Indeterminate, which outranks Satisfied — applied across a chain instead of a single link. It changes none of ARCH-027 §3's outcome definitions or §5's guarantees; it states how they combine when a candidate's dependency chain has more than one link.

---

## 7. What Guarantees Exist While Propagation Is in Progress?

**None are needed, because no such state exists.** Propagation, as this document defines it (§2), is evaluated fully at the moment of a check — it has no partial or intermediate state that could be observed by anything else, because nothing else is watching (§1). A check either has not yet run, or has completed and produced one of ARCH-027 §3's outcomes, combined per §6. There is no third, in-between state to guarantee anything about.

The one guarantee worth stating explicitly, because it is easy to assume otherwise: **no outcome is ever exposed on the basis of a partially-evaluated chain.** If a check of a multi-link chain cannot be completed, the result is Indeterminate (§6), never a provisional Satisfied awaiting confirmation of the remaining links. This follows from — and does not extend — the fail-closed principle already established.

---

## 8. Explicit Non-Goals

This document does **not** define:

- Any background worker, scheduler, queue, event-handler implementation, or polling mechanism
- Any storage, database, or cache design for recording or checking dependency chains
- Any API through which a check is requested or an outcome reported
- Any performance or timing guarantee — no claim about how quickly a change becomes observable in wall-clock terms
- Any resolution of AGR-001 F6 (deletion semantics) or F9 (validity-class/dependency-shape matrix) — both remain separately owned, per AGR-001 §5
- Any change to ARCH-025's definition of validity (§1) or ARCH-027's definition of its three outcomes, beyond the combination rule in §6
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## 9. Proposed Amendments to the Frozen Foundation

These are proposals. Per AGR-001 §8, they do not take effect until a new Architecture Governance Review (AGR-003) accepts this document and confirms no Closed Architectural Decision is contradicted. Both "current text" citations below were verified against the live documents before being written here.

### Amendment 1 — ARCH-025 §5 (Minimum-Invalidation Principles)

**Current text, verbatim:**
> "**Invalidation propagates along dependency edges, not proximity.** Where one artifact's dependency is another artifact (dependency shape 2, §3), invalidating the upstream artifact must be capable of invalidating the downstream one — but only through that recorded edge, not by association."

**Proposed replacement:**
> "**Invalidation propagates along dependency edges, not proximity.** Where one artifact's dependency is another artifact (dependency shape 2, §3), invalidating the upstream artifact must be capable of invalidating the downstream one — but only through that recorded edge, not by association. This propagation must reach the full transitive chain, evaluated at the moment of each check, never independently of one; the consistency model this requires is formally defined in ARCH-029."

**Rationale:** ARCH-025 §5 stated propagation follows recorded edges but left unstated how many edges a check must traverse, and when. This amendment adds the transitive-closure requirement and a cross-reference; it does not change what makes an artifact invalid.

### Amendment 2 — ARCH-027 §3 (Resolution Outcomes)

**Current text, verbatim, final sentence of §3 before the section break:**
> "Resolution outcomes are always scoped to one candidate artifact against one request. Resolving one request never resolves, or implies an outcome for, any other artifact — even one of the same type or from the same owning engine."

**Proposed addition, inserted immediately after that sentence (no existing text removed):**
> "Where a candidate's dependency chain includes another artifact (ARCH-025 §3, dependency shape 2), these three outcomes combine across the chain per the rule defined in ARCH-029 §6: Satisfied only if every link resolves to Satisfied; Not-satisfied if any link does; Indeterminate otherwise."

**Rationale:** ARCH-027 §3 defines outcomes for a single candidate but does not state how outcomes combine when that candidate's own dependency is another artifact. This amendment adds the combination rule without altering any of the three existing outcome definitions.

### Governance Requirement

This document must be reviewed by a new Architecture Governance Review (AGR-003) before Amendments 1 and 2 are applied to ARCH-025 and ARCH-027. Upon acceptance: ARCH-025 increments to v1.3, ARCH-027 increments to v1.2, both citing AGR-003; this document's own status changes from Draft to Frozen alongside them.

---

## Cross References

| Document | Relationship |
|---|---|
| [AGR-001 §5](../Reviews/AGR-001.md) | The deferred question (F7) this document resolves |
| [ARCH-023 §9](ARCH-023-V2-Architectural-Boundary.md) | No-side-effects principle this document's §1 and §3 rest on |
| [ARCH-025 §1, §5](ARCH-025-Artifact-Validity-Model.md) | §5 amended by this document (§9, Amendment 1); §1's validity definition is preserved, not changed |
| [ARCH-027 §1, §3](ARCH-027-Dependency-Resolution-Architecture.md) | §1's "resolution does not detect change" principle this document's §1 makes explicit; §3 amended by this document (§9, Amendment 2) |
| [ARCH-028](ARCH-028-Request-Equivalence-Architecture.md) | Sibling amendment — resolved AGR-001 F5 using the same amendment pattern this document follows for F7 |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-02 — this document is that roadmap item |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Validity Propagation Architecture — resolves AGR-001 F7 by reframing propagation as a temporal-consistency property rather than a scheduling choice. Proposed amendments to ARCH-025 and ARCH-027 pending AGR-003. |
| 1.1 | 2026-07-03 | Ferret Core Team | AGR-003 review corrections — clarified that "chain" denotes the full transitive dependency set, which may branch, not a linear sequence (§2); clarified that transitive closure terminates at Class C/Class D artifacts consistent with ARCH-025 §5's existing exclusion, rather than extending checking to them (§4). No change to §1, §3, §5–§9's conceptual content or to the proposed amendments in §9. |
