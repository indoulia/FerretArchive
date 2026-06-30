# Ferret

> Local-first code & document search with an MCP server for AI agents.

This package is a **self-contained** build of Ferret. It bundles the .NET
runtime and all native dependencies, so there is nothing else to install — no
.NET SDK, no separate runtime, no admin rights.

- **Version:** see `CHANGELOG.md`
- **Platform:** Windows x64
- **Contents:** `ferret.exe`, `install.ps1`, `uninstall.ps1`, `LICENSE`,
  `CHANGELOG.md`, `SHA256SUMS.txt`

---

## 1. Verify the download (optional but recommended)

From the extracted folder, in PowerShell:

```powershell
# Compare the published hashes against the files on disk.
Get-Content .\SHA256SUMS.txt
Get-FileHash .\ferret.exe -Algorithm SHA256 | Format-List
```

The `ferret.exe` hash should match the line for `ferret.exe` in
`SHA256SUMS.txt`.

## 2. Install (per-user, no admin)

`install.ps1` copies `ferret.exe` to `%LOCALAPPDATA%\Programs\Ferret` and adds
that folder to your user `PATH`.

```powershell
# If PowerShell blocks the script ("running scripts is disabled on this
# system"), use a per-process bypass — this does NOT change your machine policy:
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Or, to a custom location:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -InstallDir 'C:\tools\ferret'
```

> **First run / SmartScreen:** Windows tags files extracted from a downloaded
> zip with a "Mark of the Web". `install.ps1` clears it from the installed
> `ferret.exe` automatically. If you instead run `ferret.exe` straight out of
> the extracted folder, Windows may show a SmartScreen prompt — choose
> **More info → Run anyway**, or run `Unblock-File .\ferret.exe` first.

**Open a new terminal** after installing so `ferret` resolves on `PATH`, then:

```powershell
ferret --version
```

### Install without the script

If you prefer not to run a script, just copy `ferret.exe` anywhere on your
`PATH` (or call it by full path). Nothing else is required.

## 3. Quick start

```powershell
# Move into the project you want to index.
cd C:\path\to\your\project

# Create a Ferret workspace (.ferret\ folder).
ferret workspace init

# Index the files.
ferret index

# Search.
ferret search "authentication" --no-highlight
ferret search "token" --format json

# Assemble token-budgeted context for an AI prompt.
ferret context "authentication tokens" --max-documents 3
```

## 4. Commands

| Command | What it does |
|---|---|
| `ferret workspace init` | Create a `.ferret\` workspace in the current directory |
| `ferret workspace status` | Show workspace id, root, and creation date |
| `ferret index` | Discover and index files in the workspace |
| `ferret search <query>` | Full-text search (`--format json`, `--no-highlight`) |
| `ferret context "<query>"` | Build token-budgeted context (`--max-documents N`) |
| `ferret serve` | Run the MCP server over stdio (for AI agents / IDEs) |
| `ferret manual [--port N]` | Serve the built-in manual over HTTP |
| `ferret watch` | Watch the workspace and re-index on change |
| `ferret doctor` | Diagnose the install and workspace |
| `ferret --version` | Print the version |

Run `ferret <command> --help` for the full options of any command.

## 5. Use as an MCP server

`ferret serve` speaks the Model Context Protocol over stdio. Point any MCP
client (e.g. an AI coding assistant) at the installed binary. A typical client
config entry looks like:

```json
{
  "mcpServers": {
    "ferret": {
      "command": "ferret",
      "args": ["serve"]
    }
  }
}
```

Run `ferret serve` from (or configure its working directory to) a folder that
contains a `.ferret\` workspace.

## 6. Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

This removes the install directory and its `PATH` entry. Your workspace data
(`.ferret\` folders inside your projects) is left untouched — delete those
manually if you no longer want them.

## 7. Troubleshooting

| Symptom | Fix |
|---|---|
| `running scripts is disabled on this system` | Run with `powershell -ExecutionPolicy Bypass -File .\install.ps1` |
| `'ferret' is not recognized` | Open a **new** terminal after install; or check the install dir is on your user `PATH` |
| SmartScreen blocks `ferret.exe` | `Unblock-File .\ferret.exe`, or install via `install.ps1` which clears it |
| `ferret doctor` warns about AI provider | Expected on a default install with no Ollama/OpenAI configured — informational only |

## License

Ferret is licensed under the MIT License — see `LICENSE`.
