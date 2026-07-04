# ARCH-023 — Ferret V2 Architectural Boundary

| Field | Value |
|---|---|
| **Document ID** | ARCH-023 |
| **Version** | 1.3 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-001) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines boundaries only; no mechanism is specified yet that would warrant an ADR |
| **Related Spec** | None yet — this document precedes PRD-level V2 requirements |
| **Parent Architecture** | ARCH-001 (System-Level) — V2 is a platform-wide extension, not a single component |

---

## Purpose

This document establishes the architectural contract for Ferret V2: what V2 is responsible for, what it is explicitly not responsible for, and how it relates to the V1 architecture defined in ARCH-001. It is the foundation document that every subsequent V2 design document (cache/storage design, API design, benchmark design, and similar) must reference rather than redefine.

This document does not design any mechanism. It does not specify a cache architecture, database schema, storage engine, API surface, parser implementation, index implementation, CLI integration, or benchmark. Those belong to later, component-level documents written against the boundary this document establishes.

The core V2 architectural principle, which every subsequent V2 document must uphold, is:

> **Reuse every valid artifact already produced by Ferret. Recompute only the minimum invalidated portion.**

---

## Success Criteria

V2 has achieved its architectural objective when the following are true of the platform. These are architectural outcomes, not implementation metrics — no throughput, latency, or storage-size target belongs in this document or is implied by it (see Non-Goals, §4).

- Every V1 engine that owns AI invocation can determine, using only signals V1 already exposes, whether a prior AI-derived artifact remains valid — without recomputing it to find out.
- When a change invalidates part of the knowledge state, recomputation is bounded to the minimum invalidated portion of the affected AI-derived artifacts, never the whole.
- No V1 engine's ownership, lifecycle, or storage boundary (ARCH-001 §7.3) has changed as a result of V2's introduction.
- Every architectural goal in ARCH-001 §2 (AG-001–AG-010) remains fully satisfied with V2 present.
- Every subsequent V2 design document can be written by citing this document's scope, dependencies, and principles, without restating or renegotiating them.

---

## Scope

Covers:
- Why V2 exists and the problems it solves
- The architectural outcomes that indicate V2 has achieved its objective
- A formal definition of "AI-derived artifact," used consistently throughout this document
- The boundary of V2's responsibility, stated in terms of Ferret's existing V1 components
- The architectural principles that govern all future V2 design decisions
- The relationship between V1 and V2, including dependency direction and ownership
- The exact set of repository components V2 depends on
- How V2 is positioned relative to the domain architecture in ARCH-001 §30, including a conceptual boundary diagram
- How V2 interacts with AI systems without replacing the components that own AI invocation today
- What remains completely unchanged from V1
- The expected sequence of future V2 design documents

Does not cover:
- Any concrete mechanism, schema, storage technology, or API — see Non-Goals (§4)
- Component-level internals of any V1 engine — see ARCH-003 through ARCH-022 as they are written
- Requirement-level traceability (FR-XXX / NFR-XXX) — no PRD-001 requirements for V2 exist yet; future V2 design documents must establish this traceability once concrete capabilities are scoped

---

## Definitions

This document defines exactly one term. Every other term used here carries the meaning already established in GLOSSARY-001.

**AI-derived artifact** — Any output produced, in whole or in part, by an invocation of `IModelProvider` through the V1 engine that owns that invocation. This includes, without limitation, a Review Engine finding, a Knowledge Engine context assembly, and the provenance metadata Artifact Engine attaches to such output. An AI-derived artifact is always owned by the V1 engine that produced it — V2 never owns one (see the Data Ownership principle, §5).

This term is used consistently throughout the rest of this document in place of looser phrasing such as "AI-derived knowledge," "AI-derived output," or "AI-derived work."

---

## V1 Component Mapping

This mapping was produced by reading ARCH-001 (§2, §3, §7, §30), GLOSSARY-001, and ARCH-019, and confirming against the current `src/` layout. It supersedes any prior informal terminology and is the vocabulary this document uses throughout.

