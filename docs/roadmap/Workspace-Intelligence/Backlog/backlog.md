# Backlog — Workspace Intelligence Platform

Ordered by `../15-Execution-Plan.md` phase. Within a phase, tickets are listed in the order they should be picked up. "Quick win" tags mark tickets shippable independently of the rest of their phase.

**2026-07-05 implementation-readiness review:** Phase 0/1 tasks below now carry Goal/Dependencies/Expected outcome/Acceptance criteria/Dogfooding scenario. A recommended **Vertical Slice** (thin cut across Phase 1 + minimal Phase 2) is defined after Phase 2 — this, not full Phase 1 or full Phase 2, is the first thing to build and dogfood.

## Phase 0 — Founder Gate

- [x] **WIP-001** Close ADR-0026 (workspace registry model) — done: Founder accepted as specified, 2026-07-06 (T10, `30-Epic-5-Ferret-v2-Release-Execution.md`)
  - **Goal:** Founder accepts or overrides the identity-based local registry recommendation.
  - **Dependencies:** none — this is the first thing that must happen.
  - **Expected outcome:** ADR-0026 Status changes from Proposed to Accepted, with the chosen option recorded.
  - **Acceptance criteria:** ADR-0026 has a Founder-attributed decision; Phase 1 work can start.
  - **Dogfooding scenario:** n/a — decision, not code.
- ~~**WIP-002** Close ADR-0029 (v1 sharing scope)~~ — moved off Phase 0. Confirmed in this review not to block Phase 1; still required before Phase 5 starts (tracked there).

## Phase 1 — Foundation

