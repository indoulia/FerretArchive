# Sprint 10 Design Specification: Information Retrieval Platform

**Project:** Ferret (ContextOS)
**Date:** 2026-06-28
**Status:** Authoritative
**Sprint tag:** `v0.10.0-sprint10`

---

## 1. Overview

### Sprint Objective

Deliver the Information Retrieval Platform. A user can initialize a workspace, run `ferret index`, and then run `ferret search <query>` to retrieve BM25-ranked results with highlighted snippets from the FTS5 index Sprint 9 builds. The architecture is explicitly designed for semantic search in Sprint 11 with zero redesign required.

### End-to-End Success Criteria

All of the following work without error:

```bash
ferret search RuntimeBuilder
ferret search "runtime builder"
ferret search runtime*
ferret search authentication --passages
ferret search authentication --limit 5
ferret search authentication --no-highlight
```

And gracefully handle expected environmental states:

```bash
ferret search authentication   # outside workspace → friendly message
ferret search authentication   # before indexing   → friendly message
```

Additional gates:
- All prior tests remain green
- `git tag v0.10.0-sprint10` applied
- ADR-0015 committed before any implementation

### What a New User Can Do After Sprint 10

Run `ferret search <query>` to retrieve BM25-ranked, highlighted results from an indexed workspace. Output is file-centric by default; `--passages` switches to passage-level view.

### Out of Scope

- Semantic search, embeddings, vector databases (Sprint 11)
- Hybrid search provider (Sprint 11+)
- Incremental index updates
- `--path`, `--type`, `--connector`, `--provider`, `--semantic` filters (Sprint 11+)
- `--format json` fully implemented (reserved slot, Sprint 11+)
- Spectre.Console (deferred to a dedicated CLI UX sprint)

---

## 2. Architectural Principles

Four principles govern all Sprint 10 decisions and are documented in ADR-0015:

1. **Search identities are always canonical. Presentation labels are always renderer-specific.**
   `SearchHit` carries `DocumentId` and `CanonicalUri`; renderers derive display labels (file paths, issue IDs, page titles) from those.

2. **Providers produce semantic highlights; renderers produce visual highlights.**
   `BM25SearchProvider` converts FTS5 snippet output to `HighlightedText` (a span model). Renderers apply ANSI, HTML, or JSON formatting to spans — they never know FTS5 existed.

3. **The query parser never generates SQLite syntax.**
   `QueryParser` produces a `SearchQuery` AST. `BM25SearchProvider` translates the AST to FTS5 syntax. SQLite is not a public contract.

4. **Presentation is layered.**
   Search providers → backend-neutral models. Handlers → presentation models. Formatters → render. Stylers → terminal formatting. No presentation layer knows BM25, SQLite, or FTS5.

5. **Presentation models belong to the consuming layer until a second independent consumer exists.**
   `SearchViewModel` lives in `Ferret.Cli`. Extracted when a second consumer (REST, MCP) materialises.

---

## 3. Section 1 — Search Contracts (`Ferret.Core.Search`)

### Package rule

All public contracts live in `Ferret.Core`, namespace `Ferret.Core.Search`. No implementation. Zero new dependencies. Follows M1 pattern.

### Query model

```
SearchQuery
    string OriginalText          ← preserved for telemetry, history, "did you mean?"
    SearchExpression Root        ← immutable AST
```

### Query AST (`SearchExpression` hierarchy)

All node types live in `Ferret.Core.Search`. This is a platform contract, not a BM25 implementation detail.

```
SearchExpression (abstract)
    KeywordExpression(string Value)
    PhraseExpression(string Value)
    PrefixExpression(string Prefix)
    AndExpression(IReadOnlyList<SearchExpression> Operands)   ← Sprint 10 implicit AND
    OrExpression(...)                                          ← reserved, not emitted
    NotExpression(...)                                         ← reserved, not emitted
    GroupExpression(...)                                       ← reserved, not emitted
```

### Parse result

The parser never throws for user input; exceptions are reserved for genuine runtime failures.

```
SearchParseResult
    bool IsSuccess
    SearchQuery? Query
    IReadOnlyList<SearchDiagnostic> Diagnostics
```

### Search options

```
SearchOptions
    int MaxResults              ← default 10
    bool IncludePassages        ← drives --passages
    bool HighlightEnabled       ← drives --no-highlight
    int SnippetLength           ← characters, default 160
    ExecutionMode Mode          ← Auto | Keyword | Semantic | Hybrid (Sprint 10 always Keyword)
    CancellationToken Token
```

### Result model

```
SearchResult
    IReadOnlyList<SearchHit> Hits
    int TotalHits
    int ReturnedHits
```

