# ARCH-027 — Ferret V2 Dependency Resolution Architecture

| Field | Value |
|---|---|
| **Document ID** | ARCH-027 |
| **Version** | 1.2 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-001) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines a reasoning process, not a mechanism; no mechanism decision exists yet to warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-024 (Artifact Inventory); ARCH-025 (Artifact Validity Model); ARCH-026 (Persistence Requirements) |

---

## Purpose

This document answers one architectural question: **how does a Ferret engine determine whether an existing artifact satisfies the current request without recomputation?**

It defines dependency resolution — the reasoning process an engine uses to reach that determination. It does not define how a satisfying artifact is physically found or fetched. Resolution is a decision procedure, not a retrieval mechanism; cache structures, storage mechanisms, keys, databases, retrieval algorithms, APIs, and performance optimizations are explicitly out of scope. This is the elaboration of the "Validity" boundary responsibility ARCH-023 §9 already assigned to V2 — it introduces no responsibility ARCH-023 did not already establish.

---

## Scope

Covers:
- The concept of dependency resolution and how it differs from retrieval
- Which component is responsible for initiating and performing resolution
- The possible outcomes of a resolution and what each means
- How the current request's own dependencies interact with which candidate artifact is even in scope for resolution
- The architectural guarantees resolution must uphold
- What happens when resolution cannot be completed
- How resolution relates to existing V1 ownership

Does not cover:
- Cache structures or storage mechanisms
- Keys, indexes, or databases of any kind
- Retrieval or search algorithms
- APIs of any kind
- Performance optimizations
- Any redefinition of an artifact (ARCH-024), validity (ARCH-025), or persistence (ARCH-026)

---

## Repository-First Method

Every artifact, validity class, dependency shape, and persistence requirement referenced below is taken as-is from ARCH-024, ARCH-025, and ARCH-026. This document defines no new artifact, no new validity concept, and no new persistence requirement — it defines the reasoning process that consumes what those three documents already established. Ownership follows ARCH-023's Data Ownership principle exactly as already stated; no new engine or subsystem is introduced.

---

## 1. The Concept of Dependency Resolution

**Dependency resolution is the decision procedure by which an engine determines, for a request it is about to fulfil, whether an already-produced artifact still satisfies that request under ARCH-025's validity model — using the dependency state ARCH-026 requires to persist — without performing any computation.**

Three distinctions define its boundaries precisely:

**Resolution is not retrieval.** Retrieval is the physical act of finding and fetching an artifact from wherever it is held — that is a mechanism concern (ARCH-026 explicitly left the "where" and "how" of persistence undesigned) and is out of scope here. Resolution is the reasoning that happens once a candidate artifact is already in view: does it still satisfy the request, yes or no. This document assumes a candidate is identifiable (§4) and defines only what happens once it is.

**Resolution is not invalidation.** Invalidation (ARCH-025 §4) is the event that a dependency changed. Resolution is what happens at request time: it consults whatever invalidation signals and persisted dependency records already exist (ARCH-026) and reasons from them to an outcome. Resolution does not detect change; it interprets already-detected or already-recorded change.

**Resolution is not the decision to reuse.** Per ARCH-023 §9, V2's "Validity" responsibility is to determine whether a valid artifact satisfies a request — the decision of what to do with that determination (accept the reuse, or proceed to recompute) remains entirely owned by the engine that owns the artifact. Resolution answers a question; it does not act on the answer.

---

## 2. Resolution Responsibilities

Resolution responsibility follows the same ownership pattern ARCH-025 §7 and ARCH-026 §3 already established — this document assigns no new ownership.