| V1 Component | Responsibility (per ARCH-001 / GLOSSARY-001) | Participates in V2 | V2 Relationship |
|---|---|---|---|
| **Workspace Engine** | Repository lifecycle, `.ferret/workspace.json` / `.ferret/state.json`, health reporting, upgrade management (ARCH-001 §7.2, §30.2) | Yes | Unchanged — V2 reads workspace state; ownership stays with Workspace Engine |
| **Connector Platform** (`Ferret.ConnectorPlatform`, `Ferret.Connectors.*`) | Discovers assets to be indexed; owns `IConnector` / `IConnectorSession` (ARCH-019) | Yes | Unchanged — V2 does not alter discovery |
| **Parser Platform** (`Ferret.ParserPlatform`, `Ferret.Parsers.*`) | Extracts structured content from discovered assets via `IParser` (GLOSSARY-001: Parser) | Yes | Unchanged — V2 does not alter parsing |
| **Index Engine** | Builds and maintains the Index; content-hash-based incremental change detection; index manifest ownership (ARCH-001 §7.2, GLOSSARY-001: Index, Indexer) | Yes | Unchanged — V2 extends the same incremental discipline to AI-derived computation, it does not modify indexing itself |
| **Knowledge Engine** | Query interface over the Index; context assembly within a token budget; relevance scoring; sensitive-file exclusion (ARCH-001 §7.2) | Yes | Unchanged — Knowledge Engine remains the sole owner of context assembly; V2's boundary is limited to evaluating whether a prior AI-derived artifact already satisfies a request |
| **Review Engine** | Review document lifecycle; AI-assisted finding generation; finding lifecycle (Proposed → Accepted → Resolved → Rejected → Deferred); the human-review gate (ARCH-001 §7.2, AG-009) | Yes | Unchanged — Review Engine remains the sole owner of finding generation and the review gate; V2's boundary is limited to evaluating whether a prior AI-derived artifact remains valid for reuse, and never substitutes for the review gate |
| **Artifact Engine** | Provenance metadata assignment (interaction ID, model ID, knowledge state hash, timestamp); traceability; the audit log (ARCH-001 §7.2, §7.3, GLOSSARY-001: Traceability) | Yes | Unchanged — V2 relies on Artifact Engine's existing provenance model for AI-derived artifacts rather than introducing a parallel one |
| **Domain Event Bus** | Cross-engine notification of state changes (`WorkspaceInitialized`, `IndexUpdated`, `SpecificationApproved`, `ReviewCompleted`, etc.); catalogued in ARCH-013 | Yes | Unchanged — V2 consumes existing events to detect invalidation; it does not add new event categories in this document |

Four terms used in earlier discussion of this initiative — *Recommendation Lifecycle*, *Trust Lifecycle*, *Historical Ledger*, and *Notification Lifecycle* as named subsystems, and *Product Operational State Model* — do not exist in Ferret's architecture and are not used below. Where those discussions pointed at a real need, the need is met by an existing component in the table above (respectively: Review Engine's finding lifecycle, Artifact Engine's traceability model, Artifact Engine's audit log, the Domain Event Bus, and the Workspace Engine).

---

## 1. Why V2 Exists

ARCH-001 §2 establishes ten architectural goals for the V1 platform. Three of them are fully realised only for the Index Engine today:

- **AG-004 (Deterministic Behaviour)** — for a given knowledge state, the platform produces identical outputs.
- **AG-005 (Incremental at Every Layer)** — nothing should require full re-processing as the repository grows.
- **AG-006 (Repository-Local State)** — all state that matters is stored in the repository, not in an external service. (ARCH-001 §2 names the path as `.ai/`; ARCH-024 §Critical Findings confirmed the real persisted-state root is `.ferret/` — the principle is unaffected, only the path.)

The Index Engine already applies all three: it tracks content hashes and processes only changed files (ARCH-001 §7.2, Index Engine). This discipline stops at the boundary between indexing and AI-driven computation. Once an engine invokes an `IModelProvider` — Review Engine generating a finding, Knowledge Engine assembling context for an interaction — there is no equivalent mechanism to determine whether that computation, given an unchanged knowledge state, has already been performed and remains valid.

V2 exists to extend the incremental, deterministic, repository-local discipline that V1 already applies to indexing, to the AI-driven computation layered on top of it.

---

## 2. Problems V2 Solves

1. **Redundant AI computation.** An engine re-invokes `IModelProvider` for inputs that are identical, in content-hash or knowledge-state-hash terms, to a previous invocation — even when nothing relevant has changed.
2. **No validity record for AI-derived artifacts.** Review Engine findings, Knowledge Engine context assemblies, and Artifact Engine provenance records have no standing answer to "is this still valid, given the current knowledge state?" Each engine can only produce new output, not check for reusable prior output.
3. **Full-scope recomputation on partial change.** Outside the Index Engine's own file-level hashing, there is no shared way to recompute only the minimum invalidated portion of the AI-derived artifacts affected by a change — engines have no basis for anything narrower than full recomputation.
4. **Fragmented reuse.** A valid AI-derived artifact is not treated as a durable extension of the Knowledge model (GLOSSARY-001: Knowledge). It remains local to the engine invocation that produced it rather than being available as input to later invocations or other engines.