```
SearchHit (abstract)
    DocumentId DocumentId
    ConnectorInstanceId ConnectorInstanceId
    string CanonicalUri
    string DisplayName           ← renderer-derived label
    SearchHitKind Kind
    float Score                  ← BM25 score; reserved for vector similarity, hybrid ranking
    HighlightedText Snippet

FileSearchHit : SearchHit
    (no additional fields for Sprint 10)

PassageSearchHit : SearchHit
    string? Heading
    int StartOffset
    int EndOffset

SegmentSearchHit : SearchHit   ← reserved, Sprint 11

All SearchHit subtypes carry:
    string? Explanation = null   ← reserved; Sprint 11+ populates with per-provider score breakdown
                                 ← e.g. "BM25: 0.91 | Semantic: 0.84 | Hybrid: 0.89"
```

```
SearchHitKind
    File
    Passage
    Segment     ← reserved
```

### Highlight model

```
HighlightedText
    IReadOnlyList<TextSpan> Spans

TextSpan
    string Text
    TextSpanKind Kind

TextSpanKind
    Normal
    Match
    Deleted      ← reserved
    Inserted     ← reserved
    Warning      ← reserved
    AIReference  ← reserved
```

### Provider contracts

```
IQueryParser
    SearchParseResult Parse(string rawQuery)

ISearchProvider
    SearchProviderDescriptor Descriptor
    SearchCapabilities Capabilities
    Task<SearchResult> SearchAsync(SearchQuery query, SearchOptions options, CancellationToken ct)

ISearchService
    Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
    Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options)

SearchServiceResult
    SearchQuery Query
    SearchResult? Result
    SearchServiceStatus Status
    SearchProviderDescriptor? ProviderDescriptor
    SearchExecutionInfo ExecutionInfo
    IReadOnlyList<SearchDiagnostic> Diagnostics

SearchServiceStatus
    Success
    WorkspaceNotFound
    IndexNotFound
    ProviderUnavailable
    InvalidQuery

SearchExecutionInfo
    Guid SessionId           ← unique per search execution; telemetry, tracing, dashboard history
    string ProviderName
    TimeSpan Duration
    int DocumentsScanned
    string IndexVersion

SearchProviderDescriptor
    string Id
    string DisplayName
    string Version
    SearchCapabilities Capabilities

SearchCapabilities
    bool SupportsKeyword
    bool SupportsPhrase
    bool SupportsPrefix
    bool SupportsSemantic    ← reserved
    bool SupportsHybrid      ← reserved
```

### Exit criteria

- All contracts compile with zero warnings
- Unit tests: value object equality, `SearchHitKind` exhaustiveness, `TextSpanKind` exhaustiveness, `SearchServiceStatus` mapping
- No implementation types

---

## 4. Section 2 — Query Parser

### Package

Implementation in `Ferret.Search`. Interface (`IQueryParser`) in `Ferret.Core.Search`.

### Supported constructs (Sprint 10)

| Input | AST |
|---|---|
| `authentication token` | `And(Keyword(authentication), Keyword(token))` |
| `"runtime builder"` | `Phrase(runtime builder)` |
| `auth*` | `Prefix(auth)` |
| `"context window" token` | `And(Phrase(context window), Keyword(token))` |
| `auth* token` | `And(Prefix(auth), Keyword(token))` |

Boolean operators, grouping, and advanced filters are reserved for a future sprint.

### Parser stages

```
Input string
    ↓
Lexer → Token[]
    ↓
Parser → SearchExpression (AST)
    ↓
Validator
    ↓
SearchParseResult
```

The lexer and parser are internal to `Ferret.Search`. Only `IQueryParser` and `SearchParseResult` are public.

### ADR-0015 rule

> The query parser never generates SQLite syntax. It produces only the canonical `SearchQuery` AST. Provider-specific translation belongs entirely to the search provider.

### Exit criteria

- Deterministic AST for all three constructs
- `SearchParseResult.IsSuccess == false` for empty queries and malformed input
- `Diagnostics` populated on failure
- No SQLite or FTS5 references anywhere in the parser
- 100% unit test coverage of all input combinations

---

## 5. Section 3 — Search Platform (`Ferret.Search`)

### Package

New project: `Ferret.Search`. References `Ferret.Core`. Referenced by `Ferret.Cli`.

### Internal pipeline

```
SearchService.SearchAsync(string rawQuery, SearchOptions)
    ↓
QueryParser.Parse(rawQuery)         → SearchParseResult
    ↓
QueryValidator.Validate(query)
    ↓
ProviderSelector.Select(options)    → ISearchProvider
    ↓
BM25SearchProvider.SearchAsync(query, options)
    ↓
QueryTranslator (AST → FTS5 string)
    ↓
SQLite FTS5
    ↓
SnippetGenerator (internal)
    ↓
HighlightEngine (internal)          → HighlightedText
    ↓
SearchResult
    ↓
ISearchPostProcessor   ← interface; Sprint 10 has one impl (score normalization + metadata assembly)
    ↓                     future impls: deduplication, AI reranking, diversity, score blending
SearchServiceResult
```

