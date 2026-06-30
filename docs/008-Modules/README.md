# 008 — Modules

Component and module design documents for Ferret internal modules.

---

## Index

| Module | Description | Status |
|---|---|---|
| _(to be added)_ | | |

---

## Planned Modules

| Module | Package | Description |
|---|---|---|
| Core | `Ferret.Core` | Domain model, interfaces, value objects |
| Runtime | `Ferret.Runtime` | Agent orchestration engine |
| MCP | `Ferret.Mcp` | MCP client and server implementation |
| Plugins | `Ferret.Plugins` | Plugin host, isolation, and SDK |
| API | `Ferret.Api` | ASP.NET Core HTTP / gRPC surface |
| CLI | `Ferret.Cli` | CLI tool implementation |

---

## Writing a Module Design

Use [docs/templates/architecture.md](../templates/architecture.md) for module design documents.
Each module document should include C3-level component diagrams.
