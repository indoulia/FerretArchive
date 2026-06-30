# First Search

Search your indexed workspace:

```bash
ferret search "IIndexPipeline"
```

## Sample output

```
Results for "IIndexPipeline" (4 found, 120ms)

1. src/Ferret.Core/Indexing/IIndexPipeline.cs           score: 0.94
   Orchestrates a complete discover → parse → index pipeline run.

2. src/Ferret.Indexing/IndexPipeline.cs                 score: 0.87
   public sealed class IndexPipeline : IIndexPipeline

3. tests/Ferret.Indexing.Tests/IndexPipelineTests.cs    score: 0.71
   [Fact] public async Task RunAsync_Returns_Correct_Counts()
```

## Useful flags

| Flag | Effect |
|---|---|
| `--top N` | Return at most N results (default: 10) |
| `--json` | Output as JSON (for scripting) |

## Related

- [Connect Claude](connect-claude) — use Ferret from within Claude
- [Search](../user-guide/search) — ranking, advanced queries
- [MCP Reference](../reference/mcp) — `ferret_search` MCP tool
