# ARCH-030 — Ferret V2 Dependency Participation Semantics

| Field | Value |
|---|---|
| **Document ID** | ARCH-030 |
| **Version** | 1.1 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-004) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines architectural semantics, not a mechanism; no mechanism decision exists yet to warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-025 (Artifact Validity Model) §2, §3, §4 — the sections this document amends |
| **Roadmap Items** | [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) RM-03, RM-04 (batched) |
| **Resolves** | [AGR-001](../Reviews/AGR-001.md) §5, Deferred Questions F6 (Deletion Semantics) and F9 (Validity-Class / Dependency-Shape Matrix) |

---

## Purpose

This document resolves two of AGR-001's remaining deferred questions together, because both define complementary aspects of the same underlying concern: **what it architecturally means for a dependency to participate in validity.** F6 asks what happens when a dependency's target ceases to exist. F9 asks which combinations of validity class and dependency shape are even meaningful. Neither can be answered precisely without the other — deletion semantics differ by dependency shape, and the applicability matrix is incomplete without stating what happens at its edges.

This is the third amendment to the frozen V2 Foundation. It is architecture-only: it defines semantics, not a mechanism. Per AGR-001 §8, it does not become part of the frozen foundation on its own. It concludes with the specific changes it proposes (§9) and remains a proposal until a new Architecture Governance Review (AGR-004) accepts it.

---

## Scope

Covers:
- What it means, architecturally, for a dependency to exist
- The architectural meaning of deletion, and how it differs from modification
- Which dependency shapes (ARCH-025 §3) participate in validity, and how
- Which validity classes (ARCH-025 §2) participate in dependency evaluation, and how
- The canonical Validity-Class × Dependency-Shape applicability matrix
- Termination and exclusion rules for dependency chains
- Invariants any future implementation must preserve
- The specific amendments this document proposes to ARCH-025

Does not cover:
- Storage, retrieval, cache structures, or database schemas
- APIs of any kind
- Background processing, scheduling, or polling
- Performance guarantees of any kind
- How deletion is detected — this document defines what deletion *means*, not how a signal for it would be produced (a gap ARCH-025 §4 already records for modification-type changes; this document extends that same recorded gap to deletion rather than closing it)
- Any change to ARCH-025's four validity classes or five dependency shapes beyond stating how they combine
- Any change to ARCH-029's propagation model or ARCH-027's resolution outcomes
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every class, shape, artifact, and principle referenced below is taken as-is from ARCH-024 and ARCH-025. This document introduces no new validity class, no new dependency shape, and no new component. It answers a question the existing four classes and five shapes left open: how they combine, and what happens at their boundaries.

---

## 1. What Does It Mean for a Dependency to Exist?

**A dependency exists as a recorded relationship between an artifact and the state of something else at the moment the artifact was produced — not as a reference to that something else's current state.**

When ARCH-025 §1 says an artifact is valid "when none of the dependencies recorded in its... field have changed since it was produced," the dependency being checked is the *relationship* — "this artifact was produced when X was in state S" — not X itself. This distinction matters because it settles what happens when X later ceases to exist entirely (§2): the dependency does not vanish along with X, because the dependency was never a pointer to X's current state — it was always a record of X's state at production time. A dependency, once it exists, persists for as long as the artifact that recorded it is a candidate for validity checking, regardless of what subsequently happens to what it depended on.

---

## 2. What Is the Architectural Meaning of Deletion?

**Deletion is the permanent cessation of a dependency's target's existence. It is not a form of modification, and it must never be evaluated as one.**

Modification and deletion produce fundamentally different kinds of answers to a validity check:

- **Modification** — the target still exists at check time, but its state differs from what was recorded. The check yields a determinate comparison: changed, and therefore invalidating, per the ordinary rule already established (ARCH-025 §4, §5).
- **Deletion** — the target no longer exists at check time to compare against at all. There is no "before" and "after" state to compare — there is a recorded state, and nothing.

This document establishes: **a deleted target's dependency is always, unconditionally invalidating.** It requires no comparison, because none is possible. This is stronger than an ordinary modification-based invalidation, which is merely determinate — deletion is *terminal*: the specific dependency that recorded a relationship to the now-deleted target can never again be confirmed stable, because the thing it depended on is permanently gone.

