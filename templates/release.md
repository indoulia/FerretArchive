# Release Notes — Ferret v[VERSION]

| Field | Value |
|---|---|
| **Version** | X.Y.Z |
| **Release Date** | YYYY-MM-DD |
| **Release Type** | Major \| Minor \| Patch \| Pre-release |
| **Author** | [name] |

---

## Highlights

<!--
2–4 bullet points. The most important things in this release.
-->

-
-

---

## What's New

### [Feature Area]
- **[Feature name]** — [one sentence description]. ([#PR] [#Issue])

---

## Improvements

- [Description of improvement]. ([#PR])

---

## Bug Fixes

- Fixed [description of bug]. ([#PR] [#Issue])

---

## Breaking Changes

> **Breaking changes require a major version bump per SemVer.**

| Component | Change | Migration |
|---|---|---|
| | | See [migration guide](../docs/guides/migrate-vX-to-vY.md) |

---

## Deprecations

| Item | Deprecated In | Removal Planned | Replacement |
|---|---|---|---|
| | | vX.Y | |

---

## Dependency Updates

| Package | From | To |
|---|---|---|
| | | |

---

## Known Issues

- [Issue description]. Workaround: [workaround]. Tracked in [#Issue].

---

## Upgrade Guide

```powershell
# Update via NuGet
dotnet add package Ferret.Core --version X.Y.Z

# Re-run bootstrap after upgrade
./scripts/bootstrap.ps1
```

---

_Template version: 1.0 — stored in `/templates/release.md`_
