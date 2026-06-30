# Ferret

> An extensible AI platform for building, orchestrating, and operating intelligent agents and workflows.

[![Build](https://github.com/indoulia/Ferret/actions/workflows/ci.yml/badge.svg)](https://github.com/indoulia/Ferret/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com)

---

## Overview

Ferret is a modular, open-source AI platform that provides:

- **Agent Orchestration** — build, compose, and run multi-step AI agent workflows
- **MCP Integration** — first-class support for the Model Context Protocol
- **Plugin Architecture** — extend capabilities through a typed plugin system
- **CLI Tooling** — developer-friendly command-line interface for all platform operations
- **Observability** — structured logging, tracing, and metrics built in from day one

## Quick Start

```powershell
# Clone the repository
git clone https://github.com/indoulia/Ferret.git
cd ferret

# Bootstrap the workspace
./scripts/bootstrap.ps1

# Build the solution
dotnet build src/Ferret.sln

# Run the tests
dotnet test src/Ferret.sln
```

## Repository Layout

```
ferret/
├── docs/          Specifications, ADRs, architecture, and guides
├── src/           Production source code (.NET solution)
├── tests/         Integration and end-to-end tests
├── examples/      Runnable sample projects
├── scripts/       Bootstrap and automation scripts
├── tools/         Developer tooling and utilities
└── templates/     Document templates (ADR, spec, PRD, API, …)
```

See each folder's `README.md` for details.

## Documentation

| Document | Description |
|---|---|
| [Architecture](docs/architecture/overview.md) | System design and component map |
| [ADRs](docs/adr/) | Architecture Decision Records |
| [Specifications](docs/specs/) | Feature and sprint specifications |
| [API Reference](docs/api/) | REST / gRPC API documentation |
| [Contributing](CONTRIBUTING.md) | How to contribute |
| [Changelog](CHANGELOG.md) | Release history |
| [Security](SECURITY.md) | Reporting vulnerabilities |

## Contributing

We welcome contributions! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

Ferret is licensed under the [MIT License](LICENSE).
