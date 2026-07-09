# FEP-004-SPEC-F01.3.2 — Relationship Type Model

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F01.3.2 |
| **Capability** | [FEP-002-CAP-01 — Workspace Definition](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) |
| **Epic** | E01.3 — Workspace Relationships |
| **Feature** | F01.3.2 — Relationship Type Model |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-01 — Workspace Definition](../epics/FEP-003-EPIC-CAP-01-Workspace-Definition.md) · [FEP-002-CAP-01 — Workspace Definition](../capabilities/FEP-002-CAP-01-Workspace-Definition.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

Knowing that two workspaces are related is not enough for Federation to reason sensibly about how they should be considered together — a parent/child relationship and a peer relationship imply different things about how context might later be composed. Relationship Type Model exists to distinguish conceptual kinds of relationship, so Federation Scope resolution can reason about the nature of a relationship, not just its existence (FEP-003-EPIC-CAP-01 §3, F01.3.2 Objective and Product Outcome).

## 3. Scope

- Defining an explicit, recognized set of conceptual relationship types (for example: parent/child, peer).
- Attaching a type, drawn from that recognized set, to a declared relationship between two workspaces.
- Making a relationship's type resolvable alongside its existence.

## 4. Out of Scope

- Declaring that a relationship exists in the first place — that is F01.3.1 (Relationship Declaration), a strict precondition of this Feature; this Feature only classifies a relationship that F01.3.1 has already established.
- Performing any cross-workspace composition based on a relationship's type — that remains entirely Federation's responsibility, consistent with this capability's non-responsibility to never perform cross-workspace composition itself (FEP-002-CAP-01 §3).
- Assigning or resolving workspace identity, or declaring scope or configuration — those are F01.1.1, F01.2.1, and F01.2.2 respectively, unrelated to relationship typing.
- Deciding which relationship type is "correct" or most appropriate for a given business situation — this Feature only records and resolves a declared type; it does not adjudicate whether the type chosen accurately reflects reality.
- Defining an open-ended or unbounded set of relationship types — the set of recognized types must be explicit and closed at any given point, per the Feature's own completion criteria; expanding the set is a governed change to this Feature, not an implicit runtime capability.

## 5. Engineering Requirements

1. A finite, explicit set of recognized relationship types must exist (at minimum: parent/child, peer), against which any typed relationship is validated.
2. Every declared relationship that has been typed must resolve to exactly one recognized type — not zero, and not more than one, at a time.
3. A relationship's type must be resolvable in the same query, or alongside the same resolution, as the relationship's existence (F01.3.1), so a consumer is never required to check existence and type separately.
4. An attempt to assign a type not present in the recognized set must be rejected rather than silently accepted as a new, unrecognized type.
5. The recognized set of relationship types must itself be resolvable, so that a consuming capability (Federation) can determine what types exist without needing to infer them from observed relationships.
6. A relationship's type must remain resolvable and unchanged unless an explicit action changes it — a type must never drift implicitly from other changes to either connected workspace.

## 6. Inputs

- A previously declared relationship between two workspaces (F01.3.1).
- A declared type for that relationship, drawn from the recognized set of conceptual relationship types.

## 7. Outputs

- A relationship's resolvable type, alongside its existence (FEP-002-CAP-01 §6, Context Objects: Workspace Relationship, read together with its conceptual "nature" per FEP-002-CAP-01 §7).
- The recognized set of relationship types itself, as a resolvable reference set.

## 8. Preconditions

- A relationship must already be declared between the two workspaces (F01.3.1) before a type can be attached to it.

## 9. Postconditions

- Federation, when it resolves a relationship, can determine both that it exists and what conceptual kind of relationship it is, in the same act of resolution.
- No relationship is left with an ambiguous or unrecognized type.

## 10. Dependencies

**Capability dependencies.** None beyond Workspace Definition itself; Federation is the eventual consumer but this Feature does not depend on Federation to function.

**Epic dependencies.** E01.1 (Workspace Identity & Lifecycle), transitively through F01.3.1 — a relationship, and therefore its type, presupposes both workspace identities already existing.

**Feature dependencies.** F01.3.1 (Relationship Declaration) — per the E01.3 Features table, F01.3.2 depends directly on F01.3.1; a type cannot be attached to a relationship that does not yet exist.

**External dependencies.** None. The relationship type model is an entirely Ferret-internal conceptual classification; it does not depend on any external system category from FEP-001 §6.

## 11. Constraints

**Business constraints.** A relationship's type must be explicitly declared and drawn from a defined set, consistent with the capability-wide discipline against implicit inference (FEP-002-CAP-01 §8, Business) — a type must never be guessed from the two workspaces' characteristics.

**Product constraints.** A relationship's type is only meaningful in terms of a relationship that itself rests on stable workspace identities (FEP-002-CAP-01 §8, Product); type resolution inherits the same stability requirement.

**Context integrity constraints.** A relationship's type must remain consistent and resolvable for as long as the relationship itself exists — a type that becomes unresolvable while the relationship persists would leave Federation with an incomplete picture without any signal that anything is missing.

**Trust constraints.** Per Product Principle P2 (Provenance is mandatory), a resolved relationship type should be traceable to an explicit declaration of that type, not an assumption.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries) and the identified program risk of premature relationship modeling (FEP-003-EPIC-CAP-01 §7), the recognized type set must stay deliberately minimal and must not be expanded speculatively ahead of Federation's actual, demonstrated needs.