**Recreation is not restoration.** If something matching a deleted target's name or path later reappears — a file recreated with the same content, for instance — this document treats it as a new dependency target, never a return of the old one. This follows directly from §1: a dependency records a relationship to a *specific past state*, not a name or path. A recreated file has a new production context (at minimum, new modification metadata) and constitutes a distinct dependency, not a resurrection of the deleted one.

**Deletion does not apply uniformly across all five dependency shapes (ARCH-025 §3).** It applies cleanly where the shape's target is a discrete thing that can cease to exist:

| Dependency shape | Does deletion apply? | Basis |
|---|---|---|
| 1 — Source content | Yes | A file can be removed entirely, not merely changed |
| 2 — Derived-artifact | Yes | A persisted artifact (e.g., an index entry) can be removed, not merely superseded |
| 3 — Index/knowledge-state | Not distinctly | Aggregate state evolves continuously; this document does not force a distinction between a "deletion" of state and an ordinary modification of it — a future document may, but this one records the ambiguity rather than resolving it |
| 4 — Configuration/registration | Yes | A parser, connector, or model/provider registration can be withdrawn entirely, not merely reconfigured |

Leaving shape 3 unresolved is bounded, not silent: absent a future distinction, any change to aggregate index/knowledge-state — including a change that might colloquially be called "deletion," such as a full rebuild — falls through to the ordinary modification rule ARCH-025 §4/§5 already define. That rule still correctly invalidates every dependent artifact; it simply does not carry deletion's stronger, unconditional-and-irreversible consequence (§2). The gap is therefore a difference in strength of guarantee, never a lapse into unchecked or unspecified behaviour.

**Deletion belongs in ARCH-025 §4's "not currently observable" category for every shape it applies to.** Checked against ARCH-024 §9's real event catalogue (`DocumentDiscoveredEvent`, `DocumentIndexedEvent`, `DocumentParsedEvent`, `DocumentParsingFailedEvent`, `DocumentSkippedEvent`, `IndexingStartedEvent`, `IndexingCompletedEvent`, `IndexingFailedEvent`), no event signals a source, artifact, or registration ceasing to exist — only content changing or an index run completing or failing. Deletion is therefore an additional, distinctly-named gap in the same "not currently observable" list ARCH-025 §4 already maintains for shapes 1, 2, and 4 — not merely a new concept described alongside that list.
| 5 — Request-scoped input | Not applicable | A request's parameters are not a persistent entity that can be deleted; they simply are not repeated |

---

## 3. Which Dependency Shapes Participate in Validity?

All five shapes ARCH-025 §3 defines participate in validity determination — that is their defining role, stated there and not altered here. This document adds no sixth shape and removes none of the five.

What was not previously stated is that a given shape's participation is always exercised *for* a specific artifact of a specific validity class — a shape does not participate on its own; it participates because a member of some class carries it. §5 (the matrix) states which classes carry which shapes.

---

## 4. Which Validity Classes Participate in Dependency Evaluation?

- **Class A** (deterministically re-derivable) participates fully — its members are evaluated as dependency-shape targets in the ordinary sense ARCH-025 §1 and §4 already describe.
- **Class B** (non-deterministically produced) participates fully, on the same basis as Class A, per ARCH-025 §2's statement that "validity for this class rests entirely on dependency stability."
- **Class C** (primary/identity data) participates only as a *source* — something other classes' dependencies point to — never as a *target* being evaluated for its own validity. ARCH-025 §5 already establishes this ("Class C... is the source of invalidation, never its target"); this document does not change it, only reflects it into the matrix (§5).
- **Class D** (point-in-time observations) does not participate at all, in either direction. ARCH-025 §2 already excludes it from the validity model entirely.

---

## 5. The Validity-Class × Dependency-Shape Applicability Matrix

This is the canonical mapping AGR-001 F9 requested. Every "Applicable" and "Excluded" cell restates a real artifact ARCH-024 catalogues or a statement ARCH-025 already makes. Every "Indirect only" and "Source" cell is a logical extension built by combining those same established facts — for example, Class C's "Source" role for shape 3 connects `WorkspaceStateDto`'s documented state-tracking role (ARCH-024 §6) to shape 3's aggregate-state definition (ARCH-025 §3), a connection neither document states outright. No cell is asserted without a traceable basis, but "Indirect only" and "Source" cells are inferences this document draws, not direct restatements — that distinction is preserved throughout rather than presented as uniform citation.

