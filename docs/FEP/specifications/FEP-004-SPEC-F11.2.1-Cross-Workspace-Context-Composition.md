# FEP-004-SPEC-F11.2.1 — Cross-Workspace Context Composition

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F11.2.1 |
| **Capability** | [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) |
| **Epic** | E11.2 — Cross-Workspace Composition |
| **Feature** | F11.2.1 — Cross-Workspace Context Composition |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-11 — Federation](../epics/FEP-003-EPIC-CAP-11-Federation.md) · [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A consumer whose need spans multiple workspaces should not require a fundamentally different product to be served. Cross-Workspace Context Composition exists to combine the already-assembled, already-current, already-provenance-bearing context contributed by each workspace within a resolved Federation Scope into a single result, without re-acquiring, re-organizing, or re-assembling anything itself — directly satisfying this Feature's Completion Criteria that a composed result correctly reflects contributions from every workspace in scope that succeeded.

## 3. Scope

- Combining context already produced by each contributing workspace's own Context Assembly (E05.3) into a single cross-workspace result, for a request whose Federation Scope has already been resolved.
- Preserving each contributing workspace's own provenance and access decisions unchanged within the composed result.
- Determining, for the purpose of composition only, which contributing workspaces' contributions are actually incorporated into the result.

## 4. Out of Scope

- Resolving which workspaces belong in the Federation Scope for a request — that is F11.1.1 (Federation Scope Determination), a precondition of this Feature.
- Reconciling relevance or ranking judgments across the contributing workspaces — that is F11.2.2 (Cross-Workspace Relevance Reconciliation).
- Recording or disclosing per-workspace contribution outcomes (succeeded, denied, stale, absent) — that is F11.3.1 and F11.3.2.
- Acquiring, organizing, maintaining, or assembling context within any individual workspace — that remains entirely the responsibility of each contributing workspace's own capability instances, per FEP-002-CAP-11 §3 Non-Responsibilities.
- Overriding, weakening, or replacing any contributing workspace's own Access Control & Policy decision — composition must operate strictly on what each workspace already permits to be assembled.
- Establishing or altering Workspace Relationships — composition consumes a Federation Scope, it does not decide it.
- Reasoning over, generating, or evaluating the composed context — an explicit FEP-001 Non-Goal.

## 5. Engineering Requirements

1. Composition must operate only on context that each contributing workspace's own Assembly has already produced or made assemblable; it must not acquire, organize, or assemble content itself.
2. Every workspace in the resolved Federation Scope that successfully contributes context must have its contribution represented in the composed result.
3. A composed result must preserve, unaltered, the provenance attached to each contributing workspace's contribution.
4. A composed result must preserve, unaltered, each contributing workspace's own access-control outcome for its contribution — composition must not grant, in aggregate, anything no single contributing workspace granted individually.
5. A contributing workspace's failure to contribute (denial, staleness, or absence) must not prevent composition from proceeding with the workspaces that did succeed.
6. The composed result must remain traceable, contribution by contribution, to its constituent workspaces — composition must not blend contributions so thoroughly that a specific piece of context can no longer be attributed to the workspace it came from.
7. Composition must not depend on, or presuppose, any particular number or combination of contributing workspaces beyond those in the resolved Federation Scope.

## 6. Inputs

- A resolved Federation Scope (F11.1.1) for the request being served.
- Assembled, or assemblable, context, together with its provenance and access decisions, from each workspace named in the Federation Scope.

## 7. Outputs

- A single, composed, cross-workspace result whose constituent contributions remain traceable to their originating workspaces.

## 8. Preconditions

- A Federation Scope has already been resolved for the request (F11.1.1).
- Each participating workspace's own Context Assembly (E05.3, specifically F05.3.1/F05.3.2) is already functioning and capable of producing assemblable context, per FEP-003-EPIC-CAP-11 §4.
- Each participating workspace's own Access Control & Policy and Provenance & Attribution capabilities are already functioning, per FEP-002-CAP-11 §7.

## 9. Postconditions

- A consumer whose request spanned the resolved Federation Scope receives a single, coherent result.
- Every piece of context in the result remains attributable to the specific contributing workspace it came from.
- No contributing workspace's provenance, access-control outcome, or freshness guarantee has been altered or weakened by having been composed with others.

## 10. Dependencies

**Capability dependencies.** Depends on every other capability being satisfied within each participating workspace — Context Acquisition, Context Organization, Context Maintenance, Context Assembly, Context Delivery, Provenance & Attribution, and Access Control & Policy — per FEP-002-CAP-11 §7 and FEP-003-EPIC-CAP-11 §4.

**Epic dependencies.** Depends on E11.1 (Federation Scope Resolution) and, per participating workspace, on E05.3 (Composition & Gap Reporting), per FEP-003 Global Output 3.

**Feature dependencies.** F11.1.1 (Federation Scope Determination), and per-workspace F05.3.1 (Context Composition) / F05.3.2 (Assembly Gap Reporting) already functioning, per the E11.2 Features table (FEP-003-EPIC-CAP-11 §3).

**External dependencies.** Identity & access systems (FEP-001 §6), indirectly, insofar as each contributing workspace's own access decisions rely on them; this Feature does not itself consult identity & access systems directly.

## 11. Constraints

**Business constraints.** Composition must never grant, in aggregate, access that no single contributing workspace would have granted individually — composition cannot become a privilege-escalation path (FEP-002-CAP-11 §8, Business).

**Product constraints.** A composed result must remain attributable to its constituent workspaces; composition must not blend context so thoroughly that a consumer loses the ability to tell which workspace something came from (FEP-002-CAP-11 §8, Product).

**Context integrity constraints.** Composition must jointly honor each contributing workspace's own Access Control & Policy and Provenance & Attribution, per FEP-002-CAP-11 §7, rather than substituting a federation-level equivalent.

**Trust constraints.** Per Product Principle P2 (Provenance is mandatory, not optional), no contribution may enter the composed result without its originating provenance intact.

**Policy constraints.** Per Product Principle P6 (Boundaries are capability boundaries, not team boundaries), composition must not absorb any contributing workspace's own Acquisition, Organization, Maintenance, or Access Control & Policy responsibilities.

## 12. Acceptance Criteria

1. A composed result for a request spanning at least two related workspaces includes the contribution of every workspace in the resolved Federation Scope that succeeded in contributing.
2. Every element of a composed result can be traced to the specific contributing workspace it originated from.
3. No contributing workspace's access-control outcome is altered by composition — content a workspace would deny individually is not present in the composed result via another workspace's contribution.
4. A composed result is produced even when one or more workspaces in the Federation Scope fail to contribute, using only the contributions that succeeded.
5. No contributing workspace's provenance record is modified, replaced, or removed as a result of composition.

## 13. Validation Requirements

- That composed results never contain content beyond what each contributing workspace's own access decision would independently permit.
- That every contribution in a composed result remains traceable to its originating workspace under inspection.
- That composition completes and produces a usable result even when one contributing workspace fails, is denied, or is stale.
- That composition introduces no acquisition, organization, or assembly logic of its own — it strictly recombines already-produced contributions.

## 14. Failure Conditions

- **Privilege escalation via composition** (FEP-002-CAP-11 §10) — combining information from multiple workspaces reveals something no single workspace's access policy would have permitted on its own. Expected behavior: this must never occur; composition must be constrained to the union of what each workspace independently permits, never more.
- **Attribution blending** (FEP-002-CAP-11 §10) — cross-workspace context loses per-workspace traceability. Expected behavior: composition must be rejected or flagged as defective before attribution is lost, never silently accepted (Product Principle P5).
- **A contributing workspace's own guarantees are weakened by composition** — e.g., its provenance or freshness state is altered. Expected behavior: composition must leave each contributing workspace's guarantees untouched; any inability to do so must be surfaced, not silently absorbed.

## 15. Traceability

Product Vision (Mission: infrastructure that assembles and delivers engineering context to any consumer) → Goals G1 (Completeness — a cross-workspace need should be as fully served as a single-workspace one), G4 (Trustworthy context — composed context must remain evaluable for trust) → Product Principles P1 (Context over computation), P2 (Provenance is mandatory), P6 (Boundaries are capability boundaries) → Capability FEP-002-CAP-11 (Federation) → Epic E11.2 (Cross-Workspace Composition) → Feature F11.2.1 (Cross-Workspace Context Composition).

## 16. Future Considerations

- Increasingly sophisticated cross-workspace composition as Federation matures beyond Generation 3, per FEP-002-CAP-11 §11.
- Federation-aware extension points allowing new source and consumer types to declare federation-readiness, deferred to Extensibility's maturity (FEP-003-EPIC-CAP-11 §8).
- This Feature's detailed design remains provisional, per FEP-003-EPIC-CAP-11 §7, since Federation depends on the entire rest of the capability model already being mature per participating workspace.
