# ARCH-026 — Ferret V2 Persistence Requirements

| Field | Value |
|---|---|
| **Document ID** | ARCH-026 |
| **Version** | 1.1 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-001) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines requirements, not a mechanism; no mechanism decision exists yet to warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-024 (Artifact Inventory); ARCH-025 (Artifact Validity Model) |

---

## Purpose

This document answers one architectural question: **what dependency state must survive process termination in order for ARCH-025's validity model to be evaluated deterministically?**

It defines persistence *requirements* — what must outlive a process, what can remain in-memory, who owns it, and what constraints govern it. It does not define a persistence *mechanism*. Cache design, storage engine selection, database schemas, serialization formats, APIs, reuse algorithms, and performance optimizations are all out of scope and belong to later, mechanism-level documents.

---

## Scope

Covers:
- Which dependency state (per ARCH-025 §3) requires persistence, and which does not
- Ownership of any persisted dependency state
- The relationship between persisted dependency state and Ferret's existing storage
- The lifecycle of persisted dependency state, in the abstract
- Architectural constraints that govern all future persistence design
- Failure and recovery principles
- How persisted dependency state relates to repository-local state generally

Does not cover:
- Cache implementation or cache hierarchies
- Storage engine selection
- Database schemas
- Serialization formats
- APIs of any kind
- Reuse algorithms (ARCH-027)
- Performance optimizations
- Any redefinition of an artifact (ARCH-024) or a validity concept (ARCH-025)

---

## Repository-First Method

Every dependency shape, validity class, and principle referenced below is taken as-is from ARCH-025, which in turn is grounded in ARCH-024's artifact inventory. No new engine or subsystem is introduced; ownership follows ARCH-023's Data Ownership principle exactly as already stated. Where this document names an existing real persisted location (e.g. `.ferret/index-state.json`), it does so only to confirm that a requirement is already met today — it does not design or modify any storage location, format, or technology.

---

## 1. Which Dependency State Requires Persistence

Ferret's CLI and MCP surfaces are process-scoped: a command runs, produces output, and exits (ARCH-024 §7 confirms every CLI/MCP artifact is transient and in-memory only). Reuse across invocations is therefore only possible for dependency state that outlives the process that recorded it — anything else can only ever be reused within a single process's own lifetime, which is not reuse in the sense ARCH-023 exists to enable.

Applying ARCH-025 §3's five dependency shapes:

1. **Source content dependencies** (file bytes, path, modification metadata) — **already persisted.** The Index Engine's fingerprint map already survives process termination (`.ferret/index-state.json`, per ARCH-024 §3). This requirement is already met for Class A discovery and parsing artifacts.
2. **Configuration/registration dependencies** (parser version, connector configuration, model/provider identity) — **requires persistence.** ARCH-025 §4 recorded that no signal exists today for these changing. If validity is ever to be evaluated across a process restart, the specific registration/configuration identity an artifact depended on must survive — otherwise a later process has no way to know whether it changed.
3. **Index/knowledge-state dependencies** (the aggregate index state an artifact was assembled against) — **requires persistence**, but only for artifacts whose reuse is being considered. Today, nothing records "which index state a given `SearchResult`/`ContextPackage` depended on" (ARCH-024 §3–4 — both are ephemeral). Without this, no later process could compare a candidate reuse against the state it was originally computed under.
4. **Request-scoped input dependencies** (query text, token budget, and similar) — **requires persistence, conditionally.** This is only needed for the specific instances of an otherwise-ephemeral artifact that are being retained as reuse candidates. It is not a blanket requirement across every invocation.
5. **Derived-artifact dependencies** (an artifact that depends on another artifact) — **no independent persistence category.** These resolve recursively to the four shapes above; persisting a dependency chain means persisting each link's own dependency state, not inventing a fifth kind of record.

The governing rule: **dependency state requires persistence exactly when, and only when, the artifact it describes is itself a candidate for reuse beyond the process that produced it.** This ties the persistence requirement directly to the reuse question ARCH-027 will address, rather than treating persistence as a blanket default.

---

## 2. Which State Can Remain Ephemeral

- **Class D artifacts** (ARCH-025 §2) — excluded from the validity model entirely; nothing about them requires persistence.
- **Class C artifacts' live, in-memory handles** (e.g. `ConnectorRuntime`, ARCH-024 §1) — these are runtime handles to already-persisted primary data (`ConnectorInstance`), not dependency records; the underlying primary data is already persisted where it needs to be, so the handle itself has nothing further to persist.
- **Request-scoped parameters for any invocation whose output is not retained** — if an artifact stays ephemeral by design (as most are today), its dependency record is equally unnecessary. There is no purpose in persisting a dependency record for an artifact nobody will ever check the validity of again.
- **Static, compile-time/startup-time registry metadata** (`ParserDescriptor`, `ConnectorDescriptor`, per ARCH-024 §1–2) — these do not vary at runtime; their identity can be referenced by an artifact's dependency record without requiring their own independent persistence.

