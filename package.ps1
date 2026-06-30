#Requires -Version 5.1
<#
.SYNOPSIS
    Assemble a distributable Ferret release package (zip + SHA256SUMS).
.DESCRIPTION
    Stages the published self-contained binary together with the end-user
    README (packaging\README.md), LICENSE, CHANGELOG, and — for Windows RIDs —
    install.ps1 / uninstall.ps1, generates a SHA256SUMS.txt manifest, and zips
    the staging folder into artifacts\Ferret-<version>-<rid>.zip.

    By default it runs publish.ps1 for the RID first. Pass -SkipPublish to
    package whatever is already in artifacts\<rid>\.
.PARAMETER Version
    Version string used in the package/zip name (e.g. "0.14.0-rc1").
    Defaults to the <Version> in Ferret.Cli.csproj.
.PARAMETER Rid
    Runtime identifier to package. Defaults to win-x64.
.PARAMETER SkipPublish
    Do not run publish.ps1; package the existing artifacts\<rid>\ binary.
.EXAMPLE
    .\package.ps1 -Version 0.14.0-rc1
    .\package.ps1 -Rid win-x64 -SkipPublish
#>
param(
    [string]$Version = "",
    [string]$Rid     = "win-x64",
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot     = $PSScriptRoot
$CliProject   = Join-Path $RepoRoot "src\Ferret.Cli\Ferret.Cli.csproj"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$PackagingDir = Join-Path $RepoRoot "packaging"

$ValidRids = @("win-x64", "osx-arm64", "osx-x64", "linux-x64")
if ($ValidRids -notcontains $Rid) {
    Write-Error "Invalid RID: '$Rid'. Valid RIDs are: $($ValidRids -join ', ')"
    exit 1
}

# Resolve version from csproj if not provided.
if (-not $Version) {
    $csproj = Get-Content $CliProject -Raw
    if ($csproj -match '<Version>([^<]+)</Version>') {
        $Version = $Matches[1]
    } elseif ($csproj -match '<VersionPrefix>([^<]+)</VersionPrefix>') {
        $Version = $Matches[1]
    } else {
        Write-Error "Could not read version from $CliProject - pass -Version explicitly."
        exit 1
    }
}

# Use a custom name, not $isWindows: on PowerShell Core $IsWindows is a
# read-only automatic variable (names are case-insensitive) and assigning to it
# throws on the Linux release runner.
$ridIsWindows = $Rid -like "win-*"
$binaryName = if ($ridIsWindows) { "ferret.exe" } else { "ferret" }

# Publish (unless skipping) using the version prefix so the embedded version matches.
if (-not $SkipPublish) {
    $publishVersion = ($Version -split '-')[0]   # strip pre-release suffix for the assembly version
    Write-Host "[$Rid] Publishing $publishVersion ..."
    & (Join-Path $RepoRoot "publish.ps1") -Version $publishVersion -Rid $Rid
    if ($LASTEXITCODE -ne 0) {
        Write-Error "publish.ps1 failed for $Rid (exit $LASTEXITCODE)."
        exit 1
    }
}

$publishedBinary = Join-Path (Join-Path $ArtifactsDir $Rid) $binaryName
if (-not (Test-Path $publishedBinary)) {
    Write-Error "Published binary not found: $publishedBinary. Run without -SkipPublish, or run publish.ps1 first."
    exit 1
}

# --- Stage ------------------------------------------------------------------
$PackageName = "Ferret-$Version-$Rid"
$StageDir    = Join-Path $ArtifactsDir $PackageName
if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
New-Item -ItemType Directory -Path $StageDir -Force | Out-Null

Copy-Item $publishedBinary              (Join-Path $StageDir $binaryName) -Force
Copy-Item (Join-Path $RepoRoot "LICENSE")        (Join-Path $StageDir "LICENSE")      -Force
Copy-Item (Join-Path $RepoRoot "CHANGELOG.md")   (Join-Path $StageDir "CHANGELOG.md") -Force
Copy-Item (Join-Path $PackagingDir "README.md")  (Join-Path $StageDir "README.md")    -Force

if ($ridIsWindows) {
    Copy-Item (Join-Path $RepoRoot "install.ps1")   (Join-Path $StageDir "install.ps1")   -Force
    Copy-Item (Join-Path $RepoRoot "uninstall.ps1") (Join-Path $StageDir "uninstall.ps1") -Force
}

# --- SHA256SUMS.txt (lowercase hex, "hash  filename" — sha256sum compatible) -
$sumsFile = Join-Path $StageDir "SHA256SUMS.txt"
$lines = Get-ChildItem $StageDir -File |
    Where-Object { $_.Name -ne "SHA256SUMS.txt" } |
    Sort-Object Name |
    ForEach-Object {
        $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
        "$hash  $($_.Name)"
    }
Set-Content -Path $sumsFile -Value $lines -Encoding ascii -NoNewline:$false

# --- Zip (archive root folder = $PackageName) -------------------------------
$ZipPath = Join-Path $ArtifactsDir "$PackageName.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path $StageDir -DestinationPath $ZipPath -CompressionLevel Optimal

# --- Report -----------------------------------------------------------------
$zipHash = (Get-FileHash $ZipPath -Algorithm SHA256).Hash.ToLower()
Write-Host ""
Write-Host "Packaged $PackageName"
Write-Host "  Staging : $StageDir"
Write-Host "  Zip     : $ZipPath ($([math]::Round((Get-Item $ZipPath).Length / 1MB, 1)) MB)"
Write-Host "  SHA256  : $zipHash"
Write-Host ""
Write-Host "Contents:"
Get-Content $sumsFile | ForEach-Object { Write-Host "  $_" }
