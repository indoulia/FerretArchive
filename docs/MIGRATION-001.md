# MIGRATION-001 — AISpace → Ferret Migration Guide

| Field | Value |
|---|---|
| **Document ID** | MIGRATION-001 |
| **Version** | 2.0 |
| **Status** | Accepted |
| **Last Updated** | 2026-06-28 |
| **Related ADR** | ADR-0005 — Product Rebranding |

---

## Summary

During Sprint 5, the product was renamed from **AISpace** to **Ferret**. The technology platform layer retains the name **ContextOS**. All namespace prefixes, project names, solution file names, CLI binary names, and DI extension method names changed. Error code strings did not change.

This guide is for contributors with forks, branches, or local clones created before the rebrand commit.

---

## What Changed

| Before | After |
|---|---|
| Product name | AISpace | Ferret |
| Technology platform | (none) | ContextOS |
| CLI binary | `aispace` | `ferret` |
| Solution file | `src/AISpace.sln` | `src/Ferret.sln` |
| Namespace prefix | `AISpace.*` | `Ferret.*` |
| Exception base class | `AISpaceException` | `FerretException` |
| DI extension method | `AddAISpaceRuntime()` | `AddFerretRuntime()` |
| Project folders | `src/AISpace.*` | `src/Ferret.*` |
| Assembly names | `AISpace.*` | `Ferret.*` |
| NuGet package prefix | `AISpace.*` | `Ferret.*` |

---

## What Did NOT Change

| Item | Why preserved |
|---|---|
| Error code strings (`AISP-001`–`AISP-015`) | Stable identifiers; changing them would break error handling in consuming code |
| Git commit messages (Sprint 0–5) | Immutable history; historical references to AISpace are preserved as-is |
| ADR body text (ADRs 0001–0011) | Historically accurate; carry a post-rebrand notice banner instead |
| Git tags (`v0.5.0-sprint5` and earlier) | Immutable; `v0.5.0-ferret` is the first tag under the Ferret name |

---

## Migration Steps for Existing Forks / Branches

### Step 1: Identify the rebrand commit

```powershell
git log --oneline --all | Select-String "rebrand"
```

The commit message is: `feat!: rebrand AISpace to Ferret`

### Step 2: Rebase onto the rebrand commit

```powershell
git rebase v0.5.0-ferret
```

### Step 3: Update namespace references in your code

Replace `using AISpace.` with `using Ferret.` and `namespace AISpace.` with `namespace Ferret.` in any files you authored.

### Step 4: Update project references

```xml
<!-- Before -->
<ProjectReference Include="..\AISpace.Core\AISpace.Core.csproj" />
<!-- After -->
<ProjectReference Include="..\Ferret.Core\Ferret.Core.csproj" />
```

### Step 5: Update DI registration calls

```csharp
services.AddAISpaceRuntime(...)  →  services.AddFerretRuntime(...)
```

### Step 6: Update exception handling

```csharp
catch (AISpaceException ex)  →  catch (FerretException ex)
```

Note: Subclasses like `WorkspaceNotFoundException` retain their names.

### Step 7: Leave error codes unchanged

`AISP-xxx` error codes are stable — do NOT rename them.

### Step 8: Rebuild

```powershell
Remove-Item -Recurse -Force src/**/bin, src/**/obj -ErrorAction SilentlyContinue
dotnet build src/Ferret.sln
```

---

## Workspace Directory

The workspace directory also changed:

```csharp
// Before (Sprint 4 interim): ".ai"
// After (Sprint 5+): WorkspaceLayout.RootDirectoryName = ".ferret"
```

---

## Naming Rules Going Forward

| Context | Rule |
|---|---|
| User-facing text | "Ferret" |
| Technology references | "ContextOS" |
| Namespace prefix | `Ferret.*` |
| NuGet packages | `Ferret.*` |
| CLI commands | `ferret <command>` |
| New documentation | Use "Ferret" throughout |
| Historical accuracy | Preserve "AISpace" in pre-Sprint 5 artefacts |

---

## Related Documents

- ADR-0005: Product Rebranding (`docs/adr/0005-product-rebranding.md`)
- BRAND-001: Full brand identity (`docs/002-Architecture/BRAND-001.md`)
- Tag `v0.5.0-ferret`: First commit under the Ferret name
