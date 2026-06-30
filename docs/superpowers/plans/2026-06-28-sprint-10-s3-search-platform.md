# Sprint 10 — Section 3: Search Platform (`BM25SearchProvider` + `SearchService`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Deliver the working search engine: `BM25SearchProvider` translates the `SearchQuery` AST from Section 2 into FTS5 SQL, executes it against the Sprint 9 keyword index, and builds `SearchHit` results with highlighted snippets. `SearchService` orchestrates providers and post-processors behind `ISearchService`. After this section, `ferret search` has a callable backend.

**Architecture:** Three internal components — `QueryTranslator` (AST → FTS5 string), `HighlightParser` (sentinel markers → `HighlightedText`) — support `BM25SearchProvider`. `SearchService` injects `IEnumerable<ISearchProvider>` and `IEnumerable<ISearchPostProcessor>` via DI; Sprint 10 ships one provider (`BM25SearchProvider`) and zero post-processors. ADR-0015 rule: "The query parser never generates SQLite syntax" — only `QueryTranslator` generates SQL query strings.

**Tech stack:** .NET 9 / C# 13, `Microsoft.Data.Sqlite`, `System.Diagnostics.Stopwatch`, xUnit.

---

## Prerequisites

- Section 1 complete: all 20 `Ferret.Core.Search` contracts compiled and tested, including:
  - `ISearchProvider`, `SearchProviderResult`, `ISearchService`, `SearchServiceResult`, `SearchExecutionInfo`
  - `SearchHit`, `FileSearchHit`, `HighlightedText`, `TextSpan`, `TextSpanKind`
  - `SearchQuery`, `SearchOptions`, `SearchExecutionMode`
  - `ISearchPostProcessor`
- Section 2 complete: `QueryParser` and `Lexer` in `Ferret.Search`, 32 tests green
- Sprint 9 complete: `keyword-index.db` exists at `.ferret/indexes/keyword/keyword-index.db` with schema:
  ```sql
  CREATE TABLE documents (
      id                  TEXT PRIMARY KEY,
      asset_id            TEXT NOT NULL,
      connector_instance_id TEXT NOT NULL,
      canonical_uri       TEXT NOT NULL,
      title               TEXT NOT NULL,
      ...
  );
  CREATE VIRTUAL TABLE documents_fts USING fts5(
      title,
      body,
      content='documents',
      content_rowid='rowid'
  );
  ```
- `IWorkspaceContext` available in `Ferret.Core.Workspace`
- `ConnectorInstanceId` accessible (from `Ferret.ConnectorPlatform` or `Ferret.Core.Connectors` — verify at implementation time)

---

## Global Constraints

- `QueryTranslator` and `HighlightParser` are `internal static` — never exposed outside `Ferret.Search`
- `BM25SearchProvider` and `SearchService` are `public sealed` — registered via DI
- Parser never generates SQLite syntax — only `QueryTranslator` does (ADR-0015 principle 3)
- Provider never parses raw query strings — only `SearchService` calls `IQueryParser` (ADR-0015 principle 1)
- `BM25SearchProvider` opens the SQLite database read-only (`SqliteOpenMode.ReadOnly`)
- FTS5 `rank` values are negative — negate to produce positive scores in `SearchHit.Score`
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-10):`, `test(sprint-10):`

---

## File Inventory

### Modified Files

| File | Change |
|---|---|
| `src/Ferret.Search/Ferret.Search.csproj` | Add `Microsoft.Data.Sqlite` package reference |

### New Source Files

| File | Access | Purpose |
|---|---|---|
| `src/Ferret.Search/Providers/Bm25/QueryTranslator.cs` | `internal static` | `SearchExpression` → FTS5 query string |
| `src/Ferret.Search/Providers/Bm25/HighlightParser.cs` | `internal static` | Sentinel marker string → `HighlightedText` |
| `src/Ferret.Search/Providers/Bm25/Bm25SearchProvider.cs` | `public sealed` | `ISearchProvider` implementation |
| `src/Ferret.Search/SearchService.cs` | `public sealed` | `ISearchService` implementation |

### New Test Files

| File | Tests |
|---|---|
| `tests/Ferret.Search.Tests/Providers/Bm25/QueryTranslatorTests.cs` | 12 |
| `tests/Ferret.Search.Tests/Providers/Bm25/HighlightParserTests.cs` | 10 |
| `tests/Ferret.Search.Tests/Providers/Bm25/Bm25SearchProviderTests.cs` | 8 |
| `tests/Ferret.Search.Tests/SearchServiceTests.cs` | 10 |

---

## Task 1: Setup — SQLite Package + Directory Scaffold

**Files:**
- Modify: `src/Ferret.Search/Ferret.Search.csproj`

**Interfaces:**
- Produces: `Microsoft.Data.Sqlite` types available in `Ferret.Search`

- [ ] **Step 1: Add `Microsoft.Data.Sqlite` to `Ferret.Search`**

```
dotnet add src/Ferret.Search/Ferret.Search.csproj package Microsoft.Data.Sqlite
```

Verify the version added is compatible with .NET 9 (9.0.x). Open `Ferret.Search.csproj` and confirm the `<PackageReference>` appears.

- [ ] **Step 2: Create subdirectories**

```
mkdir src\Ferret.Search\Providers
mkdir src\Ferret.Search\Providers\Bm25
mkdir tests\Ferret.Search.Tests\Providers
mkdir tests\Ferret.Search.Tests\Providers\Bm25
```

- [ ] **Step 3: Verify build still passes**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors.

---

## Task 2: `QueryTranslator` + `HighlightParser` (internal)

**Files:**
- Create: `src/Ferret.Search/Providers/Bm25/QueryTranslator.cs`
- Create: `src/Ferret.Search/Providers/Bm25/HighlightParser.cs`
- Create: `tests/Ferret.Search.Tests/Providers/Bm25/QueryTranslatorTests.cs`
- Create: `tests/Ferret.Search.Tests/Providers/Bm25/HighlightParserTests.cs`

**Interfaces:**
- Consumes: `SearchExpression` hierarchy from `Ferret.Core.Search` (Section 1); `HighlightedText`, `TextSpan`, `TextSpanKind` from `Ferret.Core.Search` (Section 1)
- Produces: `QueryTranslator.Translate(SearchExpression)` → `string`; `HighlightParser.Parse(string)` → `HighlightedText`

### 2a — QueryTranslator

- [ ] **Step 1: Write failing `QueryTranslatorTests`**

Create `tests/Ferret.Search.Tests/Providers/Bm25/QueryTranslatorTests.cs`:

```csharp
using Ferret.Core.Search;
using Ferret.Search.Providers.Bm25;
using Xunit;

