# Sprint 10 — Section 5: CLI Wire-up (`ferret search`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Wire everything built in Sections 1–4 into the CLI. After this section, a developer can run `ferret search <query>` in a workspace with an indexed FTS5 database and get ranked, highlighted results in the terminal. Sprint 10 is complete; the platform is tagged `v0.10.0-sprint10`.

**Architecture:** `SearchCommandHandler` calls `ISearchService`, builds `SearchViewModel`, and passes it to `SearchRendererSelector`. `SearchCliModule` declares the `ferret search` command with four options: `--passages`, `--limit`, `--no-highlight`, `--format`. DI registrations: `IQueryParser → QueryParser`, `ISearchProvider → BM25SearchProvider`, `ISearchService → SearchService`, `ITextStyler → AnsiTextStyler` (by default), `SearchRendererSelector`. The CLI module follows the existing `ICliModule` + `CommandDefinition` platform pattern.

**Tech stack:** .NET 9 / C# 13, System.CommandLine 2.0 beta, xUnit integration tests.

---

## Prerequisites

- All of Sections 1–4 complete and green
- `ICliModule`, `CommandDefinition`, `ICommandHandler`, `IFerretContext`, `IOutputFormatter` available in `Ferret.Cli`
- `Ferret.Cli.csproj` is the CLI entry point and DI root
- Existing module pattern: look at `ConnectorCliModule` + `ConnectorListCommandHandler` for the exact API signatures before writing code

---

## Global Constraints

- Follow the existing `ICliModule` + `CommandDefinition` + `ICommandHandler` pattern exactly — do not invent new abstractions
- `Ferret.Cli.csproj` must add a `<ProjectReference>` to `Ferret.Search.csproj` for DI registration
- `IEnumerable<ISearchPostProcessor>` resolves to empty in Sprint 10 — no post-processor registrations
- Zero breaking changes to existing CLI commands
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-10):`, `test(sprint-10):`, `docs(sprint-10):`

---

## File Inventory

### Modified Files

| File | Change |
|---|---|
| `src/Ferret.Cli/Ferret.Cli.csproj` | Add `<ProjectReference>` to `Ferret.Search` |
| `src/Ferret.Cli/Program.cs` (or DI root) | Register `SearchCliModule` |
| `docs/000-Overview/PROJECT-STATE.md` | Update sprint state to Sprint 10 complete |

### New Source Files

| File | Purpose |
|---|---|
| `src/Ferret.Cli/Search/SearchCommandArgs.cs` | Parsed CLI arguments for `ferret search` |
| `src/Ferret.Cli/Search/SearchCommandHandler.cs` | `ICommandHandler` — orchestrates search + render |
| `src/Ferret.Cli/Search/SearchCliModule.cs` | `ICliModule` — declares `ferret search` command |

### New Test Files

| File | Tests |
|---|---|
| `tests/Ferret.Cli.Tests/Search/SearchCommandHandlerTests.cs` | 8 |

---

## Task 1: Add `Ferret.Search` Reference to `Ferret.Cli`

**Files:**
- Modify: `src/Ferret.Cli/Ferret.Cli.csproj`

- [ ] **Step 1: Add project reference**

```
dotnet add src/Ferret.Cli/Ferret.Cli.csproj reference src/Ferret.Search/Ferret.Search.csproj
```

- [ ] **Step 2: Verify build**

```
dotnet build src/Ferret.sln
```

Expected: 0 errors.

---

## Task 2: `SearchCommandArgs` + `SearchCommandHandler`

**Files:**
- Create: `src/Ferret.Cli/Search/SearchCommandArgs.cs`
- Create: `src/Ferret.Cli/Search/SearchCommandHandler.cs`
- Create: `tests/Ferret.Cli.Tests/Search/SearchCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISearchService` from `Ferret.Core.Search` (Section 1 + Section 3 impl); `ITextStyler`, `SearchViewModel`, `SearchOutputFormat`, `SearchRendererSelector` (Section 4); `IOutputFormatter`, `ICommandHandler` from `Ferret.Cli` — verify exact signature from `ConnectorListCommandHandler`
- Produces: `SearchCommandHandler` — registered in DI; invoked by `SearchCliModule`

- [ ] **Step 1: Read `ConnectorListCommandHandler` to confirm `ICommandHandler` signature**

Read `src/Ferret.Cli/Connectors/ConnectorListCommandHandler.cs` (or equivalent path) to verify:
- The exact `ICommandHandler<T>` signature
- How `IFerretContext` / `IOutputFormatter` is used
- How exit codes are returned

Adjust the code below if the actual signature differs.

- [ ] **Step 2: Create `SearchCommandArgs.cs`**

`src/Ferret.Cli/Search/SearchCommandArgs.cs`:

```csharp
namespace Ferret.Cli.Search;

