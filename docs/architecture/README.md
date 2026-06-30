# Architecture

System design documentation for Ferret.

---

## Index

| Document | Description |
|---|---|
| [Overview](overview.md) | High-level system architecture |

---

## Design Principles

1. **Modularity** — every component has a single responsibility and a well-defined interface.
2. **Extensibility** — new capabilities are added via plugins, not by modifying core.
3. **Observability** — structured logging, distributed tracing, and metrics are built in.
4. **Security by default** — no component exposes an insecure default configuration.
5. **Testability** — all logic is unit-testable without running external services.

---

## C4 Levels

Architecture documents use the [C4 model](https://c4model.com/):

| Level | Scope |
|---|---|
| C1 — System Context | Ferret in relation to users and external systems |
| C2 — Container | Major deployable units (API, CLI, plugin host, …) |
| C3 — Component | Internal structure of a container |
| C4 — Code | Class / package diagrams (generated where possible) |
