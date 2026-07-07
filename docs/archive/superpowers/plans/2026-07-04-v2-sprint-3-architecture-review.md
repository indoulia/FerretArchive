# V2 Sprint 3 — Architecture Review

| Field | Value |
|---|---|
| **Date** | 2026-07-04 |
| **Scope** | Current-state analysis after Sprint 2 (approved) and ARCH-037 (Dependency Graph Mechanism), and forward milestone planning for Sprint 3 |
| **Status** | Draft — for review; no implementation authorized by this document |
| **Author's note** | Repository-first: every claim below is traced to a specific doc section or file:line found in the current working tree, not to prior conversation |

---

## 0. Method

This review reads, and does not re-derive, three independent evidence sources:

1. The governing mechanism documents: `ARCH-031` (Mechanism Architecture Principles), `ARCH-032` (Persistence Mechanism Design), `ARCH-033` (Dependency Resolution Mechanism Design), `ARCH-037` (Dependency Graph Mechanism).
2. The governance record: the Sprint 2 Architecture Review (`docs/superpowers/plans/2026-07-04-v2-sprint-2-architecture-review.md`), `V2-IMPLEMENTATION-BACKLOG-001`, `V2-ROADMAP-001-Architecture-Program`.
3. The actual current code: `src/Ferret.Persistence/`, `tests/Ferret.Persistence.Tests/`, `tests/Ferret.Architecture.Tests/PersistenceArchitectureTests.cs`, `src/Ferret.VerticalSlice/VerticalSliceDriver.cs`.

Where the code and the docs agree, that is called out as validated. Where they diverge, that divergence is itself a finding — one such divergence is recorded in §1.

This document does not redefine ARCH-037. It designs no mechanism and makes no conceptual decision; it only sequences implementation work against a mechanism ARCH-037 already specifies, the same relationship the Sprint 2 review held to ARCH-032/033.

---

## 1. Sprint 3 Current-State Analysis

**No Sprint 3 implementation exists.** A repository-wide search for `DependencyGraph`, `GraphNode`, and `GraphEdge` under `src/` returns no match — ARCH-037's Graph Node, Graph Edge, and materialization procedure are architecture text only, not yet code.

**The structure ARCH-037 formalizes already exists, privately, in `ResolutionCheck`.** `src/Ferret.Persistence/ResolutionCheck.cs` contains `CompareChainAsync` → `CompareLinksAsync` → `CompareLinkAsync`, a recursive traversal that follows each `DependencyReference` in a `DependencyChain` through `IDependencyStateStore.GetRecordAsync`, tracking a `HashSet<(string EngineResponsibility, string RequestPath)> visited` to fail-closed (`Indeterminate`) on a cycle, and returning `Indeterminate` for a referenced record that cannot be fetched (`ResolutionCheck.cs:116–157`). This is exactly the traversal ARCH-037 §4 and its "Repository-First Method" section cite as the thing being generalized — confirmed byte-for-byte against the document's own description, not just by name.

**This traversal is live, not vestigial.** `src/Ferret.VerticalSlice/VerticalSliceDriver.cs:141–153` calls `ResolutionCheck.Compare`, `CompareConfiguration`, and `CompareChainAsync`, then combines all three via `ResolutionCheck.Combine`. `ResolutionCheck` is the single production consumer of `DependencyChain` today — any graph-mechanism work in Sprint 3 has exactly one existing caller to reason about, not several.

**The persistence layer ARCH-037 depends on is complete and stable.** `FileDependencyStateStore` (S2-2 through S2-9, ADR-0022/0023/0024) is the sole implementation of `IDependencyStateStore`, and ARCH-037 §4 requires materialization to use only `GetRecordAsync` — a method that already exists, unchanged, with no new query shape needed. Sprint 3 requires no new persistence work.

**The Sprint 2 architecture-regression convention is established and reusable.** `tests/Ferret.Architecture.Tests/PersistenceArchitectureTests.cs` (S2-0) encodes five reflection-based invariants in the same style as `ConnectorArchitectureTests.cs` — no IL-inspection library, no second test framework. This is the pattern Sprint 3's own regression protection should extend, per the "Milestone design principles" this review is scoped to follow.

**One naming collision, confirmed not a migration candidate.** `src/Ferret.Runtime/Registry/ModuleDependencyGraph.cs` also performs a recursive, visited-set traversal (`Visit`, `visited`, `inStack` — a topological sort for module load order). It shares no code, no owning component, and no concept with ARCH-037: it orders V1 module boot dependencies inside `Ferret.Runtime`, not V2 artifact-reuse dependency chains inside `Ferret.Persistence`. ARCH-023's Data Ownership principle and ARCH-037's "no ninth component" confirmation both rule this out as a consolidation target. It is named here only so a future reader does not mistake the coincidental name match for related work.

