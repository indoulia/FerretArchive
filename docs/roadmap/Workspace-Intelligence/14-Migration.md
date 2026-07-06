# 14 — Migration Strategy

**Status:** Ready for implementation
**Extends:** ARCH-001 §12.4 (Workspace Versioning and Upgrade), §27.1 (Future Architecture is additive by design)

## 1. The Invariant

Every existing single-repo `.ai/workspace.json` continues to work with zero required action from its owner. This is not a migration in the usual sense (nothing needs to happen for existing users) — it's a wrapping.

## 2. What Actually Happens

1. On first use of any `Ferret workspace` command in a checkout that has no workspace registry entry, Ferret auto-creates one: `kind: "personal"`, one member repo (the current checkout), zero references.
2. This uses the existing `schemaVersion` upgrade mechanism (§12.4) — a new field, not a breaking schema change. A workspace with `schemaVersion` below the version that introduces the registry gets the same validated-migration-path treatment §12.4 already defines for any schema bump.
3. All existing commands (`Ferret index build`, `Ferret knowledge query`, etc.) behave identically before and after this auto-creation, because a one-repo, zero-reference workspace is behaviorally identical to no workspace concept at all (00-Vision.md §2).

## 3. Failure Mode

If auto-creation fails (e.g., no write access to `~/.ferret/`), Ferret falls back to operating exactly as it does today, without a workspace registry entry, and logs a warning — it never blocks an existing command from working. This mirrors §12.4's existing rule: "If migration fails, the workspace is left unchanged and the error is reported."

## 4. Decision Log

| Decision | Outcome |
|---|---|
| Existing single-repo workspaces are auto-wrapped, not manually migrated | Ready for implementation |
| Registry auto-creation failure never blocks existing commands | Ready for implementation |
