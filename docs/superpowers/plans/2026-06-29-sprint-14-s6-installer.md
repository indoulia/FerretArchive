# Sprint 14 S6: Installer and Release Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce self-contained, trimmed, single-file binaries for `win-x64`, `osx-arm64`, `osx-x64`, and `linux-x64` via a single `publish.ps1` script run locally. No CI pipeline required.

**Architecture:** One `Ferret.Cli.csproj` change enables Release-mode trimming. One PowerShell script calls `dotnet publish` four times and collects outputs into `artifacts/`. The script is idempotent and can be run by any developer with .NET 9 SDK installed.

**Tech Stack:** .NET 9 SDK, `dotnet publish`, PowerShell 5+

## Global Constraints

- `PublishTrimmed`, `PublishSingleFile`, `SelfContained` only active in `Release` configuration — Debug builds are unaffected
- Trimming warnings must not break the build — suppress analysis warnings if needed
- Output binaries go to `artifacts/{rid}/ferret{.exe}` relative to repo root
- Commit prefix: `feat(sprint-14):`
- No changes to any project other than `Ferret.Cli.csproj`

---

## File Structure After S6

```
ferret/
├── src/
│   └── Ferret.Cli/
│       └── Ferret.Cli.csproj    ← updated: Release publish settings
└── publish.ps1                  ← new: local build script
```

---

### Task 1: Update `Ferret.Cli.csproj` with publish settings

**Files:**
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`

- [ ] **Step 1: Open and read `src/Ferret.Cli/Ferret.Cli.csproj`**

Identify where to insert the new `PropertyGroup` — after the first existing `PropertyGroup` and before the first `ItemGroup`.

- [ ] **Step 2: Add the Release-only publish settings**

Insert immediately after the existing `PropertyGroup` block:

```xml
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    <SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>
  </PropertyGroup>
```

- [ ] **Step 3: Verify Debug build is unaffected**

```
dotnet build src/Ferret.sln
```

Expected: Build succeeded, 0 errors — publish flags do not activate during build

- [ ] **Step 4: Smoke-publish for one RID**

```
dotnet publish src/Ferret.Cli/Ferret.Cli.csproj -c Release -r win-x64 --self-contained -o ./artifacts/win-x64
```

Expected: Succeeds, `artifacts/win-x64/ferret.exe` exists

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Ferret.Cli.csproj
git commit -m "feat(sprint-14): Ferret.Cli.csproj — Release publish settings (trim, single-file, self-contained)"
```

---

### Task 2: Create `publish.ps1` — local multi-platform build script

**Files:**
- Create: `publish.ps1` (repo root)

- [ ] **Step 1: Create `publish.ps1`**

Create `publish.ps1` at the repo root with the following content:

```powershell
#Requires -Version 5.1
<#
.SYNOPSIS
    Build self-contained Ferret binaries for all target platforms.
.DESCRIPTION
    Publishes win-x64, osx-arm64, osx-x64, and linux-x64 self-contained
    single-file binaries to the artifacts/ directory. Requires .NET 9 SDK.
.PARAMETER Version
    Version string to embed (e.g. "0.14.0"). Defaults to reading from Ferret.Cli.csproj.
.PARAMETER Rid
    Publish only the specified RID (e.g. "win-x64"). Defaults to all four.
.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -Version 0.14.0
    .\publish.ps1 -Rid win-x64
#>
param(
    [string]$Version = "",
    [string]$Rid     = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot   = $PSScriptRoot
$CliProject = Join-Path $RepoRoot "src\Ferret.Cli\Ferret.Cli.csproj"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"

$AllRids = @("win-x64", "osx-arm64", "osx-x64", "linux-x64")
$TargetRids = if ($Rid) { @($Rid) } else { $AllRids }

# Resolve version from csproj if not provided
if (-not $Version) {
    $csprojContent = Get-Content $CliProject -Raw
    if ($csprojContent -match '<Version>([^<]+)</Version>') {
        $Version = $Matches[1]
    } elseif ($csprojContent -match '<VersionPrefix>([^<]+)</VersionPrefix>') {
        $Version = $Matches[1]
    } else {
        $Version = "0.14.0"
        Write-Warning "Could not read version from csproj — using default $Version"
    }
}

Write-Host ""
Write-Host "Ferret $Version — publishing $($TargetRids -join ', ')"
Write-Host "Output: $ArtifactsDir"
Write-Host ""

$failures = @()

foreach ($rid in $TargetRids) {
    $outDir = Join-Path $ArtifactsDir $rid
    Write-Host "[$rid] Publishing..."

    $publishArgs = @(
        "publish", $CliProject,
        "--configuration", "Release",
        "--runtime", $rid,
        "--self-contained",
        "/p:VersionPrefix=$Version",
        "--output", $outDir,
        "--nologo"
    )

    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "[$rid] FAILED (exit $LASTEXITCODE)"
        $failures += $rid
        continue
    }

    # Rename output: dotnet emits "Ferret.Cli" or "Ferret.Cli.exe"; rename to "ferret" / "ferret.exe"
    $srcName = if ($rid -like "win-*") { "Ferret.Cli.exe" } else { "Ferret.Cli" }
    $dstName = if ($rid -like "win-*") { "ferret.exe"     } else { "ferret" }
    $srcPath = Join-Path $outDir $srcName
    $dstPath = Join-Path $outDir $dstName

    if (Test-Path $srcPath) {
        Move-Item $srcPath $dstPath -Force
        Write-Host "[$rid] OK — $dstPath"
    } else {
        Write-Warning "[$rid] Binary not found at $srcPath — check AssemblyName in csproj"
        $failures += $rid
    }
}

Write-Host ""
if ($failures.Count -gt 0) {
    Write-Error "Failed RIDs: $($failures -join ', ')"
    exit 1
} else {
    Write-Host "All targets published successfully."
    Write-Host ""
    Write-Host "Artifacts:"
    Get-ChildItem $ArtifactsDir -Recurse -File | Where-Object { $_.Name -match '^ferret' } |
        ForEach-Object { Write-Host "  $($_.FullName)" }
}
```

- [ ] **Step 2: Add `artifacts/` to `.gitignore`**

Open `.gitignore` (repo root). Add:

```
# Local publish artifacts
artifacts/
```

- [ ] **Step 3: Run the script for win-x64 to verify**

```powershell
.\publish.ps1 -Rid win-x64
```

Expected:
- Exit code 0
- `artifacts/win-x64/ferret.exe` exists
- Running `.\artifacts\win-x64\ferret.exe --version` prints version

- [ ] **Step 4: Verify the binary is truly self-contained**

On a machine or VM without .NET SDK installed, copy `artifacts/win-x64/ferret.exe` and run it. Expected: runs without "No .NET SDK found" error.

If no SDK-free machine is available, verify via file size — a trimmed single-file win-x64 binary should be roughly 15–30 MB. Under 5 MB indicates self-contained is not active; over 80 MB indicates trimming is not active.

- [ ] **Step 5: Run full publish for all four platforms (on a machine with cross-publish support)**

```powershell
.\publish.ps1 -Version 0.14.0
```

Expected: 4 directories under `artifacts/`, each containing a `ferret` or `ferret.exe` binary.

**Note:** All four RIDs can be published from a Windows machine with .NET 9 SDK — cross-compilation is handled by the SDK. Linux and macOS binaries produced on Windows are valid native binaries.

- [ ] **Step 6: Commit**

```
git add publish.ps1
git add .gitignore
git commit -m "feat(sprint-14): publish.ps1 — local multi-platform self-contained binary builder"
```

---

## Completion Checklist

- [ ] `Ferret.Cli.csproj` Release publish flags added; Debug build unaffected
- [ ] `dotnet build src/Ferret.sln` (Debug) still passes in 0 errors
- [ ] `.\publish.ps1 -Rid win-x64` exits 0 and produces `artifacts/win-x64/ferret.exe`
- [ ] `.\publish.ps1` produces all 4 platform binaries without errors
- [ ] `ferret.exe --version` runs cleanly on a machine without .NET SDK
- [ ] `artifacts/` is in `.gitignore`
- [ ] Binary size is 15–80 MB (confirms trim + single-file active)
- [ ] All tests still pass: `dotnet test tests/`
