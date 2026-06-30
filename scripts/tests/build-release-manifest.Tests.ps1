#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptsDir = Split-Path -Parent $PSScriptRoot
$gen = Join-Path $scriptsDir "build-release-manifest.ps1"

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("ferret-manifest-test-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $work -Force | Out-Null
try {
    "win-binary" | Set-Content (Join-Path $work "Ferret-9.9.9-win-x64.zip")
    "linux-binary" | Set-Content (Join-Path $work "Ferret-9.9.9-linux-x64.zip")

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

    $win = @($m.assets) | Where-Object { $_.rid -eq "win-x64" }
    if ($win.binary -ne "ferret.exe") { throw "win binary name wrong: $($win.binary)" }
    if ([int64]$win.size -le 0) { throw "win size not set" }

    $expected = (Get-FileHash (Join-Path $work "Ferret-9.9.9-win-x64.zip") -Algorithm SHA256).Hash.ToLower()
    if ($win.sha256 -ne $expected) { throw "win sha256 mismatch" }
    if (-not ((Get-Content $sumsPath) -match "Ferret-9.9.9-win-x64.zip")) { throw "SHA256SUMS missing win entry" }

    # The generator must self-validate the manifest with the installer's parser.
    if ($output -notmatch "manifest valid: schema 1") { throw "manifest parser validation line not found in output" }

    Write-Host "PASS: build-release-manifest.ps1 tests"
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