## 12. Acceptance Criteria

1. Every typed relationship resolves to exactly one type drawn from the explicit, recognized set.
2. An attempt to assign a type outside the recognized set is rejected, and the rejection is observable to whoever attempted it.
3. Resolving a relationship returns its type in the same resolution as its existence, without requiring a separate query.
4. The recognized set of relationship types is itself resolvable as a distinct, queryable reference set.
5. A relationship's type remains unchanged across repeated queries unless an explicit action changes it.

## 13. Validation Requirements

- That every typed relationship resolves to exactly one recognized type, with no relationship left untyped once typing has been attempted.
- That an out-of-set type assignment is detectable as rejected, not silently coerced into the nearest recognized type.
- That the recognized type set is itself independently resolvable, distinct from any specific relationship's assigned type.
- That type resolution remains stable over time absent an explicit, recorded change.

## 14. Failure Conditions

- **Unrecognized type assignment.** An attempt is made to assign a relationship a type outside the explicit, recognized set (FEP-002-CAP-01 §10, Failure Modes, read together with this Feature's own completion criterion that "the set of recognized types is explicit"). Expected behavior: per Product Principle P5, the attempt must be rejected visibly, never silently accepted as an ad hoc new type or coerced into an existing one without indication.
- **Workspace sprawl compounded by untyped or ambiguously typed relationships.** As relationships proliferate (FEP-002-CAP-01 §10, Failure Modes: Workspace sprawl), an untyped or ambiguously typed relationship undermines Federation's ability to reason about the relationships it finds. Expected behavior: an untyped relationship must be distinguishable from a typed one, never conflated with any specific recognized type by default.

## 15. Traceability

Product Vision (Mission: infrastructure operable at repository scale and beyond) → Goal G6 (Operable at repository scale and beyond — Federation's ability to reason about relationship nature is part of operating beyond a single workspace) → Product Principles P2 (Provenance is mandatory), P5 (Degrade by scope, not by silent omission), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-01 (Workspace Definition) → Epic E01.3 (Workspace Relationships) → Feature F01.3.2 (Relationship Type Model).

## 16. Future Considerations

- Formalized relationship types (parent/child, peer, dependency) are expected to mature as Federation matures (FEP-002-CAP-01 §11); the initial recognized set here is deliberately minimal.
- Over-specifying relationship types now risks rework once Federation's actual needs are known (FEP-003-EPIC-CAP-01 §7, Risk: Premature relationship modeling) — expansion of the recognized type set is anticipated but intentionally deferred, not designed here.
