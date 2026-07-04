# ARCH-032 — Ferret V2 Persistence Mechanism Design

| Field | Value |
|---|---|
| **Document ID** | ARCH-032 |
| **Version** | 1.2 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — requires a Standard Architecture Review (`AR-`) per V2-ROADMAP-001 §7's Tier 3 governance; escalates to a new Architecture Governance Review only if a conceptual gap is discovered |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None yet — this document makes no storage, format, or key decision; expected future ADRs are enumerated in "Interaction With Future ADRs" |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-024 (Artifact Inventory); ARCH-025 (Artifact Validity Model); ARCH-026 (Persistence Requirements) — the document this mechanism realizes; ARCH-027 (Dependency Resolution Architecture); ARCH-028 (Request Equivalence Architecture); ARCH-029 (Validity Propagation Architecture); ARCH-030 (Dependency Participation Semantics) |
| **Governed By** | ARCH-031 (Mechanism Architecture Principles) — the evidentiary and invariant standard this document is written to satisfy |
| **Roadmap Item** | [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) RM-07 |

---

## Purpose

ARCH-026 answers what dependency state must survive process termination. It deliberately does not answer how. This document is that "how" — the mechanism architecture that realizes ARCH-026's requirements, and the parts of ARCH-025, ARCH-027, ARCH-028, ARCH-029, and ARCH-030 that bear on what is durable, in a concrete, buildable form.

This is not an implementation design and not a storage-technology evaluation. It names no database, file format, serialization scheme, hash function, key structure, schema, table, or index. Every one of those is a consequential, hard-to-reverse choice under ARCH-031 §6's test and therefore belongs to an ADR or to implementation, not to this document.

Every statement in this document answers **how the conceptual kernel is realized**. None answers what the conceptual kernel should be. Where realizing a requirement would require deciding something the kernel left open, this document says so and stops, rather than deciding it.

---

## Scope

Covers:
- The mechanism-level responsibilities a persistence design must fulfil (§1)
- The dependency state, artifact state, and request-equivalence information that must be capable of persisting (§2)
- The persistence lifecycle, operationalized from ARCH-026 §5 (§3)
- What must remain intentionally unpersisted (§4)
- What persistence must expose for consultation, without naming a retrieval mechanism (§5)
- The guarantees a persistence mechanism must satisfy (§6)
- How persistence specifically — as distinct from resolution — preserves the kernel's invariants (§7)
- The boundary between persistence (this document) and resolution (RM-08) (§8)
- The implementation freedom this document deliberately leaves open (§9)

Does not cover, and will not decide:
- A storage technology of any kind (no evaluation of, or selection among, an embedded database, a flat file, an in-memory structure, or otherwise)
- Schemas, tables, or indexes
- Cache keys, hashes, or fingerprint designs
- Serialization formats or encodings
- APIs of any kind, public or internal
- Retrieval or lookup algorithms — those belong to RM-08 (§8)
- Any redefinition of an artifact (ARCH-024), a validity concept (ARCH-025), a persistence requirement (ARCH-026), a resolution outcome (ARCH-027), request equivalence (ARCH-028), a propagation rule (ARCH-029), or dependency participation semantics (ARCH-030)
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every requirement, class, shape, and principle referenced below is taken as-is from ARCH-024 through ARCH-030 and the evidentiary standard ARCH-031 sets for documents at this tier. This document introduces no new artifact, validity class, dependency shape, resolution outcome, equivalence rule, propagation rule, or component. Ownership follows ARCH-023's Data Ownership principle and ARCH-026 §3's ownership table exactly as already stated. Where this document states a mechanism-level rule not spelled out verbatim in the kernel, that rule is a direct, traceable corollary of an existing principle — never an independent decision — and its derivation is shown at the point it is introduced.

---

## 1. Persistence Responsibilities

A persistence mechanism exists to make the following true, regardless of how it is technically realized:

