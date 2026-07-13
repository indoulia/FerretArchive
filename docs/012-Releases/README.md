# 012 — Releases

Release notes and upgrade guides for Ferret.

---

## Index

| Version | Date | Type | Document |
|---|---|---|---|
| 2.0.0 | 2026-07-13 | Minor (Workspace Intelligence Platform) | [v2.0.0.md](v2.0.0.md) |
| 0.16.0 | 2026-07-01 | Patch (Enterprise Content Pack 1) | [CHANGELOG.md](../../CHANGELOG.md#0160--enterprise-content-pack-1--2026-07-01) |
| 0.15.0 | 2026-06-30 | Patch (Distribution Platform) | [v0.15.0.md](v0.15.0.md) |
| 0.14.0 | 2026-06-29 | Patch (RC1) | [RC1-Validation-Report.md](RC1-Validation-Report.md) |

---

## Release Process

See [RELEASE-PROCESS.md](RELEASE-PROCESS.md) for the maintainer-facing runbook. Summary:
releases are tagged directly on `main` (no `develop`/`release/vX.Y` branches) once
`src/Ferret.Cli/Ferret.Cli.csproj`'s `<Version>` is bumped and per-version customer-facing
notes exist at `docs/012-Releases/v<version>.md`. Tagging triggers `.github/workflows/release.yml`,
which builds cross-platform assets, creates a draft GitHub Release, and publishes to the
public `indoulia/ferret-dist` mirror; publishing the draft triggers `npm-publish.yml` to
publish `@indoulia/ferret` via OIDC Trusted Publishing.
