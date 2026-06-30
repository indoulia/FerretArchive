# Sprint 9 Design Specification: Content Ingestion Pipeline

**Project:** Ferret (ContextOS)
**Date:** 2026-06-28
**Status:** Authoritative
**Companion Plans:** `s1` through `s5` implementation plan files

---

## 1. Overview

### Sprint Objective

Deliver the first complete content ingestion pipeline. A user can initialize a workspace, configure a filesystem connector, run `ferret index`, and produce a populated, searchable SQLite FTS5 database.

### End-to-End Success Criteria

The sprint is complete when all six steps work without error:

```
ferret workspace init
ferret connector enable filesystem
ferret index
# Result: .ferret/indexes/keyword/keyword-index.db exists and contains indexed content
```

Additional gates:
- 245+ tests green
- `git tag v0.9.0-sprint9` applied

### Out of Scope

The following are explicitly excluded from Sprint 9:

- Semantic search, embeddings, vector databases
- Incremental indexing (only full rebuild supported)
- Background services or file watching
- AI-assisted features

### Deferred to Sprint 10

Sprint 10 delivers **Information Retrieval** — not infrastructure, a user capability:

- `ferret search <query>` — BM25 keyword search against FTS5 index
- Phrase search, highlighting, snippet generation
- Ranking, filters, query parser
- `ferret search --format json` for machine consumption

---

## 2. Architecture

### Provenance Chain

The complete data flow from configuration to indexed content:

```
connectors.json
    → IConnectorManager → ConnectorRuntime
    → IAssetSource → IAssetReader → Stream
    → IParserDispatcher → ParseResult<Document>
    → IIndexEngine → SQLite FTS5
    → .ferret/indexes/keyword/keyword-index.db
```

### Platform Pattern

All future platform subsystems follow the same structural pattern:

```
Metadata → Descriptor → Instance → Manager → Status
```

This pattern applies uniformly to: Connectors, Parsers, Models, Plugins, MCP Servers, AI Agents.

### Governing Rules (from ADR-0014)

> Only `ConnectorManager` creates and disposes connector runtime instances. No other subsystem constructs connectors directly.

> Storage engines never own orchestration. `IIndexEngine` reads and writes documents. `IIndexPipeline` owns the full lifecycle.

---

## 3. New Projects

| Project | Role |
|---|---|
| `Ferret.ParserPlatform` | Parser registry, dispatcher, 3 built-in parsers, MIME resolver |
| `Ferret.Indexing` | SQLite FTS5 engine, `IndexPipeline` orchestrator |

**Note:** `Ferret.Core.Documents`, `Ferret.Core.Indexing`, and `Ferret.Core.Events.Indexing` are namespaces within the existing `Ferret.Core` assembly — not new projects.

---

## 4. Core Contracts Added

### `Ferret.Core.Documents` namespace

| Type | Kind | Purpose |
|---|---|---|
| `ParserId` | Value object | Stable parser identity |
| `DocumentKind` | Enum | Text, Structured, Binary, Unknown |
| `DocumentSection` | Record | A named subsection of parsed content |
| `Document` | Record | Canonical parsing output — parallel to `AssetDescriptor` |
| `ParseContext` | Record | Input to parsers: asset + media type + stream |
| `MediaTypeInfo` | Record | MIME type + encoding + parameters |
| `IMimeTypeResolver` | Interface | Extension/content-sniff → `MediaTypeInfo` |
| `ParseDiagnostic` | Record | Warning or error from a parser |
| `ParseResultKind` | Enum | Success, Partial, Failed, Unsupported |
| `ParseResult<T>` | Record | Discriminated union: document or failure + diagnostics |
| `ParserCapability` | Enum | Flags for declared parser features |
| `ParserCapabilities` | Record | Declared capability set |
| `ParserDescriptor` | Record | Static parser characteristics including priority |
| `IContentParser` | Interface | Parse a stream into `ParseResult<Document>` |
| `IParserRegistry` | Interface | Register and discover parsers |
| `IParserDispatcher` | Interface | Route assets to correct parser via media type |
| `IContentNormalizer` | Interface | **Reserved stub** — text normalization before indexing |

### `Ferret.Core.Indexing` namespace

| Type | Kind | Purpose |
|---|---|---|
| `IndexResult` | Record | Outcome of indexing a single document |
| `IndexStats` | Record | Aggregate pipeline run statistics |
| `IndexPipelineOptions` | Record | `ForceRebuild` flag + pipeline configuration |
| `IndexLayout` | Static class | Path constants for index storage (like `WorkspaceLayout`) |
| `IIndexEngine` | Interface | Read/write documents to storage backend |
| `IIndexPipeline` | Interface | Orchestrates full ingestion lifecycle |
| `IProgressReporter` | Interface | **Reserved** — live progress reporting |

