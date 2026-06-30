# TECH-001 — Technology Evaluation

| Field | Value |
|---|---|
| **Document ID** | TECH-001 |
| **Version** | 1.0 |
| **Status** | Living Document |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

---

## Purpose

This document records technology evaluation decisions: what was accepted, what was rejected, and what was deferred. It captures the reasoning so future contributors understand why the stack looks the way it does and can make informed decisions about future additions.

---

## Accepted Technologies

### .NET 9 / C# 13

- **Decision:** Accepted — Sprint 0
- **Reasoning:** Current LTS, best performance characteristics for native AOT (future), top-class SDK tooling, strong open-source ecosystem. C# 13 primary constructors and collection expressions improve code density.
- **Target framework:** `net9.0` throughout
- **LangVersion:** `latest` in all non-test projects
- **Risk:** .NET 10 LTS will arrive; migration is expected but low-effort

---

### xUnit

- **Decision:** Accepted — Sprint 0
- **Reasoning:** De-facto standard for .NET open-source. Parallel test execution by default. Theory/Fact split maps cleanly to property-based vs example-based tests. No magic attributes.
- **Alternatives considered:** NUnit (more verbose), MSTest (more enterprise, less community)

---

### StyleCop (via `Microsoft.CodeAnalysis.CSharp`)

- **Decision:** Accepted — Sprint 0
- **Configuration:** `AnalysisMode: All` in `Directory.Build.props`, `TreatWarningsAsErrors: true`
- **Reasoning:** Enforces consistency across all contributors. Catches real issues (missing XML docs, unintended public APIs). Zero runtime cost — compile-time only.
- **Note:** `GenerateDocumentationFile: true` for all non-test projects enables IDE documentation popups and enforces XML comment coverage.

---

### System.CommandLine

- **Decision:** Accepted — Sprint 6
- **Version:** `2.0.0-beta4` (pre-release; stable API since beta3)
- **Reasoning:** Microsoft-backed, .NET-native, composable command tree. Handler injection via `ICommandHandler` maps cleanly to `Ferret.Cli` architecture. `--help` generation is automatic. `InvokeAsync` returns exit code.
- **Alternatives considered:** Cocona (more magic, less control), CommandLineParser (older, less active), Spectre.Console CLI (good UX but heavier dependency)
- **Risk:** Still pre-release. API may change in final release. Mitigation: all System.CommandLine types are behind `RootCommandFactory` — update is localized.

---

### Microsoft.Extensions.Hosting

- **Decision:** Accepted (internal wrap) — Sprint 5
- **Reasoning:** Standard .NET hosted service lifecycle. Well-understood by .NET developers. DI, configuration, logging all included.
- **Important:** Wrapped internally in `Ferret.Runtime`. `IHost` is never exposed through `Ferret.Core` contracts. This allows swapping the host implementation in a future sprint without breaking consumers.
- **Alternatives considered:** Custom lifecycle management (rejected: reinventing a solved problem)

---

### Microsoft.Extensions.DependencyInjection

- **Decision:** Accepted — Sprint 0
- **Reasoning:** BCL-adjacent. Used by all .NET ecosystem packages. Zero learning curve.
- **Note:** DI is the composition mechanism. No service locator pattern is permitted.

---

### System.Text.Json

- **Decision:** Accepted — Sprint 7
- **Reasoning:** BCL (.NET 9), no package reference needed. Sufficient for workspace JSON serialization. Supports `[JsonPropertyName]` camelCase contracts.
- **Alternatives considered:** Newtonsoft.Json (rejected: external dependency, heavier, unnecessary for simple JSON files), MessagePack (rejected: binary format not appropriate for human-readable config files)

---

### coverlet.collector

- **Decision:** Accepted — Sprint 0
- **Reasoning:** Standard .NET code coverage collection. Works with `dotnet test --collect:"XPlat Code Coverage"`.

---

## Rejected Technologies

### Scrutor

- **Decision:** Rejected — Sprint 3
- **Reasoning:** Assembly scanning for DI registration is convenient but makes the composition contract implicit. In `Ferret.Core`, explicit registration is required so that the DI graph is auditable and testable without the scanning library present. Scrutor would make it too easy to accidentally register internal types as public services.

