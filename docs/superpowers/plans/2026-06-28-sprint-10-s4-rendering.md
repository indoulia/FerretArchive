# Sprint 10 — Section 4: Rendering (`ITextStyler` + `SearchRendererSelector`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Deliver the presentation layer that turns `SearchServiceResult` into terminal output. `AnsiTextStyler` applies ANSI bold/dim to match spans. `NullTextStyler` strips all styling for `--no-highlight`. `SearchRendererSelector` routes to text (default) or JSON (`--format json`) rendering. JSON renderer is functional in Sprint 10; Spectre.Console deferred.

**Architecture:** ADR-0015 principle 2 — "Providers produce semantic highlights. Renderers produce visual highlights." The `TextSpanKind.Match` spans from `HighlightedText` carry semantic intent; `ITextStyler` applies the visual style. ADR-0015 principle 4 — "Presentation is layered." The renderer is the outermost layer; it never calls `ISearchService` directly. All rendering code lives in `Ferret.Cli`.

**Tech stack:** .NET 9 / C# 13, `System.Text.Json` (BCL — no package needed), ANSI escape sequences, xUnit.

---

## Prerequisites

- Section 1 complete: `SearchHit`, `FileSearchHit`, `HighlightedText`, `TextSpan`, `TextSpanKind`, `SearchExecutionInfo` available in `Ferret.Core.Search`
- Section 3 complete: `SearchService` and `BM25SearchProvider` implemented and tested
- `Ferret.Cli.Tests` project exists and is wired into `src/Ferret.sln`

---

## Global Constraints