/// <summary>Parsed arguments for the <c>ferret search &lt;query&gt;</c> command.</summary>
public sealed record SearchCommandArgs
{
    /// <summary>The raw query string typed by the user.</summary>
    public required string Query { get; init; }

    /// <summary>Maximum number of results to return. Default: 20.</summary>
    public int Limit { get; init; } = 20;

    /// <summary>Whether to return passage-level results instead of file-level. Default: false.</summary>
    public bool Passages { get; init; }

    /// <summary>Whether to strip ANSI highlighting from output. Default: false.</summary>
    public bool NoHighlight { get; init; }

    /// <summary>Output format: text (default) or json.</summary>
    public SearchOutputFormat Format { get; init; } = SearchOutputFormat.Text;
}
```

- [ ] **Step 3: Write failing handler tests**

> **Before writing tests:** read the existing handler tests (e.g., `ConnectorListCommandHandlerTests`) to match the test pattern exactly.

Create `tests/Ferret.Cli.Tests/Search/SearchCommandHandlerTests.cs`:

```csharp
using Ferret.Cli.Search;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class SearchCommandHandlerTests
{
    // ── Exit codes ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Returns_Zero_On_Successful_Search()
    {
        var handler = MakeHandler(providerHits: [MakeHit("doc-1")]);

        var exitCode = await handler.HandleAsync(
            new SearchCommandArgs { Query = "authentication" },
            new StubFerretContext());

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task HandleAsync_Returns_NonZero_On_InvalidQuery()
    {
        var handler = MakeHandler();

        // Empty string → QueryParser returns failure → InvalidQuery
        var exitCode = await handler.HandleAsync(
            new SearchCommandArgs { Query = string.Empty },
            new StubFerretContext());

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task HandleAsync_Returns_NonZero_When_Index_Not_Found()
    {
        var handler = MakeHandler(status: SearchServiceStatus.IndexNotFound);

        var exitCode = await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth" },
            new StubFerretContext());

        Assert.NotEqual(0, exitCode);
    }

    // ── Output content ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Writes_Results_To_Output()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(providerHits: [MakeHit("auth.cs")]);

        await handler.HandleAsync(new SearchCommandArgs { Query = "auth" }, ctx);

        Assert.True(ctx.Output.Length > 0);
    }

    [Fact]
    public async Task HandleAsync_NoHighlight_Output_Contains_No_Escape_Sequences()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(providerHits: [MakeHit("auth.cs")]);

        await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth", NoHighlight = true }, ctx);

        Assert.DoesNotContain("\x1B[", ctx.Output);
    }

    [Fact]
    public async Task HandleAsync_Json_Format_Produces_Valid_Json_Output()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(providerHits: [MakeHit("auth.cs")]);

        await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth", Format = SearchOutputFormat.Json }, ctx);

        Assert.True(IsValidJson(ctx.Output));
    }

    [Fact]
    public async Task HandleAsync_Limit_Is_Passed_To_Search_Service()
    {
        int capturedLimit = 0;
        var stub = new CapturingSearchService(onSearch: opts => capturedLimit = opts.MaxResults);
        var handler = BuildHandlerFromService(stub);

        await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth", Limit = 5 }, new StubFerretContext());

        Assert.Equal(5, capturedLimit);
    }

    [Fact]
    public async Task HandleAsync_Error_Message_Written_On_Failure()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(status: SearchServiceStatus.IndexNotFound);

        await handler.HandleAsync(new SearchCommandArgs { Query = "auth" }, ctx);

        Assert.True(ctx.Output.Length > 0 || ctx.ErrorOutput.Length > 0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchCommandHandler MakeHandler(
        IReadOnlyList<FileSearchHit>? providerHits = null,
        SearchServiceStatus status = SearchServiceStatus.Success) =>
        BuildHandlerFromService(new StubSearchService(providerHits ?? [], status));

    private static SearchCommandHandler BuildHandlerFromService(ISearchService service) =>
        new SearchCommandHandler(
            service,
            new SearchRendererSelector(new NullTextStyler()));

    private static FileSearchHit MakeHit(string name) =>
        new()
        {
            DocumentId = DocumentId.Parse(name),
            ConnectorInstanceId = new ConnectorInstanceId(string.Empty),
            CanonicalUri = new Uri($"file:///{name}"),
            DisplayName = name,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = new HighlightedText([new TextSpan(name, TextSpanKind.Normal)]),
        };

    private static bool IsValidJson(string text)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(text);
            return true;
        }
        catch { return false; }
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubSearchService : ISearchService
    {
        private readonly IReadOnlyList<FileSearchHit> _hits;
        private readonly SearchServiceStatus _status;

        public StubSearchService(IReadOnlyList<FileSearchHit> hits, SearchServiceStatus status)
        {
            _hits = hits;
            _status = status;
        }

        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
        {
            if (string.IsNullOrWhiteSpace(rawQuery))
            {
                return Task.FromResult(SearchServiceResult.Failure(
                    SearchServiceStatus.InvalidQuery, []));
            }

            return Task.FromResult(
                _status == SearchServiceStatus.Success
                    ? SearchServiceResult.Success(_hits, MakeInfo())
                    : SearchServiceResult.Failure(_status, []));
        }

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            Task.FromResult(
                _status == SearchServiceStatus.Success
                    ? SearchServiceResult.Success(_hits, MakeInfo())
                    : SearchServiceResult.Failure(_status, []));

        private static SearchExecutionInfo MakeInfo() =>
            new()
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "stub",
                Duration = TimeSpan.FromMilliseconds(1),
                DocumentsScanned = 0,
                IndexVersion = "stub",
            };
    }

    private sealed class CapturingSearchService : ISearchService
    {
        private readonly Action<SearchOptions> _onSearch;

        public CapturingSearchService(Action<SearchOptions> onSearch) =>
            _onSearch = onSearch;

        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
        {
            _onSearch(options);
            return Task.FromResult(SearchServiceResult.Success([], new SearchExecutionInfo
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "stub",
                Duration = TimeSpan.Zero,
                DocumentsScanned = 0,
                IndexVersion = "stub",
            }));
        }

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options)
        {
            _onSearch(options);
            return Task.FromResult(SearchServiceResult.Success([], new SearchExecutionInfo
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "stub",
                Duration = TimeSpan.Zero,
                DocumentsScanned = 0,
                IndexVersion = "stub",
            }));
        }
    }

    private sealed class StubFerretContext : IFerretContext
    {
        private readonly StringBuilder _output = new();
        private readonly StringBuilder _error = new();

        public string Output => _output.ToString();
        public string ErrorOutput => _error.ToString();

        // Implement IFerretContext per the actual interface — adjust as needed
        public IOutputFormatter OutputFormatter =>
            new LambdaOutputFormatter(line => _output.AppendLine(line));

        public void WriteError(string message) => _error.AppendLine(message);
    }

    private sealed class LambdaOutputFormatter : IOutputFormatter
    {
        private readonly Action<string> _write;
        public LambdaOutputFormatter(Action<string> write) => _write = write;
        public void WriteLine(string line) => _write(line);
        public void Write(string text) => _write(text);
    }
}
```

> **Implementation note:** `IFerretContext`, `IOutputFormatter`, `StubFerretContext`, and `LambdaOutputFormatter` — adjust to match the actual interface signatures. Read `ConnectorListCommandHandlerTests` before implementing.

- [ ] **Step 4: Confirm red**

```
dotnet test tests/Ferret.Cli.Tests --filter "SearchCommandHandlerTests"
```

Expected: FAIL — `SearchCommandHandler` not found.

- [ ] **Step 5: Create `SearchCommandHandler.cs`**

> **Before writing:** read `ConnectorListCommandHandler.cs` to confirm `ICommandHandler<T>` signature and how `IFerretContext`/`IOutputFormatter` is used. Adjust method signatures below to match.

`src/Ferret.Cli/Search/SearchCommandHandler.cs`:

```csharp
using Ferret.Core.Search;

