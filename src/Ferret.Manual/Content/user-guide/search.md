# Search

Ferret uses BM25 full-text search backed by SQLite FTS5. Search is fast, offline, and does not require an AI provider or internet connection.

## Basic Search

```bash
ferret search "IIndexPipeline"
```

Results are ranked by BM25 score. Higher score = more relevant.

## Sample Output

```
Results for "IIndexPipeline" (3 found, 45ms)

1. src/Ferret.Core/Indexing/IIndexPipeline.cs         score: 0.94
   Orchestrates a complete discover → parse → index pipeline run.

2. src/Ferret.Indexing/IndexPipeline.cs               score: 0.87
   public sealed class IndexPipeline : IIndexPipeline

3. tests/Ferret.Indexing.Tests/IndexPipelineTests.cs  score: 0.71
   [Fact] public async Task RunAsync_Returns_Correct_Counts()
```

## Flags

| Flag | Default | Description |
|---|---|---|
| `--top N` | `10` | Return at most N results |
| `--json` | off | Output as JSON array |
| `--no-highlight` | off | Disable snippet highlights |

## Query Syntax

| Syntax | Example | Matches |
|---|---|---|
| Keyword | `IIndexPipeline` | Documents containing the term |
| Phrase | `"index pipeline"` | Documents containing the exact phrase |
| Prefix | `IIndex*` | Documents where a term starts with `IIndex` |
| Multiple keywords | `index pipeline` | Documents containing both terms |

## JSON Output

For scripting, use `--json`:

```bash
ferret search "IIndexPipeline" --json
```

```json
[
  {
    "rank": 1,
    "displayName": "src/Ferret.Core/Indexing/IIndexPipeline.cs",
    "uri": "filesystem:///src/Ferret.Core/Indexing/IIndexPipeline.cs",
    "score": 0.94,
    "snippet": "Orchestrates a complete discover → parse → index pipeline run."
  }
]
```

## Search from Claude

When Ferret is running as an MCP server, Claude calls `ferret_search` automatically:

> "Search the codebase for how the index pipeline is orchestrated"

Claude will use `ferret_search` and present the results with grounded file references.

## Performance

BM25 search on a 1,000-document workspace typically returns in under 50ms. On a 10,000-document workspace, under 200ms. Results are served directly from the SQLite FTS5 index — no network call required.

## Related

- [MCP Reference](../reference/mcp) — `ferret_search` tool schema
- [Search Flow Architecture](../architecture/search-flow) — how the query pipeline works
- [Context](context) — assembling AI-ready context from search results
