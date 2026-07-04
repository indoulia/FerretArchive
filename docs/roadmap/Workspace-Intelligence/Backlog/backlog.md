# Backlog — Workspace Intelligence Platform

Ordered by `../15-Execution-Plan.md` phase. Within a phase, tickets are listed in the order they should be picked up. "Quick win" tags mark tickets shippable independently of the rest of their phase.

## Phase 0 — Founder Gate

- [ ] **WIP-001** Close ADR-0026 (workspace registry model)
- [ ] **WIP-002** Close ADR-0029 (v1 sharing scope)

## Phase 1 — Foundation

- [ ] **WIP-010** Implement `IWorkspaceRegistry` (file-based default backend) — `13-Storage.md` §3
- [ ] **WIP-011** Implement workspace manifest schema + `schemaVersion` upgrade path — `02-Workspace-Model.md` §3
- [ ] **WIP-012** `Ferret workspace create` / `add-repo` / `list` CLI commands — `12-API.md` §2
- [ ] **WIP-013** Auto-migration wrapper for existing single-repo workspaces — `14-Migration.md` *(quick win: ships with WIP-010–012, no separate release)*
- [ ] **WIP-014** MCP `workspace_list` tool — `12-API.md` §3

## Phase 2 — Federation

- [ ] **WIP-020** Implement `IFederatedKnowledgeStore` — `01-Architecture.md` §2, `03-Cross-Workspace-References.md` §2
- [ ] **WIP-021** `Ferret workspace add-reference` / `remove-reference`, cycle detection (DAG enforcement) — `03-Cross-Workspace-References.md` §5
- [ ] **WIP-022** Pinning (`pinnedStateHash`) resolution and fail-closed behavior — `03-Cross-Workspace-References.md` §3
- [ ] **WIP-023** Knowledge graph additions: `Workspace` node, `CONTAINS`/`IMPORTS` edges, `sourceWorkspaceId` tagging on results — `04-Knowledge-Graph.md`

## Phase 3 — Performance

- [ ] **WIP-030** Cross-workspace pull-based invalidation (state-hash mismatch at query time) — `06-Incremental-Indexing.md` §2
- [ ] **WIP-031** Federated query cache — `07-Caching.md` §1
- [ ] **WIP-032** Workspace reference topology cache — `07-Caching.md` §2
- [ ] **WIP-033** Scope Classifier (pre-Planner narrowing) — `05-Context-Optimization.md` §2
- [ ] **WIP-034** Compressor (post-Scorer, federated results only) — `05-Context-Optimization.md` §3
- [ ] **WIP-035** Context assembly cache — `07-Caching.md` §1

## Phase 4 — Observability *(plumbing may start alongside Phase 2)*

- [ ] **WIP-040** New metrics: `workspace.federated_query.duration`, `workspace.reference.resolve.duration`, `context.scope_narrowed.count`, `context.compression.tokens_saved`, `cache.federation.{hit,miss}` — `08-Telemetry.md` §1 *(quick win: independent of Phase 2 landing, just emits zero/no-op values until federation exists)*
- [ ] **WIP-041** Usage Ledger sink + `IUsageLedger` — `10-Usage-Ledger.md` §2–3
- [ ] **WIP-042** Close ADR-0028 (retention window)
- [ ] **WIP-043** Analytics rollup jobs (v1 aggregate set) — `09-Analytics.md` §2
- [ ] **WIP-044** `Ferret dashboard` CLI (Developer, Workspace views) — `11-Dashboard.md` §1

## Phase 5 — Sharing *(parallel to Phase 3/4 once Phase 2 lands)*

- [ ] **WIP-050** `sharing` field on workspace manifest + `Ferret workspace share` command — `12-API.md` §2
- [ ] **WIP-051** Permission check on reference resolution (Viewer-or-above required) — `03-Cross-Workspace-References.md` §4
- [ ] **WIP-052** Four-role enforcement (Owner/Admin/Developer/Viewer) — ADR-0029

## Explicitly Not on This Backlog

Everything in `../Future/Deferred-Scope.md` — org-wide analytics, billing, AI Agent role, cross-org sharing, Ferret Hub, 100K-repository scale work. Do not pull these forward without a Founder decision reopening the relevant deferral.
