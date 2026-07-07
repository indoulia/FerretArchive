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

## Supported file types

`ferret index` extracts searchable text and lightweight metadata from:

- **Source code, text & config** — plain text, Markdown, and JSON (built-in text parsers)
- **CSV / TSV** — structure-aware `CsvParser` (header + rows), so column tokens and cell values are searchable
- **PDF** — `Ferret.Parsers.Pdf` (page text + document info), a dependency-isolated package
- **Word `.docx`** and **Excel `.xlsx`** — `Ferret.Parsers.Office` (paragraphs/tables and sheet/header/cell values; Excel reads cached values via a streaming reader)

Parsers are composed into a single pack (`Ferret.Parsers` / `ParserPackModule`) and
kept dependency-isolated — heavyweight PDF/OpenXML dependencies never leak into the
parser platform or the text parsers. Opaque binaries (images, archives, native
libraries) are never indexed.

Extracted text is unlimited by default; set `Ferret:Parsers:MaxExtractedCharacters`
to cap per-document extraction (truncated documents are flagged in metadata).
`ferret doctor` lists the installed parsers and the number of supported extensions.

## Download & Install

Pre-built, **self-contained** binaries are published on the
[Releases](https://github.com/indoulia/Ferret/releases) page. They bundle the
.NET runtime and all native dependencies — no SDK, no separate runtime, and no
administrator rights required.

> **Platform:** Windows x64. macOS and Linux packages are planned for a future
> release.

1. Download `Ferret-<version>-win-x64.zip` from the latest release and extract it.
2. (Optional) Verify the download against the bundled `SHA256SUMS.txt`:
   ```powershell
   Get-FileHash .\ferret.exe -Algorithm SHA256   # compare with SHA256SUMS.txt
   ```
3. Install for the current user (the `-ExecutionPolicy Bypass` is per-process
   and does not change your machine policy):
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
4. Open a **new** terminal so `ferret` resolves on `PATH`, then verify:
   ```powershell
   ferret --version
   ```

`install.ps1` copies `ferret.exe` to `%LOCALAPPDATA%\Programs\Ferret` and adds
it to your user `PATH`. To remove it, run `uninstall.ps1` from the same folder;
your workspace data (`.ferret\` folders) is left untouched. See the README
inside the release package for the full command reference, MCP setup, and
troubleshooting.

## Build from Source

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
| [Architecture](docs/002-Architecture/) | System design and component map |
| [ADRs](docs/adr/) | Architecture Decision Records |
| [Contributing](CONTRIBUTING.md) | How to contribute |
| [Changelog](CHANGELOG.md) | Release history |
| [Security](SECURITY.md) | Reporting vulnerabilities |

## Contributing

We welcome contributions! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

Ferret is licensed under the [MIT License](LICENSE).