| Responsibility | Realizes | Owned by |
|---|---|---|
| **Record** — capture an artifact's dependency state, and (where applicable) its own output, at the moment the artifact is produced | ARCH-026 §5, "Created" | The same component that produces the artifact (ARCH-026 §3) — never V2, never a new component |
| **Retain** — keep a recorded fact available across process termination, for as long as it remains relevant to a reuse decision | ARCH-026 §1's governing rule; AG-006 (Repository-Local State) | Same owning component |
| **Expose for consultation** — make a recorded fact available to be read, without itself judging validity or performing resolution | ARCH-026 §5, "Read"; ARCH-027 §1 ("resolution... does not detect change; it interprets already-detected or already-recorded change") | Same owning component; consulted by whichever engine initiates resolution (ARCH-027 §2) |
| **Mark relevance boundaries** — reflect that a recorded fact has been superseded by a change, without deciding what happens to the record physically | ARCH-026 §5, "Superseded" | Same owning component |
| **Terminate relevance** — allow a record's disposition (retain, overwrite, discard) to be decided independently of this document, since ARCH-026 §5 explicitly leaves this open | ARCH-026 §5, "Removed" | Same owning component; policy is an implementation freedom (§9) |

No responsibility in this table is new. Each is the mechanism-level form of a requirement ARCH-026 §5 already stated in the abstract. This document adds no sixth responsibility and removes none of ARCH-026's four lifecycle stages.

**No persistence mechanism performs resolution.** Recording and exposing facts is exhaustive of persistence's responsibility; comparing those facts against a current request and reaching an outcome is RM-08's responsibility (§8).

---

## 2. Inputs

### 2.1 Dependency State

Per ARCH-026 §1's governing rule — *"dependency state requires persistence exactly when, and only when, the artifact it describes is itself a candidate for reuse beyond the process that produced it"* — a persistence mechanism must be capable of recording each of ARCH-025 §3's five dependency shapes as follows:

