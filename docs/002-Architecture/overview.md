# Ferret — Architecture Overview

| Field | Value |
|---|---|
| **Status** | Draft |
| **Version** | 0.1 |
| **Date** | 2026-06-27 |
| **Author** | Ferret Core Team |
| **Related ADR** | [ADR-0001](../adr/0001-use-architecture-decision-records.md) |

---

## C1 — System Context

```
┌─────────────────────────────────────────────────────────────┐
│                          Ferret                            │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌────────────────────────┐    │
│  │   CLI    │  │   API    │  │     Plugin Host         │    │
│  └──────────┘  └──────────┘  └────────────────────────┘    │
│         │            │                    │                 │
│         └────────────┴────────────────────┘                 │
│                        │                                    │
│               ┌─────────────────┐                           │
│               │  Agent Runtime  │                           │
│               └─────────────────┘                           │
│                        │                                    │
│          ┌─────────────┴──────────────┐                     │
│          │        MCP Client          │                     │
│          └────────────────────────────┘                     │
└─────────────────────────────────────────────────────────────┘
          │                       │
    ┌─────┴──────┐           ┌────┴──────────┐
    │  LLM APIs  │           │  MCP Servers  │
    │(Anthropic, │           │(tools, data,  │
    │ OpenAI, …) │           │  resources)   │
    └────────────┘           └───────────────┘
```

---

## C2 — Containers

| Container | Package | Technology | Responsibility |
|---|---|---|---|
| **CLI** | `Ferret.Cli` | .NET console app | Developer-facing command-line interface |
| **API** | `Ferret.Api` | ASP.NET Core | HTTP / gRPC surface for external integrations |
| **Plugin Host** | `Ferret.Plugins` | .NET library | Loads, isolates, and manages plugins |
| **Agent Runtime** | `Ferret.Runtime` | .NET library | Orchestrates multi-step agent workflows |
| **MCP Client** | `Ferret.Mcp` | .NET library | Implements the Model Context Protocol client |
| **Core** | `Ferret.Core` | .NET library | Domain model and abstractions (no infra deps) |

---

## Dependency Graph

```
Ferret.Cli      ──► Ferret.Runtime
Ferret.Api      ──► Ferret.Runtime
Ferret.Runtime  ──► Ferret.Core
Ferret.Runtime  ──► Ferret.Mcp
Ferret.Runtime  ──► Ferret.Plugins
Ferret.Mcp      ──► Ferret.Core
Ferret.Plugins  ──► Ferret.Core
```

All arrows point inward toward `Ferret.Core`, which has no project references.

---

## Key Design Decisions

| Decision | ADR |
|---|---|
| Use Architecture Decision Records | [ADR-0001](../adr/0001-use-architecture-decision-records.md) |
| Storage backend | _Pending — Sprint 1_ |
| Plugin isolation model | _Pending — Sprint 1_ |
| MCP transport | _Pending — Sprint 1_ |
| Authentication strategy | _Pending — Sprint 1_ |

---

## Status

This document is a **draft placeholder**. Detailed C2/C3/C4 diagrams will be added in Sprint 1 once component boundaries are finalised.