- **The engine that owns the artifact type initiates resolution.** Knowledge Engine, for `ContextPackage`/`SearchResult`; Review Engine or Artifact Engine, if implemented, for artifacts they would own. Resolution is something an owning engine does before it would otherwise produce a new artifact — never something performed on an engine's behalf without its request.
- **V2 performs the resolution itself, as already established in ARCH-023 §9.** This document elaborates that existing responsibility; it does not create a new one. V2 reads the current request's dependency requirements and the persisted dependency state the owning engine (and whatever components it depends on — Index Engine, Connector Platform, Parser Platform, Workspace Engine, per ARCH-026 §3) already exposes, and applies ARCH-025's validity model to reach an outcome (§3).
- **The owning engine decides what to do with the outcome.** Exactly as ARCH-023 §9 states: the decision to proceed with computation, and the computation itself, remains entirely the owning engine's. Resolution never invokes `IModelProvider` and never performs the engine's work.
- **No engine resolves another engine's artifacts.** This follows directly from Data Ownership (ARCH-023 §5): an engine's artifacts are only ever resolved using that engine's own dependency state, never by inference from another engine's internal state.

---

## 3. Resolution Outcomes

Resolution produces exactly one of three outcomes for a given candidate artifact against a given request:

**Satisfied.** Every dependency the candidate artifact's persisted state records (ARCH-026) matches the current request's own dependencies, and no invalidation source (ARCH-025 §4) has fired against any of them since the candidate was produced. The owning engine may treat the candidate as still valid for this request.

**Not satisfied.** At least one recorded dependency has changed. Per ARCH-025 §5's minimum-invalidation principle, this outcome is scoped as narrowly as the dependency graph allows — it applies to the specific candidate whose recorded dependency changed, never broadened to every artifact of the same type, owner, or category.

**Indeterminate.** The persisted dependency state needed to evaluate the candidate is missing, incomplete, or unreadable (ARCH-026 §7), or the current request's own dependencies cannot be established. This is recorded as a distinct outcome because its cause differs from "Not satisfied" (an absence of information, rather than a confirmed change) — but it carries the same consequence: an indeterminate outcome is never treated as satisfied (§5).

Resolution outcomes are always scoped to one candidate artifact against one request. Resolving one request never resolves, or implies an outcome for, any other artifact — even one of the same type or from the same owning engine.

Where a candidate's dependency chain includes another artifact (ARCH-025 §3, dependency shape 2), these three outcomes combine across the chain per the rule defined in ARCH-029 §6: Satisfied only if every link resolves to Satisfied; Not-satisfied if any link does; Indeterminate otherwise.

---

## 4. Interaction Between Dependency State and Artifact Selection

Resolution does not search for, rank, or select among a universe of candidate artifacts — that would be retrieval, which is out of scope (§1). The candidate an engine resolves against is determined by the request itself, not by resolution.

The request-scoped input dependency (ARCH-025 §3, dependency shape 5) is what identifies which candidate, if any, is even in scope: a candidate enters resolution only if it was produced for a request equivalent — per ARCH-028's exact, contract-level equivalence relation — to the current one. Dependency state's role in resolution is **confirmatory, not exploratory**: given a candidate the request itself already identifies, resolution checks whether that candidate's recorded dependencies still hold. It does not evaluate multiple unrelated candidates to find the "best" one, and it does not rank candidates by similarity or relevance — both of those are retrieval concerns.

Where no candidate is identifiable from the request at all, resolution has nothing to evaluate, and the outcome is, in effect, "Not satisfied" by default — not because a dependency changed, but because there was never a candidate to check. This is a request-identification fact, not a resolution failure, and is distinct from the "Indeterminate" outcome in §3.

---

## 5. Architectural Guarantees

| Guarantee | Statement | Basis |
|---|---|---|
| Determinism | Given the same request and the same persisted dependency state, resolution always produces the same outcome | ARCH-025 §8 (Deterministic validity determination), carried forward unchanged |
| No side effects | Resolution never mutates persisted dependency state, never invokes `IModelProvider`, and never performs the owning engine's work | ARCH-023 §9 |
| No cross-engine inference | Resolution for one engine's artifacts never depends on another engine's internal state beyond the dependency signals that engine already exposes | ARCH-025 §7, ARCH-026 §3 (Data Ownership) |
| Fail-closed | An Indeterminate outcome (§3) is never treated as Satisfied | ARCH-026 §7, carried forward |
| Minimality | A Not-satisfied outcome is scoped to the minimum affected candidate the dependency graph supports, never broadened by category, ownership, or proximity | ARCH-025 §5 |
| No new source of truth | Resolution consults only dependency state and artifacts already owned by V1 components; it maintains no independent record of "what's valid" that could diverge from what those components know | ARCH-023 §4 non-goals |

