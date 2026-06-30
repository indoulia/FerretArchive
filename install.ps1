#Requires -Version 5.1
<#
.SYNOPSIS
    Install Ferret for the current user (no administrator rights required).
.DESCRIPTION
    Copies ferret.exe to a per-user programs directory and adds that directory
    to the current user's PATH. Run from the extracted release package; the
    script installs the ferret.exe located alongside it.
.PARAMETER InstallDir
    Target directory. Defaults to %LOCALAPPDATA%\Programs\Ferret.
.EXAMPLE
    .\install.ps1
    .\install.ps1 -InstallDir 'C:\tools\ferret'
.NOTES
    If PowerShell blocks this script ("running scripts is disabled on this
    system"), run it with an explicit per-process bypass:
        powershell -ExecutionPolicy Bypass -File .\install.ps1
#>
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\Ferret')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Source    = Join-Path $ScriptDir 'ferret.exe'

if (-not (Test-Path $Source)) {
    Write-Error "ferret.exe not found next to this script ($Source). Run install.ps1 from the extracted release package."
    exit 1
}

Write-Host ""
Write-Host "Installing Ferret to $InstallDir"

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

$Dest = Join-Path $InstallDir 'ferret.exe'
Copy-Item -Path $Source -Destination $Dest -Force
# Clear the Mark-of-the-Web the binary inherited from the downloaded zip so it
# does not trip SmartScreen / "blocked because it came from another computer".
Unblock-File -Path $Dest -ErrorAction SilentlyContinue
Write-Host "  Copied ferret.exe"

# --- Add install dir to the user PATH if not already present. ---------------
# Read the RAW registry value (DoNotExpandEnvironmentNames): the .NET
# [Environment]::GetEnvironmentVariable(..,'User') API expands %VAR% tokens,
# and writing the expanded literal back would permanently destroy any
# %USERPROFILE%-style entries the user already has. We read/write the registry
# directly and preserve the REG_EXPAND_SZ value kind.
$envKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
if ($null -eq $envKey) {
    $envKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Environment')
}
try {
    $userPath = [string]$envKey.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
    # Preserve the existing value kind; default to REG_EXPAND_SZ (what Windows
    # uses for PATH) only when no Path value exists yet.
    $pathKind = [Microsoft.Win32.RegistryValueKind]::ExpandString
    try { $pathKind = $envKey.GetValueKind('Path') } catch { }

    $entries = $userPath.Split(';') | Where-Object { $_ -ne '' }
    $onPath  = $entries | Where-Object { $_.TrimEnd('\') -ieq $InstallDir.TrimEnd('\') }

    if (-not $onPath) {
        $newPath = if ($userPath.TrimEnd(';') -eq '') { $InstallDir } else { ($userPath.TrimEnd(';') + ';' + $InstallDir) }
        $envKey.SetValue('Path', $newPath, $pathKind)
        # Update the current session too.
        $env:Path = $env:Path.TrimEnd(';') + ';' + $InstallDir
        Write-Host "  Added $InstallDir to your user PATH"
        $pathChanged = $true
    } else {
        Write-Host "  $InstallDir already on PATH"
        $pathChanged = $false
    }
} finally {
    $envKey.Dispose()
}

# Best-effort: tell the shell/Explorer the environment changed so newly launched
# processes pick up the PATH without a sign-out. Already-open terminals still
# need to be reopened. Non-fatal if the broadcast fails.
if ($pathChanged) {
    try {
        if (-not ('Ferret.Native.Win32' -as [type])) {
            Add-Type -Namespace 'Ferret.Native' -Name 'Win32' -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
public static extern System.IntPtr SendMessageTimeout(System.IntPtr hWnd, uint Msg, System.UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out System.UIntPtr lpdwResult);
'@
        }
        $HWND_BROADCAST = [System.IntPtr]0xffff
        $WM_SETTINGCHANGE = 0x1A
        $SMTO_ABORTIFHUNG = 0x2
        $result = [System.UIntPtr]::Zero
        [void][Ferret.Native.Win32]::SendMessageTimeout($HWND_BROADCAST, $WM_SETTINGCHANGE, [System.UIntPtr]::Zero, 'Environment', $SMTO_ABORTIFHUNG, 5000, [ref]$result)
    } catch {
        # Non-fatal; the user just needs to open a new terminal.
    }
}

Write-Host ""
Write-Host "Ferret installed."
& $Dest --version
Write-Host ""
if ($pathChanged) {
    Write-Host "Open a NEW terminal (or sign out/in) so 'ferret' resolves on PATH."
} else {
    Write-Host "Run 'ferret --version' to verify."
}