namespace Ferret.Cli.Search;

/// <summary>
/// Handles <c>ferret search &lt;query&gt;</c>.
/// Calls <see cref="ISearchService"/>, builds <see cref="SearchViewModel"/>, and renders via <see cref="SearchRendererSelector"/>.
/// </summary>
public sealed class SearchCommandHandler : ICommandHandler<SearchCommandArgs>
{
    private readonly ISearchService _searchService;
    private readonly SearchRendererSelector _renderer;

    /// <summary>Initialises a new <see cref="SearchCommandHandler"/>.</summary>
    public SearchCommandHandler(ISearchService searchService, SearchRendererSelector renderer)
    {
        _searchService = searchService;
        _renderer = renderer;
    }

    /// <inheritdoc/>
    public async Task<int> HandleAsync(SearchCommandArgs args, IFerretContext context)
    {
        var options = new SearchOptions
        {
            MaxResults = args.Limit,
            ExecutionMode = SearchExecutionMode.Auto,
            IncludePassages = args.Passages,
        };

        var result = await _searchService.SearchAsync(args.Query, options).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var message = result.Status switch
            {
                SearchServiceStatus.InvalidQuery =>
                    $"Invalid query: {(result.Diagnostics.Count > 0 ? result.Diagnostics[0].Message : "empty or whitespace")}",
                SearchServiceStatus.IndexNotFound =>
                    "No search index found. Run 'ferret index' first.",
                SearchServiceStatus.WorkspaceNotFound =>
                    "No workspace found. Run 'ferret workspace init' first.",
                SearchServiceStatus.ProviderUnavailable =>
                    "No search provider is available for this query.",
                _ => $"Search failed: {result.Status}",
            };

            context.OutputFormatter.WriteLine(message);
            return 1;
        }