---

## 6. Failure Behaviour

- **An incomplete resolution always yields Not-satisfied or Indeterminate, never Satisfied.** This is the same fail-closed baseline ARCH-026 §7 already established, applied at the point of decision rather than the point of storage.
- **Resolution failure never blocks the owning engine.** It only removes the option to reuse — the engine's fallback path is always full recomputation, exactly the pre-V2 baseline (ARCH-026 §7). Resolution is additive to what an engine can already do; it is never a precondition for an engine to act.
- **A failed or Indeterminate resolution for one candidate does not affect any other artifact's outcome.** This follows the same minimality guarantee as §5 — failures are scoped exactly like invalidation is; one Indeterminate result never cascades into other resolutions.

---

## 7. Relationship to Existing V1 Ownership

- Resolution is performed at the initiation of, and on behalf of, whichever engine owns the artifact type in question — continuing the exact ownership already established in ARCH-025 §7 and ARCH-026 §3. This document assigns no new ownership.
- V2's role in resolution is precisely the "Validity" boundary responsibility ARCH-023 §9 already defined. This document is that responsibility's elaboration, not a new one added to it.
- Index Engine, Connector Platform, Parser Platform, and Workspace Engine remain the sources of the dependency signals resolution consults; they do not perform resolution themselves, and their ownership of their own state is unaffected by resolution existing.
- The Domain Event Bus's role — carrying invalidation signals — is unchanged; resolution may consult signals it carries without altering what it is or does.
- No V1 engine's ownership of its own artifacts, storage, or lifecycle changes as a result of this document. Resolution is something an engine does with data it and its dependencies already own — it redistributes no responsibility.

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-023's Data Ownership principle and its "Validity" boundary responsibility (§9) without modification; ARCH-024's artifact inventory as the set of things resolution can apply to; ARCH-025's validity classes, dependency shapes, minimum-invalidation, and fail-closed principles as the reasoning resolution performs; and ARCH-026's ownership table and persisted-state requirements as what resolution reads from.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behaviour to any V1 component. It elaborates a responsibility ARCH-023 already assigned to V2's boundary, not to any V1 engine.

**Existing components intentionally unchanged.** All of them. Every ownership assignment from ARCH-025 §7 and ARCH-026 §3, every gap ARCH-024 identified, and every principle from ARCH-023 remain exactly as those documents left them.

**New concepts introduced.** One: the name and formal definition of "dependency resolution" itself, as the reasoning process that fulfils ARCH-023 §9's "Validity" responsibility. Justification: ARCH-023 established that this responsibility exists; ARCH-025 defined what validity means and ARCH-026 defined what must persist to check it; none of the three defined the act of checking at request time, or drew the line between that act and retrieval. This document names and bounds that act. It introduces no new component — resolution is performed via the same V2 boundary ARCH-023 already established, over data already owned by existing V1 components.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023 §9](ARCH-023-V2-Architectural-Boundary.md) | Parent — the "Validity" boundary responsibility this document elaborates in full |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Parent — the artifacts resolution can apply to |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Parent — the validity classes, dependency shapes, minimum-invalidation, and fail-closed principles resolution applies |
| [ARCH-026](ARCH-026-Persistence-Requirements.md) | Parent — the persisted dependency state resolution reads from, and its ownership table |
| ARCH-023 (Expected V2 Architecture Series) | This document fulfils the role that series described as "Reuse," scoped specifically to dependency resolution rather than a retrieval mechanism |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial dependency resolution architecture — fourth V2 design document, built on ARCH-023 through ARCH-026. |
| 1.1 | 2026-07-03 | Ferret Core Team | AGR-002 Amendment 2 — replaced §4's undefined assumption that "a request already knows which prior artifact... was produced for that same request" with a precise statement citing ARCH-028's equivalence relation. No other change; §3 outcomes and §5 guarantees unaffected. |
| 1.2 | 2026-07-03 | Ferret Core Team | AGR-003 Amendment 2 — added the outcome-combination rule for multi-artifact dependency chains to §3, citing ARCH-029 §6. No existing outcome definition altered. |