| Class | Shape 1 (Source Content) | Shape 2 (Derived-Artifact) | Shape 3 (Index/Knowledge-State) | Shape 4 (Config/Registration) | Shape 5 (Request-Scoped) |
|---|---|---|---|---|---|
| **A** | Applicable — `AssetDescriptor`, `Document`, `ParseResult<Document>` | Applicable — `SearchResult` (on the keyword index), `ContextPackage` (on `SearchResult`) | Applicable — `SearchResult`, `ContextPackage` | Applicable — `Document` (parser registration) | Applicable — `SearchResult`, `ContextPackage` |
| **B** | Indirect only — Class B artifacts are not produced from raw source content directly; a chain to source content, if any, passes through a Class A artifact (shape 2) | Applicable — a Class B artifact may depend on a Class A artifact (e.g., a context package supplied to a model invocation) | Applicable — potentially direct, per the same reasoning ARCH-024 §5 records for model invocation context | Applicable — `ChatResponse`, `EmbeddingResult` ("the specific model and provider invoked," ARCH-024 §5) | Applicable — the prompt/request content that produced the artifact |
| **C** | Not applicable — Class C data is not file content and does not itself carry a source-content dependency | Not applicable as a target — per §4, Class C is never evaluated for its own validity | Source — `WorkspaceStateDto` contributes to the aggregate state Class A/B artifacts depend on (shape 3) | Source — `AiOptions`, `ConnectorInstance` are exactly what shape-4 dependencies reference | Not applicable — Class C artifacts are not produced in response to a request |
| **D** | Excluded | Excluded | Excluded | Excluded | Excluded |

