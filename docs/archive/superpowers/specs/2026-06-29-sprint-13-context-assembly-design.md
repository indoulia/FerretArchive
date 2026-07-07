# Sprint 13 Design Specification: Context Assembly Platform

**Project:** Ferret
**Date:** 2026-06-29
**Status:** Authoritative
**Sprint tag:** `v0.13.0-sprint13`

---

## Executive Summary

Sprint 13 delivers the Context Assembly Platform — the first AI-facing capability that Ferret's users actually see. It transforms Ferret from a search tool into a context engine: a single call to `ferret_context` assembles a complete, deduplicated, token-budgeted context package from the knowledge base and returns it ready for AI consumption.

Without Sprint 13, Claude calls `search()` three times and `read_document()` five times to gather context for a query. It receives raw search hits, performs its own deduplication, manages its own token budget, and stitches together a prompt manually. Answer quality is limited by what fits in however many tool calls the conversation allows.

With Sprint 13, Claude calls `ferret_context(query)` once. Ferret runs keyword search, deduplicates results by document identity, expands hits to full documents, applies a token budget in score order, and returns a formatted context string ready for prompt injection. The pipeline is: **Query → Search → Deduplication → Document Expansion → Content Filtering → Token Budgeting → Context Package**.

The sprint fills `Ferret.AI` — scaffolded empty in Sprint 12 — with the assembly engine. It adds `ferret_context` to the MCP tool catalog and `ferret context` to the CLI. No model is called at runtime; token counting is a character-based approximation.

---

## Architectural Outcomes

1. **Established the Context Contracts** — `ContextRequest`, `ContextDocument`, `ContextDocumentSource`, `ContextPackage`, `IContextAssembler` as Ferret-owned contracts in `Ferret.Core.Context`
2. **Delivered the Context Assembly Engine** — `ContextAssembler`, `DocumentExpander`, `ContextDeduplicator`, `ContentFilter`, `TokenEstimator` fully implemented in `Ferret.AI`
3. **Extended the MCP Tool Catalog** — `ContextTool` registered as `ferret_context`; one tool call replaces multiple search + read round-trips
4. **Extended the CLI** — `ferret context <query>` command for local testing and verification of context assembly
5. **Activated `Ferret.AI`** — the orchestration package becomes substantive; `AiModule` registers the assembly engine into DI
6. **Established the Token Budget Contract** — character-based approximation (`text.Length / 4`) is the Sprint 13–14 standard; vector-aware token counting arrives in Sprint 15

---

## Section 1: Sprint Identity

### 1.1 Sprint Name and Tag

**Name:** Sprint 13 – Context Assembly Platform
**Tag:** `v0.13.0-sprint13`

### 1.2 Theme

> Transform Ferret from a search tool into a context engine: one call returns a complete, AI-ready knowledge package.

### 1.3 Sprint Goal

> Deliver the Context Assembly Platform: pipeline contracts, assembly engine, MCP integration, and CLI command — so that AI consumers receive a complete, deduplicated, token-budgeted context package from a single `ferret_context` tool call.

### 1.4 User Story

A developer opens a Ferret-connected Claude workspace. They observe Claude's tool calls when answering a question about their codebase. Before Sprint 13: Claude calls `ferret_search` three times and `ferret_read_document` five times, managing deduplication and token budget itself. After Sprint 13: Claude calls `ferret_context` once with the query, receives a formatted context block containing the most relevant documents within the token budget, and answers in a single turn. Token usage is lower; answer quality is higher.

### 1.5 What a New User Can Do After Sprint 13

Run `ferret context "how does authentication work"` and see a formatted context package showing the top matching documents, token estimates, and assembled context text — ready to copy into a prompt or deliver via MCP. AI clients connected via MCP call `ferret_context` instead of multiple search and read calls.

### 1.6 Non-Goals

Sprint 13 explicitly does not deliver:

