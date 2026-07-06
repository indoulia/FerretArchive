# 00 — Vision: Workspace Intelligence Platform

**Status:** Founder decision — draft for sign-off
**Milestone:** Ferret v2.0 (first milestone after Dogfooding)
**Extends:** VISION-001, ARCH-001 §27.2 (Multi-Repository Federation, previously deferred)
**Domains:** DOM-001 Platform, DOM-002 Knowledge (FD-003 — no new domain required)
**Principles invoked:** PR-003 Workspace First, PR-008 Everything is Knowledge, PR-010 Incremental Evolution (FD-002)

## 1. Problem

Ferret today answers questions about **one repository**. Real engineering work spans many: a service repo, its shared libraries, its infra repo, its ADRs, its specs, and notes that live nowhere near source control. An engineer asking "why does auth work this way" has to know which of five repositories holds the answer before they can even ask.

Two consequences follow directly from the single-repo boundary:
- **Coverage gap.** Anything outside the current repo (a shared-UI library, an ADR in a docs repo) is invisible to Ferret, even though it may be exactly what the question needs.
- **Duplication pressure.** The only way teams have to work around the gap today is to copy or re-index content into every repo that needs it — which is what "no duplication, no re-indexing" is designed to prevent.

## 2. Vision Statement

**A workspace is a queryable unit of knowledge, not a checkout of one repository.** A workspace can contain multiple repositories, documents, ADRs, notes, and specs, and can *reference* other workspaces as read-only dependencies — the same way a package depends on another package. Repository boundaries become an implementation detail; the workspace boundary is what a question is scoped to.

This is additive. Every existing single-repo workspace continues to work unchanged (§14-Migration). Nothing in this milestone removes or breaks the current `.ai/workspace.json`-per-repo model — it wraps it.

## 3. What This Is Not

- **Not a rewrite.** ARCH-001's Domain Architecture (§30) is already factored so Workspace + Knowledge + Memory can become repository-scoped services. This milestone activates that, it does not replace it.
- **Not a hosted product decision.** Whether workspace sharing runs on a Ferret-operated cloud service ("Ferret Hub") is FUTURE-002 Q8 — open, unresolved, and out of scope here. This milestone is designed so that decision can be made later without rework (see 13-Storage.md).
- **Not full enterprise RBAC.** FUTURE-002 already defers "RBAC for knowledge graph and memory access" and "multi-tenant enterprise deployment" to V3. This milestone ships the minimum sharing model needed for a team to use shared workspaces safely (Owner/Admin/Developer/Viewer) and defers the rest (see `Future/Deferred-Scope.md`).

## 4. Success Metrics

| Metric | Target | Why this metric |
|---|---|---|
| Cross-repo query coverage | A question answerable only by combining ≥2 repos returns a correct, cited answer | Directly tests the core claim — repo boundaries are invisible to the querier |
| Token cost per answer | No worse than single-repo baseline ±15%, even when the query spans 3+ referenced workspaces | Federation must not become a token-cost regression — see 05-Context-Optimization.md |
| p95 federated query latency | ≤ 2x the single-repo p95 baseline in ARCH-001 §25.2 | Federation must degrade gracefully, not linearly, with reference count |
| Re-indexing avoided | 0 bytes of a referenced workspace's index duplicated into the importer | This is the literal test of "reference, not copy" |
| Time-to-first-workspace-reference | A developer can add a reference to another workspace in <5 minutes, no re-index | Adoption friction test |

## 5. Why Now

Sprints 2–3 of the V2 architecture (persistence, dependency graph) are complete and merged. The knowledge graph, state-hash versioning, and storage abstraction ARCH-001 depends on for federation already exist in production. Building the federation layer now — on top of a stable base — is lower risk than building it earlier (when the base was still moving) or later (when more single-repo assumptions will have hardened into code).

## 6. Decision Log

| Decision | Outcome |
|---|---|
| Multi-repo workspace is the v2.0 milestone, replacing all previously planned feature work | Ready for implementation — confirmed by Founder directive |
| Full enterprise RBAC (AI Agent role, cross-org sharing, billing) is out of scope for this milestone | Ready — deferred, see `Future/Deferred-Scope.md` |
| Whether Ferret Hub (hosted service) or self-hosted-only is the deployment target | Requires Founder decision — tracked as open (FUTURE-002 Q8); this milestone's storage design (13-Storage.md) keeps both paths open |
