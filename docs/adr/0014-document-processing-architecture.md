# ADR-0014 — Document Processing Architecture

| Field | Value |
|---|---|
| **Document ID** | ADR-0014 |
| **Status** | Accepted |
| **Sprint** | Sprint 9 |
| **Author** | Ferret Core Team |
| **Date** | 2026-06-28 |
| **Supersedes** | — |

---

## Context

Sprint 9 introduces the Document Platform — the second canonical pipeline stage in ContextOS, sitting between the Connector Platform (AssetDescriptor) and the Index Engine (keyword index). The platform needs a stable architectural foundation that can scale from local indexing to enterprise knowledge extraction without fundamental redesign.

---

## Decision

The Document Processing Architecture is governed by eight platform principles:

### Principle 1 — Document is the canonical parsing model

`Document` is the canonical output of the parsing stage, parallel to `AssetDescriptor` for the Connector Platform. Every parsing pipeline stage produces a `Document`; every downstream stage (indexing, knowledge extraction, context assembly, AI) consumes `Document` instances. No ad-hoc text strings or intermediate formats cross stage boundaries.

### Principle 2 — Parser dispatch by MediaType, not file extension

Parser selection is determined by `AssetDescriptor.MediaType` — a stable, connector-assigned MIME type string. File extensions are resolved to MediaType once at the connector edge (by `IMimeTypeResolver`) and never re-examined downstream. This keeps parsers portable across connectors and operating systems.

Resolution belongs at the edge: after `AssetDescriptor` is created with its `MediaType` field, no downstream component re-derives the type from the file name.

### Principle 3 — Documents are immutable

`Document` is an immutable value object. Any transformation — enrichment, normalization, summarization — creates a new `Document` instance rather than mutating the existing one. This keeps the entire pipeline deterministic and makes every stage independently testable.

### Principle 4 — Provenance is preserved across every stage

Every stage in the pipeline records where its input came from:

```
IConnector → AssetDescriptor (ConnectorId, InstanceId)
    ↓
Document (SourceAssetId, ConnectorId, InstanceId)
    ↓
Keyword Index (DocumentId → AssetId → ConnectorId)
    ↓
Knowledge (future) → Context (future) → AI response (future)
```

This traceability answers: "Which file produced this context?" and enables future analytics, audit, and explainability features.

### Principle 5 — Parsing and indexing are separate concerns

`IContentParser` produces `Document`. `IIndexEngine` consumes `Document`. No parser writes to the index; no index engine calls a parser. `IIndexPipeline` orchestrates the connection between them. This separation allows parsers and index engines to be replaced independently.

### Principle 6 — Streaming is the default processing model

Asset discovery is `IAsyncEnumerable<AssetDescriptor>`. The indexing pipeline processes one asset at a time through the parse → index path. Memory usage is O(batch), not O(corpus). Large repositories are supported without buffering all assets.

### Principle 7 — All failure modes are explicit outcomes

`IParserDispatcher.DispatchAsync` returns `ParseResult<Document>` — never throws. Failure modes (Unsupported, Empty, Failed) are explicit values. The pipeline continues after per-asset failures and reports them as Failures in `IndexResult`. Only pipeline-level failures (e.g. cannot open the index file) propagate as exceptions.

### Principle 8 — Pipeline orchestration belongs to IIndexPipeline

CLI handlers call `IIndexPipeline.RunAsync(options, ct)` and receive an `IndexResult`. They never call `IIndexEngine` or `IParserDispatcher` directly. This keeps CLI handlers as thin presentation layers and allows the pipeline to be invoked from any host (CLI, background service, API, test harness).

### Principle 9 — Only ConnectorManager creates and disposes connector runtime instances

No other subsystem constructs connectors directly. Pipelines receive `ConnectorRuntime` objects from the manager — they never call `IConnectorFactory.Create` themselves. This centralizes lifecycle, caching, and resource management (connections, sessions, health checks) in a single place.

---

## Consequences

**Positive:**
- New connectors automatically provide indexable content through `AssetDescriptor.MediaType`
- New parsers add capability without touching existing code
- Pipeline stages are independently testable with fakes
- Progress reporting via events decouples the CLI from pipeline internals
- Provenance chain enables future analytics, audit, and context assembly

**Negative:**
- MediaType-based dispatch requires `IMimeTypeResolver` to be accurate
- Parsers must not assume file extension — they work with streams and MediaType only
- A connector that omits `MediaType` will result in `Unsupported` skips at parse time

---

## Reserved Extension Points

| Interface | Purpose | Target Sprint |
|---|---|---|
| `IContentNormalizer` | Post-parse normalization (Unicode, line endings, whitespace) | Sprint 10+ |
| `IAssetEnricher` | Enrich AssetDescriptor with additional metadata after discovery | Sprint 10 |
| `DocumentVersion` | Historical document versioning for analytics | V2 |
| `IChangeSource` | Change detection for incremental indexing | Sprint 10 |
| `ConnectorPolicy` | Future read-only, bandwidth-limit, max-asset-size, security constraints attached to a `ConnectorInstance` | Sprint 10+ |
| `ConnectorProfile` | Credential sharing across multiple instances of the same connector type | Sprint 10+ |
| `ferret connector doctor` | Health, permissions, credentials, connectivity, and performance checks (documented but not implemented in Sprint 9) | Sprint 10+ |

---

## Traceability

| Decision | Governed By |
|---|---|
| `AssetDescriptor` canonical discovery model | ARCH-019, ADR-0013 |
| `Document` canonical parsing model | This ADR |
| Parsers dispatch by MediaType | Principle 2 |
| Immutability | Principle 3 |
| Provenance chain | Principle 4 |
| Streaming pipeline | Principle 6 (extends ARCH-019 §2 Goal 4) |
| `IIndexPipeline` orchestration boundary | Principle 8 |
