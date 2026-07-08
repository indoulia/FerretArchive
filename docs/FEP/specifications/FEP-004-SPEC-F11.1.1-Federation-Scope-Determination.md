# FEP-004-SPEC-F11.1.1 — Federation Scope Determination

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F11.1.1 |
| **Capability** | [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) |
| **Epic** | E11.1 — Federation Scope Resolution |
| **Feature** | F11.1.1 — Federation Scope Determination |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-11 — Federation](../epics/FEP-003-EPIC-CAP-11-Federation.md) · [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A cross-workspace request cannot be served coherently until it is known which workspaces are actually relevant to it. Federation Scope Determination exists to resolve a request's declared Workspace Relationships into a concrete, bounded Federation Scope, so that Cross-Workspace Composition (F11.2.1) always operates over a known, predictable set of workspaces rather than an ambiguous or open-ended one — directly satisfying this Feature's Completion Criteria that a cross-workspace request's Federation Scope is resolvable and predictable given the declared relationships.

## 3. Scope

- Interpreting a cross-workspace request against the Workspace Relationships already declared by Workspace Definition (F01.3.1, F01.3.2).
- Producing a concrete Federation Scope: the bounded set of workspaces a given request should draw upon.
- Ensuring the resolved Federation Scope is deterministic — the same request, against the same declared relationships, resolves to the same scope every time.
- Recognizing relationship type (e.g., parent/child, peer) as declared by F01.3.2, insofar as relationship type affects which workspaces belong in scope.

## 4. Out of Scope

- Declaring or establishing Workspace Relationships themselves — that is F01.3.1 and F01.3.2 (Workspace Definition); this Feature only consumes relationships already declared, per FEP-002-CAP-11 §3 Non-Responsibilities.
- Composing context from the workspaces once scope is determined — that is F11.2.1 (Cross-Workspace Context Composition).
- Reconciling relevance or ranking across workspaces in scope — that is F11.2.2 (Cross-Workspace Relevance Reconciliation).
- Recording or disclosing per-workspace contribution outcomes — that is F11.3.1 and F11.3.2.
- Evaluating or enforcing any individual workspace's Access Control & Policy decisions — determining scope is not the same as determining what may be delivered from a workspace once in scope.
- Any acquisition, organization, or maintenance of context within a participating workspace, per FEP-002-CAP-11 §3.
- Reasoning about, generating, or evaluating engineering artefacts — an explicit FEP-001 Non-Goal unrelated to scope resolution.

## 5. Engineering Requirements

1. A cross-workspace request must be resolvable to a Federation Scope consisting only of workspaces connected to the request's originating workspace by a declared Workspace Relationship, directly or as permitted by relationship type.
2. Resolution must be deterministic: identical requests evaluated against identical declared relationships must always produce an identical Federation Scope.
3. A request must never resolve to a Federation Scope containing a workspace with no declared relationship path to the request's originating workspace.
4. The resolved Federation Scope must be explicit and enumerable — a consumer or downstream capability must be able to determine exactly which workspaces are included, not merely infer it.
5. Resolution must complete using only declared relationship information; it must not depend on any workspace's content, freshness state, or access decisions being evaluated first.
6. A change to declared Workspace Relationships must be reflected in subsequent Federation Scope resolutions without requiring any change to this Feature's own logic.
7. Resolution must recognize relationship type where declared, such that the type of a relationship (e.g., parent/child versus peer) can affect whether a related workspace is included in scope.

## 6. Inputs

- A request whose intent spans more than one workspace.
- The declared Workspace Relationships (existence and type) applicable to the request's originating workspace.

## 7. Outputs

- A Federation Scope: the concrete, enumerable set of workspaces the request should draw upon.

## 8. Preconditions

- At least one Workspace Relationship has already been declared (F01.3.1) between the requesting workspace and at least one other workspace.
- Relationship type, where relevant to scope determination, has already been established (F01.3.2).
- The workspaces referenced by any declared relationship already exist as declared workspaces (F01.1.1), per FEP-003-EPIC-CAP-11 §4.

## 9. Postconditions

- A cross-workspace request has an associated Federation Scope that is explicit, bounded, and reproducible.
- Every workspace named in the resolved scope has a demonstrable, declared relationship path to the request's originating workspace.
- No workspace outside the resolved scope is later treated as in scope by Cross-Workspace Composition without a new resolution having included it.

## 10. Dependencies

**Capability dependencies.** Depends on Workspace Definition (FEP-002-CAP-01) for the existence of workspaces and their declared relationships; per FEP-002-CAP-11 §3, this Feature must never establish relationships itself.

**Epic dependencies.** Depends on E01.3 (Workspace Relationships) per FEP-003 Global Output 3 and FEP-003-EPIC-CAP-11 §4.

**Feature dependencies.** F01.3.1 (Relationship Declaration) and F01.3.2 (Relationship Type Model), per the E11.1 Features table (FEP-003-EPIC-CAP-11 §3).

**External dependencies.** None directly; identity & access systems (FEP-001 §6) are not consulted for scope determination itself, only later for access decisions within each workspace once scope is known.

## 11. Constraints

**Business constraints.** Scope determination must never widen a request's reach beyond what declared relationships actually support — a workspace must not enter scope on any basis other than an explicit, declared relationship (FEP-002-CAP-11 §8, Business, applied to scope rather than access).

**Product constraints.** The resolved Federation Scope must be attributable and enumerable so that Cross-Workspace Composition can later preserve per-workspace traceability, consistent with FEP-002-CAP-11 §8 Product.

**Context integrity constraints.** Resolution must be deterministic and observable; an ambiguous or non-reproducible scope would undermine every guarantee made further down the composition chain (Product Principle P3 — freshness and state must be knowable, not assumed).

**Trust constraints.** Per Product Principle P4 (No privileged consumer), scope resolution must apply the same relationship rules regardless of which consumer issued the cross-workspace request.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), this Feature must not absorb Workspace Definition's responsibility for declaring relationships, nor Cross-Workspace Composition's responsibility for using the resolved scope.

