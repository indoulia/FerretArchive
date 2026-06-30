# Sprint 10 — Section 2: Query Parser (`Ferret.Search`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Create the `Ferret.Search` project and implement `QueryParser` — the canonical translation from raw user input to the `SearchQuery` AST defined in Section 1. Three constructs supported: whitespace-separated keywords (implicit AND), quoted phrases, trailing `*` prefix. No SQL. No SQLite. No FTS5. The parser produces only `SearchExpression` nodes.

**Architecture:** `Lexer` (internal) converts raw input to a flat token list. `QueryParser` (public, implements `IQueryParser`) converts the token list to a `SearchExpression` AST and wraps it in `SearchParseResult`. All failure modes are return values — the parser never throws for user input.

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `sealed` on all concrete classes, `internal` on all parsing primitives, `InternalsVisibleTo` for test access.

---

## Prerequisites

Section 1 must be **complete** before starting this section:
- `Ferret.Core.Search` namespace with all 20 contract types compiled and tested
- `IQueryParser`, `SearchParseResult`, `SearchQuery`, `SearchExpression` hierarchy, `SearchDiagnostic` available
- `dotnet test tests/Ferret.Core.Tests` passes
- `docs(sprint-10): ADR-0015` commit applied

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes
- `internal` on all parsing primitives (`TokenKind`, `Token`, `Lexer`) — never expose parser internals
- `InternalsVisibleTo("Ferret.Search.Tests")` for direct Lexer test access
- `IQueryParser` is the only public contract — callers never know `Lexer` or `Token` exist
- Parser never throws for user input — all failure modes are `SearchParseResult.Failure(...)`
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-10):`, `test(sprint-10):`

---

## File Inventory

### New Projects

| Project | Type | Path |
|---|---|---|
| `Ferret.Search` | Class library | `src/Ferret.Search/` |
| `Ferret.Search.Tests` | xUnit test project | `tests/Ferret.Search.Tests/` |

### New Source Files

| File | Access |
|---|---|
| `src/Ferret.Search/Ferret.Search.csproj` | — |
| `src/Ferret.Search/Parsing/Token.cs` | `internal` |
| `src/Ferret.Search/Parsing/Lexer.cs` | `internal` |
| `src/Ferret.Search/QueryParser.cs` | `public` |

### New Test Files

| File | Project |
|---|---|
| `tests/Ferret.Search.Tests/Ferret.Search.Tests.csproj` | — |
| `tests/Ferret.Search.Tests/Parsing/LexerTests.cs` | Ferret.Search.Tests |
| `tests/Ferret.Search.Tests/QueryParserTests.cs` | Ferret.Search.Tests |

---

## Task 1: Scaffold `Ferret.Search` + `Ferret.Search.Tests`

**Files:**
- Create: `src/Ferret.Search/Ferret.Search.csproj`
- Create: `tests/Ferret.Search.Tests/Ferret.Search.Tests.csproj`

**Interfaces:**
- Produces: two new compilable projects wired into `src/Ferret.sln`

- [ ] **Step 1: Create `Ferret.Search` class library**

```
dotnet new classlib -n Ferret.Search -o src/Ferret.Search --framework net9.0 --no-restore
del src\Ferret.Search\Class1.cs
```

- [ ] **Step 2: Create `Ferret.Search.Tests` xUnit project**

```
dotnet new xunit -n Ferret.Search.Tests -o tests/Ferret.Search.Tests --framework net9.0 --no-restore
del tests\Ferret.Search.Tests\UnitTest1.cs
```

- [ ] **Step 3: Add both projects to the solution**

```
dotnet sln src/Ferret.sln add src/Ferret.Search/Ferret.Search.csproj
dotnet sln src/Ferret.sln add tests/Ferret.Search.Tests/Ferret.Search.Tests.csproj
```

- [ ] **Step 4: Add project references**

```
dotnet add src/Ferret.Search/Ferret.Search.csproj reference src/Ferret.Core/Ferret.Core.csproj
dotnet add tests/Ferret.Search.Tests/Ferret.Search.Tests.csproj reference src/Ferret.Search/Ferret.Search.csproj
dotnet add tests/Ferret.Search.Tests/Ferret.Search.Tests.csproj reference src/Ferret.Core/Ferret.Core.csproj
```

- [ ] **Step 5: Replace `Ferret.Search.csproj` with the project-standard format**

Replace the full content of `src/Ferret.Search/Ferret.Search.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AnalysisMode>All</AnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <RootNamespace>Ferret.Search</RootNamespace>
    <AssemblyName>Ferret.Search</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Ferret.Core/Ferret.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" PrivateAssets="all" />
    <AdditionalFiles Include="../../.editorconfig" Link=".editorconfig" />
  </ItemGroup>

  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
      <_Parameter1>Ferret.Search.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

