# ARCH-022 — Distribution Platform Architecture

| Field | Value |
|---|---|
| **Document ID** | ARCH-022 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending Architecture Review |
| **Date** | 2026-06-30 |
| **Last Updated** | 2026-06-30 |
| **Related ADRs** | Distribution Principle; Repository-agnostic distribution config; Release manifest as versioned public API |
| **Related Spec** | `docs/superpowers/specs/2026-06-30-distribution-platform-design.md` |
| **Parent Architecture** | ARCH-001 §Distribution Layer |

---

## Purpose

This document defines the architecture of Ferret's Distribution Platform — the
layer that turns a build into installable, verifiable software and delivers it
to end users. It covers the distribution principles, the release-manifest
contract, the release pipeline, the consumer model, the installer architecture,
and the dependency policy.

This document describes *structure and contracts*, not implementation. It does
not document the specific code of any installer, generator, or workflow; those
live in their source files and in the implementation plan. The manifest schema
and the boundary rules defined here are authoritative.

---

## Scope

**Covers:**
- Distribution principles (single source of truth; consumer model)
- The `release-manifest.json` contract as a versioned public API
- The release pipeline (build → package → manifest → release)
- Release/consumer decoupling
- Installer architecture (resolve → fetch manifest → verify → atomic install → launch)
- Repository-agnostic configuration
- The distribution dependency policy

**Does not cover:**
- Implementation details of any specific consumer or script
- Feature behaviour of the Ferret binary itself
- macOS code signing / notarization (a known, out-of-scope limitation of this milestone)

---

## 1. Overview

The Distribution Platform is the fifth major platform layer in Ferret,
following the Connector, Ingestion, Retrieval, and AI platforms. Where those
layers give Ferret its capabilities, the Distribution Platform makes those
capabilities *obtainable*: a user with no .NET SDK, no source checkout, and no
developer environment can install and run Ferret.

Its organising principle is a single source of truth. The build produces a set
of release artifacts; those artifacts, described by a stable manifest, are the
only thing any distribution channel consumes. NPM is the first consumer.
Homebrew, winget, Chocolatey, Scoop, an enterprise mirror, and a future
`ferret self-update` are all anticipated consumers of the same contract, and
none requires a change to how Ferret is built or released.

```
Build → Package → Manifest → GitHub Release → [ NPM | Homebrew | winget | Chocolatey | Enterprise mirror ]
```

---

## 2. Distribution Principles

1. **GitHub Releases are the single source of truth.** Every channel consumes
   published release assets. There is no second build, no per-channel
   repackaging of binaries.
2. **Consumers read the manifest.** A consumer MUST resolve assets through
   `release-manifest.json`. It MUST NOT infer filenames, scrape HTML, or
   enumerate release assets. This lets asset naming evolve without breaking any
   consumer.
3. **No implementation leaks across boundaries.** A consumer knows how to read
   the manifest and download an asset; it does not know how Ferret is built. The
   pipeline knows how to produce artifacts; it does not know about any consumer.
4. **Verify before use.** Every downloaded artifact is checked against the
   SHA256 recorded in the manifest before it is installed; a mismatch is a hard
   failure.

---

## 3. The Release Manifest Contract

`release-manifest.json` is the public contract of the Distribution Platform — a
**versioned public API**, not an implementation detail. It is frozen at
`schemaVersion = 1`. Any change to its shape requires incrementing
`schemaVersion` and updating both the producer and the consumers' tests; no
breaking change is permitted within a schema version.

Fields:

- `schemaVersion` — structure version of the manifest (currently `1`).
- `version` / `releaseTag` — the Ferret release version and its git tag.
- `published` — the release date, supplied deterministically by the pipeline.
- `minimumInstallerSchema` — the minimum installer schema this release requires.
  An installer that supports a lower schema must refuse and ask the user to
  upgrade the installer.
- `metadata` — reserved provenance namespace (`generator`, `generatorVersion`).
- `assets[]` — one entry per RID: `rid`, `file`, `size`, `sha256`, `binary`.

Compatibility rule: an installer parses the manifest, and if
`minimumInstallerSchema` exceeds the schema it supports, it refuses to proceed
rather than guessing. The producer validates every generated manifest with the
*same* parser the installer uses, so a manifest the installer would reject can
never be published.