        var styler = args.NoHighlight ? (ITextStyler)new NullTextStyler() : new AnsiTextStyler();
        var selector = new SearchRendererSelector(styler);

        var viewModel = new SearchViewModel
        {
            OriginalQuery = args.Query,
            Hits = result.Hits,
            ExecutionInfo = result.ExecutionInfo!,
        };

        var output = selector.Render(viewModel, args.Format);
        context.OutputFormatter.WriteLine(output);

        return 0;
    }
}
```

> **Implementation note:** if `IFerretContext` does not have `OutputFormatter.WriteLine`, adjust to use whatever output method the actual context provides. Check `ConnectorListCommandHandler` for the pattern.

- [ ] **Step 6: Confirm green**

```
dotnet test tests/Ferret.Cli.Tests --filter "SearchCommandHandlerTests"
dotnet build src/Ferret.sln
```

Expected: 8 tests pass, 0 build errors.

---

## Task 3: `SearchCliModule` + DI Registration

**Files:**
- Create: `src/Ferret.Cli/Search/SearchCliModule.cs`
- Modify: `src/Ferret.Cli/Program.cs` (or DI composition root — verify path)

**Interfaces:**
- Consumes: `ICliModule`, `CommandDefinition` from `Ferret.Cli`; `SearchCommandHandler`, `SearchCommandArgs`, `SearchOutputFormat` (Task 2); `ISearchService`, `IQueryParser`, `ISearchProvider` from `Ferret.Core.Search`; `QueryParser` from `Ferret.Search`; `BM25SearchProvider`, `SearchService` from `Ferret.Search`
- Produces: `ferret search <query>` command registered in CLI

- [ ] **Step 1: Read `ConnectorCliModule` to confirm `ICliModule` signature**

Read `src/Ferret.Cli/Connectors/ConnectorCliModule.cs` to verify:
- How `CommandDefinition.Group()` or `CommandDefinition.Command()` is used
- How arguments and options are declared
- How handlers are resolved from DI

- [ ] **Step 2: Create `SearchCliModule.cs`**

> **Adjust** option-declaration API to match the actual `CommandDefinition` fluent builder pattern from `ConnectorCliModule`. The pattern below is illustrative — verify against existing modules.

`src/Ferret.Cli/Search/SearchCliModule.cs`:

```csharp
using Ferret.Core.Search;
using Ferret.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Search;