---

## 3. Scope of V2

V2 is responsible for defining and enabling:

- The conditions under which a previously produced, AI-derived artifact — from Review Engine, Knowledge Engine, or Artifact Engine — is still valid, given the current knowledge state.
- The principle of minimum-invalidation recomputation for AI-derived artifacts (§5), extending the pattern the Index Engine already applies at the file level to the computation layered above it.
- The way V1 engines consult and record computation validity through their own existing extension points and the Domain Event Bus — without V2 owning or duplicating any engine's responsibilities.
- The boundary contract that subsequent V2 design documents (mechanism, storage, API, benchmarks) must build against.

---

## 4. Explicit Non-Goals

V2, at this phase and within this document, does **not** define:

- Cache architecture, database schema, or storage engine
- APIs of any kind
- Parser or index implementation changes
- CLI integrations
- Benchmarks

V2, as an architectural boundary, does **not**:

- Redesign, replace, or take over ownership of any V1 engine or the storage area it owns (ARCH-001 §7.3 ownership rule)
- Introduce a new source of truth for Knowledge — the Repository remains the sole source of truth (GLOSSARY-001: Repository)
- Bypass or weaken the human-review gate — reuse of a valid prior artifact never substitutes for a required review disposition (AG-009)
- Invoke `IModelProvider` itself, or act as a second orchestrator of AI computation
- Introduce outbound network dependencies (AG-010 remains binding)
- Define trust scoring, notification delivery, or a ledger/storage mechanism distinct from what Artifact Engine already provides

---

## 5. Architectural Principles

These principles bind every future V2 design document. They restate and extend PRINCIPLES-001 and ARCH-001 §3; they do not introduce new engineering principles outside that lineage.

| Principle | Statement | Basis |
|---|---|---|
| **Core V2 Principle** | Reuse every valid artifact already produced by Ferret. Recompute only the minimum invalidated portion | Extends AG-005 (Incremental at Every Layer) from file-level indexing to every artifact V1 already produces |
| **Data Ownership** | V2 owns no primary business data. Workspace Engine, Knowledge Engine, Review Engine, Artifact Engine, and every other V1 component described in ARCH-001 §7 remain the sole, authoritative owners of their respective data. V2 may read these as signals; it never becomes an alternate or second source of truth for any of them | ARCH-001 §7.3 |
| Never invent when something already exists | Before an engine produces a new AI-derived artifact, it must be possible to determine whether a valid one already exists in the Knowledge model | Extends GLOSSARY-001: Knowledge |
| Persistent AI Computation, not caching for its own sake | A valid AI-derived artifact is treated as a durable, attributable extension of the Knowledge model — not a transient optimisation that can be silently discarded | GLOSSARY-001: Knowledge, Traceability |
| Deterministic validity | Given an identical knowledge state, whether a prior artifact is valid is computed deterministically — never inferred by the AI model itself | AG-004 (Deterministic Behaviour) |
| Repository-local state | Any record of computation validity is repository-local, like all other platform state | AG-006 (Repository-Local State) |
| Dependency inversion, unchanged | V2 depends on the interfaces V1 already exposes; it introduces no hard dependency on a specific engine implementation, model provider, or storage technology | AG-002 (Dependency Inversion Throughout) |
| Ownership is not transferred | Each engine remains the sole writer of its own storage area (ARCH-001 §7.3); V2 consults and is consulted, it does not write to the Index, review documents, or artifact metadata directly | ARCH-001 §7.3 |

---

## 6. Relationship Between V1 and V2

V2 is strictly additive:

- V2 depends on V1. V1 does not require V2 to function — where an engine chooses to call into V2's boundary (§9), that call is optional and non-blocking: V2's absence, or a failed resolution, never prevents the engine from proceeding via its pre-V2 baseline behaviour (recomputation). This mirrors `Ferret.Core`'s zero-dependency rule (ARCH-001 §7) and the dependency-inversion goal (AG-002) in spirit — V1 has no *required* dependency on V2 — without asserting that no V1 code ever calls into V2.
- V2 does not modify the responsibilities, lifecycle states, or storage ownership of any V1 engine described in ARCH-001 §7.2 and §30.2.
- V1 engines consult V2 through their existing extension points — Review Engine's context builders, Knowledge Engine's context formatters, Specification Engine's lifecycle hooks (ARCH-001 §7.2) — and through events already catalogued in ARCH-013. V2 does not require new hard dependencies to be added to any V1 engine.
- Where V1 defines an engine's lifecycle — the Review Engine's finding lifecycle, the Specification Engine's Draft → Approved gate — V2 has no authority to alter, skip, or shorten those states.

