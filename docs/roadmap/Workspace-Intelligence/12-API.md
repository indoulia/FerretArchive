# 12 — API Design

**Status:** Ready for implementation
**Extends:** ARCH-001 §22.3 (MCP Tools), §23.2 (CLI Command Hierarchy)

## 1. Design Rule

01-Architecture.md §3 already established that federation is invisible above the storage abstraction — so the smallest correct API surface is the one that adds *only* what's needed to manage workspace membership and references, and changes nothing about how queries are issued. Confirmed here: `Ferret knowledge query` / the MCP knowledge tools take zero new parameters.

## 2. New CLI Commands

**Corrected 2026-07-05 (WIP-012 implementation review):** the commands below were originally proposed under the existing `Ferret workspace` group. That group already exists for a *different* concept — the per-repo `.ai/`/`.ferret` workspace (ARCH-001 §12, `ferret workspace init` / `ferret workspace status`) — and `workspace create/add-repo/list` would have collided with it under the same noun. Renamed to a new `workspaces` (plural) group; `ferret workspace init`/`status` are completely unchanged. This was found during implementation, not anticipated when this doc was first written — see WIP-012's Self Review for why it wasn't caught earlier.

| Command | Effect |
|---|---|
| `Ferret workspace init` / `Ferret workspace status` | **Unchanged** — the existing per-repo workspace commands (ARCH-001 §12) |
| `Ferret workspaces create --name <name> [--kind personal\|team]` | Creates a workspace registry entry (02 §2) |
| `Ferret workspaces list` | Lists all workspaces (id, name, kind, repo count) |
| `Ferret workspaces show <id-or-name>` | Full detail for one workspace (kind, every member repo/document) |
| `Ferret workspaces add-repo <id-or-name> <path>` | Adds a member repo, identity resolved from `<path>`'s own git config per ADR-0026 |
| `Ferret workspaces remove-repo <id-or-name> <path>` | Removes a member repo (matched by the same identity resolution as add-repo) |
| `Ferret workspaces add-reference <id-or-name> <target-id-or-name> [--pin <hash>]` | Adds an `IMPORTS` edge (03) — **Phase 2, not WIP-012** |
| `Ferret workspaces remove-reference <id-or-name> <target-id-or-name>` | Removes it — **Phase 2, not WIP-012** |
| `Ferret workspaces share --role <owner\|admin\|developer\|viewer> --user <id>` | Grants a role (ADR-0029) — **Phase 5, not WIP-012** |

`<id-or-name>` accepts either the workspace's UUID or its `name`. Name-based lookup is safe because `workspaces create` rejects a duplicate name (CLI-layer validation — see WIP-012's Self Review; the registry itself does not enforce name uniqueness).

No command is added for "query across workspaces" — that's just `Ferret knowledge query`, unchanged, now scoped by whichever workspace is active.

## 3. New MCP Tools

| Tool | Mirrors CLI command |
|---|---|
| `workspaces_list` | `Ferret workspaces list` |
| `workspaces_add_reference` | `Ferret workspaces add-reference` |

Existing MCP knowledge/query tools (§22.3) are unchanged — same rule as §2. Not implemented by WIP-012 — see WIP-014.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| No new parameters on existing query CLI/MCP surface | Ready for implementation |
| New commands scoped strictly to workspace/reference/sharing management | Ready for implementation |
| CLI group renamed `workspace` → `workspaces` to avoid colliding with the existing per-repo `workspace init`/`status` commands | Ready — corrected during WIP-012 implementation |
| Workspace lookup by ID or name; name uniqueness enforced at the CLI layer, not the registry | Ready — corrected during WIP-012 implementation |
