# ARCH-033 — Ferret V2 Dependency Resolution Mechanism Design

| Field | Value |
|---|---|
| **Document ID** | ARCH-033 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — requires a Standard Architecture Review (`AR-`) per V2-ROADMAP-001 §7's Tier 3 governance; escalates to a new Architecture Governance Review only if a conceptual gap is discovered |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None yet — this document makes no storage, key, ranking, or format decision; expected future ADRs are enumerated in "Interaction With Future ADRs" |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-025 (Artifact Validity Model); ARCH-027 (Dependency Resolution Architecture) — the document this mechanism realizes; ARCH-028 (Request Equivalence Architecture); ARCH-029 (Validity Propagation Architecture); ARCH-030 (Dependency Participation Semantics); ARCH-032 (Persistence Mechanism Design) — the mechanism this document consumes |
| **Governed By** | ARCH-031 (Mechanism Architecture Principles) |
| **Roadmap Item** | [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) RM-08 |

---

## Purpose

ARCH-027 answers what dependency resolution is and what it must guarantee. It deliberately does not answer how a candidate is located or how a comparison is actually carried out. This document is that "how" for resolution, and — because V2-ROADMAP-001 §5 assigns retrieval to the same roadmap item as resolution — for the retrieval act ARCH-027 §1 keeps conceptually distinct from resolution itself.

This is not an implementation design and not an algorithm or data-structure evaluation. It names no key format, index structure, ranking function, cache, or API. Every one of those is a consequential, hard-to-reverse choice under ARCH-031 §6's test and therefore belongs to an ADR or to implementation, not to this document.

Every statement in this document answers **how the conceptual kernel is realized**. None answers what the conceptual kernel should be. Where realizing a requirement would require deciding something the kernel left open, this document says so and stops.

---

## Scope

Covers:
- The mechanism-level responsibilities a resolution design must fulfil, kept explicitly separate where ARCH-027 §1 separates resolution from retrieval (§1)
- What this mechanism consumes from ARCH-032 (Persistence Mechanism Design) and from the components ARCH-026 §3 names as dependency-signal sources (§2)
- What this mechanism produces (§3)
- The retrieval responsibility ARCH-027 assumes but does not itself define (§4)
- The comparison procedure, at the mechanism tier, that reaches one of ARCH-027 §3's three outcomes (§5)
- What resolution does not do (§6)
- The guarantees a resolution mechanism must satisfy (§7)
- How resolution specifically — as distinct from persistence — preserves the kernel's invariants (§8)
- The boundary with RM-07 (Persistence) and RM-09 (Surface Integration) (§9, §10)
- The implementation freedom this document deliberately leaves open (§11)

Does not cover, and will not decide:
- A key, index, or lookup structure of any kind
- A ranking, scoring, or similarity algorithm — ARCH-028 §4 already forbids anything but exact, contract-level equivalence, so there is no ranking problem for this document to solve or defer
- Schemas, tables, caches, or storage technology (that is ARCH-032/RM-07's or an ADR's concern)
- Serialization formats or encodings
- APIs of any kind, public or internal
- Any redefinition of an artifact (ARCH-024), a validity concept (ARCH-025), a persistence requirement (ARCH-026), a resolution outcome (ARCH-027), request equivalence (ARCH-028), a propagation rule (ARCH-029), or dependency participation semantics (ARCH-030)
- Any redefinition of a persistence responsibility already fixed by ARCH-032
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every requirement, outcome, and principle referenced below is taken as-is from ARCH-025, ARCH-027, ARCH-028, ARCH-029, ARCH-030, and ARCH-032. This document introduces no new artifact, validity class, dependency shape, resolution outcome, equivalence rule, propagation rule, or component. Ownership follows ARCH-023's Data Ownership principle exactly as already stated. Where this document states a mechanism-level rule not spelled out verbatim in the kernel, its derivation from an existing principle is shown at the point it is introduced.

---

## 1. Resolution and Retrieval Responsibilities

