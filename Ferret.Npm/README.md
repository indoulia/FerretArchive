# @indoulia/ferret

Install Ferret via npm. This package downloads the official, checksum-verified
Ferret binary for your platform from GitHub Releases — it does not bundle or
build anything.

## Install

```bash
npm install -g @indoulia/ferret
ferret --version
```

## Upgrade

```bash
npm update -g @indoulia/ferret
```

## Uninstall

```bash
npm uninstall -g @indoulia/ferret
```

Removes only the Ferret binary. Your `.ferret` workspaces, indexes, and
configuration are left untouched.

## Where the binary is installed

- Windows: `%LOCALAPPDATA%\Programs\Ferret`
- macOS: `~/Library/Application Support/Ferret`
- Linux: `~/.local/share/ferret`

## Failure recovery

- **Checksum mismatch / corrupted download:** rerun `npm install -g @indoulia/ferret`.
  The installer is atomic — a failed install never overwrites a working one.
- **"Ferret binary not found":** the postinstall did not complete; rerun the
  install command.
- **Unsupported platform:** supported targets are Windows x64, Linux x64,
  macOS arm64, and macOS x64.
- **macOS Gatekeeper prompt:** macOS builds are currently unsigned; allow the
  binary in System Settings → Privacy & Security.
- **Behind a firewall / no GitHub access:** point the installer at a mirror with
  `FERRET_DIST_RELEASE_ENDPOINT=<base-url>` (and optionally `FERRET_DIST_OWNER` /
  `FERRET_DIST_REPO`).

## Configuration (advanced)

| Env var                        | Default    | Purpose                                     |
| ------------------------------ | ---------- | ------------------------------------------- |
| `FERRET_DIST_OWNER`            | `indoulia` | GitHub owner                                |
| `FERRET_DIST_REPO`             | `Ferret`   | Repository name                             |
| `FERRET_DIST_RELEASE_ENDPOINT` | _(unset)_  | Custom release base URL (enterprise mirror) |
