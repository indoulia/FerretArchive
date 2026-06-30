# Rebrand Scope: AISpace → Ferret

**Generated:** 2026-06-28  
**Task:** Sprint 5 — Rebranding Assessment (Task 16)  
**Commit target:** `feat!: rebrand AISpace to Ferret`  
**Tag:** `v0.5.0-ferret`

---

## Summary

| Category | Files requiring changes | Occurrences |
|---|---|---|
| Solution file | 1 | — |
| Source .csproj files | 9 | — |
| Test .csproj files | 9 | — |
| Sample .csproj file | 1 | — |
| Source folders to rename | 9 | — |
| Test folders to rename | 9 | — |
| Sample folders to rename | 1 | — |
| Directory.Build.props | 1 | 4 |
| Source .cs files (namespace/using) | 113 | 180 |
| Test .cs files (namespace/using) | 40 | 119 |
| Sample .cs files | 1 | 1 |
| CI/CD workflow files | 3 | 12 |
| PowerShell scripts | 2 | 5 |
| Root-level docs (README, CONTRIBUTING, etc.) | 5 | 13 |
| docs/ markdown files | 50 | 1753 |
| AssemblyInfo / InternalsVisibleTo | 1 | 1 |

**Total files requiring changes: ~145**  
(Excludes `.claude/worktrees/` which are ephemeral agent copies, and `obj/`/`bin/` build artifacts.)

---

## 1. Solution and Project Files

### 1.1 Solution file

| Current | Renamed to |
|---|---|
| `src/AISpace.sln` | `src/Ferret.sln` |

### 1.2 Source project files and folder renames

| Current folder | Current .csproj | Renamed folder | Renamed .csproj |
|---|---|---|---|
| `src/AISpace.Core/` | `AISpace.Core.csproj` | `src/Ferret.Core/` | `Ferret.Core.csproj` |
| `src/AISpace.Runtime/` | `AISpace.Runtime.csproj` | `src/Ferret.Runtime/` | `Ferret.Runtime.csproj` |
| `src/AISpace.Cli/` | `AISpace.Cli.csproj` | `src/Ferret.Cli/` | `Ferret.Cli.csproj` |
| `src/AISpace.Configuration/` | `AISpace.Configuration.csproj` | `src/Ferret.Configuration/` | `Ferret.Configuration.csproj` |
| `src/AISpace.Mcp/` | `AISpace.Mcp.csproj` | `src/Ferret.Mcp/` | `Ferret.Mcp.csproj` |
| `src/AISpace.Plugins/` | `AISpace.Plugins.csproj` | `src/Ferret.Plugins/` | `Ferret.Plugins.csproj` |
| `src/AISpace.Plugin.SDK/` | `AISpace.Plugin.SDK.csproj` | `src/Ferret.Plugin.SDK/` | `Ferret.Plugin.SDK.csproj` |
| `src/AISpace.Telemetry/` | `AISpace.Telemetry.csproj` | `src/Ferret.Telemetry/` | `Ferret.Telemetry.csproj` |

> Note: `src/AISpace.Runtime/` exists in the codebase (not listed in initial glob due to ordering). Confirmed present via file listing.

### 1.3 Test project files and folder renames

