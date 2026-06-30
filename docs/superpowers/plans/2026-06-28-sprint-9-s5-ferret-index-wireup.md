# Sprint 9 — Section 5: Final Assembly — `ferret index` ✅ COMPLETE (2026-06-28)

> **Status:** All tasks T1–T4 are complete. 651 tests passing. Tag `v0.9.0-sprint9` applied.

# Sprint 9 — Section 5: Final Assembly — `ferret index`

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Section goal:** Final assembly — wire all Sprint 9 pieces together and deliver `ferret index`. This is intentionally thin: no new platform concepts, only composition. After this section, `ferret index` is a working command that discovers files via `FilesystemConnector`, parses content via the Parser Platform, and writes documents to a SQLite FTS5 database at `.ferret/indexes/keyword/keyword-index.db`.

**Architecture:** `IWorkspaceContext` replaces all direct `Directory.GetCurrentDirectory()` calls. The CLI composition root builds `IWorkspaceContext` once at startup from CWD + `workspace.json` manifest, then passes it into every module that needs workspace paths. `IIndexEngine` (with resolved db path) is registered before `IndexingModule.ConfigureServices` so it is available when `IIndexPipeline` resolves. `IndexCommandHandler` consumes `IIndexPipeline` and `IWorkspaceContext`; it never reads `workspace.json` or calls CWD directly.

**S5 contract update:** `IIndexPipeline.RunAsync` gains a `WorkspaceId` first parameter. S3 defined `RunAsync(IndexPipelineOptions, CancellationToken)` — S5 promotes workspace identity to a first-class pipeline parameter. This is a non-breaking change (S3 is not yet merged to main).

**Tech stack:** .NET 9 / C# 13, StyleCop + `AnalysisMode=All`, `sealed` on all concrete classes, `required` on record/class properties with no sensible default.

---

## Prerequisites

Sections 1–4 must be **complete** before starting this section:
- `IAssetReader`, `IParserDispatcher`, `IIndexPipeline`, `SqliteKeywordIndexEngine`, `IndexingModule` present and green
- `FilesystemConnector` implements `IAssetReader`
- `ConnectorCliModule` registered (Sprint 9 S4 output) — or `ConnectorManager`/`IConnectorRegistry` wired via whatever module S4 produced
- `dotnet test src/Ferret.sln` passes on all existing test projects
- `dotnet build src/Ferret.sln` passes

---

## Global Constraints

- All non-private members require XML doc comments (StyleCop SA1600)
- `sealed` on all concrete classes
- `required` keyword on record/class properties with no sensible default
- `IWorkspaceContext` registered as singleton — all consumers receive it via DI
- `IIndexEngine` registration must precede `IndexingModule.ConfigureServices` in startup order
- `IndexCommandHandler` never calls `Directory.GetCurrentDirectory()` or reads `workspace.json` directly
- `IOutputFormatter` has `WriteLine(string)` but no `Write(string)` — use `WriteLine` for each output line; do not introduce a `Write` overload unless `IOutputFormatter` is extended in this section
- `ExecuteAsync` returns `Task<CommandResult>` (not `Task<int>`); use `CommandResult.Success` and `CommandResult.Failure`
- Output is accessed via `context.Services.Output` (not `context.Output`)
- `dotnet build` and `dotnet test` must pass before every commit
- Commit prefix: `feat(sprint-9):`, `test(sprint-9):`, `chore(sprint-9):`
- **Single commit at sprint end** — accumulate all Sprint 9 changes, commit once with the full sprint message

---

## File Inventory

### New Source Files (Ferret.Core)

| File |
|---|
| `src/Ferret.Core/Workspace/IWorkspaceContext.cs` |
| `src/Ferret.Core/Indexing/IndexLayout.cs` |
| `src/Ferret.Core/Indexing/IProgressReporter.cs` (reserved stub) |

### New Source Files (Ferret.Workspace)

| File |
|---|
| `src/Ferret.Workspace/DefaultWorkspaceContext.cs` |

### New Source Files (Ferret.Cli)

| File |
|---|
| `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs` |
| `src/Ferret.Cli/Commands/Indexing/IndexCommandHandler.cs` |
| `src/Ferret.Cli/Commands/Indexing/ViewModels/IndexSummaryViewModel.cs` |
| `src/Ferret.Cli/Commands/Indexing/Formatting/TextIndexSummaryFormatter.cs` |

### Modified Source Files

| File | Change |
|---|---|
| `src/Ferret.Core/Indexing/IIndexPipeline.cs` | Add `WorkspaceId` first parameter to `RunAsync` |
| `src/Ferret.Indexing/IndexPipeline.cs` | Update `RunAsync` signature to accept `WorkspaceId` |
| `src/Ferret.Cli/Commands/CoreCliModule.cs` | Remove `index` empty-group stub; register via `IndexCliModule` |
| `src/Ferret.Cli/Program.cs` | Build `IWorkspaceContext`, register modules in correct order |

### New Test Files

| File |
|---|
| `tests/Ferret.Core.Tests/Workspace/WorkspaceContextTests.cs` |
| `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCommandHandlerTests.cs` |
| `tests/Ferret.Indexing.Tests/EndToEnd/IndexPipelineEndToEndTests.cs` |
| `tests/Ferret.Indexing.Tests/Performance/IndexPipelineBenchmarkTests.cs` |

### New Doc Files

| File | Change |
|---|---|
| `docs/000-Overview/PROJECT-STATE.md` | Sprint 9 complete, Sprint 10 next |
| `docs/001-Product/ROADMAP-001.md` | Sprint 9 checked off, Sprint 10 renamed |

---

## Task 1: `IWorkspaceContext` + `IndexLayout` + `IProgressReporter` + `DefaultWorkspaceContext`

**Why first:** `IndexCliModule`, `IndexCommandHandler`, and the updated `ConnectorCliModule` all consume `IWorkspaceContext`. `IndexLayout` constants are used in both `IndexCliModule.ConfigureServices` (to build the db path) and `IndexCommandHandler.ExecuteAsync` (to format the db path in output). `IProgressReporter` is a reserved stub — add it now so Sprint 10 can extend without a Core contract change.

**Files:**
- Create: `src/Ferret.Core/Workspace/IWorkspaceContext.cs`
- Create: `src/Ferret.Core/Indexing/IndexLayout.cs`
- Create: `src/Ferret.Core/Indexing/IProgressReporter.cs`
- Create: `src/Ferret.Workspace/DefaultWorkspaceContext.cs`
- Create: `tests/Ferret.Core.Tests/Workspace/WorkspaceContextTests.cs`

**Interfaces:**
- Produces: `IWorkspaceContext`, `IndexLayout`, `IProgressReporter`, `DefaultWorkspaceContext` — consumed by Tasks 2, 3

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Core.Tests/Workspace/WorkspaceContextTests.cs`:

```csharp
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Workspace;
using Xunit;

namespace Ferret.Core.Tests.Workspace;

public sealed class WorkspaceContextTests
{
    [Fact]
    public void IWorkspaceContext_Is_An_Interface()
    {
        Assert.True(typeof(IWorkspaceContext).IsInterface);
    }