---

## 4. Release Pipeline

```
Build → Tests → NuGet → Binary Packages (per RID) → Manifest → GitHub Release
```

The pipeline is additive over the pre-existing release process: NuGet packages
continue to publish, and the per-RID self-contained binary zips, the
`SHA256SUMS.txt`, and `release-manifest.json` are produced and uploaded
alongside them. Binaries for all supported RIDs are cross-built on a single
runner because they are self-contained and single-file. Artifact generation is
deterministic: stable, known-RID asset ordering; fixed filenames; and an
explicitly supplied `published` date.

Supported RIDs: `win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`. macOS artifacts
are unsigned and unnotarized — a known, intentional limitation of this
milestone.

---

## 5. Release / Consumer Decoupling

The creation of a release and its consumption are independent. The pipeline
publishes the GitHub Release with its assets (zips, `SHA256SUMS.txt`,
`release-manifest.json`); publishing to a downstream channel is a *separate*
step that runs only after the release is live.

**If a consumer's publish step fails, the GitHub Release remains valid.** NPM is
just another consumer: a failed `npm publish` does not roll back, invalidate, or
alter the release. The release assets stand on their own and remain installable
by every other channel (and by direct download). This decoupling is structural —
release creation and channel publishing are distinct workflows triggered by
distinct events.

Channel publishing authenticates without a stored long-lived secret: npm
publishing uses **Trusted Publishing (OIDC)** — GitHub Actions mints a
short-lived identity token that npm exchanges for publish rights, and provenance
is attested automatically. There is no npm token to store, rotate, or leak.

---

## 6. Consumer Model

A consumer is anything that installs Ferret from the release. Every consumer
follows the same shape:

```
resolve target (RID) → fetch manifest → select asset → download → verify SHA256 → install → launch
```

Consumers share the manifest contract and differ only in their packaging
conventions and install locations. Adding a consumer requires no pipeline
change.

---

## 7. Installer Architecture

The installer (per consumer) is a thin, well-bounded set of units, each with a
single responsibility: platform/RID resolution, manifest fetch + validation,
download with retry, checksum verification, archive extraction, install-path
resolution, and a launcher that forwards invocation to the native binary.

**Atomic installation.** Extraction never overwrites the active installation in
place. The installer extracts to a staging location and swaps it into the final
install directory as the last step (with a cross-device copy fallback). If any
earlier step fails, the existing installation is untouched and no partial
install is left behind.

**Data preservation.** Uninstalling removes only the installed binary and
temporary files. User workspaces, indexes, and configuration are never touched.

**Install locations** follow per-OS conventions (per-user, no admin rights).

---

## 8. Repository-agnostic Configuration

No distribution component hardcodes a host or URL. Configuration is limited to
three keys: `owner`, `repository`, and an optional `releaseEndpoint`. When the
endpoint is absent, the standard public host URL is constructed from
owner/repository; when present (an enterprise mirror, GitHub Enterprise, an
internal CDN, a private release server), it is used verbatim. The concept of an
alternative release source (`IReleaseSource`) is reserved but not implemented:
all release access routes through this configuration rather than scattered
host-specific logic, so a future migration is a configuration change, not a
code change.

---

## 9. Distribution Dependency Policy

Distribution tooling stays small and trustworthy. Runtime dependencies require
architectural review and are capped at **two**. Commodity concerns are reused
from mature libraries or the language standard library; Ferret-specific logic
is owned.

| Concern | Own | Reuse |
|---|---|---|
| Release access | | standard HTTP client |
| Archive extraction | | a single vetted library |
| Checksum hashing | | standard library |
| Platform/RID detection | ✅ | |
| Asset selection | ✅ | |
| Version resolution | ✅ | |
| Manifest parsing | ✅ | |
| Install workflow | ✅ | |

---

## 10. Milestone Framing

The Distribution Platform is a product milestone, not a feature sprint:

```
RC1 → Distribution Platform → Dogfooding → Benchmarking → GA Readiness
```

It completes the platform stack — Development, Runtime, and Distribution — and
shifts the project's focus from building capabilities to making the product easy
to obtain, validate, and adopt.