| Current folder | Current .csproj | Renamed folder | Renamed .csproj |
|---|---|---|---|
| `tests/AISpace.Core.Tests/` | `AISpace.Core.Tests.csproj` | `tests/Ferret.Core.Tests/` | `Ferret.Core.Tests.csproj` |
| `tests/AISpace.Runtime.Tests/` | `AISpace.Runtime.Tests.csproj` | `tests/Ferret.Runtime.Tests/` | `Ferret.Runtime.Tests.csproj` |
| `tests/AISpace.Cli.Tests/` | `AISpace.Cli.Tests.csproj` | `tests/Ferret.Cli.Tests/` | `Ferret.Cli.Tests.csproj` |
| `tests/AISpace.Configuration.Tests/` | `AISpace.Configuration.Tests.csproj` | `tests/Ferret.Configuration.Tests/` | `Ferret.Configuration.Tests.csproj` |
| `tests/AISpace.Mcp.Tests/` | `AISpace.Mcp.Tests.csproj` | `tests/Ferret.Mcp.Tests/` | `Ferret.Mcp.Tests.csproj` |
| `tests/AISpace.Plugins.Tests/` | `AISpace.Plugins.Tests.csproj` | `tests/Ferret.Plugins.Tests/` | `Ferret.Plugins.Tests.csproj` |
| `tests/AISpace.Plugin.SDK.Tests/` | `AISpace.Plugin.SDK.Tests.csproj` | `tests/Ferret.Plugin.SDK.Tests/` | `Ferret.Plugin.SDK.Tests.csproj` |
| `tests/AISpace.Telemetry.Tests/` | `AISpace.Telemetry.Tests.csproj` | `tests/Ferret.Telemetry.Tests/` | `Ferret.Telemetry.Tests.csproj` |
| `tests/AISpace.Integration.Tests/` | `AISpace.Integration.Tests.csproj` | `tests/Ferret.Integration.Tests/` | `Ferret.Integration.Tests.csproj` |

### 1.4 Sample project files and folder renames

| Current folder | Current .csproj | Renamed folder | Renamed .csproj |
|---|---|---|---|
| `samples/plugins/AISpace.Plugins.Sample/` | `AISpace.Plugins.Sample.csproj` | `samples/plugins/Ferret.Plugins.Sample/` | `Ferret.Plugins.Sample.csproj` |

---

## 2. Namespace Occurrences

**Total `namespace AISpace` / `using AISpace` occurrences: 300** (180 in src, 119 in tests, 1 in samples)

### 2.1 Source — by project

| Project | .cs files affected | Occurrences |
|---|---|---|
| `AISpace.Core` | ~90 | ~130 |
| `AISpace.Runtime` | ~20 | ~47 |
| `AISpace.Cli` | 1 | 1 |
| `AISpace.Configuration` | 1 | 1 |
| `AISpace.Mcp` | 1 | 1 |
| `AISpace.Plugins` | 1 | 1 |
| `AISpace.Plugin.SDK` | 1 | 1 |
| `AISpace.Telemetry` | 1 | 1 |

### 2.2 Tests — by project

| Project | .cs files affected | Occurrences |
|---|---|---|
| `AISpace.Core.Tests` | ~14 | ~42 |
| `AISpace.Runtime.Tests` | ~18 | ~65 |
| `AISpace.Cli.Tests` | 1 | 1 |
| `AISpace.Configuration.Tests` | 1 | 1 |
| `AISpace.Mcp.Tests` | 1 | 1 |
| `AISpace.Plugins.Tests` | 1 | 1 |
| `AISpace.Plugin.SDK.Tests` | 1 | 1 |
| `AISpace.Telemetry.Tests` | 1 | 1 |
| `AISpace.Integration.Tests` | 1 | 1 |
| `AISpace.Runtime.Tests` (integration) | 1 | 5 |

### 2.3 Samples

| Project | .cs files affected | Occurrences |
|---|---|---|
| `AISpace.Plugins.Sample` | 1 | 1 |

---

## 3. Assembly Names and Root Namespaces

All `<AssemblyName>` and `<RootNamespace>` properties are set explicitly in every .csproj. All reference AISpace.

### Source projects

| File | AssemblyName | RootNamespace |
|---|---|---|
| `src/AISpace.Core/AISpace.Core.csproj` | `AISpace.Core` | `AISpace.Core` |
| `src/AISpace.Runtime/AISpace.Runtime.csproj` | `AISpace.Runtime` | `AISpace.Runtime` |
| `src/AISpace.Cli/AISpace.Cli.csproj` | `AISpace.Cli` | `AISpace.Cli` |
| `src/AISpace.Configuration/AISpace.Configuration.csproj` | `AISpace.Configuration` | `AISpace.Configuration` |
| `src/AISpace.Mcp/AISpace.Mcp.csproj` | `AISpace.Mcp` | `AISpace.Mcp` |
| `src/AISpace.Plugins/AISpace.Plugins.csproj` | `AISpace.Plugins` | `AISpace.Plugins` |
| `src/AISpace.Plugin.SDK/AISpace.Plugin.SDK.csproj` | `AISpace.Plugin.SDK` | `AISpace.Plugin.SDK` |
| `src/AISpace.Telemetry/AISpace.Telemetry.csproj` | `AISpace.Telemetry` | `AISpace.Telemetry` |