    [Fact]
    public void IWorkspaceContext_Has_WorkspaceId_Property()
    {
        var prop = typeof(IWorkspaceContext).GetProperty("WorkspaceId");

        Assert.NotNull(prop);
        Assert.Equal(typeof(WorkspaceId), prop.PropertyType);
    }

    [Fact]
    public void IWorkspaceContext_Has_WorkspaceRoot_Property()
    {
        var prop = typeof(IWorkspaceContext).GetProperty("WorkspaceRoot");

        Assert.NotNull(prop);
        Assert.Equal(typeof(WorkspacePath), prop.PropertyType);
    }

    [Fact]
    public void DefaultWorkspaceContext_Exposes_WorkspaceId_Correctly()
    {
        var id = WorkspaceId.Create("test-id");
        var path = WorkspacePath.Create(Environment.CurrentDirectory);
        var ctx = new DefaultWorkspaceContext(id, path);

        Assert.Equal(id, ctx.WorkspaceId);
    }

    [Fact]
    public void DefaultWorkspaceContext_Exposes_WorkspaceRoot_Correctly()
    {
        var id = WorkspaceId.Create("test-id");
        var path = WorkspacePath.Create(Environment.CurrentDirectory);
        var ctx = new DefaultWorkspaceContext(id, path);

        Assert.Equal(path, ctx.WorkspaceRoot);
    }

    [Fact]
    public void IndexLayout_IndexDirectoryName_Is_Indexes()
    {
        Assert.Equal("indexes", IndexLayout.IndexDirectoryName);
    }

    [Fact]
    public void IndexLayout_KeywordDirectoryName_Is_Keyword()
    {
        Assert.Equal("keyword", IndexLayout.KeywordDirectoryName);
    }

    [Fact]
    public void IndexLayout_KeywordDatabaseFileName_Is_Keyword_Index_Db()
    {
        Assert.Equal("keyword-index.db", IndexLayout.KeywordDatabaseFileName);
    }

    [Fact]
    public void IndexLayout_Constants_Combine_To_Correct_Relative_Path()
    {
        var relative = Path.Combine(
            WorkspaceLayout.RootDirectoryName,
            IndexLayout.IndexDirectoryName,
            IndexLayout.KeywordDirectoryName,
            IndexLayout.KeywordDatabaseFileName);

        Assert.Equal(
            Path.Combine(".ferret", "indexes", "keyword", "keyword-index.db"),
            relative);
    }

    [Fact]
    public void IProgressReporter_Is_An_Interface()
    {
        Assert.True(typeof(Ferret.Core.Indexing.IProgressReporter).IsInterface);
    }
}
```

Note: Read `WorkspaceId.cs` and `WorkspacePath.cs` in `Ferret.Core/Primitives` to confirm the correct factory method (`Create` vs constructor). Adjust the test helpers to match the actual API. `WorkspaceLayout` is in `Ferret.Core.Workspace` — read it to confirm `RootDirectoryName` constant name.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Core.Tests --filter "WorkspaceContextTests"
```

Expected: FAIL — `IWorkspaceContext`, `IndexLayout`, `DefaultWorkspaceContext`, `IProgressReporter` not found.

- [ ] **Step 3: Create `IWorkspaceContext.cs`**

`src/Ferret.Core/Workspace/IWorkspaceContext.cs`:

```csharp
using Ferret.Core.Primitives;

namespace Ferret.Core.Workspace;

/// <summary>Provides workspace context to subsystems that need root path, ID, and layout.
/// Registered as a singleton in the CLI composition root. All commands and modules that
/// need workspace location consume this interface — never call <c>Directory.GetCurrentDirectory()</c>
/// or read <c>workspace.json</c> directly.</summary>
public interface IWorkspaceContext
{
    /// <summary>Gets the workspace unique identifier.</summary>
    WorkspaceId WorkspaceId { get; }

    /// <summary>Gets the workspace root path.</summary>
    WorkspacePath WorkspaceRoot { get; }

    // Reserved: WorkspaceMetadata Metadata { get; }
}
```

- [ ] **Step 4: Create `IndexLayout.cs`**

`src/Ferret.Core/Indexing/IndexLayout.cs`:

```csharp
namespace Ferret.Core.Indexing;

/// <summary>Conventional paths within the <c>.ferret</c> index directory.
/// Mirrors <see cref="Ferret.Core.Workspace.WorkspaceLayout"/> for the index subsystem.
/// Used by the CLI host (S5) to build the SQLite database path and by
/// <c>IndexCommandHandler</c> to display the resolved path in command output.</summary>
public static class IndexLayout
{
    /// <summary>Subdirectory containing all index databases. Relative to <c>.ferret/</c>.</summary>
    public const string IndexDirectoryName = "indexes";

    /// <summary>Subdirectory for keyword (FTS5) index. Relative to <c>.ferret/indexes/</c>.</summary>
    public const string KeywordDirectoryName = "keyword";

    /// <summary>Filename of the keyword FTS5 database.</summary>
    public const string KeywordDatabaseFileName = "keyword-index.db";

    // Reserved: VectorDirectoryName = "vector"
    // Reserved: AnalyticsDirectoryName = "analytics"
    // Reserved: CacheDirectoryName = "cache"
}
```

- [ ] **Step 5: Create `IProgressReporter.cs`**

`src/Ferret.Core/Indexing/IProgressReporter.cs`:

```csharp
namespace Ferret.Core.Indexing;

/// <summary>Reports live progress during pipeline operations. Reserved for Sprint 10+.
/// Implementations will be injected into <c>IIndexPipeline</c> to surface per-document
/// progress events to CLI spinners, log sinks, or IPC streams.</summary>
public interface IProgressReporter
{
    // Reserved: void Report(IndexProgress progress);
    // Reserved: IAsyncEnumerable<IndexProgress> WatchAsync(CancellationToken ct);
}
```

- [ ] **Step 6: Create `DefaultWorkspaceContext.cs`**

`src/Ferret.Workspace/DefaultWorkspaceContext.cs`:

Read `src/Ferret.Core/Primitives/WorkspaceId.cs` and `src/Ferret.Core/Primitives/WorkspacePath.cs` first to confirm exact type names and whether there are equality constraints.

```csharp
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;

namespace Ferret.Workspace;

/// <summary>
/// Default implementation of <see cref="IWorkspaceContext"/>.
/// Built once by the CLI composition root from CWD + workspace.json manifest.
/// Registered as a singleton so all commands share the same workspace identity.
/// </summary>
internal sealed class DefaultWorkspaceContext : IWorkspaceContext
{
    /// <summary>Initializes a new <see cref="DefaultWorkspaceContext"/>.</summary>
    /// <param name="workspaceId">The workspace identifier read from workspace.json.</param>
    /// <param name="workspaceRoot">The workspace root path (CWD at startup).</param>
    public DefaultWorkspaceContext(WorkspaceId workspaceId, WorkspacePath workspaceRoot)
    {
        WorkspaceId = workspaceId;
        WorkspaceRoot = workspaceRoot;
    }

    /// <inheritdoc/>
    public WorkspaceId WorkspaceId { get; }

    /// <inheritdoc/>
    public WorkspacePath WorkspaceRoot { get; }
}
```

- [ ] **Step 7: Confirm green**

