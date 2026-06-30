# Ferret — Architecture Overview

| Field | Value |
|---|---|
| **Status** | Draft |
| **Version** | 0.1 |
| **Date** | 2026-06-27 |
| **Author** | Ferret Core Team |

---

## C1 — System Context

```
┌─────────────────────────────────────────────────────────┐
│                        Ferret                          │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │
│  │   CLI    │  │   API    │  │   Plugin Host        │  │
│  └──────────┘  └──────────┘  └──────────────────────┘  │
│         │            │                  │               │
│         └────────────┴──────────────────┘               │
│                       │                                 │
│              ┌─────────────────┐                        │
│              │  Agent Runtime  │                        │
│              └─────────────────┘                        │
└─────────────────────────────────────────────────────────┘
         │                   │
   ┌─────┴─────┐       ┌─────┴──────┐
   │ LLM APIs  │       │  MCP Hosts  │
   │(Anthropic,│       │(tools,data, │
   │ OpenAI, …)│       │  resources) │
   └───────────┘       └────────────┘
```

---

## C2 — Containers

| Container | Technology | Responsibility |
|---|---|---|
| **CLI** | .NET console app | Developer-facing command-line interface |
| **API** | ASP.NET Core | HTTP / gRPC surface for external integrations |
| **Plugin Host** | .NET library | Loads, isolates, and manages plugins |
| **Agent Runtime** | .NET library | Orchestrates multi-step agent workflows |
| **MCP Client** | .NET library | Implements the Model Context Protocol client |

---

## Key Design Decisions

- See [ADR-0001](../adr/0001-use-architecture-decision-records.md) for the ADR process itself.
- Further ADRs will capture decisions on: storage backend, plugin isolation model, MCP transport, authentication strategy.

---

## Status

This document is a **placeholder**. Detailed C2/C3/C4 diagrams will be added in Sprint 1 once core component boundaries are agreed.