---

## 7. V2 Dependencies — Repository Components

V2 depends on exactly the following existing repository components. It introduces no component outside this list, and it depends on nothing that does not already exist in V1.

| Component | Why V2 Depends On It |
|---|---|
| **Workspace Engine** | Source of the repository's operational state (`.ferret/workspace.json`, `.ferret/state.json`) against which validity is evaluated |
| **Connector Platform** | Source of the discovered-asset set that ultimately bounds what the Index Engine, and therefore V2, can reason about |
| **Parser Platform** | Source of the structured content the Index Engine derives the content hashes V2 relies on from |
| **Index Engine** | Source of content hashes and index-manifest state — the primary signal V2 uses to determine whether a prior artifact is still valid |
| **Knowledge Engine** | Owner of context assembly and of the knowledge state hash that V2 evaluates AI-derived artifact validity against |
| **Review Engine** | Owner of finding generation and the finding lifecycle; V2's boundary is limited to evaluating whether a prior AI-derived artifact remains valid for reuse |
| **Artifact Engine** | Owner of the provenance and audit-log model V2 relies on to attribute and account for reused AI-derived artifacts, rather than introducing a parallel one |
| **Domain Event Bus** | Source of the change signals (`IndexUpdated`, `SpecificationApproved`, `ReviewCompleted`, etc.) V2 consumes to detect invalidation |

No other repository component is a V2 dependency. Any future document that introduces a dependency outside this list must amend this document first.

---

## 8. High-Level Architectural Positioning

ARCH-001 §30.2 groups V1 into six domains: Workspace, Knowledge, Memory, Specification, Plugin, and Infrastructure. V2 is not a seventh domain that owns new business logic or new lifecycle states.

V2 is a cross-cutting capability whose boundary sits alongside, not inside, two of these domains:

- The **Knowledge Domain** (Knowledge Engine, Index Engine) — V2's boundary concerns the validity of AI-derived artifacts that Knowledge Engine's context assembly would otherwise reproduce
- The **Specification Domain** (Review Engine, Artifact Engine) — V2's boundary concerns the validity of AI-derived artifacts that Review Engine's finding generation and Artifact Engine's provenance recording would otherwise reproduce

Concrete placement — a new module, a capability embedded in `Ferret.Runtime`, or a library consumed by existing engines — is a later design decision, out of scope here (§4). Whatever form it takes, it must preserve the dependency rules already established in ARCH-001 §8 and §30.3 — for example, the Knowledge Domain's existing rule that it must not depend on the Memory Domain directly continues to apply unchanged.

The diagram below is conceptual only — it shows where V2's boundary sits relative to V1, not a module structure, call sequence, or data flow:

```text
========================= Ferret V1 (unchanged) ==========================

  Connector Platform -> Parser Platform -> Index Engine
                                                |
                                                v
  Workspace Engine          Domain Event Bus          Knowledge Engine
                                                                |
                                                                v
                                  Review Engine ---> Artifact Engine

============================================================================
                                    ^
                                    |  validity questions only —
                                    |  no writes, no ownership,
                                    |  no AI invocation
                                    |
========================= Ferret V2 (this boundary) =======================
                boundary only — owns no primary business data
============================================================================
```

Every component inside the V1 line is an existing component from the V1 Component Mapping; V2 introduces no component of its own above that line, and the arrow crossing it runs in one direction only: a validity question, never a write.

---

## 9. How V2 Interacts With AI Systems Without Replacing Them

`IModelProvider` remains the sole interface through which any engine invokes an AI model (AG-002; PRINCIPLES-001 §1, AI Agnostic). V2 does not call `IModelProvider`, and does not wrap or proxy it.

V2's role is limited to two boundary responsibilities relative to any engine that owns AI invocation. Neither involves V2 invoking `IModelProvider` or performing that engine's work:

- **Validity.** Determining whether a valid AI-derived artifact already satisfies a request an engine would otherwise fulfil by invoking `IModelProvider`. The decision whether to invoke `IModelProvider`, and the invocation itself, remains entirely owned by that engine.
- **Eligibility.** Making a newly produced AI-derived artifact eligible for future reuse once the owning engine has produced it. The mechanism by which this happens is explicitly deferred (§4).

This preserves AG-003 (Plugin Isolation) and the Provider definition in GLOSSARY-001: a Model Provider remains a plugin supplying an external capability, and V2 introduces no new coupling between any engine and a specific model provider.

---

## 10. What Remains Completely Unchanged From V1

