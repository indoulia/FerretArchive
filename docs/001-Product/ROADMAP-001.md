# ROADMAP-001 — Platform Roadmap

| Field | Value |
|---|---|
| **Document ID** | ROADMAP-001 |
| **Version** | 2.0 |
| **Status** | Living Document |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

---

## Overview

This document is the authoritative roadmap for the Ferret platform. It records completed sprints, the current sprint, and the planned direction for upcoming work. Milestone and sprint scopes evolve; this document reflects the latest approved plan.

For the long-horizon product vision (V2–V4), see `ROADMAP-002-Future-Vision.md`.

---

## V1 — Ferret Platform

V1 goal: A developer can initialize a Ferret workspace, index their codebase, ask questions about it via CLI and MCP, and get context-aware answers. Every sprint answers: "what can a user do today they couldn't yesterday?"

---

## Completed Sprints

### Sprint 0 — Project Foundation

**Delivered:**
- Git repository, .NET 9 solution skeleton (`src/Ferret.sln`)
- 17 compilable projects (8 source, 9 test)
- `Directory.Build.props`: StyleCop, `TreatWarningsAsErrors=true`, `AnalysisMode=All`, nullability, LangVersion
- CI pipeline bootstrap
- License, contributing guide, code of conduct

**Tag:** (pre-tagging)

---

### Sprint 1 — Repository Scaffold

**Delivered:**
- Full project reference graph established
- ARCH-001 dependency rules as MSBuild build targets (enforced)
- Sample plugin scaffold

---

### Sprint 2–3 — Core Kernel

**Delivered:**
- `Ferret.Core` (zero external dependencies):
  - 10 typed value object IDs (`WorkspaceId`, `WorkspacePath`, etc.)
  - 7 result types (`Result<T>`, `CommandResult`, etc.)
  - 9 base interfaces
  - Domain, Integration, and System event taxonomy
  - `WorkspaceException` hierarchy
  - `Ferret.Events` in-process event bus

**Tests:** 60+ passing

---

### Sprint 4 — Architecture Baseline + Public Contracts

**Delivered:**
- Architecture document stack: ARCH-001, ARCH-011, ARCH-013, ARCH-014
- Runtime Foundation Contracts: `IRuntimeHost`, `IRuntimeBuilder`, `IModule`, `IModuleDescriptor`, `IModuleRegistry`, `IModuleContext`, `IExecutionContext`, `ILifecycleParticipant`, `IRuntimeService`
- Workspace Public Contracts: `IWorkspaceEngine`, `IWorkspaceLocator`, `IWorkspaceStateStore`, `WorkspaceContext`, `WorkspaceMetadata`, `WorkspaceCapabilities`, `WorkspaceStatistics`, `WorkspaceHealthReport`, `WorkspaceInitResult`, `WorkspaceUpgradeResult`, `Changeset`, `WorkspaceOptions`
- Exception namespace migration: workspace errors → `Ferret.Core.Workspace.Errors`

**Tests:** 119 passing | **Tag:** `v0.4.0-sprint4`

---

### Sprint 5 — Runtime Host

**Delivered:**
- `Ferret.Runtime`: `RuntimeHost`, `RuntimeBuilder`, `ModuleRegistry`, `ILifecycleParticipant` ordered teardown
- `Microsoft.Extensions.Hosting` wrapped internally (not exposed through Core contracts)
- `Ferret.Hosting`: `IHostedService` integration, startup/shutdown
- **Product rebrand: AISpace → Ferret** (264 files, atomic commit)
- ContextOS named as the technology platform

**Tags:** `v0.5.0-sprint5` (last AISpace tag), `v0.5.0-ferret` (first Ferret tag)

---

### Sprint 6 — Platform Entry Point & CLI Host

**Delivered:**
- `Ferret.Cli`: `System.CommandLine` integration, `ICliModule` pattern, `RootCommandFactory`
- `IFerretContext`, `IFerretServices`, `IOutputFormatter`, `ICommandHandler`
- `ferret --version` (AssemblyInformationalVersion from build)
- `ferret doctor` (`IDiagnosticCheck`, `DiagnosticRunner`, module-contributed)
- `ferret status` (not-running stub; IPC deferred to Sprint 7)
- `CoreCliModule` (wire-up of all above)
- M1 Platform Foundation Freeze (ADR-0012): Core, Runtime, Hosting, Cli, Events, Health frozen

**Tests:** 245 passing | **Tag:** `v0.6.0-sprint6`

---

## Current Sprint

### Sprint 10 — Information Retrieval

**Goal:** A user can run `ferret search "query"` and get relevant results from the indexed FTS5 database.

**Scope:**
- `ISearchEngine` + `SqliteKeywordSearchEngine`: BM25 MATCH queries, phrase search, ranking
- `ferret search <query>` command with result highlighting
- `IProgressReporter` live search progress

**Plan:** TBD
**Expected tag:** `v0.10.0-sprint10`