namespace Ferret.Search.Tests.Providers.Bm25;

public sealed class QueryTranslatorTests
{
    [Fact]
    public void Keyword_Translates_To_Bare_Word()
    {
        var result = QueryTranslator.Translate(new KeywordExpression("authentication"));
        Assert.Equal("authentication", result);
    }

    [Fact]
    public void Phrase_Translates_To_Double_Quoted_String()
    {
        var result = QueryTranslator.Translate(new PhraseExpression("runtime builder"));
        Assert.Equal("\"runtime builder\"", result);
    }

    [Fact]
    public void Prefix_Translates_To_Word_With_Asterisk()
    {
        var result = QueryTranslator.Translate(new PrefixExpression("auth"));
        Assert.Equal("auth*", result);
    }

    [Fact]
    public void EmptyPrefix_Translates_To_Bare_Asterisk()
    {
        var result = QueryTranslator.Translate(new PrefixExpression(string.Empty));
        Assert.Equal("*", result);
    }

    [Fact]
    public void AndExpression_Joins_Terms_With_Space()
    {
        var expr = new AndExpression([
            new KeywordExpression("authentication"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("authentication token", result);
    }

    [Fact]
    public void AndExpression_With_Phrase_And_Keyword()
    {
        var expr = new AndExpression([
            new PhraseExpression("context window"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("\"context window\" token", result);
    }

    [Fact]
    public void AndExpression_With_Prefix_And_Keyword()
    {
        var expr = new AndExpression([
            new PrefixExpression("auth"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("auth* token", result);
    }

    [Fact]
    public void Phrase_With_Inner_Quotes_Doubles_Them()
    {
        var result = QueryTranslator.Translate(new PhraseExpression("say \"hello\""));
        Assert.Equal("\"say \"\"hello\"\"\"", result);
    }

    [Fact]
    public void Keyword_That_Matches_Fts5_Reserved_Word_Is_Quoted()
    {
        // "AND" is a reserved FTS5 operator — must be quoted to search for the literal word
        var result = QueryTranslator.Translate(new KeywordExpression("AND"));
        Assert.Equal("\"AND\"", result);
    }

    [Fact]
    public void Keyword_NOT_Is_Quoted()
    {
        var result = QueryTranslator.Translate(new KeywordExpression("NOT"));
        Assert.Equal("\"NOT\"", result);
    }

    [Fact]
    public void Keyword_OR_Is_Quoted()
    {
        var result = QueryTranslator.Translate(new KeywordExpression("OR"));
        Assert.Equal("\"OR\"", result);
    }

    [Fact]
    public void ThreeTerm_And_Produces_Space_Separated_String()
    {
        var expr = new AndExpression([
            new PhraseExpression("runtime builder"),
            new PrefixExpression("auth"),
            new KeywordExpression("token"),
        ]);
        var result = QueryTranslator.Translate(expr);
        Assert.Equal("\"runtime builder\" auth* token", result);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Search.Tests --filter "QueryTranslatorTests"
```

Expected: FAIL — `Ferret.Search.Providers.Bm25` namespace not found.

- [ ] **Step 3: Create `QueryTranslator.cs`**

`src/Ferret.Search/Providers/Bm25/QueryTranslator.cs`:

```csharp
using Ferret.Core.Search;

namespace Ferret.Search.Providers.Bm25;

/// <summary>
/// Translates a <see cref="SearchExpression"/> AST into an FTS5 query string.
/// This is the ONLY place in <c>Ferret.Search</c> that produces SQLite/FTS5 syntax (ADR-0015, principle 3).
/// </summary>
internal static class QueryTranslator
{
    private static readonly HashSet<string> Fts5ReservedWords =
        new(StringComparer.OrdinalIgnoreCase) { "AND", "OR", "NOT", "NEAR" };

    /// <summary>Translates a <see cref="SearchExpression"/> to an FTS5 MATCH argument string.</summary>
    internal static string Translate(SearchExpression expression) =>
        expression switch
        {
            KeywordExpression { Value: var v } => EscapeKeyword(v),
            PhraseExpression { Value: var v } => $"\"{v.Replace("\"", "\"\"")}\"",
            PrefixExpression { Prefix: var p } when p.Length == 0 => "*",
            PrefixExpression { Prefix: var p } => $"{EscapeKeyword(p)}*",
            AndExpression { Operands: var operands } =>
                string.Join(" ", operands.Select(Translate)),
            _ => throw new InvalidOperationException(
                $"Unsupported expression type '{expression.GetType().Name}' — not supported in Sprint 10."),
        };

    private static string EscapeKeyword(string value)
    {
        // FTS5 reserved words must be double-quoted to search for their literal text.
        if (Fts5ReservedWords.Contains(value))
        {
            return $"\"{value}\"";
        }

        // Keywords containing non-alphanumeric characters (other than _ and -) are also quoted.
        return value.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Search.Tests --filter "QueryTranslatorTests"
```

Expected: 12 tests pass.

### 2b — HighlightParser

- [ ] **Step 5: Write failing `HighlightParserTests`**

Create `tests/Ferret.Search.Tests/Providers/Bm25/HighlightParserTests.cs`:

```csharp
using Ferret.Core.Search;
using Ferret.Search.Providers.Bm25;
using Xunit;

namespace Ferret.Search.Tests.Providers.Bm25;

public sealed class HighlightParserTests
{
    // Sentinel constants (same as HighlightParser internals)
    private const char Open = '\x02';
    private const char Close = '\x03';

    [Fact]
    public void Plain_Text_Produces_Single_Normal_Span()
    {
        var ht = HighlightParser.Parse("hello world");
        Assert.Single(ht.Spans);
        Assert.Equal("hello world", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
    }

    [Fact]
    public void Match_Sentinel_Produces_Match_Span()
    {
        var ht = HighlightParser.Parse($"{Open}auth{Close}");
        Assert.Single(ht.Spans);
        Assert.Equal("auth", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[0].Kind);
    }

    [Fact]
    public void Normal_Then_Match_Produces_Two_Spans()
    {
        var ht = HighlightParser.Parse($"before {Open}auth{Close}");
        Assert.Equal(2, ht.Spans.Count);
        Assert.Equal("before ", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
        Assert.Equal("auth", ht.Spans[1].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[1].Kind);
    }

    [Fact]
    public void Match_Then_Normal_Produces_Two_Spans()
    {
        var ht = HighlightParser.Parse($"{Open}auth{Close} token");
        Assert.Equal(2, ht.Spans.Count);
        Assert.Equal("auth", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[0].Kind);
        Assert.Equal(" token", ht.Spans[1].Text);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[1].Kind);
    }

    [Fact]
    public void Normal_Match_Normal_Produces_Three_Spans()
    {
        var ht = HighlightParser.Parse($"before {Open}auth{Close} after");
        Assert.Equal(3, ht.Spans.Count);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
        Assert.Equal(TextSpanKind.Match, ht.Spans[1].Kind);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[2].Kind);
    }

    [Fact]
    public void Multiple_Matches_Produce_Correct_Span_Sequence()
    {
        var ht = HighlightParser.Parse($"a {Open}b{Close} c {Open}d{Close} e");
        Assert.Equal(5, ht.Spans.Count);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[0].Kind);
        Assert.Equal(TextSpanKind.Match, ht.Spans[1].Kind);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[2].Kind);
        Assert.Equal(TextSpanKind.Match, ht.Spans[3].Kind);
        Assert.Equal(TextSpanKind.Normal, ht.Spans[4].Kind);
    }

    [Fact]
    public void Empty_Input_Produces_Empty_Spans()
    {
        var ht = HighlightParser.Parse(string.Empty);
        Assert.Empty(ht.Spans);
    }

    [Fact]
    public void Ellipsis_In_Snippet_Is_Treated_As_Normal_Text()
    {
        var ht = HighlightParser.Parse($"...before {Open}auth{Close} after...");
        Assert.Equal(3, ht.Spans.Count);
        Assert.Equal("...before ", ht.Spans[0].Text);
        Assert.Equal("auth", ht.Spans[1].Text);
        Assert.Equal(" after...", ht.Spans[2].Text);
    }

    [Fact]
    public void Match_Span_Text_Does_Not_Include_Sentinels()
    {
        var ht = HighlightParser.Parse($"{Open}authentication{Close}");
        Assert.Equal("authentication", ht.Spans[0].Text);
        Assert.DoesNotContain("\x02", ht.Spans[0].Text);
        Assert.DoesNotContain("\x03", ht.Spans[0].Text);
    }

    [Fact]
    public void Match_Value_Is_Preserved_Including_Spaces()
    {
        var ht = HighlightParser.Parse($"{Open}runtime builder{Close}");
        Assert.Single(ht.Spans);
        Assert.Equal("runtime builder", ht.Spans[0].Text);
        Assert.Equal(TextSpanKind.Match, ht.Spans[0].Kind);
    }
}
```

- [ ] **Step 6: Confirm red**

```
dotnet test tests/Ferret.Search.Tests --filter "HighlightParserTests"
```

Expected: FAIL — `HighlightParser` not found.

- [ ] **Step 7: Create `HighlightParser.cs`**

`src/Ferret.Search/Providers/Bm25/HighlightParser.cs`:

```csharp
using System.Text;
using Ferret.Core.Search;

namespace Ferret.Search.Providers.Bm25;

/// <summary>
/// Converts an FTS5 <c>snippet()</c> output — containing sentinel characters marking match boundaries —
/// into a <see cref="HighlightedText"/> span list.
/// FTS5 snippet format: text + <c>char(2)</c> (STX) opens a match, <c>char(3)</c> (ETX) closes it.
/// </summary>
internal static class HighlightParser
{
    /// <summary>STX character used as open-match sentinel in FTS5 <c>snippet()</c> calls.</summary>
    internal const char MatchOpen = '\x02';

    /// <summary>ETX character used as close-match sentinel in FTS5 <c>snippet()</c> calls.</summary>
    internal const char MatchClose = '\x03';

    /// <summary>Parses a sentinel-delimited FTS5 snippet string into a <see cref="HighlightedText"/>.</summary>
    internal static HighlightedText Parse(string snippet)
    {
        if (snippet.Length == 0)
        {
            return new HighlightedText([]);
        }

        var spans = new List<TextSpan>();
        var buffer = new StringBuilder();
        var inMatch = false;

        foreach (var ch in snippet)
        {
            if (ch == MatchOpen)
            {
                if (buffer.Length > 0)
                {
                    spans.Add(new TextSpan(buffer.ToString(), TextSpanKind.Normal));
                    buffer.Clear();
                }

                inMatch = true;
            }
            else if (ch == MatchClose)
            {
                if (buffer.Length > 0)
                {
                    spans.Add(new TextSpan(buffer.ToString(), TextSpanKind.Match));
                    buffer.Clear();
                }

                inMatch = false;
            }
            else
            {
                buffer.Append(ch);
            }
        }

        if (buffer.Length > 0)
        {
            spans.Add(new TextSpan(buffer.ToString(), inMatch ? TextSpanKind.Match : TextSpanKind.Normal));
        }

        return new HighlightedText(spans);
    }
}
```

- [ ] **Step 8: Confirm green**

```
dotnet test tests/Ferret.Search.Tests --filter "HighlightParserTests"
dotnet build src/Ferret.sln
```

Expected: 10 `HighlightParserTests` pass, 0 build errors.

---

## Task 3: `BM25SearchProvider`

**Files:**
- Create: `src/Ferret.Search/Providers/Bm25/Bm25SearchProvider.cs`
- Create: `tests/Ferret.Search.Tests/Providers/Bm25/Bm25SearchProviderTests.cs`

**Interfaces:**
- Consumes: `QueryTranslator`, `HighlightParser` (Task 2); `ISearchProvider`, `SearchProviderResult`, `SearchQuery`, `SearchOptions`, `SearchExecutionMode`, `FileSearchHit`, `SearchHitKind`, `SearchServiceStatus` from `Ferret.Core.Search` (Section 1); `IWorkspaceContext` from `Ferret.Core.Workspace` (Sprint 9); `DocumentId` from `Ferret.Core.Primitives`; `ConnectorInstanceId` from `Ferret.ConnectorPlatform` or `Ferret.Core.Connectors` (verify at implementation time)
- Produces: `BM25SearchProvider` — registered as `ISearchProvider` in DI; consumed by `SearchService` (Task 4)

- [ ] **Step 1: Write failing `BM25SearchProviderTests`**

Create `tests/Ferret.Search.Tests/Providers/Bm25/Bm25SearchProviderTests.cs`:

```csharp
using System.Diagnostics;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Search.Providers.Bm25;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Search.Tests.Providers.Bm25;

public sealed class Bm25SearchProviderTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _dbPath;
    private readonly Bm25SearchProvider _provider;

    public Bm25SearchProviderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ferret-test-{Guid.NewGuid():N}");
        var indexDir = Path.Combine(_tempRoot, ".ferret", "indexes", "keyword");
        Directory.CreateDirectory(indexDir);
        _dbPath = Path.Combine(indexDir, "keyword-index.db");

        SeedDatabase(_dbPath);
        _provider = new Bm25SearchProvider(new StubWorkspaceContext(_tempRoot));
    }

    // ── CanExecute ────────────────────────────────────────────────────────────

    [Fact]
    public void CanExecute_Returns_True_For_Keyword_Mode()
    {
        var query = MakeQuery(new KeywordExpression("auth"));
        var options = new SearchOptions { ExecutionMode = SearchExecutionMode.Keyword };
        Assert.True(_provider.CanExecute(query, options));
    }

    [Fact]
    public void CanExecute_Returns_True_For_Auto_Mode()
    {
        var query = MakeQuery(new KeywordExpression("auth"));
        var options = new SearchOptions { ExecutionMode = SearchExecutionMode.Auto };
        Assert.True(_provider.CanExecute(query, options));
    }

    [Fact]
    public void CanExecute_Returns_False_For_Semantic_Mode()
    {
        var query = MakeQuery(new KeywordExpression("auth"));
        var options = new SearchOptions { ExecutionMode = SearchExecutionMode.Semantic };
        Assert.False(_provider.CanExecute(query, options));
    }

    // ── SearchAsync: success cases ────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_Single_Keyword_Returns_Matching_Hits()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var options = DefaultOptions();

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Hits);
        Assert.All(result.Hits, h => Assert.False(string.IsNullOrEmpty(h.DisplayName)));
    }

    [Fact]
    public async Task SearchAsync_Score_Is_Positive()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var options = DefaultOptions();

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        Assert.All(result.Hits, h => Assert.True(h.Score > 0f));
    }

    [Fact]
    public async Task SearchAsync_Snippet_Has_At_Least_One_Span()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var options = DefaultOptions();

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        Assert.All(result.Hits, h => Assert.NotEmpty(h.Snippet.Spans));
    }

    [Fact]
    public async Task SearchAsync_Snippet_Contains_Match_Span_For_Query_Term()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var options = DefaultOptions();

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        var firstHit = result.Hits[0];
        Assert.Contains(firstHit.Snippet.Spans, s => s.Kind == TextSpanKind.Match);
    }

    [Fact]
    public async Task SearchAsync_Returns_FileSearchHit_Type()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var options = DefaultOptions();

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        Assert.All(result.Hits, h => Assert.IsType<FileSearchHit>(h));
    }

    // ── SearchAsync: missing index ─────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_Returns_IndexNotFound_When_Database_Missing()
    {
        File.Delete(_dbPath);
        var query = MakeQuery(new KeywordExpression("auth"));
        var options = DefaultOptions();

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.IndexNotFound, result.Status);
    }