- Semantic or vector search — `ferret_context` uses keyword search only
- LLM calls — no model is invoked; token counting is character-based approximation only
- Streaming context — context is assembled once and returned complete
- Conversation history — memory interfaces remain null implementations (Sprint 15)
- Context caching — context packages are not cached between calls
- REST or HTTP exposure of the context API
- Reranking — document ordering uses raw search scores only
- Multi-query context assembly — one `ferret_context` call, one query string

**Version Gate Rule:** Sprint 13 must not call any AI model at runtime. `dotnet test` must pass with no LLM API calls. Token counting is the character-based approximation `text.Length / 4` only.

---

## Section 2: Architecture

### 2.1 Position in the Platform

```
┌─────────────────────────────────────────────────────────────────────┐
│                  Presentation / Integration Hosts                   │
│   Ferret.Cli        Ferret.Mcp       Future REST    Future Web UI  │
│   [ferret context]  [ferret_context]                               │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
┌────────────────────────────────▼────────────────────────────────────┐
│              Context Assembly Platform (Sprint 13 NEW)              │
│                        Ferret.AI                                    │
│   ContextAssembler    DocumentExpander    ContextDeduplicator       │
│   TokenEstimator                                                    │
└──────────┬─────────────────────────────────┬────────────────────────┘
           │                                 │
┌──────────▼─────────────┐     ┌─────────────▼──────────────────────┐
│   Ferret.Search        │     │   Ferret.Workspace / DocService     │
│   ISearchService       │     │   IDocumentService                  │
└────────────────────────┘     └────────────────────────────────────┘
                                 │
┌────────────────────────────────▼────────────────────────────────────┐
│                   Ferret.Core (zero-dependency contracts)           │
│   Ferret.Core.Context (NEW)   Ferret.Core.Search   Ferret.Core.Ai  │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 New and Modified Packages

| Package | Change | Purpose |
|---|---|---|
| `Ferret.Core` (modified) | Add `Ferret.Core.Context` namespace | Context contracts — `ContextRequest`, `ContextDocument`, `ContextPackage`, `IContextAssembler` |
| `Ferret.AI` (modified — was empty scaffold) | Add assembly engine | `ContextAssembler`, `DocumentExpander`, `ContextDeduplicator`, `TokenEstimator`; `AiModule` updated |
| `Ferret.Mcp` (modified) | Add `ContextTool` | `ferret_context` MCP tool; registered in `McpModule` |
| `Ferret.Cli` (modified) | Add `ContextCliModule` | `ferret context <query>` CLI command |

Test packages added: `Ferret.AI.Tests` (new project — created in sub-plan s2).

### 2.3 Dependency Flow

`Ferret.AI` depends on `Ferret.Core`, `Ferret.Search` (for `ISearchService`), and the document service contract (resolved from `Ferret.Workspace` or equivalent). It has no dependency on `Ferret.Models`, `Ferret.Prompts`, or any provider package. No vendor AI SDK is referenced.

`Ferret.Mcp` already depends on `Ferret.Core`. It acquires a dependency on `Ferret.AI` to access `IContextAssembler`.

`Ferret.Cli` already depends on `Ferret.Core`. It acquires a dependency on `Ferret.AI` to access `IContextAssembler` for the CLI command.

### 2.4 No New SDK Isolation Rules

Sprint 13 introduces no external SDK dependencies. `TokenEstimator` uses only BCL types. The SDK isolation boundary established in Sprint 12 (ADR-0019) is unchanged.

---

## Section 3: Core Context Contracts (`Ferret.Core.Context`)

All types in this section live in the `Ferret.Core` assembly under the `Ferret.Core.Context` namespace. They have zero external NuGet dependencies.

### 3.1 `ContextRequest`

Encapsulates the parameters for a single context assembly operation.

| Property | Type | Default | Notes |
|---|---|---|---|
| `Query` | `string` | required | The natural-language or keyword query to search for |
| `MaxTokens` | `int` | `8000` | Token budget ceiling; assembly stops when this is reached |
| `MaxDocuments` | `int` | `10` | Maximum number of documents to include regardless of token budget |
| `IncludeSections` | `bool` | `true` | Reserved for Sprint 14 section-level assembly; no effect in Sprint 13 |

`ContextRequest` is a record with positional constructor: `ContextRequest(string Query, int MaxTokens = 8000, int MaxDocuments = 10, bool IncludeSections = true)`.

### 3.2 `ContextDocumentSource`

Enum indicating how a `ContextDocument` was sourced:

```
ContextDocumentSource
  FullDocument   // Content is the document's full PlainText
  Section        // Content is a specific document section (reserved for Sprint 14)
