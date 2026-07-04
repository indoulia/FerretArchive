# 02 — Workspace Model

**Status:** Ready for implementation, pending ADR-0026 sign-off on registry location
**Extends:** ARCH-001 §12 (Workspace Architecture), §12.3 (Workspace Metadata)

## 1. The Core Change

Today, one `.ai/workspace.json` = one repo checkout = one workspace. That triple collapses. Going forward:

```
Workspace (id: ws_...)
    member repos       (0..N — each keeps its own .ai/ index, unchanged)
    member documents    (docs, ADRs, notes, specs not tied to a repo)
    references          (0..N other workspaces, read-only — see 03-Cross-Workspace-References.md)
```

A workspace with exactly one member repo and zero references is indistinguishable in behavior from today's single-repo workspace. This is the backward-compatibility invariant (see 14-Migration.md).

## 2. Where a Workspace Lives (ADR-0026)

A workspace's own `.ai/` per repo is unaffected — repos still index themselves the same way (§12, §19). What's new is a **workspace registry entry** that sits above any single repo checkout:

```
~/.ferret/workspaces/<workspace-id>/workspace.json
```

**Recommendation (pending Founder sign-off, ADR-0026):** identity-based, not path-based. Member repos and referenced workspaces are addressed by a durable identity (git remote URL, or a workspace ID for references) with the local checkout path cached alongside it, not used as the identity itself.

Why this matters: a path-based manifest (`repos: ["../service-a", "../service-b"]`) ties a workspace to one machine's directory layout. That breaks the moment a workspace is shared with a colleague whose checkout lives somewhere else, and it gives cloud sync (top-priority requirement) nothing to synchronize — paths aren't portable. An identity-based registry is the same pattern ARCH-001 already uses for knowledge state (content hash, not path) and the pattern FUTURE-002 §13.3 uses for memory tiers (`IMemoryStore` — same interface, different backend at Local/Shared/Enterprise level). A local file-based registry today can become a hosted registry backend later (Ferret Hub, V3) via the same abstraction swap, with no schema change to `workspace.json` itself.

## 3. Workspace Manifest Schema

```json
{
  "schemaVersion": "1.0",
  "workspaceId": "ws_8f2a...",
  "name": "customer-platform",
  "kind": "team",
  "members": {
    "repos": [
      { "remote": "git@github.com:acme/service-a.git", "localPath": "C:/dev/service-a" }
    ],
    "documents": [
      { "path": "C:/dev/notes/auth-decisions", "type": "notes" }
    ]
  },
  "references": [
    { "workspaceId": "ws_1a9c...", "mode": "read-only", "pinnedStateHash": null }
  ],
  "sharing": { "ownerId": "user_...", "visibility": "team" }
}
```

`kind` implements the enterprise workspace model (Objective 4): `personal | team | organization | shared-library | collection`. This is a closed enum in v1 — new kinds require the same ARB approval path as a new Product Domain (FD-003 §Governance), since a workspace kind has governance implications (who can create one, default sharing behavior), not just a technical one.

`sharing.visibility` and roles are defined in `Future/Deferred-Scope.md` / ADR-0029 — v1 ships Owner/Admin/Developer/Viewer only, not the full 5-role model in the original brief.

## 4. Knowledge Graph Additions

Extends ARCH-001 §13.2's node/edge table (additive — no existing node or edge type changes):

```
New Nodes:
  Workspace        (id, kind, name, schemaVersion)

New Edges:
  CONTAINS         (Workspace → SourceSymbol|Document|ADR|Specification, via member repo/doc)
  IMPORTS          (Workspace → Workspace, the reference relationship — see 03)
```

## 5. Decision Log

| Decision | Outcome |
|---|---|
| Workspace registry is identity-based, not path-based | Requires Founder decision (recommendation: identity-based) — ADR-0026 |
| `kind` enum limited to 5 values, closed, ARB-gated for new values | Ready for implementation |
| Existing single-repo `.ai/workspace.json` format is unchanged | Ready — hard constraint, see 14-Migration.md |
| Full sharing role model (5 roles incl. AI Agent) | Deferred — v1 ships 4 roles, see `Future/Deferred-Scope.md` |