- [x] **WIP-010** Implement `IWorkspaceRegistry` (file-based default backend) — `13-Storage.md` §3 — done: `src/Ferret.Workspace.Graph/`, 12 tests green
  - **Goal:** A narrow, swappable interface (`Resolve`, `List`, `Save`) over a file-based `~/.ferret/workspaces/<id>/workspace.json` store, with atomic (temp-file + rename) writes and fail-closed behavior on a corrupt manifest (ADR-0026 "Registry Storage" section).
  - **Dependencies:** WIP-001 (ADR-0026 approved).
  - **Expected outcome:** A workspace entry can be created, read back, and listed via the interface — no CLI yet.
  - **Acceptance criteria:** Unit tests cover create/resolve/list/save round-trip, a simulated crash-mid-write (previous valid file survives), and a corrupt-JSON case (fails closed with a clear error, doesn't auto-repair or delete). No path-based identity anywhere in the implementation (per ADR-0026).
  - **Dogfooding scenario:** n/a — not user-facing until WIP-012.
- [x] **WIP-011** Implement workspace manifest schema + `schemaVersion` upgrade path — `02-Workspace-Model.md` §3 — done: extends `WorkspaceRegistryEntry`, 20 tests green
  - **Goal:** The JSON schema in `02-Workspace-Model.md` §3, plus the upgrade mechanism from ARCH-001 §12.4 wired to the new schema.
  - **Dependencies:** WIP-010.
  - **Expected outcome:** A manifest can be validated and upgraded across `schemaVersion` bumps using the existing upgrade mechanism, unmodified.
  - **Acceptance criteria:** Round-trip test for a v1.0 manifest; a synthetic future-schema-version manifest triggers the existing migration-path validation, not new code.
  - **Dogfooding scenario:** n/a — not user-facing until WIP-012.
- [x] **WIP-012** `Ferret workspaces create` / `list` / `show` / `add-repo` / `remove-repo` CLI commands — `12-API.md` §2 — done: 55 new tests, dogfooded end-to-end against this repo's own remote
  - **Goal:** User-facing entry point to WIP-010/011, implementing ADR-0026's identity resolution (canonicalized `origin` remote; documented fallback for no-remote and multi-remote repos). Renamed from `workspace` to `workspaces` during implementation — see `12-API.md` §2's correction note.
  - **Dependencies:** WIP-010, WIP-011.
  - **Expected outcome:** A developer can create a workspace, add/remove repos by remote identity, list all workspaces, and inspect one workspace's full manifest — all from the CLI, none of it colliding with the existing `ferret workspace init`/`status` commands.
  - **Acceptance criteria:** `create`/`add-repo`/`list`/`show`/`remove-repo` round-trip against a real local git repo; `list`/`show` output matches the manifest state; a repo with no remote gets the local-identity fallback (ADR-0026, `.ferret/workspace-identity.json` — corrected from the doc's original `.ai/...` reference to match the actual `WorkspaceLayout.RootDirectoryName` constant in code, see WIP-012's Self Review); adding a repo with a differently-formatted URL for an already-added remote (`git@...` vs `https://...`) is recognized as the same identity, not a duplicate; `create` rejects a duplicate name.
  - **Dogfooding scenario:** A developer with 2+ related repos groups them under one workspace and confirms `workspaces list`/`show` shows both correctly. Success = accurate listing, no errors, existing `Ferret index build`/`query` and `Ferret workspace init`/`status` on either repo unaffected. Rollback trigger: any regression in existing single-repo command behavior.
- [ ] **WIP-013** Auto-migration wrapper for existing single-repo workspaces — `14-Migration.md` *(quick win: ships with WIP-010–012, no separate release)*
  - **Goal:** Zero-action wrapping of every existing `.ai/workspace.json` into a `kind: "personal"` registry entry.
  - **Dependencies:** WIP-010, WIP-011.
  - **Expected outcome:** Running any `Ferret workspace` command in an un-migrated checkout silently creates the wrapper entry.
  - **Acceptance criteria:** Existing single-repo integration test suite passes unmodified after this ships (14-Migration.md §2's invariant); failure path falls back to no-registry behavior per §3, never blocks the underlying command.
  - **Dogfooding scenario:** Run the full existing dogfooding command set against an already-migrated dogfooding-branch checkout; confirm identical behavior/output to pre-migration baseline.
- [ ] **WIP-014** MCP `workspace_list` tool — `12-API.md` §3
  - **Goal:** MCP parity for WIP-012's `list`.
  - **Dependencies:** WIP-012.
  - **Expected outcome:** An MCP client can enumerate workspace membership.
  - **Acceptance criteria:** Tool output matches CLI `list` output for the same workspace.
  - **Dogfooding scenario:** Exercised via whichever MCP client the team already uses for existing knowledge tools — no new client needed.

## Vertical Slice — "Two Workspaces, One Cross-Repo Answer" (recommended first dogfooding target)

Cuts across Phase 1 (all of it) and a **minimal** subset of Phase 2 — just enough to prove the architecture's central bet (federated query, zero duplication) end to end. Explicitly excludes pinning (WIP-022) and all of Phase 3/4/5 — those make it fast, safe, and shared, but aren't needed to prove it *works*.

- [x] **WIP-SLICE-1** Minimal `IFederatedKnowledgeStore` — fan-out + merge + `sourceWorkspaceId` tagging only, no scope narrowing, no compression, no caching — done: `src/Ferret.Knowledge.Federation/`, 7 unit tests + 3 real-repo integration tests green; see `16-Vertical-Slice-Validation.md`
  - **Goal:** Prove a query against Workspace A returns correct, cited results from referenced Workspace B without copying B's index.
  - **Dependencies:** WIP-010–013; a trimmed slice of WIP-023 (just the `Workspace`/`CONTAINS`/`IMPORTS` graph additions and result tagging — not the full ticket).
  - **Expected outcome:** `Ferret knowledge query` against A transparently includes B's content when A references B.
  - **Acceptance criteria:** A query answerable only by combining A+B returns a correct, cited answer (00-Vision.md §4's own success metric); inspecting B's on-disk index after the query shows zero new files written to A.
- [x] **WIP-SLICE-2** `Ferret workspace add-reference` + cycle detection (DAG enforcement), no pinning — done: `ferret workspaces add-reference`/`ferret workspaces query`, `src/Ferret.Workspace.Graph/ReferenceGraph.cs`, 9 unit tests green
  - **Goal:** Let a developer actually create the reference the slice needs.
  - **Dependencies:** WIP-012.
  - **Expected outcome:** `add-reference` creates an `IMPORTS` edge; attempting a cycle is rejected outright, matching `03-Cross-Workspace-References.md` §5.
  - **Acceptance criteria:** Cycle-creation attempt fails with a clear error in a test; non-cycle reference succeeds.

**Dogfooding scenario for the slice:** Take two repos the team already uses together (e.g. a service and a shared library it depends on). Auto-migrate the service's existing single-repo workspace (WIP-013). Create a new workspace for the shared library, or reuse its auto-migrated one. Add a reference from the service's workspace to the library's. Ask a question that can only be answered by combining both (e.g. "what implements the interface the service depends on, and where is it defined") via the unchanged `Ferret knowledge query` surface.

**Success looks like:** a correct answer, cited with the library's workspace as source, with no duplicated index content anywhere on disk.

**What triggers rollback/redesign (not just a bug fix):** if the *architecture* is wrong — e.g., the federated fan-out fundamentally can't preserve citation accuracy, or zero-duplication turns out to be incompatible with acceptable latency even before Phase 3 optimization is applied. A wrong answer from an implementation bug is a fix, not a redesign trigger; a wrong answer that traces back to the fan-out/merge *design* in `03-Cross-Workspace-References.md` §2 is.

## Phase 2 — Federation (full scope, beyond the vertical slice above)

- [x] **WIP-020** Implement `IFederatedKnowledgeStore` — `01-Architecture.md` §2, `03-Cross-Workspace-References.md` §2 — done: satisfied by `src/Ferret.Knowledge.Federation/FederatedKnowledgeStore.cs` (WIP-SLICE-1) plus its Stabilization Sprint 1 hardening (per-source exception boundary, diagnostics). No further work identified beyond what WIP-SLICE-1 + hardening already delivered.
- [x] **WIP-021** `Ferret workspace add-reference` / `remove-reference`, cycle detection (DAG enforcement) — `03-Cross-Workspace-References.md` §5 — done: `add-reference`/cycle detection shipped in WIP-SLICE-2; `remove-reference` added here (`WorkspacesRemoveReferenceCommandHandler`), 7 new tests, dogfooded live including the moved-repo repair workflow (remove stale reference, re-add at corrected path)
- [x] **WIP-022** Pinning (`pinnedStateHash`) resolution and fail-closed behavior — `03-Cross-Workspace-References.md` §3 — done: `IWorkspaceStateFingerprintProvider`/`WorkspaceStateFingerprintProvider` (content-hash based, per the ADR-0027 Amendment), fail-closed pin comparison in `FederatedKnowledgeStore`, `ferret workspaces pin-reference`/`unpin-reference` CLI commands. Real-repo integration test and live CLI dogfooding both confirm the full lifecycle (pin → succeeds → referenced content changes → fails closed, excluding only that source → unpin → floats again).
- [x] **WIP-023** Knowledge graph additions: `Workspace` node, `CONTAINS`/`IMPORTS` edges, `sourceWorkspaceId` tagging on results — `04-Knowledge-Graph.md` — done: per `04-Knowledge-Graph.md` §3, these are explicitly logical/query-time constructs, not a separate persisted graph store — `CONTAINS` = `WorkspaceMembers.Repos`, `IMPORTS` = `WorkspaceReference` (WIP-SLICE-2), `sourceWorkspaceId` tagging = `SearchHit.SourceWorkspaceId` (WIP-SLICE-1). No separate `IKnowledgeGraph`/graph-store code exists anywhere in the codebase to build against; nothing further identified.

## Phase 3 — Performance

- [x] **WIP-030** Cross-workspace pull-based invalidation (state-hash mismatch at query time) — `06-Incremental-Indexing.md` §2 — done: shipped merged with WIP-031 per `20-Phase-3-Priority-Assessment.md` §1; on `main` via PR #34 (T1, `30-Epic-5-Ferret-v2-Release-Execution.md`)
- [x] **WIP-031** Federated query cache — `07-Caching.md` §1 — done: `src/Ferret.Knowledge.Federation/FederatedQueryCache.cs` + `CachingFederatedKnowledgeStore.cs`, including the P3-002 regression fix; on `main` via PR #34
- [x] **WIP-032** Workspace reference topology cache — `07-Caching.md` §2 — done: `src/Ferret.Workspace.Graph/CachingWorkspaceRegistry.cs`; on `main` via PR #32
- [ ] **WIP-033** Scope Classifier (pre-Planner narrowing) — `05-Context-Optimization.md` §2 — **Deferred to v2.1** (Gate E, `30-Epic-5-Ferret-v2-Release-Execution.md` T6, 2026-07-06): `24` proved the naive shape isn't a net win at scale; `25` validated a pooled-connection shape only via Python simulation. Zero real C# implementation and zero connection-pooling infrastructure exist anywhere in the repo to build from — the plan's only High-risk item (`28` §7), and its Definition of Done requires re-validating at R=26 scale against a real `dogfood-hub`-sized multi-workspace corpus, which this environment does not have. Gate E accepts explicit deferral as a fully valid outcome; not left ambiguous.
- [ ] **WIP-034** Compressor (post-Scorer, federated results only) — `05-Context-Optimization.md` §3 — **Deferred to v2.1**, consequent on WIP-033's deferral (hard dependency, `29`/`30` §5)
- [ ] **WIP-035** Context assembly cache — `07-Caching.md` §1 — **Deferred to v2.1**, consequent on WIP-033's deferral (hard dependency, `29`/`30` §5)

## Phase 4 — Observability *(plumbing may start alongside Phase 2)*

- [ ] **WIP-040** New metrics: `workspace.federated_query.duration`, `workspace.reference.resolve.duration`, `context.scope_narrowed.count`, `context.compression.tokens_saved`, `cache.federation.{hit,miss}` — `08-Telemetry.md` §1 *(quick win: independent of Phase 2 landing, just emits zero/no-op values until federation exists)*
- [ ] **WIP-041** Usage Ledger sink + `IUsageLedger` — `10-Usage-Ledger.md` §2–3
- [ ] **WIP-042** Ship ADR-0028's 90-day default retention (no Founder sign-off required; revisit only if usage data warrants a different window)
- [ ] **WIP-043** Analytics rollup jobs (v1 aggregate set) — `09-Analytics.md` §2
- [ ] **WIP-044** `Ferret dashboard` CLI (Developer, Workspace views) — `11-Dashboard.md` §1

## Phase 5 — Sharing *(parallel to Phase 3/4 once Phase 2 lands)*

- [ ] **WIP-050** `sharing` field on workspace manifest + `Ferret workspace share` command — `12-API.md` §2
- [ ] **WIP-051** Permission check on reference resolution (Viewer-or-above required) — `03-Cross-Workspace-References.md` §4
- [ ] **WIP-052** Four-role enforcement (Owner/Admin/Developer/Viewer) — ADR-0029

## Explicitly Not on This Backlog

Everything in `../Future/Deferred-Scope.md` — org-wide analytics, billing, AI Agent role, cross-org sharing, Ferret Hub, 100K-repository scale work. Do not pull these forward without a Founder decision reopening the relevant deferral.
