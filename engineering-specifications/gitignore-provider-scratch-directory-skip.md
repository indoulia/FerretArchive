```
Specification Type: Standard
Implementation Readiness: Ready with Assumptions
External Tracker Reference: Ferret issue #45
```

## Goal / Problem Statement

Exact class-name searches (`search("IWorkspaceRegistry")`, `search("FileWorkspaceRegistry")`) return AI-agent development-scratch content from `.superpowers/sdd/` in every top-10 slot, ahead of the classes' own real source files and governing ADR. Root cause, confirmed directly against source: `GitIgnoreProvider` (`src/Ferret.Connectors.Filesystem/Ignore/GitIgnoreProvider.cs:17`) reads exactly one, root-level `.gitignore` and has no logic to discover or apply nested `.gitignore` files anywhere else in the tree. `.superpowers/sdd/` carries its own nested `.gitignore` (a bare `*`, confirmed present), which real `git` honors but Ferret's indexer never sees, because the root `.gitignore` itself has no matching entry (confirmed: no `superpowers` line exists in it).

## Success Criteria

- `search("IWorkspaceRegistry")` and `search("FileWorkspaceRegistry")`, run against a freshly rebuilt index, no longer surface any `.superpowers/sdd/` path.
- The classes' real source files and governing ADR are not newly excluded or demoted as a side effect.
- No behavior change for any repository that does not contain a directory matching the new skip entry.

## Existing Capability Analysis

- `FilesystemConnector.HardcodedSkipDirs` (`FilesystemConnector.cs:12-16`) already exists — `.git`, `.ferret`, `.svn`, `.hg`, `.worktrees`, `node_modules`, `bin`, `obj`, `packages` — and is already applied on both the full directory walk (`WalkDirectoryAsync`) and the targeted-lookup path (`HasHardcodedSkipAncestor`, used by `TryGetAsync`), per the code's own stated invariant that the two paths must never disagree (`FilesystemConnector.cs:225-228`).
- `GitIgnoreProvider` applies exactly one, root-level `.gitignore` (confirmed: `Path.Join(rootPath, ".gitignore")`, no nested-file discovery anywhere in this class or its tests).
- `FerretIgnoreProvider` (`Ignore/FerretIgnoreProvider.cs`), chained via `CompositeIgnoreProvider`, already gives a repository an opt-in, root-level `.ferretignore` mechanism — a user could add a `.superpowers/` line there today, with zero code change. This does not fix the systemic default: it requires a user to already know to opt in, and does nothing for a repository that never creates one.
- This is insufficient because nothing in the existing mechanism causes common AI-agent scratch-directory conventions to be skipped by default.

## Functional Requirements

Add `.superpowers` (a literal directory name, matching `HardcodedSkipDirs`'s existing case-insensitive comparer) to `FilesystemConnector.HardcodedSkipDirs`, so it is honored identically wherever the existing set is already consulted — both `WalkDirectoryAsync`'s per-`DirectoryInfo` check and `HasHardcodedSkipAncestor`'s ancestor-segment check.

## Out of Scope

- General nested-`.gitignore` discovery (issue #45's Disposition Option 1) — a larger change to `GitIgnoreProvider`'s own file-discovery logic, not attempted here.
- Any AI-agent scratch-directory convention not already observed in this repository.
- Any change to `.ferretignore`/`GitIgnoreProvider` behavior for paths not matching the new hardcoded entry.

## Risks

- Carried directly from the issue's own text: a hardcoded name does not generalize — it will not catch a different scratch-directory convention under a different name, and will recur for the next one.
- A hardcoded directory-name skip is coarser than real `.gitignore` semantics; it could in principle skip a legitimately-named `.superpowers` directory that is not scratch content in some other repository, though no such case is known today.

## Assumptions

- **This Specification assumes Disposition Option 2 from issue #45** ("add `.superpowers/sdd/`, or a pattern for common AI-agent scratch directories, to `HardcodedSkipDirs`") **is the option being authorized**, not Option 1 (general nested-`.gitignore` support) or Option 3 (both). The issue itself records this choice as **"Needs Founder Decision"** — unresolved by any repository artifact. This Specification does not make that decision; it drafts against the narrower option, consistent with keeping this one task small, and surfaces the choice explicitly rather than silently deciding it. See `## Clarification Log`.
- Ferret's repository context is treated as already known from this AEF program's own prior, real dogfooding history against this same repository, not from a freshly invoked Repository Onboarding Assessment (`AGT-EXE-0007`) in this session — no persisted Technology/Project Profile artifact exists under `Ferret/.ai/` to consume instead. Recorded here as an assumption, not silently absorbed.
- The skip token is assumed to be the literal name `.superpowers` (matching how `HardcodedSkipDirs` already stores literal names, not glob patterns) — broad enough to cover `.superpowers/sdd/` and any sibling path under `.superpowers/`, narrow enough not to match unrelated directories.

## Acceptance Criteria

- A new automated test in `Ferret.Connectors.Filesystem.Tests` (alongside the existing `FilesystemConnectorDiscoveryTests.cs`) proves a `.superpowers` directory is skipped on both the walk and targeted-lookup paths, mirroring the existing tests' own pattern for the other `HardcodedSkipDirs` entries.
- `search("IWorkspaceRegistry")` / `search("FileWorkspaceRegistry")`, run live against a freshly rebuilt index, no longer return any `.superpowers/sdd/` path — reproducing the exact two queries issue #45 itself used.
- No regression in existing `Ferret.Connectors.Filesystem.Tests`.

## Test Strategy

Unit-level: extend `FilesystemConnectorDiscoveryTests.cs`'s existing per-skip-directory test pattern with one new case for `.superpowers`. Live verification: re-run the exact two `search(...)` queries from issue #45 against a rebuilt index — the same reproduction method the issue itself used and this Specification's own Existing Capability Analysis independently re-confirmed against real source.

## Clarification Log

- **Q:** Which of issue #45's three disposition options should this Specification target? **A:** Not answered by any repository artifact — the issue itself records this as "Needs Founder Decision." This Specification proceeds under the Assumption above (Option 2) and surfaces it for Business Approval rather than choosing silently.
