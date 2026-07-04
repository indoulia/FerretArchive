# ARCH-025 — Ferret V2 Artifact Validity Model

| Field | Value |
|---|---|
| **Document ID** | ARCH-025 |
| **Version** | 1.4 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-001) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines a conceptual model only; no mechanism is specified yet that would warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-024 (Artifact Inventory) |

---

## Purpose

This document answers one question: **when is an existing artifact still valid, and therefore eligible for reuse instead of recomputation?**

It defines the concept of artifact validity, the dependency and invalidation model that determines it, and the responsibilities existing V1 components carry with respect to it. It does not design how validity is checked, recorded, or stored — those are the concerns of ARCH-026 (Persistence) and ARCH-027 (Reuse). This document is the shared conceptual model those documents will implement against.

---

## Scope

Covers:
- The concept of artifact validity and how it differs from correctness or quality
- Classes of validity applicable to the artifacts ARCH-024 catalogued
- The dependency types that determine validity
- The sources of invalidation already observable in the repository today
- The minimum-invalidation principles that govern how invalidation propagates
- How validity relates to each existing V1 component
- Each existing component's responsibility with respect to validity
- The architectural principles that govern every future validity decision

Does not cover:
- Persistence mechanisms, cache design, or storage technology — ARCH-026
- Reuse mechanisms — ARCH-027
- Database schemas or APIs of any kind
- AI provider integrations
- Benchmarking
- Any redefinition of an artifact already catalogued in ARCH-024

---

## Repository-First Method

Every artifact, dependency, and mechanism referenced below is taken as-is from ARCH-024. No artifact is redefined, renamed, or given new fields here. Where this document needs a concept ARCH-024 did not name, it says so explicitly rather than inventing a component — per ARCH-023's Repository First Principle, only the eight approved component names (Workspace Engine, Connector Platform, Parser Platform, Index Engine, Knowledge Engine, Review Engine, Artifact Engine, Domain Event Bus) are used as owning-component labels.

---

## 1. The Concept of Artifact Validity

An artifact, as catalogued in ARCH-024, is **valid** at a given point in time when none of the dependencies recorded in its "Dependencies for validity" field have changed since it was produced.

Two distinctions matter:

**Validity is not correctness.** This document does not ask whether an artifact's content is good, accurate, or complete — only whether the conditions that produced it still hold. A `Document` with a parsing bug is just as "valid," in this sense, as one without — validity is about staleness, not quality.

**Validity is defined by dependency stability, not by output reproducibility.** For a deterministic artifact (e.g. `Document`, `ContextPackage` — ARCH-024 §2, §4), recomputing from unchanged inputs would reproduce the same output, so the two notions coincide in practice. For a non-deterministic artifact (e.g. `ChatResponse`, `EmbeddingResult` — ARCH-024 §5), recomputation from unchanged inputs would *not* reproduce the same output, because model sampling varies. If validity were defined as "recomputing would give the same result," no AI-derived artifact could ever be valid, which would make the concept useless for exactly the artifact category ARCH-023 exists to address. Validity is therefore defined solely in terms of whether the recorded dependencies have changed — never in terms of whether recomputation would match.

This resolves the tension ARCH-024's Critical Finding 2 surfaced: the fact that no AI-derived artifact is produced in the product today does not mean this model is inapplicable to them — it means their validity, once they exist, must be judged the same way as everything else: by dependency stability.

---

## 2. Classes of Validity

These classes are descriptive groupings of artifacts ARCH-024 already catalogued. They are not new components, engines, or storage boundaries.

**Class A — Deterministically re-derivable artifacts.** Given unchanged dependencies, recomputation would reproduce the same output. Examples from ARCH-024: `AssetDescriptor`, `Document`, `ParseResult<Document>`, `ContextPackage`, `SearchResult`. For this class, dependency stability and output reproducibility coincide, but validity is still judged by dependency stability alone (§1), so that the same reasoning applies uniformly across all classes.

**Class B — Non-deterministically produced artifacts.** Recomputation from unchanged dependencies would not necessarily reproduce the same output. Examples from ARCH-024 §5: `ChatResponse`, `ChatResponseChunk`, `EmbeddingResult`. None of these are currently produced in production use (ARCH-024, Critical Finding 2), but the class applies to them as soon as they are. Validity for this class rests entirely on dependency stability, per §1.

**Class C — Primary/identity data.** Artifacts that are not derived computations over some other input, but are themselves the authoritative record of current state. Examples from ARCH-024 §6: `WorkspaceManifest`, `WorkspaceStateDto`, `ConnectorInstance`, `AiOptions`. These are not subject to invalidation in the sense this document defines — they don't go stale relative to a dependency, because they *are* the dependency other artifacts are checked against. This class is out of scope for reuse decisions (there is nothing to recompute), but its members are frequently the source of invalidation for Class A and Class B artifacts (§4).

