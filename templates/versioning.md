# Versioning Policy

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Version** | 1.0 |
| **Date** | 2026-06-27 |

---

## Scheme

Ferret follows [Semantic Versioning 2.0.0](https://semver.org/):

```
MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
```

| Segment | Increment when… |
|---|---|
| **MAJOR** | A breaking change is introduced (API, CLI, plugin contract) |
| **MINOR** | Backward-compatible new functionality is added |
| **PATCH** | Backward-compatible bug fixes only |
| **Pre-release** | `alpha`, `beta`, `rc.N` — not production-ready |

---

## Pre-release Labels

| Label | Stability | Audience |
|---|---|---|
| `0.x.y-alpha` | Unstable, APIs may break | Core contributors |
| `0.x.y-beta` | Feature-complete, hardening | Early adopters |
| `0.x.y-rc.N` | Release candidate | Broad testing |
| `1.0.0` | GA — stable API contract | All users |

---

## Branch Strategy

| Branch | Purpose | Version |
|---|---|---|
| `main` | Latest stable release | Tagged release |
| `develop` | Integration branch | `X.Y.Z-beta` |
| `feat/*` | Feature branches | N/A |
| `hotfix/*` | Production hotfixes | `X.Y.(Z+1)` |
| `release/vX.Y` | Release stabilisation | `X.Y.Z-rc.N` |

---

## NuGet Package Versioning

- Package version = assembly version = `VersionPrefix` in `Directory.Build.props`.
- CI sets `/p:VersionPrefix=$(git describe --tags)` on tagged builds.
- Symbol packages (`.snupkg`) are published alongside every NuGet package.

---

## Plugin Contract Compatibility

Plugins declare a `minRuntimeVersion` in `plugin.json`.  
The Ferret runtime enforces this at load time — incompatible plugins are rejected with a clear error.

---

_Template version: 1.0 — stored in `/templates/versioning.md`_
