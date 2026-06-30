# Why Context Assembly?

Context Assembly is the pipeline that transforms search results into a `ContextPackage` suitable for an AI prompt. It is not search. It is a separate, ordered pipeline.

## Search results ≠ context

When an AI assistant asks Ferret for context about "how does file watching work?", a raw keyword search returns the top-10 BM25 matches. That is not yet context. Context requires:

1. **Deduplication** — two results from the same file should not both appear
2. **Expansion** — a function definition is more useful with its callers
3. **Content filtering** — binary files, generated code, and test fixtures are usually noise
4. **Token budgeting** — the context must fit within the AI model's context window

Without this pipeline, every AI-assisted query would either overflow the context window (too much) or return meaningless fragments (too little).

## Why a pipeline, not a function

The stages are independently composable, testable, and replaceable. A user can configure a `ContentFilter` to exclude `*.generated.cs`. A team can add a custom `Expander` that includes related ADRs. The pipeline model makes this extensible without touching the search layer.

Each stage in `Ferret.Core.Context` is an `IContextStage` with a single method: `ProcessAsync(ContextPackage, CancellationToken)`. The pipeline runs them in order.

## What it costs

Context Assembly adds latency. For most queries on a 1,000-document workspace, the pipeline runs in under 100ms. For large workspaces with aggressive expansion, it can take several hundred milliseconds. The token budget stage short-circuits early when the limit is reached.

## Related

- [Context Assembly Architecture](../architecture/context-assembly) — the pipeline diagram
- [MCP Reference](../reference/mcp) — how `ferret_context` triggers the pipeline
