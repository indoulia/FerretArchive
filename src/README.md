# src

Production source code for Ferret — a single .NET solution containing all library and application projects.

---

## Solution Layout

```
src/
├── Ferret.sln                      Solution file
├── Ferret.Core/                    Core domain model and abstractions (no infrastructure deps)
├── Ferret.Runtime/                 Agent runtime — orchestration engine
├── Ferret.Mcp/                     MCP client implementation
├── Ferret.Plugins/                 Plugin host and SDK
├── Ferret.Api/                     ASP.NET Core API host
└── Ferret.Cli/                     .NET CLI tool
```

> These project folders do not exist yet — they will be created sprint by sprint.

---

## Build

```powershell
# From repo root
dotnet build src/Ferret.sln

# Release build
dotnet build src/Ferret.sln --configuration Release

# Run tests
dotnet test src/Ferret.sln
```

---

## Project Dependencies (Planned)

```
Ferret.Cli ──────────► Ferret.Runtime
Ferret.Api ──────────► Ferret.Runtime
Ferret.Runtime ──────► Ferret.Core
Ferret.Runtime ──────► Ferret.Mcp
Ferret.Runtime ──────► Ferret.Plugins
Ferret.Mcp ──────────► Ferret.Core
Ferret.Plugins ──────► Ferret.Core
```

Dependencies flow inward toward `Ferret.Core`, which has no project references.
