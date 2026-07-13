# PROJECT-STATE — Ferret Platform: Current State

> **This is the first document to read.** It answers every "what, where, and why" question about the project at a glance. Keep it current.

| Field | Value |
|---|---|
| **Document ID** | PROJECT-STATE |
| **Version** | 1.0 |
| **Status** | Living Document — update after every sprint |
| **Last Updated** | 2026-07-13 |
| **Sprint 9 completed** | 2026-06-28 |
| **Sprint 10 completed** | 2026-06-28 |
| **Sprint 11 completed** | 2026-06-28 |
| **Sprint 12 completed** | 2026-06-29 |

> **Note (2026-07-13):** the sprint-numbered cadence below stops at Sprint 12. From v0.13.0
> onward, delivery moved to version-numbered releases tracked in `CHANGELOG.md` and, for the
> v2.0 milestone, the `docs/roadmap/` tree — those are the current sources of truth for
> post-Sprint-12 work; this document's Sprint 0–12 history is preserved as-is below.

---

## At a Glance

| Attribute | Value |
|---|---|
| **Product name** | Ferret |
| **Technology platform** | ContextOS |
| **Tagline** | Ferret — Dig Deep. Deliver Context. |
| **Current version** | 2.0.0 (Workspace Intelligence Platform) — see `CHANGELOG.md` and `docs/012-Releases/v2.0.0.md` |
| **Current milestone** | v2.0 shipped; v2.1 (federated context optimization) not yet started — see `docs/roadmap/FERRET-PRODUCT-ROADMAP.md` |
| **Test count** | 1,601 passing (0 failed) as of v2.0.0 |
| **Platform freeze** | M1 frozen at `v0.6.0-sprint6` (ADR-0012) |
| **CLI binary** | `ferret` |
| **Solution** | `src/Ferret.sln` |
| **Namespace prefix** | `Ferret.*` |

---

## Completed Sprints

| Sprint | Name | Status | Tests | Tag |
|---|---|---|---|---|
| Sprint 0 | Project Foundation | Done | — | — |
| Sprint 1 | Repository Scaffold | Done | — | — |
| Sprint 2–3 | Core Kernel | Done | 60+ | — |
| Sprint 4 | Architecture Baseline + Contracts | Done | 119 | `v0.4.0-sprint4` |
| Sprint 5 | Runtime Host + Rebrand | Done | ~180 | `v0.5.0-sprint5`, `v0.5.0-ferret` |
| Sprint 6 | Platform Entry Point & CLI | Done | 245 | `v0.6.0-sprint6` |
| Sprint 7 | Workspace Engine | Done | 245+ | `v0.7.0-sprint7` |
| Sprint 8 | Connector Platform | Done | 245+ | `v0.8.0-sprint8` |
| Sprint 9 | Content Ingestion Pipeline | Done | 651 | `v0.9.0-sprint9` |
| Sprint 10 | Information Retrieval | Done | 100+ | `v0.10.0-sprint10` |
| Sprint 11 | MCP Server | Done | 769 | `v0.11.0-sprint11` |
| Sprint 12 | AI Platform | Done | ~1060 | `v0.12.0-sprint12` |

---

## Sprint 7 — Workspace Engine (v0.7.0)

**Status:** Complete
**Tag:** `v0.7.0-sprint7`

### Delivered

- `Ferret.Workspace` library: `WorkspaceEngine`, `WorkspaceLocator`, `WorkspaceStateStore`
- `.ferret/` ContextOS directory tree (connectors, indexes, memory, knowledge, models, snapshots, telemetry, temp)
- `workspace.json` with ContextOS fields: `contextOsVersion`, `workspaceType`, `features`, `enabledConnectors`, `enabledModels`
- `state.json` with: `knowledgeVersion`, `graphVersion`, `lastIndex`, `connectors`, `statistics`
- `config/` seeded: `runtime.json`, `plugins.json`, `models.json`, `connectors.json`
- `IConnector`, `ConnectorType`, `ConnectorMetadata`, `ConnectorCapabilities`, `ConnectorHealth` in `Ferret.Core.Connectors`
- `WorkspaceCliModule`: `ferret workspace init`, `ferret workspace status`
- `RootCommandFactory` grouped subcommand activation