### `Ferret.Core.Events.Indexing` namespace

8 domain events covering the full pipeline lifecycle:

| Event | Raised When |
|---|---|
| `IndexingStartedEvent` | Pipeline begins |
| `DocumentDiscoveredEvent` | Asset enumerated from connector |
| `DocumentParsedEvent` | Asset successfully parsed |
| `DocumentIndexedEvent` | Document written to index |
| `DocumentSkippedEvent` | Asset skipped (unsupported, filtered) |
| `DocumentParsingFailedEvent` | Parser returned failure result |
| `IndexingCompletedEvent` | Pipeline completes successfully |
| `IndexingFailedEvent` | Pipeline aborted with error |

### `Ferret.Core.Connectors` additions

| Type | Kind | Purpose |
|---|---|---|
| `IAssetReader` | Interface | Content retrieval, separate from `IAssetSource` discovery |
| `ConnectorInstance` | Record | Workspace-scoped connector configuration record |
| `ConnectorConfiguration` | Record | Abstraction over `Dictionary<string,string>` — future-proofs secrets |
| `ConnectorRuntime` | Record | Live instance: source + reader + metadata |
| `ConnectorStatus` | Enum | Updated to cover full lifecycle states |
| `ValidationResult` | Record | Outcome of connector configuration validation |
| `ValidationIssue` | Record | Individual validation finding |
| `ValidationSeverity` | Enum | Error, Warning, Info |
| `IConnectorInstanceStore` | Interface | Persistence of `ConnectorInstance` records |
| `IConnectorManager` | Interface | Full lifecycle: create, enable, disable, validate, dispose |

`IConnectorFactory.Create` updated to accept `ConnectorInstance` (was raw config dictionary).

### `Ferret.Core.Workspace` additions

| Type | Kind | Purpose |
|---|---|---|
| `IWorkspaceContext` | Interface | `WorkspaceId` + `WorkspaceRoot`; replaces `Directory.GetCurrentDirectory()` everywhere |

### `Ferret.Core.Primitives` additions

- `DocumentId.From(AssetId)` — factory method for deriving document identity from asset identity

---

## 5. Section Summary

| Section | Goal | Key Output |
|---|---|---|
| S1 | Core Document Contracts | 16 Document types, 5 Indexing types, 8 events, `IAssetReader` |
| S2 | Parser Platform | `Ferret.ParserPlatform`, 3 built-in parsers, `MimeTypeResolver` |
| S3 | Index Engine | `SqliteKeywordIndexEngine` (FTS5), `IndexPipeline` |
| S4 | Connector Config CLI | `ConnectorInstance`, `ConnectorManager`, `enable`/`disable`/`configure`/`inspect`/`validate` |
| S5 | `ferret index` + Wire-up | `IWorkspaceContext`, `IndexLayout`, `ferret index`, end-to-end integration test |

---

## 6. Key Design Decisions

Each decision below was explicitly debated during design.

### 1. Parser platform, not just 3 parsers

`IParserRegistry`, `IParserDispatcher`, and `ParserDescriptor.Priority` are defined as platform contracts. Adding a new parser in any future sprint requires no changes to `Ferret.Core` — only a new `IContentParser` implementation registered via DI. The 3 bundled parsers are first-class implementations of this platform, not special-cased logic.

### 2. MediaType-based dispatch, not file extension

File extension is resolved once at the connector boundary and stored as `MediaTypeInfo` on the asset. No downstream component re-derives media type from file paths. This eliminates ambiguity for files without extensions and allows connectors to override MIME type when they have authoritative knowledge (e.g., a SharePoint connector knows document types from the API response).

### 3. `Document` as canonical parsing output

`Document` is the parsing-layer parallel to `AssetDescriptor` in the connector layer. It carries: identity (`DocumentId`), provenance (`AssetId`), structured content (`DocumentSection[]`), metadata, and diagnostics. The `IIndexEngine` consumes `Document` records exclusively — it has no awareness of assets or connectors.

### 4. `Ferret.Core.Documents` namespace, not a new assembly

`Document` is a platform-wide concept consumed by indexing, search, and future AI subsystems. Placing it in a separate `Ferret.Documents.Abstractions` project would create a cross-cutting dependency that every subsystem must reference. Keeping it in `Ferret.Core` follows the established pattern for `Ferret.Core.Connectors` and `Ferret.Core.Workspace`.

### 5. 3 parsers bundled in `Ferret.ParserPlatform`

Plain text (`.txt`), Markdown (`.md`), and JSON (`.json`) are sufficient for Sprint 9 validation. Larger format parsers (PDF, DOCX, HTML) require third-party dependencies and are deferred. All 3 bundled parsers live in `Ferret.ParserPlatform` — no separate parser projects until the dependency footprint justifies isolation.

