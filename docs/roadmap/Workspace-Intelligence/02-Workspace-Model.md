# 02 — Workspace Model

**Status:** Ready for implementation, pending Founder approval of ADR-0026 (identity rules, storage, and simplifications finalized 2026-07-05; no remaining open design questions)
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

**Simplified 2026-07-05 (ADR-0026 finalization review):** the original draft shipped `references` and `sharing` in the v1.0 schema even though nothing reads or writes them until Phase 2 and Phase 5 respectively, and pre-committed to a 5-value `kind` enum when 3 of those values have no consumer until Enterprise/Future work. Both are unused-surface-area at Phase 1. Trimmed to what Phase 1 actually needs; `references` and `sharing` are added by later `schemaVersion` bumps exactly when Phase 2/5 need them, exercising the existing upgrade mechanism (§12.4) incrementally instead of assuming it works, untested, until much later.

**v1.0 (Phase 1) schema:**

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
  }
}
```

`kind` is `personal | team` in v1.0 — the only two values with a Phase 1–4 consumer. `organization | shared-library | collection` are real values from the enterprise workspace model (Objective 4) but are added when Phase 5 or Future/Deferred-Scope work actually needs them, via the same ARB-gated path FD-003 §Governance already requires for a new Product Domain-adjacent value. `remote` is canonicalized per ADR-0026's Identity Rules, not stored as given.

**v1.1 (added at Phase 2, additive schemaVersion bump):** `references: [{ workspaceId, mode, pinnedStateHash }]` — see `03-Cross-Workspace-References.md`.

**v1.2 (added at Phase 5, additive schemaVersion bump):** `sharing: { ownerId, visibility }` — roles defined in ADR-0029; v1 ships Owner/Admin/Developer/Viewer only, not the full 5-role model in the original brief.

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
| Workspace registry is identity-based, not path-based | Ready for implementation — ADR-0026 finalized and recommended for approval |
| Repo identity is canonicalized `origin` remote URL, with defined fallbacks for no-remote and multi-remote repos | Ready for implementation — ADR-0026 Identity Rules |
| `kind` enum: ship `personal \| team` in v1.0; `organization \| shared-library \| collection` added later via schemaVersion bump when a consumer exists | Ready for implementation — simplified 2026-07-05 |
| `references` and `sharing` fields excluded from v1.0 schema, added via schemaVersion bumps at Phase 2 and Phase 5 | Ready for implementation — simplified 2026-07-05 |
| Existing single-repo `.ai/workspace.json` format is unchanged | Ready — hard constraint, see 14-Migration.md |
| Full sharing role model (5 roles incl. AI Agent) | Deferred — v1 ships 4 roles, see `Future/Deferred-Scope.md` |
