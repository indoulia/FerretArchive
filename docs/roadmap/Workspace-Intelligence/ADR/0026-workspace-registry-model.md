# ADR-0026 — Workspace Registry Model

| Field | Value |
|---|---|
| **Status** | Proposed — finalized for Founder approval (2026-07-05 finalization review closed identity/failure-mode/sharing-compatibility gaps found in the original draft) |
| **Date** | 2026-07-05 |
| **Deciders** | Founder |
| **Milestone** | Workspace Intelligence Platform, Phase 0 |
| **Supersedes** | — |

---

## Context

A workspace can now span multiple repository checkouts, documents, and references to other workspaces (see `../02-Workspace-Model.md`). No single repo's `.ai/` directory can hold this record, because it is not scoped to one repo. Something new must own the multi-repo workspace's identity, membership list, and reference list. Where that record lives determines whether shared workspaces and future cloud sync (both top Founder priorities) are possible without rework later.

## Decision

We will use an **identity-based local registry**: `~/.ferret/workspaces/<workspace-id>/workspace.json`, addressing member repos and referenced workspaces by durable identity (git remote URL, or workspace ID) with local checkout paths cached alongside, not used as the identity. Access is mediated by a new `IWorkspaceRegistry` interface, following the same pluggable-backend pattern ARCH-001 §19.3 already established for `IKnowledgeStore`.

### Identity Rules (closes a gap found in the 2026-07-05 ADR-0026 finalization review)

- **`workspaceId`:** a UUIDv4, generated client-side at `workspace create` time. Never reused, never derived from content or path.
- **Member repo identity:** the repo's `origin` remote URL, canonicalized (strip `.git` suffix; treat `git@host:path` and `https://host/path` as the same identity after canonicalization) before comparison or storage. If a repo has no `origin` remote, fall back to the next remote in `git remote` output, alphabetically, for determinism.
- **Local-only repos (no remote at all):** fall back to a locally-generated UUID, persisted in that repo's own `.ferret/workspace-identity.json` — a new file in the existing per-repo root directory (`Ferret.Core.Workspace.WorkspaceLayout.RootDirectoryName`, §19.2's storage area), not a new storage area. **Correction (WIP-012 implementation, 2026-07-05):** this originally said `.ai/workspace-identity.json`; ARCH-001 §19.2 documents `.ai/` as the storage-area root, but the actual shipped constant is `.ferret` (`WorkspaceLayout.RootDirectoryName`) — the code, not the doc, is authoritative here. See WIP-012's Self Review for the broader `.ai` vs `.ferret` documentation drift this surfaced. This identity travels with the repo only if `.ferret/` is preserved; a local-only repo cloned to a new machine without its `.ferret/` directory gets a new identity, which is correct — an unpushed repo has no server-verifiable durable identity to preserve in the first place.
- **Divergence recovery** (remote URL changed after registration — host migration, rename): resolution tries the stored identity first; on failure, prompts the user to re-link via the cached `localPath` if it still exists, updating the stored identity only on explicit confirmation. Never auto-relinks silently — a silent re-point could merge two genuinely different repos.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Path-based manifest in a parent directory (repos listed by relative/absolute path) | Ties a workspace to one machine's directory layout; breaks the moment a workspace is shared with a colleague or synced to another machine — directly forecloses two stated Founder priorities |
| Cloud-hosted registry from day one | No hosting infrastructure exists yet; breaks the local-first / air-gap invariant (FUTURE-002 Conditions 3–4) that Ferret has already committed to |

## Registry Storage: Layout, Atomicity, Failure Handling

- **Layout:** `~/.ferret/workspaces/<workspaceId>/workspace.json`, one directory per workspace. `List()` scans the `workspaces/` directory; at v1 scale (a handful to low hundreds of workspaces per developer) a directory scan is sufficient and requires no separate index file. An index file is an implementation detail to add later if scan cost becomes measurable — not an architecture decision.
- **Atomicity:** `Save(entry)` writes to a temp file in the same directory and renames over the target — the same crash-safe write pattern used elsewhere in the platform for any file the loss of which would corrupt state. A crash mid-write leaves the previous valid `workspace.json` intact, never a half-written one.
- **Corruption:** if `workspace.json` fails to parse, `Resolve()` fails closed — the workspace is reported unresolvable with a clear message (which file, why), never silently discarded or auto-repaired. Recovery is manual: recreate the workspace (cheap, see Backup below) or hand-fix the JSON.
- **Backup:** none is provided or required. A workspace registry entry is small, and re-creating one (re-running `add-repo`/`add-reference`) is cheap and idempotent. This is an explicit trade-off, not an oversight — building backup infrastructure for state this cheap to regenerate would be exactly the kind of speculative complexity this milestone is avoiding.
- **Deleted/missing member repo:** if a member repo's cached `localPath` no longer exists, that member is reported unresolvable in `workspace list`/query results (tagged, not silently dropped) — the workspace itself remains valid; only that member is degraded. Same treatment as an unreachable referenced workspace (`03-Cross-Workspace-References.md` §3).

## Sharing and Topology Compatibility

The default file-based `IWorkspaceRegistry` backend described above is scoped to `kind: "personal"` workspaces on a single developer's machine. It does **not**, by itself, make a `team`/`organization`/`shared-library` workspace visible to a second machine — nothing currently moves a registry entry between machines. That gap is real but does not block this ADR or Phase 1: ARCH-001 §26.4 (Team Server topology) already describes multiple developers sharing one Ferret instance and one filesystem, which is a natural home for a *shared* `IWorkspaceRegistry` backend selected by workspace `kind` — the same pluggable-backend mechanism this ADR already establishes. Designing that backend selection is Phase 5 implementation work (`workspace share`, WIP-050), not a new architecture decision, and is explicitly out of scope here per the Founder's instruction not to design sharing in this review.

## Consequences

### Positive
- Workspace identity survives being shared or synced to a different machine
- The same `IWorkspaceRegistry` abstraction can later be backed by a hosted service (Ferret Hub, V3) or a Team Server-shared backend (Phase 5) with no schema change to `workspace.json`
- Consistent with the existing `IMemoryStore` Local→Shared→Enterprise tiering pattern (FUTURE-002 §13.3)
- Identity rules above make the model correct for repos with no remote, multiple remotes, or a renamed remote — not just the common single-`origin` case

### Negative
- Slightly more implementation work up front than a path-based manifest (identity resolution, path caching/reconciliation when a cached path goes stale)

### Neutral / Risks
- Cross-machine duplicate-identity conflicts (two machines independently creating what's meant to be "the same" team workspace) are not addressed here because no sync mechanism exists yet to bring them into contact — this becomes a real question only when cloud sync or Team Server sharing is designed (Future/Deferred-Scope.md), not before
- Divergence recovery (re-linking a renamed remote) requires explicit user confirmation by design (see Identity Rules) — slightly more friction than silent auto-repair, traded deliberately for correctness