</Project>
```

- [ ] **Step 6: Replace `Ferret.Search.Tests.csproj` with the project-standard format**

Replace the full content of `tests/Ferret.Search.Tests/Ferret.Search.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" PrivateAssets="all" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Ferret.Search/Ferret.Search.csproj" />
    <ProjectReference Include="../../src/Ferret.Core/Ferret.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Create the `Parsing/` subdirectory placeholder so the folder exists**

```
mkdir src\Ferret.Search\Parsing
mkdir tests\Ferret.Search.Tests\Parsing
```

- [ ] **Step 8: Verify both projects build**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors. Both new projects compile (they have no source files yet).

---

## Task 2: `Token` + `Lexer` (internal)

**Files:**
- Create: `src/Ferret.Search/Parsing/Token.cs`
- Create: `src/Ferret.Search/Parsing/Lexer.cs`
- Create: `tests/Ferret.Search.Tests/Parsing/LexerTests.cs`

**Interfaces:**
- Produces: `TokenKind` (enum), `Token` (record), `Lexer` (class) — all `internal`; consumed only by `QueryParser` (Task 3)

- [ ] **Step 1: Write failing Lexer tests**

Create `tests/Ferret.Search.Tests/Parsing/LexerTests.cs`:

```csharp
using Ferret.Search.Parsing;
using Xunit;

namespace Ferret.Search.Tests.Parsing;

public sealed class LexerTests
{
    [Fact]
    public void SingleKeyword_Produces_One_Word_Token()
    {
        var tokens = Tokenize("authentication");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Word, tokens[0].Kind);
        Assert.Equal("authentication", tokens[0].Value);
    }

    [Fact]
    public void TwoKeywords_Produce_Two_Word_Tokens_In_Order()
    {
        var tokens = Tokenize("authentication token");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Word, tokens[0].Kind);
        Assert.Equal("authentication", tokens[0].Value);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("token", tokens[1].Value);
    }

    [Fact]
    public void QuotedPhrase_Produces_Phrase_Token_Without_Quotes()
    {
        var tokens = Tokenize("\"runtime builder\"");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal("runtime builder", tokens[0].Value);
    }

    [Fact]
    public void TrailingAsterisk_Produces_Prefix_Token_Without_Asterisk()
    {
        var tokens = Tokenize("auth*");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Prefix, tokens[0].Kind);
        Assert.Equal("auth", tokens[0].Value);
    }

    [Fact]
    public void EmptyInput_Produces_No_Tokens()
    {
        Assert.Empty(Tokenize(string.Empty));
    }

    [Fact]
    public void WhitespaceOnly_Produces_No_Tokens()
    {
        Assert.Empty(Tokenize("   "));
    }

    [Fact]
    public void PhraseAndKeyword_Produce_Two_Tokens_In_Order()
    {
        var tokens = Tokenize("\"context window\" token");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal("context window", tokens[0].Value);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("token", tokens[1].Value);
    }

    [Fact]
    public void PrefixAndKeyword_Produce_Two_Tokens_In_Order()
    {
        var tokens = Tokenize("auth* token");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Prefix, tokens[0].Kind);
        Assert.Equal("auth", tokens[0].Value);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("token", tokens[1].Value);
    }

    [Fact]
    public void UnclosedQuote_Produces_Phrase_Token_From_Remaining_Input()
    {
        var tokens = Tokenize("\"unclosed phrase");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal("unclosed phrase", tokens[0].Value);
    }

    [Fact]
    public void AsteriskAlone_Produces_Prefix_Token_With_Empty_Value()
    {
        var tokens = Tokenize("*");
        Assert.Single(tokens);
        Assert.Equal(TokenKind.Prefix, tokens[0].Kind);
        Assert.Equal(string.Empty, tokens[0].Value);
    }

    [Fact]
    public void LeadingAndTrailingWhitespace_Is_Stripped()
    {
        var tokens = Tokenize("  auth  ");
        Assert.Single(tokens);
        Assert.Equal("auth", tokens[0].Value);
    }

    [Fact]
    public void Token_Position_Reflects_Start_Offset_In_Input()
    {
        var tokens = Tokenize("auth token");
        Assert.Equal(0, tokens[0].Position);
        Assert.Equal(5, tokens[1].Position);
    }

    [Fact]
    public void ThreeTerms_Produce_Three_Tokens()
    {
        var tokens = Tokenize("\"runtime\" auth* token");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(TokenKind.Phrase, tokens[0].Kind);
        Assert.Equal(TokenKind.Prefix, tokens[1].Kind);
        Assert.Equal(TokenKind.Word, tokens[2].Kind);
    }

    private static IReadOnlyList<Token> Tokenize(string input) =>
        new Lexer(input).Tokenize();
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Search.Tests --filter "LexerTests"
```