/// <summary>
/// Registers the <c>ferret search &lt;query&gt;</c> command and all required services.
/// Sprint 10: keyword search only. ISearchPostProcessor registrations: none (zero post-processors).
/// </summary>
public sealed class SearchCliModule : ICliModule
{
    /// <inheritdoc/>
    public IEnumerable<CommandDefinition> GetCommands()
    {
        yield return CommandDefinition.Command(
            name: "search",
            description: "Search the workspace index for files matching a query.",
            handler: typeof(SearchCommandHandler),
            configure: cmd =>
            {
                cmd.AddArgument<string>("query", "Search query (keywords, \"phrase\", prefix*)");
                cmd.AddOption<int>("--limit", "Maximum results to return", defaultValue: 20);
                cmd.AddOption<bool>("--passages", "Return passage-level results instead of files");
                cmd.AddOption<bool>("--no-highlight", "Disable ANSI highlighting");
                cmd.AddOption<string>("--format", "Output format: text (default) or json",
                    defaultValue: "text");
            });
    }

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services)
    {
        // Query parsing
        services.AddSingleton<IQueryParser, QueryParser>();

        // Search providers — IEnumerable<ISearchProvider> resolved by SearchService
        services.AddSingleton<ISearchProvider, BM25SearchProvider>();

        // Search service — no post-processors in Sprint 10
        services.AddSingleton<ISearchService, SearchService>();

        // Rendering
        services.AddSingleton<SearchRendererSelector>();

        // Handler
        services.AddTransient<SearchCommandHandler>();
    }
}
```

> **Implementation note:** the `CommandDefinition` API above is illustrative. Match the exact fluent builder calls from `ConnectorCliModule`. The `SearchCommandHandler.HandleAsync` must parse the `--format` string to `SearchOutputFormat` — add a helper or use `Enum.Parse<SearchOutputFormat>` in the handler.

- [ ] **Step 3: Register `SearchCliModule` in `Program.cs`**

Find where existing modules are registered (look for `ConnectorCliModule` registration). Add:

```csharp
// In the module registration block — alongside ConnectorCliModule, WorkspaceCliModule, etc.
modules.Add(new SearchCliModule());
```

Or if modules are registered via DI:
```csharp
services.AddSingleton<ICliModule, SearchCliModule>();
```

- [ ] **Step 4: Build and verify the command appears**

```
dotnet build src/Ferret.sln
dotnet run --project src/Ferret.Cli -- search --help
```

Expected output includes:
```
Description:
  Search the workspace index for files matching a query.

Usage:
  ferret search <query> [options]

Options:
  --limit <limit>    Maximum results to return [default: 20]
  --passages         Return passage-level results instead of files
  --no-highlight     Disable ANSI highlighting
  --format <format>  Output format: text (default) or json [default: text]
