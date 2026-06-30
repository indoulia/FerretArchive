#Requires -Version 7.0
<#
.SYNOPSIS
    Bootstraps the Ferret development workspace.

.DESCRIPTION
    Validates prerequisites, installs local .NET tools, restores NuGet packages,
    and verifies the solution builds cleanly.
    Safe to re-run — all steps are idempotent.

.PARAMETER SkipBuild
    Skip the final dotnet build verification step.

.PARAMETER Verbose
    Show detailed output from each step.

.EXAMPLE
    ./scripts/bootstrap.ps1
    ./scripts/bootstrap.ps1 -SkipBuild
#>
[CmdletBinding()]
param(
    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---- Helpers ----------------------------------------------------------------

function Write-Step([string] $Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string] $Message) {
    Write-Host "    [OK] $Message" -ForegroundColor Green
}

function Write-Warn([string] $Message) {
    Write-Host "    [WARN] $Message" -ForegroundColor Yellow
}

function Assert-Command([string] $Name, [string] $MinVersion = '') {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "Required command '$Name' not found. Please install it and re-run bootstrap."
    }
    Write-Ok "$Name found at $($cmd.Source)"
}

# ---- Resolve repo root -------------------------------------------------------

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {

# ---- Step 1: Prerequisites ---------------------------------------------------

Write-Step 'Checking prerequisites'

Assert-Command 'git'
Assert-Command 'dotnet'

$dotnetVersion = (& dotnet --version)
$dotnetMajor   = [int]($dotnetVersion.Split('.')[0])
if ($dotnetMajor -lt 9) {
    throw ".NET 9+ required. Found: $dotnetVersion. Download from https://dotnet.microsoft.com/download"
}
Write-Ok ".NET $dotnetVersion"

# ---- Step 2: Git submodules (future use) -------------------------------------

Write-Step 'Updating Git submodules'
& git submodule update --init --recursive
Write-Ok 'Submodules up to date'

# ---- Step 3: Restore local .NET tools ----------------------------------------

$toolManifest = Join-Path $repoRoot 'src' '.config' 'dotnet-tools.json'
if (Test-Path $toolManifest) {
    Write-Step 'Restoring local .NET tools'
    & dotnet tool restore --tool-manifest $toolManifest
    Write-Ok 'Local tools restored'
} else {
    Write-Warn "No tool manifest found at $toolManifest — skipping tool restore"
}

# ---- Step 4: Restore NuGet packages ------------------------------------------

Write-Step 'Restoring NuGet packages'
& dotnet restore (Join-Path $repoRoot 'src' 'Ferret.sln')
Write-Ok 'NuGet packages restored'

# ---- Step 5: Build verification ----------------------------------------------

if (-not $SkipBuild) {
    Write-Step 'Verifying build'
    & dotnet build (Join-Path $repoRoot 'src' 'Ferret.sln') `
        --no-restore `
        --configuration Debug `
        --verbosity minimal
    Write-Ok 'Solution builds cleanly'
}

# ---- Done --------------------------------------------------------------------

Write-Host "`n Ferret workspace is ready.`n" -ForegroundColor Green

} finally {
    Pop-Location
}