---

## 3. Ownership of Persisted Dependency State

Per ARCH-023's Data Ownership principle: **V2 owns no primary business data, and it owns no persisted dependency state either.** Ownership of any persisted dependency record belongs to whichever V1 component already owns the corresponding dependency or artifact — exactly the same components ARCH-025 §6 already identified as producing each validity class.

| Dependency state | Owning component | Current status |
|---|---|---|
| Source content / file fingerprint | Index Engine | Already owned and persisted (`.ferret/index-state.json`) — unchanged |
| Parser registration/version identity | Parser Platform | Not yet persisted as part of any dependency record — a requirement this document places on Parser Platform's own domain, not a new one |
| Connector configuration identity | Connector Platform | `ConnectorInstance` itself is already persisted (ARCH-024 §1); recording it as part of a dependent artifact's dependency set is not yet done |
| Model/provider configuration identity | **Unassigned** — no ARCH-023-approved component currently owns this. `Ferret.Configuration.AI` was grouped with Workspace Engine in ARCH-024 §6 for reading convenience only; ARCH-024 does not establish that grouping as an ownership relationship | `AiOptions` exists in memory only; not yet persisted or referenced as a dependency; recorded as a gap, not attributed to any component |
| Index/knowledge-state reference for Knowledge Engine artifacts | Knowledge Engine | Not yet persisted — Knowledge Engine persists nothing today (ARCH-024 §4) |
| Request-scoped input for a retained artifact | Whichever component owns the artifact (Knowledge Engine for `ContextPackage`/`SearchResult`; Review Engine/Artifact Engine, if implemented, for AI-derived artifacts) | Conditional — applies only if/when that artifact becomes a reuse candidate |

No row in this table assigns ownership to a new component. Where a component does not yet persist anything, the requirement is that *that component*, within its own domain, begins to — never that V2 or a new component does so on its behalf.

---

## 4. Relationship Between Persisted Dependency State and Existing V1 Storage

Persisted dependency state is not a new storage system. It is additional content within whichever persisted domain each owning component already maintains, or — where a component persists nothing today — a new responsibility placed on that component's own domain, not a separate one introduced alongside it.

This follows directly from two constraints already established:
- **AG-006 (Repository-Local State):** all platform state that matters lives in the repository. ARCH-024's Critical Finding 1 confirmed the real repository-local root is `.ferret/`, not `.ai/` as ARCH-001 originally stated (corrected in ARCH-023 v1.2).
- **ARCH-023 §4 non-goal:** V2 introduces no new source of truth for Knowledge.

Concretely: where a component already persists state relevant to its own artifacts (Index Engine's fingerprint map and keyword index; Workspace Engine's manifest and state), any additional dependency-state content belongs alongside that existing content, under that same component's ownership — not in a parallel structure V2 defines. Where a component persists nothing today (Knowledge Engine, and conditionally Review Engine/Artifact Engine), this document establishes only that persistence must eventually exist within that component's own domain if its artifacts are to be reuse candidates — it does not name a file, format, or technology for that persistence, which is a mechanism decision for a later document.

---

## 5. Lifecycle of Persisted Dependency State

- **Created** — at the same time the artifact it describes is produced, by the same component that produces the artifact (never by V2, per §3).
- **Read** — whenever a later process evaluates an artifact's validity per ARCH-025. This is a read-only consultation; reading dependency state never itself triggers recomputation. The decision to reuse or recompute is made elsewhere, consistent with ARCH-023 §9 (V2 never performs an engine's work).
- **Superseded** — when the dependency it records changes (an invalidation event, per ARCH-025 §4). At that point the record no longer describes current reality. What the owning component does with a superseded record — overwrite it, retain it, or discard it — is a mechanism decision this document does not make.
- **Removed** — this document defines no retention or eviction policy; that would be a mechanism decision, explicitly out of scope. It notes only that a persisted dependency record has a natural end of relevance (the moment it is superseded), after which its disposition belongs to later design.

---

## 6. Architectural Constraints on Persistence