### What a new user can do after Sprint 7

Run `ferret workspace init` to create a `.ferret/` workspace and `ferret workspace status` to inspect it.

---

## Sprint 8 — Connector Platform (v0.8.0)

**Status:** Complete
**Tag:** `v0.8.0-sprint8`
**Date:** 2026-06-28

### Delivered

- `Ferret.ConnectorPlatform` — connector registry, capability model, typed IDs (`ConnectorId`, `ConnectorInstanceId`, `AssetId`), `AssetDescriptor`, `AssetKind`, `AssetFingerprint`, `IIgnoreProvider`, `AssetDiscoveryOptions`, `ConnectorCapability`, `ConnectorCapabilities` (8 singletons), `ConnectorDescriptor`, `IConnectorFactory`, `IConnectorSession`, `IAssetSource`, `IConnectorRegistry`, `ConnectorRegistry`, `RegistryBuilder`
- `Ferret.Connectors.Filesystem` — `FilesystemConnector` (IConnector + IAssetSource), `FilesystemConnectorFactory`, `FilesystemConnectorSession`, `FilesystemConnectorConfiguration`, `GitIgnoreProvider`, `FerretIgnoreProvider`, `CompositeIgnoreProvider`
- `Ferret.Cli` additions (non-breaking) — `ArgumentDefinition`, `ICommandResultFormatter<T>`, `CommandDefinition.WithArgument`, `RootCommandFactory` positional argument wiring, `ConnectorCliModule`, `ConnectorListCommandHandler`, `ConnectorInfoCommandHandler`, `TextConnectorListFormatter`, `TextConnectorInfoFormatter`
- `ferret connector list` — tabular list of registered connectors with capabilities
- `ferret connector info <id>` — capability detail with ✓/✗ matrix over all 8 singletons
- `Ferret.Architecture.Tests` — 6 executable architectural rules enforcing ADR-0013
- `Ferret.Core` updates — `ConnectorIoCapabilities` (renamed from `ConnectorCapabilities`), `IConnector.ConnectAsync` → `Task<IConnectorSession>`

### Architecture Documents

- ARCH-019: `docs/002-Architecture/ARCH-019-Connector-Platform-Architecture.md`
- ADR-0013: `docs/adr/0013-capability-based-platform-architecture.md`

### What a new user can do after Sprint 8

Run `ferret connector list` and `ferret connector info filesystem` to inspect the platform's connectors.

---

## Sprint 9 — Content Ingestion Pipeline (v0.9.0)

**Status:** Complete
**Tag:** `v0.9.0-sprint9`
**Date:** 2026-06-28

### Delivered

- `IWorkspaceContext` + `DefaultWorkspaceContext`: workspace identity propagated to all subsystems
- `IndexLayout`: conventional path constants for `.ferret/indexes/keyword/keyword-index.db`
- `IIndexPipeline.RunAsync` gains `WorkspaceId` first parameter
- `Ferret.ParserPlatform`: `ParserRegistry`, `ParserRegistryBuilder`, `ParserDispatcher`, `MimeTypeResolver`, `PlainTextParser`, `MarkdownParser`, `JsonParser`
- `SqliteKeywordIndexEngine`: FTS5 schema, upsert, `ClearAsync`, `GetStatsAsync`
- `IndexPipeline`: discover → parse → index orchestrator (`IConnectorManager` + `IParserDispatcher` + `IIndexEngine`)
- `ConnectorConfiguration`, `ConnectorInstance`, `ConnectorRuntime`, `ValidationResult`
- `IConnectorInstanceStore` / `ConnectorInstanceStore`: atomic JSON persistence to `.ferret/connectors.json`
- `IConnectorManager` / `ConnectorManager`: process-scoped runtime cache
- `FilesystemConnectorFactory`: config-driven connector creation
- `IndexSummaryViewModel` + `TextIndexSummaryFormatter`: structured CLI output
- `IndexCommandHandler`: `ferret index [--rebuild]` command
- `IndexCliModule`: DI module registering `IIndexEngine` + `IndexCommandHandler`
- `NullEventBus`: no-op event bus for Sprint 9 composition
- ADR-0014: Document Processing Architecture
- E2E tests: real filesystem, real parsers, real SQLite — 0 failures
- Benchmark test: 100 files indexed in < 10 seconds