### Test projects

| File | AssemblyName | RootNamespace |
|---|---|---|
| `tests/AISpace.Core.Tests/AISpace.Core.Tests.csproj` | `AISpace.Core.Tests` | `AISpace.Core.Tests` |
| `tests/AISpace.Runtime.Tests/AISpace.Runtime.Tests.csproj` | `AISpace.Runtime.Tests` | `AISpace.Runtime.Tests` |
| `tests/AISpace.Cli.Tests/AISpace.Cli.Tests.csproj` | `AISpace.Cli.Tests` | `AISpace.Cli.Tests` |
| `tests/AISpace.Configuration.Tests/AISpace.Configuration.Tests.csproj` | `AISpace.Configuration.Tests` | `AISpace.Configuration.Tests` |
| `tests/AISpace.Mcp.Tests/AISpace.Mcp.Tests.csproj` | `AISpace.Mcp.Tests` | `AISpace.Mcp.Tests` |
| `tests/AISpace.Plugins.Tests/AISpace.Plugins.Tests.csproj` | `AISpace.Plugins.Tests` | `AISpace.Plugins.Tests` |
| `tests/AISpace.Plugin.SDK.Tests/AISpace.Plugin.SDK.Tests.csproj` | `AISpace.Plugin.SDK.Tests` | `AISpace.Plugin.SDK.Tests` |
| `tests/AISpace.Telemetry.Tests/AISpace.Telemetry.Tests.csproj` | `AISpace.Telemetry.Tests` | `AISpace.Telemetry.Tests` |
| `tests/AISpace.Integration.Tests/AISpace.Integration.Tests.csproj` | `AISpace.Integration.Tests` | `AISpace.Integration.Tests` |

### Additional: Directory.Build.props

`Directory.Build.props` at repo root contains:
```xml
<Product>AISpace</Product>
<Company>AISpace Contributors</Company>
<Copyright>Copyright © 2026 AISpace Contributors</Copyright>
<Authors>AISpace Contributors</Authors>
```
All four values must be updated to `Ferret` / `Ferret Contributors`.

### Additional: AssemblyInfo / InternalsVisibleTo

`src/AISpace.Runtime/Properties/AssemblyInfo.cs` line 3:
```csharp
[assembly: InternalsVisibleTo("AISpace.Runtime.Tests")]
```
Must be updated to `Ferret.Runtime.Tests`.

---

## 4. NuGet Package IDs

`Directory.Packages.props` contains **no AISpace.* package references** — no NuGet package IDs need renaming. The file is used for version pinning only.

No .csproj files declare a `<PackageId>` pointing to an AISpace.* ID (packages are not yet published).

---

## 5. Documentation Files

**50 doc files** under `docs/` contain "AISpace" with a combined 1753 occurrences. Files are grouped by required treatment:

### 5.1 Historical/archival — handle with care (preserve "AISpace (renamed to Ferret during Sprint 5)")

These files have high AISpace density and record decisions made when the product was named AISpace. A surgical "add context note at top + bulk-replace product name" strategy is required:

| File | Occurrences | Risk |
|---|---|---|
| `docs/superpowers/plans/2026-06-27-sprint-2-repository-bootstrap.md` | 330 | HIGH — historical sprint plan, bulk replace risky |
| `docs/superpowers/plans/2026-06-28-sprint-5-runtime-host.md` | 307 | HIGH — historical sprint plan |
| `docs/superpowers/plans/2026-06-27-sprint-3-platform-kernel.md` | 291 | HIGH — historical sprint plan |
| `docs/superpowers/plans/2026-06-28-sprint-4-platform-foundation.md` | 267 | HIGH — historical sprint plan |
| `docs/superpowers/plans/2026-06-27-sprint-1-foundation-completion.md` | 86 | HIGH — historical sprint plan |
| `docs/001-Product/PRD-001.md` | 55 | HIGH — product definition doc |
| `docs/002-Architecture/ARCH-001.md` | 99 | HIGH — core architecture doc |
| `docs/000-Overview/Vision.md` | 25 | HIGH — vision doc, replace with Ferret + add rename note |
| `docs/007-SDK/SDK-001.md` | 25 | MEDIUM |
| `docs/superpowers/plans/2026-06-27-architecture-improvements.md` | 35 | MEDIUM |
| `docs/adr/0011-rename-aispace-sdk-to-aispace-plugin-sdk.md` | 20 | HIGH — ADR documents internal rename, title must stay accurate and note this was pre-Ferret rename |

### 5.2 Standard replacement — bulk replace safe

| File | Occurrences |
|---|---|
| `docs/002-Architecture/ARCH-003.md` | 22 |
| `docs/002-Architecture/ARCH-TEMPLATE-001.md` | 16 |
| `docs/000-Overview/Glossary.md` | 16 |
| `docs/002-Architecture/ARCH-013.md` | 8 |
| `docs/002-Architecture/ARCH-014.md` | 8 |
| `docs/000-Overview/Mission.md` | 21 |
| `docs/000-Overview/Principles.md` | 11 |
| `docs/002-Architecture/decisions/ADR-004-runtime-engine-container.md` | 9 |
| `docs/Reviews/AR-002.md` | 12 |
| `docs/Reviews/AR-001.md` | 6 |
| `docs/001-Product/ROADMAP-001.md` | 6 |
| `docs/002-Architecture/decisions/sprint-3-technology-evaluation.md` | 5 |
| `docs/008-Modules/README.md` | 7 |
| `docs/007-SDK/README.md` | 5 |
| `docs/002-Architecture/overview.md` | 17 |
| `docs/architecture/overview.md` | 3 |
| `docs/002-Architecture/ARCH-011.md` | 3 |
| `docs/002-Architecture/README.md` | 3 |
| `docs/001-Product/sprint-0-project-foundation.md` | 3 |
| `docs/009-Testing/README.md` | 3 |
| `docs/005-MCP/README.md` | 3 |
| `docs/architecture/README.md` | 2 |
| `docs/adr/0001-use-architecture-decision-records.md` | 2 |
| `docs/api/README.md` | 2 |
| `docs/specs/sprint-0-project-foundation.md` | 3 |
| `docs/adr/README.md` | 1 |
| `docs/specs/README.md` | 1 |
| `docs/guides/README.md` | 1 |
| `docs/006-CLI/README.md` | 1 |
| `docs/010-Security/README.md` | 1 |
| `docs/011-Performance/README.md` | 1 |
| `docs/012-Releases/README.md` | 1 |
| `docs/004-Database/README.md` | 1 |
| `docs/001-Product/README.md` | 1 |
| `docs/000-Overview/README.md` | 3 |
| `docs/README.md` | 1 |
| `docs/superpowers/plans/2026-06-28-sprint-6-cli-host.md` | 2 |

---

## 6. CI/CD and Scripts

### 6.1 GitHub Actions workflows

| File | Occurrences | Lines affected |
|---|---|---|
| `.github/workflows/ci.yml` | 5 | `dotnet restore src/AISpace.sln`, `dotnet build src/AISpace.sln`, `dotnet test src/AISpace.sln`, `dotnet format src/AISpace.sln` |
| `.github/workflows/release.yml` | 5 | Same pattern + `AISpace v${{ steps.version.outputs.VERSION }}` release name |
| `.github/workflows/security.yml` | 3 | `dotnet build src/AISpace.sln`, `dotnet restore src/AISpace.sln`, `dotnet list src/AISpace.sln` |