**Finding — governance status discrepancy.** ARCH-037's own document header (`docs/002-Architecture/ARCH-037-Dependency-Graph-Mechanism.md:7,10`) still reads `Status: Draft` / `Review Status: Pending — requires a Standard Architecture Review`. No `AR-` record in `docs/Reviews/` (the index in `docs/Reviews/README.md` lists only AR-001, AGR-001–004) approves ARCH-037. Per V2-ROADMAP-001 §7, a Tier 3 mechanism document requires that Standard Architecture Review before implementation proceeds. This review takes ARCH-037's approval as a given per the task framing it was issued under, but the repository itself does not yet record that approval — closing this gap (or confirming it was closed out-of-band) is listed as a prerequisite in §3 and §5, not assumed away.

---

## 2. Technical Debt Review

### Acceptable as-is

- **`ResolutionCheck`'s private recursive traversal.** Correct, tested, and explicitly not required to change by ARCH-037 (§9: *"`ResolutionCheck.CompareChainAsync`'s existing private traversal... is not required to change as a result of this document"*). Leaving it untouched is the architecturally sanctioned default, not deferred cleanup.
- **No actual duplication exists yet.** ARCH-037's stated motivation is prospective — *"each [future capability] would otherwise reimplement `ResolutionCheck.CompareChainAsync`'s traversal privately"* (Purpose, ARCH-037). Today there is exactly one traversal and one consumer. There is no second implementation to de-duplicate. This changes how urgently any consolidation milestone should be prioritized (§3, §4).

### Nothing carried over from Sprint 2 as still open

The Sprint 2 review's Technical Debt Review (§3 of that document) listed six items; all six were assigned to S2-1A through S2-9, and this repository's current state (`FileDependencyStateStore`, dependency-inverted `VerticalSliceCommandHandler`, the relocated `Ferret.VerticalSlice` assembly, `PersistenceArchitectureTests`) is consistent with all of them being closed. No unresolved Sprint 2 debt item carries into this review.

---

## 3. Risk Assessment