Expected: FAIL — `Ferret.Search.Parsing` namespace not found.

- [ ] **Step 3: Create `Token.cs`**

`src/Ferret.Search/Parsing/Token.cs`:

```csharp
namespace Ferret.Search.Parsing;

/// <summary>The kind of a lexed token.</summary>
internal enum TokenKind
{
    /// <summary>A plain keyword (e.g. <c>authentication</c>).</summary>
    Word,

    /// <summary>A quoted phrase with quotes stripped (e.g. input <c>"runtime builder"</c> → value <c>runtime builder</c>).</summary>
    Phrase,

    /// <summary>A prefix match with trailing <c>*</c> stripped (e.g. input <c>auth*</c> → value <c>auth</c>).</summary>
    Prefix,
}

/// <summary>A single lexed token produced by <see cref="Lexer"/>.</summary>
/// <param name="Kind">The token classification.</param>
/// <param name="Value">The token value (quotes and asterisks already stripped).</param>
/// <param name="Position">The zero-based character offset of this token in the original input.</param>
internal sealed record Token(TokenKind Kind, string Value, int Position);
```

- [ ] **Step 4: Create `Lexer.cs`**

`src/Ferret.Search/Parsing/Lexer.cs`:

```csharp
namespace Ferret.Search.Parsing;

/// <summary>
/// Converts a raw query string into a flat list of <see cref="Token"/> values.
/// Recognises three token forms: plain words (keyword), quoted phrases, and words ending with <c>*</c> (prefix).
/// Whitespace is consumed as a delimiter and produces no tokens.
/// Unclosed quotes are treated leniently — the remaining input becomes the phrase value.
/// </summary>
internal sealed class Lexer
{
    private readonly string _input;
    private int _pos;

    /// <summary>Initialises a new <see cref="Lexer"/> for the given raw query string.</summary>
    /// <param name="input">The raw query string.</param>
    internal Lexer(string input)
    {
        _input = input;
        _pos = 0;
    }

    /// <summary>
    /// Scans the input and returns all tokens in source order.
    /// Never throws. Returns an empty list for empty or whitespace-only input.
    /// </summary>
    internal IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        SkipWhitespace();

        while (_pos < _input.Length)
        {
            var start = _pos;
            tokens.Add(_input[_pos] == '"' ? ReadPhrase(start) : ReadWordOrPrefix(start));
            SkipWhitespace();
        }

        return tokens;
    }

    private Token ReadPhrase(int start)
    {
        _pos++; // consume opening "
        var valueStart = _pos;

        while (_pos < _input.Length && _input[_pos] != '"')
        {
            _pos++;
        }

        var value = _input[valueStart.._pos];

        if (_pos < _input.Length)
        {
            _pos++; // consume closing "
        }
        // else: unclosed quote — treat remaining input as phrase value (lenient)

        return new Token(TokenKind.Phrase, value, start);
    }

    private Token ReadWordOrPrefix(int start)
    {
        while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]))
        {
            _pos++;
        }

        var raw = _input[start.._pos];

        return raw.EndsWith('*')
            ? new Token(TokenKind.Prefix, raw[..^1], start)
            : new Token(TokenKind.Word, raw, start);
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
        {
            _pos++;
        }
    }
}
```

