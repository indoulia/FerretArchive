# ARCH-037 — Ferret V2 Dependency Graph Mechanism

| Field | Value |
|---|---|
| **Document ID** | ARCH-037 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — requires a Standard Architecture Review (`AR-`) per V2-ROADMAP-001 §7's Tier 3 governance; escalates to a new Architecture Governance Review only if a conceptual gap is discovered |
| **Date** | 2026-07-04 |
| **Last Updated** | 2026-07-04 |
| **Related ADRs** | None yet — this document makes no storage, technology, API, or format decision; see "Interaction With Future ADRs" |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-025 (Artifact Validity Model); ARCH-026 (Persistence Requirements); ARCH-027 (Dependency Resolution Architecture); ARCH-028 (Request Equivalence Architecture); ARCH-029 (Validity Propagation Architecture); ARCH-030 (Dependency Participation Semantics); ARCH-032 (Persistence Mechanism Design) — the mechanism this document consumes; ARCH-033 (Dependency Resolution Mechanism Design) — the mechanism whose existing private traversal this document generalizes into reusable infrastructure |
| **Governed By** | ARCH-031 (Mechanism Architecture Principles) |
| **Roadmap Item** | Not previously enumerated in [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md)'s Tier 3 sequence (RM-07–RM-09). Introduced under ARCH-031's "Expected Mechanism Document Sequence" allowance that "a later document may re-scope within it, provided it does not violate this document's invariants (§8) or the roadmap's dependency ordering" — this document adds no new conceptual responsibility (§4 of ARCH-031's table is unchanged) and depends only on RM-07 (ARCH-032) being complete, which it is |

---

## Purpose

ARCH-029 §6 already requires that resolution combine outcomes across a multi-artifact dependency chain, and ARCH-025 §3 already defines the shape-2 dependency — a reference to another artifact's request identity, carrying no embedded copy of that artifact's state — that makes such a chain possible. Realizing that requirement, in Sprint 2's S2-6 and S2-7 milestones, required a traversal procedure: starting at one artifact's persisted record, following each `DependencyReference` it holds, fetching the referenced record through `IDependencyStateStore`, and repeating, while detecting a reference cycle rather than recursing forever. That traversal exists today as `ResolutionCheck.CompareChainAsync` and its private `CompareLinksAsync`/`CompareLinkAsync` helpers. It exists only as an implementation detail of one specific consumer — the part of resolution that decides a `ResolutionOutcome`. It is not a reusable, independently defined structure of its own.

This document names that structure — the set of nodes and edges a traversal starting from one root request identity necessarily visits — the **Dependency Graph**, and defines the mechanism by which it is materialized. It does so because more than one future capability needs the same underlying structure without needing resolution's specific semantic decision: a future explanation of why an artifact is invalid needs to show which edge in the chain failed; a future impact analysis needs to know which nodes a change reaches; a future rebuild plan needs the order edges impose. Each is a different interpretation of the same structural facts. Absent this document, each would otherwise reimplement `ResolutionCheck.CompareChainAsync`'s traversal privately, at the risk of each producing a subtly different, undocumented notion of "the graph."

This document does not design any of those future capabilities. It defines only the structure they will each consume: what a node is, what an edge is, how the structure is built, and what invariants it must uphold, regardless of which future mechanism reasons over it. Every statement below answers **how** the already-approved concepts of a dependency reference (ARCH-025 §3) and a dependency chain (ARCH-030) are materialized into a traversable, in-memory structure. None answers **what** an artifact's validity is, or what a reference or chain *means* — those remain exactly as ARCH-025, ARCH-027, ARCH-029, and ARCH-030 already state them, and exactly as ARCH-033 already realizes them for resolution's specific purpose.

---

## Scope

Covers:
- Precise terminology distinguishing the existing, unchanged concepts (Dependency Record, Dependency Reference, Dependency Chain) from the two new structural concepts this document introduces (Dependency Graph, Graph Node, Graph Edge) (§1)
- The graph's lifecycle — on-demand, rooted, immutable, discarded, never a new source of truth (§2)
- What constitutes a graph's identity (§3)
- The deterministic materialization procedure (§4)
- Structural invariants every materialization must uphold (§5)
- How a reference cycle is represented (§6)
- How an unavailable referenced record (missing or unreadable, per ARCH-026 §7 and its S2-8 realization) is represented (§7)
- The separation of concerns between persistence, graph, resolution, and higher-layer consumers (§8)
- Explicit non-goals (§9)

Does not cover, and will not decide:
- Explainability, impact analysis, rebuild planning, or visualization of any kind — each is a future, separate architecture document that *consumes* this mechanism; none is designed here
- Any caching, indexing, or persistence of a constructed graph — §2's lifecycle invariant forecloses this at the mechanism-design level, not merely as an unaddressed detail
- Any comparison, validity, or resolution-outcome logic — that remains ARCH-027's, ARCH-029's, and ARCH-033's exclusive domain; this document's vocabulary contains no `ResolutionOutcome`, no "satisfied," no "valid"
- Any storage technology, public API shape, or in-memory representation detail (a specific class, record, or collection type) — implementation-tier, per ARCH-031 §2's layering test
- Any redefinition of a Dependency Record (ARCH-032), a Dependency Reference or Dependency Chain (ARCH-025 §3, ARCH-030), an artifact's validity (ARCH-025), a resolution outcome (ARCH-027), request equivalence (ARCH-028), or a propagation rule (ARCH-029)
- Whether, or when, `ResolutionCheck` is refactored to consume this mechanism instead of its own internal traversal — an implementation-tier migration decision left to a future milestone (§8, §9)
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every concept referenced below as already fixed is taken as-is from ARCH-025 §3 (dependency shapes), ARCH-030 (chain semantics and applicability), ARCH-028 (request identity), ARCH-026 §7 (fail-closed treatment of missing/corrupted/unreadable state), ARCH-032 (the `IDependencyStateStore` contract this mechanism consumes without modification), and ARCH-033 (`ResolutionCheck.CompareChainAsync`, the existing traversal this document generalizes). This document introduces no new artifact, validity class, dependency shape, resolution outcome, equivalence rule, propagation rule, or owning component. Where it names a new term (Dependency Graph, Graph Node, Graph Edge), that term names a structure the existing traversal already implicitly builds and discards on every call — it is extracted and formalized here, never invented.

---

## 1. Terminology

| Term | Status | Definition |
|---|---|---|
| **Dependency Record** | Existing (ARCH-032; `DependencyRecord`) | The persisted fact about one artifact's dependencies, identified by the request identity (`EngineResponsibility`, `RequestPath`) ARCH-028 defines. Unchanged by this document. |
| **Dependency Reference** | Existing (ARCH-025 §3, ARCH-030; `DependencyReference`) | A recorded pointer from one Dependency Record to another artifact's request identity — never an embedded copy of that artifact's state. Unchanged by this document. |
| **Dependency Chain** | Existing (ARCH-030; `DependencyChain`) | The ordered collection of Dependency References one Dependency Record holds, representing that artifact's shape-2 dependencies. Unchanged by this document. |
| **Dependency Graph** | New (this document) | The materialized structure produced by recursively following Dependency References outward from one root request identity: a set of Graph Nodes connected by Graph Edges, per §4's construction procedure. |
| **Graph Node** | New (this document) | One distinct request identity encountered during a single materialization, together with a materialization state — **Resolved** (the Dependency Record was read successfully and is attached to the node) or **Unavailable** (the record could not be materialized; §7) — and nothing else. |
| **Graph Edge** | New (this document) | One Dependency Reference, materialized as a directed edge from the Dependency Record that recorded it to the Graph Node representing the referenced request identity, carrying exactly one structural flag: whether following this edge closed a cycle (§6). |

A Dependency Graph is not itself a new kind of persisted fact. It is a view computed over Dependency Records, Dependency References, and Dependency Chains that already exist under ARCH-025/ARCH-030's unchanged definitions.

---

## 2. Lifecycle

> A dependency graph is a deterministically materialized, immutable, in-memory projection of dependency records already persisted in the repository. It is rooted at a request identity, reconstructed on demand for a single operation, and discarded when that operation completes. It is not persisted, cached, or treated as a new source of truth.

Every other section of this document is a corollary of this one paragraph. In particular:
- **On demand, per operation.** A consumer that needs a graph (resolution today; explainability, impact analysis, or rebuild planning tomorrow) materializes one at the moment it needs it, from whatever `IDependencyStateStore` currently returns, and discards it when that operation ends.
- **Rooted.** A graph always has exactly one root request identity. There is no whole-repository or multi-root graph in this mechanism (§9).
- **Not persisted, not cached, not a new source of truth.** No graph, or any part of one, survives past the operation that built it. Two operations that materialize a graph from the same root, one after the other, perform two independent materializations — the second never reuses the first's result, even if nothing changed in between. This is the direct realization of ARCH-031 §3's "No new source of truth" guarantee at this mechanism's tier.

---

## 3. Graph Identity

A Dependency Graph's identity is its root request identity — the same (`EngineResponsibility`, `RequestPath`) pair ARCH-028 already defines as a request identity, and nothing more:

- **Not a persisted object.** A graph has no row, file, or record of its own anywhere in the repository.
- **Not a GUID or other synthetic identifier.** No materialization allocates an identifier for the graph as a whole; only nodes and edges exist as structure, and nodes are identified by request identity (§1), never by a synthetic key.
- **Not a file.** No materialization writes any output to disk. §2 already forecloses this; this section states it as an identity property, not only a lifecycle one — two things with different identities cannot be the same graph, and a graph has no identity beyond the root it was materialized from.

Two materializations sharing the same root request identity are, by definition, materializations *of the same graph* — but because persisted state can change between them (§2), they are not guaranteed to produce identical structure across time. Within a single materialization, structure is deterministic (§5's "Deterministic construction" invariant); across separate materializations at different moments, only the root identity is guaranteed to match.

---

## 4. Materialization

Materialization begins at a root request identity and proceeds as follows, using only `IDependencyStateStore` (ARCH-032) as already defined — no new interface, storage call, or query shape is introduced:

1. Call `IDependencyStateStore.GetRecordAsync` for the root identity.
2. If no record is returned (per S2-8's classification: missing, corrupted, or otherwise unreadable), the root materializes as a single **Unavailable** Graph Node with no outgoing edges. Materialization ends; the resulting graph has exactly one node.
3. If a record is returned, the root materializes as a **Resolved** Graph Node carrying that Dependency Record.
4. For each Dependency Reference in the resolved node's Dependency Chain:
   a. If the referenced request identity has already been materialized as a Graph Node within this same construction, reuse that existing node object and materialize a Graph Edge to it, flagged as cycle-closing (§6).
   b. Otherwise, materialize a new Graph Node for that identity by repeating steps 1–4 recursively for it, and materialize a (non-cycle-closing) Graph Edge from the current node to the new one.
5. Materialization ends when every reachable identity, starting from the root and following every Dependency Reference transitively, has been visited exactly once.

This is a direct generalization of `ResolutionCheck.CompareLinkAsync`'s existing visited-set recursion (ARCH-033), decoupled from producing a `ResolutionOutcome`: it reads through the same, unmodified `IDependencyStateStore` contract, and it produces a graph object instead of an outcome value. It introduces no new read path, no new store method, and no new query the store does not already support.

---

## 5. Structural Invariants

A materialization that does not satisfy every invariant below has not correctly realized this mechanism, regardless of how it is implemented:

| Invariant | Statement |
|---|---|
| **Deterministic construction** | Given the same root request identity and the same persisted state at the moment of construction, materialization always produces a structurally identical graph — the same nodes, the same edges, the same materialization states, the same cycle flags. |
| **Immutable graph** | Once materialized, a Dependency Graph and every Graph Node and Graph Edge within it never change. A later change to persisted state requires a fresh materialization (§2); it never mutates an existing graph object. |
| **No duplicate nodes for the same request identity** | Within one materialization, each distinct request identity ever encountered corresponds to exactly one Graph Node object. Every edge that points to that identity points to the same node object. This is what makes cycle detection (§6) possible at all — a "cycle" is precisely a second edge arriving at a node that already exists. |
| **Repository remains the only source of truth** | The graph is never consulted as a substitute for a fresh `IDependencyStateStore` read, and its existence never implies that persisted state matches what the graph recorded at construction time. It is a snapshot of one moment, never an authority extending past that moment. |
| **No derived semantic state** | A Graph Node or Graph Edge carries no validity label, no resolution outcome, no "is this dependency stale" answer, and no judgment of any kind. It carries only: an identity, a materialization state (Resolved/Unavailable), and, for edges, whether following it closed a cycle. Every other question about what the structure *means* belongs to a consumer (§8), never to this mechanism. |

---

## 6. Cycle Handling

A cycle — a Dependency Reference chain that returns to a request identity already visited during the same materialization — is a **structural fact**, not a semantic one. Materialization:

- **Represents the back-edge.** The Graph Edge that closes the cycle is materialized like any other edge, pointing to the already-existing Graph Node (§5's no-duplicate-nodes invariant is what makes "pointing back" meaningful rather than creating an infinite copy).
- **Never omits it.** A cycle-closing edge is exactly as present in the graph as any other edge. Silently dropping it would violate §5's determinism invariant (a materialization that sometimes includes and sometimes omits the same structural fact is not deterministic) and would misrepresent what the repository actually records.
- **Never fails because of it.** Encountering a cycle does not raise an error, abort materialization, or produce a partial graph. Materialization completes normally; the cycle is simply part of the completed structure.
- **Never assigns semantic meaning to it.** The graph states only that "this edge closes a cycle." It does not state whether that cycle is valid, invalid, a bug, or expected. `ResolutionCheck` today independently treats a reference cycle as fail-closed Indeterminate (ARCH-033) — that is a resolution-layer interpretation of the same structural fact this mechanism exposes, not a fact this mechanism itself asserts. A future explainability consumer might render the same fact as prose ("this dependency chain is cyclic"); a future impact-analysis or rebuild-planning consumer must simply traverse the structure correctly in its presence. All three interpretations are downstream of, and consistent with, the same one structural fact.

The graph exposes the cycle-closing flag on the edge for exactly this reason: every future consumer would otherwise have to independently re-detect the same cycle by maintaining its own visited set, duplicating work materialization already performed.

---

## 7. Unavailable Dependencies

Materialization is **lossless**: every Dependency Reference encountered is represented in the resulting graph, whether or not the record it points to could actually be read.

If a referenced Dependency Record cannot be materialized — because `IDependencyStateStore.GetRecordAsync` returns null, whether due to a genuinely absent record or S2-8's fail-closed classification of corrupted or otherwise unreadable content — materialization:

- **Does not silently omit the reference.** The Dependency Reference that pointed to it is preserved as a Graph Edge, exactly as it would be for a successfully materialized target.
- **Creates an explicit Unavailable Graph Node** for the referenced identity, rather than treating the reference as though it did not exist.
- **Assigns no semantic outcome.** The node states only that this identity's record could not be materialized. It does not state, imply, or encode `Indeterminate`, `NotSatisfied`, `Satisfied`, "missing," or any other resolution- or narrower-cause-specific vocabulary. `IDependencyStateStore` (ARCH-032, S2-8) already declines to distinguish "missing" from "corrupted" from "unreadable" at its own interface boundary — a Graph Node's Unavailable state faithfully carries that same boundary forward rather than inventing a finer distinction the persistence layer does not expose.

Omitting an unavailable target would understate what the repository actually records: a Dependency Record that references a target the graph silently drops would appear, incorrectly, to have no further dependency at all. The explicit Unavailable node preserves the true shape of what was recorded, even where its content could not be read.

---

## 8. Separation of Concerns

This mechanism completes a four-layer model, each layer adding exactly one kind of meaning to the layer below it:

| Layer | Owns | Realized by |
|---|---|---|
| **Repository** | Persistence — durable dependency facts, read and written | ARCH-032; `IDependencyStateStore`, `FileDependencyStateStore` |
| **Graph** | Structure — which nodes exist, which edges connect them, which are cyclic, which are unavailable | This document |
| **Resolution** | Semantic evaluation — validity, `ResolutionOutcome`, combination rules | ARCH-027, ARCH-029, ARCH-033; `ResolutionCheck` |
| **Higher layers** | Interpretation for a purpose — explanation, impact, rebuild ordering, presentation | Future, separate documents; not designed here |

Higher layers consume graph structure rather than reconstructing traversal independently. This is the practical benefit this document exists to provide: once a Dependency Graph is materialized, a consumer reasons over its nodes and edges directly — it does not need its own copy of §4's recursive traversal, its own visited-set, or its own decision about how to represent a cycle or an unavailable reference. Each layer trusts the layer below it for exactly the kind of fact that layer owns, and adds nothing else.

---

## 9. Non-Goals

This document explicitly does not define, and no future implementation of it may smuggle in under the guise of "necessary detail" (ARCH-031 §9):

- **Graph persistence.** No materialized graph is ever written to disk, a database, or any durable store.
- **Graph caching.** No materialized graph, or part of one, is reused across operations, requests, or process lifetimes.
- **Graph optimization.** No specific data structure, algorithm complexity target, or performance characteristic is mandated beyond determinism (§5) and losslessness (§6, §7).
- **Rebuild planning.** No ordering, scheduling, or recomputation-sequencing capability is defined here. A future document may build one atop this mechanism's structure.
- **Explainability.** No human-readable explanation, summary, or narrative over a graph's structure is defined here.
- **Impact analysis.** No reverse-traversal ("what depends on this identity") capability is defined here. This mechanism is forward-only, rooted at one identity (§2, §3); a reverse-traversal capability would require its own architectural treatment of what state, if any, makes reverse lookup possible, which this document does not decide.
- **Visualization.** No rendering, diagramming, or presentation format is defined here.
- **Comparison or resolution logic of any kind.** No node or edge carries a validity, satisfaction, or staleness judgment (§5, §6, §7). That vocabulary belongs exclusively to ARCH-027, ARCH-029, and ARCH-033.
- **Whether or when `ResolutionCheck` migrates onto this mechanism.** `ResolutionCheck.CompareChainAsync`'s existing private traversal (ARCH-033) is not required to change as a result of this document. Whether a future milestone refactors it to consume a materialized Dependency Graph instead of its own recursion is an implementation-tier decision this document neither mandates nor forbids (see "Interaction With ARCH-033," below).
- **Mutation.** No API implied by this mechanism ever modifies a Dependency Record, a Dependency Reference, or a Dependency Chain. Materialization is read-only in every respect.

---

## Conformance With ARCH-031

### Guarantee-by-guarantee trace (ARCH-031 §3)

| Guarantee | How this mechanism preserves it |
|---|---|
| Core V2 Principle (reuse valid artifacts, recompute only the minimum invalidated) | The graph makes no reuse or recomputation decision (§5, §9) — it only materializes structure a consumer uses to make that decision. A mechanism that decides nothing about validity cannot violate a validity-scoped principle. |
| Dependency stability over output reproducibility | Nodes and edges are built solely from recorded Dependency References and identities (§4) — materialization never inspects an artifact's output. |
| Deterministic evaluation | §5's "Deterministic construction" invariant states this directly: same root, same persisted state at construction time ⇒ structurally identical graph. |
| Fail-closed | §7 (Unavailable Dependencies) ensures a materialization failure is preserved as an explicit Unavailable node and edge, never silently treated as "no dependency" or omitted — no consumer can mistake absence-of-information for absence-of-dependency. |
| Minimum-invalidation | The graph asserts no invalidation scope at all (§5's "no derived semantic state"); by computing zero semantic state, it cannot broaden or narrow an invalidation decision, which remains entirely resolution's responsibility. |
| No cross-engine inference / Data Ownership | Materialization reads exclusively through the already-approved `IDependencyStateStore.GetRecordAsync` (§4), exactly as `ResolutionCheck.CompareChainAsync` already does, and follows only references the owning engine itself recorded — it never reaches into a V1 engine's internals beyond that existing interface. |
| Resolution is not retrieval | This mechanism is retrieval/materialization only; its vocabulary contains no `ResolutionOutcome`, "satisfied," or "valid" anywhere (§1, §5) — the distinction ARCH-027 §1 requires is preserved by construction, not by convention. |
| Exact request equivalence | Graph Node identity is exactly the (`EngineResponsibility`, `RequestPath`) pair ARCH-028 already defines (§1) — no alternate or fuzzy identity notion is introduced. |
| Transitive, point-in-time propagation consistency | Because a graph is reconstructed fresh per operation and never cached (§2), any consumer reasoning over it is reasoning over one consistent snapshot taken at one moment — never a stale mixture of state from different points in time. |
| Deletion is unconditional and irreversible per dependency instance | The graph makes no deletion determination; it reflects only whatever `IDependencyStateStore` currently returns at construction time (§4), so it cannot contradict or reinterpret ARCH-030's deletion semantics, which remain resolution's and the owning engine's concern. |
| No new source of truth | §2's lifecycle invariant states this directly: never persisted, never cached, discarded after use. |
| The eight approved component names are the only owning-component vocabulary | This mechanism introduces no new owning component. Like `IDependencyStateStore` and `ResolutionCheck` before it, it is V2 cross-cutting mechanism infrastructure operating only across identities and references the eight already-approved components themselves recorded — never a ninth engine. |

### Responsibility trace (ARCH-031 §4)

ARCH-031 §4's table does not list "graph materialization" as one of the four responsibilities the frozen kernel deliberately left open (Dependency State, Request Equivalence, Artifact Validity, Dependency Resolution) — because no conceptual document anticipated naming this structure explicitly. This document does not add a fifth open responsibility to that table. It observes, instead, that ARCH-029 §6 already requires resolution to combine outcomes across a dependency chain, and that doing so correctly already presupposes *some* deterministic way to enumerate the chain's nodes, edges, and cycles — a structural prerequisite ARCH-033's Dependency Resolution responsibility (row 4) already had to solve privately in order to realize ARCH-029 §6 at all. This document extracts that already-necessary structural prerequisite into standalone, reusable form. It changes nothing about what a Dependency Reference, a Dependency Chain, or a `ResolutionOutcome` *means* (the "fixed" columns of ARCH-031 §4 remain untouched); it only gives the "how" that Dependency Resolution's row already required a name, a definition, and invariants of its own, so that future consumers besides resolution can rely on the same structure rather than re-deriving it.

### Ownership trace (ARCH-031 §7 item 3)

This mechanism introduces no new owning component. It is not assigned to, and does not create, a ninth entry beside the eight approved component names (AGR-001 §6.2). Like `IDependencyStateStore` and `ResolutionCheck`, it is V2's own cross-cutting mechanism infrastructure — it owns no domain data of any of the eight engines, and it materializes structure solely from records those engines already caused to be persisted through the existing, unmodified persistence mechanism (ARCH-032).

### Explicit non-goals

§9, above, and Scope's "Does not cover."

### Statement of ADRs produced

None produced by this document, and none anticipated. This document specifies no storage technology, wire format, persisted structure, or public API shape — it names no consequential, hard-to-reverse choice under ARCH-031 §6's test. Should a future implementation of this mechanism introduce such a choice (for example, a specific in-memory representation exposed across an assembly boundary), that choice would produce its own ADR at implementation time, cross-referenced from this document by amendment.

### Confirmation no Closed Architectural Decision is contradicted

See Impact on Existing Architecture, below.

---

## Interaction With ARCH-033 (Dependency Resolution Mechanism Design)

ARCH-033 already contains a traversal procedure for shape-2 dependency chains, realized in code as `ResolutionCheck.CompareChainAsync`/`CompareLinksAsync`/`CompareLinkAsync`. This document:

- Does not amend, redefine, or narrow ARCH-033's chain-comparison procedure, its cycle-handling behavior (fail-closed Indeterminate), or the `ResolutionOutcome` vocabulary in any way.
- Does not require `ResolutionCheck`'s existing implementation to change. `ResolutionCheck.CompareChainAsync` remains, as of this document, a self-contained, already-correct realization of ARCH-033 and ARCH-029 §6.
- Formalizes, as a named, independently defined structure, the traversal `ResolutionCheck` already performs privately — so that a future consumer other than resolution (explainability, impact analysis, rebuild planning) can rely on the same structure without depending on, or reimplementing, `ResolutionCheck` internals.
- Explicitly leaves open, as a Non-Goal (§9), whether a future milestone migrates `ResolutionCheck` to consume a materialized Dependency Graph instead of its own internal recursion. Were that migration to happen, ARCH-033's own guarantee-by-guarantee trace would need to reconfirm that the substitution preserves every invariant it already establishes (fail-closed cycle handling, exact equivalence, transitive point-in-time consistency) — that confirmation belongs to whatever document or milestone proposes the migration, not to this one.

---

## Interaction With Future ADRs

None are anticipated at this time (Conformance With ARCH-031, above). If a future implementation surfaces a consequential, hard-to-reverse choice this document did not anticipate, it will be recorded as an ADR and cross-referenced here by amendment, following the same pattern ARCH-032 and ARCH-033 already established for their own future ADRs.

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-025 §3's dependency shapes and ARCH-030's chain semantics and applicability matrix; ARCH-028's request-identity properties as Graph Node identity; ARCH-026 §7's fail-closed treatment of missing/corrupted/unreadable state as the basis for Graph Node unavailability (§8); ARCH-032's `IDependencyStateStore` contract as the exclusive read path materialization uses; and ARCH-033's existing `ResolutionCheck.CompareChainAsync` traversal as the concrete evidence that this structure already implicitly exists — all without modification.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behavior to any V1 component, and no new method to `IDependencyStateStore`. It defines a new mechanism-tier structure that consumes existing interfaces exactly as they already stand.

**Existing components intentionally unchanged.** All of them. `DependencyRecord`, `DependencyReference`, `DependencyChain`, `IDependencyStateStore`, `ResolutionCheck`, and `ResolutionOutcome` remain exactly as ARCH-025, ARCH-030, ARCH-032, and ARCH-033 already established them.

**New concepts introduced.** Two, both at the mechanism tier, neither at the conceptual tier: the Dependency Graph as a named, independently defined structure (§1–§5), and its two structural annotations — cycle-closing edges (§6) and Unavailable nodes (§7). Both are direct corollaries of already-existing conceptual rules (ARCH-025 §3's dependency-reference shape; ARCH-026 §7's fail-closed principle) rather than independent decisions about what those rules mean.

**Closed Architectural Decisions.** All nine (AGR-001 §6) checked individually against this document's text; none is contradicted, narrowed, or reinterpreted. In particular, Closed Decision 6 (validity is determined by dependency stability, never output reproducibility) and Closed Decision 7 (minimum-invalidation and fail-closed govern both validity and persistence failure) are preserved exactly as stated — this document makes no validity determination of any kind, and its treatment of unavailable and cyclic structure is strictly more conservative (never less) than silently omitting them would be.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Data Ownership principle and the eight approved component names this document confirms it introduces no ninth of (Ownership trace) |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Source of the shape-2 dependency-reference concept this document materializes as a Graph Edge |
| [ARCH-026](ARCH-026-Persistence-Requirements.md) | Source of the fail-closed treatment of missing/corrupted/unreadable state this document carries forward as Graph Node unavailability (§7) |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Source of the "resolution is not retrieval" distinction this document preserves by containing no resolution vocabulary |
| [ARCH-028](ARCH-028-Request-Equivalence-Architecture.md) | Source of the request-identity properties this document adopts unchanged as Graph Node identity |
| [ARCH-029](ARCH-029-Validity-Propagation-Architecture.md) | Source of the transitive-chain-combination requirement whose structural prerequisite this document formalizes |
| [ARCH-030](ARCH-030-Dependency-Participation-Semantics.md) | Source of the Dependency Chain and Dependency Reference definitions this document leaves unmodified |
| [ARCH-031](ARCH-031-Mechanism-Architecture-Principles.md) | Governing document — the evidentiary standard and invariant checklist this document is written to satisfy |
| [ARCH-032](ARCH-032-Persistence-Mechanism-Design.md) | The mechanism (`IDependencyStateStore`) this document's materialization procedure exclusively reads through |
| [ARCH-033](ARCH-033-Dependency-Resolution-Mechanism-Design.md) | The mechanism (`ResolutionCheck.CompareChainAsync`) whose existing private traversal this document generalizes into reusable, independently defined infrastructure (Interaction With ARCH-033, above) |
| [AGR-001](../Reviews/AGR-001.md) | Source of the Closed Architectural Decisions confirmed unaffected (Impact on Existing Architecture, above) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | Sequences the Tier 3 mechanism documents this one extends beyond the originally anticipated RM-07–RM-09 set, per ARCH-031's explicit allowance for later re-scoping |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-04 | Ferret Core Team | Initial draft — defines the Dependency Graph Mechanism (Graph Node, Graph Edge, materialization, structural invariants, cycle handling, unavailable-dependency handling, separation of concerns) as reusable infrastructure generalized from `ResolutionCheck.CompareChainAsync`'s existing private traversal, per ARCH-031's evidentiary and invariant standard. Pending Standard Architecture Review. |
