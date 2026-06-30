# Platform Overview

Ferret is a five-layer platform. Each layer depends only on the layers below it. No layer reaches upward.

## Layer Stack

```
┌─────────────────────────────────────────────────────┐
│                    Ferret.Cli                        │
│  CLI entry point · command dispatch · branding       │
└──────────────────────┬──────────────────────────────┘
                       │ depends on
┌──────────────────────▼──────────────────────────────┐
│              Application Layer                       │
│  Ferret.Mcp · Ferret.Manual · Ferret.Ai              │
│  Hosts, adapters, and application services           │
└──────────────────────┬──────────────────────────────┘
                       │ depends on
┌──────────────────────▼──────────────────────────────┐
│              Feature Packages                        │
│  Ferret.Workspace · Ferret.Indexing · Ferret.Search  │
│  Ferret.Context · Ferret.Models · Ferret.Prompts     │
│  Domain implementations of platform capabilities    │
└──────────────────────┬──────────────────────────────┘
                       │ depends on
┌──────────────────────▼──────────────────────────────┐
│              Platform Foundation                     │
│  Ferret.Runtime · Ferret.Hosting · Ferret.Events     │
│  Ferret.Health · Ferret.Diagnostics                  │
│  Runtime host · module lifecycle · DI orchestration  │
└──────────────────────┬──────────────────────────────┘
                       │ depends on
┌──────────────────────▼──────────────────────────────┐
│                   Ferret.Core                        │
│  Base contracts · result types · exceptions          │
│  Workspace contracts · connector contracts           │
│  Search contracts · AI contracts · MCP contracts     │
└─────────────────────────────────────────────────────┘
```

## Package Responsibilities

| Package | Layer | Responsibility |
|---|---|---|
| `Ferret.Core` | Core | Base contracts, result types, all interface definitions |
| `Ferret.Runtime` | Foundation | Runtime host, module lifecycle, DI orchestration |
| `Ferret.Hosting` | Foundation | `IHostedService` integration, startup/shutdown |
| `Ferret.Events` | Foundation | In-process event bus |
| `Ferret.Health` | Foundation | `IDiagnosticCheck`, health reporting |
| `Ferret.Workspace` | Feature | Workspace init, config, state management |
| `Ferret.Indexing` | Feature | Index pipeline, parser dispatcher, FTS5 writer |
| `Ferret.Search` | Feature | BM25 search provider, query AST, result types |
| `Ferret.Models` | Feature | Model registry, model router |
| `Ferret.Prompts` | Feature | Prompt registry, template renderer |
| `Ferret.Mcp` | Application | MCP server, tool registry, stdio transport |
| `Ferret.Manual` | Application | Self-hosted documentation server |
| `Ferret.Ai` | Application | AI completion orchestration |
| `Ferret.Providers.Ollama` | Provider | Ollama HTTP API adapter |
| `Ferret.Providers.OpenAi` | Provider | OpenAI-compatible API adapter |
| `Ferret.Cli` | Entry Point | Command dispatch, Spectre.Console UI |

## Foundation Freeze

The Foundation and Core layers are frozen as of Sprint 6 (ADR-0012). No breaking changes to these packages without a superseding ADR.

## Related

- [Dependency Graph](dependency-graph) — package reference diagram
- [Design Decisions](../design/why-platform-first) — why the platform was built first
- [Extension Points](extension-points) — where to add new capabilities