**Class D — Point-in-time observations.** Artifacts that describe a single past event or run rather than a current, checkable state. Examples from ARCH-024: `IndexResult`/`IndexStats`, `DiagnosticCheckResult`, CLI/MCP surface artifacts (ARCH-024 §7). These are explicitly excluded from the validity model — a diagnostic result or a run summary does not "remain valid" or "go stale"; it simply describes what happened at the time it was produced. This reaffirms the "Reuse-eligible: N/A" classification ARCH-024 already assigned to these artifacts.

---

## 3. Dependencies That Determine Validity

Generalising the "Dependencies for validity" column ARCH-024 recorded per artifact, five dependency shapes recur:

1. **Source content dependencies** — the bytes, path, and modification metadata of a discovered file. Governs `AssetDescriptor`, `Document`, `ParseResult<Document>` (ARCH-024 §1–2).
2. **Derived-artifact dependencies** — an artifact built from another artifact's output, not from source content directly. `SearchResult` depends on the keyword index; `ContextPackage` depends on `SearchResult` and the keyword index (ARCH-024 §3–4). Invalidating the upstream artifact must be capable of invalidating the downstream one.
3. **Index/knowledge-state dependencies** — the aggregate state of the index at the moment an artifact was produced, distinct from any single file. Governs `SearchResult` and `ContextPackage` (ARCH-024 §3–4), and is the closest realisation of ARCH-023's "knowledge state hash" concept found anywhere in the current inventory.
4. **Configuration/registration dependencies** — the specific parser, connector, or model/provider registration active when the artifact was produced. ARCH-024 records this explicitly for `Document` ("the specific parser version/registration that produced it," §2) and for `ChatResponse`/`EmbeddingResult` ("the specific model and provider invoked," §5).
5. **Request-scoped input dependencies** — parameters supplied by the specific request that produced the artifact (a query string, a token budget), rather than repository state. Governs `SearchResult` and `ContextPackage`. "Request" and the conditions under which two requests are equivalent are formally defined in ARCH-028.

One dependency shape found in the inventory resists this treatment: the Index Engine's fingerprint map (ARCH-024 §3) depends on "the full history of prior index runs," not a single checkable current state. This document records that as a distinct, harder case rather than forcing it into the five shapes above — a future document may need to address it, but this one does not invent a resolution.

The canonical mapping between these five dependency shapes and the four validity classes (§2) — which shapes apply to which classes, which apply only indirectly, and which classes participate only as a source or not at all — is defined in ARCH-030.

---

## 4. Invalidation Sources

An invalidation source is a real, already-observable signal that one of the dependency types in §3 has changed. This document catalogues which sources already exist and which do not — it does not design new ones.

**Already observable today**, per ARCH-024 §9 (Domain Events):
- File-level change, surfaced through `AssetFingerprint` comparison and the `DocumentDiscoveredEvent`/`DocumentIndexedEvent`/`DocumentParsingFailedEvent`/`DocumentSkippedEvent` family
- Index-run completion or failure, surfaced through `IndexingStartedEvent`/`IndexingCompletedEvent`/`IndexingFailedEvent`

**Not currently observable** — real invalidation sources with no corresponding signal today:
- A parser's registration or version changing (dependency shape 4, §3) — no event exists for this
- A connector's configuration changing (`ConnectorInstance` is Class C; changes to it are not published anywhere per ARCH-024 §1)
- A model or provider's configuration changing (`AiOptions`, Class C) — likewise unsignalled
- Any change to Review Engine or Artifact Engine state (both ARCH-023-approved names), or to the "Specification" and "Memory" concepts ARCH-024 catalogued as gaps and explicitly noted are not ARCH-023-approved component names — moot today, since none of the four are implemented (ARCH-024, Critical Finding 3), but a gap this document records for whenever they are

This asymmetry matters: a component that depends on parser registration, connector configuration, or model configuration cannot today be told when to reconsider its validity. This document does not resolve that gap — it is recorded so ARCH-026/ARCH-027 do not silently assume a signal exists where none does.

Deletion is architecturally distinct from modification: modification changes a dependency's target while it continues to exist; deletion means the target ceases to exist entirely, leaving nothing to compare against. Deletion belongs in this section's "not currently observable" category for every shape it applies to — no event in the existing catalogue signals a source, artifact, or registration ceasing to exist. ARCH-030 defines deletion's architectural meaning and its unconditional, irreversible invalidation consequence in full.

