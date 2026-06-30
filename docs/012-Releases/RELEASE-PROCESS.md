# Release Process (Maintainer Guide)

| Field | Value |
|---|---|
| **Audience** | Ferret maintainers (publishing releases) |
| **Status** | Active |
| **Related** | ARCH-022 (Distribution Platform); per-version notes in `docs/012-Releases/v<version>.md` |

This is the maintainer-facing runbook for cutting a Ferret release. Customer-facing
release notes live separately in `docs/012-Releases/v<version>.md` and on the
GitHub Release; this document is the operational procedure behind them.

## One-time prerequisites

Before the **first** public npm publish:

- **npm scope** — create the `@indoulia` org/scope on npmjs.com, owned by the publishing account. `npm publish` rejects an unowned scope.
- **`NPM_TOKEN` secret** — generate a **granular automation** access token on npmjs.com (Read + Write to the `@indoulia` scope; no org-management permission; no IP restriction — GitHub runners use dynamic IPs). Add it as the `NPM_TOKEN` repository secret under **Settings → Secrets and variables → Actions**. The token expires; rotate before expiry or npm publishing will start failing with 401.
  - *Planned change:* after the first successful publish, npm publishing migrates to **Trusted Publishing (OIDC)**, which removes the stored `NPM_TOKEN`. Tracked separately.

## Release procedure

1. Ensure `main` is green and the version is set: `src/Ferret.Cli/Ferret.Cli.csproj` `<Version>` matches the release.
2. Write the customer-facing notes at `docs/012-Releases/v<version>.md`.
3. Tag the release commit on `main` and push the tag:
   ```bash
   git tag -a v<version> -m "<short milestone summary>"
   git push origin v<version>
   ```
4. `release.yml` builds the per-RID self-contained zips, `SHA256SUMS.txt`, and `release-manifest.json`, runs the manifest self-validation, and creates a **draft** GitHub Release with all assets attached (alongside the `*.nupkg`).
5. Set the draft's body from `docs/012-Releases/v<version>.md`:
   ```bash
   gh release edit v<version> --notes-file docs/012-Releases/v<version>.md
   ```
6. Review the draft — assets present, manifest correct, notes accurate.
7. **Publish the draft.** Publishing emits `release: published`, which triggers `npm-publish.yml` to publish `@indoulia/ferret@<version>` with `--access public`.

## Decoupling & failure handling

`release.yml` (creates the Release + assets) and `npm-publish.yml` (publishes to npm) are **separate workflows on separate triggers**. If the npm publish fails (missing scope/token, registry outage), the GitHub Release stays published and valid — every other consumer can still download and verify the assets via `release-manifest.json` / `SHA256SUMS.txt`. To recover, fix the cause and re-run `npm-publish.yml` (it supports `workflow_dispatch` with the version); no re-tag or re-release is needed.

## Re-tagging a failed build

If a release run fails **before** the draft Release is created (no assets published, nothing consumed), it is safe to fix the cause on `main`, delete and re-push the same tag:
```bash
git tag -d v<version>
git push origin :refs/tags/v<version>
git tag -a v<version> -m "<summary>"
git push origin v<version>
```
Do **not** re-tag once a Release has been published or the npm package version exists.

## Notes

- macOS artifacts are unsigned/unnotarized (see the per-version notes and ARCH-022).
- The release pipeline cross-builds all RIDs on a single Linux runner; packaging scripts must run under PowerShell Core (`pwsh`), so avoid Windows-PowerShell-only constructs and read-only automatic variables (e.g. `$IsWindows`).
