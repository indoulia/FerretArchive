#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptsDir = Split-Path -Parent $PSScriptRoot
$gen = Join-Path $scriptsDir "build-release-manifest.ps1"
$RidOrderCheck = @("win-x64", "linux-x64", "osx-arm64", "osx-x64")

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("ferret-manifest-test-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $work -Force | Out-Null
try {
    "win-binary" | Set-Content (Join-Path $work "Ferret-9.9.9-win-x64.zip")
    "linux-binary" | Set-Content (Join-Path $work "Ferret-9.9.9-linux-x64.zip")
    # Stale artifact from a longer-prefixed version must NOT be picked up when
    # building 9.9.9 (the captured RID 'rc1-win-x64' is not a known RID).
    "stale" | Set-Content (Join-Path $work "Ferret-9.9.9-rc1-win-x64.zip")

    $output = & $gen -Version "9.9.9" -ArtifactsDir $work -Published "2026-06-30" | Out-String

    $manifestPath = Join-Path $work "release-manifest.json"
    $sumsPath = Join-Path $work "SHA256SUMS.txt"
    if (-not (Test-Path $manifestPath)) { throw "release-manifest.json not created" }
    if (-not (Test-Path $sumsPath)) { throw "SHA256SUMS.txt not created" }

    $m = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($m.schemaVersion -ne 1) { throw "schemaVersion != 1" }
    if ($m.version -ne "9.9.9") { throw "version mismatch" }
    if ($m.releaseTag -ne "v9.9.9") { throw "releaseTag mismatch" }
    if ($m.minimumInstallerSchema -ne 1) { throw "minimumInstallerSchema != 1" }
    if ($m.metadata.generator -ne "build-release-manifest.ps1") { throw "metadata.generator wrong" }
    if (@($m.assets).Count -ne 2) { throw "expected 2 assets, got $(@($m.assets).Count)" }
    if (@($m.assets) | Where-Object { $_.rid -notin $RidOrderCheck }) { throw "manifest contains an unknown RID (stale artifact leaked in)" }

    $win = @($m.assets) | Where-Object { $_.rid -eq "win-x64" }
    if ($win.binary -ne "ferret.exe") { throw "win binary name wrong: $($win.binary)" }
    if ([int64]$win.size -le 0) { throw "win size not set" }

    $expected = (Get-FileHash (Join-Path $work "Ferret-9.9.9-win-x64.zip") -Algorithm SHA256).Hash.ToLower()
    if ($win.sha256 -ne $expected) { throw "win sha256 mismatch" }
    if (-not ((Get-Content $sumsPath) -match "Ferret-9.9.9-win-x64.zip")) { throw "SHA256SUMS missing win entry" }

    # The generator must self-validate the manifest with the installer's parser.
    if ($output -notmatch "manifest valid: schema 1") { throw "manifest parser validation line not found in output" }

    # Regression: an empty artifacts dir must fail with the "no zips" message,
    # not a StrictMode crash on a missing .Count (single/zero-item scalar). The
    # generator runs with ErrorActionPreference=Stop, so Write-Error is a
    # terminating error and must be caught.
    $empty = Join-Path ([System.IO.Path]::GetTempPath()) ("ferret-manifest-empty-" + [guid]::NewGuid())
    New-Item -ItemType Directory -Path $empty -Force | Out-Null
    try {
        $threw = $false
        try {
            & $gen -Version "9.9.9" -ArtifactsDir $empty -Published "2026-06-30" 2>&1 | Out-Null
        }
        catch {
            $threw = $true
            if ("$_" -notmatch "No Ferret-9.9.9-\*\.zip files found") { throw "wrong error for empty dir: $_" }
        }
        if (-not $threw) { throw "expected failure for empty artifacts dir" }
    }
    finally {
        Remove-Item $empty -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "PASS: build-release-manifest.ps1 tests"
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