- [ ] **Step 5: Confirm green**

```
dotnet test tests/Ferret.Search.Tests --filter "LexerTests"
dotnet build src/Ferret.sln
```

Expected: 13 tests pass, 0 build errors.

---

## Task 3: `QueryParser` (implements `IQueryParser`)

**Files:**
- Create: `src/Ferret.Search/QueryParser.cs`
- Create: `tests/Ferret.Search.Tests/QueryParserTests.cs`

**Interfaces:**
- Consumes: `Lexer`, `Token`, `TokenKind` (Task 2); `IQueryParser`, `SearchQuery`, `SearchExpression` hierarchy, `SearchParseResult`, `SearchDiagnostic` (Section 1)
- Produces: `QueryParser` — registered in DI as `IQueryParser`; consumed by S3 (`SearchService`), S5 (`SearchCommandHandler` via `ISearchService`)

- [ ] **Step 1: Write failing QueryParser tests**

Create `tests/Ferret.Search.Tests/QueryParserTests.cs`:

```csharp
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Search.Tests;

public sealed class QueryParserTests
{
    private readonly IQueryParser _parser = new QueryParser();

    // ── Failure cases ────────────────────────────────────────────────────────

    [Fact]
    public void EmptyString_Returns_Failure()
    {
        var result = _parser.Parse(string.Empty);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(SearchDiagnosticSeverity.Error, result.Diagnostics[0].Severity);
    }

    [Fact]
    public void WhitespaceOnly_Returns_Failure()
    {
        var result = _parser.Parse("   ");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Failure_Query_Is_Null()
    {
        var result = _parser.Parse(string.Empty);
        Assert.Null(result.Query);
    }

    // ── Single-term success cases ─────────────────────────────────────────────

    [Fact]
    public void SingleKeyword_Returns_KeywordExpression()
    {
        var result = _parser.Parse("authentication");
        Assert.True(result.IsSuccess);
        var keyword = Assert.IsType<KeywordExpression>(result.Query!.Root);
        Assert.Equal("authentication", keyword.Value);
    }

    [Fact]
    public void QuotedPhrase_Returns_PhraseExpression()
    {
        var result = _parser.Parse("\"runtime builder\"");
        Assert.True(result.IsSuccess);
        var phrase = Assert.IsType<PhraseExpression>(result.Query!.Root);
        Assert.Equal("runtime builder", phrase.Value);
    }

    [Fact]
    public void PrefixQuery_Returns_PrefixExpression()
    {
        var result = _parser.Parse("auth*");
        Assert.True(result.IsSuccess);
        var prefix = Assert.IsType<PrefixExpression>(result.Query!.Root);
        Assert.Equal("auth", prefix.Prefix);
    }

    // ── Multi-term AND cases ──────────────────────────────────────────────────

    [Fact]
    public void TwoKeywords_Returns_AndExpression_With_Two_Keyword_Operands()
    {
        var result = _parser.Parse("authentication token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(2, and.Operands.Count);
        Assert.IsType<KeywordExpression>(and.Operands[0]);
        Assert.IsType<KeywordExpression>(and.Operands[1]);
        Assert.Equal("authentication", ((KeywordExpression)and.Operands[0]).Value);
        Assert.Equal("token", ((KeywordExpression)and.Operands[1]).Value);
    }

    [Fact]
    public void PhraseAndKeyword_Returns_AndExpression()
    {
        var result = _parser.Parse("\"context window\" token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(2, and.Operands.Count);
        Assert.IsType<PhraseExpression>(and.Operands[0]);
        Assert.IsType<KeywordExpression>(and.Operands[1]);
    }

    [Fact]
    public void PrefixAndKeyword_Returns_AndExpression()
    {
        var result = _parser.Parse("auth* token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(2, and.Operands.Count);
        Assert.IsType<PrefixExpression>(and.Operands[0]);
        Assert.IsType<KeywordExpression>(and.Operands[1]);
        Assert.Equal("auth", ((PrefixExpression)and.Operands[0]).Prefix);
        Assert.Equal("token", ((KeywordExpression)and.Operands[1]).Value);
    }

    [Fact]
    public void ThreeTerms_Returns_AndExpression_With_Three_Operands()
    {
        var result = _parser.Parse("authentication token session");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(3, and.Operands.Count);
    }

    [Fact]
    public void PhraseAndPrefixAndKeyword_Returns_AndExpression_With_Correct_Types()
    {
        var result = _parser.Parse("\"runtime builder\" auth* token");
        Assert.True(result.IsSuccess);
        var and = Assert.IsType<AndExpression>(result.Query!.Root);
        Assert.Equal(3, and.Operands.Count);
        Assert.IsType<PhraseExpression>(and.Operands[0]);
        Assert.IsType<PrefixExpression>(and.Operands[1]);
        Assert.IsType<KeywordExpression>(and.Operands[2]);
    }

    // ── OriginalText preservation ─────────────────────────────────────────────

    [Fact]
    public void OriginalText_Is_Preserved_Verbatim_In_Query()
    {
        const string raw = "auth* \"context window\"";
        var result = _parser.Parse(raw);
        Assert.True(result.IsSuccess);
        Assert.Equal(raw, result.Query!.OriginalText);
    }

    [Fact]
    public void OriginalText_Preserves_Casing()
    {
        const string raw = "RuntimeBuilder";
        var result = _parser.Parse(raw);
        Assert.True(result.IsSuccess);
        Assert.Equal("RuntimeBuilder", result.Query!.OriginalText);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Success_Result_Has_Empty_Diagnostics()
    {
        var result = _parser.Parse("authentication");
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void UnclosedQuote_Is_Parsed_Leniently_As_PhraseExpression()
    {
        var result = _parser.Parse("\"unclosed phrase");
        Assert.True(result.IsSuccess);
        Assert.IsType<PhraseExpression>(result.Query!.Root);
        Assert.Equal("unclosed phrase", ((PhraseExpression)result.Query.Root).Value);
    }

    [Fact]
    public void Keyword_Value_Preserves_Case()
    {
        var result = _parser.Parse("RuntimeBuilder");
        Assert.True(result.IsSuccess);
        var keyword = Assert.IsType<KeywordExpression>(result.Query!.Root);
        Assert.Equal("RuntimeBuilder", keyword.Value);
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Search.Tests --filter "QueryParserTests"
```