### Provider registration

`IEnumerable<ISearchProvider>` is injected into `SearchService`. No registry or factory in Sprint 10. DI registers `BM25SearchProvider` directly. Additional providers (Semantic, Hybrid) are added to DI in later sprints — `SearchService` never changes.

`ProviderSelector` is an internal `SearchService` concern: it inspects `SearchOptions.ExecutionMode` and `ISearchProvider.Capabilities` to pick the appropriate provider. Sprint 10 always selects `BM25SearchProvider` (only `Keyword` mode supported). The selector is not exposed as a public interface.

### `ISearchPostProcessor`

Formal interface, not an internal class. `SearchService` injects `IEnumerable<ISearchPostProcessor>` and runs them in sequence after the provider returns results. Sprint 10 registers one implementation: `DefaultSearchPostProcessor` (score normalization, execution metadata assembly). Future implementations (deduplication, AI reranking, diversity) are added to DI without touching `SearchService` or providers.

### `BM25SearchProvider` responsibilities

- Translate `SearchExpression` AST to FTS5 query string via `QueryTranslator`
- Execute FTS5 query against `keyword-index.db` (path from workspace config)
- Use SQLite `snippet()` function to obtain raw marked text + offsets
- Convert to `HighlightedText` via internal `HighlightEngine`
- Return `IReadOnlyList<SearchHit>` ranked by BM25 score
- Return typed status (`IndexNotFound`, `WorkspaceNotFound`) rather than throwing for expected states

### `HighlightEngine` (internal)

Converts FTS5 `snippet()` output to `HighlightedText`. Never exposed outside `Ferret.Search`. The renderer has no knowledge of FTS5 markup.

### `SemanticSearchProvider` stub

Registered as a no-op placeholder implementing `ISearchProvider`. `Capabilities.SupportsSemantic == true` but `SearchAsync` returns `ProviderUnavailable`. Reserved for Sprint 11.

### `HybridSearchProvider`

Not implemented. Reserved for Sprint 12.

### Exit criteria

- `ferret search authentication` returns ranked `FileSearchHit` results from a real FTS5 index
- `ferret search "runtime builder"` returns phrase-matched results
- `ferret search auth*` returns prefix-matched results
- `SearchServiceResult.Status == WorkspaceNotFound` when no `.ferret/` exists
- `SearchServiceResult.Status == IndexNotFound` when index file is absent
- `HighlightEngine` unit tested independently of SQLite
- `QueryTranslator` unit tested: AST → expected FTS5 string for all three constructs

---

## 6. Section 4 — Rendering & Presentation

### Package

All view models and renderers live in `Ferret.Cli`. Extracted to a shared package only when a second independent consumer (REST, MCP) exists.

### Mapping chain

```
SearchServiceResult
    ↓
SearchViewModel (Ferret.Cli)
    ↓
SearchRendererSelector
    ↓ (based on SearchOptions)
FileRenderer          ← default
PassageRenderer       ← --passages
JsonRenderer          ← --format json (reserved, returns "not yet available")
    ↓
ITextStyler
    ↓
Console
```

### View models

```
SearchViewModel
    string OriginalQuery
    string ProviderName
    TimeSpan Duration
    string IndexVersion
    int TotalHits
    IReadOnlyList<SearchHitViewModel> Hits

SearchHitViewModel
    string DisplayPath     ← derived from CanonicalUri by renderer
    float Score
    SearchHitKind Kind
    HighlightedText Snippet
    string? Heading        ← PassageSearchHit only
```

### `ITextStyler`

```
ITextStyler
    string Normal(string text)
    string Highlight(string text)
```

`AnsiTextStyler` implements this with ANSI escape codes derived from `TextSpan.Kind`. `SpectreConsoleStyler` added in a future CLI UX sprint — no renderer changes required.

### Renderer pattern

Each renderer implements `ICommandResultFormatter<SearchViewModel>`, consistent with Sprint 8. The handler calls `SearchRendererSelector.Select(options).Format(viewModel, writer)`. No `if` statements in the handler.

### Console output format (FileRenderer)

```
Searching...

Provider: BM25 | Query: "runtime builder" | 14 results | 27 ms

src/Ferret.Runtime/RuntimeBuilder.cs                      [0.94]
  ...the IHostedService integration with the Runtime Builder
  component during startup...

src/Ferret.Cli/Program.cs                                 [0.71]
  ...RuntimeBuilder.Configure() registers all CLI modules...
```

### ADR-0015 principle

> Presentation models belong to the consuming presentation layer until a second independent presentation consumer exists.

### Exit criteria

