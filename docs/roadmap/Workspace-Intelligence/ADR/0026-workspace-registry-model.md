# ADR-0026 — Workspace Registry Model

| Field | Value |
|---|---|
| **Status** | Proposed — requires Founder decision |
| **Date** | 2026-07-05 |
| **Deciders** | Founder |
| **Milestone** | Workspace Intelligence Platform, Phase 0 |
| **Supersedes** | — |

---

## Context

A workspace can now span multiple repository checkouts, documents, and references to other workspaces (see `../02-Workspace-Model.md`). No single repo's `.ai/` directory can hold this record, because it is not scoped to one repo. Something new must own the multi-repo workspace's identity, membership list, and reference list. Where that record lives determines whether shared workspaces and future cloud sync (both top Founder priorities) are possible without rework later.

## Decision

We will use an **identity-based local registry**: `~/.ferret/workspaces/<workspace-id>/workspace.json`, addressing member repos and referenced workspaces by durable identity (git remote URL, or workspace ID) with local checkout paths cached alongside, not used as the identity. Access is mediated by a new `IWorkspaceRegistry` interface, following the same pluggable-backend pattern ARCH-001 §19.3 already established for `IKnowledgeStore`.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Path-based manifest in a parent directory (repos listed by relative/absolute path) | Ties a workspace to one machine's directory layout; breaks the moment a workspace is shared with a colleague or synced to another machine — directly forecloses two stated Founder priorities |
| Cloud-hosted registry from day one | No hosting infrastructure exists yet; breaks the local-first / air-gap invariant (FUTURE-002 Conditions 3–4) that Ferret has already committed to |

## Consequences

### Positive
- Workspace identity survives being shared or synced to a different machine
- The same `IWorkspaceRegistry` abstraction can later be backed by a hosted service (Ferret Hub, V3) with no schema change to `workspace.json`
- Consistent with the existing `IMemoryStore` Local→Shared→Enterprise tiering pattern (FUTURE-002 §13.3)

### Negative
- Slightly more implementation work up front than a path-based manifest (identity resolution, path caching/reconciliation when a cached path goes stale)

### Neutral / Risks
- If a cached local path and a repo's actual remote diverge (e.g., remote renamed), resolution must re-establish identity — this is an implementation detail for Phase 1, not a design gap, but should have a defined fallback (prompt to re-link) before Phase 1 ships
