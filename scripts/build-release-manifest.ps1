#Requires -Version 5.1
<#
.SYNOPSIS
    Generate SHA256SUMS.txt and release-manifest.json for a Ferret release.
.DESCRIPTION
    Scans ArtifactsDir for Ferret-<Version>-<rid>.zip files, computes SHA256 and
    size for each, and writes the top-level checksum manifest and the
    Distribution Platform public contract (release-manifest.json). The generated
    manifest is then validated with the wrapper's own parser (validate-manifest.js)
    so a manifest the installer would reject never reaches a release.
.PARAMETER Version
    Release version (e.g. "0.14.0" or "0.14.0-rc1").
.PARAMETER ArtifactsDir
    Directory containing the per-RID zips. Defaults to <repo>/artifacts.
.PARAMETER Published
    Date string (yyyy-MM-dd) for the manifest. Defaults to today (UTC).
.PARAMETER ReleaseTag
    Git tag. Defaults to "v<Version>".
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$ArtifactsDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts"),
    [string]$Published = "",
    [string]$ReleaseTag = ""
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $ReleaseTag) { $ReleaseTag = "v$Version" }
if (-not $Published) { $Published = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd") }

$SchemaVersion = 1
$RidOrder = @("win-x64", "linux-x64", "osx-arm64", "osx-x64")
# Constrain the matched RID to the known set so a stale artifact from a
# longer-prefixed version (e.g. 0.14.0-rc1 when building 0.14.0) can never be
# mistaken for a "<version>-<rid>.zip" of this release.
$ridPattern = ($RidOrder | ForEach-Object { [regex]::Escape($_) }) -join '|'

# @(...) forces an array so .Count is valid under StrictMode even for a single
# matching zip (Get-ChildItem returns a scalar for one item otherwise).
$zips = @(Get-ChildItem -Path $ArtifactsDir -Filter "Ferret-$Version-*.zip" -File | Sort-Object Name)
if ($zips.Count -eq 0) {
    Write-Error "No Ferret-$Version-*.zip files found in $ArtifactsDir."
    exit 1
}

$assets = @()
$sumsLines = @()
$escaped = [regex]::Escape($Version)
foreach ($zip in $zips) {
    if ($zip.Name -notmatch "^Ferret-$escaped-($ridPattern)\.zip$") { continue }
    $rid = $Matches[1]
    $hash = (Get-FileHash $zip.FullName -Algorithm SHA256).Hash.ToLower()
    $binary = if ($rid -like "win-*") { "ferret.exe" } else { "ferret" }
    $assets += [ordered]@{
        rid    = $rid
        file   = $zip.Name
        size   = [int64]$zip.Length
        sha256 = $hash
        binary = $binary
    }
    $sumsLines += "$hash  $($zip.Name)"
}

if (@($assets).Count -eq 0) {
    Write-Error "No Ferret-$Version-<rid>.zip files matching a known RID ($($RidOrder -join ', ')) found in $ArtifactsDir."
    exit 1
}

# Stable, deterministic asset ordering by known RID order, then by rid name.
$assets = $assets | Sort-Object `
    @{ Expression = { $i = $RidOrder.IndexOf($_.rid); if ($i -lt 0) { 999 } else { $i } } }, `
    @{ Expression = { $_.rid } }

$manifest = [ordered]@{
    schemaVersion          = $SchemaVersion
    version                = $Version
    releaseTag             = $ReleaseTag
    published              = $Published
    minimumInstallerSchema = 1
    metadata               = [ordered]@{ generator = "build-release-manifest.ps1"; generatorVersion = "1" }
    assets                 = @($assets)
}

$sumsPath = Join-Path $ArtifactsDir "SHA256SUMS.txt"
Set-Content -Path $sumsPath -Value $sumsLines -Encoding ascii

$manifestPath = Join-Path $ArtifactsDir "release-manifest.json"
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath -Encoding ascii

Write-Host "Wrote $sumsPath"
Write-Host "Wrote $manifestPath"

# Validate the generated manifest with the wrapper's own parser (same logic the
# installer uses) so a malformed manifest never reaches a release.
$validator = Join-Path $PSScriptRoot "validate-manifest.js"
$node = Get-Command node -ErrorAction SilentlyContinue
if ($node) {
    & node $validator $manifestPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Generated manifest failed parser validation."
        exit 1
    }
}
else {
    Write-Warning "node not found - skipping manifest parser validation."
}