ARCH-027 §1 keeps resolution and retrieval conceptually distinct: retrieval is "the physical act of finding and fetching an artifact from wherever it is held"; resolution is "the reasoning that happens once a candidate artifact is already in view." V2-ROADMAP-001 §5 assigns both to this roadmap item (RM-08) as a single mechanism document, but that scheduling choice does not collapse the concepts into one. This document keeps them in separate tables throughout.

| Retrieval responsibility | Realizes |
|---|---|
| **Locate** — given a request's identity (ARCH-028 §2), determine whether a persisted candidate exists that was produced for an equivalent request | ARCH-028 §7; ARCH-027 §4 |
| **Fetch** — obtain that candidate's exposed dependency state, artifact output (if durable), and request-identity record from ARCH-032's persistence mechanism | ARCH-032 §5 (Outputs) |

| Resolution responsibility | Realizes |
|---|---|
| **Compare** — for the fetched candidate, evaluate each recorded dependency (ARCH-025 §3) against current state to determine whether it still holds | ARCH-027 §1, §3 |
| **Combine** — where the candidate's dependency chain includes another artifact, combine per-link outcomes into one candidate-level outcome | ARCH-029 §6 |
| **Report** — return exactly one of Satisfied, Not-satisfied, or Indeterminate (ARCH-027 §3) to the engine that initiated resolution | ARCH-027 §2, §3 |

**Neither responsibility decides what to do with the outcome.** Per ARCH-023 §9 and ARCH-027 §1, the decision to reuse or recompute remains entirely the owning engine's. Resolution answers a question; it never acts on the answer.

---

## 2. Inputs

This mechanism consumes, and produces nothing on its own initiative from, the following:

- **The current request's identity** — the three properties ARCH-028 §2 defines (engine responsibility invoked, explicit parameter set, ambient dependency scope), supplied by the engine initiating resolution.
- **Persisted state exposed by ARCH-032** — per ARCH-032 §5: the complete recorded dependency state for any persisted candidate, the candidate's durable artifact output where one exists, its recorded request-identity properties, and whether its record is readable, complete, and uncorrupted.
- **Current dependency state** — the present-day state of whatever ARCH-026 §3 names as the source of each dependency shape: Index Engine for source-content and index/knowledge-state facts, Parser Platform and Connector Platform for configuration/registration facts, Workspace Engine for Class C primary data a chain may terminate against (ARCH-030 §6).

This mechanism never reads any component's internal state beyond the dependency signals that component already exposes (ARCH-025 §7; ARCH-027 §2) — it does not infer, and it does not maintain a shadow copy of, any owning component's own domain state.

---

## 3. Outputs

For a given request and, where retrieval located one, a given candidate, this mechanism produces exactly one of ARCH-027 §3's three outcomes:

