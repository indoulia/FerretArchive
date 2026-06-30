# 006 — CLI

Command-line interface reference for the `Ferret` tool.

---

## Index

| Document | Description | Status |
|---|---|---|
| _(to be added in Sprint 1)_ | | |

---

## Overview

The Ferret CLI (`Ferret`) provides developer-facing commands for:

- Managing agents and workflows
- Running and inspecting MCP servers
- Plugin management
- Configuration and authentication

---

## Usage

```
Ferret [command] [options]

Options:
  --config, -c    Config file path  [default: ~/.Ferret/config.json]
  --output, -o    Output format: json|table|plain  [default: table]
  --verbose, -v   Verbose logging
  --help, -h      Show help

Commands:
  agent           Manage agents
  workflow        Manage workflows
  plugin          Manage plugins
  mcp             MCP server operations
  config          Manage configuration
```

---

## Template

Use [docs/templates/cli.md](../templates/cli.md) for CLI command reference documents.
