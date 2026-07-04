# 12 — API Design

**Status:** Ready for implementation
**Extends:** ARCH-001 §22.3 (MCP Tools), §23.2 (CLI Command Hierarchy)

## 1. Design Rule

01-Architecture.md §3 already established that federation is invisible above the storage abstraction — so the smallest correct API surface is the one that adds *only* what's needed to manage workspace membership and references, and changes nothing about how queries are issued. Confirmed here: `Ferret knowledge query` / the MCP knowledge tools take zero new parameters.

## 2. New CLI Commands

| Command | Effect |
|---|---|
| `Ferret workspace create --kind <kind>` | Creates a workspace registry entry (02 §2) |
| `Ferret workspace add-repo <remote>` | Adds a member repo by identity |
| `Ferret workspace add-reference <workspace-id> [--pin <hash>]` | Adds an `IMPORTS` edge (03) |
| `Ferret workspace remove-reference <workspace-id>` | Removes it; fails if doing so would orphan a pinned dependency another member relies on — see 14-Migration.md for compatibility rules |
| `Ferret workspace list` | Lists members + references + reference health (11 §1) |
| `Ferret workspace share --role <owner\|admin\|developer\|viewer> --user <id>` | Grants a role (ADR-0029) |

No command is added for "query across workspaces" — that's just `Ferret knowledge query`, unchanged, now scoped by whichever workspace is active.

## 3. New MCP Tools

| Tool | Mirrors CLI command |
|---|---|
| `workspace_list` | `Ferret workspace list` |
| `workspace_add_reference` | `Ferret workspace add-reference` |

Existing MCP knowledge/query tools (§22.3) are unchanged — same rule as §2.

## 4. Decision Log

| Decision | Outcome |
|---|---|
| No new parameters on existing query CLI/MCP surface | Ready for implementation |
| New commands scoped strictly to workspace/reference/sharing management | Ready for implementation |