- `ITextStyler`, `AnsiTextStyler`, `NullTextStyler` are in `Ferret.Cli` namespace — never in `Ferret.Search` or `Ferret.Core`
- No Spectre.Console in Sprint 10 — ANSI escape sequences only
- JSON serialization uses `System.Text.Json` from BCL only — no third-party JSON library
- `SearchRendererSelector.Render()` returns `string` — the caller writes it to output (tested without output coupling)
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-10):`, `test(sprint-10):`

---

## File Inventory

### New Source Files (in `Ferret.Cli`)

| File | Access | Purpose |
|---|---|---|
| `src/Ferret.Cli/Search/ITextStyler.cs` | `public` | Contract for applying visual emphasis to text spans |
| `src/Ferret.Cli/Search/AnsiTextStyler.cs` | `public sealed` | ANSI escape code implementation |
| `src/Ferret.Cli/Search/NullTextStyler.cs` | `public sealed` | No-op — used for `--no-highlight` |
| `src/Ferret.Cli/Search/SearchOutputFormat.cs` | `public` | Enum: `Text`, `Json` |
| `src/Ferret.Cli/Search/SearchViewModel.cs` | `public sealed` | View model passed to renderer |
| `src/Ferret.Cli/Search/SearchRendererSelector.cs` | `public sealed` | Routes to text or JSON renderer |

### New Test Files (in `Ferret.Cli.Tests`)

| File | Tests |
|---|---|
| `tests/Ferret.Cli.Tests/Search/AnsiTextStylerTests.cs` | 8 |
| `tests/Ferret.Cli.Tests/Search/NullTextStylerTests.cs` | 4 |
| `tests/Ferret.Cli.Tests/Search/SearchRendererSelectorTests.cs` | 12 |

---

## Task 1: `ITextStyler` + `AnsiTextStyler` + `NullTextStyler`

**Files:**
- Create: `src/Ferret.Cli/Search/ITextStyler.cs`
- Create: `src/Ferret.Cli/Search/AnsiTextStyler.cs`
- Create: `src/Ferret.Cli/Search/NullTextStyler.cs`
- Create: `tests/Ferret.Cli.Tests/Search/AnsiTextStylerTests.cs`
- Create: `tests/Ferret.Cli.Tests/Search/NullTextStylerTests.cs`

**Interfaces:**
- Consumes: nothing external — pure string transformation
- Produces: `ITextStyler` — injected into `SearchRendererSelector` (Task 2)

- [ ] **Step 1: Write failing styler tests**

Create `tests/Ferret.Cli.Tests/Search/AnsiTextStylerTests.cs`:

```csharp
using Ferret.Cli.Search;
using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class AnsiTextStylerTests
{
    private readonly ITextStyler _styler = new AnsiTextStyler();

    [Fact]
    public void Match_Wraps_Text_In_Bold_Escape_Sequence()
    {
        var result = _styler.Match("authentication");
        Assert.StartsWith("\x1B[1m", result);
        Assert.EndsWith("\x1B[0m", result);
        Assert.Contains("authentication", result);
    }

    [Fact]
    public void Muted_Wraps_Text_In_Dim_Escape_Sequence()
    {
        var result = _styler.Muted("metadata");
        Assert.StartsWith("\x1B[2m", result);
        Assert.EndsWith("\x1B[0m", result);
        Assert.Contains("metadata", result);
    }

    [Fact]
    public void Normal_Returns_Text_Unchanged()
    {
        var result = _styler.Normal("plain text");
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void Match_Preserves_Inner_Text_Verbatim()
    {
        var result = _styler.Match("auth token");
        Assert.Contains("auth token", result);
    }

    [Fact]
    public void Muted_Preserves_Inner_Text_Verbatim()
    {
        var result = _styler.Muted("12ms · bm25");
        Assert.Contains("12ms · bm25", result);
    }

    [Fact]
    public void Match_Returns_Non_Empty_String()
    {
        Assert.False(string.IsNullOrEmpty(_styler.Match("x")));
    }

    [Fact]
    public void Match_And_Normal_Produce_Different_Output_For_Same_Input()
    {
        var text = "authentication";
        Assert.NotEqual(_styler.Match(text), _styler.Normal(text));
    }

    [Fact]
    public void Muted_And_Normal_Produce_Different_Output_For_Same_Input()
    {
        var text = "metadata";
        Assert.NotEqual(_styler.Muted(text), _styler.Normal(text));
    }
}
```

Create `tests/Ferret.Cli.Tests/Search/NullTextStylerTests.cs`:

```csharp
using Ferret.Cli.Search;
using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class NullTextStylerTests
{
    private readonly ITextStyler _styler = new NullTextStyler();

    [Fact]
    public void Match_Returns_Text_Unchanged()
    {
        Assert.Equal("authentication", _styler.Match("authentication"));
    }

    [Fact]
    public void Muted_Returns_Text_Unchanged()
    {
        Assert.Equal("metadata", _styler.Muted("metadata"));
    }

    [Fact]
    public void Normal_Returns_Text_Unchanged()
    {
        Assert.Equal("plain", _styler.Normal("plain"));
    }

    [Fact]
    public void All_Methods_Are_Pure_Passthrough()
    {
        const string input = "any text";
        Assert.Equal(input, _styler.Match(input));
        Assert.Equal(input, _styler.Muted(input));
        Assert.Equal(input, _styler.Normal(input));
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Cli.Tests --filter "AnsiTextStylerTests|NullTextStylerTests"
```

Expected: FAIL — `Ferret.Cli.Search` namespace not found.

- [ ] **Step 3: Create directory + `ITextStyler.cs`**

```
mkdir src\Ferret.Cli\Search
mkdir tests\Ferret.Cli.Tests\Search
```

`src/Ferret.Cli/Search/ITextStyler.cs`:

```csharp
namespace Ferret.Cli.Search;

/// <summary>
/// Applies visual emphasis to text for terminal output.
/// Sprint 10 implementation: ANSI escape codes (<see cref="AnsiTextStyler"/>).
/// No-op implementation: <see cref="NullTextStyler"/> — used for <c>--no-highlight</c>.
/// Reserved: SpectreConsoleStyler (future dedicated CLI UX sprint).
/// </summary>
public interface ITextStyler
{
    /// <summary>Applies match/highlight emphasis (bold in ANSI).</summary>
    string Match(string text);

    /// <summary>Applies muted/dim emphasis for metadata (path, score, timing).</summary>
    string Muted(string text);

    /// <summary>Returns text without modification.</summary>
    string Normal(string text);
}
```

- [ ] **Step 4: Create `AnsiTextStyler.cs`**

`src/Ferret.Cli/Search/AnsiTextStyler.cs`:

```csharp
namespace Ferret.Cli.Search;

/// <summary>
/// Implements <see cref="ITextStyler"/> using ANSI terminal escape sequences.
/// Bold (<c>ESC[1m</c>) for matches; dim (<c>ESC[2m</c>) for metadata; reset (<c>ESC[0m</c>) after each span.
/// </summary>
public sealed class AnsiTextStyler : ITextStyler
{
    private const string Bold = "\x1B[1m";
    private const string Dim = "\x1B[2m";
    private const string Reset = "\x1B[0m";

    /// <inheritdoc/>
    public string Match(string text) => $"{Bold}{text}{Reset}";

    /// <inheritdoc/>
    public string Muted(string text) => $"{Dim}{text}{Reset}";

    /// <inheritdoc/>
    public string Normal(string text) => text;
}
```

- [ ] **Step 5: Create `NullTextStyler.cs`**

`src/Ferret.Cli/Search/NullTextStyler.cs`:

```csharp
namespace Ferret.Cli.Search;

/// <summary>
/// No-op implementation of <see cref="ITextStyler"/>. All methods return text unchanged.
/// Used when the user passes <c>--no-highlight</c> or when output is piped to a non-TTY.
/// </summary>
public sealed class NullTextStyler : ITextStyler
{
    /// <inheritdoc/>
    public string Match(string text) => text;

    /// <inheritdoc/>
    public string Muted(string text) => text;

    /// <inheritdoc/>
    public string Normal(string text) => text;
}
```

- [ ] **Step 6: Confirm green**

```
dotnet test tests/Ferret.Cli.Tests --filter "AnsiTextStylerTests|NullTextStylerTests"
dotnet build src/Ferret.sln
```

Expected: 12 tests pass, 0 build errors.

---

## Task 2: `SearchOutputFormat` + `SearchViewModel`

**Files:**
- Create: `src/Ferret.Cli/Search/SearchOutputFormat.cs`
- Create: `src/Ferret.Cli/Search/SearchViewModel.cs`

**Interfaces:**
- Consumes: `SearchHit`, `SearchExecutionInfo` from `Ferret.Core.Search` (Section 1)
- Produces: `SearchViewModel` — passed to `SearchRendererSelector.Render()`; `SearchOutputFormat` — driven by `--format` CLI flag

- [ ] **Step 1: Add `Ferret.Core` reference to `Ferret.Cli` if not already present**

Verify `Ferret.Cli.csproj` already references `Ferret.Core`. If not:
```
dotnet add src/Ferret.Cli/Ferret.Cli.csproj reference src/Ferret.Core/Ferret.Core.csproj
```

Also, `Ferret.Cli` will need `Ferret.Search` in Section 5 for DI registration; skip that reference until Section 5.

- [ ] **Step 2: Create `SearchOutputFormat.cs`**

`src/Ferret.Cli/Search/SearchOutputFormat.cs`:

```csharp
namespace Ferret.Cli.Search;

/// <summary>Output format for <c>ferret search</c> results.</summary>
public enum SearchOutputFormat
{
    /// <summary>Human-readable text with ANSI highlighting (default).</summary>
    Text,

    /// <summary>Machine-readable JSON array of hits.</summary>
    Json,
}
```

- [ ] **Step 3: Create `SearchViewModel.cs`**

`src/Ferret.Cli/Search/SearchViewModel.cs`:

```csharp
using Ferret.Core.Search;

namespace Ferret.Cli.Search;

/// <summary>
/// View model produced by <c>SearchCommandHandler</c> and consumed by <see cref="SearchRendererSelector"/>.
/// Presentation models live in the CLI layer per ADR-0015, principle 5.
/// </summary>
public sealed record SearchViewModel
{
    /// <summary>The raw query string as typed by the user.</summary>
    public required string OriginalQuery { get; init; }

    /// <summary>Ranked hits from <see cref="ISearchService"/>.</summary>
    public required IReadOnlyList<SearchHit> Hits { get; init; }

    /// <summary>Provider name, duration, and document count from the search execution.</summary>
    public required SearchExecutionInfo ExecutionInfo { get; init; }
}
```

- [ ] **Step 4: Build to verify types compile**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors.

---

## Task 3: `SearchRendererSelector`

**Files:**
- Create: `src/Ferret.Cli/Search/SearchRendererSelector.cs`
- Create: `tests/Ferret.Cli.Tests/Search/SearchRendererSelectorTests.cs`

**Interfaces:**
- Consumes: `ITextStyler` (Task 1); `SearchViewModel`, `SearchOutputFormat` (Task 2); `HighlightedText`, `TextSpanKind` from `Ferret.Core.Search` (Section 1)
- Produces: `SearchRendererSelector.Render(SearchViewModel, SearchOutputFormat)` → `string` — consumed by `SearchCommandHandler` (Section 5)

- [ ] **Step 1: Write failing renderer tests**

Create `tests/Ferret.Cli.Tests/Search/SearchRendererSelectorTests.cs`:

```csharp
using System.Text.Json;
using Ferret.Cli.Search;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class SearchRendererSelectorTests
{
    private readonly SearchRendererSelector _ansiRenderer =
        new SearchRendererSelector(new AnsiTextStyler());

    private readonly SearchRendererSelector _plainRenderer =
        new SearchRendererSelector(new NullTextStyler());

    // ── Text format — zero results ────────────────────────────────────────────

    [Fact]
    public void Text_NoHits_Contains_Query_In_Output()
    {
        var vm = MakeViewModel("authentication", []);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("authentication", output);
    }

    [Fact]
    public void Text_NoHits_Does_Not_Contain_Table_Rows()
    {
        var vm = MakeViewModel("auth", []);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.DoesNotContain("doc-", output);
    }

    // ── Text format — with results ────────────────────────────────────────────

    [Fact]
    public void Text_WithHits_Contains_DisplayName()
    {
        var vm = MakeViewModel("auth", [MakeHit("auth-service.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("auth-service.cs", output);
    }

    [Fact]
    public void Text_WithHits_Contains_Snippet_Text()
    {
        var vm = MakeViewModel("token", [MakeHit("file.cs", "token")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("token", output);
    }

    [Fact]
    public void Text_WithHits_Contains_Provider_Name_In_Footer()
    {
        var vm = MakeViewModel("auth", [MakeHit("f.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("bm25-fts5", output);
    }

    [Fact]
    public void Text_AnsiRenderer_Match_Spans_Get_Bold_Escape_Sequences()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _ansiRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("\x1B[1m", output);  // ANSI bold
    }

    [Fact]
    public void Text_NullRenderer_Contains_No_Escape_Sequences()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.DoesNotContain("\x1B[", output);
    }

    [Fact]
    public void Text_Footer_Contains_Hit_Count()
    {
        var hits = new[] { MakeHit("a.cs", "auth"), MakeHit("b.cs", "auth") };
        var vm = MakeViewModel("auth", hits);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Text);
        Assert.Contains("2", output);
    }

    // ── JSON format ───────────────────────────────────────────────────────────

    [Fact]
    public void Json_Output_Is_Valid_Json()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        var doc = JsonDocument.Parse(output); // throws if invalid JSON
        Assert.NotNull(doc);
    }

    [Fact]
    public void Json_Output_Contains_Query_Field()
    {
        var vm = MakeViewModel("authentication", []);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        Assert.Contains("authentication", output);
    }

    [Fact]
    public void Json_Output_Contains_Hits_Array()
    {
        var vm = MakeViewModel("auth", [MakeHit("file.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        var doc = JsonDocument.Parse(output);
        Assert.True(doc.RootElement.TryGetProperty("hits", out var hits));
        Assert.Equal(JsonValueKind.Array, hits.ValueKind);
    }

    [Fact]
    public void Json_Output_Contains_Total_Field()
    {
        var vm = MakeViewModel("auth", [MakeHit("a.cs", "auth"), MakeHit("b.cs", "auth")]);
        var output = _plainRenderer.Render(vm, SearchOutputFormat.Json);
        var doc = JsonDocument.Parse(output);
        Assert.True(doc.RootElement.TryGetProperty("total", out var total));
        Assert.Equal(2, total.GetInt32());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchViewModel MakeViewModel(string query, IReadOnlyList<FileSearchHit> hits) =>
        new()
        {
            OriginalQuery = query,
            Hits = hits,
            ExecutionInfo = new SearchExecutionInfo
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "bm25-fts5",
                Duration = TimeSpan.FromMilliseconds(12),
                DocumentsScanned = hits.Count,
                IndexVersion = "fts5",
            },
        };

    private static FileSearchHit MakeHit(string displayName, string matchText) =>
        new()
        {
            DocumentId = DocumentId.Parse(displayName),
            ConnectorInstanceId = new ConnectorInstanceId(string.Empty),
            CanonicalUri = new Uri($"file:///{displayName}"),
            DisplayName = displayName,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = new HighlightedText([
                new TextSpan("before ", TextSpanKind.Normal),
                new TextSpan(matchText, TextSpanKind.Match),
                new TextSpan(" after", TextSpanKind.Normal),
            ]),
        };
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Cli.Tests --filter "SearchRendererSelectorTests"
```

Expected: FAIL — `SearchRendererSelector` not found.

- [ ] **Step 3: Create `SearchRendererSelector.cs`**

`src/Ferret.Cli/Search/SearchRendererSelector.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Ferret.Core.Search;

namespace Ferret.Cli.Search;

/// <summary>
/// Routes <see cref="SearchViewModel"/> rendering to the appropriate format.
/// Text format: ANSI-highlighted hit list with snippet and footer.
/// JSON format: machine-readable JSON for scripting and tool integration.
/// </summary>
public sealed class SearchRendererSelector
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true };

    private readonly ITextStyler _styler;

    /// <summary>Initialises a new <see cref="SearchRendererSelector"/>.</summary>
    public SearchRendererSelector(ITextStyler styler)
    {
        _styler = styler;
    }

    /// <summary>Renders the view model to a string in the requested format.</summary>
    public string Render(SearchViewModel viewModel, SearchOutputFormat format) =>
        format switch
        {
            SearchOutputFormat.Json => RenderJson(viewModel),
            _ => RenderText(viewModel),
        };

    private string RenderText(SearchViewModel viewModel)
    {
        var sb = new StringBuilder();

        if (viewModel.Hits.Count == 0)
        {
            sb.AppendLine($"No results for \"{viewModel.OriginalQuery}\".");
            return sb.ToString();
        }

        foreach (var hit in viewModel.Hits)
        {
            sb.AppendLine(_styler.Muted(hit.DisplayName));

            foreach (var span in hit.Snippet.Spans)
            {
                sb.Append(span.Kind == TextSpanKind.Match
                    ? _styler.Match(span.Text)
                    : _styler.Normal(span.Text));
            }

            sb.AppendLine();
            sb.AppendLine();
        }

        var info = viewModel.ExecutionInfo;
        sb.Append(_styler.Muted(
            $"{viewModel.Hits.Count} result(s) · {info.ProviderName} · {info.Duration.TotalMilliseconds:F0}ms"));

        return sb.ToString();
    }

    private static string RenderJson(SearchViewModel viewModel)
    {
        var hits = viewModel.Hits.Select(h => new
        {
            documentId = h.DocumentId.ToString(),
            displayName = h.DisplayName,
            canonicalUri = h.CanonicalUri.ToString(),
            score = h.Score,
            snippet = string.Concat(h.Snippet.Spans.Select(s => s.Text)),
        }).ToList();

        return JsonSerializer.Serialize(
            new
            {
                query = viewModel.OriginalQuery,
                total = viewModel.Hits.Count,
                hits,
            },
            JsonOptions);
    }
}
```

- [ ] **Step 4: Confirm green**

```
dotnet test tests/Ferret.Cli.Tests --filter "SearchRendererSelectorTests"
dotnet test tests/Ferret.Cli.Tests
dotnet build src/Ferret.sln
```

Expected: 12 new tests pass, all `Ferret.Cli.Tests` pass, 0 build errors.

---

## Task 4: Commit Section 4

- [ ] **Step 1: Run full test suite**

```
dotnet test tests/Ferret.Core.Tests
dotnet test tests/Ferret.Search.Tests
dotnet test tests/Ferret.Cli.Tests
```

Expected: all pass — 12 `AnsiTextStylerTests`+`NullTextStylerTests` + 12 `SearchRendererSelectorTests` = 24 new tests in `Ferret.Cli.Tests`.

- [ ] **Step 2: Full build — zero warnings**

```
dotnet build src/Ferret.sln
```

- [ ] **Step 3: Commit Section 4**

```bash
git add src/Ferret.Cli/Search/ tests/Ferret.Cli.Tests/Search/
git commit -m "feat(sprint-10): ITextStyler, AnsiTextStyler, NullTextStyler, SearchRendererSelector; 24 new tests"
```

---

## Section 4 Complete

**Outputs:**
- `ITextStyler` — abstraction contract in `Ferret.Cli.Search`
- `AnsiTextStyler` — ANSI bold/dim implementation; 8 tests
- `NullTextStyler` — passthrough for `--no-highlight`; 4 tests
- `SearchOutputFormat` enum — `Text` | `Json`
- `SearchViewModel` — CLI-layer view model (ADR-0015 principle 5)
- `SearchRendererSelector` — routes text/JSON rendering; 12 tests

**What Section 5 depends on from here:**
- `SearchRendererSelector` — `SearchCommandHandler` calls `Render(viewModel, format)` and writes the result to `IOutputFormatter`
- `AnsiTextStyler` / `NullTextStyler` — registered in DI; `SearchCommandHandler` receives `ITextStyler`; selects `AnsiTextStyler` by default, `NullTextStyler` when `--no-highlight` is passed
- `SearchViewModel` — `SearchCommandHandler` constructs it from `SearchServiceResult`
- `SearchOutputFormat` — parsed from `--format text|json` CLI option