```

---

## Task 4: Integration Test

**Files:**
- Create: `tests/Ferret.Cli.Tests/Search/SearchIntegrationTests.cs`

**Interfaces:**
- Consumes: full stack — `SearchService` + `BM25SearchProvider` + `SearchCommandHandler` + `SearchRendererSelector` against a real temp SQLite DB

- [ ] **Step 1: Create integration test**

Create `tests/Ferret.Cli.Tests/Search/SearchIntegrationTests.cs`:

```csharp
using Ferret.Cli.Search;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Cli.Tests.Search;

/// <summary>
/// End-to-end integration test: real SQLite FTS5 database → BM25SearchProvider → SearchService → SearchCommandHandler → rendered output.
/// </summary>
public sealed class SearchIntegrationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly SearchCommandHandler _handler;

    public SearchIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ferret-e2e-{Guid.NewGuid():N}");
        var indexDir = Path.Combine(_tempRoot, ".ferret", "indexes", "keyword");
        Directory.CreateDirectory(indexDir);
        var dbPath = Path.Combine(indexDir, "keyword-index.db");

        SeedDatabase(dbPath);

        var workspaceContext = new StubWorkspaceContext(_tempRoot);
        var queryParser = new QueryParser();
        var provider = new BM25SearchProvider(workspaceContext);
        var service = new SearchService(queryParser, [provider], []);
        var renderer = new SearchRendererSelector(new NullTextStyler());

        _handler = new SearchCommandHandler(service, renderer);
    }

    [Fact]
    public async Task Search_Keyword_Returns_Zero_Exit_Code()
    {
        var ctx = new CapturingContext();
        var exitCode = await _handler.HandleAsync(
            new SearchCommandArgs { Query = "authentication" }, ctx);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Search_Keyword_Produces_Non_Empty_Output()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(new SearchCommandArgs { Query = "authentication" }, ctx);
        Assert.True(ctx.Output.Length > 0);
    }

    [Fact]
    public async Task Search_Keyword_Output_Contains_Matching_Document_Name()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(new SearchCommandArgs { Query = "authentication" }, ctx);
        Assert.Contains("AuthenticationToken", ctx.Output);
    }

    [Fact]
    public async Task Search_Json_Format_Is_Valid_Json()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(
            new SearchCommandArgs { Query = "authentication", Format = SearchOutputFormat.Json }, ctx);
        var doc = System.Text.Json.JsonDocument.Parse(ctx.Output);
        Assert.NotNull(doc);
    }

    [Fact]
    public async Task Search_Empty_Query_Returns_NonZero_Exit_Code()
    {
        var exitCode = await _handler.HandleAsync(
            new SearchCommandArgs { Query = string.Empty }, new CapturingContext());
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task Search_Phrase_Returns_Matching_Results()
    {
        var ctx = new CapturingContext();
        var exitCode = await _handler.HandleAsync(
            new SearchCommandArgs { Query = "\"runtime initialization\"" }, ctx);
        Assert.Equal(0, exitCode);
        Assert.Contains("RuntimeBuilder", ctx.Output);
    }

    [Fact]
    public async Task Search_Prefix_Returns_Results_Starting_With_Stem()
    {
        var ctx = new CapturingContext();
        var exitCode = await _handler.HandleAsync(
            new SearchCommandArgs { Query = "auth*" }, ctx);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Search_Limit_One_Returns_At_Most_One_Result()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(
            new SearchCommandArgs { Query = "content", Limit = 1, Format = SearchOutputFormat.Json }, ctx);
        var doc = System.Text.Json.JsonDocument.Parse(ctx.Output);
        var total = doc.RootElement.GetProperty("total").GetInt32();
        Assert.True(total <= 1);
    }

    private static void SeedDatabase(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE documents (
                id TEXT PRIMARY KEY,
                asset_id TEXT NOT NULL,
                connector_instance_id TEXT NOT NULL DEFAULT '',
                canonical_uri TEXT NOT NULL DEFAULT '',
                title TEXT NOT NULL DEFAULT ''
            );
            CREATE VIRTUAL TABLE documents_fts USING fts5(
                title, body, content='documents', content_rowid='rowid'
            );
            INSERT INTO documents VALUES
                ('doc-1','a-1','filesystem','file:///auth/token.cs','AuthenticationToken'),
                ('doc-2','a-2','filesystem','file:///auth/session.cs','SessionManager'),
                ('doc-3','a-3','filesystem','file:///runtime/builder.cs','RuntimeBuilder');
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
        catch (IOException) { /* best-effort */ }
    }

    private sealed class StubWorkspaceContext : IWorkspaceContext
    {
        public StubWorkspaceContext(string root) => WorkspaceRoot = new DirectoryInfo(root);
        public WorkspaceId WorkspaceId => new("e2e-test");
        public DirectoryInfo WorkspaceRoot { get; }
    }

    private sealed class CapturingContext : IFerretContext
    {
        private readonly StringBuilder _sb = new();
        public string Output => _sb.ToString();

        // Adjust to match actual IFerretContext interface
        public IOutputFormatter OutputFormatter => new InlineFormatter(_sb);

        private sealed class InlineFormatter : IOutputFormatter
        {
            private readonly StringBuilder _sb;
            public InlineFormatter(StringBuilder sb) => _sb = sb;
            public void WriteLine(string line) => _sb.AppendLine(line);
            public void Write(string text) => _sb.Append(text);
        }
    }
}
```

> **Implementation note:** `IFerretContext` and `IOutputFormatter` — adjust stubs to match the actual interface. Reference `ConnectorListCommandHandlerTests` for the test-double pattern.

- [ ] **Step 2: Confirm integration tests pass**

```
dotnet test tests/Ferret.Cli.Tests --filter "SearchIntegrationTests"
```

Expected: 8 tests pass.

---

## Task 5: Manual Smoke Test

> **This task is manual.** No automated test can substitute for verifying the actual terminal output with ANSI highlighting.

- [ ] **Step 1: Initialize a test workspace**

```
dotnet run --project src/Ferret.Cli -- workspace init
dotnet run --project src/Ferret.Cli -- index
```

- [ ] **Step 2: Run a keyword search**

```
dotnet run --project src/Ferret.Cli -- search authentication
```

Expected:
- Ranked file list with highlighted snippets (bold ANSI for matches)
- Footer showing result count, provider name, and duration

- [ ] **Step 3: Run a phrase search**

```
dotnet run --project src/Ferret.Cli -- search "runtime builder"
```

- [ ] **Step 4: Run a prefix search**

```
dotnet run --project src/Ferret.Cli -- search auth*
```

- [ ] **Step 5: Test JSON format**

```
dotnet run --project src/Ferret.Cli -- search authentication --format json
```

Expected: valid JSON with `query`, `total`, `hits` fields.

- [ ] **Step 6: Test `--no-highlight`**

```
dotnet run --project src/Ferret.Cli -- search authentication --no-highlight
```

Expected: same output as text format but no ANSI escape sequences.

- [ ] **Step 7: Test `--limit`**

```
dotnet run --project src/Ferret.Cli -- search authentication --limit 3
```

Expected: at most 3 results.

---

## Task 6: Sprint Wrap-up

**Files:**
- Modify: `docs/000-Overview/PROJECT-STATE.md`

- [ ] **Step 1: Run full test suite — confirm all pass**

```
dotnet test tests/Ferret.Core.Tests
dotnet test tests/Ferret.Search.Tests
dotnet test tests/Ferret.Cli.Tests
dotnet test tests/Ferret.Architecture.Tests
```

Expected: all pass. Total new tests in Sprint 10: ~100+.

- [ ] **Step 2: Full solution build**

```
dotnet build src/Ferret.sln -c Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Update `PROJECT-STATE.md`**