All three workflows reference `src/AISpace.sln` and must be updated to `src/Ferret.sln`.  
The release workflow also has the literal string `AISpace v${{ ... }}` as the GitHub release name — update to `Ferret v${{ ... }}`.

### 6.2 PowerShell scripts

| File | Occurrences | Content |
|---|---|---|
| `scripts/bootstrap.ps1` | 4 | "Bootstraps the AISpace development workspace" + 3x `src/AISpace.sln` references + `` "`n AISpace workspace is ready." `` |
| `scripts/init-workspace.ps1` | 1 | `src/AISpace.sln` |

### 6.3 Other

No Makefile, Dockerfile, or shell scripts found.

---

## 7. String Literals in Source Code

### 7.1 AISpaceException class (highest priority)

`src/AISpace.Core/Errors/AISpaceException.cs` — the base exception class is named `AISpaceException` and appears in 9 occurrences (class name, XML doc comments, constructors). This becomes `FerretException`. All derived exceptions that inherit from it are unaffected by class name but their namespace changes.

### 7.2 Comment strings referencing "AISpace"

Found in multiple files as XML doc comments and inline comments. Key examples:
- `src/AISpace.Runtime/Bootstrap/RuntimeOptions.cs`: XML doc comment "Layer: AISpace.Runtime only"
- `src/AISpace.Cli/Program.cs`: comment "AISpace CLI entry point"
- `src/AISpace.Core/Errors/AISpaceException.cs`: XML doc "Base class for all AISpace platform exceptions"

### 7.3 AssemblyInfo / InternalsVisibleTo

`src/AISpace.Runtime/Properties/AssemblyInfo.cs`:
```csharp
[assembly: InternalsVisibleTo("AISpace.Runtime.Tests")]
```
Must change to `Ferret.Runtime.Tests` after the test project is renamed.

### 7.4 Generated build artifacts

The `obj/` and `bin/` directories contain generated files with AISpace names (`.dll`, `.pdb`, `.xml`, `.deps.json`, etc.). These are regenerated on build after the rename and **do not require manual editing**. Delete `obj/` and `bin/` after the rename, then run `dotnet build`.

---

## 8. README and Root-level Files

| File | Occurrences | Notes |
|---|---|---|
| `README.md` | 5 | Title "# AISpace", description text, two `dotnet ... src/AISpace.sln` commands, GitHub badge URL, "AISpace is licensed under..." |
| `CONTRIBUTING.md` | 5 | References to AISpace project name |
| `CHANGELOG.md` | 1 | Single reference (likely header or version entry) |
| `SECURITY.md` | 1 | Single reference |
| `.github/PULL_REQUEST_TEMPLATE.md` | 1 | Single reference |
| `.github/PULL_REQUEST_TEMPLATE.md` | 1 | Single reference |

`CODE_OF_CONDUCT.md` — not verified to contain AISpace (not checked, likely 0 references).

---

## Risk Notes

### CRITICAL

1. **`AISpaceException` base class rename** — Changing `AISpaceException` to `FerretException` is a breaking API change for any plugin authors. All 9 workspace exception classes inherit from it. The rename must be applied atomically across `src/AISpace.Core/Errors/AISpaceException.cs` and all subclasses in a single pass.

2. **`InternalsVisibleTo` must match assembly name** — `src/AISpace.Runtime/Properties/AssemblyInfo.cs` references `AISpace.Runtime.Tests` by string. If the test project is renamed before this attribute is updated (or vice versa), the build will fail with `[InternalsVisibleTo]` resolution errors. Update both together.

3. **Solution file and .csproj references** — The `.sln` file contains relative project paths including folder names. Renaming folders without updating the `.sln` project entries will break the solution. The `.sln` must be regenerated or manually updated atomically with the folder renames.

### HIGH

