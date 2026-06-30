#!/usr/bin/env pwsh
[CmdletBinding()]
param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Test,
    [switch]$Clean,
    [switch]$Format
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot
$sln = Join-Path $repoRoot 'src' 'Ferret.sln'

if ($Clean) {
    Write-Host 'Cleaning...' -ForegroundColor Cyan
    & dotnet clean $sln --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Building Ferret ($Configuration)..." -ForegroundColor Cyan
& dotnet build $sln --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Format) {
    Write-Host 'Checking format...' -ForegroundColor Cyan
    & dotnet format $sln --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($Test) {
    Write-Host 'Running tests...' -ForegroundColor Cyan
    & dotnet test $sln --no-build --configuration $Configuration --verbosity normal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'Done.' -ForegroundColor Green