```
dotnet test tests/Ferret.Core.Tests --filter "WorkspaceContextTests"
dotnet test tests/Ferret.Core.Tests
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 2: `IndexSummaryViewModel` + `TextIndexSummaryFormatter` + `IndexCommandHandler`

**Why:** These three types are the CLI-facing layer for `ferret index`. `IndexSummaryViewModel` maps `IndexResult` to a display model (no direct use of domain types in the formatter). `TextIndexSummaryFormatter` formats the view model as human-readable text. `IndexCommandHandler` orchestrates: resolve options → call pipeline → patch Duration → format → output → return `CommandResult`.

**Key discrepancy vs spec:** `ExecuteAsync` returns `Task<CommandResult>` (not `Task<int>`). Output is accessed via `context.Services.Output` (not `context.Output`). Use `CommandResult.Success` and `CommandResult.Failure`. `IOutputFormatter` has `WriteLine(string)` only — call it once per line; the formatter's `Format` method returns a multi-line string; split on newline and call `WriteLine` for each, or add a `Write(string text)` method to `IOutputFormatter` if the team prefers avoiding the split. The simplest approach: call `context.Services.Output.WriteLine(_formatter.Format(vm))` directly since `Format` produces a single multi-line string and `WriteLine` appends one trailing newline — acceptable for CLI output.

**Files:**
- Create: `src/Ferret.Cli/Commands/Indexing/ViewModels/IndexSummaryViewModel.cs`
- Create: `src/Ferret.Cli/Commands/Indexing/Formatting/TextIndexSummaryFormatter.cs`
- Create: `src/Ferret.Cli/Commands/Indexing/IndexCommandHandler.cs`
- Create: `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IIndexPipeline`, `IWorkspaceContext`, `IndexLayout`, `WorkspaceLayout`, `IndexResult`, `CommandResult`, `IFerretContext`
- Produces: `IndexSummaryViewModel`, `TextIndexSummaryFormatter`, `IndexCommandHandler` — consumed by Task 3 (`IndexCliModule`)

- [ ] **Step 1: Write failing tests**

Create `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCommandHandlerTests.cs`:

```csharp
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Indexing.Formatting;
using Ferret.Cli.Commands.Indexing.ViewModels;
using Ferret.Core.Indexing;
using Xunit;

namespace Ferret.Cli.Tests.Commands.Indexing;