---

## Completed Sprints (continued)

### Sprint 7 — Workspace Engine (ContextOS Foundation)

**Goal:** `ferret workspace init` and `ferret workspace status` ship. A user can create and inspect a `.ferret/` workspace that is the long-term foundation for ContextOS.

**Scope:**
- `Ferret.Workspace` library: `WorkspaceEngine`, `WorkspaceLocator`, `WorkspaceStateStore`
- `.ferret/` directory tree: connectors, indexes, memory, knowledge, models, snapshots, telemetry, temp
- ContextOS JSON schemas: `workspace.json` (with `contextOsVersion`, `workspaceType`, `features`, `enabledConnectors`, `enabledModels`), `state.json` (with `knowledgeVersion`, `graphVersion`, `lastIndex`, `connectors`, `statistics`)
- `config/` seeded: `runtime.json`, `plugins.json`, `models.json`, `connectors.json`
- Connector contracts in `Ferret.Core.Connectors`: `IConnector`, `ConnectorType`, `ConnectorMetadata`, `ConnectorCapabilities`, `ConnectorHealth`
- `WorkspaceCliModule`: `ferret workspace init`, `ferret workspace status`
- `RootCommandFactory` grouped subcommand activation

**Plan:** `docs/superpowers/plans/2026-06-28-sprint-7-workspace-engine.md`
**Expected tag:** `v0.7.0-sprint7`

---

### Sprint 8 — Filesystem Connector [✅ Complete]

**Goal:** The first real `IConnector` implementation proves the connector architecture.

**Scope:**
- `FilesystemConnector : IConnector` in a new `Ferret.Connectors.Filesystem` project
- File discovery, change detection (mtime + hash)
- `ferret connector list` (shows registered connectors and their health)
- Connector state persisted to `state.json`

---

### Sprint 9 — Indexing Pipeline [✅ Complete]

**Delivered:** `ferret index` command — discovers assets via connectors, parses with the parser platform, indexes into SQLite FTS5 at `.ferret/indexes/keyword/keyword-index.db`. 651 tests passing. Tag: `v0.9.0-sprint9`.

---

## Planned Sprints

### Sprint 10 — Information Retrieval

**Goal:** A user can run `ferret search "query"` and get semantically relevant results.

**Scope:**
- Embedding model integration (local-first: `nomic-embed-text` or similar)
- Vector index (`indexes/semantic/`)
- `ferret search` command

---

### Sprint 11 — MCP Server

**Goal:** A user can point any MCP-compatible AI host at Ferret and get context-aware answers.

**Scope:**
- `Ferret.Mcp` project: MCP protocol implementation
- `ferret serve` command (starts MCP server)
- Tools exposed: `search_context`, `get_workspace_status`, `get_decision_history`
- Claude Desktop / VS Code integration guide

---

### Sprint 12 — Context Intelligence

**Goal:** Ferret assembles context intelligently — not just the most recent files, but the most relevant content for the current task.

**Scope:**
- Context assembly engine: relevance ranking, recency weighting, token budget management
- Context compression (summarize older context when approaching token limit)
- `ferret context` command (preview assembled context for a query)
- Context history (what was assembled, when, for what query)

---

### Sprint 13 — AI Gateway

**Goal:** Ferret routes AI requests to the best available model — local or remote — with automatic fallback and cost tracking.

**Scope:**
- `Ferret.AI` project: model registry, routing, cost tracking
- Local model support: Ollama / LM Studio integration
- Remote model support: Claude API, OpenAI API (configurable in `models.json`)
- `ferret ai models` (list available models and their status)
- Token usage tracking → analytics events

---

### Sprint 14 — V1 Release Candidate

**Goal:** Ferret V1 is feature-complete and ready for public release.

**Scope:**
- End-to-end smoke test suite
- Documentation complete (user guide, quickstart, connector setup guides)
- Performance benchmarks (index time, search latency, context assembly time)
- `ferret --version` shows release version from git tag
- Release pipeline: NuGet package, GitHub release, changelog

---

## Milestone Map

| Milestone | Sprints | Status |
|---|---|---|
| M1 — Platform Foundation | 0–6 | Done / Frozen (ADR-0012) |
| M2 — Workspace Engine | 7–8 | In Progress |
| M3 — Indexing & Search | 9–10 | Planned |
| M4 — MCP & AI Gateway | 11–13 | Planned |
| M5 — V1 Release | 14 | Planned |
| M6 — ContextOS (V2) | Post-V1 | Future Vision |

---

## Traceability

| Input Document | Role |
|---|---|
| `ROADMAP-002-Future-Vision.md` | V2–V4 long-horizon vision |
| `docs/000-Overview/Vision.md` | Long-term vision this roadmap advances |
| `docs/000-Overview/Mission.md` | Success criteria |
| `docs/001-Product/PRD-001.md` | Product requirements |
| `docs/adr/README.md` | All architecture decisions |