Expected: FAIL — `Ferret.Search.QueryParser` type not found.

- [ ] **Step 3: Create `QueryParser.cs`**

`src/Ferret.Search/QueryParser.cs`:

```csharp
using Ferret.Core.Search;
using Ferret.Search.Parsing;

namespace Ferret.Search;

/// <summary>
/// Parses a raw user query string into a canonical <see cref="SearchQuery"/> AST.
/// Implements <see cref="IQueryParser"/> — register via DI; do not construct directly in application code.
/// Sprint 10 constructs: whitespace-separated keywords (implicit AND), quoted phrases, trailing <c>*</c> prefix.
/// All failure modes are <see cref="SearchParseResult"/> values — the parser never throws for user input.
/// </summary>
public sealed class QueryParser : IQueryParser
{
    /// <inheritdoc/>
    public SearchParseResult Parse(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return SearchParseResult.Failure("Query must contain at least one search term.");
        }

        var tokens = new Lexer(rawQuery).Tokenize();

        if (tokens.Count == 0)
        {
            return SearchParseResult.Failure("Query must contain at least one search term.");
        }

        var expressions = BuildExpressions(tokens);
        var root = expressions.Count == 1 ? expressions[0] : new AndExpression(expressions);

        return SearchParseResult.Success(new SearchQuery
        {
            OriginalText = rawQuery,
            Root = root,
        });
    }

    private static List<SearchExpression> BuildExpressions(IReadOnlyList<Token> tokens)
    {
        var expressions = new List<SearchExpression>(tokens.Count);

        foreach (var token in tokens)
        {
            expressions.Add(token.Kind switch
            {
                TokenKind.Word => new KeywordExpression(token.Value),
                TokenKind.Phrase => new PhraseExpression(token.Value),
                TokenKind.Prefix => new PrefixExpression(token.Value),
                _ => throw new InvalidOperationException(
                    $"Unexpected token kind '{token.Kind}' at position {token.Position}."),
            });
        }

        return expressions;
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Search.Tests --filter "QueryParserTests"
dotnet build src/Ferret.sln
```