```

Sprint 13 assembler always produces `FullDocument`. `Section` is defined now so Sprint 14 does not change the enum's binary identity.

### 3.3 `ContextDocument`

Represents one document in the assembled context package.

| Property | Type | Notes |
|---|---|---|
| `DocumentId` | `DocumentId` | Identity key from the search hit |
| `CanonicalUri` | `Uri` | Canonical URI of the source document |
| `DisplayName` | `string` | Human-readable name (from `SearchHit.DisplayName`) |
| `Title` | `string?` | Document title from `Document.Title`; null if absent |
| `Content` | `string` | Full plain text or section text included in the context |
| `Score` | `float` | Relevance score from the originating search hit |
| `TokenEstimate` | `int` | Estimated token count for `Content`; computed via `TokenEstimator.Estimate(Content)` |
| `Source` | `ContextDocumentSource` | Always `FullDocument` in Sprint 13 |

`ContextDocument` is an immutable record.

### 3.4 `ContextPackage`

The complete output of the context assembly pipeline.

| Property | Type | Notes |
|---|---|---|
| `Query` | `string` | The original query from `ContextRequest` |
| `Documents` | `IReadOnlyList<ContextDocument>` | Documents included in the package, in descending score order |
| `TotalTokenEstimate` | `int` | Sum of `TokenEstimate` across all included documents |
| `DocumentsConsidered` | `int` | Number of search hits examined before budget was applied |
| `DocumentsIncluded` | `int` | `Documents.Count` — provided for convenience |
| `AssembledAt` | `DateTimeOffset` | UTC timestamp of assembly |

`ContextPackage` provides one method:

```
ToPromptString() → string
```

Renders the package as a formatted string suitable for direct injection into an AI prompt. The format is:

```
CONTEXT PACKAGE
Query: {Query}
Documents: {DocumentsIncluded} | Token estimate: {TotalTokenEstimate}
Assembled: {AssembledAt:u}

--- Document 1 of N ---
Title: {Title ?? DisplayName}
Source: {CanonicalUri}
Score: {Score:F3}
Tokens: ~{TokenEstimate}

{Content}

--- Document 2 of N ---
...
```

`ContextPackage` is an immutable record.

### 3.5 `IContextAssembler`

```csharp
public interface IContextAssembler
{
    Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken cancellationToken = default);
}
```

Single method. Implementations must be safe to call concurrently (stateless orchestration; no shared mutable state).

---

## Section 4: Context Assembly Engine (`Ferret.AI`)

### 4.1 `TokenEstimator`

Static utility class. No constructor. No dependencies.

```csharp
public static class TokenEstimator
{
    public static int Estimate(string text) => Math.Max(1, text.Length / 4);
}
```

Rationale: character-to-token ratio of 4:1 is a well-established approximation for English prose and code. It is intentionally conservative (overestimates for dense code, underestimates for very long tokens). No LLM call is made. This is the Sprint 13–14 standard; a tiktoken-equivalent may replace it in Sprint 15 if measurement shows material budget overrun.

### 4.2 `ContextDeduplicator`

Deduplicates `SearchHit[]` by `DocumentId`. First occurrence (highest score, since results arrive in descending score order) wins. Subsequent hits for the same `DocumentId` are dropped.

```csharp
public sealed class ContextDeduplicator
{
    public IReadOnlyList<SearchHit> Deduplicate(IReadOnlyList<SearchHit> hits);
}
```

Input is assumed to be in descending score order (as returned by `ISearchService.SearchAsync`). Output preserves that order with duplicates removed. This is a pure in-memory operation with O(n) complexity via a `HashSet<DocumentId>`.

### 4.3 `DocumentExpander`

Resolves `SearchHit[]` to full `Document[]` by calling `IDocumentService.GetAsync` for each hit.

```csharp
public sealed class DocumentExpander
{
    public DocumentExpander(IDocumentService documentService, ILogger<DocumentExpander> logger);