| Shape | Mechanism must be capable of recording | Status per ARCH-026 §1 |
|---|---|---|
| 1 — Source content | The file identity and modification state an artifact was produced against | Already met (Index Engine's fingerprint map) — this document imposes no new requirement here |
| 2 — Derived-artifact | Which other artifact(s) a given artifact depends on, by reference, not by embedding a copy of that artifact's own dependency state (§7.3) | No independent persistence category — resolves recursively into shapes 1, 3, 4 |
| 3 — Index/knowledge-state | The aggregate index/knowledge state an artifact was assembled against, sufficient to determine later whether that aggregate state has changed | Required, conditionally, for reuse-candidate artifacts only |
| 4 — Configuration/registration | The specific parser, connector, or model/provider registration identity active at production time | Required — currently unmet for any component (ARCH-026 §3) |
| 5 — Request-scoped input | See §2.3 — elaborated separately per ARCH-028 | Required, conditionally |

A persistence mechanism must be able to represent **that a shape-1, shape-2, or shape-4 target has been deleted**, as architecturally distinct from that target having been modified (ARCH-030 §2). This document does not define how a deletion signal is produced — that gap is recorded by ARCH-025 §4 and ARCH-030 §2 as unresolved for every shape it applies to, and remains unresolved here. What this document requires is only that the persistence mechanism's capacity to represent a dependency's state must not foreclose representing its absence once such a signal exists. A mechanism that can only express "unchanged" or "changed-to-X," with no way to ever express "no longer exists," would under-realize ARCH-030 §2 the moment a deletion signal is introduced — even though no such signal exists today.

### 2.2 Artifact State

ARCH-026 is framed around dependency state, but a dependency record with nothing to reuse is not reuse. Where an artifact itself becomes durable (a mechanism decision each owning component makes independently, per ARCH-023's Data Ownership principle — this document does not mandate that any specific artifact become durable), its own output and its dependency record are companion facts, not separate concerns:

- They are persisted **together**, under the same owning component, per ARCH-026 §4's "additive, never parallel" constraint — never in a structure this document defines on the component's behalf.
- They apply only to **Class A and Class B** artifacts (ARCH-025 §2). Class C is already persisted, in each owning component's own existing domain, as primary data — never as a reuse candidate (ARCH-030 §4). Class D is excluded from persistence for reuse purposes entirely (ARCH-025 §2; ARCH-030 §4, §6) — a Class D artifact's transient record of what happened is a different concern from what this document, or ARCH-026, addresses.
- **This document does not decide which artifacts become durable.** Whether Knowledge Engine ever persists `ContextPackage` or `SearchResult` (both ephemeral today, per ARCH-024 §3–4) is a decision that component makes within its own domain. This document defines only what must be true *if and when* it does.

### 2.3 Request-Equivalence State

ARCH-028 §2 defines a request's identity as exactly three properties: the engine responsibility invoked, the complete explicit parameter set, and the ambient dependency scope not already captured by an explicit parameter. Per ARCH-027 §4 and ARCH-028 §7, a persisted artifact can never become a resolution candidate unless a later request's equivalence to the request that produced it can be evaluated — which is impossible unless all three properties were recorded, not a convenient subset.

A persistence mechanism must therefore be capable of recording each of the three properties independently and completely, for any artifact for which request-scoped input (dependency shape 5, §2.1) applies. This document does not define how those three properties are encoded, compared, or looked up — ARCH-028 §9 already excludes "any representation of a request... no key, hash, fingerprint, identifier, or encoding of any kind" from its own scope, and this document does not reach past that exclusion. It states only that omitting any one of the three properties from what is recorded would make a later equivalence determination impossible to perform honestly — an omission this document forbids, without specifying the representation that avoids it.

---

## 3. Persistence Lifecycle

Operationalizing ARCH-026 §5's four stages at the mechanism tier:

- **Created.** Recording happens at the same time, and by the same component, that produces the artifact (§1). A mechanism must not defer recording to a later, separate process — doing so would create a window in which an artifact exists without a way to ever judge its validity, which is indistinguishable from not persisting it at all.
- **Read.** Consultation is read-only. A persistence mechanism must never be mutated as a side effect of being read — this is the persistence-layer restatement of ARCH-027 §5's "no side effects" guarantee, itself grounded in ARCH-023 §9's rule that V2 never performs an owning engine's work. Reading is performed by whatever process initiates resolution (RM-08); persistence's responsibility ends at making a complete, accurate record available to that read.
- **Superseded.** A mechanism must be able to reflect that a recorded fact no longer describes current reality once its dependency changes (ARCH-025 §4). It is **not** required to decide what happens to the record physically at that moment — ARCH-026 §5 explicitly leaves overwrite, retention, or discard open, and this document does not close that gap; it is implementation freedom (§9). The one requirement that is not optional: a superseded record must never be presented, on read, as indistinguishable from a current one (§6, §7).
- **Removed.** ARCH-026 §5 defines no retention or eviction policy, and this document does not invent one. A mechanism may retain a superseded or stale record indefinitely, evict it immediately, or anything between — the correctness of resolution never depends on which. What resolution depends on is fail-closed behavior (§6) when a record is absent, which is a guarantee about behavior in the absence of a record, not a rule about when removal should happen.

---

## 4. What Is Intentionally Not Persisted

Directly per ARCH-026 §2, restated at mechanism tier — a persistence mechanism has no responsibility toward:

- **Class D artifacts.** Excluded from the validity model entirely (ARCH-025 §2); nothing about them requires persistence for reuse purposes.
- **Class C live, in-memory handles.** A runtime handle to already-persisted primary data has nothing further to persist — the underlying primary data is already persisted where its owning component already persists it, outside this document's scope.
- **Request-scoped parameters for artifacts that are not retained.** If an artifact stays ephemeral by design, there is no purpose in persisting a dependency or request-identity record nobody will ever check.
- **Static, compile-time/startup-time registry metadata** (e.g., a parser or connector descriptor that does not vary at runtime). Such identity can be referenced by a dependency record without requiring independent persistence of its own.
- **Any judgment of artifact correctness or quality.** Validity is not correctness (ARCH-025 §1) — a persistence mechanism records whether dependencies have changed, never whether the artifact's content is good, accurate, or complete. There is no "confidence" or "quality" field this document authorizes.
- **A precomputed validity verdict.** This is a mechanism-level corollary of ARCH-029 §1 and §3, not a new principle: because a dependency change becomes architecturally observable only at check time, and eager propagation is not architecturally permitted, a persistence mechanism must never store "this artifact is currently valid" as a fact in its own right. It stores the raw dependency facts (§2); the verdict is computed at check time, by resolution (§8), from those facts. Persisting a verdict would be indistinguishable from implementing the eager propagation ARCH-029 §3 rules out.
- **A second source of truth for anything a V1 component already owns.** Per ARCH-023 §4 and ARCH-027 §5 ("no new source of truth"), a persisted dependency record is never an independent record that could diverge from what the owning component's own state already says — it is a fact about that state at production time, not a competing copy of the state itself.

---

## 5. Outputs — What Persistence Exposes for Consultation

For a given persisted artifact, a persistence mechanism must make available, to whatever process performs resolution:

- The complete recorded dependency state (§2.1), covering every dependency shape relevant to that artifact — a partial exposure cannot honestly support a validity decision (ARCH-026 §6).
- The artifact's own durable output, where the owning component has chosen to make it durable (§2.2).
- The complete recorded request-identity properties (§2.3), where request-scoped input applies.
- Whether the record itself is readable, complete, and uncorrupted, or not — this is a boolean-shaped fact about the record's own integrity, not a judgment about the artifact's validity. Resolution (§8) is responsible for turning "the record is unreadable" into the Indeterminate outcome ARCH-027 §3 defines; persistence's responsibility is only to make that distinction detectable at all.

This document does not define the form in which any of the above is exposed — that is retrieval, which belongs to RM-08 (§8) and, ultimately, an ADR.

---

## 6. Persistence Guarantees

| Guarantee | Statement | Basis |
|---|---|---|
| Repository-local only | No persisted dependency, artifact, or request-identity state is ever held outside the repository | AG-006; ARCH-026 §6 |
| Owned exclusively by the producing component | Persisted state is never owned, written, or read-authoritatively by V2 or by any component other than the one that produced the artifact it describes | ARCH-023 Data Ownership; ARCH-026 §3, §6 |
| Additive, never parallel | Persisted dependency/artifact/request state extends each component's existing persisted domain; it never constitutes a second, competing store | ARCH-026 §4, §6 |
| Scoped to reuse candidates only | Persistence exists exactly where §2's governing rule applies — never as a blanket default across every artifact an engine produces | ARCH-026 §1, §6 |
| Complete enough to support a deterministic decision | A dependency record captures every dependency shape relevant to its artifact; a partial record is never presented as sufficient | ARCH-026 §6; extends ARCH-025 §8's fail-closed principle to the persistence layer |
| No new engine or subsystem | Persisted state is held by the same eight ARCH-023-approved components, each within its own existing domain | ARCH-026 §6 |
| Fail-closed on absence, corruption, or unreadability | Missing, corrupted, or unreadable state means unknown validity, never assumed validity | ARCH-025 §8; ARCH-026 §7 |
| Reconstructible in principle | A persisted fact is never the only place the fact it records is knowable — it can always, in principle, be reconstructed from the same V1 data that originally justified it | ARCH-026 §7 |
| Degrades to recomputation, never to incorrect reuse | Loss of persisted state removes the option to reuse; it never causes an artifact to be treated as valid when it cannot be confirmed so | ARCH-026 §7; AG-004 |
| No precomputed verdict | Persistence records dependency facts, never a validity or resolution outcome derived from them | ARCH-029 §1, §3 (§4 of this document) |

---

## 7. Mechanism-Level Invariants — Preserving Conceptual Guarantees Through Persistence

This section traces each kernel invariant bearing on persistence specifically — as distinct from resolution (§8) — to how this document's design realizes it, satisfying ARCH-031 §7's evidentiary requirement.

**7.1 Fail-closed (ARCH-025 §8; ARCH-026 §7; ARCH-031 §3, §8).** Realized by §6's "Fail-closed on absence, corruption, or unreadability" guarantee: persistence's only obligation on failure is to make the failure detectable, never to substitute a default. It never silently treats "no record" or "unreadable record" as "unchanged."

**7.2 Repository First (ARCH-023 §4; AG-006; ARCH-031 §8).** Realized by §6's "Repository-local only" guarantee and by this document naming no external state store anywhere in §1–§6.

**7.3 Existing ownership (ARCH-023 Data Ownership; ARCH-026 §3; ARCH-031 §8).** Realized by §1's responsibility table and §6's ownership guarantee: every responsibility is assigned to "the same component that produces the artifact," never to V2 or a new component. §2.1's dependency-shape-2 row makes this concrete: a derived-artifact dependency is recorded **by reference**, not by embedding a copy of the upstream artifact's own dependency record — embedding would let a downstream component's persisted state silently duplicate, and potentially diverge from, the upstream owner's own record, which §6's "additive, never parallel" and "no new source of truth" guarantees forbid.

**7.4 Minimum invalidation (ARCH-025 §5; ARCH-031 §3, §8).** Persistence's role is confined to recording facts at the granularity ARCH-025 §3 already defines (per-shape, per-artifact) — it never aggregates, buckets, or stores a dependency fact at a coarser grain than the artifact it describes. Coarser storage would make minimum-scoped invalidation undecidable at the persistence layer, forcing resolution to over-invalidate for lack of granularity. This document requires the finest grain the kernel already defines and no coarser.

**7.5 No hidden side effects (ARCH-023 §9; ARCH-027 §5; ARCH-031 §3, §8).** Realized by §3's "Read" lifecycle rule: consultation never mutates. Persistence's own write path (Created, Superseded) is triggered only by the owning component's own artifact production or invalidation handling — never by a resolution read.

**7.6 No silent recomputation (ARCH-031 §3, §8).** Persistence has no computation to silently perform — it stores and exposes facts, and computes no outcome (§4, "no precomputed verdict"). This invariant binds resolution (§8) more directly than persistence; persistence's contribution is to never present an absent or stale fact as though it were a fresh Satisfied result, which would let resolution recompute "successfully" against corrupted assumptions without anyone noticing.

**7.7 Deterministic evaluation (AG-004; ARCH-031 §3, §8).** Persistence's contribution to determinism is completeness (§6) and non-mutation-on-read (§3): given the same recorded facts, resolution's determinism (a guarantee ARCH-027 §5 already establishes) depends on those facts not changing between reads. A persistence mechanism that returned different content for the same fact on two reads — even if each read were individually "complete" — would break determinism at a layer resolution cannot detect or correct for.

**7.8 Deletion is unconditional and irreversible per dependency instance (ARCH-030 §2, §7).** Realized by §2.1's explicit deletion-representability requirement: a persistence mechanism must be able to express "this target's dependency has been deleted" as distinct from "this target has not changed," even though this document does not define how that signal arises. A mechanism that cannot express this at all would make ARCH-030 §2's unconditional consequence unrealizable no matter how resolution is designed.

**7.9 Class D exclusion is absolute (ARCH-030 §4, §7).** Realized by §4's explicit non-goal: a persistence mechanism has no code path that persists Class D artifact state for reuse purposes at all — there is nothing to guard against constructing, because it is never constructed.

**7.10 Class C is a source, never a persisted target of invalidation (ARCH-030 §4, §6).** Realized by §2.1's shape-2 by-reference rule (§7.3, above) applied to Class C specifically: a dependency record referencing Class C data must record a reference to that data's identity, never an embedded, frozen copy of its value. Class C's current state is already persisted by its own owning component (ARCH-026 §3); a dependency record that duplicated Class C's value would itself become a second, potentially stale, copy — precisely what "Class C termination always performs its comparison" (ARCH-030 §7) requires resolution to be able to check against the component's live state, not against a copy this document would otherwise have introduced.

**7.11 The Validity-Class × Dependency-Shape matrix is exhaustive and closed (ARCH-030 §5, §7).** A persistence mechanism records only the dependency shapes §2.1 enumerates, for only the validity classes ARCH-030 §5's matrix marks Applicable or Indirect for that shape. This document authorizes no persisted record for a class-shape combination the matrix marks Not Applicable or Excluded — for example, no dependency record is ever created describing a Class D artifact's shape-1 dependency, because no such combination exists in the matrix.

---

## 8. Persistence Responsibilities vs. Resolution Responsibilities (Boundary With RM-08)

Per V2-ROADMAP-001 §5's own division of Tier 3 work, resolution (RM-08) owns retrieval and the decision procedure; persistence (this document) owns durability and completeness of what is retrieved and decided over. Concretely:

| Belongs to Persistence (this document) | Belongs to RM-08 (Resolution and Retrieval) |
|---|---|
| That a dependency fact survives process termination | Locating which persisted candidate, if any, corresponds to a given request (retrieval) |
| That a fact, once recorded, is complete for its artifact | Comparing a candidate's recorded facts against current dependency state to reach Satisfied / Not-satisfied / Indeterminate (resolution, ARCH-027 §3) |
| That reading a fact never mutates it | Evaluating request equivalence per ARCH-028 §3's contract-level relation (resolution) |
| That a superseded fact is distinguishable, on read, from a current one | Deciding what "distinguishable" implies for the outcome of a specific check (resolution) |
| That absence or corruption of a fact is detectable | Turning detected absence or corruption into the Indeterminate outcome (resolution, ARCH-027 §3) |
| Ownership of the fact (which V1 component's domain it lives in) | Combining outcomes across a dependency chain (resolution, ARCH-029 §6) — this reads facts from potentially several owning components' persisted state without owning any of them |
| — (not a persistence responsibility) | Key design, indexing, and lookup structure (retrieval) — V2-ROADMAP-001 §5 assigns "retrieval approach, keys" wholly to RM-08's exit criteria; this document decides none of it |

Persistence and resolution are two different acts (ARCH-027 §1), even though V2-ROADMAP-001 §5 assigns both resolution and retrieval to the same document, RM-08. Neither persistence nor RM-08 performs the other's role: persistence never judges validity or locates a candidate; RM-08 never decides what survives a process boundary.

---

## 9. Implementation Freedom Remaining

After this document, at least the following remain entirely open, to be settled by RM-08 (where they concern retrieval) or by an ADR (where they concern a technology or format choice):

- Storage technology, location, and structure — any of a file, an embedded database, an in-memory structure, or another form, provided §6's repository-local and ownership guarantees hold
- Serialization format or encoding
- Key design, hashing scheme, or fingerprinting approach used to locate a record
- Whether superseded records are overwritten, retained, or discarded (§3)
- Retention or eviction policy for records no longer relevant (§3)
- Whether any specific piece of persisted state is version-controlled or excluded from version control (ARCH-026 §8 already leaves this open; this document does not close it)
- How corruption or unreadability is actually detected (by whatever means the chosen storage technology provides) — this document requires only that detection be possible (§6), not how
- The retrieval algorithm or structure used to locate a persisted candidate for a given request (RM-08)

**Not an implementation freedom — an unresolved conceptual gap, recorded here rather than assigned.** How a deletion signal is actually produced and delivered is left open by ARCH-025 §4 and ARCH-030 §2 as a gap in the same category as the still-unsignalled parser/connector/model-configuration changes — not a technology choice safe for an ADR to settle unilaterally. Unlike the freedoms above, any design that would produce such a signal may itself touch what ARCH-025 §4 accepts as a valid invalidation source, which could require a new Architecture Governance Review before an ADR or implementation proceeds (V2-ROADMAP-001 §1's escalation rule). This document does not resolve the gap and does not assign it to RM-08 or to any ADR by default.

---

## Relationship to the Conceptual Kernel

This document adds nothing to the frozen kernel and amends none of ARCH-023 through ARCH-030. It realizes ARCH-026's requirements, at the mechanism tier ARCH-031 defines, using validity classes and dependency shapes from ARCH-025 and ARCH-030, request-identity properties from ARCH-028, and the consultative-propagation and outcome-combination model from ARCH-029, exactly as each already states them. Where this document states a rule not verbatim in the kernel — the no-precomputed-verdict rule (§4, §7.6) and the by-reference recording of derived-artifact and Class C dependencies (§2.1, §7.3, §7.10) chief among them — each is shown, at the point it is introduced, to be a direct corollary of an existing kernel principle, never an independent addition to it.

---

## Interaction With RM-08

RM-07 (this document) must be complete before RM-08 (Resolution Mechanism Design) proceeds, per V2-ROADMAP-001 §5's entry criteria for RM-08. RM-08 may assume this document's guarantees (§6) and invariants (§7) hold; it may not assume any specific storage technology, format, or retrieval structure, since none is decided here (§9) — RM-08 is precisely where the retrieval approach and key design this document defers are settled, per V2-ROADMAP-001 §5's exit criteria for RM-08. Where RM-08 discovers that a guarantee in §6 cannot be upheld by any retrieval design consistent with ARCH-027, that is a conceptual gap in this document, not a license for RM-08 to weaken the guarantee — per ARCH-031 §9, RM-08 must halt and escalate rather than silently proceed.

---

## Interaction With Future ADRs

Per ARCH-031 §6's test — a decision is ADR-level, not ARCH-level, if reversing it would not require a new governance review because no §6/§7 guarantee or invariant would be affected — the following are expected to be recorded as ADRs once RM-07 and RM-08 are both far enough along to make them concretely, following the existing convention already established in this repository (`docs/adr/`, per ADR-0001):

- The storage technology and structure chosen to hold persisted dependency, artifact, and request-identity state
- The serialization format chosen
- The retention/eviction policy chosen for superseded or stale records
- The specific mechanism chosen to detect corruption or unreadability

The key or lookup structure used to locate a persisted candidate is **not** this document's ADR to produce — per §8's boundary table, key design belongs wholly to RM-08 (ARCH-033), consistent with V2-ROADMAP-001 §5's assignment of "retrieval approach, keys" to RM-08's exit criteria alone. This document's only obligation toward that future ADR is that whatever key an ADR under ARCH-033 defines, this document's storage mechanism must be able to be located by it without this document itself deciding its form.

Each such ADR must state, in its Consequences section, which of this document's guarantees (§6) and invariants (§7) it upholds and how — an ADR that cannot make that statement has made a decision this document's constraints do not permit, and must be revised rather than accepted as-is.

---

## Conformance With ARCH-031

| ARCH-031 §7 requirement | Satisfied by |
|---|---|
| Guarantee-by-guarantee trace | §7 (Mechanism-Level Invariants), tracing eleven kernel invariants individually |
| Responsibility trace | §1 (Persistence Responsibilities) and §8 (Boundary With RM-08) |
| Ownership trace | §1's ownership column and §7.3; no new owning component introduced anywhere in this document |
| Explicit non-goals | Scope ("Does not cover"), §4, §9 |
| Statement of ADRs produced | Interaction With Future ADRs — none produced by this document itself; five anticipated |
| Confirmation no Closed Architectural Decision is contradicted | See Impact on Existing Architecture, below |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-026's persistence requirements, ownership table, and lifecycle stages; ARCH-025's validity classes, dependency shapes, and fail-closed principle; ARCH-028's request-identity properties; ARCH-029's consultative-propagation model and outcome-combination rule; and ARCH-030's deletion semantics and applicability matrix — all without modification.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behavior to any V1 component beyond what ARCH-026 §3 already conditionally required of Parser Platform, Connector Platform, and Knowledge Engine.

**Existing components intentionally unchanged.** All of them. Every ownership assignment, gap, and gap-classification from ARCH-024 through ARCH-030 remains exactly as those documents left it.

**New concepts introduced.** None at the conceptual tier — this document introduces no new artifact, validity class, dependency shape, resolution outcome, equivalence rule, propagation rule, or component. Two mechanism-tier corollaries are introduced, both derived directly from existing kernel principles rather than added independently: the no-precomputed-verdict rule (§4, §7.6 — a direct consequence of ARCH-029 §1 and §3) and the by-reference (never embedded-copy) recording rule for derived-artifact and Class C dependencies (§2.1, §7.3, §7.10 — a direct consequence of ARCH-023's Data Ownership principle and ARCH-027 §5's "no new source of truth" guarantee). Neither is a conceptual decision about what the kernel should be; both are mechanism-level constraints on how the kernel's existing guarantees remain true once persistence is realized.

**Closed Architectural Decisions.** All nine (AGR-001 §6) checked individually against this document's text; none is contradicted, narrowed, or reinterpreted.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Data Ownership principle; the eight approved component names this document assigns every responsibility to |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Source of the artifacts referenced in §2.2's examples |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Validity classes, dependency shapes, and fail-closed principle this document persists state for |
| [ARCH-026](ARCH-026-Persistence-Requirements.md) | Parent — the requirements document this mechanism design realizes in full |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Boundary this document's §8 draws against — resolution consumes what persistence exposes |
| [ARCH-028](ARCH-028-Request-Equivalence-Architecture.md) | Source of the three request-identity properties §2.3 requires persistence to capture |
| [ARCH-029](ARCH-029-Validity-Propagation-Architecture.md) | Source of the consultative-propagation model behind §4's no-precomputed-verdict rule |
| [ARCH-030](ARCH-030-Dependency-Participation-Semantics.md) | Source of the deletion-representability (§2.1, §7.8), Class D exclusion (§7.9), Class C sourcing (§7.10), and matrix-closure (§7.11) requirements |
| [ARCH-031](ARCH-031-Mechanism-Architecture-Principles.md) | Governing document — the evidentiary standard (§7) and invariant checklist this document is written to satisfy |
| [AGR-001](../Reviews/AGR-001.md) | Source of the nine Closed Architectural Decisions confirmed unaffected (Impact, above) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-07 — this document is that roadmap item; RM-08's entry criteria depend on this document's completion |
| `docs/adr/README.md`, [ADR-0001](../adr/0001-use-architecture-decision-records.md) | Existing ADR process and location this document defers its consequential technology choices to (Interaction With Future ADRs) |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Persistence Mechanism Design — realizes ARCH-026 at the mechanism tier ARCH-031 defines. Pending Standard Architecture Review. |
| 1.1 | 2026-07-03 | Ferret Core Team | Pre-acceptance review corrections — separated the deletion-signal-production gap from ordinary implementation freedoms (§9), corrected the ARCH-024 §4 citation to §3–4 (§2.2), removed a broken self-citation from the field table, retitled §8's boundary table to distinguish resolution from retrieval per ARCH-027 §1, reworded §8's key-design row to assign it wholly to RM-08, corrected the "no side effects" attribution (§3), and replaced an illustrative reference to "checksum" with technology-neutral language (§9). No change to any guarantee, invariant, or responsibility. |
| 1.2 | 2026-07-03 | Ferret Core Team | Mechanism-package review correction — removed "Interaction With Future ADRs"' conflicting claim that the key/lookup ADR is jointly owned with RM-08, which contradicted §8's boundary table; key design is now stated as belonging wholly to RM-08 (ARCH-033) in both places, consistent with V2-ROADMAP-001 §5. No change to any guarantee or invariant. |