Expected: 19 tests pass, 0 build errors.

---

## Task 4: Full Section Verification + Commit

**Files:** (no new files — verification only)

**Interfaces:**
- Section 2 output: `QueryParser` implements `IQueryParser`, `Lexer` + `Token` are internal, 32 tests pass, `Ferret.Search` compiles clean

- [ ] **Step 1: Run all tests across both projects**

```
dotnet test tests/Ferret.Core.Tests
dotnet test tests/Ferret.Search.Tests
```

Expected:
- `Ferret.Core.Tests`: all existing + Section 1 tests pass (no regressions)
- `Ferret.Search.Tests`: 13 `LexerTests` + 19 `QueryParserTests` = 32 new tests pass

- [ ] **Step 2: Full solution build — zero warnings**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors, 0 warnings (StyleCop clean).

- [ ] **Step 3: Commit Section 2**

```bash
git add src/Ferret.Search/ tests/Ferret.Search.Tests/ src/Ferret.sln
git commit -m "feat(sprint-10): Ferret.Search — QueryParser, Lexer, Token; 32 parser tests"
```

---

## Section 2 Complete

**Outputs of Section 2:**
- `Ferret.Search` class library — wired into `src/Ferret.sln`
- `QueryParser` (public, `IQueryParser`) — translates raw input to `SearchQuery` AST
- `Lexer` + `Token` + `TokenKind` (internal to `Ferret.Search`) — produce tokens from raw input
- 13 `LexerTests` — cover all token types, edge cases, position tracking
- 19 `QueryParserTests` — cover all three constructs, multi-term AND, edge cases, original text preservation
- 32 total new tests, 0 regressions

**What Section 3 (Search Platform) depends on from here:**
- `QueryParser` — `SearchService` injects `IQueryParser` and calls `Parse(rawQuery)` on the high-level overload
- `SearchParseResult` (from Section 1) — `SearchService` checks `IsSuccess` and branches on `InvalidQuery` status
- `SearchExpression` hierarchy (from Section 1) — `BM25SearchProvider.QueryTranslator` pattern-matches on AST nodes to produce FTS5 syntax
- `SearchQuery.OriginalText` — echoed in `SearchServiceResult` for display and telemetry