    public Task<IReadOnlyList<Document>> ExpandAsync(
        IReadOnlyList<SearchHit> hits,
        CancellationToken cancellationToken = default);
}
```

Expansion is performed in parallel with a concurrency limit of 5 (`SemaphoreSlim(5, 5)`). If `IDocumentService.GetAsync` returns `null` for a `DocumentId` (document deleted between search and expand), the hit is skipped and a warning is logged at `LogWarning` level. The returned list contains only successfully resolved documents, in the same order as the input hits.

### 4.4 `ContentFilter`

Removes low-quality documents from the expanded set before token budget is applied. Runs as a pure in-memory pass — no I/O.

```csharp
public static class ContentFilter
{
    public static IReadOnlyList<Document> Filter(IReadOnlyList<Document> documents);
}
```

**Filter rules applied in order (all rules are AND'd — a document must pass every rule to be included):**

| Rule | Condition to EXCLUDE | Rationale |
|---|---|---|
| Empty | `string.IsNullOrWhiteSpace(doc.PlainText)` | Binary files, zero-byte files, and parse failures produce no usable text |
| Too small | `doc.PlainText.Trim().Length < 50` | Stub files, header guards, and auto-generated single-line files waste token budget |
| Content duplicate | Fingerprint of `(length, first 200 chars)` already seen in this pass | Same file copied to multiple paths appears only once; first occurrence (highest score) wins |

`ContentFilter` is a static class — no DI, no state. The fingerprint is a composite of `(doc.PlainText.Trim().Length, doc.PlainText.Trim()[..Math.Min(200, length)])` stored in a `HashSet<string>` local to the call. The first document with a given fingerprint is kept; subsequent documents with the same fingerprint are dropped.

### 4.5 `ContextAssembler : IContextAssembler`

Orchestrates the full pipeline. Receives `ISearchService`, `ContextDeduplicator`, `DocumentExpander`, and `ILogger<ContextAssembler>` via constructor injection.

**Pipeline steps (executed in order):**

1. **Search** — Call `ISearchService.SearchAsync(request.Query, SearchOptions.Default)`. Receive `SearchServiceResult` containing `SearchHit[]` in descending score order.
2. **Deduplicate** — Pass hits to `ContextDeduplicator.Deduplicate`. Remove duplicate `DocumentId`s, preserving score order.
3. **Expand** — Pass deduplicated hits to `DocumentExpander.ExpandAsync`. Receive `IReadOnlyList<Document>`.
4. **Filter** — Pass expanded documents to `ContentFilter.Filter`. Remove empty, too-small, and content-duplicate documents.
5. **Token budget** — Iterate filtered documents in score order. Add each document to the package if doing so would not exceed `request.MaxTokens` and the current count is below `request.MaxDocuments`. Stop when either limit is reached. Documents that would exceed the token budget are skipped (not truncated).
6. **Assemble** — Construct `ContextPackage` from included documents with `AssembledAt = DateTimeOffset.UtcNow`.

```csharp
public sealed class ContextAssembler : IContextAssembler
{
    public ContextAssembler(
        ISearchService searchService,
        ContextDeduplicator deduplicator,
        DocumentExpander expander,
        ILogger<ContextAssembler> logger);

