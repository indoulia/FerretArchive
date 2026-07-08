# FEP-004-SPEC-F01.2.1 — Scope Boundary Declaration

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F01.2.1 |
| **Capability** | [FEP-002-CAP-01 — Workspace Definition](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) |
| **Epic** | E01.2 — Scope Declaration & Configuration |
| **Feature** | F01.2.1 — Scope Boundary Declaration |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-01 — Workspace Definition](../epics/FEP-003-EPIC-CAP-01-Workspace-Definition.md) · [FEP-002-CAP-01 — Workspace Definition](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Context Acquisition cannot know what to observe until something tells it, explicitly, what belongs to a workspace and what does not. Scope Boundary Declaration exists to give a workspace an explicit, resolvable statement of its in-scope source categories and boundaries, so that Acquisition has an unambiguous boundary to acquire within (FEP-003-EPIC-CAP-01 §3, F01.2.1 Objective and Product Outcome).

## 3. Scope

- Declaring which source categories are within a workspace's bounds.
- Declaring the boundaries that delimit a workspace's scope (what belongs, what is excluded).
- Making a workspace's scope declaration resolvable as an explicit statement at any time.
- Ensuring scope is always the product of an explicit declaration, never an inference from prior acquisition activity.

## 4. Out of Scope

- Actually discovering or reading any source's content — that is Context Acquisition, an entirely separate capability that only consumes the scope declaration this Feature produces.
- Declaring workspace-wide configuration such as freshness expectations or policy references — that is F01.2.2 (Workspace Configuration Management), a sibling Feature within the same Epic.
- Propagating a scope change to dependent capabilities once it occurs — that is F01.2.3 (Scope Change Propagation); this Feature owns declaring and resolving scope, not notifying others of a change to it.
- Assigning or resolving workspace identity — that is F01.1.1, a precondition of this Feature.
- Declaring or resolving relationships between workspaces — that is F01.3.1 / F01.3.2.
- Deciding whether a specific piece of content is trustworthy, relevant, or well-organized — those are Provenance & Attribution and Context Organization concerns respectively, not scope.
- Enforcing access policy over in-scope sources — that is Access Control & Policy; this Feature may hold a scope statement that access decisions consult, but it does not itself gate access.

## 5. Engineering Requirements

1. A workspace's scope must be declarable as an explicit statement of included source categories and boundaries.
2. Scope must never be inferred from what has already been acquired — an empty acquisition history must not be interpreted as an empty or undefined scope, and a rich acquisition history must not be interpreted as an expanded scope.
3. A workspace's scope must be resolvable, in full, at any time after declaration, independent of whether acquisition has yet occurred against it.
4. It must be possible to determine, for any given source category, whether it is included in or excluded from a workspace's declared scope.
5. Scope declaration must be possible only for a workspace that already has a stable, resolvable identity.
6. A scope declaration must be capable of expressing exclusions as well as inclusions, so that boundaries can be stated precisely rather than only by enumeration of what is included.
7. An unresolved or ambiguous scope boundary must be detectable as such, rather than defaulting silently to either "everything in scope" or "nothing in scope."

## 6. Inputs

- A resolvable workspace identity (F01.1.1) that the scope declaration attaches to.
- A declaration of intended scope from whoever is establishing or updating the workspace's bounds: which source categories, and which boundaries and exclusions, apply (FEP-002-CAP-01 §4, Inputs).

## 7. Outputs

- A resolved scope declaration stating what is in bounds and out of bounds for a workspace (FEP-002-CAP-01 §5, Outputs).

## 8. Preconditions

- The workspace must already be declared with a stable identity (F01.1.1) before a scope boundary can be attached to it.

## 9. Postconditions

- Any capability, most immediately Context Acquisition, can determine unambiguously whether a given source category is in or out of a workspace's scope.
- The scope declaration exists as an explicit, resolvable statement independent of acquisition activity having occurred.

## 10. Dependencies

**Capability dependencies.** None beyond Workspace Definition itself; this Feature exists to be consumed by Context Acquisition, but does not itself depend on Acquisition functioning.

**Epic dependencies.** E01.1 (Workspace Identity & Lifecycle) — per FEP-003-EPIC-CAP-01 §5 (Execution Order), E01.2 depends on identity existing before scope can be declared against it.

**Feature dependencies.** F01.1.1 (Workspace Declaration) — per the E01.2 Features table, F01.2.1 depends directly on F01.1.1.

**External dependencies.** Source systems (version control, issue trackers, documentation platforms, and other categories per FEP-001 §6) are the conceptual things a scope declaration refers to by category; this Feature does not read from them, only declares which categories are considered in scope.

## 11. Constraints

**Business constraints.** A workspace's scope must be explicitly stated, never inferred implicitly from whatever happens to already be acquired (FEP-002-CAP-01 §8, Business — this constraint applies to this Feature more directly than to any other in the capability).

**Product constraints.** Scope is stated in terms of a stable workspace identity (FEP-002-CAP-01 §8, Product); a scope declaration that could not be traced back to a specific, stable workspace would be meaningless.

**Context integrity constraints.** A scope declaration, once made, must be resolvable consistently — Acquisition must never receive a different answer to "is X in scope" from one query to the next without an intervening, explicit scope change (FEP-002-CAP-01 §8, Context integrity, read together with F01.2.3's propagation guarantee).

**Trust constraints.** Per Product Principle P5 (Degrade by scope, not by silent omission), an ambiguous or partially-declared scope must be surfaced as such, never silently treated as either fully in-scope or fully excluded.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries), scope declaration governs boundary, not enforcement — it must not be conflated with Access Control & Policy's enforcement responsibility, even though Access Control & Policy may consult it.

## 12. Acceptance Criteria

1. A declared workspace's scope resolves to an explicit statement of included and excluded source categories at any time it is queried.
2. Querying whether a specific source category is in scope for a workspace returns an unambiguous in-scope or out-of-scope answer, never an undefined result, once scope has been declared.
3. A workspace with no acquisition history yet performed still resolves a complete scope declaration if one has been made.
4. An attempt to declare scope against a workspace identity that does not exist or is not resolvable is rejected.
5. A scope declaration that leaves a source category's status ambiguous is detectable as an ambiguous declaration, distinct from an explicit exclusion.

## 13. Validation Requirements

- That scope resolution is unambiguous for every declared source category, with no category left in an undetermined state.
- That scope never silently drifts to reflect acquisition history rather than the explicit declaration.
- That boundary exclusions are resolvable with the same clarity as inclusions.
- That scope declaration is rejected, not silently accepted, when attempted against an unresolvable workspace identity.

## 14. Failure Conditions

- **Ambiguous scope.** An under-specified boundary leaves Acquisition unable to determine definitively whether a source category is in or out of scope (FEP-002-CAP-01 §10, Failure Modes: Ambiguous scope). Expected behavior: the ambiguity must be surfaced as an observable condition — Acquisition must be able to detect "scope is ambiguous here" rather than guessing and over-collecting or silently missing sources.
- **Scope declared against a non-existent workspace.** A scope declaration is attempted for an identity that does not resolve. Expected behavior: the declaration is rejected and the rejection is observable, never silently accepted as an orphaned scope statement.

## 15. Traceability

Product Vision (Mission: infrastructure that acquires engineering context) → Goals G1 (Completeness of context — an explicit scope is the precondition for anything being knowably complete), G2 (Currency of context — scope must be resolvable at any moment, not only historically) → Product Principles P5 (Degrade by scope, not by silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-01 (Workspace Definition) → Epic E01.2 (Scope Declaration & Configuration) → Feature F01.2.1 (Scope Boundary Declaration).

## 16. Future Considerations

- Richer workspace typologies (product lines, teams, cross-cutting concerns) will likely require scope boundaries to be expressed over more than a single-repository model — deferred until real multi-workspace use cases exist (FEP-003-EPIC-CAP-01 §8; FEP-002-CAP-01 §11).
- As the acquisition surface grows (FEP-001 §8, Unbounded acquisition surface risk), the vocabulary of source categories a scope declaration can reference may need to expand without requiring this Feature's resolution guarantees to change.
