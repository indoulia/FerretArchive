# Operational Discovery — Build Strategy Selection Ambiguity for Hybrid Repositories

| Field | Value |
|---|---|
| **Status** | Recorded — Evidence Only. No defect declared, no action taken. |
| **Date** | 2026-07-09 |
| **Source** | AEF (aef-platform) Repository Diagnostics, run against this repository during AEF's first production engineering cycle |
| **Category** | Operational Discovery (not a Bug, not a Decision) |
| **Affects** | This repository's own build/test verification when driven by AEF's Repository Validation capability |

---

## Summary

When AEF's Repository Validation capability (`REPO-VALIDATION`) was run against this repository, it selected `npm test` as the verification command rather than `dotnet test src/Ferret.sln` — the command this repository's own `README.md`, `CONTRIBUTING.md`, and live `.github/workflows/ci.yml` all name as the actual, authoritative build/test gate. On the host that ran it, `npm` was not installed, so the run failed with an environment error rather than a false pass — no fabricated result was produced.

This is filed as evidence about **AEF's own build-strategy-selection logic**, not as a defect in this repository. This repository's real structure and documented process are correct and unchanged by this finding.

## What was found

This repository is a genuinely Hybrid technology repository: a full .NET solution (`src/Ferret.sln`, real product code) alongside a Node.js/npm packaging concern (`Ferret.Npm/package.json`, backed by its own separate `npm-publish.yml` workflow — publishing, not testing). AEF's technology detector correctly identified both. AEF's build-command selector, however, has no tie-breaking logic for repositories with more than one detected technology — it deterministically picks whichever technology its own filesystem traversal happens to encounter first, which in this run was `node` (via `Ferret.Npm`, a root-level folder) ahead of `dotnet` (via `src/`, whose marker files sit one directory level deeper).

Full root-cause trace, evidence, and confidence levels are recorded in the originating AEF session (not duplicated here in full) — the essential fact for this repository's own record is: **AEF's current command selection cannot yet reliably tell that `dotnet test src/Ferret.sln` is this repository's real gate**, for a mechanical reason on AEF's side (unranked, traversal-order-dependent technology list), not because this repository's own conventions are ambiguous or under-documented.

## Disposition

Per Founder instruction: this is evidence, not a defect, and no implementation change is authorized on either side (AEF or this repository) from this single observation. If AEF is run against other repositories with more than one detected technology and the same ambiguity independently recurs, that would be enough evidence to justify AEF deriving a general Build Strategy Selection policy. Until then, this record stands as a single data point.

## No action taken

- This repository's own files, build configuration, and CI workflow are unchanged.
- No AEF code was modified.
- This repository's real verification gate remains `dotnet build src/Ferret.sln` / `dotnet test src/Ferret.sln`, exactly as already documented — this discovery does not change that.