### What a new user can do after Sprint 9

Run `ferret index` to discover files via `FilesystemConnector`, parse them with the parser platform, and write documents to a SQLite FTS5 database at `.ferret/indexes/keyword/keyword-index.db`.

---

## Sprint 10 — Information Retrieval (v0.10.0)

**Status:** Complete
**Tag:** `v0.10.0-sprint10`
**Date:** 2026-06-28

### Delivered

- `Ferret.Core.Search` — 20 contract types: `SearchExpression` AST hierarchy, `SearchHit` hierarchy, `ISearchService`, `IQueryParser`, `ISearchProvider`, `ISearchPostProcessor`, `SearchParseResult`, `SearchQuery`, `SearchOptions`, `SearchExecutionInfo`, `HighlightedText`, `TextSpan`, `SearchServiceResult`, `SearchDiagnostic`, `SearchProviderResult`
- `Ferret.Search` — `QueryParser` (implements `IQueryParser`), internal `Lexer` + `Token`, `Bm25SearchProvider` (SQLite FTS5), `QueryTranslator` (AST → FTS5), `HighlightParser` (sentinels → spans), `SearchService` (orchestrates providers + post-processors)
- `Ferret.Cli` additions — `ITextStyler`, `AnsiTextStyler`, `NullTextStyler`, `SearchRendererSelector`, `SearchViewModel`, `SearchCommandHandler`, `SearchCliModule`
- ADR-0015: Information Retrieval Architecture (5 principles)
- `ferret search <query>` — BM25 keyword search with ranked results and ANSI highlighting
- `ferret search --format json` — machine-readable JSON output
- `ferret search --limit N` — result count control
- `ferret search --no-highlight` — plain text output
- `ISearchProvider` extensibility — semantic provider slot reserved for Sprint 11+

### Architecture Documents

- ADR-0015: `docs/adr/0015-information-retrieval-architecture.md`

### What a new user can do after Sprint 10

Run `ferret search authentication` to get ranked, highlighted search results from the workspace index.

### Sprint 11 Technical Debt (from final review)

- `Bm25SearchProvider`: `documentsScanned` reports returned hit count, not rows examined by FTS5
- `Bm25SearchProvider`: `SqliteErrorCode == 1` (SQLITE_ERROR) is overly broad — swallows IO/schema errors as InvalidQuery
- `SearchCommandHandler`: `Mode = SearchExecutionMode.Auto` undocumented — diverges from `SearchOptions` default of `Keyword`
- `SearchCliModule`: `typeof(int)` for `--limit` OptionDefinition should be `typeof(string)` (matches CLI framework behaviour)

---

## Sprint 11 — MCP Server (v0.11.0)

**Status:** Complete
**Tag:** `v0.11.0-sprint11`
**Date:** 2026-06-28

### Delivered

