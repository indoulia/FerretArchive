#Requires -Version 5.1
<#
.SYNOPSIS
    Uninstall Ferret for the current user.
.DESCRIPTION
    Removes the Ferret install directory and its entry from the current user's
    PATH. Workspace data (.ferret directories inside your projects) is left
    untouched.
.PARAMETER InstallDir
    Directory Ferret was installed to. Defaults to %LOCALAPPDATA%\Programs\Ferret.
.EXAMPLE
    .\uninstall.ps1
.NOTES
    If PowerShell blocks this script ("running scripts is disabled on this
    system"), run it with an explicit per-process bypass:
        powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
#>
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\Ferret')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "Uninstalling Ferret from $InstallDir"

# Remove the install directory.
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force
    Write-Host "  Removed $InstallDir"
} else {
    Write-Host "  $InstallDir not found (already removed)"
}

# --- Remove the install dir from the user PATH. -----------------------------
# Read/write the RAW registry value to preserve %VAR% tokens and the
# REG_EXPAND_SZ value kind (see install.ps1 for the rationale).
$envKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
if ($null -ne $envKey) {
    try {
        $userPath = [string]$envKey.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        $pathKind = [Microsoft.Win32.RegistryValueKind]::ExpandString
        try { $pathKind = $envKey.GetValueKind('Path') } catch { }
        if (-not [string]::IsNullOrEmpty($userPath)) {
            $kept = $userPath.Split(';') |
                Where-Object { $_ -ne '' -and ($_.TrimEnd('\') -ine $InstallDir.TrimEnd('\')) }
            $newPath = [string]::Join(';', $kept)
            if ($newPath -ne $userPath) {
                $envKey.SetValue('Path', $newPath, $pathKind)
                Write-Host "  Removed $InstallDir from your user PATH"
            } else {
                Write-Host "  $InstallDir was not on PATH"
            }
        }
    } finally {
        $envKey.Dispose()
    }
}

Write-Host ""
Write-Host "Ferret uninstalled. Workspace data (.ferret folders) was left in place."
Write-Host "Open a NEW terminal so PATH changes take effect."
