# 012 — Releases

Release notes and upgrade guides for Ferret.

---

## Index

| Version | Date | Type | Document |
|---|---|---|---|
| _(no releases yet)_ | | | |

---

## Release Process

1. Feature freeze on `develop` branch
2. Create `release/vX.Y` branch
3. Write release notes using [docs/templates/release.md](../templates/release.md)
4. Final testing and hardening
5. Merge to `main` and tag `vX.Y.Z`
6. GitHub Release created automatically by CI (see `.github/workflows/release.yml`)
7. NuGet packages published to NuGet.org

---

## Versioning Policy

See [docs/templates/versioning.md](../templates/versioning.md) for the full SemVer policy.

---

## Planned Milestones

| Milestone | Target | Description |
|---|---|---|
| 0.1.0-alpha | TBD | Core runtime + CLI proof of concept |
| 0.2.0-beta | TBD | MCP integration + plugin system |
| 1.0.0 | TBD | GA — stable API contract |
