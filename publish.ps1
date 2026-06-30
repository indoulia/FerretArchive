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

$RepoRoot     = $PSScriptRoot
$CliProject   = Join-Path $RepoRoot "src\Ferret.Cli\Ferret.Cli.csproj"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"

$AllRids    = @("win-x64", "osx-arm64", "osx-x64", "linux-x64")

# Validate -Rid parameter if provided
if ($Rid -and $AllRids -notcontains $Rid) {
    Write-Error "Invalid RID: '$Rid'. Valid RIDs are: $($AllRids -join ', ')"
    exit 1
}

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
        Write-Warning "Could not read version from csproj - using default $Version"
    }
}

Write-Host ""
Write-Host "Ferret $Version - publishing $($TargetRids -join ', ')"
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

    # AssemblyName is "ferret" in Ferret.Cli.csproj - output is already ferret / ferret.exe
    $binaryName = if ($rid -like "win-*") { "ferret.exe" } else { "ferret" }
    $binaryPath = Join-Path $outDir $binaryName

    if (Test-Path $binaryPath) {
        $sizeKB = [math]::Round((Get-Item $binaryPath).Length / 1KB)
        Write-Host "[$rid] OK - $binaryPath ($sizeKB KB)"
    } else {
        Write-Warning "[$rid] Binary not found at $binaryPath - check AssemblyName in Ferret.Cli.csproj"
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
    Get-ChildItem $ArtifactsDir -Recurse -File |
        Where-Object { $_.Name -match '^ferret' } |
        ForEach-Object { Write-Host "  $($_.FullName)" }
}