Update the following fields in `docs/000-Overview/PROJECT-STATE.md`:

- `**Last Updated**` → `2026-06-28` (or actual date)
- `**Current version**` → `0.10.0 (Sprint 10 complete)`
- `**Current sprint**` → `Sprint 11 — MCP Server (planned)`
- Add Sprint 10 row to the Completed Sprints table:

```markdown
| Sprint 10 | Information Retrieval | Done | 100+ | `v0.10.0-sprint10` |
```

- Add Sprint 10 detail section (after Sprint 8 section):

```markdown
## Sprint 10 — Information Retrieval (v0.10.0)

**Status:** Complete
**Tag:** `v0.10.0-sprint10`
**Date:** 2026-06-28

### Delivered

- `Ferret.Core.Search` — 20 contract types: `SearchExpression` AST hierarchy, `SearchHit` hierarchy, `ISearchService`, `IQueryParser`, `ISearchProvider`, `ISearchPostProcessor`, `SearchParseResult`, `SearchQuery`, `SearchOptions`, `SearchExecutionInfo`, `HighlightedText`, `TextSpan`, `SearchServiceResult`, `SearchDiagnostic`, `SearchProviderResult`
- `Ferret.Search` — `QueryParser` (implements `IQueryParser`), internal `Lexer` + `Token`, `BM25SearchProvider` (SQLite FTS5), `QueryTranslator` (AST → FTS5), `HighlightParser` (sentinels → spans), `SearchService` (orchestrates providers + post-processors)
- `Ferret.Cli` additions — `ITextStyler`, `AnsiTextStyler`, `NullTextStyler`, `SearchRendererSelector`, `SearchViewModel`, `SearchCommandHandler`, `SearchCliModule`
- ADR-0015: Information Retrieval Architecture (5 principles)
- `ferret search <query>` — BM25 keyword search with ranked results and ANSI highlighting
- `ferret search --format json` — machine-readable JSON output
- `ferret search --limit N` — result count control
- `ferret search --no-highlight` — plain text output
- `ISearchProvider` extensibility — `SemanticSearchProvider` stub reserved (Sprint 11+)

### Architecture Documents

- ADR-0015: `docs/adr/0015-information-retrieval-architecture.md`
- Spec: `docs/superpowers/specs/2026-06-28-sprint-10-information-retrieval-design.md`

### What a new user can do after Sprint 10

Run `ferret search authentication` to get ranked, highlighted search results from the workspace index.
```