    public Task<ContextPackage> AssembleAsync(
        ContextRequest request,
        CancellationToken cancellationToken = default);
}
```

`ContextAssembler` is stateless. The same instance may serve concurrent requests without locking.

### 4.6 `AiModule` (updated)

`Ferret.AI.AiModule.ConfigureServices(IServiceCollection services)` is updated from its empty Sprint 12 scaffold to register the assembly engine:

```csharp
services.AddSingleton<IContextAssembler, ContextAssembler>();
services.AddSingleton<DocumentExpander>();
services.AddSingleton<ContextDeduplicator>();
```

`TokenEstimator` and `ContentFilter` are static classes and require no registration.

---

## Section 5: MCP Integration (`Ferret.Mcp`)

### 5.1 `ContextTool : IMcpTool`

```csharp
public sealed class ContextTool : IMcpTool
{
    public ContextTool(IContextAssembler contextAssembler);

    public McpToolDescriptor Descriptor { get; }
    public Task<McpToolResult> ExecuteAsync(McpArguments args, CancellationToken ct);
}
```

**Tool descriptor:**

| Field | Value |
|---|---|
| Name | `ferret_context` |
| Description | `Assemble a complete, deduplicated, token-budgeted context package for a query. Returns formatted document context ready for AI consumption.` |

**Input schema:**

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `query` | `string` | yes | — | The search query |
| `max_tokens` | `integer` | no | `8000` | Token budget ceiling |
| `max_documents` | `integer` | no | `10` | Maximum documents to include |

**Execution:**

1. Extract `query` (required; return error result if absent or empty).
2. Parse `max_tokens` and `max_documents` from args with defaults.
3. Construct `ContextRequest`.
4. Call `IContextAssembler.AssembleAsync`.
5. Return `McpToolResult` with `Content = package.ToPromptString()`.
6. On exception: return `McpToolResult` with `IsError = true` and a concise error message. Do not propagate exceptions to the MCP transport.

### 5.2 Registration in `McpModule`

Add to `McpModule.ConfigureServices`:

```csharp
services.AddSingleton<IMcpTool, ContextTool>();
```

`McpModule.csproj` acquires a project reference to `Ferret.AI` (for `IContextAssembler`).

---

## Section 6: CLI Command (`Ferret.Cli`)

### 6.1 `ferret context`

```
ferret context <query> [--max-tokens <n>] [--max-documents <n>]
```

Assembles a context package and renders it to stdout for local inspection.

**Output format:**

```
Context assembled: 4 documents, ~3200 tokens

  [1] AuthService.cs  (score: 0.921, ~800 tokens)
      src/Auth/AuthService.cs
      The AuthService class handles JWT validation and session management...
      [first 500 chars shown]

  [2] AuthMiddleware.cs  (score: 0.876, ~640 tokens)
      src/Middleware/AuthMiddleware.cs
      AuthMiddleware intercepts incoming requests and validates Bearer tokens...
      [first 500 chars shown]

  ...
```

The CLI command shows:
- Summary line: document count and total token estimate
- Per-document entry: display name, score, token estimate, canonical URI, and first 500 characters of content

### 6.2 `ContextCliModule`

Follows the established pattern of `ConnectorCliModule` and `IndexCliModule`:

```csharp
public static class ContextCliModule
{
    public static Command BuildCommand(IServiceProvider services);
}
```

Registers a `context` sub-command with the root `Command`. Command handler constructs `ContextRequest` from CLI arguments, calls `IContextAssembler.AssembleAsync`, and writes the formatted output to `IConsole` (or `Console.Out`).

`Ferret.Cli.csproj` acquires a project reference to `Ferret.AI` if not already present.

---

## Section 7: File Structure Map

```
src/Ferret.Core/
  Context/
    ContextRequest.cs               [NEW]
    ContextDocument.cs              [NEW]
    ContextDocumentSource.cs        [NEW]
    ContextPackage.cs               [NEW]
    Interfaces/
      IContextAssembler.cs          [NEW]

