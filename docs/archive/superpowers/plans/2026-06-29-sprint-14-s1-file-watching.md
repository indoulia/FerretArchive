# Sprint 14 S1: File Watching Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ferret watch` command that monitors the workspace directory for file changes and automatically re-indexes modified or deleted files using `System.IO.FileSystemWatcher` with 500ms debounce.

**Architecture:** `FileChangeDebouncer` batches rapid filesystem events within a 500ms window, coalescing duplicates by path. On file change, `IIndexPipeline.RunAsync` re-indexes the workspace incrementally (S2's fingerprint skipping makes this cheap). On file deletion, `IIndexEngine.DeleteAsync` removes the document. `WatchCommandHandler` orchestrates the watcher and debouncer, running until cancellation.

**Tech Stack:** .NET 9, `System.IO.FileSystemWatcher`, `System.Threading.Timer`, `Microsoft.Extensions.Logging`, xUnit, NSubstitute

## Global Constraints

- .NET 9 / C# 13: `required`, `init`, `record` types; no `new()` constraints
- All new types: `src/Ferret.Cli/Commands/Watch/` namespace
- Tests: xUnit in `tests/Ferret.Cli.Tests/Commands/Watch/` and `tests/Ferret.Indexing.Tests/`
- TDD: failing test first → verify red → implement → verify green → commit
- Commit prefix: `feat(sprint-14):` for features, `test(sprint-14):` for test-only commits
- **Depends on S2:** Implement S1 after S2 so incremental re-index is cheap

---

## File Structure

**New files:**
- `src/Ferret.Core/Indexing/IIndexEngine.cs` — add `DeleteAsync` method
- `src/Ferret.Indexing/SqliteKeywordIndexEngine.cs` — implement `DeleteAsync`
- `src/Ferret.Cli/Commands/Watch/FileChangeDebouncer.cs` — debounce helper
- `src/Ferret.Cli/Commands/Watch/WatchCommandHandler.cs` — command logic
- `src/Ferret.Cli/Commands/Watch/WatchCliModule.cs` — CLI module registration

**Modified files:**
- `src/Ferret.Cli/Program.cs` — register `WatchCliModule`

**Test files:**
- `tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineDeleteTests.cs`
- `tests/Ferret.Cli.Tests/Commands/Watch/FileChangeDebouncerTests.cs`
- `tests/Ferret.Cli.Tests/Commands/Watch/WatchCommandHandlerTests.cs`

---

### Task 1: `IIndexEngine.DeleteAsync` + `SqliteKeywordIndexEngine` implementation

**Files:**
- Modify: `src/Ferret.Core/Indexing/IIndexEngine.cs`
- Modify: `src/Ferret.Indexing/SqliteKeywordIndexEngine.cs`
- Test: `tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineDeleteTests.cs`

**Interfaces:**
- Produces: `Task IIndexEngine.DeleteAsync(DocumentId documentId, CancellationToken ct = default)`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineDeleteTests.cs`:

```csharp
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;

namespace Ferret.Indexing.Tests;

public sealed class SqliteKeywordIndexEngineDeleteTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteKeywordIndexEngine _engine;

    public SqliteKeywordIndexEngineDeleteTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ferret-delete-test-{Guid.NewGuid():N}.db");
        _engine = new SqliteKeywordIndexEngine(_dbPath);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocumentFromIndex()
    {
        var docId = DocumentId.Create("file:///workspace/file1.cs");
        var doc = new Document
        {
            Id = docId,
            Title = "File1",
            PlainText = "public class File1 { }",
            Kind = DocumentKind.Code,
            MediaType = "text/plain"
        };
        await _engine.WriteAsync(doc);
        var statsBefore = await _engine.GetStatsAsync();
        Assert.Equal(1, statsBefore.DocumentCount);

        await _engine.DeleteAsync(docId);

        var statsAfter = await _engine.GetStatsAsync();
        Assert.Equal(0, statsAfter.DocumentCount);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentDocument_DoesNotThrow()
    {
        var docId = DocumentId.Create("file:///workspace/nonexistent.cs");
        await _engine.DeleteAsync(docId); // must not throw
    }

    [Fact]
    public async Task DeleteAsync_DocumentNotReturnedInSearchAfterDelete()
    {
        var docId = DocumentId.Create("file:///workspace/searchable.cs");
        var doc = new Document
        {
            Id = docId,
            Title = "Searchable",
            PlainText = "public class SearchableClass { }",
            Kind = DocumentKind.Code,
            MediaType = "text/plain"
        };
        await _engine.WriteAsync(doc);
        await _engine.DeleteAsync(docId);

        var results = await _engine.SearchAsync("SearchableClass", new SearchOptions { TopK = 10 });
        Assert.Empty(results);
    }

    public void Dispose()
    {
        _engine.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "SqliteKeywordIndexEngineDeleteTests" -v
```

Expected: FAIL — `IIndexEngine` has no `DeleteAsync` member

- [ ] **Step 3: Add `DeleteAsync` to `IIndexEngine`**

In `src/Ferret.Core/Indexing/IIndexEngine.cs`, add after `WriteAsync`:

```csharp
/// <summary>Removes a single document from the index. No-ops if the document does not exist.</summary>
Task DeleteAsync(DocumentId documentId, CancellationToken ct = default);
```

- [ ] **Step 4: Implement `DeleteAsync` in `SqliteKeywordIndexEngine`**

Add the SQL constant alongside the other constants near the top of `SqliteKeywordIndexEngine.cs`:

```csharp
private const string DeleteDocumentByIdSql = """
    DELETE FROM documents_fts WHERE rowid = (SELECT rowid FROM documents WHERE id = @id);
    DELETE FROM documents WHERE id = @id;
    """;
```

Add the method after `ClearAsync`:

```csharp
/// <inheritdoc/>
public async Task DeleteAsync(DocumentId documentId, CancellationToken ct = default)
{
    ArgumentNullException.ThrowIfNull(documentId);
    ct.ThrowIfCancellationRequested();

    await using var cmd = _connection.CreateCommand();
    cmd.CommandText = DeleteDocumentByIdSql;
    cmd.Parameters.AddWithValue("@id", documentId.Value);
    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
}
```

- [ ] **Step 5: Add `DeleteAsync` stub to any `IIndexEngine` test fakes**

Search for classes implementing `IIndexEngine` in tests: `grep -r "IIndexEngine" tests/ --include="*.cs" -l`

For each fake (e.g., `FakeIndexEngine`), add:
```csharp
public Task DeleteAsync(DocumentId documentId, CancellationToken ct = default)
{
    return Task.CompletedTask;
}
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "SqliteKeywordIndexEngineDeleteTests" -v
```

Expected: PASS — 3 tests pass

- [ ] **Step 7: Run full test suite to confirm no regressions**

```
dotnet test tests/ -v
```

Expected: all tests pass

- [ ] **Step 8: Commit**

```
git add src/Ferret.Core/Indexing/IIndexEngine.cs
git add src/Ferret.Indexing/SqliteKeywordIndexEngine.cs
git add tests/Ferret.Indexing.Tests/SqliteKeywordIndexEngineDeleteTests.cs
git commit -m "feat(sprint-14): IIndexEngine.DeleteAsync — single-document removal from SQLite FTS index"
```

---

### Task 2: `FileChangeDebouncer` — debounced file event batching

**Files:**
- Create: `src/Ferret.Cli/Commands/Watch/FileChangeDebouncer.cs`
- Test: `tests/Ferret.Cli.Tests/Commands/Watch/FileChangeDebouncerTests.cs`

**Interfaces:**
- Produces: `FileChangeDebouncer(TimeSpan debounceWindow)`, `void Track(string path, WatcherChangeTypes changeType)`, `event EventHandler<DebouncedChangesEventArgs> ChangesReady`

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Cli.Tests/Commands/Watch/FileChangeDebouncerTests.cs`:

```csharp
using Ferret.Cli.Commands.Watch;
using System.IO;

namespace Ferret.Cli.Tests.Commands.Watch;

public sealed class FileChangeDebouncerTests : IDisposable
{
    private readonly FileChangeDebouncer _debouncer;

    public FileChangeDebouncerTests()
    {
        // 50ms window for fast tests
        _debouncer = new FileChangeDebouncer(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task Track_SingleChange_FiresAfterDebounceWindow()
    {
        var fired = new TaskCompletionSource<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => fired.TrySetResult(e);

        _debouncer.Track("/workspace/file.cs", WatcherChangeTypes.Changed);

        var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Single(result.Changes);
        Assert.Equal("/workspace/file.cs", result.Changes[0].Path);
    }

    [Fact]
    public async Task Track_RapidChangesToSamePath_CoalescesIntoOne()
    {
        var batches = new List<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => batches.Add(e);

        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed);
        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed); // duplicate
        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed); // duplicate

        await Task.Delay(200);

        Assert.Single(batches);
        Assert.Single(batches[0].Changes); // a.cs deduplicated
    }

    [Fact]
    public async Task Track_MultipleDistinctPaths_AllIncluded()
    {
        var fired = new TaskCompletionSource<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => fired.TrySetResult(e);

        _debouncer.Track("/workspace/a.cs", WatcherChangeTypes.Changed);
        _debouncer.Track("/workspace/b.cs", WatcherChangeTypes.Created);

        var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Equal(2, result.Changes.Count);
    }

    [Fact]
    public async Task Track_DeleteOverridesChange_KeepsDeleteType()
    {
        var fired = new TaskCompletionSource<DebouncedChangesEventArgs>();
        _debouncer.ChangesReady += (_, e) => fired.TrySetResult(e);

        _debouncer.Track("/workspace/file.cs", WatcherChangeTypes.Changed);
        _debouncer.Track("/workspace/file.cs", WatcherChangeTypes.Deleted); // last event wins

        var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Single(result.Changes);
        Assert.Equal(WatcherChangeTypes.Deleted, result.Changes[0].ChangeType);
    }

    public void Dispose() => _debouncer.Dispose();
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FileChangeDebouncerTests" -v
```

Expected: FAIL — type not found

- [ ] **Step 3: Implement `FileChangeDebouncer`**

Create `src/Ferret.Cli/Commands/Watch/FileChangeDebouncer.cs`:

```csharp
using System.IO;

namespace Ferret.Cli.Commands.Watch;

public sealed class DebouncedChangesEventArgs : EventArgs
{
    public required IReadOnlyList<(string Path, WatcherChangeTypes ChangeType)> Changes { get; init; }
}

public sealed class FileChangeDebouncer : IDisposable
{
    private readonly TimeSpan _window;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, WatcherChangeTypes> _pending =
        new(StringComparer.OrdinalIgnoreCase);
    private Timer? _timer;
    private bool _disposed;

    public event EventHandler<DebouncedChangesEventArgs>? ChangesReady;

    public FileChangeDebouncer(TimeSpan debounceWindow)
    {
        _window = debounceWindow;
    }

    public void Track(string path, WatcherChangeTypes changeType)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _pending[path] = changeType; // last event wins for same path
            _timer?.Dispose();
            _timer = new Timer(_ => Flush(), null, _window, Timeout.InfiniteTimeSpan);
        }
    }

    private void Flush()
    {
        List<(string, WatcherChangeTypes)> snapshot;
        lock (_lock)
        {
            if (_pending.Count == 0) return;
            snapshot = _pending.Select(kv => (kv.Key, kv.Value)).ToList();
            _pending.Clear();
        }
        ChangesReady?.Invoke(this, new DebouncedChangesEventArgs { Changes = snapshot });
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "FileChangeDebouncerTests" -v
```

Expected: PASS — 4 tests pass

- [ ] **Step 5: Commit**

```
git add src/Ferret.Cli/Commands/Watch/FileChangeDebouncer.cs
git add tests/Ferret.Cli.Tests/Commands/Watch/FileChangeDebouncerTests.cs
git commit -m "feat(sprint-14): FileChangeDebouncer — 500ms debounce with last-event-wins coalescing"
```

---

### Task 3: `WatchCliModule` + `WatchCommandHandler` — `ferret watch` command

**Files:**
- Create: `src/Ferret.Cli/Commands/Watch/WatchCommandHandler.cs`
- Create: `src/Ferret.Cli/Commands/Watch/WatchCliModule.cs`
- Modify: `src/Ferret.Cli/Program.cs` — register `WatchCliModule`
- Test: `tests/Ferret.Cli.Tests/Commands/Watch/WatchCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IIndexPipeline.RunAsync` (returns `IndexResult` with `DocumentsIndexed`), `IIndexEngine.DeleteAsync` (Task 1), `FileChangeDebouncer` (Task 2)
- Produces: `ferret watch` CLI command

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Cli.Tests/Commands/Watch/WatchCommandHandlerTests.cs`:

```csharp
using Ferret.Cli.Commands.Watch;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Ferret.Cli.Tests.Commands.Watch;

public sealed class WatchCommandHandlerTests
{
    private readonly IIndexPipeline _pipeline = Substitute.For<IIndexPipeline>();
    private readonly IIndexEngine _engine = Substitute.For<IIndexEngine>();

    [Fact]
    public void WatchCommandHandler_CanBeInstantiated()
    {
        var handler = new WatchCommandHandler(_pipeline, _engine, NullLogger<WatchCommandHandler>.Instance);
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task HandleAsync_CancelsCleanly_ReturnsZero()
    {
        var handler = new WatchCommandHandler(_pipeline, _engine, NullLogger<WatchCommandHandler>.Instance);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var context = Substitute.For<IFerretContext>();
            context.WorkspaceRoot.Returns(tmpDir);
            context.WorkspaceId.Returns(WorkspaceId.Create("test"));

            var exitCode = await handler.HandleAsync(context, cts.Token);
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "WatchCommandHandlerTests" -v
```

Expected: FAIL — type not found

- [ ] **Step 3: Implement `WatchCommandHandler`**

Create `src/Ferret.Cli/Commands/Watch/WatchCommandHandler.cs`:

```csharp
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Ferret.Cli.Commands.Watch;

public sealed class WatchCommandHandler
{
    private readonly IIndexPipeline _pipeline;
    private readonly IIndexEngine _engine;
    private readonly ILogger<WatchCommandHandler> _logger;

    public WatchCommandHandler(
        IIndexPipeline pipeline,
        IIndexEngine engine,
        ILogger<WatchCommandHandler> logger)
    {
        _pipeline = pipeline;
        _engine = engine;
        _logger = logger;
    }

    public async Task<int> HandleAsync(IFerretContext context, CancellationToken ct = default)
    {
        var workspaceRoot = context.WorkspaceRoot;
        if (!Directory.Exists(workspaceRoot))
        {
            _logger.LogError("Workspace root does not exist: {Path}", workspaceRoot);
            return 1;
        }

        using var debouncer = new FileChangeDebouncer(TimeSpan.FromMilliseconds(500));
        debouncer.ChangesReady += (_, e) =>
        {
            // Fire-and-forget: process changes without blocking the event callback.
            // Cancellation is checked inside ProcessChangesAsync.
            _ = ProcessChangesAsync(context, e.Changes, ct);
        };

        using var watcher = new FileSystemWatcher(workspaceRoot)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
        };

        watcher.Changed += (_, e) => debouncer.Track(e.FullPath, WatcherChangeTypes.Changed);
        watcher.Created += (_, e) => debouncer.Track(e.FullPath, WatcherChangeTypes.Created);
        watcher.Deleted += (_, e) => debouncer.Track(e.FullPath, WatcherChangeTypes.Deleted);
        watcher.Renamed += (_, e) =>
        {
            debouncer.Track(e.OldFullPath, WatcherChangeTypes.Deleted);
            debouncer.Track(e.FullPath, WatcherChangeTypes.Created);
        };

        _logger.LogInformation("Watching {Path} for changes. Press Ctrl+C to stop.", workspaceRoot);
        Console.WriteLine($"Watching {workspaceRoot} for changes. Press Ctrl+C to stop.");

        try
        {
            await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown on Ctrl+C
        }

        return 0;
    }

    private async Task ProcessChangesAsync(
        IFerretContext context,
        IReadOnlyList<(string Path, WatcherChangeTypes ChangeType)> changes,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        var deletions = changes.Where(c => c.ChangeType == WatcherChangeTypes.Deleted).ToList();
        var modifications = changes.Where(c => c.ChangeType != WatcherChangeTypes.Deleted).ToList();

        foreach (var (path, _) in deletions)
        {
            // DocumentId.From(AssetId) maps 1:1 with the asset URI used by connectors.
            // For file deletions the URI matches the connector's CanonicalUri format.
            var docId = DocumentId.Create(new Uri(path).ToString());
            _logger.LogDebug("Removing deleted document: {Path}", path);
            await _engine.DeleteAsync(docId, ct).ConfigureAwait(false);
        }

        if (modifications.Count > 0)
        {
            _logger.LogInformation("Re-indexing {Count} changed file(s)...", modifications.Count);
            var options = new IndexPipelineOptions { ForceRebuild = false };
            var result = await _pipeline.RunAsync(context.WorkspaceId, options, ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Re-index complete: {Indexed} indexed, {Skipped} skipped.",
                result.DocumentsIndexed,
                result.DocumentsSkipped);
        }
    }
}
```

- [ ] **Step 4: Create `WatchCliModule`**

Look at `src/Ferret.Cli/Commands/Indexing/IndexCliModule.cs` for the exact `CliModuleBase` pattern used in this project, then create `src/Ferret.Cli/Commands/Watch/WatchCliModule.cs` following the same pattern:

```csharp
using Ferret.Core.Indexing;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Ferret.Cli.Commands.Watch;

public sealed class WatchCliModule : CliModuleBase
{
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        var watchCmd = new Command("watch", "Watch the workspace for file changes and automatically re-index");

        watchCmd.SetHandler(async (ctx) =>
        {
            var sp = ctx.BindingContext.GetRequiredService<IServiceProvider>();
            var handler = sp.GetRequiredService<WatchCommandHandler>();
            var ferretCtx = sp.GetRequiredService<IFerretContext>();
            ctx.ExitCode = await handler.HandleAsync(ferretCtx, ctx.GetCancellationToken())
                .ConfigureAwait(false);
        });

        yield return new CommandDefinition(watchCmd);
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WatchCommandHandler>();
    }
}
```

**Note:** Match the exact `SetHandler` / `BindingContext` / `IFerretContext` resolution pattern used in `IndexCliModule.cs` — do not guess. Read that file first.

- [ ] **Step 5: Register `WatchCliModule` in `Program.cs`**

Open `src/Ferret.Cli/Program.cs`. Find the block that registers other `ICliModule` singletons. Add:

```csharp
services.AddSingleton<ICliModule, WatchCliModule>();
```

Add at the top of the file if missing:
```csharp
using Ferret.Cli.Commands.Watch;
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Cli.Tests/ --filter "WatchCommandHandlerTests" -v
```

Expected: PASS — 2 tests pass

- [ ] **Step 7: Build and smoke test**

```
dotnet build src/Ferret.sln
```

Expected: Build succeeded, 0 errors

Run with a timeout to confirm no startup errors:
```
dotnet run --project src/Ferret.Cli -- watch 2>&1 | head -3
```

Expected: "Watching … for changes. Press Ctrl+C to stop."

- [ ] **Step 8: Commit**

```
git add src/Ferret.Cli/Commands/Watch/
git add src/Ferret.Cli/Program.cs
git add tests/Ferret.Cli.Tests/Commands/Watch/WatchCommandHandlerTests.cs
git commit -m "feat(sprint-14): ferret watch — FileSystemWatcher with 500ms debounce, auto re-index on change"
```

---

## Completion Checklist

- [ ] `IIndexEngine.DeleteAsync` added to interface and implemented in `SqliteKeywordIndexEngine`
- [ ] `DeleteAsync` with non-existent document ID does not throw
- [ ] Deleted document is not returned in subsequent search results
- [ ] `FileChangeDebouncer` coalesces rapid events with last-event-wins for same path
- [ ] `FileChangeDebouncer` fires after debounce window with all distinct changes
- [ ] `ferret watch` starts and prints watch message
- [ ] File changes trigger incremental re-index via `IIndexPipeline.RunAsync`
- [ ] File deletions call `IIndexEngine.DeleteAsync`
- [ ] `ferret watch` exits cleanly on Ctrl+C (exit code 0)
- [ ] All tests pass: `dotnet test tests/`
- [ ] Build passes: `dotnet build src/Ferret.sln`
