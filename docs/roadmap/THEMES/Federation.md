# Theme: Federation

Cross-workspace and cross-organization composition — extending the capability model across workspace boundaries, per [FEP-002-CAP-11](../../FEP/capabilities/FEP-002-CAP-11-Federation.md).

## Roadmap Items

- **Ferret v2 — Workspace Intelligence Platform** (org-local, approved, in execution) — [NEXT/V2.md](../NEXT/V2.md)
- **Ferret v4 — Cross-Organization Federation ("Ferret Hub")** (roadmapped) — [FUTURE/V4.md](../Future/V4.md)
- **Enterprise-scale workspace registry** — the workspace-*count* scaling problem, distinct from per-workspace size — [Research Candidates](../FERRET-PRODUCT-ROADMAP.md#6-research-candidates)

## Sequencing Rationale

Federation's scope grows in three stages, each depending on the previous: within one workspace (already shipped) → across workspaces in one organization (v2.0) → across organizations (v4). [FEP-001 §8](../../FEP/FEP-001-Product-Architecture.md) explicitly flags "Federation treated as an afterthought" as a risk — this theme's sequencing exists to avoid retrofitting identity/trust concerns backward into earlier stages.

## Deferred From This Theme

Cross-reference conflict resolution when two federated workspaces disagree — see [Deferred-Scope.md](../Future/Deferred-Scope.md).
