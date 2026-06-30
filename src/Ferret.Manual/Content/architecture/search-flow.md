# Search Flow

A `ferret search` query or a `ferret_search` MCP call passes through four layers before results are returned. Each layer has a single responsibility.

## Flow Diagram

```
User / AI assistant
      │
      │  "IIndexPipeline"
      ▼
┌─────────────────┐
│  SearchCommand  │  CLI handler (Ferret.Cli)
│  or SearchTool  │  MCP tool (Ferret.Mcp)
└────────┬────────┘
         │  SearchQuery AST
         ▼
┌─────────────────┐
│  SearchService  │  Ferret.Search
│                 │  injects IEnumerable<ISearchProvider>
└────────┬────────┘
         │  provider.SearchAsync(query, options)
         ▼
┌─────────────────┐
│  BM25Search     │  Ferret.Search
│  Provider       │  translates AST → SQLite FTS5 query
└────────┬────────┘
         │  SQL: SELECT ... FROM documents_fts WHERE documents_fts MATCH ?
         ▼
┌─────────────────┐
│  SQLite FTS5    │  .ferret/indexes/keyword/keyword-index.db
│  Index          │  BM25 ranking built in
└────────┬────────┘
         │  ranked rows
         ▼
┌─────────────────┐
│  Post-          │  ISearchPostProcessor (Ferret.Search)
│  Processors     │  deduplication, score normalization
└────────┬────────┘
         │  SearchServiceResult
         ▼
┌─────────────────┐
│  Formatter /    │  SearchViewModel → console/JSON/MCP text
│  MCP Renderer   │
└─────────────────┘
```

## Query AST

The query parser converts a string like `"IIndexPipeline"` into a canonical `SearchQuery` AST:

```csharp
// Single keyword
new SearchQuery(new KeywordExpression("IIndexPipeline"))

// Phrase
new SearchQuery(new PhraseExpression("index pipeline"))

// Prefix
new SearchQuery(new PrefixExpression("IIndex"))
```

The AST is provider-agnostic. The BM25 provider translates it to FTS5 syntax. A future semantic provider would translate it to a vector query.

## Score Normalization

BM25 scores are raw FTS5 scores (negative floats, more negative = lower rank). The post-processor normalizes them to `[0.0, 1.0]` before returning results to the caller.

## Related

- [Storage](storage) — the FTS5 schema
- [Why BM25 Before Vectors?](../design/why-bm25) — why keyword search ships first
- [MCP Reference](../reference/mcp) — `ferret_search` tool schema
