# Context Assembly

Context Assembly transforms raw search results into a `ContextPackage` ready for an AI prompt. It is a six-stage pipeline, not a single function call.

## Pipeline Diagram

```
ferret_context(query, token_budget)
      │
      ▼
┌─────────────────────────────┐
│ Stage 1: Search             │
│ ISearchService.SearchAsync  │
│ Returns top-N BM25 hits     │
└──────────────┬──────────────┘
               │ SearchHit[]
               ▼
┌─────────────────────────────┐
│ Stage 2: Deduplicate        │
│ Remove hits from same file  │
│ Keep highest-scoring hit    │
└──────────────┬──────────────┘
               │ deduplicated hits
               ▼
┌─────────────────────────────┐
│ Stage 3: Filter             │
│ Drop binary, generated,     │
│ and configured-exclude files│
└──────────────┬──────────────┘
               │ filtered hits
               ▼
┌─────────────────────────────┐
│ Stage 4: Expand             │
│ Add caller/callee context   │
│ Add related documents       │
└──────────────┬──────────────┘
               │ expanded document set
               ▼
┌─────────────────────────────┐
│ Stage 5: Token Budget       │
│ Measure each document       │
│ Drop until budget satisfied │
│ Shortest documents first    │
└──────────────┬──────────────┘
               │ budget-constrained set
               ▼
┌─────────────────────────────┐
│ Stage 6: Format             │
│ Build ContextPackage        │
│ XML envelope with metadata  │
└──────────────┬──────────────┘
               │ ContextPackage
               ▼
         AI assistant
```

## IContextStage

Each stage implements a single interface:

```csharp
public interface IContextStage
{
    Task ProcessAsync(ContextPackage package, CancellationToken ct);
}
```

Stages mutate the `ContextPackage` in place. The pipeline runs them in registration order.

## Token Budget

The token budget stage uses a character-count approximation (1 token ≈ 4 characters) to stay within the model's context window. The default budget is 8,000 tokens. Configure via:

```json
{
  "context": {
    "defaultTokenBudget": 8000
  }
}
```

## Related

- [Why Context Assembly?](../design/why-context-assembly) — the design rationale
- [MCP Reference](../reference/mcp) — `ferret_context` tool schema
- [Search Flow](search-flow) — the search stage that feeds context assembly