- **Scope-creep into explainability, impact analysis, rebuild planning, or visualization.** ARCH-037 §9 excludes all four by name. A milestone framed as "graph diagnostics" or "graph consumers" is, on inspection, one of these four wearing a different label — none may be scheduled without its own architecture document first, per the task's own instruction. This is the same category of risk the Sprint 2 review flagged for deletion detection (§5 of that document): a natural-seeming next step that is actually blocked pending its own governance track.
- **Premature `ResolutionCheck` migration.** ARCH-037's own "Interaction With ARCH-033" section requires that *if* `ResolutionCheck` is migrated onto the new mechanism, ARCH-033's guarantee-by-guarantee trace (nine invariants, §8 of that document) must be reconfirmed. Attempting this now, with no second consumer yet requiring it, pays that reverification cost for a DRY improvement alone — a real but non-urgent debt (§2), not a defect. This is a YAGNI risk, not an architecture-conformance risk: doing it later, once a second consumer exists, costs the same reverification either way.
- **Reverse-traversal temptation.** ARCH-037 §9 is explicit that impact analysis ("what depends on this identity") needs "its own architectural treatment of what state, if any, makes reverse lookup possible" — this mechanism is forward-only by design (§2, §3 of ARCH-037). A Sprint 3 milestone that quietly adds a reverse index or a "dependents of" query would be extending the mechanism past what this document authorizes.
- **Fixture realism for cycle and unavailable-node tests.** ARCH-037 §5's determinism invariant and §6/§7's cycle- and unavailable-node-handling requirements need dependency chains that branch, cycle, and reference missing records — the current Sprint 1/2 vertical-slice corpus is a single file with, at most, a single-shape chain. Sprint 3's test plan needs synthetic multi-record fixtures (a fake or seeded `IDependencyStateStore`, not the real filesystem corpus) to exercise these paths at all; this is a test-design task, not an architecture question, but it will not fall out of existing fixtures for free.
- **Governance prerequisite risk.** Per the Finding in §1, no `AR-` record yet approves ARCH-037. Starting implementation without that review recorded would repeat the exact process gap V2-ROADMAP-001 §7 exists to prevent for every other Tier 3 document (ARCH-032, ARCH-033 each have "Review Status: Pending" language identical to ARCH-037's, and neither has shipped without a corresponding governance record referenced from `docs/Reviews/`).

---

## 4. Proposed Sprint 3 Milestones

Each milestone introduces exactly one architectural responsibility, per this review's design principles. Capability milestones are preferred over granular slices where the slices would not be independently reviewable — S3-1 below is intentionally one capability (materialize a graph correctly) rather than three unreviewable fragments (define a node type; define an edge type; write a loop).

| # | Milestone | Concept | Depends on |
|---|---|---|---|
| S3-0 | Regression protection for existing contracts | Encode, as reflection-based architecture tests, that introducing the graph mechanism changes nothing about `ResolutionCheck`'s or `IDependencyStateStore`'s existing shape | none |
| S3-1 | Dependency Graph materialization | `DependencyGraph`, `GraphNode`, `GraphEdge` (ARCH-037 §1) and the deterministic materialization procedure (§4), including cycle handling (§6) and unavailable-node handling (§7) | S3-0 |
| S3-2 | Graph structural-invariant regression tests | Permanent architecture-level tests guarding ARCH-037 §5's five invariants against future erosion (e.g., a later change adding a validity field to `GraphNode`) | S3-1 |

**Explicitly not proposed for Sprint 3, and not numbered above:** migrating `ResolutionCheck` onto `DependencyGraph`. See §5.

### 4.1 S3-0 — Invariants Protecting Existing Contracts

Sequenced first, mirroring the Sprint 2 review's own rationale for S2-0: these checks must be green **before** S3-1 begins, as a baseline proving the graph mechanism is additive, not a silent rewrite of what Sprint 2 already shipped. Extends `PersistenceArchitectureTests.cs` (no new file required unless preferred for isolation).

| Invariant | Protects | Check (reflection-based) | Expected state today |
|---|---|---|---|
| `IDependencyStateStore` still declares exactly `GetRecordAsync` and `SetRecordAsync`, no third method | ARCH-037 §4: "introduces no new interface, storage call, or query shape" | `typeof(IDependencyStateStore).GetMethods().Length == 2` (or equivalent named-method check) | Green today — baseline before S3-1 |
| `ResolutionCheck`'s public method set (`Compare`, `CompareConfiguration`, `Combine`, `CompareChainAsync`) is unchanged | ARCH-037 §9: "`ResolutionCheck.CompareChainAsync`'s existing private traversal... is not required to change" | Enumerate `typeof(ResolutionCheck)`'s public static methods and assert the known set, by name and signature | Green today — guards against S3-1 accidentally touching resolution while building the graph |
| No type in the assembly containing the future graph mechanism references `ResolutionOutcome` | ARCH-037 §1, §5: graph vocabulary contains no resolution/validity vocabulary | Reflection scan of field/property/parameter/return types reachable from the graph types for `typeof(ResolutionOutcome)` | Vacuously green today (no graph types exist yet); becomes a meaningful gate the moment S3-1 adds them — written now so S3-1's diff cannot introduce a violation unnoticed |

The third row is deliberately written against types that do not exist yet — it is a gate the S3-1 diff must pass, not a check with something to fail today. This is the correct sequencing per the milestone design principle that regression protection should exist before the code it protects, exactly as Sprint 2's S2-0 preceded S2-1A/S2-1B.

### 4.2 S3-1 — Dependency Graph Materialization

Realizes ARCH-037 §1–§7 in full: the three new types, the recursive materialization procedure (§4), and both structural annotations (cycle-closing edges, §6; Unavailable nodes, §7). TDD-first per the standing project convention — failing tests demonstrating each of §5's five invariants, §6's cycle behavior, and §7's lossless-unavailable behavior, against a seeded/fake `IDependencyStateStore` (per the fixture-realism risk in §3), before the materialization procedure is written.

**Acceptance criteria**, traced directly to ARCH-037's own text so this milestone's review can check the same evidence ARCH-037 itself demands of a mechanism document (§7 of ARCH-031, applied here at the implementation tier per ARCH-036 §1's conformance definition):

- Materializing the same root against the same persisted state twice produces structurally identical graphs (§5, "Deterministic construction").
- A `Graph Node`/`Graph Edge`, once constructed, exposes no mutator — immutability is a compile-time property of the type, not a runtime check (§5, "Immutable graph").
- Two references to the same request identity within one materialization resolve to the same node object, not two equal-but-distinct ones (§5, "No duplicate nodes").
- A `Graph Node`/`Graph Edge` exposes no property beyond identity, materialization state, and (for edges) the cycle flag — no field is added that could be mistaken for a validity or resolution judgment (§5, "No derived semantic state").
- A reference cycle back to an already-visited identity in the same materialization is represented as a flagged edge, completes without error, and does not recurse unboundedly (§6).
- A reference to a record `IDependencyStateStore.GetRecordAsync` cannot return is represented as an explicit `Unavailable` node with the reference preserved as an edge, never silently dropped (§7).
- Materialization performs no write — it calls only `GetRecordAsync`, never `SetRecordAsync`, anywhere in its call graph (S3-0's first invariant already guards the interface shape; this criterion is the corresponding behavioral check for the new code).

### 4.3 S3-2 — Graph Structural-Invariant Regression Tests

Once S3-1 exists, the S3-0-style reflection checks that were vacuous become meaningful and get their own pass: confirm no `ResolutionOutcome`-shaped vocabulary reaches the graph types (closing the gate S3-0 opened), confirm `GraphNode`/`GraphEdge`/`DependencyGraph` expose only `init`-only properties (no public setter, reflection-checkable), and confirm the type or method that performs materialization has no reachable call to `IDependencyStateStore.SetRecordAsync`. This is protection against *future* erosion (a later milestone adding a "cached" flag, say), the same role Sprint 2's S2-0 rows 3–5 played for `RequestEquivalence`/`ResolutionCheck`/`IDependencyStateStore` after S2-1 landed.

---

## 5. Recommended Stopping Points

**Stop after S3-2.** At that point the Dependency Graph Mechanism exists as reusable, independently tested infrastructure exactly as ARCH-037 specifies it, with permanent regression protection in place, and `ResolutionCheck` is untouched and still the sole, already-correct consumer of dependency chains.

**Do not schedule a `ResolutionCheck` migration inside Sprint 3 by default.** Per §3's risk analysis, migrating `ResolutionCheck` to consume `DependencyGraph` is optional cleanup with a real reverification cost (ARCH-033's nine guarantees) and no current second consumer to justify paying it now. If a future sprint introduces the first real second consumer (explainability, impact analysis, or rebuild planning — each requiring its own new architecture document first, per §6), that is the natural point to revisit the migration, reconfirming ARCH-033's trace as ARCH-037 itself requires. Proposing it speculatively here would be designing for a hypothetical future requirement rather than a scheduled one.

Before S3-0 begins: close, or explicitly accept as a tracked gap, the governance-status discrepancy in §1 (no `AR-` record yet approves ARCH-037). This mirrors the exact gate V2-ROADMAP-001 §7 already applies to ARCH-032 and ARCH-033.

---

## 6. Deferred Work Requiring Future Architecture

None of the following are scheduled by this review, and none should be started under cover of a "graph consumer," "graph diagnostics," or "cleanup" milestone label — each requires its own new architecture document (and, in one case below, a new governance review) before implementation:

| Capability | Why deferred | What it needs |
|---|---|---|
| **Explainability** (why an artifact is invalid, in human-readable terms) | ARCH-037 §9 names it explicitly as a non-goal | A new architecture document consuming `DependencyGraph`'s structure |
| **Impact analysis** (what depends on a given identity) | ARCH-037 §9: forward-only by design; reverse lookup "requires its own architectural treatment of what state, if any, makes reverse lookup possible" | A new architecture document — not decidable as an extension of ARCH-037 |
| **Rebuild planning / recomputation ordering** | ARCH-037 §9 names it explicitly as a non-goal | A new architecture document building on graph structure |
| **Visualization / rendering** | ARCH-037 §9 names it explicitly as a non-goal | A new architecture document, likely paired with ARCH-034's existing surface-integration mechanism (RM-09) |
| **Graph caching or persistence** | ARCH-037 §2's lifecycle ("not persisted, cached, or treated as a new source of truth") is not an implementation freedom left open by §9 the way the above four are — it is stated as a direct corollary of ARCH-031 §3's "No new source of truth" guarantee | Would require a **new governance review**, not merely a new ARCH document, since it touches a guarantee traced to a Closed Architectural Decision (AGR-001 §6) — not scheduled, not planned |
| **`ResolutionCheck` → `DependencyGraph` migration** | Optional, implementation-tier; not blocked, not scheduled by default (§5) | No new architecture document required if attempted later — only a reconfirmation of ARCH-033's existing guarantee-by-guarantee trace, per ARCH-037's own "Interaction With ARCH-033" section |

Collectively, the first four rows are exactly the set of capabilities the task framing anticipated might need "a future Dependency Intelligence mechanism" — this review does not name that future document further than the task instructed, since doing so would itself be designing an architecture this document is scoped not to produce.

---

No code is authorized by this document. Awaiting approval before any Sprint 3 milestone begins.