- Update the CLI Commands table to mark `ferret search` as Shipped.

- [ ] **Step 4: Commit sprint wrap-up**

```bash
git add src/ tests/ docs/
git commit -m "feat(sprint-10): ferret search — BM25 keyword search, ANSI highlighting, JSON output; sprint 10 complete"
```

- [ ] **Step 5: Tag the sprint**

```bash
git tag v0.10.0-sprint10
```

---

## Section 5 Complete — Sprint 10 Done

**Sprint 10 deliverables:**
- `ferret search <query>` — working keyword search
- `ferret search "phrase"` — phrase matching
- `ferret search prefix*` — prefix matching
- `ferret search --format json` — machine-readable output
- `ferret search --no-highlight` — plain text
- `ferret search --limit N` — result cap
- `ISearchProvider` extensibility — ready for semantic search without redesign
- ADR-0015 governing all search architecture decisions
- 100+ new tests across `Ferret.Core.Tests`, `Ferret.Search.Tests`, `Ferret.Cli.Tests`
- Tag: `v0.10.0-sprint10`

**Sprint 11 (MCP Server) prerequisites satisfied:**
- `ISearchService` — MCP server calls it to answer search queries from AI agents
- `SearchQuery` AST — MCP server passes structured queries
- `SearchHit` + `HighlightedText` — MCP server serializes hits as MCP resources
- `ISearchProvider` extensibility — Sprint 11 or 12 can add `SemanticSearchProvider` without changing `SearchService`