---

### MediatR

- **Decision:** Rejected — Sprint 3
- **Reasoning:** MediatR adds a generic mediator layer that obscures the actual call path. `ICommandHandler` achieves the same decoupling with explicit types and no external dependency. In-process event bus (`Ferret.Events`) handles event dispatch without MediatR's overhead.

---

### Polly

- **Decision:** Rejected for M1; Deferred for connector layer — Sprint 3
- **Reasoning:** Resilience policies are not needed in the platform foundation (all M1 operations are local and synchronous). When connectors are added (Sprint 8+), Polly becomes the right choice for retry/circuit-breaker on remote connections.
- **Target:** `Ferret.Connectors.*` projects

---

### System.Threading.Channels

- **Decision:** Deferred — Sprint 3
- **Reasoning:** Evaluated for the in-process event bus. The simpler `List<IEventHandler>` dispatch was sufficient for M1 synchronous event delivery. Channels will be reconsidered when async streaming events are needed (e.g., real-time log ingestion in Sprint 9+).
- **Target:** `Ferret.Events` v2 (post-M1)

---

### ASP.NET Core Health Checks middleware

- **Decision:** Deferred — Sprint 6
- **Reasoning:** `IDiagnosticCheck` / `DiagnosticRunner` in `Ferret.Health` is the custom health check framework. It does not depend on ASP.NET Core. When the MCP server is added (Sprint 11), ASP.NET Core will be added to the MCP project. At that point, health check middleware can bridge to `DiagnosticRunner`.
- **Target:** `Ferret.Mcp` (Sprint 11)

---

### Spectre.Console

- **Decision:** Deferred — Sprint 6
- **Reasoning:** `IOutputFormatter` provides structured output abstraction. Spectre.Console is suitable for rich terminal UI (progress bars, tables, live display). Deferred until `ferret index` needs progress reporting (Sprint 9).
- **Target:** Sprint 9 indexing pipeline

---

### Entity Framework Core

- **Decision:** Rejected for workspace state
- **Reasoning:** Workspace state (workspace.json, state.json) is human-readable JSON in the repository. EF Core would add a database dependency for what is fundamentally a file-backed store. The knowledge graph (V2) will use a dedicated graph database or embedded property graph — not EF Core.

---

### SQLite (for workspace state)

- **Decision:** Deferred for indexes; Not needed for workspace state
- **Reasoning:** For keyword indexes (Sprint 9), SQLite FTS5 is a strong candidate. For workspace state and config, JSON files are intentional (human-readable, git-diffable, no extra dependency).
- **Target:** `Ferret.Index.Keyword` (Sprint 9)

---

## Deferred Technology Decisions

| Technology | Category | Target Sprint | Notes |
|---|---|---|---|
| Embedding models (local) | AI | Sprint 10 | `nomic-embed-text`, `all-MiniLM-L6-v2` candidates |
| Vector index | AI | Sprint 10 | Evaluate: HNSW in-process vs SQLite-vec vs FAISS |
| Property graph DB | Knowledge | Sprint 12+ | Evaluate: LiteGraph, DGraph embedded, custom |
| Polly | Resilience | Sprint 8 | Connector retry/circuit-breaker |
| Channels | Async events | Post-M1 | Async event streaming |
| Spectre.Console | UX | Sprint 9 | Rich terminal UI for indexing |
| SQLite FTS5 | Index | Sprint 9 | Keyword index backend |
| gRPC / named pipes | IPC | Sprint 7 | `ferret status` real-time: detect running daemon |
| ASP.NET Core | Web | Sprint 11 | MCP server HTTP layer |

---

## Technology Risk Register

| Technology | Risk | Mitigation |
|---|---|---|
| System.CommandLine (pre-release) | API breaking change in GA | Wrapped behind `RootCommandFactory`; update isolated to one file |
| Local embedding models | Model quality vs size tradeoff | Configurable via `.ferret/config/models.json` |
| Property graph (V2) | No obvious embedded .NET graph DB | Prototype before committing; custom JSON graph is fallback |
| .NET AOT (future) | Some reflection-heavy code incompatible | `System.Text.Json` source generators ready when needed; `Ferret.Core` is AOT-compatible already |
