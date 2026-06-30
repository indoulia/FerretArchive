# Distribution Platform — Design

**Milestone:** Distribution Platform (NPM as first consumer)
**Date:** 2026-06-30
**Status:** Approved design — ready for implementation planning

## Architectural Spine

**GitHub Releases are the single source of truth.** Every distribution channel
consumes published release assets through a stable, hosting-agnostic contract:
`release-manifest.json`. NPM is the first consumer. Homebrew, winget,
Chocolatey, Scoop, and enterprise mirrors reuse the same contract with no
pipeline redesign.

```
                 GitHub Release
                       │
              release-manifest.json
                       │
      ┌────────┬────────┬────────┬────────┐
      │        │        │        │        │
     NPM   Homebrew  winget  Chocolatey  Enterprise
```

NPM never knows how Ferret is built. It only knows how to read the manifest and
download official release assets.

## Scope

In scope:

1. Extend `release.yml` to publish per-RID self-contained binary zips.
2. Publish a top-level `SHA256SUMS.txt` and `release-manifest.json`.
3. Upload all assets to the GitHub Release (additive — existing `.nupkg`
   publishing is preserved).
4. Build the NPM wrapper that detects platform, reads the manifest, downloads
   the correct zip, verifies SHA256, extracts, installs, and launches Ferret.
5. Validate the complete flow by installing Ferret from NPM on a clean machine.

Out of scope (this milestone):

- macOS code signing / notarization. macOS artifacts are unsigned and
  unnotarized — an intentional limitation of this milestone, to be addressed
  if/when macOS becomes a first-class supported platform. macOS users will see
  a Gatekeeper prompt.
- Additional consumers (Homebrew, winget, Chocolatey, Scoop). The manifest is
  designed to support them later; none are implemented now.
- No business logic, no .NET source changes, no bundled binaries, no TypeScript.

## Approach Decisions

1. **Single Ubuntu build runner.** Binaries are self-contained single-file, so
   `publish.ps1` cross-compiles all four RIDs from one Linux runner via `pwsh`.
   No per-OS matrix — it would add cost and time without improving artifacts.
2. **Separate manifest generator.** `package.ps1` stays single-RID (one RID →
   one zip). A new `build-release-manifest.ps1` runs after all four zips exist,
   hashes each, and emits the top-level `SHA256SUMS.txt` + `release-manifest.json`.
   Both scripts stay reusable and independently testable.
3. **Additive `release.yml`.** Keep the existing `.nupkg` publish; add binary-zip
   and manifest publishing alongside it. No regression risk.

```
Build → Tests → NuGet → Binary Packages → Manifest → GitHub Release
```

## Component 1 — Release Pipeline (`release.yml`)

On tag push, after build/test, a new job:

- Runs `publish.ps1` (all 4 RIDs: `win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`)
  → `package.ps1` per RID → four `Ferret-<version>-<rid>.zip`.
- Runs `build-release-manifest.ps1` → top-level `SHA256SUMS.txt` (lowercase hex,
  `hash  filename`, sha256sum-compatible) and `release-manifest.json`.
- Uploads zips + `SHA256SUMS.txt` + `release-manifest.json` to the GitHub
  Release, alongside the existing `.nupkg`.

## Component 2 — The Contract (`release-manifest.json`)

**The release manifest is the public contract of the Distribution Platform.**
It is a versioned public API, not an implementation detail. Consumers MUST read
the manifest to resolve assets. Consumers MUST NOT infer filenames, scrape HTML,
or enumerate release assets.

```
GitHub → manifest → asset → download
```

This gives the platform freedom to evolve asset naming without breaking any
consumer.

```json
{
  "schemaVersion": 1,
  "version": "0.14.0",
  "releaseTag": "v0.14.0",
  "published": "<workflow date>",
  "minimumInstallerSchema": 1,
  "assets": [
    {
      "rid": "win-x64",
      "file": "Ferret-0.14.0-win-x64.zip",
      "sha256": "...",
      "binary": "ferret.exe"
    }
  ]
}
```

Field meanings:

- `schemaVersion` — version of this manifest's structure. Lets consumers
  evolve safely.
- `version` / `releaseTag` — Ferret release version and its git tag.
- `published` — release date, supplied by the workflow.
- `minimumInstallerSchema` — the minimum installer schema this release expects.
  An installer compares its own schema support and either continues or tells the
  user to upgrade the installer.
- `assets[]` — one entry per RID: `rid`, `file` (zip name), `sha256`, and
  `binary` (the executable name inside the zip: `ferret` or `ferret.exe`).

### Manifest compatibility flow

```
Manifest schema → Compatible? → Continue
                              ↘ Upgrade installer
```

## Component 3 — NPM Wrapper (`Ferret.Npm/`)

Repository-agnostic. One runtime dependency (`extract-zip`); everything else is
Node standard library.

```
Ferret.Npm/
  package.json                 // bin: ferret → bin/ferret.js
                               // postinstall → scripts/install.js
                               // deps: extract-zip (only)
  bin/ferret.js                // launcher: locate installed binary, spawn,
                               //   forward argv + stdio, propagate exit code
  scripts/install.js           // postinstall orchestrator
  scripts/uninstall.js         // remove binary + temp; preserve user data
  lib/distribution-config.js   // owner, repository, releaseEndpoint (optional);
                               //   env-overridable; NO hardcoded URLs
  lib/platform.js              // process.platform/arch → RID; unsupported error
  lib/manifest.js              // fetch + parse + schema-check release-manifest.json
  lib/download.js              // fetch zip to temp dir
  lib/verify.js                // sha256(file) === manifest.sha256, else hard fail
  lib/extract.js               // extract-zip wrapper → install dir
  lib/paths.js                 // per-OS install + temp dirs
```

