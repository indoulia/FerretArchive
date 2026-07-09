# FEP-002-CAP-11 — Federation

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-11 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.11 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

Some needs cannot be answered from a single workspace. Federation exists to serve a consumer whose need spans more than one workspace coherently, without requiring a fundamentally different product for that case.

## 2. Responsibilities

- Recognize and use the Workspace Relationships declared by Workspace Definition to know which workspaces may be composed together.
- Compose context across multiple workspaces' already-organized, already-current, already-assemblable context, without re-acquiring or re-organizing anything itself.
- Reconcile relevance and ranking across workspace boundaries when a request spans more than one workspace.
- Preserve per-workspace provenance and access control when composing across workspaces — a cross-workspace result must remain traceable and permission-respecting at the level of each contributing workspace.
- Represent honestly when a cross-workspace request cannot be fully satisfied — for example, because one relevant workspace denies access or is stale — rather than silently narrowing to only the workspaces that succeeded.

## 3. Non-Responsibilities

- Must never acquire, organize, or maintain context itself — it depends entirely on each contributing workspace's own Acquisition, Organization, and Maintenance already functioning correctly, per FEP-001 §4.
- Must never override a single workspace's Access Control & Policy decision — cross-workspace composition must still respect each workspace's own gating.
- Must never establish workspace relationships itself — it consumes relationships declared by Workspace Definition, it does not decide which workspaces should be related.

## 4. Inputs

- Workspace Relationships from Workspace Definition.
- Assembled, or assemblable, context, freshness state, provenance, and access decisions from each contributing workspace's own capability instances.
- A request whose scope spans more than one workspace.

## 5. Outputs

- Composed, cross-workspace context, with per-workspace provenance and access decisions preserved.
- An honest indication of which contributing workspaces succeeded, were denied, or were stale, for any given cross-workspace result.

## 6. Context Objects

- **Federation Scope** — the conceptual set of workspaces a given cross-workspace request draws upon.
- **Cross-Workspace Composition** — the conceptual result of composing context from multiple workspaces for a single request.
- **Contribution Outcome** — the conceptual record of how each workspace within a Federation Scope fared for a given composition: succeeded, denied, stale, or absent.

## 7. Relationships

Depends on Workspace Definition for relationships between workspaces, and on each contributing workspace's full Context Supply Chain — Acquisition through Delivery — already functioning. Must jointly honor each contributing workspace's Access Control & Policy and Provenance & Attribution rather than replacing them with a federation-level equivalent.

## 8. Constraints

- **Business.** Federation must never grant, in aggregate, access that no single contributing workspace would have granted individually — composition cannot become a privilege-escalation path.
- **Product.** A cross-workspace result must remain attributable to its constituent workspaces; Federation must not blend context so thoroughly that a consumer loses the ability to tell which workspace something came from.
- **Context integrity.** Federation must surface partial success honestly, consistent with Product Principle P5, rather than presenting a partial composition as a complete one.

## 9. Success Criteria

- A request spanning multiple workspaces receives a coherent, composed result without any contributing workspace's guarantees — provenance, access control, freshness — being weakened.
- Cross-workspace results remain traceable to their constituent workspaces.
- Partial composition, due to access, staleness, or absence in one workspace, is visible rather than silently smoothed over.

## 10. Failure Modes

- **Privilege escalation via composition** — combining information from multiple workspaces reveals something no single workspace's access policy would have permitted on its own.
- **Attribution blending** — cross-workspace context loses per-workspace traceability, undermining Provenance & Attribution's guarantees at the federation layer.
- **Silent partial composition** — a cross-workspace result quietly omits a workspace that failed, denied, or was stale, without indicating the composition is incomplete.
- **Relationship sprawl without governance** — workspace relationships proliferate without a clear model of what they entitle, making Federation Scope unpredictable.

## 11. Future Evolution

Increasingly sophisticated cross-workspace relevance and ranking as Federation matures beyond Generation 3. Support for federation across organizational boundaries, per FEP-001 Open Question 5, which will require this capability's constraints — especially the no-privilege-escalation constraint — to be revisited explicitly. Federation-aware extension points, allowing new source and consumer types to declare federation-readiness as part of how Extensibility admits them.