- `Ferret.Mcp` — pure protocol adapter exposing Ferret platform capabilities over the MCP stdio transport
- `Ferret.Mcp.Protocol` — `IMcpTool`, `IMcpResource`, `IMcpTransport`, `IMcpRuntime`, `IMcpErrorMapper`, `McpArguments`, `McpContent`, `McpToolResult`, `McpResourceContent`, `McpToolDescriptor`, `McpResourceDescriptor`, `McpTransportDescriptor`
- `Ferret.Mcp.Registry` — `IMcpToolRegistry`, `IMcpResourceRegistry`, `McpToolRegistry`, `McpResourceRegistry`, `McpToolRegistryBuilder`, `McpResourceRegistryBuilder`
- `Ferret.Mcp.Tools` — `SearchTool` (search), `ReadDocumentTool` (read_document), `WorkspaceStatusTool` (workspace_status)
- `Ferret.Mcp.Resources` — `WorkspaceStatusResource` (workspace://status), `IndexStatsResource` (workspace://index/stats), `ConnectorsResource` (workspace://connectors)
- `Ferret.Mcp.Transport.Stdio` — `StdioTransport`, `SdkToolAdapter`, `SdkResourceAdapter`, `McpArgumentsFactory`, `McpErrorMapper` (MCP SDK isolated here only)
- `Ferret.Mcp.Runtime` — `McpRuntime` (DI composition orchestrator)
- `McpModule.ConfigureServices` — DI composition root registering all tools, resources, transport, and runtime
- `Ferret.Cli` additions — `ServeCliModule`, `ServeCommandHandler`, `ferret serve` command
- `Ferret.Architecture.Tests` additions — 5 MCP isolation tests (ADR-0018)
- ADRs 0016, 0017, 0018 — MCP architecture decisions
- `IDocumentService` + `DocumentService` — document retrieval service
- ModelContextProtocol SDK 1.4.0 integration (handler-delegate pattern via `McpServerHandlers`)

### Architecture

MCP SDK types (`ModelContextProtocol.*`) are confined to `Ferret.Mcp.Transport.Stdio` namespace only. All other layers use pure Ferret contracts with no SDK dependency.

### What a new user can do after Sprint 11

Run `ferret serve` to start an MCP stdio server. Any MCP-compatible AI host (Claude Desktop, Cursor, etc.) can query workspace search, read documents, and inspect workspace status via the MCP protocol.

---

## Sprint 12 — AI Platform (v0.12.0)

**Status:** Complete
**Tag:** `v0.12.0-sprint12`
**Date:** 2026-06-29

### Delivered

- `Ferret.Core.Ai` — zero-dependency AI contracts: `IModelProvider`, `IModelRegistry`, `IModelRouter`, `ModelDescriptor`, `ModelId`, `ProviderId`, `ModelCapabilities` ([Flags] enum), `ModelNotFoundException`
- `Ferret.Configuration.AI` — `AiOptions`, `OllamaOptions`, `OpenAiOptions`, `AiConfigurationModule`
- `Ferret.Models` — `ModelRegistry` (immutable async factory, fault isolation), `ModelRouter`, `ModelPlatformModule`
- `Ferret.Providers.Ollama` — `OllamaModelProvider`, `OllamaChatModel`, `OllamaEmbeddingModel`, `OllamaProviderModule`
- `Ferret.Providers.OpenAi` — `OpenAiModelProvider`, `OpenAiChatModel`, `OpenAiEmbeddingModel`, `OpenAiProviderModule`
- `Ferret.Providers.Compliance` — abstract `ProviderComplianceTests` (15 `[Fact]` methods); both providers pass
- `Ferret.Prompts` — `PromptTemplate`, `PromptVariables`, `IPromptRegistry`, `PromptRegistry`, `IPromptRenderer`, `PromptRenderer`, `PromptRenderException`, `PromptsModule`
- `Ferret.Cli` additions — `ferret models list`, `ferret models info <model-id>`, `ferret prompt list`
- `Ferret.Architecture.Tests` additions — 5 AI platform isolation tests (ADR-0019)
- ADR-0019: AI Platform Architecture (SDK isolation, IModelProvider unit, immutable registry, config-driven routing)
- ADR-0020: Prompt Platform Architecture (mustache substitution, immutable registry, DI template registration, stateless renderer)

### Architecture

Vendor AI SDKs (`OllamaSharp`, `OpenAI`) are confined to their respective `Ferret.Providers.*` assemblies. `Ferret.Core.Ai` has zero external references. Sprint 12 version gate: no LLM calls at runtime — model discovery and routing only.

### What a new user can do after Sprint 12

Run `ferret models list` to see the registered AI model catalog, `ferret models info ollama/llama3.2` for model detail, and `ferret prompt list` to inspect the prompt template registry.

---

## Post-Sprint-12 Delivery

**Status:** Sprint-numbered cadence ended after Sprint 12. Delivery since has been
version-numbered: v0.13.0–v0.16.0 (see `CHANGELOG.md`), then the Workspace Intelligence
Platform milestone shipped as **v2.0.0** (2026-07-13, see `docs/012-Releases/v2.0.0.md`).
Full engineering detail for v2.0.0 lives under `docs/roadmap/Workspace-Intelligence/`.

---

## Platform Architecture

### Layer Model

```
Ferret.Cli              ← CLI entry point; ICliModule; System.CommandLine
  Ferret.Workspace      ← Sprint 7: WorkspaceEngine, WorkspaceLocator (NEW)
    Ferret.Runtime      ← RuntimeHost wrapping Microsoft.Extensions.Hosting
      Ferret.Hosting    ← IHostedService integration
        Ferret.Core     ← Zero-dependency contracts, value objects, exceptions
          Ferret.Events ← In-process event bus
          Ferret.Health ← IDiagnosticCheck, DiagnosticRunner
```

### Frozen M1 Packages (ADR-0012)

These packages are frozen as of Sprint 6. No breaking changes without a superseding ADR:

| Package | Responsibility |
|---|---|
| `Ferret.Core` | Base contracts, exceptions, result types, typed IDs |
| `Ferret.Runtime` | Runtime host, module lifecycle, DI orchestration |
| `Ferret.Hosting` | `IHostedService` integration, startup/shutdown |
| `Ferret.Cli` | CLI entry point, command dispatch, branding |
| `Ferret.Events` | Event bus contracts and in-process implementation |
| `Ferret.Health` | `IDiagnosticCheck`, `DiagnosticRunner`, health reporting |

### Key Design Patterns

| Pattern | Location | Purpose |
|---|---|---|
| `IModule` | `Ferret.Core` | Every capability is a module; DI-first |
| `ICliModule` + `CommandDefinition` | `Ferret.Cli` | Modules contribute commands to CLI |
| `ICommandHandler` | `Ferret.Cli` | Single-method command handler interface |
| `IFerretContext` | `Ferret.Cli` | Handler isolation: no static state |
| `IOutputFormatter` | `Ferret.Cli` | All CLI output abstracted for testing |
| `IDiagnosticCheck` | `Ferret.Health` | Module-contributed `ferret doctor` checks |
| `IConnector` | `Ferret.Core.Connectors` | (Sprint 7) Connector contract |
| `CommandDefinition.Group` | `Ferret.Cli` | Subcommand nesting (activated Sprint 7) |

---

## CLI Commands

| Command | Status | Sprint |
|---|---|---|
| `ferret --version` | Shipped | Sprint 6 |
| `ferret doctor` | Shipped | Sprint 6 |
| `ferret status` | Stub (not-running) | Sprint 6; IPC deferred |
| `ferret workspace init` | Shipped | Sprint 7 |
| `ferret workspace status` | Shipped | Sprint 7 |
| `ferret connector list` | Shipped | Sprint 8 |
| `ferret connector info <id>` | Shipped | Sprint 8 |
| `ferret index` | Shipped | Sprint 9 |
| `ferret search` | Shipped | Sprint 10 |
| `ferret serve` (MCP) | Shipped | Sprint 11 |
| `ferret models list` | Shipped | Sprint 12 |
| `ferret models info <id>` | Shipped | Sprint 12 |
| `ferret prompt list` | Shipped | Sprint 12 |

---

## Active ADRs

| ADR | Title | Status |
|---|---|---|
| ADR-0001 | Use Architecture Decision Records | Accepted |
| ADR-0005 | Product Rebranding: AISpace to Ferret | Accepted |
| ADR-0011 | Rename Ferret.Sdk to Ferret.Plugin.SDK | Accepted |
| ADR-0012 | Milestone 1: Platform Foundation Freeze | Accepted |
| ADR-0013 | Capability-Based Platform Architecture | Accepted |
| ADR-0019 | AI Platform Architecture | Accepted |
| ADR-0020 | Prompt Platform Architecture | Accepted |

---

## Roadmap Summary

| Version | Theme | Key Deliverable |
|---|---|---|
| **V1 — Ferret Platform** | Working product | `ferret workspace init`, `ferret search`, MCP, AI Gateway (Sprints 7–14) |
| V2 — ContextOS | Context OS | Full connector suite, knowledge graph, multi-modal search, memory |
| V2.5 — Ferret Insights | Business Intelligence | Analytics platform, dashboards, ROI reporting |
| V3 — Enterprise Intelligence | Enterprise AI | Decision Intelligence, Root Cause Intelligence, Architecture Intelligence |
| V3.5 — Enterprise Time Machine | Temporal Knowledge | Snapshots, incident replay, historical search |
| V4 — Autonomous Enterprise | Autonomous | Digital Twin, AI Learning, Autonomous quality monitoring |

See `ROADMAP-001.md` (V1 committed) and `ROADMAP-002-Future-Vision.md` (V2–V4 vision).

### V1 Sprint Outlook

| Sprint | Name | User Value |
|---|---|---|
| 7 | Workspace Engine | `ferret workspace init / status` |
| 8 | Filesystem Connector | `ferret connector list` |
| 9 | Indexing Pipeline | `ferret index` |
| 10 | Semantic Search | `ferret search` |
| 11 | MCP Server | AI hosts consume Ferret context |
| 12 | Context Intelligence | Context compression + assembly |
| 13 | AI Gateway | Local + remote model routing |
| 14 | V1 Release Candidate | Public release |

---

## Technology Stack

| Layer | Technology | Decision |
|---|---|---|
| Runtime | .NET 9, C# 13 | Accepted |
| Testing | xUnit | Accepted |
| Code analysis | StyleCop, AnalysisMode: All | Accepted |
| CLI | System.CommandLine 2.0 beta | Accepted |
| Hosting | Microsoft.Extensions.Hosting (internal) | Accepted |
| JSON | System.Text.Json (BCL) | Accepted |
| DI | Microsoft.Extensions.DependencyInjection | Accepted |
| Resilience | Polly | Deferred (Sprint 8) |
| Rich terminal | Spectre.Console | Deferred (Sprint 9) |
| Embedding models | TBD (nomic-embed-text candidate) | Deferred (Sprint 10) |
| MCP server | ModelContextProtocol 1.4.0 (stdio) | Shipped Sprint 11 |

See `TECH-001-Technology-Evaluation.md` for the full evaluation grid including rejected technologies.

---

## Technical Debt

| Item | Sprint introduced | Priority | Plan |
|---|---|---|---|
| `ferret status` IPC (process liveness) | Sprint 6 | Medium | Sprint 7 (named pipes or gRPC) |
| Missing ADRs 0002–0004, 0006–0010 | Pre-Sprint 6 | Low | Backfill when relevant decisions are documented |
| Health check middleware (ASP.NET Core) | Sprint 6 | Low | Sprint 11 when MCP HTTP server added |
| `TestCancellationToken` parallel isolation | Sprint 6 | Done | Fixed in Sprint 6 final commit |

---

## Known Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| System.CommandLine GA API breaks | Medium | Medium | Wrapped behind `RootCommandFactory`; update localized |
| Local embedding model quality insufficient | Medium | High | Configurable via `models.json`; pluggable in V2 |
| Property graph DB for V2 | Medium | High | Prototype before Sprint 12 commitment |
| M1 package design flaw discovered | Low | High | ADR supersession process; formal correction path |

---

## Key File Locations

| Type | Location |
|---|---|
| Solution | `src/Ferret.sln` |
| Architecture ADRs | `docs/adr/` |
| Sprint plans | `docs/archive/superpowers/plans/` |
| Sprint reviews | `docs/archive/sprint-reviews/` |
| Product roadmap | `docs/001-Product/ROADMAP-001.md` |
| Future vision | `docs/001-Product/ROADMAP-002-Future-Vision.md` |
| Brand identity | `docs/002-Architecture/BRAND-001.md` |
| Technology decisions | `docs/002-Architecture/TECH-001-Technology-Evaluation.md` |
| Decision history | `docs/013-Governance/DECISION-LOG.md` |
| Competitive landscape | `docs/001-Product/COMPETITIVE-001.md` |
| Future architecture | `docs/002-Architecture/FUTURE-001-Future-Architecture.md` |
| Rebranding history | `docs/HISTORY.md` |
| Ideas backlog | `docs/IDEAS.md` |
| Research backlog | `docs/RESEARCH-001-Future-Research.md` |
| Migration guide | `docs/MIGRATION-001.md` |
| M1 freeze ADR | `docs/adr/0012-milestone-1-platform-foundation-freeze.md` |
| Capability platform ADR | `docs/adr/0013-capability-based-platform-architecture.md` |
| Storage architecture | `docs/002-Architecture/ARCH-017-Storage-Architecture.md` |
| Analytics architecture | `docs/002-Architecture/ARCH-018-Analytics-Architecture.md` |
| Connector platform architecture | `docs/002-Architecture/ARCH-019-Connector-Platform-Architecture.md` |
| Reusable prompt template | `docs/archive/superpowers/prompts/roadmap-consolidation.md` |

---

## For AI Assistants

If you are an AI assistant reading this document to continue development on Ferret:

1. **Read this document first.** It is the source of truth for current project state.
2. **M1 is frozen.** Do not modify `Ferret.Core`, `Ferret.Runtime`, `Ferret.Hosting`, `Ferret.Cli`, `Ferret.Events`, or `Ferret.Health` in breaking ways. New types may be added to `Ferret.Core.*` namespaces as non-breaking additions.
3. **Sprints 7–12 are complete; the sprint cadence ended there.** Sprint 12 delivered the AI platform, prompt platform, and `ferret models`/`ferret prompt` CLI commands. Delivery since is version-numbered — current version is 2.0.0 (Workspace Intelligence Platform); see `CHANGELOG.md` and `docs/roadmap/` for what's shipped and what's next.
4. **TDD.** Every task: failing test first, confirm red, implement, verify green.
5. **System.Text.Json** is available in BCL (.NET 9) — no package reference needed.
6. **`WorkspaceStatistics.Create`** signature: `(int totalFiles, int indexedFiles, DateTimeOffset lastIndexed, string schemaVersion)` — `lastIndexed` is non-nullable; use `DateTimeOffset.MinValue` as sentinel.
7. **Commit after every task.** Use prefixes: `feat(sprint-13):`, `test(sprint-13):`, `chore(sprint-13):`.
8. **Tag each sprint** on completion: `v0.13.0-sprint13` format.
9. **Every sprint answers:** "What can a new user do today they couldn't do yesterday?" — no pure-infrastructure sprints.
10. **Ferret.Manual (`HtmlTemplate.cs`)** has pre-existing StyleCop errors — do not fix; ignore this file.
