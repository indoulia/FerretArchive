# 008 — Modules

Component and module design documents for Ferret internal modules.

---

## Index

*(Last synchronized against `src/` on 2026-07-06. No dedicated ARCH-NNN Type-B component doc exists yet for most of these — see the "Architecture Documentation Gap" note below.)*

| Module | Package | Description |
|---|---|---|
| Core | `Ferret.Core` | Domain model, contracts, value objects, exceptions. Zero external dependencies (AC-012 Minimal Core). |
| Runtime | `Ferret.Runtime` | Module lifecycle, DI composition, `IRuntimeHost` (wraps `Microsoft.Extensions.Hosting` internally). |
| CLI | `Ferret.Cli` | CLI command implementations and the `ferret` entry point; hosts all `ICliModule`s. |
| MCP | `Ferret.Mcp` | MCP server and tool implementations: `search`, `ferret_context`, `read_document`, `workspace_status`, `workspace_list`. |
| Plugins | `Ferret.Plugins` | Plugin host and isolation boundary. |
| Plugin SDK | `Ferret.Plugin.SDK` | Public contracts for plugin authors (renamed from `Ferret.Sdk`, see ADR-0011). |
| Configuration | `Ferret.Configuration` | Configuration loading and validation. |
| AI Configuration | `Ferret.Configuration.AI` | AI-provider-specific configuration schema. |
| Connector Platform | `Ferret.ConnectorPlatform` | Connector manager and connector instance lifecycle. |
| Filesystem Connector | `Ferret.Connectors.Filesystem` | The default filesystem connector — discovery, ignore-file handling, skip-list. |
| Indexing | `Ferret.Indexing` | Discover → parse → index pipeline; incremental fingerprint state store. |
| Parser Platform | `Ferret.ParserPlatform` | Parser dispatch and MIME type resolution. |
| Parsers | `Ferret.Parsers` | Base/plain-text content parsers. |
| Office Parsers | `Ferret.Parsers.Office` | Word / Excel / PowerPoint parsers. |
| PDF Parser | `Ferret.Parsers.Pdf` | PDF parser. |
| Search | `Ferret.Search` | Query parsing and the BM25/FTS5 keyword search provider. |
| AI Core | `Ferret.AI` | Context assembly pipeline (`ferret context` / MCP `ferret_context`); model provider abstractions. |
| Prompts | `Ferret.Prompts` | Prompt template platform. |
| Ollama Provider | `Ferret.Providers.Ollama` | Ollama model provider plugin. |
| OpenAI Provider | `Ferret.Providers.OpenAi` | OpenAI model provider plugin. |
| Models | `Ferret.Models` | Shared model/provider contracts. |
| Workspace | `Ferret.Workspace` | Single-repo workspace engine (`ferret workspace init` / `status`). |
| Workspace Graph | `Ferret.Workspace.Graph` | Multi-repo workspace registry, cross-workspace references, DAG enforcement — see `docs/roadmap/Workspace-Intelligence/01-Architecture.md`, ADR-0026. |
| Knowledge Federation | `Ferret.Knowledge.Federation` | Federated cross-workspace knowledge queries (`IFederatedKnowledgeStore`) — see ADR-0027. |
| Persistence | `Ferret.Persistence` | V2 dependency-graph persistence mechanism (ADR-0022–0024). |
| Vertical Slice | `Ferret.VerticalSlice` | V2 architecture vertical-slice host. |
| Telemetry | `Ferret.Telemetry` | Logging / metrics / tracing sink pipeline. |
| Manual | `Ferret.Manual` | Documentation portal content and generator. |

---

## Architecture Documentation Gap

Most modules above have no dedicated ARCH-NNN Type-B component document (per `ARCH-TEMPLATE-001`) — only `Ferret.Workspace` (ARCH-003), `Ferret.Configuration` (ARCH-011), and the platform-wide `ARCH-001` exist at that level today. Search, Indexing, MCP, CLI, Connectors, and AI/Context are covered only by lighter-weight ADRs (0014–0017, 0019–0020) and sprint plans, not a full component doc. Not fixed here — writing new ARCH-NNN documents is design work, out of scope for a documentation-alignment pass; flagged for a future architecture-documentation sprint.

---

## Writing a Module Design

Use [docs/templates/architecture.md](../templates/architecture.md) for module design documents.
Each module document should include C3-level component diagrams.

## Writing a Module Design

Use [docs/templates/architecture.md](../templates/architecture.md) for module design documents.
Each module document should include C3-level component diagrams.