---

## 5. Minimum-Invalidation Principles

These principles state how invalidation propagates, per the Core V2 Principle established in ARCH-023 ("recompute only the minimum invalidated portion").

**Invalidation is scoped to recorded dependencies, never to category or ownership.** An artifact is invalidated only when a dependency it actually records (§3) has changed — never because it shares an owning component, a category (§2), or a storage location with something else that changed.

**Invalidation propagates along dependency edges, not proximity.** Where one artifact's dependency is another artifact (dependency shape 2, §3), invalidating the upstream artifact must be capable of invalidating the downstream one — but only through that recorded edge, not by association. This propagation must reach the full transitive chain, evaluated at the moment of each check, never independently of one; the consistency model this requires is formally defined in ARCH-029.

**Partial invalidation is the default, not the exception.** This is not a new idea — it is already how the Index Engine behaves: ARCH-024 §3 confirms the fingerprint map allows a single changed file to invalidate only that file's `Document`, not the whole index. This document generalises that existing, working pattern to every artifact class, rather than introducing a new one.

**Class C and Class D artifacts do not participate in invalidation.** Class C (primary data) is the *source* of invalidation, never its target. Class D (point-in-time observations) is exempt entirely (§2).

---

## 6. Relationships Between Validity and Existing Components

| Component (ARCH-023 vocabulary) | Real implementation (ARCH-024) | Validity class(es) it produces | Dependency signal it supplies |
|---|---|---|---|
| Connector Platform | `Ferret.ConnectorPlatform`, `Ferret.Connectors.Filesystem` | Class A (`AssetDescriptor`), Class C (`ConnectorInstance`) | File-level change (`AssetFingerprint`) — signalled. Connector configuration change — not signalled (§4) |
| Parser Platform | `Ferret.ParserPlatform`, `Ferret.Parsers.*` | Class A (`Document`, `ParseResult<Document>`) | Source content — signalled via the connector. Parser registration/version — not signalled (§4) |
| Index Engine | `Ferret.Indexing` | Class A (persisted keyword-index rows) | Index-run completion/failure — signalled. Already the one component with a working, generalisable invalidation pattern (§5) |
| Knowledge Engine | `Ferret.Search`, `Ferret.AI` (`ContextAssembler`) | Class A (`SearchResult`, `ContextPackage`) | Derived entirely from Index Engine's signal plus request-scoped input; supplies no independent signal of its own |
| Review Engine | Not implemented (ARCH-024, Critical Finding 3) | Would be Class B, if implemented | None — no implementation exists to supply one |
| Artifact Engine | Not implemented (ARCH-024, Critical Finding 3) | N/A — records provenance for other artifacts, is not itself a validity-bearing artifact | None |
| Domain Event Bus | `IEventBus` | N/A — carries signals, produces no artifact itself | Carries every signal listed as "already observable" in §4 |
| Workspace Engine | `Ferret.Workspace` | Class C (`WorkspaceManifest`, `WorkspaceStateDto`) | Supplies workspace-identity context other components' dependencies may reference; is not itself checked for validity |

---

## 7. Responsibilities of Existing Engines Regarding Validity

Each component is responsible only for the dependency signal it already produces. No component is responsible for judging another component's validity — this follows directly from ARCH-023's Data Ownership principle.

- **Connector Platform** is responsible for the accuracy of the file-level change signal it already supplies (`AssetFingerprint`). It is not responsible for signalling its own configuration changes today — that is a recorded gap (§4), not a new duty this document assigns.
- **Parser Platform** is responsible for the content it extracts being attributable to a specific, identifiable parser registration. It does not currently record that registration as part of an artifact's dependency set — a gap, not a duty invented here.
- **Index Engine** is responsible for exactly what it already does: comparing recorded fingerprints before recomputing, and publishing completion/failure signals. Its responsibility is unchanged by this document — this document generalises Index Engine's existing pattern to other components; it does not add to Index Engine's job.
- **Knowledge Engine** is responsible for the dependency set (index state, request parameters) that a produced `SearchResult`/`ContextPackage` rests on. It has no responsibility to retain that dependency set once its result is returned — whether it should is a persistence question, out of scope here (ARCH-026).
- **Review Engine** and **Artifact Engine**, if and when implemented, would be responsible for recording the dependency set — including model/provider identity and knowledge state — that justified accepting an AI-derived artifact. This document assigns that responsibility conditionally, to the concept ARCH-001/ARCH-023 already name, not to any code that exists today.
- **Domain Event Bus** is responsible for carrying whatever invalidation signals the producing component already emits. It does not evaluate validity itself.
- **Workspace Engine** is responsible for the accuracy of the primary/identity data (Class C) other components' dependencies may reference. It is never itself subject to invalidation.