    // ── MaxResults ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_Respects_MaxResults_Limit()
    {
        // Seed extra docs so we have more than 1
        var query = MakeQuery(new KeywordExpression("content"));
        var options = new SearchOptions { MaxResults = 1, ExecutionMode = SearchExecutionMode.Keyword };

        var result = await _provider.SearchAsync(query, options, CancellationToken.None);

        Assert.True(result.Hits.Count <= 1);
    }

    // ── Provider name ─────────────────────────────────────────────────────────

    [Fact]
    public void Name_Is_Non_Empty()
    {
        Assert.False(string.IsNullOrEmpty(_provider.Name));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchQuery MakeQuery(SearchExpression root) =>
        new() { OriginalText = "test", Root = root };

    private static SearchOptions DefaultOptions() =>
        new() { MaxResults = 20, ExecutionMode = SearchExecutionMode.Keyword };

    private static void SeedDatabase(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE documents (
                id                    TEXT PRIMARY KEY,
                asset_id              TEXT NOT NULL,
                connector_instance_id TEXT NOT NULL DEFAULT '',
                canonical_uri         TEXT NOT NULL DEFAULT '',
                title                 TEXT NOT NULL DEFAULT ''
            );
            CREATE VIRTUAL TABLE documents_fts USING fts5(
                title,
                body,
                content='documents',
                content_rowid='rowid'
            );
            """;
        cmd.ExecuteNonQuery();

        cmd.CommandText = """
            INSERT INTO documents (id, asset_id, connector_instance_id, canonical_uri, title)
            VALUES
                ('doc-1', 'asset-1', 'filesystem', 'file:///src/auth/token.cs', 'AuthenticationToken'),
                ('doc-2', 'asset-2', 'filesystem', 'file:///src/auth/session.cs', 'SessionManager'),
                ('doc-3', 'asset-3', 'filesystem', 'file:///src/runtime/builder.cs', 'RuntimeBuilder');
            INSERT INTO documents_fts (rowid, title, body)
            SELECT rowid, title,
                CASE id
                    WHEN 'doc-1' THEN 'Token-based authentication content here'
                    WHEN 'doc-2' THEN 'Session management for authenticated users'
                    WHEN 'doc-3' THEN 'Builder content for runtime initialization'
                END
            FROM documents;
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); }
        catch (IOException) { /* best-effort cleanup */ }
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private sealed class StubWorkspaceContext : IWorkspaceContext
    {
        public StubWorkspaceContext(string rootPath) =>
            WorkspaceRoot = new DirectoryInfo(rootPath);

        public WorkspaceId WorkspaceId => new WorkspaceId("test-workspace");

        public DirectoryInfo WorkspaceRoot { get; }
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Search.Tests --filter "Bm25SearchProviderTests"
```

Expected: FAIL — `Bm25SearchProvider` not found.

- [ ] **Step 3: Create `Bm25SearchProvider.cs`**

`src/Ferret.Search/Providers/Bm25/Bm25SearchProvider.cs`:

```csharp
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Microsoft.Data.Sqlite;

namespace Ferret.Search.Providers.Bm25;

/// <summary>
/// BM25 keyword search provider backed by SQLite FTS5.
/// Reads from the keyword index produced by Sprint 9 at <c>.ferret/indexes/keyword/keyword-index.db</c>.
/// Supports <see cref="SearchExecutionMode.Keyword"/> and <see cref="SearchExecutionMode.Auto"/>.
/// </summary>
public sealed class Bm25SearchProvider : ISearchProvider
{
    /// <inheritdoc/>
    public string Name => "bm25-fts5";

    private readonly IWorkspaceContext _workspace;

    /// <summary>Initialises a new <see cref="Bm25SearchProvider"/>.</summary>
    public Bm25SearchProvider(IWorkspaceContext workspace)
    {
        _workspace = workspace;
    }

    /// <inheritdoc/>
    public bool CanExecute(SearchQuery query, SearchOptions options) =>
        options.ExecutionMode is SearchExecutionMode.Keyword or SearchExecutionMode.Auto;

    /// <inheritdoc/>
    public async Task<SearchProviderResult> SearchAsync(
        SearchQuery query, SearchOptions options, CancellationToken cancellationToken)
    {
        var dbPath = GetDatabasePath();

        if (!File.Exists(dbPath))
        {
            return SearchProviderResult.Failure(SearchServiceStatus.IndexNotFound);
        }

        try
        {
            return await ExecuteAsync(dbPath, query, options, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // SQLITE_ERROR — bad FTS5 syntax
        {
            return SearchProviderResult.Failure(SearchServiceStatus.InvalidQuery);
        }
    }

    private string GetDatabasePath() =>
        Path.Combine(_workspace.WorkspaceRoot.FullName, ".ferret", "indexes", "keyword", "keyword-index.db");

    private static async Task<SearchProviderResult> ExecuteAsync(
        string dbPath, SearchQuery query, SearchOptions options, CancellationToken cancellationToken)
    {
        var ftsQuery = QueryTranslator.Translate(query.Root);

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString();

        await using var connection = new SqliteConnection(cs);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT
                d.id,
                d.connector_instance_id,
                d.canonical_uri,
                d.title,
                snippet(documents_fts, 1, char(2), char(3), '...', 15) AS snippet,
                documents_fts.rank
            FROM documents_fts
            JOIN documents d ON d.rowid = documents_fts.rowid
            WHERE documents_fts MATCH @query
            ORDER BY documents_fts.rank
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@query", ftsQuery);
        cmd.Parameters.AddWithValue("@limit", options.MaxResults);

        var hits = new List<SearchHit>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            hits.Add(BuildHit(reader));
        }

        return SearchProviderResult.Success(hits, documentsScanned: hits.Count, indexVersion: "fts5");
    }

    private static FileSearchHit BuildHit(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var connectorId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var uri = reader.GetString(2);
        var title = reader.GetString(3);
        var snippetText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
        var rank = reader.GetDouble(5);

        return new FileSearchHit
        {
            DocumentId = DocumentId.Parse(id),          // verify API — may be new DocumentId(id) or DocumentId.From(id)
            ConnectorInstanceId = new ConnectorInstanceId(connectorId),
            CanonicalUri = new Uri(string.IsNullOrEmpty(uri) ? "file:///unknown" : uri),
            DisplayName = title,
            Kind = SearchHitKind.File,
            Score = (float)-rank,   // FTS5 rank is negative; negate to positive
            Snippet = HighlightParser.Parse(snippetText),
        };
    }
}
```

> **Implementation note:** `DocumentId.Parse(id)` — verify the exact factory method against Sprint 9 code. It may be `new DocumentId(id)`, `DocumentId.From(id)`, or similar. Adjust as needed. Same for `ConnectorInstanceId` — verify constructor signature.

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Search.Tests --filter "Bm25SearchProviderTests"
dotnet build src/Ferret.sln
```

Expected: 9 tests pass (8 + `Name_Is_Non_Empty`), 0 build errors.

---

## Task 4: `SearchService`

**Files:**
- Create: `src/Ferret.Search/SearchService.cs`
- Create: `tests/Ferret.Search.Tests/SearchServiceTests.cs`

**Interfaces:**
- Consumes: `ISearchProvider`, `SearchProviderResult`, `ISearchService`, `SearchServiceResult`, `SearchServiceStatus`, `SearchExecutionInfo`, `IQueryParser`, `SearchParseResult`, `ISearchPostProcessor` from `Ferret.Core.Search` (Section 1); `QueryParser` from `Ferret.Search` (Section 2)
- Produces: `SearchService` — registered as `ISearchService` in DI; consumed by S5 `SearchCommandHandler`

- [ ] **Step 1: Write failing `SearchServiceTests`**

Create `tests/Ferret.Search.Tests/SearchServiceTests.cs`:

```csharp
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Search.Tests;

public sealed class SearchServiceTests
{
    // ── String overload: parse failure path ──────────────────────────────────

    [Fact]
    public async Task SearchAsync_String_EmptyQuery_Returns_InvalidQuery_Status()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);

        var result = await service.SearchAsync(string.Empty, DefaultOptions());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.InvalidQuery, result.Status);
    }

    [Fact]
    public async Task SearchAsync_String_WhitespaceQuery_Returns_InvalidQuery_Status()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);

        var result = await service.SearchAsync("   ", DefaultOptions());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.InvalidQuery, result.Status);
    }

    // ── No provider ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_NoProviders_Returns_ProviderUnavailable_Status()
    {
        var service = MakeService([]);  // empty provider list

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.ProviderUnavailable, result.Status);
    }

    [Fact]
    public async Task SearchAsync_AllProvidersRefuse_Returns_ProviderUnavailable_Status()
    {
        var service = MakeService([new NeverExecuteProvider()]);

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.ProviderUnavailable, result.Status);
    }

    // ── Success path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ReturnsSuccess_When_Provider_Succeeds()
    {
        var hit = MakeHit("doc-1");
        var service = MakeService([new AlwaysSucceedProvider([hit])]);

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.True(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.Success, result.Status);
    }

    [Fact]
    public async Task SearchAsync_Hits_Match_Provider_Output()
    {
        var hit = MakeHit("doc-42");
        var service = MakeService([new AlwaysSucceedProvider([hit])]);

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.Single(result.Hits);
        Assert.Equal("doc-42", result.Hits[0].DocumentId.ToString());
    }

    [Fact]
    public async Task SearchAsync_ExecutionInfo_Is_Populated_On_Success()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.NotNull(result.ExecutionInfo);
        Assert.False(string.IsNullOrEmpty(result.ExecutionInfo!.ProviderName));
        Assert.NotEqual(Guid.Empty, result.ExecutionInfo.SessionId);
    }

    [Fact]
    public async Task SearchAsync_Duration_Is_Non_Negative()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.True(result.ExecutionInfo!.Duration >= TimeSpan.Zero);
    }

    // ── Post-processor ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_PostProcessor_Can_Filter_Hits()
    {
        var hits = new[] { MakeHit("keep"), MakeHit("drop") };
        var service = MakeService(
            [new AlwaysSucceedProvider(hits)],
            [new RemoveHitProcessor("drop")]);

        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());

        Assert.Single(result.Hits);
        Assert.Equal("keep", result.Hits[0].DocumentId.ToString());
    }

    [Fact]
    public async Task SearchAsync_String_Overload_Success_Populates_Hits()
    {
        var hit = MakeHit("doc-1");
        var service = MakeService([new AlwaysSucceedProvider([hit])]);

        var result = await service.SearchAsync("authentication", DefaultOptions());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Hits);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ISearchService MakeService(
        IReadOnlyList<ISearchProvider> providers,
        IReadOnlyList<ISearchPostProcessor>? postProcessors = null) =>
        new SearchService(new QueryParser(), providers, postProcessors ?? []);

    private static SearchQuery MakeQuery(string keyword) =>
        new() { OriginalText = keyword, Root = new KeywordExpression(keyword) };

    private static SearchOptions DefaultOptions() =>
        new() { MaxResults = 20, ExecutionMode = SearchExecutionMode.Auto };

    private static FileSearchHit MakeHit(string id) =>
        new()
        {
            DocumentId = DocumentId.Parse(id),
            ConnectorInstanceId = new ConnectorInstanceId(string.Empty),
            CanonicalUri = new Uri($"file:///{id}"),
            DisplayName = id,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = new HighlightedText([new TextSpan(id, TextSpanKind.Normal)]),
        };

    // ── Stub providers / processors ───────────────────────────────────────────

    private sealed class AlwaysSucceedProvider : ISearchProvider
    {
        private readonly IReadOnlyList<SearchHit> _hits;

        public AlwaysSucceedProvider(IReadOnlyList<SearchHit>? hits = null) =>
            _hits = hits ?? [];

        public string Name => "stub-success";
        public bool CanExecute(SearchQuery query, SearchOptions options) => true;
        public Task<SearchProviderResult> SearchAsync(
            SearchQuery query, SearchOptions options, CancellationToken ct) =>
            Task.FromResult(SearchProviderResult.Success(_hits, documentsScanned: _hits.Count, indexVersion: "stub"));
    }

    private sealed class NeverExecuteProvider : ISearchProvider
    {
        public string Name => "stub-never";
        public bool CanExecute(SearchQuery query, SearchOptions options) => false;
        public Task<SearchProviderResult> SearchAsync(
            SearchQuery query, SearchOptions options, CancellationToken ct) =>
            throw new InvalidOperationException("Should not be called.");
    }

    private sealed class RemoveHitProcessor : ISearchPostProcessor
    {
        private readonly string _documentIdToRemove;

        public RemoveHitProcessor(string documentIdToRemove) =>
            _documentIdToRemove = documentIdToRemove;

        public Task<IReadOnlyList<SearchHit>> ProcessAsync(
            IReadOnlyList<SearchHit> hits, SearchQuery query, SearchOptions options)
        {
            IReadOnlyList<SearchHit> filtered =
                hits.Where(h => h.DocumentId.ToString() != _documentIdToRemove).ToList();
            return Task.FromResult(filtered);
        }
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Search.Tests --filter "SearchServiceTests"
```

Expected: FAIL — `SearchService` not found.

- [ ] **Step 3: Create `SearchService.cs`**

`src/Ferret.Search/SearchService.cs`:

```csharp
using System.Diagnostics;
using Ferret.Core.Search;

namespace Ferret.Search;

/// <summary>
/// Orchestrates search across registered <see cref="ISearchProvider"/> implementations.
/// Parses raw query strings via <see cref="IQueryParser"/>, selects the first capable provider,
/// applies registered <see cref="ISearchPostProcessor"/> instances, and returns <see cref="SearchServiceResult"/>.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly IQueryParser _queryParser;
    private readonly IEnumerable<ISearchProvider> _providers;
    private readonly IEnumerable<ISearchPostProcessor> _postProcessors;

    /// <summary>Initialises a new <see cref="SearchService"/>.</summary>
    public SearchService(
        IQueryParser queryParser,
        IEnumerable<ISearchProvider> providers,
        IEnumerable<ISearchPostProcessor> postProcessors)
    {
        _queryParser = queryParser;
        _providers = providers;
        _postProcessors = postProcessors;
    }

    /// <inheritdoc/>
    public async Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
    {
        var parseResult = _queryParser.Parse(rawQuery);

        if (!parseResult.IsSuccess)
        {
            return SearchServiceResult.Failure(SearchServiceStatus.InvalidQuery, parseResult.Diagnostics);
        }

        return await SearchAsync(parseResult.Query!, options).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options)
    {
        var provider = _providers.FirstOrDefault(p => p.CanExecute(query, options));

        if (provider is null)
        {
            return SearchServiceResult.Failure(SearchServiceStatus.ProviderUnavailable, []);
        }

        var stopwatch = Stopwatch.StartNew();
        var providerResult = await provider.SearchAsync(query, options, CancellationToken.None)
            .ConfigureAwait(false);
        stopwatch.Stop();

        if (!providerResult.IsSuccess)
        {
            return SearchServiceResult.Failure(providerResult.Status, []);
        }

        var hits = await ApplyPostProcessorsAsync(providerResult.Hits, query, options)
            .ConfigureAwait(false);

        var executionInfo = new SearchExecutionInfo
        {
            SessionId = Guid.NewGuid(),
            ProviderName = provider.Name,
            Duration = stopwatch.Elapsed,
            DocumentsScanned = providerResult.DocumentsScanned,
            IndexVersion = providerResult.IndexVersion,
        };

        return SearchServiceResult.Success(hits, executionInfo);
    }

    private async Task<IReadOnlyList<SearchHit>> ApplyPostProcessorsAsync(
        IReadOnlyList<SearchHit> hits, SearchQuery query, SearchOptions options)
    {
        var current = hits;

        foreach (var postProcessor in _postProcessors)
        {
            current = await postProcessor.ProcessAsync(current, query, options).ConfigureAwait(false);
        }

        return current;
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Search.Tests --filter "SearchServiceTests"
dotnet build src/Ferret.sln
```

Expected: 10 `SearchServiceTests` pass, 0 build errors.

---

## Task 5: Full Section Verification + Commit

**Files:** (no new files — verification only)

- [ ] **Step 1: Run all tests**

```
dotnet test tests/Ferret.Core.Tests
dotnet test tests/Ferret.Search.Tests
```

Expected:
- `Ferret.Core.Tests`: all existing + Section 1 tests pass (no regressions)
- `Ferret.Search.Tests`:
  - Section 2: 13 `LexerTests` + 19 `QueryParserTests` = 32 (unchanged)
  - Section 3 new: 12 `QueryTranslatorTests` + 10 `HighlightParserTests` + 9 `Bm25SearchProviderTests` + 10 `SearchServiceTests` = 41
  - Total: 73 tests pass

- [ ] **Step 2: Full solution build — zero warnings**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings (StyleCop clean).

- [ ] **Step 3: Commit Section 3**

```bash
git add src/Ferret.Search/ tests/Ferret.Search.Tests/
git commit -m "feat(sprint-10): BM25SearchProvider, SearchService, QueryTranslator, HighlightParser; 41 new tests"
```

---

## Section 3 Complete

**Outputs of Section 3:**
- `QueryTranslator` (internal) — `SearchExpression` → FTS5 MATCH string; 12 tests
- `HighlightParser` (internal) — FTS5 snippet sentinels → `HighlightedText`; 10 tests
- `BM25SearchProvider` (public, `ISearchProvider`) — reads Sprint 9 FTS5 index; 9 integration tests with real SQLite
- `SearchService` (public, `ISearchService`) — orchestrates providers + post-processors; 10 unit tests with stubs
- 41 new tests, 73 total in `Ferret.Search.Tests`, 0 regressions

**What Section 4 (Rendering) depends on from here:**
- `ISearchService` — `SearchCommandHandler` in Section 5 calls it and passes the `SearchServiceResult` to the renderer
- `SearchServiceResult.Hits` (`IReadOnlyList<SearchHit>`) — renderer iterates and formats each `SearchHit`
- `HighlightedText` + `TextSpan` + `TextSpanKind` — renderer uses `TextSpanKind.Match` to apply ANSI bold/colour
- `SearchExecutionInfo` — renderer displays provider name + duration + documents scanned in footer

**What Section 5 (CLI wire-up) depends on from here:**
- `SearchService` DI registration: `services.AddSingleton<ISearchService, SearchService>()`
- `BM25SearchProvider` DI registration: `services.AddSingleton<ISearchProvider, BM25SearchProvider>()`
- `QueryParser` DI registration: `services.AddSingleton<IQueryParser, QueryParser>()` (already done in Section 2 wire-up)
- No `ISearchPostProcessor` registrations in Sprint 10 (zero post-processors, `IEnumerable` resolves to empty)
