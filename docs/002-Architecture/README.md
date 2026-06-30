# 002 — Architecture

System design documents for Ferret.

---

## Index

| Document | ID | Description | Status |
|---|---|---|---|
| [ARCH-TEMPLATE-001 — Architecture Document Standard](ARCH-TEMPLATE-001.md) | ARCH-TEMPLATE-001 | Required sections, metadata, diagram conventions, review checklist, quality gates | Accepted |
| [ARCH-001 — Overall System Architecture](ARCH-001.md) | ARCH-001 | Platform architecture: layers, modules, dependency rules, domain view, fitness functions, capability matrix | Draft |
| ARCH-002 — Ferret.Core Architecture | ARCH-002 | Core module: interfaces, value objects, domain events, extension points | Planned |
| [ARCH-003 — Workspace Architecture](ARCH-003.md) | ARCH-003 | Workspace Engine: components, data flows, configuration reference, error handling | Draft |
| ARCH-004 — Knowledge Architecture | ARCH-004 | Knowledge Engine: graph model, query model, context assembly internals | Planned |
| ARCH-005 — Index Architecture | ARCH-005 | Index Engine: pipeline, change detection, atomicity, parser dispatch | Planned |
| ARCH-006 — Memory Architecture | ARCH-006 | Memory Engine: session state, repository memory, working sets | Planned |
| ARCH-007 — Plugin Architecture | ARCH-007 | Plugin Host: lifecycle, isolation, permissions, SDK, registry | Planned |
| ARCH-008 — Review & Specification Architecture | ARCH-008 | Review Engine + Specification Engine: lifecycle, finding model, approval gates | Planned |
| ARCH-009 — CLI Architecture | ARCH-009 | CLI: command hierarchy, output formats, exit codes, shell completion | Planned |
| ARCH-010 — MCP Architecture | ARCH-010 | MCP Server + Client: tools, resources, transports, protocol versioning | Planned |
| [ARCH-011 — Configuration Architecture](ARCH-011.md) | ARCH-011 | Configuration: sources, precedence, schema, secret resolution, validation | Draft |
| ARCH-012 — Security Architecture | ARCH-012 | Security model: trust boundaries, plugin sandbox, audit, sensitive data | Planned |
| [ARCH-013 — Event Architecture](ARCH-013.md) | ARCH-013 | Domain events: full catalogue, schemas, delivery model, publisher/consumer map | Draft |
| [ARCH-014 — Platform Error Model](ARCH-014.md) | ARCH-014 | Exception hierarchy, error codes, propagation rules | Draft |
| ARCH-015 — Telemetry Architecture | ARCH-015 | Structured logging, distributed tracing, metrics, exporters | Planned |
| [Overview](overview.md) | — | Placeholder — superseded by ARCH-001 | Superseded |

---

## Design Principles

1. **Modularity** — single-responsibility components with well-defined interfaces
2. **Extensibility** — new capabilities added via plugins, not by modifying core
3. **Observability** — structured logging, tracing, and metrics built in from day one
4. **Security by default** — no component exposes an insecure default
5. **Testability** — all logic unit-testable without running external services

---

## C4 Model Levels

| Level | Scope | Status |
|---|---|---|
| C1 — System Context | Ferret vs external actors | Draft (in overview.md) |
| C2 — Container | Deployable units | Draft (in overview.md) |
| C3 — Component | Internal structure per container | Planned — Sprint 1+ |
| C4 — Code | Class diagrams | Generated — Sprint 1+ |

---

## Template

Use [docs/templates/architecture.md](../templates/architecture.md) for new architecture documents.