## 12. Acceptance Criteria

1. Given a set of declared Workspace Relationships, a cross-workspace request resolves to a Federation Scope containing exactly the workspaces reachable via those relationships, and no others.
2. Repeating the identical resolution against an unchanged set of declared relationships produces an identical Federation Scope every time.
3. A workspace with no declared relationship to the requesting workspace never appears in a resolved Federation Scope.
4. Adding or removing a declared Workspace Relationship changes the Federation Scope resolved for subsequent requests, without any change to this Feature's own logic.
5. The resolved Federation Scope for any given request can be enumerated on demand by any capability or consumer authorized to inspect it.

## 13. Validation Requirements

- That scope resolution is deterministic across repeated runs against unchanged relationship data.
- That every workspace appearing in a resolved scope has a traceable, declared relationship path to the requesting workspace.
- That relationship type is correctly consulted where it affects inclusion.
- That scope resolution does not silently depend on, or get blocked by, any downstream capability (composition, access control) that has not yet run.

## 14. Failure Conditions

- **Relationship sprawl without governance** (FEP-002-CAP-11 §10) — declared relationships proliferate without a clear model of what they entitle, making Federation Scope unpredictable. Expected behavior: resolution must still be deterministic and enumerable even under a large or complex relationship graph; an unresolvable or ambiguous scope must be surfaced as a failure, never guessed.
- **Stale or missing relationship declarations.** A request references a workspace relationship that has not been declared, or has been retired. Expected behavior: the workspace in question is excluded from the resolved scope, and its exclusion is observable rather than silently absorbed (Product Principle P5).

## 15. Traceability

Product Vision (Mission: infrastructure that assembles and delivers engineering context across workspaces) → Goals G1 (Completeness — a cross-workspace need must be served fully within declared relationships), G6 (Operable at repository scale and beyond) → Product Principles P3 (Freshness/state is first-class, not assumed), P4 (No privileged consumer), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-11 (Federation) → Epic E11.1 (Federation Scope Resolution) → Feature F11.1.1 (Federation Scope Determination).

## 16. Future Considerations

- Increasingly sophisticated scope resolution as Federation matures beyond Generation 3, per FEP-002-CAP-11 §11.
- Federation across organizational boundaries, deferred pending a governance decision (FEP-001 Open Question 5; FEP-003-EPIC-CAP-11 §8), which would require this Feature's relationship-reachability rules to be revisited.
- This Feature's detailed design remains provisional, per FEP-003-EPIC-CAP-11 §7, until real multi-workspace use cases exist to validate the relationship model it depends on (F01.3.2).
