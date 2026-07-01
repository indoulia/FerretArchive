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

- **npm scope** — the `@indoulia` org/scope must exist on npmjs.com, owned by the publishing account. `npm publish` rejects an unowned scope.
- **Trusted Publishing (OIDC)** — `npm-publish.yml` authenticates to npm via GitHub Actions OIDC; there is **no stored npm token**. Configure the package's trusted publisher once on npmjs.com:
  - npmjs.com → the `@indoulia/ferret` package → **Settings → Trusted Publisher** → add **GitHub Actions** with: repository `indoulia/Ferret`, workflow file `.github/workflows/npm-publish.yml` (and environment if one is used).
  - The workflow grants `id-token: write` and upgrades npm to ≥ 11.5.1 (OIDC trusted publishing requirement). Provenance attestation is produced automatically.
  - The legacy `NPM_TOKEN` repository secret is no longer used and can be deleted once a release has published successfully via OIDC.

> Historical note: the inaugural `v0.15.0` publish used a granular `NPM_TOKEN` automation token to minimize variables during first rollout. Publishing migrated to Trusted Publishing immediately afterward.

- **Public distribution mirror** — the installer downloads release binaries anonymously, and GitHub only serves release assets without authentication from **public** repositories. Download assets are therefore published to the public mirror repo **`indoulia/ferret-dist`** (source can stay private). Configure once:
  - The mirror repo `indoulia/ferret-dist` must exist and be **public** (assets only, no source).
  - Add a repository secret **`DIST_REPO_TOKEN`** to `indoulia/Ferret` — a token with `contents: write` on `indoulia/ferret-dist` (fine-grained PAT scoped to that repo, or a classic PAT with `repo`). The default `GITHUB_TOKEN` cannot write to a different repository, so the mirror step needs this.
  - The installer's download host is defined by `Ferret.Npm/lib/distribution-config.js` (defaults to `indoulia/ferret-dist`) and overridable via `FERRET_DIST_OWNER` / `FERRET_DIST_REPO` / `FERRET_DIST_RELEASE_ENDPOINT`.

## Release procedure

1. Ensure `main` is green and the version is set: `src/Ferret.Cli/Ferret.Cli.csproj` `<Version>` matches the release.
2. Write the customer-facing notes at `docs/012-Releases/v<version>.md`.
3. Tag the release commit on `main` and push the tag:
   ```bash
   git tag -a v<version> -m "<short milestone summary>"
   git push origin v<version>
   ```
4. `release.yml` builds the per-RID self-contained zips, `SHA256SUMS.txt`, and `release-manifest.json`, runs the manifest self-validation, and creates a **draft** GitHub Release with all assets attached (alongside the `*.nupkg`). It then publishes the download payload (zips, `SHA256SUMS.txt`, `release-manifest.json`) to the **public** mirror `indoulia/ferret-dist` as a **published** release, and runs `Ferret.Npm/scripts/verify-download-endpoint.js` to confirm the manifest is anonymously reachable. If the mirror is missing/private or `DIST_REPO_TOKEN` is unset, the release fails here rather than shipping an npm package that 404s on install.
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