- **Workspace Engine** — ownership of `.ferret/workspace.json`, `.ferret/state.json`, health reporting, upgrade management
- **Connector Platform** — asset discovery, connector session lifecycle
- **Parser Platform** — file-type parsing, content and symbol extraction
- **Index Engine** — incremental, content-hash-based index maintenance and manifest ownership
- **Knowledge Engine** — query interface, context assembly, relevance scoring, sensitive-file exclusion
- **Review Engine** — review document lifecycle, finding lifecycle, the human-review gate (AG-009)
- **Artifact Engine** — provenance metadata, traceability, the audit log
- **Specification Engine** — specification lifecycle and its approval gate
- **Domain Event Bus** — all event schemas and publisher/consumer relationships already catalogued in ARCH-013
- **Plugin architecture and Core contracts** — `IModelProvider`, `IParser`, `IKnowledgeStore`, `IReviewPublisher`, `IWorkItemPublisher`
- **All architectural goals and principles** — AG-001 through AG-010 (ARCH-001 §2) and the principles in ARCH-001 §3 apply to V2 with no exceptions

---

## Expected V2 Architecture Series

This document is the parent every subsequent V2 document must cite. The following sequence is expected to follow, each scoped to a single concern within the boundary this document establishes. This section establishes the roadmap only — it does not define the content of any of these documents.

1. **Artifact Taxonomy** — classifies the kinds of AI-derived artifact V2 must reason about, building on the definition in this document.
2. **Computation Validity** — defines how the validity of an AI-derived artifact is determined against a knowledge state.
3. **Persistence** — defines where and how a valid AI-derived artifact is retained, consistent with AG-006 (Repository-Local State).
4. **Reuse** — defines how a V1 engine retrieves and applies a valid AI-derived artifact in place of recomputing it.
5. **AI Integration** — defines the concrete contract between V2 and the engines that own `IModelProvider` invocation, within the boundary set by §9 of this document.
6. **Benchmarking** — defines how the Core V2 Principle is measured and verified.

This sequence is expected, not binding. A later document may re-scope or reorder these concerns as it is written, provided it does not violate the boundary established here.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-001 §2](ARCH-001.md) | Architectural goals AG-001–AG-010 that motivate and constrain V2 (§1, §5 of this document) |
| [ARCH-001 §3](ARCH-001.md) | Architecture principles this document extends (§5) |
| [ARCH-001 §7](ARCH-001.md) | Module and engine responsibilities used as the authoritative V1 vocabulary (V1 Component Mapping) |
| [ARCH-001 §7.3](ARCH-001.md) | Engine ownership rule — no other component writes to an engine's owned storage area (§4, §5) |
| [ARCH-001 §8](ARCH-001.md) | Dependency rules V2's positioning must not violate (§8) |
| [ARCH-001 §30](ARCH-001.md) | Domain architecture — basis for V2's cross-cutting positioning (§8) |
| [ARCH-013](ARCH-013.md) | Domain event catalogue V2 consumes for invalidation signals (§6, §7, §10) |
| [ARCH-019](ARCH-019-Connector-Platform-Architecture.md) | Connector Platform responsibilities referenced in the V1 Component Mapping and V2 Dependencies (§7) |
| `docs/000-Overview/Glossary.md` (GLOSSARY-001) | Canonical definitions for Knowledge, Index, Traceability, Repository, and Provider used throughout this document |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | First V2 document built on this boundary; its code-level research corrected the `.ai/` → `.ferret/` path error in this document (§1, §7, §10) |
| Future V2 design documents | Must cite this document as their boundary contract |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial draft — establishes the V2 architectural boundary and the authoritative V1 component mapping. |
| 1.1 | 2026-07-03 | Ferret Core Team | Refinement pass — added Success Criteria, a formal definition of "AI-derived artifact" used consistently throughout, a Data Ownership principle, a conceptual boundary diagram, and the Expected V2 Architecture Series. Replaced sequencing language ("consulted before...") with boundary-oriented phrasing throughout. No change to scope, dependencies, or non-goals. |
| 1.2 | 2026-07-03 | Ferret Core Team | Correction — replaced all `.ai/workspace.json` / `.ai/state.json` path references (inherited from ARCH-001) with the actual persisted-state root `.ferret/`, per ARCH-024's code-level research. No change to scope, dependencies, principles, or non-goals — path fact only. |
| 1.3 | 2026-07-03 | Ferret Core Team | AGR-001 correction F3 — reworded §6 to state V1's relationship to V2 as "not required to function," removing the absolute "V1 has no dependency on V2" claim that contradicted §9's description of engines calling into V2's boundary. Frozen per AGR-001. |