### Ownership boundary

We own Ferret-specific logic; we reuse mature solutions for commodity concerns.

| Concern                 | Own | Reuse                |
|-------------------------|-----|----------------------|
| GitHub Release access   |     | fetch (built-in)     |
| ZIP extraction          |     | extract-zip          |
| SHA256 hashing          |     | crypto (built-in)    |
| HTTP downloads          |     | fetch (built-in)     |
| Platform detection      | ✅  |                      |
| Binary selection        | ✅  |                      |
| Version resolution      | ✅  |                      |
| Manifest parsing        | ✅  |                      |
| Install workflow        | ✅  |                      |

### Distribution Configuration

`lib/distribution-config.js` defines:

- `owner` — GitHub owner (default `indoulia`).
- `repository` — repo name (default `Ferret`).
- `releaseEndpoint` — *optional*. When absent, the GitHub URL is constructed as
  `https://github.com/{owner}/{repository}`. Only enterprises (GitHub Enterprise,
  Azure DevOps, internal CDN, private mirror) need a custom endpoint.

All values are environment-overridable. No URLs are hardcoded anywhere else in
the wrapper.

### Reserved concept — `IReleaseSource`

Reserved, not implemented. Today there is effectively one release source
(GitHub). The design keeps the door open for `GitHubEnterprise`, `AzureDevOps`,
`InternalCDN`, or a private enterprise mirror by routing all release access
through the distribution config rather than scattering source-specific logic.
No abstraction is built now — only the concept is reserved.

## Data Flow (Install)

```
npm i -g @indoulia/ferret@X
  → postinstall reads its own package.json version X
  → distribution-config builds release URL for tag vX
  → fetch + parse release-manifest.json (schema check)
  → platform resolves RID
  → select asset for RID
  → download zip to temp
  → verify sha256 (fail hard on mismatch)
  → extract to install dir
  → mark executable (chmod +x on POSIX)
  → clean temp
  → done
bin/ferret.js (every invocation): locate installed binary, spawn, forward args
```

## Install Locations (platform conventions)

- Windows: `%LOCALAPPDATA%\Programs\Ferret` (matches Git, VS Code User Installer,
  Azure CLI, and the existing `install.ps1` target).
- macOS: `~/Library/Application Support/Ferret`
- Linux: `~/.local/share/ferret`
- Temp downloads under an OS temp subdir; cleaned after extract.

## Error Handling

Every failure is actionable and aborts cleanly — no half-install:

- Unsupported platform/architecture.
- Release/manifest 404 (version not published).
- `schemaVersion` newer than the installer supports → tell user to upgrade
  the installer.
- No asset for the resolved RID.
- Download failure (with retry).
- **Checksum mismatch → delete the file, hard fail.**
- Extraction failure.

Uninstall removes only the installed binary and temporary files. It NEVER
touches user `.ferret` workspaces, indexes, or configuration.

## Versioning, Update, Uninstall

- NPM package version === Ferret release version (version-locked, no floating
  latest lookup). `npm install @indoulia/ferret@0.14.0` installs Ferret 0.14.0.
- `npm update -g @indoulia/ferret` pulls the newer binary.
- `npm uninstall -g @indoulia/ferret` runs `scripts/uninstall.js`.
- CI: a publish-to-NPM step on release sets the package version from the tag and
  publishes under the `@indoulia` scope. A future organization migration may
  introduce a new scope or package name via configuration, not code changes.

## Distribution Dependency Policy

Runtime dependencies in distribution tooling require architectural review.

- **Maximum runtime dependencies: 2.**
- Initial runtime dependencies: `extract-zip` (only).
- Everything else comes from Node: `crypto`, `fetch`, `fs`, `path`,
  `child_process`, `stream`.
- DevDependencies: `eslint`, `prettier`.
- No additional third-party package without an explicit design review, and only
  for a fundamental cross-platform problem the standard library cannot
  reasonably address.

This keeps the wrapper lean and trustworthy.

## Testing

- `node:test` (built-in — no test-framework dependency):
  - platform/arch → RID mapping (including unsupported-platform error)
  - manifest parse + schemaVersion / minimumInstallerSchema guard
  - verify: checksum match and mismatch
  - distribution-config environment override
  - install-path resolution per OS
- Manual end-to-end on a clean machine as the milestone acceptance gate:

```bash
npm install -g @indoulia/ferret
ferret --version
ferret init
ferret index
ferret search "ContextAssembler"
ferret serve
npm update -g @indoulia/ferret
npm uninstall -g @indoulia/ferret
```

## ADRs to Record

1. **Distribution Principle** — GitHub Releases as single source of truth;
   consumers read the manifest (never infer filenames / scrape HTML / enumerate
   assets); own-logic vs. commodity-reuse boundary; maximum 2 runtime
   dependencies in distribution tooling.
2. **Repository-agnostic distribution config** — no hardcoded URLs, no
   hosting-provider assumption; `owner` / `repository` / optional
   `releaseEndpoint`; `IReleaseSource` concept reserved.
3. **Release manifest as versioned public API** — `release-manifest.json` is the
   public contract, documented with a schema version and compatibility
   expectations; all current and future distribution mechanisms consume it.

## Milestone Framing

The effort is a product milestone, not a numbered sprint:

```
RC1 ✅ → Distribution Platform → Dogfooding → Benchmarking → GA Readiness
```

The engineering work remains, but the primary goal has shifted from building
capabilities to making the product easy to obtain, validate, and adopt.