4. **Historical plan docs with 290+ occurrences** — `sprint-2-repository-bootstrap.md` (330), `sprint-5-runtime-host.md` (307), `sprint-3-platform-kernel.md` (291) are large historical planning documents. Bulk-replacing AISpace in these will rewrite historical context. Recommended approach: add a one-line banner at the top of each: `> **Note:** This document was written when the product was named AISpace, which was renamed to Ferret during Sprint 5.` Then bulk-replace product name in prose while preserving technical artifact names in code blocks (e.g., namespace examples).

5. **ADR-0011 references internal rename** — `docs/adr/0011-rename-aispace-sdk-to-aispace-plugin-sdk.md` documents the renaming of `AISpace.SDK` to `AISpace.Plugin.SDK`. This ADR title is historically accurate and should retain both old names in its body with an added note that both are now under the `Ferret.*` namespace.

6. **GitHub badge URL in README.md** — The CI badge points to `github.com/indoulia/Ferret/actions/workflows/ci.yml`. If the repository is also renamed (not in scope here), the badge URL breaks. If only code is renamed, the badge URL stays.

### MEDIUM

7. **CI workflow release name** — `.github/workflows/release.yml` has the literal string `AISpace v${{ steps.version.outputs.VERSION }}` as the GitHub release title. Easy to miss in a bulk replace since it's not in a `src/AISpace.sln` pattern.

8. **`docs/000-Overview/Vision.md`** — 25 occurrences. This is a product-defining document that uses AISpace as both a technical artifact name and a product concept. Replacement is correct but requires careful review to ensure the voice reads naturally as "Ferret."

---

## Recommended Rename Order

Tasks must proceed in this sequence to avoid broken intermediate states:

### Phase 1 — Infrastructure (no source compilation required)

1. Rename all `src/AISpace.*` folders and their `.csproj` files to `Ferret.*`
2. Rename all `tests/AISpace.*` folders and their `.csproj` files to `Ferret.*`
3. Rename `samples/plugins/AISpace.Plugins.Sample/` folder and `.csproj`
4. Regenerate or manually update `src/AISpace.sln` → `src/Ferret.sln` with new project paths
5. Update `Directory.Build.props` (Product, Company, Copyright, Authors)

### Phase 2 — Source code (enables build to go green)

6. Global search-replace `namespace AISpace` → `namespace Ferret` across all `.cs` files
7. Global search-replace `using AISpace` → `using Ferret` across all `.cs` files
8. Rename `AISpaceException` class to `FerretException` in `src/Ferret.Core/Errors/FerretException.cs` and update all XML doc comments within that file
9. Update `InternalsVisibleTo("AISpace.Runtime.Tests")` → `InternalsVisibleTo("Ferret.Runtime.Tests")` in `src/Ferret.Runtime/Properties/AssemblyInfo.cs`
10. Update remaining comment strings referencing "AISpace" in source files (e.g., `// AISpace CLI entry point`, doc comments)

### Phase 3 — CI/CD and scripts (ensures pipeline green)

11. Update all three workflow files: replace `src/AISpace.sln` → `src/Ferret.sln` and `AISpace v` → `Ferret v`
12. Update `scripts/bootstrap.ps1` and `scripts/init-workspace.ps1`

### Phase 4 — Documentation (no build impact)

13. Update root-level files: `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `SECURITY.md`, `.github/PULL_REQUEST_TEMPLATE.md`
14. Update `docs/000-Overview/` files (Vision, Mission, Principles, Glossary)
15. Add historical-context banners to the five large sprint plan docs, then bulk-replace prose
16. Update remaining `docs/` markdown files with standard bulk-replace
17. Update `docs/adr/0011-rename-aispace-sdk-to-aispace-plugin-sdk.md` with post-rename context note

### Phase 5 — Verification

18. Delete `src/*/obj/` and `src/*/bin/` directories (stale build artifacts)
19. Run `dotnet build src/Ferret.sln` — must be green
20. Run `dotnet test src/Ferret.sln` — must be green
21. Commit all changes as `feat!: rebrand AISpace to Ferret`
22. Tag `v0.5.0-ferret`
