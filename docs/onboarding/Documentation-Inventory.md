# Documentation Inventory

> Part of the AEF first-time onboarding package for Ferret. This document catalogs the repository's documentation and reports quality issues found by reading — nothing has been fixed, consolidated, or removed. See `AEF-Onboarding-Validation.md` for how these findings roll up into PASS/WARNING/FAIL classifications.

## Doc Count by Top-Level `docs/` Folder

| Folder | Files | Notes |
|---|---|---|
| `archive/` | 99 | `dogfooding/`, `pkm/`, `sprint-reviews/`, `superpowers/` subfolders. Explicitly labeled in `docs/README.md` as "superseded/historical, kept for provenance only — not part of current onboarding reference." |
| `roadmap/` | 39 | Includes `Future/`, `Workspace-Intelligence/` (the active v2 program) |
| `002-Architecture/` | 36 | ARCH-001 through ARCH-037 plus `decisions/` (see below) |
| `adr/` | 20 | 18 numbered ADRs + template + README |
| `Reviews/` | 7 | AR-*/AGR-* governance review records |
| `001-Product/` | 6 | PRD, roadmap, competitive analysis |
| `000-Overview/` | 6 | Vision, Mission, Principles, Glossary, PROJECT-STATE |
| `012-Releases/` | 5 | Release notes + process runbook |
| `benchmarks/`, `013-Governance/`, `007-SDK/` | 2 each | |
| `011-Performance/`, `010-Security/`, `009-Testing/`, `008-Modules/`, `006-CLI/`, `005-MCP/`, `004-Database/`, `003-Workspace/` | 1 each | Stub `README.md` only, mostly untouched since the 2026-06-30 initial commit |

