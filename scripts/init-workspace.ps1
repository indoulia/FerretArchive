#Requires -Version 7.0
<#
.SYNOPSIS
    Idempotent workspace initialisation — run after any pull, merge, or branch switch.

.DESCRIPTION
    Lighter than bootstrap.ps1 — restores packages and checks for any new
    tool requirements without re-validating all prerequisites.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {

    Write-Host '==> Syncing workspace...' -ForegroundColor Cyan

    # Update submodules
    & git submodule update --init --recursive

    # Restore NuGet
    & dotnet restore (Join-Path $repoRoot 'src' 'Ferret.sln') --verbosity quiet

    # Restore local tools if manifest exists
    $toolManifest = Join-Path $repoRoot 'src' '.config' 'dotnet-tools.json'
    if (Test-Path $toolManifest) {
        & dotnet tool restore --tool-manifest $toolManifest --verbosity quiet
    }

    Write-Host '    Workspace synced.' -ForegroundColor Green

} finally {
    Pop-Location
}