### 6. Two-table SQLite schema

```sql
CREATE TABLE documents (id TEXT PRIMARY KEY, asset_id TEXT, title TEXT, ...);
CREATE VIRTUAL TABLE documents_fts USING fts5(title, body, content='documents', content_rowid='rowid');
```

Metadata updates (title, path changes) do not trigger FTS rebuild. The `content=` option links the virtual table to the metadata table. This is the correct FTS5 pattern for content that changes independently of its text.

### 7. `IConnectorManager` owns connector lifetime

The pipeline receives a fully constructed `ConnectorRuntime` from `IConnectorManager`. It never constructs, configures, or disposes connector instances. This boundary enforces the ADR-0014 rule and makes the pipeline independently testable with mock runtimes.

### 8. `ConnectorConfiguration` abstraction

A typed record over `Dictionary<string, string>` rather than a raw dictionary. This provides a stable type boundary for future evolution: encrypted values, secret references, JSON sub-documents, environment variable substitution. The underlying storage format remains a flat key-value map in Sprint 9.

### 9. `IWorkspaceContext` over `Directory.GetCurrentDirectory()`

Workspace root is resolved once at the composition root and injected via `IWorkspaceContext`. No subsystem calls `Directory.GetCurrentDirectory()` directly. This makes every component that touches workspace paths testable without file system mocking and eliminates CWD race conditions in parallel tests.

### 10. `IIndexPipeline` as orchestration boundary

The CLI `ferret index` command calls `IIndexPipeline.RunAsync()` and receives `IndexStats`. It never touches `IIndexEngine`, `IConnectorManager`, or `IParserDispatcher` directly. All orchestration — connector enumeration, parsing, error handling, event publication — lives inside `IndexPipeline`. The CLI is a thin adapter.

### 11. `ForceRebuild` in `IndexPipelineOptions`

`ForceRebuild = true` triggers `IIndexEngine.ClearAsync()` before the pipeline runs. There is no partial-state rebuild or diff-based update in Sprint 9. The semantics are explicit: rebuild means clear and re-index everything. Incremental indexing (change detection, checksums, last-modified tracking) is deferred to a future sprint.

### 12. `IndexLayout` constants

Path management for index storage mirrors `WorkspaceLayout`. All paths under `.ferret/indexes/` are defined as constants in `IndexLayout`. No subsystem constructs index paths by string concatenation.

---

## 7. Reserved Extension Points

All extension points below are defined as reserved stubs or constants in Sprint 9. None are implemented.

| Interface / Type | Reserved For |
|---|---|
| `IContentNormalizer` | Text normalization (stemming, stop words, lowercasing) before indexing |
| `IAssetEnricher` | Post-discovery metadata enrichment (tags, labels, custom fields) |
| `IProgressReporter` | Live pipeline progress reporting to CLI or UI |
| `IIndexStore` | Pluggable storage backend below `IIndexEngine` |
| `ConnectorPolicy` | Read-only, bandwidth throttle, max-file-size, security constraints per connector |
| `ConnectorProfile` | Shared credential profiles reusable across multiple connector instances |
| `ferret connector doctor` | Health, permissions, and connectivity diagnostics |
| `IndexLayout.VectorDirectoryName` | Vector embedding database path constant |
| `IndexLayout.AnalyticsDirectoryName` | Analytics/usage database path constant |
| `DocumentVersion` | Document versioning in the provenance chain |
| `ConnectorRuntime.Session` | Active session object returned by `ConnectAsync()` |

---

## 8. ADR References

| ADR | Title | Relevance |
|---|---|---|
| `docs/adr/0014-document-processing-architecture.md` | Document Processing Architecture | 10 principles governing the full ingestion pipeline; canonical authority for all decisions in this sprint |

---

## 9. Sprint 10 Preview

Sprint 10 delivers **Information Retrieval** — the first search capability a user can invoke:

- `ferret search <query>` — BM25 keyword search against the FTS5 index built in Sprint 9
- Phrase search (`"exact phrase"`), term boosting
- Snippet generation with match highlighting
- Ranking and result filtering
- `ferret search --format json` for machine consumption and scripting
- Query parser for structured queries

Sprint 10 has no new infrastructure projects. It builds entirely on the index produced by Sprint 9.

---

## 10. Platform Rule

> **Metadata describes capabilities. Descriptor describes static characteristics. Instance describes workspace configuration. Manager owns lifecycle. Status represents runtime state.**

This pattern applies to every platform subsystem: Connectors, Parsers, Models, Plugins, MCP Servers, AI Agents. New subsystems introduced in future sprints must follow this structure. Deviation requires an ADR.