Root: `README.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `LICENSE`. `.github/`: `CODEOWNERS`, issue templates, PR template. `.ai/`: 30 files (agent/checklist/command/workflow scaffolding plus live session-state files).

## Freshness Highlights

| Doc | Last touched | Observation |
|---|---|---|
| `docs/000-Overview/PROJECT-STATE.md` | 2026-07-07 (path fix only; content not refreshed since 2026-06-29) | Self-declares "must be kept current" but is severely stale — see below |
| `docs/adr/README.md`, `docs/013-Governance/DECISION-LOG.md`, `docs/roadmap/Workspace-Intelligence/README.md` | 2026-07-06 | Current and well-maintained |
| `docs/001-Product/ROADMAP-001.md` | Not updated since early sprints | States a different "current sprint" than PROJECT-STATE.md — see the three-way contradiction below |
| `docs/012-Releases/README.md` | 2026-06-30 | Its own release index still says "(no releases yet)" despite v0.15.0/v0.16.0 existing in the same folder |
| Most `00X-*/README.md` stubs (003–011) | 2026-06-30 | Untouched "day-1 scaffolding," several describing subsystems that are now fully implemented |

## Confirmed Inconsistencies

1. **Three ADR/decision locations** (`docs/adr/`, `docs/roadmap/Workspace-Intelligence/ADR/`, `docs/002-Architecture/decisions/`) — only the first is indexed as canonical; the third is invisible from the canonical index and uses a different ID format (`ADR-004` vs `0004`). Full detail in `Architecture-Inventory.md`.
2. **`docs/012-Releases/README.md` contradicts its own folder** — index says no releases exist; `v0.15.0.md`, `v0.16.0.md`, `RC1-Validation-Report.md` say otherwise.
3. **Dangling links left by an incomplete cleanup pass.** The most recent commit (`904d05f`, "chore: strip AI-session operational artifacts for fresh AEF onboarding") deleted `docs/architecture/`, `docs/database/`, `docs/api/`, `docs/guides/`, `docs/specs/`, and `docs/002-Architecture/overview.md`, but missed updating: `SECURITY.md:53` (still links to `docs/guides/security-hardening.md`), `templates/README.md:14-22` (still tables the deleted flat `docs/specs|architecture|api|database/` tree), `templates/release.md:48` (still links to `docs/guides/migrate-vX-to-vY.md`), and `docs/002-Architecture/README.md:27,61-62` (still indexes/links the deleted `overview.md`). The commit's own message claims all such references were fixed — these four were not.
4. **Section indexes stale relative to their own folder contents** — `docs/001-Product/README.md` omits `COMPETITIVE-001.md` and `ROADMAP-002-Future-Vision.md`, which exist in the same folder; `docs/007-SDK/README.md` says "to be added in Sprint 1" despite `SDK-001.md` existing there.
5. **Overlapping "future vision" documents with no precedence order**: `docs/002-Architecture/FUTURE-001-Future-Architecture.md`, `FUTURE-002-Enterprise-Intelligence-Vision.md`, `docs/001-Product/ROADMAP-002-Future-Vision.md`, and `docs/roadmap/Future/Deferred-Scope.md` cover overlapping V2–V4/enterprise territory from different eras with no cross-reference.
6. **`docs/roadmap/Workspace-Intelligence/README.md`'s numbered reading order (00–30) interleaves pure design docs with dated sprint-journal/retro content** as if they were peer chapters.
7. **`docs/archive/pkm/README.md` does not self-identify as archived** — reads as a live, current index ("Current Status: PKM v0.1 Released") with no banner; only the folder-level `docs/README.md` note marks the whole `archive/` tree as historical.
8. **`docs/archive/pkm/Validation-Report.md`** has `Status: Approved` but `Owner: TODO`, `Approved By: TODO`, `Last Updated: TODO`.
9. **The three-way "what sprint is this" contradiction** (the most significant single finding of this onboarding pass):
   - `docs/000-Overview/PROJECT-STATE.md`: "Current sprint: Sprint 13 — Context Assembly (Not yet started)."
   - `docs/001-Product/ROADMAP-001.md`: "Current Sprint / Sprint 10 — Information Retrieval."
   - `.ai/session.md` / `.ai/current-context.json`: reset to empty/pristine "Not yet started" state.
   - None of the three mentions Sprint 14+ (v0.16.0), the Workspace Intelligence Platform, ADR-0026–0030, or Epic 5 — all of which are real, merged, `main`-branch work.

**This is not accidental drift.** The `904d05f` commit message explicitly states it left "PROJECT-STATE.md staleness, the orphaned `docs/002-Architecture/decisions/` ADR-numbering gap, overlapping future-vision docs, stale Sprint-0 placeholder READMEs, and the `docs/roadmap/Workspace-Intelligence/17-28` sprint-journal docs embedded in the live roadmap sequence... intentionally preserved as real-world signal for the upcoming fresh-onboarding validation run." Findings 1–9 above should be read as the intended validation surface for this exercise, not as newly-discovered defects requiring urgent triage.

## Suspected-but-Unverified

- ~10 of ~37 catalogued ARCH-* documents are listed as "Planned"/unlinked in `docs/002-Architecture/README.md` — could be intentional sequencing rather than abandonment; not independently confirmed either way.
- Whether `docs/002-Architecture/decisions/sprint-3-technology-evaluation.md` was ever formally superseded by `TECH-001-Technology-Evaluation.md` — no supersession note found in either file.
- `.worktrees/v2-workspace-intelligence/` still contains the pre-cleanup flat doc tree and stale copies of `PROJECT-STATE.md`/`README.md` — expected worktree behavior (it's an old checkout), not a defect in the main tree, but could confuse a search that doesn't realize it's a separate branch snapshot.

## Archive Hygiene

`docs/archive/` is clearly demarcated at the folder level (`docs/README.md` labels it historical/not-for-onboarding), and the `904d05f` cleanup commit's `git mv` preserved history while updating the small number of live cross-references that pointed at old locations. The gap is at the file level: individual archived docs (e.g. `docs/archive/pkm/README.md`) don't self-identify as archived, so a reader who opens one directly (e.g. via search) rather than browsing from `docs/README.md` would not know it's historical.

## Gaps / Unknowns

- `docs/006-CLI`, `004-Database`, `005-MCP`, `008-Modules`, `009-Testing`, `010-Security`, `011-Performance` have no content beyond a stub README — cannot assess quality of content that doesn't exist yet.
- No full repo-wide link-checker pass was run; only a sample of links from the most structurally important docs were spot-checked. More dangling references than the four found above are plausible.
- `.ai/` directory (30 files: agents, checklists, workflows, templates) was inventoried but not content-reviewed for quality — sizeable and out of scope for this pass.
- No doc explains why `docs/IDEAS.md`, `docs/MIGRATION-001.md`, and `docs/RESEARCH-001-Future-Research.md` sit unfoldered at `docs/` root while everything else follows the numbered `0NN-*` convention.