- `FileRenderer` produces correct ANSI-highlighted output for all three query types
- `PassageRenderer` produces passage-level output with heading when available
- `JsonRenderer` prints a clear "not yet available" message and exits with code 2
- `AnsiTextStyler` unit tested: `Normal` returns plain text, `Highlight` wraps in ANSI codes
- `SearchRendererSelector` unit tested: routes based on `SearchOptions`

---

## 7. Section 5 — CLI & Wire-up

### Command surface

```bash
ferret search <query> [options]

Options:
  --passages          Show passage-level results instead of file-level
  --limit <n>         Maximum results to return (default: 10)
  --no-highlight      Disable ANSI highlighting (useful for piped output)
  --format <format>   Output format: text (default) | json (reserved)
```

All other options (`--path`, `--type`, `--connector`, `--provider`, `--semantic`) deferred to Sprint 11+.

### `SearchCommandHandler`

```
SearchCommandHandler
    ISearchService SearchService
    SearchRendererSelector RendererSelector

ExecuteAsync(query, options, writer):
    result = await SearchService.SearchAsync(query, options)
    switch result.Status:
        WorkspaceNotFound → print "No workspace found. Run `ferret workspace init` first."
        IndexNotFound     → print "No index found. Run `ferret index` first."
        InvalidQuery      → print diagnostics
        Success           → map to SearchViewModel, render
```

No filesystem inspection in the handler. Status codes from `SearchServiceResult` drive all branching.

### `SearchCliModule`

Registers `SearchCommandHandler` and `SearchCliModule` in DI. Contributes `ferret search` command group to `RootCommandFactory`. Follows `ConnectorCliModule` pattern exactly.

### DI registrations

```csharp
services.AddSingleton<IQueryParser, QueryParser>();
services.AddSingleton<ISearchProvider, BM25SearchProvider>();
services.AddSingleton<ISearchProvider, SemanticSearchProvider>(); // stub
services.AddSingleton<ISearchService, SearchService>();
services.AddSingleton<ITextStyler, AnsiTextStyler>();
services.AddSingleton<SearchRendererSelector>();
```

### ADR-0015 (written first)

ADR-0015 is the first commit of Sprint 10, before any code. It documents:
- Search AST and query language
- Provider abstraction and `ISearchProvider` contract
- Rendering pipeline and `ITextStyler` abstraction
- `SearchServiceResult` status model
- Future hybrid search design

### Integration tests

| Scenario | Expected |
|---|---|
| `ferret search RuntimeBuilder` (indexed workspace) | Ranked file results |
| `ferret search "runtime builder"` (phrase) | Phrase-matched results |
| `ferret search runtime*` (prefix) | Prefix-matched results |
| `ferret search auth --passages` | Passage-level results |
| `ferret search auth --limit 3` | At most 3 results |
| `ferret search auth --no-highlight` | Plain text, no ANSI codes |
| `ferret search auth` (no workspace) | Friendly message, exit 1 |
| `ferret search auth` (no index) | Friendly message, exit 1 |
| `ferret search auth --format json` | "not yet available", exit 2 |
| `ferret search` (empty query) | Parse diagnostic, exit 1 |

### Sprint tag

`git tag v0.10.0-sprint10` applied after all tests green.

---

## 8. Testing Strategy

| Section | Coverage |
|---|---|
| Contracts | Value object equality, `SearchHitKind` exhaustiveness, `SearchServiceStatus` mapping, `TextSpanKind` |
| Query Parser | All three constructs → deterministic AST; empty/malformed input → `SearchParseResult.IsSuccess == false`; `Diagnostics` populated |
| Search Platform | `QueryTranslator` AST→FTS5; `HighlightEngine` span model; BM25 ranking; `WorkspaceNotFound`/`IndexNotFound` status codes; provider injection |
| Rendering | `AnsiTextStyler`; renderer selector routing; `FileRenderer` snippet formatting; `PassageRenderer` heading; `JsonRenderer` reserved message |
| CLI | Full integration test matrix above |

---

## 9. Roadmap Position

```
Sprint 8  — Connector Platform    (Discover)
Sprint 9  — Indexing Pipeline     (Ingest)
Sprint 10 — Information Retrieval (Retrieve)  ← this sprint
Sprint 11 — Semantic Search       (Understand)
Sprint 12 — Context Assembly      (Assemble)
V1        — ContextOS retrieval layer
```

Each sprint adds a platform layer. No sprint replaces a previous one.

---

## 10. ADR Reference

**ADR-0015** — Information Retrieval Architecture  
Path: `docs/adr/0015-information-retrieval-architecture.md`  
Written: before Sprint 10 implementation begins  
Documents: Search AST, provider abstraction, rendering pipeline, `ITextStyler` abstraction, future hybrid-search design
