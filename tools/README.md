# tools

Developer tooling and workspace utilities for Ferret contributors.

---

## Contents (planned)

| Tool | Description |
|---|---|
| `tools/analyzers/` | Custom Roslyn analyzers enforced at build time |
| `tools/codegen/` | Source generators and scaffolding scripts |
| `tools/devcontainer/` | VS Code Dev Container definition |

---

## .NET Local Tools

Global tools are declared in `src/.config/dotnet-tools.json` (to be created) and restored by the bootstrap script.

```powershell
# Restore local .NET tools
dotnet tool restore
```

Common tools installed by bootstrap:

| Tool | Purpose |
|---|---|
| `dotnet-format` | Code formatting |
| `dotnet-outdated` | NuGet update checker |
| `nbgv` | Nerdbank.GitVersioning |
