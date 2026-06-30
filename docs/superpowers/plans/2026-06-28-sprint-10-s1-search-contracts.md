# Sprint 10 — Section 1: Search Contracts (`Ferret.Core.Search` + ADR-0015)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Extend `Ferret.Core` with the `Ferret.Core.Search` namespace — all public contracts for the Information Retrieval Platform. No implementation. Every downstream section (S2 Query Parser, S3 Search Platform, S4 Rendering, S5 CLI) depends on this section. ADR-0015 is committed before any code.

**Architecture:** `SearchExpression` is the canonical query AST — shared by every search provider (BM25, Semantic, Hybrid, Knowledge). `SearchHit` carries `DocumentId` + `CanonicalUri` as canonical identities; renderers derive display labels. `SearchServiceResult` uses typed status codes, never exceptions, for expected environmental conditions.

**ADR:** `docs/adr/0015-information-retrieval-architecture.md` — written as Task 1, committed before any code.

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `required` on record/class properties with no sensible default, `sealed` on all concrete classes and records.

---

## Prerequisites

Sprint 9 must be **complete** before starting this section:
- `Ferret.ParserPlatform`, `Ferret.Indexing` merged and green
- `ferret index` working end-to-end against a real `.ferret/` workspace
- `dotnet test` passes on `master`
- Tag `v0.9.0-sprint9` applied

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes and records
- `required` keyword on all record/class properties with no sensible default
- No breaking changes to existing `Ferret.Core.*` types
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-10):`, `test(sprint-10):`, `chore(sprint-10):`
- ADR-0015 is committed as `docs(sprint-10): ADR-0015` **before** any code commit

---

## File Inventory

### New Source Files

| File | Namespace |
|---|---|
| `src/Ferret.Core/Search/TextSpanKind.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/TextSpan.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/HighlightedText.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchHitKind.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchExpression.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchQuery.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchDiagnostic.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchParseResult.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/IQueryParser.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/ExecutionMode.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchOptions.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchHit.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchResult.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchProviderDescriptor.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchCapabilities.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchServiceStatus.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchExecutionInfo.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/SearchServiceResult.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/ISearchProvider.cs` | `Ferret.Core.Search` |
| `src/Ferret.Core/Search/ISearchService.cs` | `Ferret.Core.Search` |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.Core.Tests/Search/SearchHighlightTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Search/SearchQueryAstTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Search/SearchOptionsTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Search/SearchHitTests.cs` | Ferret.Core.Tests |
| `tests/Ferret.Core.Tests/Search/SearchServiceModelTests.cs` | Ferret.Core.Tests |

### New Doc Files

| File |
|---|
| `docs/adr/0015-information-retrieval-architecture.md` |

---

## Task 1: ADR-0015 — Information Retrieval Architecture

**Why first:** The ADR is the architectural contract for the Search Platform. Committed before any code so the intent is documented before decisions get encoded.

**Files:**
- Create: `docs/adr/0015-information-retrieval-architecture.md`

**Interfaces:**
- Produces: architectural record — referenced by S2, S3, S4, S5 implementers

- [ ] **Step 1: Create ADR-0015**

Create `docs/adr/0015-information-retrieval-architecture.md`:

```markdown
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
```

- [ ] **Step 2: Verify build is clean**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors.

- [ ] **Step 3: Commit ADR-0015**

```bash
git add docs/adr/0015-information-retrieval-architecture.md
git commit -m "docs(sprint-10): ADR-0015 Information Retrieval Architecture"
```

---

## Task 2: Highlight Primitives + `SearchHitKind`

**Why:** Lowest-level types in the search model. Everything in Tasks 3–7 depends on them.

**Files:**
- Create: `src/Ferret.Core/Search/TextSpanKind.cs`
- Create: `src/Ferret.Core/Search/TextSpan.cs`
- Create: `src/Ferret.Core/Search/HighlightedText.cs`
- Create: `src/Ferret.Core/Search/SearchHitKind.cs`
- Create: `tests/Ferret.Core.Tests/Search/SearchHighlightTests.cs`

