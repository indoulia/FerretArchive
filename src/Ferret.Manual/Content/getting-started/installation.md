# Installation

Ferret ships as a single self-contained binary. No .NET SDK, no runtime, no dependencies.

## Windows

Run `publish.ps1` from the repo root to build locally:

```powershell
.\publish.ps1 -Rid win-x64
```

Copy `artifacts\win-x64\ferret.exe` to a directory on your PATH (e.g. `C:\tools\`).

## macOS

```bash
# Apple Silicon (M1/M2/M3)
chmod +x ferret-osx-arm64
sudo mv ferret-osx-arm64 /usr/local/bin/ferret

# Intel
chmod +x ferret-osx-x64
sudo mv ferret-osx-x64 /usr/local/bin/ferret
```

## Linux

```bash
chmod +x ferret-linux-x64
sudo mv ferret-linux-x64 /usr/local/bin/ferret
```

## Verify

```bash
ferret --version
# ferret 0.14.0
```

> **Note:** If `ferret` is not found after installation, ensure the directory is on your PATH
> and restart your terminal.

## Related

- [First Workspace](first-workspace) — initialise your first workspace
- [Troubleshooting](../troubleshooting) — installation errors
