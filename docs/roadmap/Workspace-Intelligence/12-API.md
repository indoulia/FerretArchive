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

~~No command is added for "query across workspaces" — that's just `Ferret knowledge query`, unchanged, now scoped by whichever workspace is active.~~

**Corrected 2026-07-09 (WIP-038 implementation review, superseding the line above):** `Ferret knowledge query` as named here does not exist as a literal command — the real single-repo query surface is `ferret search`, hardwired to one CWD-resolved `IWorkspaceContext`. Making it workspace-registry-aware was found, during the vertical-slice build, to require new infrastructure outside that slice's scope; `16-Vertical-Slice-Validation.md` §"New CLI surface" already validated the actual, shipped answer — an additive `ferret workspaces query <workspace> <text>` command, zero regression risk to `ferret search` — but that correction was never back-ported to this doc until now. Treat `16-Vertical-Slice-Validation.md` as authoritative over this line.

## 3. New MCP Tools

| Tool | Mirrors CLI command |
|---|---|
| `workspaces_list` | `Ferret workspaces list` |
| `workspaces_add_reference` | `Ferret workspaces add-reference` |
| `workspace_query` | `Ferret workspaces query` — **added WIP-038**, MCP parity for the federated-query surface introduced by WIP-SLICE-1/2 (see correction above); previously the only federation-capable surface was the CLI, leaving MCP clients (Ferret's primary AI-agent surface) unable to use federation at all |

Existing MCP knowledge/query tools (§22.3 — `search`, `ferret_context`) are unchanged — same rule as §2; they remain single-workspace and are not made federation-aware by WIP-038. Not implemented by WIP-012 — see WIP-014 (`workspace_list`) and WIP-038 (`workspace_query`).

## 4. Decision Log

| Decision | Outcome |
|---|---|
| No new parameters on existing query CLI/MCP surface | Ready for implementation |
| New commands scoped strictly to workspace/reference/sharing management | Ready for implementation |
| CLI group renamed `workspace` → `workspaces` to avoid colliding with the existing per-repo `workspace init`/`status` commands | Ready — corrected during WIP-012 implementation |
| Workspace lookup by ID or name; name uniqueness enforced at the CLI layer, not the registry | Ready — corrected during WIP-012 implementation |
| Federated query needs its own additive CLI surface (`workspaces query`) rather than retrofitting `ferret search` | Ready — corrected and validated during WIP-SLICE-1/2 (`16-Vertical-Slice-Validation.md`), back-ported here WIP-038 |
| MCP tool `workspace_query` added as parity for `workspaces query`, reusing `FederatedKnowledgeStore`/`CachingFederatedKnowledgeStore` unchanged (WIP-030/031 caching, WIP-040 logging preserved) | Ready — WIP-038 |
