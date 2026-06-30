# scripts

Automation and maintenance scripts for Ferret contributors and CI.

---

## Scripts

| Script | Purpose |
|---|---|
| `bootstrap.ps1` | One-shot workspace setup (prerequisites, restore, validate) |
| `init-workspace.ps1` | Idempotent workspace initialisation (safe to re-run) |

---

## Running Scripts

All scripts target **PowerShell 7+**.

```powershell
# Bootstrap a fresh checkout
./scripts/bootstrap.ps1

# Re-initialise after a pull or branch switch
./scripts/init-workspace.ps1
```

Scripts are signed for execution policy compatibility where required.