- **Repository-local only** (AG-006) — no external state store, ever.
- **Owned exclusively by the component that owns the corresponding artifact or dependency** (ARCH-023 Data Ownership) — V2 is never the writer or owner of persisted dependency state.
- **Additive, never parallel** — persisted dependency state extends each component's existing persisted domain; it never constitutes a second, competing store (§4).
- **Scoped to reuse candidates only** — persistence is required exactly where §1's governing rule applies, never as a blanket default across every artifact.
- **Complete enough to support a deterministic validity decision** — a dependency record must capture every dependency shape (ARCH-025 §3) relevant to its artifact. A partial record cannot honestly support a validity decision; this echoes ARCH-025 §8's fail-closed principle at the persistence layer.
- **No new engine or subsystem** — persisted dependency state is held by the same eight ARCH-023-approved components, each within its own existing domain. This document introduces no ninth component to hold it.

---

## 7. Failure and Recovery Principles

- **Missing, corrupted, or unreadable dependency state means unknown validity — never assumed validity.** This extends ARCH-025 §8's fail-closed principle from "no signal exists" to "the signal existed but is now unreadable." Either way, the artifact cannot be certified valid.
- **Persisted dependency state must always be reconstructible from the same source that originally justified it.** Because it records facts about already-existing V1 data — file content, configuration, index state — the record itself is never the only place the fact is knowable. This document does not design how reconstruction happens; it establishes that reconstruction must always be possible in principle.
- **Loss of persisted dependency state degrades the system to full recomputation, never to incorrect reuse.** This is the direct consequence of fail-closed: when validity cannot be determined, the safe outcome is to recompute — the pre-V2 baseline — not to reuse optimistically. This preserves AG-004 (Deterministic Behaviour) as a floor that failure can never fall below.

---

## 8. Interaction With Repository-Local State

Persisted dependency state is repository-local state — not a special or separate category of it. It must:

- Share the same root as every other piece of V1 platform state that matters (the `.ferret/` root ARCH-024 confirmed, per AG-006).
- Coexist with, rather than duplicate, the persisted state each component already owns — the Index Engine's fingerprint map and keyword index, the Workspace Engine's manifest and state file, and whatever record Knowledge Engine, Review Engine, or Artifact Engine begin to maintain for their own artifacts in the future.
- Travel with the repository the same way the rest of `.ferret/` does, since the Repository (GLOSSARY-001) is Ferret's primary unit of knowledge and the platform maintains no knowledge store external to it.

Whether any specific piece of persisted dependency state is version-controlled or excluded from version control is a mechanism decision this document leaves undisturbed and does not address.

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-025's dependency shapes and validity classes, ARCH-024's artifact inventory, and ARCH-023's Data Ownership and Repository-Local State principles without modification. It reuses the Index Engine's existing fingerprint-map persistence as the working proof that this requirement is achievable within a component's own domain — the same pattern is required of other components, not a new one invented for them.

**Existing components extended.** None, directly. This document places a conditional future requirement on Parser Platform, Connector Platform, and Knowledge Engine to begin persisting dependency-relevant state within their own domains if their artifacts are to become reuse candidates. Model/provider configuration identity carries the same conditional requirement, but — per the correction in §3 — no ARCH-023-approved component currently owns it, so this document places no requirement on Workspace Engine specifically for that dependency. None of this makes a change to any component today; it states requirements a later, mechanism-level document would implement.

**Existing components intentionally unchanged.** Every V1 component's current ownership, storage location, and content — Index Engine's fingerprint map and keyword index, Workspace Engine's manifest and state file, and every gap ARCH-024 identified (Review Engine and Artifact Engine remain unimplemented, as do the "Specification" and "Memory" concepts ARCH-024 noted are not ARCH-023-approved component names; parser/connector/model configuration changes remain unsignalled). Nothing here changes any of it.

**New concepts introduced.** None. Every classification used in this document — dependency shapes, validity classes, the fail-closed principle — was already established in ARCH-025. This document applies that existing taxonomy to a new question (what must survive process termination) rather than introducing new categories, components, or storage boundaries.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Parent — Data Ownership and Repository-Local State principles this document applies to persistence specifically |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Parent — confirms `.ferret/` as the real persisted-state root (Critical Finding 1) and catalogues which artifacts are already persisted |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Parent — the dependency shapes, validity classes, and fail-closed principle this document extends to the persistence question |
| [ARCH-001 §2](ARCH-001.md) | AG-006 (Repository-Local State), AG-004 (Deterministic Behaviour) — basis for §4, §6, §7, §8 |
| ARCH-027 (Reuse) | Next document in the series — will define how a persisted, valid dependency record is used to retrieve and apply an artifact in place of recomputation |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial persistence requirements — third V2 design document, built on ARCH-023, ARCH-024, and ARCH-025. |
| 1.1 | 2026-07-03 | Ferret Core Team | AGR-001 corrections F1, F2 — re-caveated "Specification"/"Memory" as non-approved gap references; corrected the model/provider configuration ownership row from "Workspace Engine" to "Unassigned," since ARCH-024 never established that ownership. Frozen per AGR-001. |