- **Satisfied** — every recorded dependency the candidate's persisted state carries matches current state, and no chain link resolves to anything but Satisfied (ARCH-029 §6).
- **Not-satisfied** — at least one recorded dependency has changed, at least one chain link resolves to Not-satisfied, or retrieval located no candidate at all (ARCH-027 §4's default).
- **Indeterminate** — the persisted state needed to evaluate the candidate, or the current request's own dependencies, cannot be established (ARCH-026 §7; ARCH-027 §3), and no link resolves to Not-satisfied.

The outcome is always scoped to the one candidate against the one request that produced it (ARCH-027 §3). This mechanism never produces, or implies, an outcome for any other artifact.

---

## 4. The Retrieval Responsibility ARCH-027 Assumes

ARCH-027 §4 states plainly that resolution "does not search for, rank, or select among a universe of candidate artifacts — that would be retrieval." It assumes a candidate is already identifiable from the request. This document is where that assumption is finally realized: given a request's identity (ARCH-028 §2), retrieval must determine whether a persisted candidate exists whose recorded request identity is **equivalent** to it, per ARCH-028 §3's exact, contract-level relation — never approximate, ranked, or partial (ARCH-028 §4).

This is a lookup, not a search. ARCH-028 §4 forbids recognizing any partial or approximate form of equivalence; a mechanism that ranked candidates by similarity, or that treated the "closest" request as good enough, would not be realizing ARCH-028 — it would be inventing a form of equivalence the kernel explicitly excludes. Retrieval's only job is to answer, for a given request, "is there exactly one persisted candidate produced for a request this exact relation confirms as the same one" — never "which of several candidates is close enough."

Where no candidate is equivalent, retrieval reports that fact plainly; per ARCH-027 §4, this is a request-identification fact, not a resolution failure, and yields Not-satisfied by default (§3).

---

## 5. Comparison Procedure

Once retrieval (§4) has identified a candidate, comparison proceeds as follows, entirely as an elaboration of what ARCH-025, ARCH-029, and ARCH-030 already require — this document invents no additional step:

1. For each dependency shape the candidate's persisted state records (ARCH-025 §3, realized by ARCH-032 §2.1), compare the recorded state against current state (§2 of this document).
2. Where a recorded target has been deleted (ARCH-030 §2), the comparison for that link is unconditionally Not-satisfied — no further comparison of that link is performed or possible (ARCH-030 §6, termination rule 3).
3. Where a chain reaches a Class C artifact (ARCH-030 §4, §6), the comparison performs a live check against that artifact's own current, authoritative state — never a shortcut, and never a comparison against a value copied into the candidate's own record (ARCH-032 §7.10 explains why persistence never allows that copy to exist to compare against in the first place).
4. A chain never extends to or through a Class D artifact (ARCH-030 §6, termination rule 1) — this is an absolute exclusion, not a comparison that returns a result.
5. Per-link results combine per ARCH-029 §6: Satisfied only if every link is Satisfied; Not-satisfied if any link is; Indeterminate otherwise.
6. The entire comparison is performed fresh, at the moment of the check (ARCH-029 §1) — this mechanism never treats a previous comparison's result, or any precomputed verdict, as still current. ARCH-032 §4 already forbids persistence from storing one; this document independently forbids resolution from producing or reusing one across separate resolution calls, for the same reason: doing so would be indistinguishable from the eager propagation ARCH-029 §3 rules out.

---

## 6. What Resolution Does Not Do

- **Resolution does not detect invalidation.** It interprets already-detected or already-recorded change (ARCH-027 §1); it never independently watches for a change to occur.
- **Resolution does not persist anything.** It never writes to, marks as superseded, or removes any record ARCH-032 governs — persistence's lifecycle (ARCH-032 §3) is untouched by a resolution call.
- **Resolution does not decide to reuse or recompute.** That decision, and the recomputation itself, remain entirely the owning engine's (ARCH-023 §9; ARCH-027 §1).
- **Resolution does not invoke `IModelProvider`.** Per ARCH-023 §9, this is true regardless of which engine's artifact is being resolved.
- **Resolution does not rank, score, or select among approximate candidates.** Per ARCH-028 §4, there is no "closest match" concept for this mechanism to implement.
- **Resolution does not resolve another engine's artifacts using that engine's internal state.** Per ARCH-025 §7 and ARCH-027 §2, only the dependency signals a component already exposes are consulted.

---

## 7. Resolution Guarantees

| Guarantee | Statement | Basis |
|---|---|---|
| Determinism | The same request and the same persisted and current dependency state always produce the same outcome | ARCH-027 §5; AG-004 |
| No side effects | Resolution never mutates persisted state, never invokes `IModelProvider`, and never performs the owning engine's work | ARCH-023 §9; ARCH-027 §5 |
| No cross-engine inference | Resolution for one engine's artifacts never depends on another engine's internal state beyond signals that engine already exposes | ARCH-025 §7; ARCH-027 §2 |
| Fail-closed | An Indeterminate outcome is never treated as Satisfied | ARCH-026 §7; ARCH-027 §5 |
| Minimality | A Not-satisfied outcome is scoped to the minimum affected candidate the dependency graph supports, never broadened by category, ownership, or proximity | ARCH-025 §5; ARCH-027 §5 |
| No new source of truth | Resolution consults only dependency state and artifacts already owned by V1 components; it maintains no independent record of what's valid | ARCH-023 §4; ARCH-027 §5 |
| Exact equivalence only | A candidate is considered only if its recorded request identity is equivalent, per ARCH-028 §3's exact relation, to the current request — never approximately | ARCH-028 §3, §4 |
| Transitive closure | A candidate's full dependency chain is checked, not merely its directly-recorded dependencies | ARCH-029 §4 |
| No stale positive | A candidate is never reported Satisfied while any link in its chain is itself invalid | ARCH-029 §4 |
| Order-independence | Given the same chain state, the outcome does not depend on the order in which links happen to be evaluated | ARCH-029 §4 |

---

## 8. Mechanism-Level Invariants — Preserving Conceptual Guarantees Through Resolution

**8.1 Fail-closed (ARCH-025 §8; ARCH-026 §7; ARCH-027 §5, §6).** Realized by §3's Indeterminate outcome and §7's fail-closed guarantee: an unreadable, incomplete, or missing persisted record (as surfaced by ARCH-032 §5's integrity signal) is turned into Indeterminate, never Satisfied.

**8.2 Minimum invalidation (ARCH-025 §5; ARCH-027 §5).** Realized by §3: an outcome is always scoped to one candidate against one request; this mechanism never broadens a Not-satisfied result to other artifacts sharing a type, owner, or category.

**8.3 No hidden side effects (ARCH-023 §9; ARCH-027 §5).** Realized by §6: comparison and combination are read-only operations over persisted and current state; nothing about performing a resolution call writes anything, anywhere.

**8.4 No silent recomputation (ARCH-031 §3, §8).** Realized by §3 and §6: resolution never triggers recomputation itself — it reports an outcome, and per ARCH-027 §6, the owning engine's fallback to recomputation is always an explicit, visible consequence of a Not-satisfied or Indeterminate outcome the engine can see and act on, never a silent substitution this mechanism performs on the engine's behalf.

**8.5 Exact request equivalence (ARCH-028 §3, §4).** Realized by §4: retrieval recognizes exactly one form of equivalence and none other. A mechanism that introduced ranking or partial matching would not be a more capable realization of ARCH-028 — it would be a different, unauthorized architecture.

**8.6 Transitive, point-in-time propagation consistency (ARCH-029 §1, §4, §6).** Realized by §5, step 5 and step 6: chains are evaluated in full and fresh at each check; no precomputed or cached verdict is substituted for a live comparison.

**8.7 Deletion is unconditional and irreversible per dependency instance (ARCH-030 §2, §6, §7).** Realized by §5, step 2: a deleted target's link is Not-satisfied unconditionally, with no comparison performed and no possibility of a later "recreated" target satisfying it (ARCH-030 §2).

**8.8 Class D exclusion is absolute; Class C termination always compares (ARCH-030 §4, §6, §7).** Realized by §5, steps 3 and 4: a chain never extends through Class D, and a chain terminating at Class C always performs a live comparison against that component's current, authoritative state — never a shortcut.

**8.9 No new source of truth (ARCH-023 §4; ARCH-027 §5).** Realized by §2 and §6: this mechanism reads dependency signals and persisted state that V1 components and ARCH-032 already expose; it maintains no parallel record of what is valid that could diverge from them.

---

## 9. Boundary With RM-07 (Persistence)

This is the mirror of ARCH-032 §8, stated from the resolution side:

- This mechanism never writes to, supersedes, or removes anything ARCH-032 governs. It consumes exactly what ARCH-032 §5 exposes and nothing more — it does not reach into a component's storage independently of what that component's persistence mechanism exposes.
- This mechanism never decides retention, eviction, or physical disposition of a superseded record (ARCH-032 §3) — that remains entirely ARCH-032/RM-07's and, ultimately, an ADR's concern.
- Where ARCH-032 reports a record as unreadable or corrupted, this mechanism's only obligation is to turn that fact into Indeterminate (§3, §8.1) — it never attempts to reconstruct, repair, or bypass a persistence-layer failure itself.
- This mechanism assumes ARCH-032's guarantees (its §6) and invariants (its §7) hold. It does not assume any specific storage technology, format, or structure, since ARCH-032 decides none of those.

---

## 10. Boundary With RM-09 (Surface Integration)

- This mechanism has no visibility into, and makes no assumption about, how or whether its outcome ever reaches a CLI or MCP surface. That is entirely ARCH-034/RM-09's concern.
- This mechanism reports its outcome only to the engine that initiated resolution (ARCH-027 §2) — never directly to a surface artifact (`CommandResult`, `McpToolResult`, or similar, per ARCH-024 §7). Any surface exposure happens only through the owning engine's own existing surface integration, unchanged by this document.
- This mechanism never produces, and is never asked to produce, any CLI- or MCP-facing representation of its outcome. Doing so would be defining an API, which this document does not do.

---

## 11. Implementation Freedom Remaining

After this document, at least the following remain entirely open, to be settled by an ADR or by RM-09 where applicable:

- The key, index, or lookup structure used to perform retrieval (§4)
- The internal data structure or algorithm used to perform comparison and combination (§5), provided the guarantees in §7 hold
- In-memory computation strategy during a single resolution call (e.g., how intermediate per-link results are held while a chain is evaluated) — provided no such intermediate state is persisted as a verdict across separate resolution calls (§5, step 6; ARCH-032 §4)
- How retrieval and comparison are packaged into one or more internal operations, procedures, or components, as long as the retrieval/resolution conceptual distinction (§1) remains visible in the design's own documentation
- Performance characteristics and optimization strategy, subject only to not weakening a §7/§8 guarantee to achieve them

This document does not decide, and does not authorize any future ADR to decide unilaterally, how a deletion signal is produced (ARCH-032 §9 already records this as an unresolved conceptual gap, not a freedom, for the persistence side of the same question) — a resolution mechanism that later needs such a signal to evaluate a deletion-sensitive link inherits that same open gap and must escalate rather than invent one.

---

## Relationship to the Conceptual Kernel

This document adds nothing to the frozen kernel and amends none of ARCH-023 through ARCH-030. It realizes ARCH-027's resolution concept and outcomes, ARCH-028's equivalence relation, ARCH-029's propagation and combination model, and ARCH-030's deletion and matrix semantics, exactly as each already states them, and consumes ARCH-032's persistence mechanism exactly as ARCH-032 exposes it. Where this document states a rule not verbatim in the kernel — the retrieval-is-a-lookup-not-a-search framing (§4) and the no-cross-call-verdict-reuse rule (§5, step 6) chief among them — each is shown, at the point it is introduced, to be a direct corollary of an existing kernel principle.

---

## Interaction With RM-07

RM-08 (this document) requires RM-07 (ARCH-032) to be complete before it can proceed, per V2-ROADMAP-001 §5's entry criteria for RM-08. This document assumes ARCH-032's guarantees and invariants hold and decides nothing about how persistence is realized internally. Where this document discovers that a guarantee it needs cannot be upheld by anything ARCH-032 exposes, that is a gap in ARCH-032 or in the frozen kernel, not license for this document to read persisted state some other way — per ARCH-031 §9, this document must halt and escalate rather than silently work around it.

---

## Interaction With RM-09

RM-09 (ARCH-034) requires RM-08 (this document) to be complete before it can proceed, per V2-ROADMAP-001 §5's entry criteria for RM-09. RM-09 may assume this document's outcomes (§3) and guarantees (§7) but may not assume any specific retrieval or comparison mechanism, since none is decided here (§11).

---

## Interaction With Future ADRs

Per ARCH-031 §6's test, the following are expected to be recorded as ADRs, following the existing convention already established in this repository (`docs/adr/`, per ADR-0001):

- The key or lookup structure chosen for retrieval — this belongs wholly to this document (RM-08), not jointly with ARCH-032/RM-07, per V2-ROADMAP-001 §5's assignment of "retrieval approach, keys" to RM-08's exit criteria alone and per ARCH-032 §8's boundary table
- The internal data structure and algorithm chosen for comparison and chain combination
- The in-memory computation strategy chosen for evaluating a chain during a single resolution call

Each such ADR must state which of this document's guarantees (§7) and invariants (§8) it upholds and how.

---

## Conformance With ARCH-031

| ARCH-031 §7 requirement | Satisfied by |
|---|---|
| Guarantee-by-guarantee trace | §8, tracing nine kernel invariants individually |
| Responsibility trace | §1 (Retrieval and Resolution Responsibilities), §9, §10 |
| Ownership trace | §2 (Inputs); no new owning component introduced anywhere in this document |
| Explicit non-goals | Scope ("Does not cover"), §6, §11 |
| Statement of ADRs produced | Interaction With Future ADRs — none produced by this document itself; three anticipated |
| Confirmation no Closed Architectural Decision is contradicted | See Impact on Existing Architecture, below |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-027's resolution concept, outcomes, and guarantees; ARCH-028's equivalence relation; ARCH-029's transitive-closure and combination rules; ARCH-030's deletion and matrix semantics; and ARCH-032's persistence outputs — all without modification.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behavior to any V1 component.

**Existing components intentionally unchanged.** All of them. Every ownership assignment, outcome definition, and gap from ARCH-024 through ARCH-032 remains exactly as those documents left it.

**New concepts introduced.** None at the conceptual tier. Two mechanism-tier corollaries are introduced, both derived directly from existing kernel principles: the retrieval-is-a-lookup-not-a-search framing (§4 — a direct consequence of ARCH-028 §4's exclusion of partial equivalence) and the no-cross-call-verdict-reuse rule (§5, step 6 — a direct consequence of ARCH-029 §1 and §3, applied to resolution the same way ARCH-032 §4 applies it to persistence). Neither is a decision about what the kernel should be.

**Closed Architectural Decisions.** All nine (AGR-001 §6) checked individually against this document's text; none is contradicted, narrowed, or reinterpreted.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Data Ownership principle; no-side-effects basis for §6, §7 |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Dependency shapes and fail-closed principle this document compares against |
| [ARCH-026](ARCH-026-Persistence-Requirements.md) | Source of the fail-closed-on-failure principle carried into §7, §8.1 |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Parent — the resolution concept, outcomes, and guarantees this mechanism realizes |
| [ARCH-028](ARCH-028-Request-Equivalence-Architecture.md) | Source of the exact equivalence relation §4 and §8.5 realize |
| [ARCH-029](ARCH-029-Validity-Propagation-Architecture.md) | Source of the transitive-closure and combination model §5 and §8.6 realize |
| [ARCH-030](ARCH-030-Dependency-Participation-Semantics.md) | Source of the deletion and matrix-termination rules §5 and §8.7–§8.8 realize |
| [ARCH-031](ARCH-031-Mechanism-Architecture-Principles.md) | Governing document — the evidentiary standard and invariant checklist this document is written to satisfy |
| [ARCH-032](ARCH-032-Persistence-Mechanism-Design.md) | The persistence mechanism this document consumes (§2, §9); sibling mechanism document in the same package |
| [AGR-001](../Reviews/AGR-001.md) | Source of the nine Closed Architectural Decisions confirmed unaffected (Impact, above) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-08 — this document is that roadmap item; RM-09's entry criteria depend on this document's completion |
| `docs/adr/README.md`, [ADR-0001](../adr/0001-use-architecture-decision-records.md) | Existing ADR process and location this document defers its consequential technology choices to |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Dependency Resolution Mechanism Design — realizes ARCH-027, ARCH-028, and ARCH-029 at the mechanism tier ARCH-031 defines, consuming ARCH-032. Pending Standard Architecture Review. |
