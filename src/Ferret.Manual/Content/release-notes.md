# Release Notes

Version history for Ferret from Sprint 8 (v0.8.0) to Sprint 14 / RC1 (v0.14.0).

For earlier sprint history (Sprints 0-7), see `docs/adr/` and the git tag history.

---

## v0.14.0 — RC1 (Sprint 14)

**Tag:** `v0.14.0-sprint14`  
**Status:** Release Candidate

### Highlights

- **The Ferret Manual** — self-hosted documentation site; `ferret manual` opens this manual in your browser
- **File Watching** — `ferret watch` monitors workspace for changes and re-indexes automatically
- **Incremental Indexing** — subsequent `ferret index` runs only process changed files
- **Performance** — batch writer, parallel parser, SQLite WAL mode; 1,000-document workspace indexes in under 5 seconds
- **Diagnostics** — `ferret doctor` extended with index health, provider health, and workspace integrity checks
- **Configuration** — three-layer config: `ferret.config.json` → environment variables → defaults
- **Installer** — self-contained binaries for `win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`
- **E2E Tests** — end-to-end test suite covering init → index → search → serve → watch lifecycle

---

## v0.13.0 (Sprint 13)

**Tag:** `v0.13.0-sprint13`

### Highlights

- **Context Assembly** — `ferret_context` MCP tool; six-stage pipeline (search → deduplicate → filter → expand → token budget → format)
- **Context MCP Tool** — `IContextStage` pipeline; configurable token budget
- **MCP CLI Wireup** — `ferret serve` starts the MCP server; all four tools registered and tested

---

## v0.12.0 (Sprint 12)

**Tag:** `v0.12.0-sprint12`

### Highlights

- **AI Platform** — `Ferret.Core.Ai` contracts: `IModelProvider`, `IChatModel`, `IEmbeddingModel` (ADR-0019)
- **Ollama Provider** — `Ferret.Providers.Ollama` using OllamaSharp; local model support
- **OpenAI Provider** — `Ferret.Providers.OpenAi` using OpenAI SDK; cloud model support
- **Model Registry** — `ModelRegistry` built at startup, immutable thereafter
- **Model Router** — `IModelRouter` reads `AiOptions.DefaultChatModel`, resolves provider at startup
- **Prompt Platform** — `IPromptRegistry`, `IPromptRenderer`, `{{variable}}` substitution (ADR-0020)
- **CLI** — `ferret models list`, `ferret models info`, `ferret prompt list`, `ferret prompt run`
- **Architecture Tests** — SDK isolation enforced: `OllamaSharp.*` and `OpenAI.*` confined to provider packages

---

## v0.11.0 (Sprint 11)

**Tag:** `v0.11.0-sprint11`

### Highlights

- **MCP Runtime** — `Ferret.Mcp` package; stdio transport; `IMcpTool`, `IMcpRuntime`, `IMcpToolRegistry` (ADR-0017)
- **Search Tool** — `ferret_search` MCP tool; calls `ISearchService`
- **Read Document Tool** — `ferret_read_document` MCP tool; returns full document content
- **Workspace Status Tool** — `ferret_workspace_status` MCP tool
- **SDK Isolation** — `ModelContextProtocol.*` confined to `Transport/Stdio/` (ADR-0016, ADR-0017)
- **Integration Architecture** — Host Architecture Pattern: `Capabilities → Platform Services → Hosts → Protocols` (ADR-0016)

---

## v0.10.0 (Sprint 10)

**Tag:** `v0.10.0-sprint10`

### Highlights

- **Search** — `ferret search` command; BM25/FTS5 search against the index (ADR-0015)
- **Search Architecture** — canonical query AST; `ISearchProvider`; `ISearchPostProcessor`; `SearchServiceResult`
- **BM25 Search Provider** — SQLite FTS5 BM25 ranking; score normalization to `[0.0, 1.0]`
- **CLI Output** — results with snippets, scores, and ANSI highlights
- **JSON Output** — `ferret search --json` for scripting

---

## v0.9.0 (Sprint 9)

**Tag:** `v0.9.0-sprint9`

### Highlights

- **Document Processing** — `IContentParser`, `IParserDispatcher`, `Document` canonical model (ADR-0014)
- **Index Pipeline** — `IIndexPipeline.RunAsync`; discover → parse → index; `IndexResult` with counts
- **Parsers** — C#, Markdown, JSON, XML, plain text, YAML, HTML, INI parsers registered
- **SQLite FTS5 Index** — `keyword-index.db`; `documents` and `documents_fts` tables
- **ferret index** — full index command; progress output
- **Incremental foundation** — `IChangeSource` reserved for Sprint 14

---

## v0.8.0 (Sprint 8)

**Tag:** `v0.8.0-sprint8`

### Highlights

- **Connector Platform** — `IConnector`, `IAssetSource`, `AssetDescriptor`, `ConnectorMetadata` (ADR-0013)
- **Filesystem Connector** — default connector; glob include/exclude; `.ferretignore` support
- **Capability Principles** — seven platform principles adopted as ADR-0013; architecture tests added
- **Connector Registry** — `IConnectorRegistry`, `IConnectorManager`
- **Workspace v1** — `workspace.json` schema; connector configuration

## Related

- [Architecture Reference](../reference/architecture) — ADR index
- [Getting Started](../getting-started/index) — install and use the current release