public sealed class IndexCommandHandlerTests
{
    [Fact]
    public void IndexSummaryViewModel_From_Maps_AssetsDiscovered()
    {
        var result = BuildResult(discovered: 5, indexed: 3, skipped: 1, failures: 1);
        var vm = IndexSummaryViewModel.From(result, "/tmp/keyword-index.db");

        Assert.Equal(5, vm.AssetsDiscovered);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_DocumentsIndexed()
    {
        var result = BuildResult(indexed: 4);
        var vm = IndexSummaryViewModel.From(result, "/tmp/keyword-index.db");

        Assert.Equal(4, vm.DocumentsIndexed);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_Failures()
    {
        var result = BuildResult(failures: 2, failureMessages: ["file-a: parse error", "file-b: unsupported"]);
        var vm = IndexSummaryViewModel.From(result, "/tmp/keyword-index.db");

        Assert.Equal(2, vm.Failures);
        Assert.Equal(2, vm.FailureMessages.Count);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_DatabasePath()
    {
        var result = BuildResult();
        var vm = IndexSummaryViewModel.From(result, "/some/path/keyword-index.db");

        Assert.Equal("/some/path/keyword-index.db", vm.DatabasePath);
    }

    [Fact]
    public void IndexSummaryViewModel_From_Maps_Duration()
    {
        var duration = TimeSpan.FromSeconds(2.5);
        var result = BuildResult(duration: duration);
        var vm = IndexSummaryViewModel.From(result, "/tmp/keyword-index.db");

        Assert.Equal(duration, vm.Duration);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Discovered()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(discovered: 10);

        var output = formatter.Format(vm);

        Assert.Contains("10", output);
        Assert.Contains("Discovered", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Indexed()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(indexed: 8);

        var output = formatter.Format(vm);

        Assert.Contains("8", output);
        Assert.Contains("Indexed", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Skipped_And_Failed()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(skipped: 2, failures: 1);

        var output = formatter.Format(vm);

        Assert.Contains("Skipped", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Failed", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_Duration()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(duration: TimeSpan.FromSeconds(1.23));

        var output = formatter.Format(vm);

        Assert.Contains("Duration", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1.23", output);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_DatabasePath()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(dbPath: "/path/to/keyword-index.db");

        var output = formatter.Format(vm);

        Assert.Contains("/path/to/keyword-index.db", output);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Contains_FailureMessages_When_Failures_NonZero()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(failures: 1, failureMessages: ["bad-file.cs: syntax error"]);

        var output = formatter.Format(vm);

        Assert.Contains("bad-file.cs: syntax error", output);
        Assert.Contains("Failures", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TextIndexSummaryFormatter_Format_Does_Not_Contain_Failures_Section_When_Zero()
    {
        var formatter = new TextIndexSummaryFormatter();
        var vm = BuildViewModel(failures: 0);

        var output = formatter.Format(vm);

        // The "Failures:" section header should not appear when there are no failures
        Assert.DoesNotContain("Failures:", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handler_Returns_Success_When_No_Failures()
    {
        var fakePipeline = new FakeIndexPipeline(BuildResult(failures: 0));
        var fakeContext = new FakeWorkspaceContext();
        var formatter = new TextIndexSummaryFormatter();
        var handler = new IndexCommandHandler(fakePipeline, fakeContext, formatter);

        var result = await handler.ExecuteAsync(FerretContext.CreateTest(TextWriter.Null));

        Assert.Equal(Ferret.Cli.Cli.CommandResult.Success, result);
    }

    [Fact]
    public async Task Handler_Returns_Failure_When_Failures_NonZero()
    {
        var fakePipeline = new FakeIndexPipeline(BuildResult(failures: 1));
        var fakeContext = new FakeWorkspaceContext();
        var formatter = new TextIndexSummaryFormatter();
        var handler = new IndexCommandHandler(fakePipeline, fakeContext, formatter);

        var result = await handler.ExecuteAsync(FerretContext.CreateTest(TextWriter.Null));

        Assert.Equal(Ferret.Cli.Cli.CommandResult.Failure, result);
    }

    [Fact]
    public async Task Handler_Passes_ForceRebuild_True_When_Rebuild_Option_Set()
    {
        var fakePipeline = new FakeIndexPipeline(BuildResult());
        var fakeContext = new FakeWorkspaceContext();
        var formatter = new TextIndexSummaryFormatter();
        var handler = new IndexCommandHandler(fakePipeline, fakeContext, formatter);
        var ctx = FerretContext.CreateTest(
            TextWriter.Null,
            options: new Dictionary<string, object?> { ["rebuild"] = true });

        await handler.ExecuteAsync(ctx);

        Assert.True(fakePipeline.LastOptions?.ForceRebuild);
    }

    [Fact]
    public async Task Handler_Passes_ForceRebuild_False_When_Rebuild_Option_Not_Set()
    {
        var fakePipeline = new FakeIndexPipeline(BuildResult());
        var fakeContext = new FakeWorkspaceContext();
        var formatter = new TextIndexSummaryFormatter();
        var handler = new IndexCommandHandler(fakePipeline, fakeContext, formatter);

        await handler.ExecuteAsync(FerretContext.CreateTest(TextWriter.Null));

        Assert.False(fakePipeline.LastOptions?.ForceRebuild ?? true);
    }

    // ---- Helpers ----

    private static IndexResult BuildResult(
        int discovered = 0,
        int indexed = 0,
        int skipped = 0,
        int failures = 0,
        TimeSpan duration = default,
        IReadOnlyList<string>? failureMessages = null)
    {
        return new IndexResult
        {
            AssetsDiscovered = discovered,
            AssetsProcessed  = indexed + skipped + failures,
            DocumentsIndexed = indexed,
            DocumentsSkipped = skipped,
            Failures         = failures,
            Warnings         = 0,
            Duration         = duration,
            FailureMessages  = failureMessages ?? [],
        };
    }

    private static IndexSummaryViewModel BuildViewModel(
        int discovered = 5,
        int indexed = 3,
        int skipped = 1,
        int failures = 0,
        TimeSpan duration = default,
        string dbPath = "/tmp/keyword-index.db",
        IReadOnlyList<string>? failureMessages = null)
    {
        return new IndexSummaryViewModel
        {
            AssetsDiscovered = discovered,
            AssetsProcessed  = indexed + skipped + failures,
            DocumentsIndexed = indexed,
            DocumentsSkipped = skipped,
            Failures         = failures,
            Duration         = duration == default ? TimeSpan.FromSeconds(1.0) : duration,
            DatabasePath     = dbPath,
            FailureMessages  = failureMessages ?? [],
        };
    }

    // ---- Inner fakes ----

    private sealed class FakeIndexPipeline : Ferret.Core.Indexing.IIndexPipeline
    {
        private readonly IndexResult _result;

        internal FakeIndexPipeline(IndexResult result) => _result = result;

        internal IndexPipelineOptions? LastOptions { get; private set; }

        public Task<IndexResult> RunAsync(
            Ferret.Core.Primitives.WorkspaceId workspaceId,
            IndexPipelineOptions options,
            CancellationToken ct = default)
        {
            LastOptions = options;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeWorkspaceContext : Ferret.Core.Workspace.IWorkspaceContext
    {
        public Ferret.Core.Primitives.WorkspaceId WorkspaceId =>
            Ferret.Core.Primitives.WorkspaceId.Create("test");

        public Ferret.Core.Primitives.WorkspacePath WorkspaceRoot =>
            Ferret.Core.Primitives.WorkspacePath.Create(
                Path.GetTempPath());
    }
}
```

Note: Read `FerretContext.CreateTest` signature in `src/Ferret.Cli/Cli/FerretContext.cs` before writing. The test uses `FerretContext.CreateTest(TextWriter.Null, options: ...)` — adjust to match the actual method signature. Read `IIndexPipeline.cs` after S3 is merged to confirm the exact `RunAsync` signature before writing fakes.

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Cli.Tests --filter "IndexCommandHandlerTests"
```

Expected: FAIL — `IndexSummaryViewModel`, `TextIndexSummaryFormatter`, `IndexCommandHandler` not found.

- [ ] **Step 3: Create `IndexSummaryViewModel.cs`**

`src/Ferret.Cli/Commands/Indexing/ViewModels/IndexSummaryViewModel.cs`:

```csharp
using Ferret.Core.Indexing;

namespace Ferret.Cli.Commands.Indexing.ViewModels;

/// <summary>View model for the <c>ferret index</c> summary output.
/// Decouples the formatter from <see cref="IndexResult"/> domain types.</summary>
public sealed record IndexSummaryViewModel
{
    /// <summary>Gets the number of assets discovered across all connectors.</summary>
    public required int AssetsDiscovered { get; init; }

    /// <summary>Gets the number of assets that entered the parse stage.</summary>
    public required int AssetsProcessed { get; init; }

    /// <summary>Gets the number of documents successfully indexed.</summary>
    public required int DocumentsIndexed { get; init; }

    /// <summary>Gets the number of documents skipped (unsupported type, empty content, no reader).</summary>
    public required int DocumentsSkipped { get; init; }

    /// <summary>Gets the number of per-asset failures (parse errors, open errors).</summary>
    public required int Failures { get; init; }

    /// <summary>Gets the wall-clock duration of the indexing run.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Gets the absolute path to the SQLite database file.</summary>
    public required string DatabasePath { get; init; }

    /// <summary>Gets the failure messages, if any. Empty list when <see cref="Failures"/> is zero.</summary>
    public IReadOnlyList<string> FailureMessages { get; init; } = [];

    /// <summary>Maps an <see cref="IndexResult"/> and resolved database path to a view model.</summary>
    /// <param name="result">The pipeline result.</param>
    /// <param name="databasePath">Absolute path to the SQLite database file.</param>
    /// <returns>A populated <see cref="IndexSummaryViewModel"/>.</returns>
    public static IndexSummaryViewModel From(IndexResult result, string databasePath) => new()
    {
        AssetsDiscovered = result.AssetsDiscovered,
        AssetsProcessed  = result.AssetsProcessed,
        DocumentsIndexed = result.DocumentsIndexed,
        DocumentsSkipped = result.DocumentsSkipped,
        Failures         = result.Failures,
        Duration         = result.Duration,
        DatabasePath     = databasePath,
        FailureMessages  = result.FailureMessages,
    };
}
```

- [ ] **Step 4: Create `TextIndexSummaryFormatter.cs`**

`src/Ferret.Cli/Commands/Indexing/Formatting/TextIndexSummaryFormatter.cs`:

```csharp
using System.Text;
using Ferret.Cli.Commands.Indexing.ViewModels;

namespace Ferret.Cli.Commands.Indexing.Formatting;

/// <summary>Formats an <see cref="IndexSummaryViewModel"/> as human-readable text for the CLI.</summary>
internal sealed class TextIndexSummaryFormatter
{
    /// <summary>Formats the view model as a multiline summary string.</summary>
    /// <param name="vm">The view model to format.</param>
    /// <returns>A multiline string suitable for output via <c>IOutputFormatter.WriteLine</c>.</returns>
    public string Format(IndexSummaryViewModel vm)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"  Discovered   {vm.AssetsDiscovered,6} assets");
        sb.AppendLine($"  Indexed      {vm.DocumentsIndexed,6} documents");
        sb.AppendLine($"  Skipped      {vm.DocumentsSkipped,6}");
        sb.AppendLine($"  Failed       {vm.Failures,6}");
        sb.AppendLine($"  Duration     {vm.Duration.TotalSeconds:F2}s");
        sb.AppendLine($"  Database     {vm.DatabasePath}");
        if (vm.FailureMessages.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Failures:");
            foreach (var msg in vm.FailureMessages)
            {
                sb.AppendLine($"  - {msg}");
            }
        }

        return sb.ToString();
    }
}
```

- [ ] **Step 5: Create `IndexCommandHandler.cs`**

`src/Ferret.Cli/Commands/Indexing/IndexCommandHandler.cs`:

Read `ICommandHandler.cs` and one existing handler (e.g. `StatusCommandHandler.cs`) before writing to confirm:
- `ExecuteAsync(IFerretContext)` returns `Task<CommandResult>`
- output is via `context.Services.Output.WriteLine(...)`
- options via `context.GetOption<bool>("rebuild")`

```csharp
using System.Diagnostics;
using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Indexing.Formatting;
using Ferret.Cli.Commands.Indexing.ViewModels;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Indexing;

/// <summary>
/// Handles the <c>ferret index</c> command. Resolves workspace context, invokes
/// <see cref="IIndexPipeline"/>, patches wall-clock duration, formats, and outputs the summary.
/// </summary>
internal sealed class IndexCommandHandler : ICommandHandler
{
    private readonly IIndexPipeline _pipeline;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly TextIndexSummaryFormatter _formatter;

    /// <summary>Initializes a new <see cref="IndexCommandHandler"/>.</summary>
    /// <param name="pipeline">The index pipeline.</param>
    /// <param name="workspaceContext">Provides workspace ID and root path.</param>
    /// <param name="formatter">Formats the index summary for CLI output.</param>
    public IndexCommandHandler(
        IIndexPipeline pipeline,
        IWorkspaceContext workspaceContext,
        TextIndexSummaryFormatter formatter)
    {
        _pipeline = pipeline;
        _workspaceContext = workspaceContext;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var forceRebuild = context.GetOption<bool>("rebuild");
        var options = new IndexPipelineOptions { ForceRebuild = forceRebuild };

        if (forceRebuild)
        {
            context.Services.Output.WriteLine("Rebuilding index from scratch...");
        }
        else
        {
            context.Services.Output.WriteLine("Indexing workspace...");
        }

        var sw = Stopwatch.StartNew();
        var result = await _pipeline.RunAsync(
            _workspaceContext.WorkspaceId,
            options,
            context.CancellationToken).ConfigureAwait(false);
        sw.Stop();

        // Patch Duration: IndexPipeline uses internal DateTimeOffset diff; CLI owns wall-clock.
        result = result with { Duration = sw.Elapsed };

        var dbPath = Path.Combine(
            _workspaceContext.WorkspaceRoot.FullPath,
            WorkspaceLayout.RootDirectoryName,
            IndexLayout.IndexDirectoryName,
            IndexLayout.KeywordDirectoryName,
            IndexLayout.KeywordDatabaseFileName);

        var vm = IndexSummaryViewModel.From(result, dbPath);
        context.Services.Output.WriteLine(_formatter.Format(vm));

        return result.Failures > 0 ? CommandResult.Failure : CommandResult.Success;
    }
}
```

Note: Read `WorkspacePath.cs` to confirm `.FullPath` is the correct property. Read `WorkspaceLayout.cs` to confirm `RootDirectoryName` is correct. If `IndexResult` is not a `record` (check `IndexResult.cs`), replace `result with { Duration = sw.Elapsed }` with a copy-constructor approach.

- [ ] **Step 6: Confirm green**

```
dotnet test tests/Ferret.Cli.Tests --filter "IndexCommandHandlerTests"
dotnet build src/Ferret.sln
```

Expected: all tests pass, 0 build errors.

---

## Task 3: Composition Root + `IndexCliModule` + Module Registration

**Why:** `IndexCliModule` provides the DI registration for `IIndexEngine` and `IndexCommandHandler` together with the `CommandDefinition` for `ferret index`. `Program.cs` and `CoreCliModule` must be updated in tandem so the `index` empty-group stub is replaced by a real command. The registration order matters: `IIndexEngine` → `IndexingModule.ConfigureServices` → `IndexCliModule.ConfigureServices`.

**Files:**
- Create: `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs`
- Modify: `src/Ferret.Core/Indexing/IIndexPipeline.cs` (add `WorkspaceId` to `RunAsync`)
- Modify: `src/Ferret.Indexing/IndexPipeline.cs` (update signature)
- Modify: `src/Ferret.Cli/Commands/CoreCliModule.cs` (remove `index` empty-group stub)
- Modify: `src/Ferret.Cli/Program.cs` (build `IWorkspaceContext`, register modules)

**Interfaces:**
- Consumes: `IWorkspaceContext` (Task 1), `IndexCommandHandler` (Task 2), `SqliteKeywordIndexEngine` (S3), `IndexingModule` (S3), `ConnectorCliModule` (S4), `ParserPlatformModule` (S2), `JsonWorkspaceStore` (Ferret.Workspace), `WorkspaceManifest` (Ferret.Workspace)
- Produces: compiled and runnable `ferret index` command

- [ ] **Step 1: Write failing tests**

Add to `tests/Ferret.Cli.Tests/Commands/Indexing/IndexCommandHandlerTests.cs` (or a new file `IndexCliModuleTests.cs`):

```csharp
// tests/Ferret.Cli.Tests/Commands/Indexing/IndexCliModuleTests.cs
using Ferret.Cli.Commands.Indexing;
using Ferret.Core.Workspace;
using Ferret.Core.Indexing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ferret.Cli.Tests.Commands.Indexing;

public sealed class IndexCliModuleTests
{
    [Fact]
    public void GetCommands_Returns_One_Command_Named_Index()
    {
        var module = new IndexCliModule();

        var commands = module.GetCommands();

        Assert.Single(commands);
        Assert.Equal("index", commands[0].Metadata.Name);
    }

    [Fact]
    public void GetCommands_Index_Has_Rebuild_Option()
    {
        var module = new IndexCliModule();

        var cmd = module.GetCommands()[0];

        Assert.NotNull(cmd.Options);
        Assert.Contains(cmd.Options!, o => o.Name == "--rebuild");
    }

    [Fact]
    public void ConfigureServices_Registers_IIndexEngine()
    {
        var services = new ServiceCollection();
        var ctx = BuildFakeWorkspaceContext();

        new IndexCliModule().ConfigureServices(services, ctx);
        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IIndexEngine>());
    }

    [Fact]
    public void ConfigureServices_Registers_IndexCommandHandler()
    {
        var services = new ServiceCollection();
        var ctx = BuildFakeWorkspaceContext();

        // IndexCommandHandler depends on IIndexPipeline — register a fake
        services.AddSingleton<IIndexPipeline>(new FakeIndexPipeline());
        new IndexCliModule().ConfigureServices(services, ctx);
        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IndexCommandHandler>());
    }

    private static IWorkspaceContext BuildFakeWorkspaceContext()
    {
        // Use DefaultWorkspaceContext or a minimal anonymous implementation
        return new Ferret.Workspace.DefaultWorkspaceContext(
            Ferret.Core.Primitives.WorkspaceId.Create("test"),
            Ferret.Core.Primitives.WorkspacePath.Create(
                Path.GetTempPath()));
    }

    private sealed class FakeIndexPipeline : IIndexPipeline
    {
        public Task<IndexResult> RunAsync(
            Ferret.Core.Primitives.WorkspaceId workspaceId,
            IndexPipelineOptions options,
            CancellationToken ct = default)
        {
            return Task.FromResult(new IndexResult
            {
                AssetsDiscovered = 0,
                AssetsProcessed  = 0,
                DocumentsIndexed = 0,
                DocumentsSkipped = 0,
                Failures         = 0,
                Warnings         = 0,
                Duration         = TimeSpan.Zero,
                FailureMessages  = [],
            });
        }
    }
}
```

- [ ] **Step 2: Confirm red**

```
dotnet test tests/Ferret.Cli.Tests --filter "IndexCliModuleTests"
```

Expected: FAIL — `IndexCliModule` not found.

- [ ] **Step 3: Update `IIndexPipeline.RunAsync` to accept `WorkspaceId`**

Read `src/Ferret.Core/Indexing/IIndexPipeline.cs` first. Replace the `RunAsync` signature:

Before (from S3):
```csharp
Task<IndexResult> RunAsync(IndexPipelineOptions options, CancellationToken ct = default);
```

After:
```csharp
/// <summary>Runs the full ingestion pipeline for the specified workspace.
/// Discovery → Parse → Index. Returns a summary result.</summary>
/// <param name="workspaceId">The workspace being indexed. Passed to events for correlation.</param>
/// <param name="options">Pipeline run options (ForceRebuild, etc.).</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>A summary of the indexing run.</returns>
Task<IndexResult> RunAsync(
    WorkspaceId workspaceId,
    IndexPipelineOptions options,
    CancellationToken ct = default);
```

Add `using Ferret.Core.Primitives;` if not already present.

- [ ] **Step 4: Update `IndexPipeline.RunAsync` to accept `WorkspaceId`**

Read `src/Ferret.Indexing/IndexPipeline.cs`. Update the `RunAsync` and `RunCoreAsync` public method signatures to match. Pass `workspaceId.Value` to `IndexingStartedEvent` and `IndexingCompletedEvent` constructors (they currently receive `"workspace"` as the entity ID — replace with the actual value).

- [ ] **Step 5: Update `FakeIndexPipeline` in S3 tests**

Read `tests/Ferret.Indexing.Tests/IndexPipelineTests.cs`. Update all `FakeIndexPipeline` / `FakeConnectorRegistry` patterns where `RunAsync` is called to pass a `WorkspaceId`. Search for `pipeline.RunAsync(` calls and add `new WorkspaceId("test-ws")` as first arg. Ensure all tests remain green.

- [ ] **Step 6: Create `IndexCliModule.cs`**

`src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs`:

Read `src/Ferret.Cli/Commands/Workspace/WorkspaceCliModule.cs` first to match the module pattern (implements `CliModuleBase` or just has static helpers — confirm which pattern S4's `ConnectorCliModule` uses, then follow that).

```csharp
using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Indexing.Formatting;
using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Indexing;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Indexing;

/// <summary>
/// CLI module for the <c>ferret index</c> command.
/// Registers <see cref="IIndexEngine"/> (with workspace-resolved db path) and
/// <see cref="IndexCommandHandler"/>. Exposes the <c>index</c> command definition.
/// </summary>
public sealed class IndexCliModule
{
    /// <summary>Registers <see cref="IIndexEngine"/>, <see cref="TextIndexSummaryFormatter"/>,
    /// and <see cref="IndexCommandHandler"/> as singletons.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="workspaceContext">Provides workspace root for db path resolution.</param>
    public void ConfigureServices(IServiceCollection services, IWorkspaceContext workspaceContext)
    {
        var dbPath = Path.Combine(
            workspaceContext.WorkspaceRoot.FullPath,
            WorkspaceLayout.RootDirectoryName,
            IndexLayout.IndexDirectoryName,
            IndexLayout.KeywordDirectoryName,
            IndexLayout.KeywordDatabaseFileName);

        services.AddSingleton<IIndexEngine>(_ => new SqliteKeywordIndexEngine(dbPath));
        services.AddSingleton<TextIndexSummaryFormatter>();
        services.AddSingleton<IndexCommandHandler>();
    }

    /// <summary>Returns the command definitions contributed by this module.</summary>
    public IReadOnlyList<CommandDefinition> GetCommands() =>
    [
        new CommandDefinition(
            new CommandMetadata("index", "Index workspace content."),
            typeof(IndexCommandHandler),
            Options:
            [
                new OptionDefinition("--rebuild", "Rebuild the index from scratch (clear + re-index).", typeof(bool)),
            ]),
    ];
}
```

Note: Read `CommandDefinition.cs` and `OptionDefinition.cs` in `src/Ferret.Cli/Cli/` to confirm the exact constructor signatures. The spec uses `.WithOption<bool>(...)` fluent syntax — if that method does not exist on `CommandDefinition`, use the constructor-based form shown above. Read `WorkspaceCliModule.cs` to confirm whether `ICliModule` / `CliModuleBase` is used — if S4 uses a different pattern, align with it.

- [ ] **Step 7: Update `CoreCliModule.cs`**

Read `src/Ferret.Cli/Commands/CoreCliModule.cs`. Remove the `index` empty-group stub (the `CommandDefinition.EmptyGroup("index", ...)` yield return). The real `index` command is contributed by `IndexCliModule`.

- [ ] **Step 8: Update `Program.cs`**

Read `src/Ferret.Cli/Program.cs` and `src/Ferret.Workspace/Persistence/JsonWorkspaceStore.cs` (confirm `ReadManifestAsync` signature and return type — it returns `WorkspaceManifest?`).

Replace the current `Program.cs` with:

```csharp
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Indexing;
using Ferret.Cli.Commands.Workspace;
using Ferret.Core.Primitives;
using Ferret.Core.Workspace;
using Ferret.Workspace;
using Ferret.Workspace.Persistence;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// 1. Build IWorkspaceContext once from CWD + workspace.json
//    ReadManifestAsync returns WorkspaceManifest? — null when not in a workspace.
var cwd = Directory.GetCurrentDirectory();
var workspacePath = WorkspacePath.Create(cwd);
var manifest = await JsonWorkspaceStore.ReadManifestAsync(workspacePath, cts.Token)
    .ConfigureAwait(false);

// Fallback WorkspaceId when not in a workspace (commands that require a workspace
// will surface a user-friendly error via their own guard checks).
var workspaceId = manifest is not null
    ? WorkspaceId.Create(manifest.Id)
    : WorkspaceId.Create("no-workspace");

IWorkspaceContext workspaceContext = new DefaultWorkspaceContext(workspaceId, workspacePath);

// 2. Assemble modules — order matters:
//    CoreCliModule → WorkspaceCliModule → ConnectorCliModule → IndexCliModule
var indexCliModule = new IndexCliModule();

return await RootCommandFactory.Build(
    [
        new CoreCliModule(),
        new WorkspaceCliModule(),
        // Sprint 8 S4: new ConnectorCliModule(workspaceContext),
        indexCliModule,
    ],
    serviceSetup: services =>
    {
        services.AddSingleton<IWorkspaceContext>(workspaceContext);
        // Sprint 8 S4: new ConnectorCliModule(workspaceContext).ConfigureServices(services, workspaceContext);
        // Sprint 9 S2: new ParserPlatformModule().ConfigureServices(services);
        // Sprint 9 S3: IndexingModule.ConfigureServices(services); // registers IIndexPipeline
        indexCliModule.ConfigureServices(services, workspaceContext);
    })
    .InvokeAsync(args)
    .ConfigureAwait(false);
```

**Important:** Read `RootCommandFactory.cs` to understand its actual `Build` signature before writing. The current `Program.cs` uses `RootCommandFactory.Build([...]).InvokeAsync(args)` without a `serviceSetup` callback — check whether such a callback exists or if services are configured differently (via `ICliModule.ConfigureServices`). Align with the actual API. If `ConnectorCliModule` and `ParserPlatformModule` and `IndexingModule` registrations come through `ICliModule.ConfigureServices` rather than a callback, remove the inline comments and use the module pattern consistently.

- [ ] **Step 9: Verify full build and test pass**

```
dotnet build src/Ferret.sln
dotnet test src/Ferret.sln
```

Expected: 0 errors, 0 warnings, all tests pass.

---

## Task 4: End-to-End Test + Benchmark + Docs + Sprint Tag

**Why last:** Integration tests verify the real pipeline wiring (real SQLite, real filesystem, real parsers). The benchmark establishes a performance baseline. Docs and tag close the sprint. All Sprint 9 work must be green before these run.

**Files:**
- Create: `tests/Ferret.Indexing.Tests/EndToEnd/IndexPipelineEndToEndTests.cs`
- Create: `tests/Ferret.Indexing.Tests/Performance/IndexPipelineBenchmarkTests.cs`
- Modify: `docs/000-Overview/PROJECT-STATE.md`
- Modify: `docs/001-Product/ROADMAP-001.md`

**Interfaces:**
- Consumes: `SqliteKeywordIndexEngine`, `IndexPipeline`, `FilesystemConnector`, `MimeTypeResolver`, parsers (PlainText, Markdown, JSON), `WorkspaceId`, real temp filesystem
- Produces: confirmed end-to-end green, sprint tag `v0.9.0-sprint9`

- [ ] **Step 1: Create `TempDirectory` helper if not already in test project**

Check `tests/Ferret.Indexing.Tests/` for an existing `TempDirectory` helper. If absent, create one:

`tests/Ferret.Indexing.Tests/Helpers/TempDirectory.cs`:

```csharp
namespace Ferret.Indexing.Tests.Helpers;

/// <summary>Creates a temporary directory and deletes it on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    internal TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
```

Note: Check `tests/Ferret.Connectors.Filesystem.Tests/` — a `TempDirectory` may already exist there. If so, confirm it is not accessible from `Ferret.Indexing.Tests` (different project) and create a copy in this project.

- [ ] **Step 2: Create end-to-end test**

`tests/Ferret.Indexing.Tests/EndToEnd/IndexPipelineEndToEndTests.cs`:

Read `src/Ferret.Indexing/IndexPipeline.cs` (after S3) to confirm the constructor signature. Read `src/Ferret.Connectors.Filesystem/FilesystemConnector.cs` to confirm the constructor. Read `src/Ferret.ParserPlatform/` to confirm `MimeTypeResolver`, `PlainTextParser`, `MarkdownParser`, `JsonParser` class names. Read `src/Ferret.Core/Connectors/IConnectorRegistry.cs` (or the S3 fake) to confirm which type is used.

The test uses a `FakeConnectorManager` built from the real `FilesystemConnector` — read `ConnectorRuntime`, `ConnectorInstance`, and `ConnectorStatus` type names from S3/S4 outputs. Adjust type names to match actual S3/S4 API.

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Fakes;
using Ferret.Indexing.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Indexing.Tests.EndToEnd;

/// <summary>
/// End-to-end test: real filesystem, real parsers, real SQLite.
/// 6 files: 4 parseable (md, json, cs, txt), 2 binary (png, dll).
/// </summary>
public sealed class IndexPipelineEndToEndTests
{
    [Fact]
    public async Task IndexPipeline_Indexes_All_Parseable_Files_And_Skips_Binaries()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(tempDir.Path, "README.md"), "# Hello\nThis is Ferret.");
        await File.WriteAllTextAsync(
            Path.Combine(tempDir.Path, "config.json"),
            """{"name":"ferret","version":"9.0"}""");
        await File.WriteAllTextAsync(
            Path.Combine(tempDir.Path, "Program.cs"),
            "Console.WriteLine(\"Hello\");");
        await File.WriteAllTextAsync(
            Path.Combine(tempDir.Path, "notes.txt"), "plain text content");
        await File.WriteAllBytesAsync(
            Path.Combine(tempDir.Path, "image.png"), [0x89, 0x50, 0x4E, 0x47]);
        await File.WriteAllBytesAsync(
            Path.Combine(tempDir.Path, "library.dll"), [0x4D, 0x5A]);

        var dbPath = Path.Combine(tempDir.Path, "keyword-index.db");
        await using var engine = new SqliteKeywordIndexEngine(dbPath);

        // Read the actual class names and constructor signatures from S2 and S3 before filling these in:
        // var mimeResolver = new MimeTypeResolver();
        // var parserRegistry = ParserRegistryBuilder.Build([...parsers...]);
        // var dispatcher = new ParserDispatcher(parserRegistry, ...);
        // var connector = new FilesystemConnector(connectorConfig, mimeResolver);
        // var registry = new FakeConnectorRegistry([connector]);
        // var pipeline = new IndexPipeline(registry, dispatcher, engine, new FakeEventBus(), new CorrelationId("e2e"));

        // Placeholder — fill in exact types after reading S2/S3 source:
        var pipeline = BuildRealPipeline(tempDir.Path, engine);

        var result = await pipeline.RunAsync(
            WorkspaceId.Create("test-ws"),
            IndexPipelineOptions.Default,
            CancellationToken.None);

        Assert.Equal(6, result.AssetsDiscovered);
        Assert.Equal(4, result.DocumentsIndexed);    // README.md, config.json, Program.cs, notes.txt
        Assert.Equal(2, result.DocumentsSkipped);    // image.png, library.dll
        Assert.Equal(0, result.Failures);
        Assert.True(File.Exists(dbPath));

        // Verify SQLite directly
        await using var verifyEngine = new SqliteKeywordIndexEngine(dbPath);
        var stats = await verifyEngine.GetStatsAsync(CancellationToken.None);
        Assert.Equal(4, stats.DocumentCount);
    }

    /// <summary>Builds the real pipeline. Read S2/S3/S4 source files and fill in actual types.</summary>
    private static IndexPipeline BuildRealPipeline(string rootPath, SqliteKeywordIndexEngine engine)
    {
        // TODO: fill in after reading:
        //   src/Ferret.ParserPlatform/MimeTypeResolver.cs
        //   src/Ferret.ParserPlatform/Parsers/PlainTextParser.cs
        //   src/Ferret.ParserPlatform/Parsers/MarkdownParser.cs
        //   src/Ferret.ParserPlatform/Parsers/JsonParser.cs
        //   src/Ferret.ParserPlatform/ParserDispatcher.cs
        //   src/Ferret.Connectors.Filesystem/FilesystemConnector.cs
        //   tests/Ferret.Indexing.Tests/Fakes/FakeConnectorRegistry.cs (or equivalent)
        //   src/Ferret.Core/Primitives/CorrelationId.cs
        throw new NotImplementedException(
            "Fill in real types after reading S2/S3/S4 source. Remove this exception.");
    }
}
```

**Note:** The `BuildRealPipeline` placeholder must be completed after reading the S2/S3/S4 source files. Do not leave a `NotImplementedException` in committed code — complete the implementation before committing. The test asserts the exact counts shown above: 6 discovered, 4 indexed, 2 skipped, 0 failures.

- [ ] **Step 3: Create benchmark test**

`tests/Ferret.Indexing.Tests/Performance/IndexPipelineBenchmarkTests.cs`:

```csharp
using System.Diagnostics;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Helpers;
using Xunit;

namespace Ferret.Indexing.Tests.Performance;

/// <summary>Performance baseline: 1000 text files must index in under 30 seconds.
/// Typical on modern hardware: under 5 seconds.</summary>
public sealed class IndexPipelineBenchmarkTests
{
    [Fact]
    public async Task IndexPipeline_1000_TextFiles_Completes_Under_30_Seconds()
    {
        using var tempDir = new TempDirectory();
        for (int i = 0; i < 1000; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDir.Path, $"file{i:D4}.txt"),
                $"This is file number {i}. It contains some indexable content for benchmarking.");
        }

        var dbPath = Path.Combine(tempDir.Path, "bench.db");
        await using var engine = new SqliteKeywordIndexEngine(dbPath);
        var pipeline = BuildRealPipeline(tempDir.Path, engine);

        var sw = Stopwatch.StartNew();
        var result = await pipeline.RunAsync(
            WorkspaceId.Create("bench"),
            IndexPipelineOptions.Default,
            CancellationToken.None);
        sw.Stop();

        Assert.Equal(1000, result.DocumentsIndexed);
        Assert.True(
            sw.Elapsed.TotalSeconds < 30,
            $"Indexing 1000 files took {sw.Elapsed.TotalSeconds:F1}s, expected < 30s");

        // Record baseline (not a hard assertion — surfaces the number in test output):
        // Typical: < 5s on modern hardware
    }

    /// <summary>Builds the real pipeline. Copy the completed implementation from the end-to-end test.</summary>
    private static IndexPipeline BuildRealPipeline(string rootPath, SqliteKeywordIndexEngine engine)
    {
        // TODO: copy the completed BuildRealPipeline from IndexPipelineEndToEndTests after S2/S3/S4 are read.
        throw new NotImplementedException(
            "Fill in real types after reading S2/S3/S4 source. Remove this exception.");
    }
}
```

- [ ] **Step 4: Run full suite — confirm all green**

```
dotnet test src/Ferret.sln
```

Expected: all tests pass, including the two new end-to-end and benchmark tests. Confirm test count is 245 or greater.

- [ ] **Step 5: Update `PROJECT-STATE.md`**

Read `docs/000-Overview/PROJECT-STATE.md` first. Update to mark Sprint 9 complete and Sprint 10 as next:

- Sprint 9: Content Ingestion Pipeline — **complete**
  - Parser Platform (Markdown, JSON, plain-text)
  - SQLite FTS5 keyword index engine
  - `ferret index` command wired end-to-end
  - `IWorkspaceContext` — workspace identity propagated to all subsystems
- Sprint 10: Information Retrieval — **next**
  - `ferret search` — BM25 queries against FTS5
  - Phrase search, highlighting, ranking
  - `IProgressReporter` live search progress

- [ ] **Step 6: Update `ROADMAP-001.md`**

Read `docs/001-Product/ROADMAP-001.md` first. Mark Sprint 9 checked off. Rename Sprint 10 to:

> **Sprint 10 — Information Retrieval:** `ferret search`, BM25 phrase search, highlighting, ranking

- [ ] **Step 7: Commit and tag**

Verify `dotnet test src/Ferret.sln` is fully green, then:

```
git add src/Ferret.Core/Workspace/IWorkspaceContext.cs
git add src/Ferret.Core/Indexing/IndexLayout.cs
git add src/Ferret.Core/Indexing/IProgressReporter.cs
git add src/Ferret.Core/Indexing/IIndexPipeline.cs
git add src/Ferret.Workspace/DefaultWorkspaceContext.cs
git add src/Ferret.Indexing/IndexPipeline.cs
git add src/Ferret.Cli/Commands/Indexing/
git add src/Ferret.Cli/Commands/CoreCliModule.cs
git add src/Ferret.Cli/Program.cs
git add tests/Ferret.Core.Tests/Workspace/
git add tests/Ferret.Cli.Tests/Commands/Indexing/
git add tests/Ferret.Indexing.Tests/EndToEnd/
git add tests/Ferret.Indexing.Tests/Performance/
git add docs/000-Overview/PROJECT-STATE.md
git add docs/001-Product/ROADMAP-001.md
git add .claude/
```

```
git commit -m @'
feat(sprint-9): complete content ingestion pipeline — IWorkspaceContext, Parser Platform, Index Engine, Connector Config CLI, ferret index

- IWorkspaceContext + DefaultWorkspaceContext: workspace identity propagated to all subsystems
- IndexLayout: conventional paths for .ferret/indexes/keyword/keyword-index.db
- IIndexPipeline.RunAsync gains WorkspaceId parameter
- IndexSummaryViewModel + TextIndexSummaryFormatter: structured CLI output
- IndexCommandHandler: ferret index command — discover, parse, index, summarize
- IndexCliModule: registers IIndexEngine (resolved db path) + IndexCommandHandler
- Program.cs: builds IWorkspaceContext from CWD + workspace.json at startup
- End-to-end: 6 files, 4 indexed, 2 skipped, 0 failures, SQLite verified
- Benchmark: 1000 files < 30s
- 245+ tests green

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
'@
```

```
git tag v0.9.0-sprint9
```

---

## Section 5 Complete / Sprint 9 Complete

**Outputs of Section 5:**

- `IWorkspaceContext` (Ferret.Core.Workspace) — workspace identity and root path for all subsystems
- `DefaultWorkspaceContext` (Ferret.Workspace) — CLI composition root implementation
- `IndexLayout` (Ferret.Core.Indexing) — conventional path constants for `.ferret/indexes/keyword/`
- `IProgressReporter` (Ferret.Core.Indexing) — reserved stub for Sprint 10 live search progress
- `IIndexPipeline.RunAsync` updated to accept `WorkspaceId`
- `IndexSummaryViewModel` — view model decoupling formatter from domain types
- `TextIndexSummaryFormatter` — human-readable index run summary
- `IndexCommandHandler` — `ferret index` orchestrator; returns `CommandResult.Failure` when failures > 0
- `IndexCliModule` — DI module; registers `IIndexEngine` with workspace-resolved db path
- `Program.cs` updated — builds `IWorkspaceContext` once at startup; modules registered in correct order
- End-to-end test green: 6 files, 4 indexed (md/json/cs/txt), 2 skipped (png/dll), SQLite verified
- Benchmark test green: 1000 files in < 30 seconds

**Sprint 9 Success Criteria:**

```
ferret workspace init
ferret connector enable filesystem
ferret index
→ .ferret/indexes/keyword/keyword-index.db populated
→ 4 documents indexed (from test repo)
→ SELECT count(*) FROM documents == 4
→ Exit 0
→ 245+ tests green
git tag v0.9.0-sprint9
```

**What Sprint 10 (Information Retrieval) builds on:**

- `IIndexEngine` + `SqliteKeywordIndexEngine` — for `ferret search` BM25 queries via FTS5 `MATCH`
- `Document` + `PlainText` — for snippet generation and result highlighting
- `IndexLayout` — consistent db path resolution across `ferret index` and `ferret search`
- `IWorkspaceContext` — workspace identity for all future commands (`ferret search`, `ferret status`, etc.)
- `IProgressReporter` — live search progress surfacing to CLI spinner / log sink
