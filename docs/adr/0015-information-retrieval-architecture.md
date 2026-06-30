# ADR-0015: Information Retrieval Architecture

**Status:** Accepted
**Date:** 2026-06-28
**Sprint:** 10

## Context

Sprint 10 delivers the Information Retrieval Platform — BM25/FTS5 keyword search against the index Sprint 9 builds. The architecture must support multiple search providers without CLI or contract changes, a canonical query language independent of any backend syntax, connector-agnostic result identities, and a rendering pipeline that can target console, JSON, HTML, MCP, and REST.

## Decision

### 1. Canonical Query AST

All search queries are represented as a `SearchExpression` AST in `Ferret.Core.Search`. Sprint 10 emits `KeywordExpression`, `PhraseExpression`, `PrefixExpression`, and `AndExpression`. `OrExpression`, `NotExpression`, and `GroupExpression` are reserved.

> The query parser never generates SQLite syntax. It produces only the canonical `SearchQuery` AST. Provider-specific translation belongs entirely to the search provider.

### 2. Provider Abstraction

`ISearchProvider` is the single interface all providers implement. `SearchService` injects `IEnumerable<ISearchProvider>` — no registry in Sprint 10. `ISearchPostProcessor` is a formal interface injected as `IEnumerable<ISearchPostProcessor>`. Sprint 10 registers one post-processor. Future implementations (AI reranking, deduplication) are added to DI without touching providers or `SearchService`.

### 3. Canonical Result Identities

- `DocumentId` — durable document identity, derived from `AssetId`
- `ConnectorInstanceId` — disambiguates connectors indexing different roots
- `CanonicalUri` — universal locator (`filesystem:///src/...`, `jira://ENG-1234`, `git://main/...`)

Renderers derive human-friendly display labels from `CanonicalUri`. The search platform never hardcodes filesystem paths.

> Search identities are always canonical. Presentation labels are always renderer-specific.

### 4. Semantic Highlight Model

Providers convert backend snippet output to `HighlightedText` — a `IReadOnlyList<TextSpan>` tagged `Normal` or `Match`. The `HighlightEngine` is internal to `Ferret.Search`.

> Providers produce semantic highlights. Renderers produce visual highlights.

### 5. Status-Based Service API

`SearchServiceResult` carries a typed `SearchServiceStatus`. Expected conditions (`WorkspaceNotFound`, `IndexNotFound`) are status codes. Exceptions are reserved for genuine runtime failures.

### 6. Layered Presentation

```
ISearchProvider → SearchResult (backend-neutral)
SearchService   → SearchServiceResult (with status, execution info)
Handler         → SearchViewModel (presentation model in Ferret.Cli)
Formatter       → ICommandResultFormatter<SearchViewModel>
Styler          → ITextStyler (ANSI today; Spectre-ready tomorrow)
```

> Presentation models belong to the consuming presentation layer until a second independent presentation consumer exists.

### 7. SearchSessionId

Every search execution carries `Guid SessionId` in `SearchExecutionInfo` for telemetry, distributed tracing, and dashboard history.

### 8. Explanation Field (reserved)

`SearchHit.Explanation` is `string?`, defaulting to `null` in Sprint 10. Sprint 11+ populates it with per-provider score breakdown.

## Consequences

- Every provider implements `ISearchProvider` — no provider-specific CLI code
- Adding Sprint 11 semantic search: register `SemanticSearchProvider` in DI — zero changes elsewhere
- Adding Sprint 12 hybrid: register `HybridSearchProvider` and a post-processor — zero changes elsewhere

## Future Evolution

Sprint 10: BM25SearchProvider → Sprint 11: SemanticSearchProvider → Sprint 12: HybridSearchProvider → Sprint 13: KnowledgeSearchProvider → V2: ContextOS retrieval layer