src/Ferret.AI/
  Ferret.AI.csproj                  [MODIFY — add Ferret.Search ref if not present]
  AiModule.cs                       [MODIFY — register assembly engine]
  Context/
    TokenEstimator.cs               [NEW]
    ContextDeduplicator.cs          [NEW]
    DocumentExpander.cs             [NEW]
    ContextAssembler.cs             [NEW]

src/Ferret.Mcp/
  Tools/
    ContextTool.cs                  [NEW]
  McpModule.cs                      [MODIFY — AddSingleton<IMcpTool, ContextTool>()]
  Ferret.Mcp.csproj                 [MODIFY — add Ferret.AI project reference]

src/Ferret.Cli/
  Commands/Context/
    ContextCliModule.cs             [NEW]
    ContextCommandHandler.cs        [NEW]
  Program.cs                        [MODIFY — register ContextCliModule]
  Ferret.Cli.csproj                 [MODIFY — add Ferret.AI project reference if absent]

tests/Ferret.Core.Tests/
  Context/
    ContextRequestTests.cs          [NEW]
    ContextDocumentTests.cs         [NEW]
    ContextPackageTests.cs          [NEW — includes ToPromptString format tests]

tests/Ferret.AI.Tests/
  Ferret.AI.Tests.csproj            [NEW — created in s2]
  Context/
    TokenEstimatorTests.cs          [NEW]
    ContextDeduplicatorTests.cs     [NEW]
    DocumentExpanderTests.cs        [NEW]
    ContextAssemblerTests.cs        [NEW]

src/Ferret.sln                      [MODIFY — add Ferret.AI.Tests project]
```

---

## Section 8: Global Constraints

- Sprint 12 must be fully implemented before Sprint 13 begins. `Ferret.AI` scaffold, `ISearchService`, and `IDocumentService` must all be resolvable from DI.
- All tasks: TDD — write failing test first, confirm red, implement, verify green. No implementation without a failing test.
- Commit prefix: `feat(sprint-13):`, `test(sprint-13):`, `chore(sprint-13):`, `docs(sprint-13):`.
- No AI model is called at runtime. `TokenEstimator` uses `text.Length / 4` only. The Version Gate Rule is: zero LLM API calls during `dotnet test`.
- `ContextAssembler` must be stateless — safe for concurrent calls without locking.
- `DocumentExpander` must cap parallel `IDocumentService.GetAsync` calls at 5. Missing documents are logged and skipped; they do not throw.
- `ContextTool.ExecuteAsync` must never propagate exceptions to the MCP transport. All errors return `McpToolResult` with `IsError = true`.
- `ContextPackage.ToPromptString()` output format is stable and considered part of the public contract from Sprint 13 onward. Do not change it without a corresponding update to this specification.
- Full solution must pass: `dotnet test src/Ferret.sln -v n`.

---

## Section 9: Sub-Plans

Sprint 13 is implemented as three ordered sub-plans:

| Sub-Plan | File | Prerequisite | Scope |
|---|---|---|---|
| s1 | `2026-06-29-sprint-13-s1-context-core-contracts.md` | Sprint 12 complete | `Ferret.Core.Context` namespace in `Ferret.Core` + `tests/Ferret.Core.Tests/Context/` |
| s2 | `2026-06-29-sprint-13-s2-context-assembly-engine.md` | s1 complete | `Ferret.AI` implementation + new `tests/Ferret.AI.Tests/` project (including csproj) |
| s3 | `2026-06-29-sprint-13-s3-mcp-cli-wireup.md` | s2 complete | `ContextTool` in `Ferret.Mcp` + `ContextCliModule` in `Ferret.Cli` + solution wiring |

Sub-plans s1 → s2 → s3 are strictly sequential. s2 creates `Ferret.AI.Tests.csproj`; s3 relies on it being present.