**Reading the matrix:** "Applicable" means a real or architecturally anticipated member of that class carries that shape as a checkable dependency. "Indirect only" means the class never carries the shape directly but can inherit its effect transitively through a shape-2 chain (ARCH-029 §4's transitive closure). "Source" means the class is what other classes' dependencies point to, never itself evaluated. "Excluded" means the combination never occurs and must not be constructed.

---

## 6. Architectural Termination and Exclusion Rules

A dependency chain (ARCH-029 §2, §4) can end in exactly three ways. An implementation must recognize all three as distinct, not conflate them.

**1. Class D exclusion.** A chain never includes a Class D artifact, in either direction. Class D artifacts have no recorded dependencies to chase (they are not derived) and are never the target of another artifact's dependency (§4, §5). This is an absolute exclusion, not a terminal comparison.

**2. Class C termination — a leaf comparison, not a bypass.** A chain terminates upon reaching a Class C artifact, because Class C has no upstream dependency of its own to chase (§4). Termination here still requires a direct comparison of the Class C data's current state against what was recorded — reaching Class C is not an automatic "valid" shortcut. If the Class C data has changed, that comparison yields Not-satisfied for the link, exactly as ARCH-025 §4/§5 already establish for any other invalidation source.

**3. Deletion termination — unconditional invalidation, no comparison possible.** A chain terminates upon reaching a dependency whose recorded target has been deleted (§2). Unlike Class C termination, no comparison occurs here — there is nothing to compare against — and the result is always Not-satisfied for that link. Per ARCH-029 §6's outcome-combination rule, this unconditionally makes the entire candidate Not-satisfied, regardless of what any other link in the chain resolves to.

---

## 7. Invariants Every Implementation Must Preserve

- **The matrix (§5) is exhaustive and closed.** Every artifact ARCH-024 catalogues maps to exactly one validity class (already true per ARCH-025 §2) and exhibits only the dependency shapes this matrix marks Applicable, Indirect, or Source for that class. No implementation may construct a dependency shape for a class this matrix marks Not Applicable or Excluded.
- **Deletion is unconditional and irreversible per dependency instance.** No implementation may treat a recreated target as satisfying a prior dependency on a deleted one (§2). A recreated target is always a new dependency.
- **Class D exclusion is absolute.** No implementation may make a Class D artifact validity-checkable without amending ARCH-025 §2 through the governance process this series already establishes.
- **Class C termination always performs its comparison.** No implementation may treat reaching Class C as a shortcut that skips checking whether the Class C data itself has changed.
- **These rules apply uniformly regardless of chain depth or branching**, consistent with ARCH-029's transitive-closure model — a chain five links deep or with several branches is governed by exactly the same matrix, exclusion rules, and deletion semantics as one link.

---

## 8. Explicit Non-Goals

This document does **not** define:

- Any storage, retrieval, cache, or database mechanism
- Any API through which deletion, class, or shape information is queried or reported
- Any background process, scheduler, or polling mechanism for detecting deletion
- Any performance guarantee
- How a deletion signal would actually be produced (a gap this document records for shapes 1, 2, and 4 — parallel to the gap ARCH-025 §4 already records for ordinary modification of shapes 3 and 4)
- A resolution for dependency shape 3's deletion ambiguity (§2) — recorded, not resolved
- Any change to ARCH-025's four validity classes or five dependency shapes, beyond stating their combination (§5) and boundary behaviour (§2, §6)
- Any change to ARCH-029's propagation model or ARCH-027's resolution outcomes
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## 9. Proposed Amendments to the Frozen Foundation

These are proposals. Per AGR-001 §8, they do not take effect until a new Architecture Governance Review (AGR-004) accepts this document and confirms no Closed Architectural Decision is contradicted. Both insertion points below were verified against the live document before being written here.

### Amendment 1 — ARCH-025 §4 (Invalidation Sources) — resolves F6

**Current text, verbatim, final paragraph of §4:**
> "This asymmetry matters: a component that depends on parser registration, connector configuration, or model configuration cannot today be told when to reconsider its validity. This document does not resolve that gap — it is recorded so ARCH-026/ARCH-027 do not silently assume a signal exists where none does."

**Proposed addition, inserted immediately after that paragraph (no existing text removed):**
> "Deletion is architecturally distinct from modification: modification changes a dependency's target while it continues to exist; deletion means the target ceases to exist entirely, leaving nothing to compare against. Deletion belongs in this section's 'not currently observable' category for every shape it applies to — no event in the existing catalogue signals a source, artifact, or registration ceasing to exist. ARCH-030 defines deletion's architectural meaning and its unconditional, irreversible invalidation consequence in full."

### Amendment 2 — ARCH-025 §3 (Dependency Types That Determine Validity) — resolves F9

**Current text, verbatim, final paragraph of §3:**
> "One dependency shape found in the inventory resists this treatment: the Index Engine's fingerprint map (ARCH-024 §3) depends on 'the full history of prior index runs,' not a single checkable current state. This document records that as a distinct, harder case rather than forcing it into the five shapes above — a future document may need to address it, but this one does not invent a resolution."

**Proposed addition, inserted immediately after that paragraph (no existing text removed):**
> "The canonical mapping between these five dependency shapes and the four validity classes (§2) — which shapes apply to which classes, which apply only indirectly, and which classes participate only as a source or not at all — is defined in ARCH-030."

### Governance Requirement

This document must be reviewed by a new Architecture Governance Review (AGR-004) before Amendments 1 and 2 are applied to ARCH-025. Upon acceptance: ARCH-025 increments to v1.4, citing AGR-004; this document's own status changes from Draft to Frozen alongside it.

---

## Cross References

| Document | Relationship |
|---|---|
| [AGR-001 §5](../Reviews/AGR-001.md) | The deferred questions (F6, F9) this document resolves |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Source of every artifact cited as evidence in the matrix (§5) |
| [ARCH-025 §2, §3, §4](ARCH-025-Artifact-Validity-Model.md) | Amended by this document (§9, Amendments 1 and 2) |
| [ARCH-029 §2, §4, §6](ARCH-029-Validity-Propagation-Architecture.md) | Transitive-closure and outcome-combination model this document's termination rules (§6) plug into |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-03, RM-04 — this document is those roadmap items, batched |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Dependency Participation Semantics — resolves AGR-001 F6 and F9 together. Proposed amendments to ARCH-025 pending AGR-004. |
| 1.1 | 2026-07-03 | Ferret Core Team | AGR-004 review corrections — explicitly classified deletion into ARCH-025 §4's "not currently observable" category rather than describing it only as an adjacent concept (§2, §9 Amendment 1); added an explicit bounded-fallback statement for Shape 3's unresolved deletion distinction (§2); softened the matrix's grounding claim to distinguish direct citation from logical inference (§5). No change to the deletion model's substance or the matrix's conclusions. |