---

## 8. Architectural Principles Governing Validity Decisions

| Principle | Statement | Basis |
|---|---|---|
| Core V2 Principle (carried forward) | Reuse every valid artifact already produced by Ferret. Recompute only the minimum invalidated portion | ARCH-023 §5 |
| Dependency stability over output reproducibility | Validity is determined solely by whether an artifact's recorded dependencies have changed — never by whether recomputation would reproduce the same output | §1 of this document; required to make validity meaningful for Class B artifacts |
| No cross-engine validity inference | No component judges another component's validity from its own internal state; each component's validity rests only on the dependency signals that component's dependencies already expose | ARCH-023 Data Ownership principle |
| Fail-closed on unrecorded dependencies | If a real dependency exists but has no signal (§4), an artifact cannot be certified valid on the basis of that dependency being "probably unchanged." Absence of a signal is not evidence of stability | Extends AG-004 (Deterministic Behaviour) — an undetermined validity state must not be treated as a positive one |
| Validity scoped to recorded dependencies only | Invalidation is never inferred from an artifact's category, owning component, or storage proximity to something else that changed | §5 of this document |
| Deterministic validity determination (carried forward) | Given identical dependency state, whether an artifact is valid is computed deterministically | ARCH-023 §5 (AG-004) |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-024's full artifact inventory and its "Dependencies for validity" field verbatim as its evidence base. It reuses ARCH-023's Core V2 Principle, Data Ownership principle, and "AI-derived artifact" definition without modification. It reuses the Index Engine's existing fingerprint-comparison behaviour as the working pattern every other principle in this document generalises from — nothing here is invented independently of what Index Engine already does.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behaviour to any V1 component. Where §7 states what Parser Platform or Review Engine "would be responsible for," that is a conditional statement about a future implementation, not a change made to any component today.

**Existing components intentionally unchanged.** All of them. Every gap ARCH-024 identified — the unimplemented Review Engine and Artifact Engine, the unimplemented "Specification" and "Memory" concepts ARCH-024 noted are not ARCH-023-approved component names, and the unsignalled parser/connector/model configuration changes — remains exactly as ARCH-024 found it. This document describes a model for reasoning about validity; it implements nothing.

**New concepts introduced.** Two, both purely conceptual: the four validity classes (§2) and the "dependency stability over output reproducibility" principle (§1, §8). Justification: ARCH-023 defines "AI-derived artifact" as a category V2 must reuse, but a non-deterministic artifact cannot be validated by the same reasoning as a deterministic one without this distinction — without it, "validity" would either exclude AI-derived artifacts entirely (defeating ARCH-023's purpose) or be defined so loosely it means nothing. Neither concept introduces a new component, interface, storage boundary, or API.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Parent — Core V2 Principle, Data Ownership principle, and "AI-derived artifact" definition this document builds on |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Parent — the artifact inventory, and specifically the "Dependencies for validity" field, this document generalises into a model |
| [ARCH-001 §2](ARCH-001.md) | AG-004 (Deterministic Behaviour), basis for the deterministic-validity-determination and fail-closed principles (§8) |
| [ARCH-013](ARCH-013.md) | Domain event catalogue — source of the "already observable" invalidation signals in §4 |
| ARCH-026 (Persistence) | Next document in the series — will define how validity-relevant dependency state is retained |
| ARCH-027 (Reuse) | Will define the mechanism by which a valid artifact is retrieved and applied in place of recomputation |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial artifact validity model — second V2 design document, built on ARCH-023 and ARCH-024. |
| 1.1 | 2026-07-03 | Ferret Core Team | AGR-001 correction F1 — re-caveated "Specification" and "Memory" as non-ARCH-023-approved gap references, no longer listed as if peer terms to Review Engine/Artifact Engine. Frozen per AGR-001. |
| 1.2 | 2026-07-03 | Ferret Core Team | AGR-002 Amendment 1 — appended a cross-reference to ARCH-028 in dependency shape 5 (§3), formally defining "request" and request equivalence, which this document had presupposed but never defined. No other change. |
| 1.3 | 2026-07-03 | Ferret Core Team | AGR-003 Amendment 1 — appended the transitive-closure requirement to §5's dependency-edge propagation principle, citing ARCH-029's consistency model. No other change. |
| 1.4 | 2026-07-03 | Ferret Core Team | AGR-004 Amendments 1 and 2 — appended deletion's definition and its "not currently observable" classification to §4; appended a cross-reference to ARCH-030's canonical Validity-Class × Dependency-Shape matrix to §3. No existing sentence altered. |