**Interfaces:**
- Produces: `TextSpanKind`, `TextSpan`, `HighlightedText`, `SearchHitKind` — consumed by Tasks 5, 6; S3 (`HighlightEngine`); S4 (renderers, `ITextStyler`)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Search/SearchHighlightTests.cs`:

```csharp
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchHighlightTests
{
    [Fact]
    public void TextSpan_Equality_By_Value()
    {
        Assert.Equal(new TextSpan("hello", TextSpanKind.Normal), new TextSpan("hello", TextSpanKind.Normal));
    }

    [Fact]
    public void TextSpan_Inequality_Different_Kind()
    {
        Assert.NotEqual(new TextSpan("hello", TextSpanKind.Normal), new TextSpan("hello", TextSpanKind.Match));
    }

    [Fact]
    public void HighlightedText_Plain_Creates_Single_Normal_Span()
    {
        var ht = HighlightedText.Plain("hello world");
        Assert.Single(ht.Spans);
        Assert.Equal("hello world", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
    }

    [Fact]
    public void HighlightedText_Empty_Has_No_Spans()
    {
        Assert.Empty(HighlightedText.Empty.Spans);
    }

    [Fact]
    public void HighlightedText_Spans_Is_ReadOnly()
    {
        var ht = HighlightedText.Plain("x");
        Assert.IsAssignableFrom<IReadOnlyList<TextSpan>>(ht.Spans);
    }

    [Fact]
    public void TextSpanKind_Has_Six_Values()
    {
        Assert.Equal(6, Enum.GetValues<TextSpanKind>().Length);
    }

    [Fact]
    public void SearchHitKind_Has_Three_Values()
    {
        Assert.Equal(3, Enum.GetValues<SearchHitKind>().Length);
    }

    [Fact]
    public void SearchHitKind_File_Is_Zero()
    {
        Assert.Equal(0, (int)SearchHitKind.File);
    }

    [Fact]
    public void SearchHitKind_Segment_Is_Two()
    {
        Assert.Equal(2, (int)SearchHitKind.Segment);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchHighlightTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `TextSpanKind.cs`**

`src/Ferret.Core/Search/TextSpanKind.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Classifies a <see cref="TextSpan"/> within a <see cref="HighlightedText"/>.
/// Providers assign span kinds; renderers apply formatting based on them.
/// </summary>
public enum TextSpanKind
{
    /// <summary>Ordinary text — no special formatting.</summary>
    Normal = 0,

    /// <summary>Text that matched the search query — highlighted by the renderer.</summary>
    Match = 1,

    /// <summary>Reserved: text deleted in a diff context (Sprint 11+).</summary>
    Deleted = 2,

    /// <summary>Reserved: text inserted in a diff context (Sprint 11+).</summary>
    Inserted = 3,

    /// <summary>Reserved: text flagged with a warning annotation (Sprint 11+).</summary>
    Warning = 4,

    /// <summary>Reserved: text referenced by an AI-generated answer (Sprint 11+).</summary>
    AIReference = 5,
}
```

- [ ] **Step 4: Create `TextSpan.cs`**

`src/Ferret.Core/Search/TextSpan.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// An immutable segment of text within a <see cref="HighlightedText"/>, tagged with a display kind.
/// The provider assigns the kind; the renderer applies formatting.
/// </summary>
/// <param name="Text">The text content of this span.</param>
/// <param name="Kind">The display classification of this span.</param>
public sealed record TextSpan(string Text, TextSpanKind Kind);
```

- [ ] **Step 5: Create `HighlightedText.cs`**

`src/Ferret.Core/Search/HighlightedText.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// A sequence of <see cref="TextSpan"/> values representing snippet text with semantic highlight markers.
/// Produced by the provider's internal <c>HighlightEngine</c>; consumed by renderers.
/// No renderer knows the backend markup format (e.g. FTS5 snippet syntax) that produced this model.
/// </summary>
public sealed class HighlightedText
{
    /// <summary>Gets the ordered spans that compose this highlighted text.</summary>
    public required IReadOnlyList<TextSpan> Spans { get; init; }

    /// <summary>Creates a <see cref="HighlightedText"/> from a single plain (un-highlighted) string.</summary>
    /// <param name="text">The plain text content.</param>
    public static HighlightedText Plain(string text) =>
        new() { Spans = [new TextSpan(text, TextSpanKind.Normal)] };

    /// <summary>Gets a shared empty instance with no spans.</summary>
    public static HighlightedText Empty { get; } = new() { Spans = [] };
}
```

- [ ] **Step 6: Create `SearchHitKind.cs`**

`src/Ferret.Core/Search/SearchHitKind.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Classifies the granularity of a <see cref="SearchHit"/>.
/// Sprint 10: <see cref="File"/> (default) and <see cref="Passage"/> (<c>--passages</c>).
/// <see cref="Segment"/> is reserved for Sprint 11 semantic search.
/// </summary>
public enum SearchHitKind
{
    /// <summary>Result represents an entire file — the best-matching snippet is surfaced.</summary>
    File = 0,

    /// <summary>Result represents a human-readable passage (heading + body block).</summary>
    Passage = 1,

    /// <summary>Reserved: AI processing unit (embedding chunk, notebook cell, AST node). Sprint 11+.</summary>
    Segment = 2,
}
```

- [ ] **Step 7: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchHighlightTests"
dotnet build src/Ferret.sln
```

Expected: 9 tests pass, 0 build errors.

---

## Task 3: Query AST + `SearchQuery` + `SearchParseResult` + `IQueryParser`

**Files:**
- Create: `src/Ferret.Core/Search/SearchExpression.cs`
- Create: `src/Ferret.Core/Search/SearchQuery.cs`
- Create: `src/Ferret.Core/Search/SearchDiagnostic.cs`
- Create: `src/Ferret.Core/Search/SearchParseResult.cs`
- Create: `src/Ferret.Core/Search/IQueryParser.cs`
- Create: `tests/Ferret.Core.Tests/Search/SearchQueryAstTests.cs`

**Interfaces:**
- Produces: `SearchExpression` hierarchy, `SearchQuery`, `SearchParseResult`, `IQueryParser` — consumed by S2 (`QueryParser`), S3 (`QueryTranslator`), Tasks 6–7

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Search/SearchQueryAstTests.cs`:

```csharp
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchQueryAstTests
{
    [Fact]
    public void KeywordExpression_Equality_By_Value()
    {
        Assert.Equal(new KeywordExpression("auth"), new KeywordExpression("auth"));
    }

    [Fact]
    public void KeywordExpression_Inequality_Different_Value()
    {
        Assert.NotEqual(new KeywordExpression("auth"), new KeywordExpression("token"));
    }

    [Fact]
    public void PhraseExpression_Equality_By_Value()
    {
        Assert.Equal(new PhraseExpression("runtime builder"), new PhraseExpression("runtime builder"));
    }

    [Fact]
    public void PrefixExpression_Equality_By_Prefix()
    {
        Assert.Equal(new PrefixExpression("auth"), new PrefixExpression("auth"));
    }

    [Fact]
    public void AndExpression_Equality_By_Operands()
    {
        var a = new AndExpression([new KeywordExpression("auth"), new KeywordExpression("token")]);
        var b = new AndExpression([new KeywordExpression("auth"), new KeywordExpression("token")]);
        Assert.Equal(a, b);
    }

    [Fact]
    public void SearchQuery_Preserves_OriginalText()
    {
        var q = new SearchQuery
        {
            OriginalText = "auth token",
            Root = new AndExpression([new KeywordExpression("auth"), new KeywordExpression("token")]),
        };
        Assert.Equal("auth token", q.OriginalText);
    }

    [Fact]
    public void SearchParseResult_Success_IsSuccess_True_And_Query_Set()
    {
        var q = new SearchQuery { OriginalText = "auth", Root = new KeywordExpression("auth") };
        var result = SearchParseResult.Success(q);
        Assert.True(result.IsSuccess);
        Assert.Same(q, result.Query);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SearchParseResult_Failure_IsSuccess_False_And_Has_Error_Diagnostic()
    {
        var result = SearchParseResult.Failure("unexpected token at position 3");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Query);
        Assert.Single(result.Diagnostics);
        Assert.Equal(SearchDiagnosticSeverity.Error, result.Diagnostics[0].Severity);
        Assert.Equal("unexpected token at position 3", result.Diagnostics[0].Message);
    }

    [Fact]
    public void SearchParseResult_Failure_With_Diagnostics_List()
    {
        var diagnostics = new[]
        {
            new SearchDiagnostic(SearchDiagnosticSeverity.Error, "msg1"),
            new SearchDiagnostic(SearchDiagnosticSeverity.Warning, "msg2"),
        };
        var result = SearchParseResult.Failure(diagnostics);
        Assert.Equal(2, result.Diagnostics.Count);
    }

    [Fact]
    public void SearchDiagnostic_Position_Defaults_To_Null()
    {
        var d = new SearchDiagnostic(SearchDiagnosticSeverity.Error, "msg");
        Assert.Null(d.Position);
    }

    [Fact]
    public void SearchDiagnostic_With_Position_Preserves_It()
    {
        var d = new SearchDiagnostic(SearchDiagnosticSeverity.Warning, "warn", 5);
        Assert.Equal(5, d.Position);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchQueryAstTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `SearchExpression.cs`**

`src/Ferret.Core/Search/SearchExpression.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Base type for all nodes in the search query AST.
/// The AST is a canonical platform contract — shared by every search provider.
/// Providers translate AST nodes to backend-specific syntax; the AST is never backend-specific.
/// </summary>
public abstract record SearchExpression;

/// <summary>A single keyword term. Matches documents containing this word.</summary>
/// <param name="Value">The keyword (case-insensitive matching is provider-determined).</param>
public sealed record KeywordExpression(string Value) : SearchExpression;

/// <summary>An exact phrase. Matches documents containing these words adjacent and in order.</summary>
/// <param name="Value">The phrase text, excluding surrounding quotes.</param>
public sealed record PhraseExpression(string Value) : SearchExpression;

/// <summary>A prefix match. Matches documents containing any word starting with this prefix.</summary>
/// <param name="Prefix">The prefix (the trailing <c>*</c> is stripped by the parser).</param>
public sealed record PrefixExpression(string Prefix) : SearchExpression;

/// <summary>
/// Implicit AND of two or more operands. Sprint 10: produced for all multi-term queries.
/// All operands must match for a document to be returned.
/// </summary>
/// <param name="Operands">The child expressions — must contain at least two items.</param>
public sealed record AndExpression(IReadOnlyList<SearchExpression> Operands) : SearchExpression;

// ── Reserved expressions — not emitted by the Sprint 10 parser ───────────────

/// <summary>Reserved: OR of two or more operands. Not emitted in Sprint 10.</summary>
/// <param name="Operands">The child expressions.</param>
public sealed record OrExpression(IReadOnlyList<SearchExpression> Operands) : SearchExpression;

/// <summary>Reserved: logical NOT of a single operand. Not emitted in Sprint 10.</summary>
/// <param name="Operand">The negated expression.</param>
public sealed record NotExpression(SearchExpression Operand) : SearchExpression;

/// <summary>Reserved: explicit grouping for precedence. Not emitted in Sprint 10.</summary>
/// <param name="Inner">The grouped expression.</param>
public sealed record GroupExpression(SearchExpression Inner) : SearchExpression;
```

- [ ] **Step 4: Create `SearchQuery.cs`**

`src/Ferret.Core/Search/SearchQuery.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// An immutable, parsed search query. Carries the original text alongside the canonical AST.
/// <see cref="OriginalText"/> is for telemetry, logging, query history, and "did you mean?" suggestions.
/// <see cref="Root"/> is the machine-readable form consumed by providers.
/// </summary>
public sealed record SearchQuery
{
    /// <summary>Gets the raw query text as entered by the user. Preserved verbatim.</summary>
    public required string OriginalText { get; init; }

    /// <summary>Gets the root of the parsed query AST.</summary>
    public required SearchExpression Root { get; init; }
}
```

- [ ] **Step 5: Create `SearchDiagnostic.cs`**

`src/Ferret.Core/Search/SearchDiagnostic.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>A diagnostic message produced during query parsing or search execution.</summary>
/// <param name="Severity">The severity of this diagnostic.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
/// <param name="Position">The zero-based character position in the raw query where the issue occurred, if applicable.</param>
public sealed record SearchDiagnostic(SearchDiagnosticSeverity Severity, string Message, int? Position = null);

/// <summary>Severity levels for search diagnostics.</summary>
public enum SearchDiagnosticSeverity
{
    /// <summary>Informational note — no action required.</summary>
    Info = 0,

    /// <summary>Non-fatal issue — search proceeded with a best-effort interpretation.</summary>
    Warning = 1,

    /// <summary>Fatal error — search could not proceed.</summary>
    Error = 2,
}
```

- [ ] **Step 6: Create `SearchParseResult.cs`**

`src/Ferret.Core/Search/SearchParseResult.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// The outcome of a query parse attempt. The parser never throws for user input;
/// all failure modes are represented as <see cref="SearchParseResult"/> values.
/// Use the static factory methods to construct instances.
/// </summary>
public sealed class SearchParseResult
{
    private SearchParseResult() { }

    /// <summary>Gets a value indicating whether parsing produced a valid query.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Gets the parsed query. Only valid when <see cref="IsSuccess"/> is true.</summary>
    public SearchQuery? Query { get; private init; }

    /// <summary>Gets diagnostics collected during parsing.</summary>
    public IReadOnlyList<SearchDiagnostic> Diagnostics { get; private init; } = [];

    /// <summary>Parsing succeeded and produced a valid query.</summary>
    public static SearchParseResult Success(SearchQuery query) =>
        new() { IsSuccess = true, Query = query };

    /// <summary>Parsing failed with multiple diagnostics.</summary>
    public static SearchParseResult Failure(IReadOnlyList<SearchDiagnostic> diagnostics) =>
        new() { IsSuccess = false, Diagnostics = diagnostics };

    /// <summary>Parsing failed with a single error message.</summary>
    public static SearchParseResult Failure(string message) =>
        Failure([new SearchDiagnostic(SearchDiagnosticSeverity.Error, message)]);
}
```

- [ ] **Step 7: Create `IQueryParser.cs`**

`src/Ferret.Core/Search/IQueryParser.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Parses a raw user query string into a canonical <see cref="SearchQuery"/> AST.
/// Sprint 10 supports: whitespace-separated keywords (implicit AND), quoted phrases, trailing * prefix.
/// Never throws for syntactically invalid input — all outcomes are <see cref="SearchParseResult"/> values.
/// Implementation lives in <c>Ferret.Search</c>; interface lives in <c>Ferret.Core</c>.
/// </summary>
public interface IQueryParser
{
    /// <summary>Parses <paramref name="rawQuery"/> into a <see cref="SearchParseResult"/>.</summary>
    /// <param name="rawQuery">The raw query string as entered by the user.</param>
    SearchParseResult Parse(string rawQuery);
}
```

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchQueryAstTests|SearchHighlightTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 4: `ExecutionMode` + `SearchOptions`

**Files:**
- Create: `src/Ferret.Core/Search/ExecutionMode.cs`
- Create: `src/Ferret.Core/Search/SearchOptions.cs`
- Create: `tests/Ferret.Core.Tests/Search/SearchOptionsTests.cs`

**Interfaces:**
- Produces: `ExecutionMode`, `SearchOptions` — consumed by Task 7 (`ISearchService`, `ISearchProvider`), S3 (`SearchService`, `ProviderSelector`)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Search/SearchOptionsTests.cs`:

```csharp
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchOptionsTests
{
    [Fact]
    public void SearchOptions_Default_MaxResults_Is_10()
    {
        Assert.Equal(10, SearchOptions.Default.MaxResults);
    }

    [Fact]
    public void SearchOptions_Default_HighlightEnabled_Is_True()
    {
        Assert.True(SearchOptions.Default.HighlightEnabled);
    }

    [Fact]
    public void SearchOptions_Default_SnippetLength_Is_160()
    {
        Assert.Equal(160, SearchOptions.Default.SnippetLength);
    }

    [Fact]
    public void SearchOptions_Default_Mode_Is_Keyword()
    {
        Assert.Equal(ExecutionMode.Keyword, SearchOptions.Default.Mode);
    }

    [Fact]
    public void SearchOptions_Default_IncludePassages_Is_False()
    {
        Assert.False(SearchOptions.Default.IncludePassages);
    }

    [Fact]
    public void SearchOptions_Can_Be_Customised()
    {
        var opts = new SearchOptions { MaxResults = 5, IncludePassages = true, HighlightEnabled = false };
        Assert.Equal(5, opts.MaxResults);
        Assert.True(opts.IncludePassages);
        Assert.False(opts.HighlightEnabled);
    }

    [Fact]
    public void ExecutionMode_Has_Four_Values()
    {
        Assert.Equal(4, Enum.GetValues<ExecutionMode>().Length);
    }

    [Fact]
    public void ExecutionMode_Auto_Is_Zero()
    {
        Assert.Equal(0, (int)ExecutionMode.Auto);
    }

    [Fact]
    public void ExecutionMode_Keyword_Is_One()
    {
        Assert.Equal(1, (int)ExecutionMode.Keyword);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchOptionsTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `ExecutionMode.cs`**

`src/Ferret.Core/Search/ExecutionMode.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Controls which search provider(s) <see cref="ISearchService"/> uses for a request.
/// Sprint 10 always uses <see cref="Keyword"/>. Future values are reserved.
/// </summary>
public enum ExecutionMode
{
    /// <summary>Reserved: automatically select the best available provider based on query and capabilities. Sprint 11+.</summary>
    Auto = 0,

    /// <summary>Use the BM25/FTS5 keyword search provider.</summary>
    Keyword = 1,

    /// <summary>Reserved: use the semantic (embedding) search provider. Sprint 11+.</summary>
    Semantic = 2,

    /// <summary>Reserved: use both keyword and semantic providers, fused by a post-processor. Sprint 12+.</summary>
    Hybrid = 3,
}
```

- [ ] **Step 4: Create `SearchOptions.cs`**

`src/Ferret.Core/Search/SearchOptions.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Controls how a search request executes. Separate from <see cref="SearchQuery"/> (what the user wants)
/// so the same query can be executed differently by CLI, MCP, REST, and programmatic callers.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>Gets the maximum number of hits to return. Default: 10.</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>Gets a value indicating whether to return passage-level hits instead of file-level hits.</summary>
    public bool IncludePassages { get; init; }

    /// <summary>Gets a value indicating whether to apply ANSI/HTML highlight markers to snippets. Default: true.</summary>
    public bool HighlightEnabled { get; init; } = true;

    /// <summary>Gets the maximum character length of each snippet. Default: 160.</summary>
    public int SnippetLength { get; init; } = 160;

    /// <summary>Gets the execution mode controlling provider selection. Default: <see cref="ExecutionMode.Keyword"/>.</summary>
    public ExecutionMode Mode { get; init; } = ExecutionMode.Keyword;

    /// <summary>Gets the cancellation token for this request.</summary>
    public CancellationToken Token { get; init; } = CancellationToken.None;

    /// <summary>Gets a shared default instance with all defaults applied.</summary>
    public static SearchOptions Default { get; } = new();
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchOptionsTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 5: `SearchHit` Hierarchy + `SearchResult`

**Files:**
- Create: `src/Ferret.Core/Search/SearchHit.cs`
- Create: `src/Ferret.Core/Search/SearchResult.cs`
- Create: `tests/Ferret.Core.Tests/Search/SearchHitTests.cs`

**Interfaces:**
- Consumes: `DocumentId` (`Ferret.Core.Primitives`), `ConnectorInstanceId` (`Ferret.Core.Connectors`), `HighlightedText`, `SearchHitKind` (Tasks 2)
- Produces: `SearchHit`, `FileSearchHit`, `PassageSearchHit`, `SegmentSearchHit`, `SearchResult` — consumed by Task 6 (`SearchServiceResult`); S3 (`BM25SearchProvider`); S4 (renderers)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Search/SearchHitTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchHitTests
{
    [Fact]
    public void FileSearchHit_Kind_Is_File()
    {
        Assert.Equal(SearchHitKind.File, MakeFileHit().Kind);
    }

    [Fact]
    public void FileSearchHit_Score_Preserved()
    {
        Assert.Equal(0.92f, MakeFileHit().Score);
    }

    [Fact]
    public void FileSearchHit_Explanation_Defaults_To_Null()
    {
        Assert.Null(MakeFileHit().Explanation);
    }

    [Fact]
    public void PassageSearchHit_Kind_Is_Passage()
    {
        Assert.Equal(SearchHitKind.Passage, MakePassageHit().Kind);
    }

    [Fact]
    public void PassageSearchHit_Heading_May_Be_Null()
    {
        var hit = MakePassageHit() with { Heading = null };
        Assert.Null(hit.Heading);
    }

    [Fact]
    public void PassageSearchHit_Preserves_Offsets()
    {
        var hit = MakePassageHit();
        Assert.Equal(10, hit.StartOffset);
        Assert.Equal(200, hit.EndOffset);
    }

    [Fact]
    public void SearchResult_Empty_Has_Zero_Hits()
    {
        var result = SearchResult.Empty;
        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalHits);
        Assert.Equal(0, result.ReturnedHits);
    }

    [Fact]
    public void SearchResult_ReturnedHits_Matches_Hits_Count()
    {
        var result = new SearchResult
        {
            Hits = [MakeFileHit()],
            TotalHits = 5,
            ReturnedHits = 1,
        };
        Assert.Equal(1, result.ReturnedHits);
        Assert.Equal(5, result.TotalHits);
    }

    private static FileSearchHit MakeFileHit() => new()
    {
        DocumentId = new DocumentId("filesystem:///src/Program.cs"),
        ConnectorInstanceId = new ConnectorInstanceId("src-root"),
        CanonicalUri = new Uri("filesystem:///src/Program.cs"),
        DisplayName = "src/Program.cs",
        Kind = SearchHitKind.File,
        Score = 0.92f,
        Snippet = HighlightedText.Plain("...the main entry point..."),
    };

    private static PassageSearchHit MakePassageHit() => new()
    {
        DocumentId = new DocumentId("filesystem:///src/Program.cs"),
        ConnectorInstanceId = new ConnectorInstanceId("src-root"),
        CanonicalUri = new Uri("filesystem:///src/Program.cs"),
        DisplayName = "src/Program.cs",
        Kind = SearchHitKind.Passage,
        Score = 0.85f,
        Snippet = HighlightedText.Plain("...authentication context..."),
        Heading = "Authentication",
        StartOffset = 10,
        EndOffset = 200,
    };
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchHitTests"
```

Expected: FAIL — `SearchHit`, `FileSearchHit`, `PassageSearchHit`, `SearchResult` not found.

- [ ] **Step 3: Create `SearchHit.cs`**

`src/Ferret.Core/Search/SearchHit.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Search;

/// <summary>
/// Base type for a single search result. Identity is always canonical — renderers derive display labels.
/// Concrete subtypes add kind-specific fields without nullable properties on the base.
/// </summary>
public abstract record SearchHit
{
    /// <summary>Gets the durable document identifier, derived from the source asset.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the connector instance that owns the source asset.
    /// Disambiguates two connectors indexing different roots (e.g. two filesystem connectors).</summary>
    public required ConnectorInstanceId ConnectorInstanceId { get; init; }

    /// <summary>Gets the universal locator for this document.
    /// Examples: <c>filesystem:///src/Program.cs</c>, <c>jira://ENG-1234</c>, <c>git://main/abc123</c>.
    /// Renderers derive human-friendly display labels from this value.</summary>
    public required Uri CanonicalUri { get; init; }

    /// <summary>Gets the renderer-derived human-friendly label for display.
    /// For filesystem hits, this is the relative file path; for JIRA hits, the issue key; etc.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the granularity of this hit.</summary>
    public required SearchHitKind Kind { get; init; }

    /// <summary>Gets the relevance score assigned by the provider.
    /// BM25 score in Sprint 10; vector similarity, hybrid score, or knowledge confidence in future sprints.</summary>
    public required float Score { get; init; }

    /// <summary>Gets the highlighted snippet for this hit.</summary>
    public required HighlightedText Snippet { get; init; }

    /// <summary>Gets the per-provider score breakdown. Null in Sprint 10; populated by Sprint 11+ providers.
    /// Example: "BM25: 0.91 | Semantic: 0.84 | Hybrid: 0.89".</summary>
    public string? Explanation { get; init; }
}

/// <summary>A file-level search hit — one result per matching file, best snippet surfaced.</summary>
public sealed record FileSearchHit : SearchHit;

/// <summary>A passage-level search hit — one result per matching passage within a file.
/// Returned when <see cref="SearchOptions.IncludePassages"/> is true (<c>--passages</c>).</summary>
public sealed record PassageSearchHit : SearchHit
{
    /// <summary>Gets the heading of the passage, if extracted by the parser. May be null.</summary>
    public string? Heading { get; init; }

    /// <summary>Gets the character offset where this passage begins within the document plain text.</summary>
    public int StartOffset { get; init; }

    /// <summary>Gets the character offset where this passage ends within the document plain text.</summary>
    public int EndOffset { get; init; }
}

/// <summary>Reserved: an AI processing unit (embedding chunk, notebook cell, AST node). Sprint 11+.</summary>
public sealed record SegmentSearchHit : SearchHit;
```

- [ ] **Step 4: Create `SearchResult.cs`**

`src/Ferret.Core/Search/SearchResult.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// The raw output of a single provider execution — hits ranked by score.
/// Wrapped in <see cref="SearchServiceResult"/> by <c>SearchService</c> before returning to callers.
/// </summary>
public sealed record SearchResult
{
    /// <summary>Gets the ranked hits returned by the provider.</summary>
    public required IReadOnlyList<SearchHit> Hits { get; init; }

    /// <summary>Gets the total number of matching documents in the index (may exceed <see cref="ReturnedHits"/>).</summary>
    public required int TotalHits { get; init; }

    /// <summary>Gets the number of hits actually returned (capped by <see cref="SearchOptions.MaxResults"/>).</summary>
    public required int ReturnedHits { get; init; }

    /// <summary>Gets a shared empty result — zero hits, used for no-match responses.</summary>
    public static SearchResult Empty { get; } = new() { Hits = [], TotalHits = 0, ReturnedHits = 0 };
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchHitTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 6: Service Model (`SearchProviderDescriptor`, `SearchCapabilities`, `SearchServiceStatus`, `SearchExecutionInfo`, `SearchServiceResult`)

**Files:**
- Create: `src/Ferret.Core/Search/SearchProviderDescriptor.cs`
- Create: `src/Ferret.Core/Search/SearchCapabilities.cs`
- Create: `src/Ferret.Core/Search/SearchServiceStatus.cs`
- Create: `src/Ferret.Core/Search/SearchExecutionInfo.cs`
- Create: `src/Ferret.Core/Search/SearchServiceResult.cs`
- Create: `tests/Ferret.Core.Tests/Search/SearchServiceModelTests.cs`

**Interfaces:**
- Consumes: `SearchQuery` (Task 3), `SearchResult` (Task 5), `SearchDiagnostic` (Task 3)
- Produces: service model types — consumed by Task 7 (`ISearchService`, `ISearchProvider`), S3 (`SearchService`), S5 (`SearchCommandHandler`)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Search/SearchServiceModelTests.cs`:

```csharp
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Core.Tests.Search;

public sealed class SearchServiceModelTests
{
    [Fact]
    public void SearchExecutionInfo_SessionId_Is_Guid()
    {
        var info = MakeExecutionInfo();
        Assert.IsType<Guid>(info.SessionId);
        Assert.NotEqual(Guid.Empty, info.SessionId);
    }

    [Fact]
    public void SearchExecutionInfo_Duration_Preserved()
    {
        var info = MakeExecutionInfo();
        Assert.Equal(TimeSpan.FromMilliseconds(27), info.Duration);
    }

    [Fact]
    public void SearchCapabilities_SupportsKeyword_True()
    {
        var caps = new SearchCapabilities
        {
            SupportsKeyword = true, SupportsPhrase = true, SupportsPrefix = true,
        };
        Assert.True(caps.SupportsKeyword);
    }

    [Fact]
    public void SearchCapabilities_SupportsSemantic_Defaults_To_False()
    {
        var caps = new SearchCapabilities
        {
            SupportsKeyword = true, SupportsPhrase = true, SupportsPrefix = true,
        };
        Assert.False(caps.SupportsSemantic);
        Assert.False(caps.SupportsHybrid);
    }

    [Fact]
    public void SearchServiceStatus_Has_Five_Values()
    {
        Assert.Equal(5, Enum.GetValues<SearchServiceStatus>().Length);
    }

    [Fact]
    public void SearchServiceStatus_Success_Is_Zero()
    {
        Assert.Equal(0, (int)SearchServiceStatus.Success);
    }

    [Fact]
    public void SearchServiceResult_Diagnostics_Defaults_To_Empty()
    {
        var result = MakeServiceResult();
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SearchServiceResult_ProviderDescriptor_May_Be_Null()
    {
        var result = MakeServiceResult() with { ProviderDescriptor = null };
        Assert.Null(result.ProviderDescriptor);
    }

    [Fact]
    public void SearchServiceResult_Result_May_Be_Null_When_Status_Is_Not_Success()
    {
        var result = MakeServiceResult() with
        {
            Status = SearchServiceStatus.IndexNotFound,
            Result = null,
        };
        Assert.Equal(SearchServiceStatus.IndexNotFound, result.Status);
        Assert.Null(result.Result);
    }

    private static SearchExecutionInfo MakeExecutionInfo() => new()
    {
        SessionId = Guid.NewGuid(),
        ProviderName = "BM25",
        Duration = TimeSpan.FromMilliseconds(27),
        DocumentsScanned = 150,
        IndexVersion = "1.0",
    };

    private static SearchServiceResult MakeServiceResult() => new()
    {
        Query = new SearchQuery { OriginalText = "auth", Root = new KeywordExpression("auth") },
        Result = SearchResult.Empty,
        Status = SearchServiceStatus.Success,
        ExecutionInfo = MakeExecutionInfo(),
    };
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchServiceModelTests"
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create `SearchProviderDescriptor.cs`**

`src/Ferret.Core/Search/SearchProviderDescriptor.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Static descriptor for a registered search provider type. Immutable.
/// Mirrors <c>ConnectorDescriptor</c> and <c>ParserDescriptor</c> in their respective platforms.
/// </summary>
public sealed record SearchProviderDescriptor
{
    /// <summary>Gets the unique provider identifier (e.g. "bm25", "semantic", "hybrid").</summary>
    public required string Id { get; init; }

    /// <summary>Gets the human-readable provider name for display.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the provider version string.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the capabilities this provider supports.</summary>
    public required SearchCapabilities Capabilities { get; init; }
}
```

- [ ] **Step 4: Create `SearchCapabilities.cs`**

`src/Ferret.Core/Search/SearchCapabilities.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Describes the search capabilities of a provider.
/// Sprint 10: BM25 providers set <see cref="SupportsKeyword"/>, <see cref="SupportsPhrase"/>,
/// and <see cref="SupportsPrefix"/>. Semantic and hybrid capabilities are reserved.
/// </summary>
public sealed record SearchCapabilities
{
    /// <summary>Gets a value indicating whether this provider supports keyword (BM25) search.</summary>
    public required bool SupportsKeyword { get; init; }

    /// <summary>Gets a value indicating whether this provider supports exact phrase matching.</summary>
    public required bool SupportsPhrase { get; init; }

    /// <summary>Gets a value indicating whether this provider supports prefix wildcard matching.</summary>
    public required bool SupportsPrefix { get; init; }

    /// <summary>Reserved: indicates embedding-based semantic similarity search. Sprint 11+.</summary>
    public bool SupportsSemantic { get; init; }

    /// <summary>Reserved: indicates hybrid (keyword + semantic) fusion search. Sprint 12+.</summary>
    public bool SupportsHybrid { get; init; }
}
```

- [ ] **Step 5: Create `SearchServiceStatus.cs`**

`src/Ferret.Core/Search/SearchServiceStatus.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Describes the outcome of an <see cref="ISearchService"/> request.
/// Expected environmental conditions are status codes, not exceptions.
/// Exceptions are reserved for genuine runtime failures (database corruption, unexpected I/O).
/// </summary>
public enum SearchServiceStatus
{
    /// <summary>Search completed successfully. <see cref="SearchServiceResult.Result"/> is populated.</summary>
    Success = 0,

    /// <summary>No <c>.ferret/</c> workspace was found in the current directory tree.
    /// CLI should print: "No workspace found. Run <c>ferret workspace init</c> first."</summary>
    WorkspaceNotFound = 1,

    /// <summary>The workspace exists but the keyword index file is absent or was never built.
    /// CLI should print: "No index found. Run <c>ferret index</c> first."</summary>
    IndexNotFound = 2,

    /// <summary>The requested <see cref="ExecutionMode"/> is not supported by any registered provider.
    /// Example: <see cref="ExecutionMode.Semantic"/> requested before Sprint 11 ships.</summary>
    ProviderUnavailable = 3,

    /// <summary>The raw query string could not be parsed into a valid AST. Diagnostics describe the error.</summary>
    InvalidQuery = 4,
}
```

- [ ] **Step 6: Create `SearchExecutionInfo.cs`**

`src/Ferret.Core/Search/SearchExecutionInfo.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Execution metadata for a single search request. Carried in <see cref="SearchServiceResult"/>
/// for telemetry, distributed tracing, dashboard history, and diagnostics.
/// </summary>
public sealed record SearchExecutionInfo
{
    /// <summary>Gets a unique identifier for this search execution.
    /// Generated per-request by <c>SearchService</c>. Used for telemetry and distributed tracing.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Gets the display name of the provider that executed the search.</summary>
    public required string ProviderName { get; init; }

    /// <summary>Gets the wall-clock duration of the search execution (parse + provider + post-process).</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Gets the number of index documents scanned by the provider.</summary>
    public required int DocumentsScanned { get; init; }

    /// <summary>Gets the version string of the index that was queried.</summary>
    public required string IndexVersion { get; init; }
}
```

- [ ] **Step 7: Create `SearchServiceResult.cs`**

`src/Ferret.Core/Search/SearchServiceResult.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// The complete output of an <see cref="ISearchService"/> request — results, status, execution metadata,
/// and diagnostics. The canonical output of the search pipeline; consumed by CLI handlers, MCP, REST, dashboards.
/// </summary>
public sealed record SearchServiceResult
{
    /// <summary>Gets the parsed query that drove this search. Always populated, even on failure.</summary>
    public required SearchQuery Query { get; init; }

    /// <summary>Gets the raw provider results. Null when <see cref="Status"/> is not <see cref="SearchServiceStatus.Success"/>.</summary>
    public SearchResult? Result { get; init; }

    /// <summary>Gets the outcome status of this request.</summary>
    public required SearchServiceStatus Status { get; init; }

    /// <summary>Gets the descriptor of the provider that executed the search. Null on pre-provider failure.</summary>
    public SearchProviderDescriptor? ProviderDescriptor { get; init; }

    /// <summary>Gets execution metadata (session ID, provider, duration, documents scanned, index version).</summary>
    public required SearchExecutionInfo ExecutionInfo { get; init; }

    /// <summary>Gets diagnostics from parsing or execution (warnings, errors, recovery hints).</summary>
    public IReadOnlyList<SearchDiagnostic> Diagnostics { get; init; } = [];
}
```

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "SearchServiceModelTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 7: `ISearchProvider` + `ISearchService`

**Files:**
- Create: `src/Ferret.Core/Search/ISearchProvider.cs`
- Create: `src/Ferret.Core/Search/ISearchService.cs`

**Interfaces:**
- Consumes: all types from Tasks 2–6
- Produces: `ISearchProvider`, `ISearchService` — consumed by S3 (`BM25SearchProvider`, `SearchService`), S5 (`SearchCommandHandler`)

No unit tests — these are pure interface contracts verified by `dotnet build`. S3 integration tests cover provider and service behaviour end-to-end.

- [ ] **Step 1: Create `ISearchProvider.cs`**

`src/Ferret.Core/Search/ISearchProvider.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// A search provider that executes queries against a backing store and returns ranked hits.
/// Implementations: <c>BM25SearchProvider</c> (Sprint 10), <c>SemanticSearchProvider</c> (Sprint 11),
/// <c>HybridSearchProvider</c> (Sprint 12).
/// All providers receive the same <see cref="SearchQuery"/> AST — no provider knows the original query string.
/// </summary>
public interface ISearchProvider
{
    /// <summary>Gets the static descriptor for this provider.</summary>
    SearchProviderDescriptor Descriptor { get; }

    /// <summary>Gets the capabilities this provider supports.</summary>
    SearchCapabilities Capabilities { get; }

    /// <summary>Executes the query against the backing store and returns ranked results.
    /// The provider translates the <see cref="SearchQuery"/> AST to backend-specific syntax internally.
    /// Never throws for expected conditions — return an empty <see cref="SearchResult"/> with status
    /// represented through <see cref="SearchServiceResult.Status"/> at the service level.</summary>
    /// <param name="query">The parsed query AST.</param>
    /// <param name="options">Execution options including limits, highlighting, and mode.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<SearchResult> SearchAsync(SearchQuery query, SearchOptions options, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create `ISearchService.cs`**

`src/Ferret.Core/Search/ISearchService.cs`:

```csharp
namespace Ferret.Core.Search;

/// <summary>
/// Orchestrates the full search pipeline: parse → validate → select provider → execute → post-process.
/// Exposes two overloads: a high-level string overload for CLI/MCP/REST callers, and a typed overload
/// for unit tests, benchmarks, AI agents, and future programmatic consumers.
/// The string overload parses and delegates to the typed overload — one implementation, no duplication.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Parses <paramref name="rawQuery"/> and executes a full search pipeline.
    /// Suitable for CLI, MCP, and REST callers that receive raw user input.
    /// </summary>
    /// <param name="rawQuery">The raw query string as entered by the user.</param>
    /// <param name="options">Execution options.</param>
    Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options);

    /// <summary>
    /// Executes a full search pipeline against a pre-parsed query.
    /// Suitable for unit tests, benchmarks, AI agents, saved searches, and programmatic consumers.
    /// </summary>
    /// <param name="query">The pre-parsed query AST.</param>
    /// <param name="options">Execution options.</param>
    Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options);
}
```

- [ ] **Step 3: Confirm green — full section**

```
dotnet build src/Ferret.sln
dotnet test tests/Ferret.Core.Tests
```

Expected: all tests pass (existing + new), 0 build errors.

- [ ] **Step 4: Commit Section 1**

```bash
git add src/Ferret.Core/Search/ tests/Ferret.Core.Tests/Search/
git commit -m "feat(sprint-10): Ferret.Core.Search — 20 contract types, 5 test files, 42 tests"
```

---

## Section 1 Complete

**Outputs of Section 1:**
- `Ferret.Core.Search` namespace — 20 new types across 20 files
- Highlight model: `TextSpanKind`, `TextSpan`, `HighlightedText`, `SearchHitKind`
- Query model: `SearchExpression` hierarchy (7 nodes, 4 reserved), `SearchQuery`, `SearchDiagnostic`, `SearchParseResult`, `IQueryParser`
- Execution config: `ExecutionMode`, `SearchOptions`
- Result model: `SearchHit` hierarchy (`FileSearchHit`, `PassageSearchHit`, `SegmentSearchHit` reserved), `SearchResult`
- Service model: `SearchProviderDescriptor`, `SearchCapabilities`, `SearchServiceStatus`, `SearchExecutionInfo`, `SearchServiceResult`
- Provider contracts: `ISearchProvider`, `ISearchService`
- 5 test files, ~42 tests
- ADR-0015 committed before all code

**What Section 2 (Query Parser) depends on from here:**
- `IQueryParser` — implements this interface in `Ferret.Search`
- `SearchExpression` hierarchy — `QueryParser` emits `KeywordExpression`, `PhraseExpression`, `PrefixExpression`, `AndExpression`
- `SearchQuery` — `QueryParser` constructs and returns this
- `SearchParseResult` — `QueryParser` returns this for all outcomes
- `SearchDiagnostic`, `SearchDiagnosticSeverity` — `QueryParser` populates on failure
