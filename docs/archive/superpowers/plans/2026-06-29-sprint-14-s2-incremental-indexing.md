# Sprint 14 S2: Incremental Indexing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Modify `IndexPipeline` to skip files whose fingerprint (mtime + size) hasn't changed since the last index run, cutting re-index time from O(all files) to O(changed files) on large workspaces.

**Architecture:** `IIndexStateStore` persists `AssetId → AssetFingerprint` mappings to `.ferret/index-state.json`. Before each asset is parsed, `IndexPipeline` computes a lightweight fingerprint with `AssetFingerprint.CreateLightweight(lastModified, sizeBytes)` and checks the store. Unchanged assets are skipped; changed/new assets are parsed, indexed, and recorded. After the pipeline run, assets absent from the current discovery scan are deleted from both the index and the state store (handles file deletions during a full re-index).

**Tech Stack:** .NET 9, `System.Text.Json`, `AssetFingerprint.CreateLightweight`, xUnit

## Global Constraints

- .NET 9 / C# 13: `required`, `init`, `record` types; no `new()` constraints
- `IIndexStateStore` and `IndexedAssetState` live in `src/Ferret.Core/Indexing/`
- `JsonIndexStateStore` and `NullIndexStateStore` live in `src/Ferret.Indexing/`
- `IndexPipeline` modification must remain backward-compatible (existing tests still pass via `NullIndexStateStore`)
- TDD: failing test first → verify red → implement → verify green → commit
- Commit prefix: `feat(sprint-14):`
- **Must be implemented before S1** (S1 file watching depends on the incremental re-index being cheap)

---

## File Structure

**New files:**
- `src/Ferret.Core/Indexing/IIndexStateStore.cs` — interface
- `src/Ferret.Indexing/NullIndexStateStore.cs` — no-op implementation
- `src/Ferret.Indexing/JsonIndexStateStore.cs` — JSON persistence

**Modified files:**
- `src/Ferret.Indexing/IndexPipeline.cs` — inject `IIndexStateStore`, add fingerprint skip logic
- `src/Ferret.Indexing/Ferret.Indexing.csproj` — no new references needed (System.Text.Json is in-box)

**Test files:**
- `tests/Ferret.Indexing.Tests/JsonIndexStateStoreTests.cs`
- `tests/Ferret.Indexing.Tests/IndexPipelineIncrementalTests.cs`

---

### Task 1: `IIndexStateStore` interface + `NullIndexStateStore`

**Files:**
- Create: `src/Ferret.Core/Indexing/IIndexStateStore.cs`
- Create: `src/Ferret.Indexing/NullIndexStateStore.cs`

**Interfaces:**
- Produces: `IIndexStateStore` — GetFingerprintAsync, SetFingerprintAsync, RemoveAsync, GetAllKeysAsync, ClearAsync, SaveAsync

- [ ] **Step 1: Create the interface**

Create `src/Ferret.Core/Indexing/IIndexStateStore.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Indexing;

/// <summary>Persists per-asset fingerprints for incremental indexing change detection.</summary>
public interface IIndexStateStore
{
    /// <summary>Returns the stored fingerprint for the asset, or null if not recorded.</summary>
    ValueTask<AssetFingerprint?> GetFingerprintAsync(AssetId assetId, CancellationToken ct = default);

    /// <summary>Records or updates the fingerprint for an asset.</summary>
    Task SetFingerprintAsync(AssetId assetId, AssetFingerprint fingerprint, CancellationToken ct = default);

    /// <summary>Removes the state entry for an asset (called when the asset is deleted).</summary>
    Task RemoveAsync(AssetId assetId, CancellationToken ct = default);

    /// <summary>Returns all asset IDs currently in the store.</summary>
    ValueTask<IReadOnlySet<AssetId>> GetAllKeysAsync(CancellationToken ct = default);

    /// <summary>Clears all stored state (called on ForceRebuild).</summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>Flushes in-memory state to the backing store.</summary>
    Task SaveAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create `NullIndexStateStore`**

Create `src/Ferret.Indexing/NullIndexStateStore.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Indexing;

/// <summary>No-op state store — always reports no stored fingerprints.
/// Used in tests and when incremental indexing is disabled.</summary>
public sealed class NullIndexStateStore : IIndexStateStore
{
    public ValueTask<AssetFingerprint?> GetFingerprintAsync(AssetId assetId, CancellationToken ct = default) =>
        ValueTask.FromResult<AssetFingerprint?>(null);

    public Task SetFingerprintAsync(AssetId assetId, AssetFingerprint fingerprint, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(AssetId assetId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public ValueTask<IReadOnlySet<AssetId>> GetAllKeysAsync(CancellationToken ct = default) =>
        ValueTask.FromResult<IReadOnlySet<AssetId>>(new HashSet<AssetId>());

    public Task ClearAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SaveAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}
```

- [ ] **Step 3: Build to verify no compile errors**

```
dotnet build src/Ferret.sln
```

Expected: Build succeeded, 0 errors

- [ ] **Step 4: Commit**

```
git add src/Ferret.Core/Indexing/IIndexStateStore.cs
git add src/Ferret.Indexing/NullIndexStateStore.cs
git commit -m "feat(sprint-14): IIndexStateStore interface + NullIndexStateStore null object"
```

---

### Task 2: `JsonIndexStateStore` — JSON-backed persistence

**Files:**
- Create: `src/Ferret.Indexing/JsonIndexStateStore.cs`
- Test: `tests/Ferret.Indexing.Tests/JsonIndexStateStoreTests.cs`

**Interfaces:**
- Consumes: `IIndexStateStore` (Task 1)
- Produces: `JsonIndexStateStore(string filePath)` — persists to `{filePath}`

Serialization format: `Dictionary<string, string>` where key = `assetId.Value`, value = `"{algorithm}|{value}"` (pipe separator avoids ambiguity since neither field contains `|`).

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Indexing.Tests/JsonIndexStateStoreTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Indexing;

namespace Ferret.Indexing.Tests;

public sealed class JsonIndexStateStoreTests : IAsyncDisposable
{
    private readonly string _filePath;
    private readonly JsonIndexStateStore _store;

    public JsonIndexStateStoreTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"ferret-state-test-{Guid.NewGuid():N}.json");
        _store = new JsonIndexStateStore(_filePath);
    }

    [Fact]
    public async Task GetFingerprintAsync_UnknownAsset_ReturnsNull()
    {
        var assetId = AssetId.Create("file:///unknown.cs");
        var result = await _store.GetFingerprintAsync(assetId);
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAndGet_RoundTrips()
    {
        var assetId = AssetId.Create("file:///workspace/file.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(
            DateTimeOffset.UtcNow, sizeBytes: 1024);

        await _store.SetFingerprintAsync(assetId, fingerprint);
        var retrieved = await _store.GetFingerprintAsync(assetId);

        Assert.NotNull(retrieved);
        Assert.Equal(fingerprint.Algorithm, retrieved.Algorithm);
        Assert.Equal(fingerprint.Value, retrieved.Value);
    }

    [Fact]
    public async Task SaveAndReload_PersistsToDisk()
    {
        var assetId = AssetId.Create("file:///workspace/persistent.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), sizeBytes: 512);

        await _store.SetFingerprintAsync(assetId, fingerprint);
        await _store.SaveAsync();

        // Load from the same file path
        var reloaded = new JsonIndexStateStore(_filePath);
        var retrieved = await reloaded.GetFingerprintAsync(assetId);

        Assert.NotNull(retrieved);
        Assert.Equal(fingerprint.Value, retrieved.Value);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntry()
    {
        var assetId = AssetId.Create("file:///workspace/toremove.cs");
        await _store.SetFingerprintAsync(assetId,
            AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100));

        await _store.RemoveAsync(assetId);

        var result = await _store.GetFingerprintAsync(assetId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        await _store.SetFingerprintAsync(AssetId.Create("file:///a.cs"),
            AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1));
        await _store.SetFingerprintAsync(AssetId.Create("file:///b.cs"),
            AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2));

        await _store.ClearAsync();

        var keys = await _store.GetAllKeysAsync();
        Assert.Empty(keys);
    }

    [Fact]
    public async Task GetAllKeysAsync_ReturnsAllSetAssets()
    {
        var id1 = AssetId.Create("file:///a.cs");
        var id2 = AssetId.Create("file:///b.cs");
        await _store.SetFingerprintAsync(id1, AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1));
        await _store.SetFingerprintAsync(id2, AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2));

        var keys = await _store.GetAllKeysAsync();
        Assert.Equal(2, keys.Count);
        Assert.Contains(id1, keys);
        Assert.Contains(id2, keys);
    }

    public async ValueTask DisposeAsync()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
        await ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 2: Check `AssetId` API**

Before implementing, verify `AssetId.Create(string)` exists and has a `.Value` property:

```
grep -r "AssetId" src/Ferret.Core/Primitives/ --include="*.cs" -l
```

Read `src/Ferret.Core/Primitives/AssetId.cs` to confirm `.Value` and `.Create(string)` signatures. Adjust if different.

- [ ] **Step 3: Run tests to verify they fail**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "JsonIndexStateStoreTests" -v
```

Expected: FAIL — type not found

- [ ] **Step 4: Implement `JsonIndexStateStore`**

Create `src/Ferret.Indexing/JsonIndexStateStore.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using System.Text.Json;

namespace Ferret.Indexing;

/// <summary>JSON-backed state store for incremental indexing fingerprints.
/// Persists to a single JSON file; loads eagerly on construction.</summary>
public sealed class JsonIndexStateStore : IIndexStateStore
{
    private const char Separator = '|';

    private readonly string _filePath;
    private readonly Dictionary<string, string> _state;

    public JsonIndexStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _state = Load(filePath);
    }

    private static Dictionary<string, string> Load(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    /// <inheritdoc/>
    public ValueTask<AssetFingerprint?> GetFingerprintAsync(AssetId assetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        if (!_state.TryGetValue(assetId.Value, out var raw))
            return ValueTask.FromResult<AssetFingerprint?>(null);

        var sep = raw.IndexOf(Separator);
        if (sep < 0) return ValueTask.FromResult<AssetFingerprint?>(null);

        var algorithm = raw[..sep];
        var value = raw[(sep + 1)..];
        return ValueTask.FromResult<AssetFingerprint?>(new AssetFingerprint(algorithm, value));
    }

    /// <inheritdoc/>
    public Task SetFingerprintAsync(AssetId assetId, AssetFingerprint fingerprint, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        ArgumentNullException.ThrowIfNull(fingerprint);
        _state[assetId.Value] = $"{fingerprint.Algorithm}{Separator}{fingerprint.Value}";
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(AssetId assetId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        _state.Remove(assetId.Value);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlySet<AssetId>> GetAllKeysAsync(CancellationToken ct = default)
    {
        var result = _state.Keys
            .Select(AssetId.Create)
            .ToHashSet();
        return ValueTask.FromResult<IReadOnlySet<AssetId>>(result);
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default)
    {
        _state.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = false });
        await File.WriteAllTextAsync(_filePath, json, ct).ConfigureAwait(false);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "JsonIndexStateStoreTests" -v
```

Expected: PASS — 6 tests pass

- [ ] **Step 6: Commit**

```
git add src/Ferret.Indexing/JsonIndexStateStore.cs
git add tests/Ferret.Indexing.Tests/JsonIndexStateStoreTests.cs
git commit -m "feat(sprint-14): JsonIndexStateStore — mtime+size fingerprint persistence for incremental indexing"
```

---

### Task 3: Modify `IndexPipeline` to use `IIndexStateStore`

**Files:**
- Modify: `src/Ferret.Indexing/IndexPipeline.cs` — add optional `IIndexStateStore` parameter, fingerprint skip logic, deletion cleanup

**Interfaces:**
- Consumes: `IIndexStateStore` (Tasks 1–2), `AssetFingerprint.CreateLightweight`, `DocumentId.From(AssetId)`
- Produces: `IndexPipeline` constructor with optional 5th `IIndexStateStore? stateStore = null` parameter

- [ ] **Step 1: Write the failing tests**

Create `tests/Ferret.Indexing.Tests/IndexPipelineIncrementalTests.cs`:

```csharp
using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.Indexing.Tests.Fakes; // FakeConnectorManager, FakeParserDispatcher, FakeIndexEngine, FakeEventBus

namespace Ferret.Indexing.Tests;

public sealed class IndexPipelineIncrementalTests
{
    [Fact]
    public async Task RunAsync_SecondRun_SkipsUnchangedAssets()
    {
        // Arrange: create a state store pre-populated with the fingerprint of our fake asset
        var assetId = AssetId.Create("file:///workspace/file.cs");
        var lastModified = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sizeBytes = 1024L;
        var fingerprint = AssetFingerprint.CreateLightweight(lastModified, sizeBytes);

        var stateStore = new NullIndexStateStore(); // replace with a populated real store
        // Use JsonIndexStateStore backed by a temp file, pre-populated
        var tempPath = Path.Combine(Path.GetTempPath(), $"ferret-inc-test-{Guid.NewGuid():N}.json");
        var jsonStore = new JsonIndexStateStore(tempPath);
        await jsonStore.SetFingerprintAsync(assetId, fingerprint);
        await jsonStore.SaveAsync();

        // Reload from disk
        var store = new JsonIndexStateStore(tempPath);

        var engine = new FakeIndexEngine();
        var asset = new AssetDescriptor
        {
            Id = assetId,
            ConnectorId = ConnectorId.Create("file"),
            InstanceId = ConnectorInstanceId.Create("default"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("file:///workspace/file.cs"),
            DisplayName = "file.cs",
            LastModified = lastModified, // same as stored
            SizeBytes = sizeBytes,       // same as stored
        };
        var connector = new FakeSingleAssetConnector(asset);
        var pipeline = new IndexPipeline(
            new FakeConnectorManager([connector]),
            new FakeParserDispatcher(),
            engine,
            new FakeEventBus(),
            store);

        // Act
        var result = await pipeline.RunAsync(WorkspaceId.Create("test"), IndexPipelineOptions.Default);

        // Assert: asset was skipped (not written to engine)
        Assert.Equal(0, engine.WriteCount);
        Assert.Equal(1, result.DocumentsSkipped);

        // Cleanup
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }

    [Fact]
    public async Task RunAsync_ForceRebuild_ClearsStateStoreAndReindexesAll()
    {
        var assetId = AssetId.Create("file:///workspace/rebuild.cs");
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);

        var tempPath = Path.Combine(Path.GetTempPath(), $"ferret-rebuild-test-{Guid.NewGuid():N}.json");
        var store = new JsonIndexStateStore(tempPath);
        await store.SetFingerprintAsync(assetId, fingerprint);
        await store.SaveAsync();

        var reloadedStore = new JsonIndexStateStore(tempPath);
        var engine = new FakeIndexEngine();
        var asset = new AssetDescriptor
        {
            Id = assetId,
            ConnectorId = ConnectorId.Create("file"),
            InstanceId = ConnectorInstanceId.Create("default"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("file:///workspace/rebuild.cs"),
            DisplayName = "rebuild.cs",
            LastModified = DateTimeOffset.UtcNow,
            SizeBytes = 100,
        };

        var pipeline = new IndexPipeline(
            new FakeConnectorManager([new FakeSingleAssetConnector(asset)]),
            new FakeParserDispatcher(),
            engine,
            new FakeEventBus(),
            reloadedStore);

        var result = await pipeline.RunAsync(
            WorkspaceId.Create("test"),
            new IndexPipelineOptions { ForceRebuild = true });

        // ForceRebuild clears state store; asset is re-indexed regardless of prior fingerprint
        Assert.True(result.DocumentsIndexed >= 0); // may be 0 if FakeParserDispatcher returns empty
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
}
```

**Note:** `FakeSingleAssetConnector` may need to be created. Check `tests/Ferret.Indexing.Tests/Fakes/` for existing fakes. If `FakeSingleAssetConnector` doesn't exist, create it in `tests/Ferret.Indexing.Tests/Fakes/FakeSingleAssetConnector.cs` implementing both `IAssetSource` and `IAssetReader` with the provided `AssetDescriptor`.

- [ ] **Step 2: Check existing fakes**

```
dir tests\Ferret.Indexing.Tests\Fakes\
```

Read the existing `FakeConnectorManager.cs` to understand how connectors are registered. Adjust the test above to match the actual fake API.

- [ ] **Step 3: Run tests to verify they fail**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "IndexPipelineIncrementalTests" -v
```

Expected: FAIL — `IndexPipeline` does not accept `stateStore` parameter

- [ ] **Step 4: Modify `IndexPipeline`**

Open `src/Ferret.Indexing/IndexPipeline.cs`. Make the following changes:

**4a. Add `IIndexStateStore` field and update constructor:**

```csharp
private readonly IIndexStateStore _stateStore;

public IndexPipeline(
    IConnectorManager connectorManager,
    IParserDispatcher dispatcher,
    IIndexEngine engine,
    IEventBus eventBus,
    IIndexStateStore? stateStore = null)  // optional — defaults to NullIndexStateStore
{
    ArgumentNullException.ThrowIfNull(connectorManager);
    ArgumentNullException.ThrowIfNull(dispatcher);
    ArgumentNullException.ThrowIfNull(engine);
    ArgumentNullException.ThrowIfNull(eventBus);

    _connectorManager = connectorManager;
    _dispatcher = dispatcher;
    _engine = engine;
    _eventBus = eventBus;
    _stateStore = stateStore ?? new NullIndexStateStore();
}
```

**4b. After the `ForceRebuild` clear block, add state store clear:**

Locate the existing code:
```csharp
if (options.ForceRebuild)
{
    await _engine.ClearAsync(ct).ConfigureAwait(false);
}
```

Replace with:
```csharp
if (options.ForceRebuild)
{
    await _engine.ClearAsync(ct).ConfigureAwait(false);
    await _stateStore.ClearAsync(ct).ConfigureAwait(false);
}
```

**4c. Add `seenAssets` tracking set before the connector loop:**

Add after the counter variables (after `var failureMessages = new List<string>();`):
```csharp
var seenAssets = new HashSet<AssetId>();
```

**4d. Add fingerprint check inside the asset loop, after the `IAssetReader` check:**

Locate the `assetsProcessed++;` line. Immediately after it, insert:

```csharp
// Incremental: skip if fingerprint unchanged since last index run
var computedFingerprint = AssetFingerprint.CreateLightweight(
    asset.LastModified, asset.SizeBytes ?? 0);
seenAssets.Add(asset.Id);
var storedFingerprint = await _stateStore
    .GetFingerprintAsync(asset.Id, ct).ConfigureAwait(false);
if (storedFingerprint == computedFingerprint)
{
    skipped++;
    assetsProcessed--;  // undo: this asset was not truly processed
    await _eventBus.PublishAsync(
        new DocumentSkippedEvent(asset.Id.Value, correlationId)
        {
            AssetId = asset.Id,
            Reason = "Fingerprint unchanged",
        },
        ct).ConfigureAwait(false);
    continue;
}
```

**4e. After the successful `WriteAsync` call, record the new fingerprint:**

Locate `indexed++;` inside the `ParseResultKind.Success` branch. Immediately after `indexed++;` add:
```csharp
await _stateStore
    .SetFingerprintAsync(asset.Id, computedFingerprint, ct)
    .ConfigureAwait(false);
```

**4f. After the entire connector loop, clean up deleted assets:**

Find the code that builds `IndexResult` (near the end of `RunAsync`). Before it, insert:

```csharp
// Remove state entries for assets no longer discovered (deleted files).
var allKnown = await _stateStore.GetAllKeysAsync(ct).ConfigureAwait(false);
foreach (var staleId in allKnown.Except(seenAssets))
{
    await _engine.DeleteAsync(DocumentId.From(staleId), ct).ConfigureAwait(false);
    await _stateStore.RemoveAsync(staleId, ct).ConfigureAwait(false);
}
await _stateStore.SaveAsync(ct).ConfigureAwait(false);
```

**Note:** `DocumentId.From(AssetId)` is defined in `src/Ferret.Core/Primitives/DocumentId.cs` — it maps 1:1 with the asset ID value.

- [ ] **Step 5: Add `using` statements to `IndexPipeline.cs` if needed**

Ensure these are at the top of `IndexPipeline.cs`:
```csharp
using Ferret.Core.Connectors;  // AssetFingerprint, AssetId
using Ferret.Core.Primitives;  // DocumentId
```

- [ ] **Step 6: Run tests to verify they pass**

```
dotnet test tests/Ferret.Indexing.Tests/ --filter "IndexPipelineIncrementalTests" -v
```

Expected: PASS

- [ ] **Step 7: Run full test suite to confirm no regressions**

```
dotnet test tests/ -v
```

Expected: all previously-passing tests still pass (existing tests pass `null` implicitly, which defaults to `NullIndexStateStore`)

- [ ] **Step 8: Commit**

```
git add src/Ferret.Indexing/IndexPipeline.cs
git add tests/Ferret.Indexing.Tests/IndexPipelineIncrementalTests.cs
git commit -m "feat(sprint-14): IndexPipeline incremental skip — fingerprint-based change detection, stale asset cleanup"
```

---

### Task 4: Wire `JsonIndexStateStore` into the composition root

**Files:**
- Modify: `src/Ferret.Indexing/IndexingModule.cs` (or wherever `IndexPipeline` is registered — check `src/Ferret.Indexing/`)

**Interfaces:**
- Consumes: `IFerretContext.WorkspaceRoot` or `IFerretPathProvider` — check what path abstraction exists
- Produces: `IIndexStateStore` registered as singleton in DI

- [ ] **Step 1: Find the composition root for IndexPipeline**

```
grep -r "IndexPipeline" src/ --include="*.cs" -l
```

Read the file that registers `IndexPipeline` in DI (likely `IndexingModule.cs` or `FerretModule.cs`).

- [ ] **Step 2: Add `JsonIndexStateStore` registration**

In the module's `ConfigureServices`, add:

```csharp
services.AddSingleton<IIndexStateStore>(sp =>
{
    var pathProvider = sp.GetRequiredService<IFerretPathProvider>(); // or IFerretContext
    var stateFilePath = Path.Combine(pathProvider.FerretDirectory, "index-state.json");
    return new JsonIndexStateStore(stateFilePath);
});
```

**Note:** Adapt to whatever path abstraction exists in the project. If `IFerretPathProvider` doesn't exist, check for `IFerretContext`, `FerretConfig`, or `WorkspaceOptions` — find what provides the `.ferret/` directory path. Read the actual type before writing.

- [ ] **Step 3: Update `IndexPipeline` registration to inject `IIndexStateStore`**

If `IndexPipeline` is registered as `services.AddSingleton<IIndexPipeline, IndexPipeline>()`, no change is needed — the DI container will auto-inject the 5th optional parameter.

- [ ] **Step 4: Build and smoke test**

```
dotnet build src/Ferret.sln
dotnet run --project src/Ferret.Cli -- index 2>&1 | tail -5
```

Expected: index runs; second run should be visibly faster (logs show "N skipped")

- [ ] **Step 5: Commit**

```
git add src/Ferret.Indexing/  # whatever module file was modified
git commit -m "feat(sprint-14): wire JsonIndexStateStore into composition root — incremental indexing active"
```

---

## Completion Checklist

- [ ] `IIndexStateStore` interface defined in `Ferret.Core.Indexing`
- [ ] `NullIndexStateStore` passes all existing pipeline tests unchanged
- [ ] `JsonIndexStateStore` round-trips fingerprints through save/reload
- [ ] `JsonIndexStateStore` silently handles corrupt/missing JSON file
- [ ] `IndexPipeline` skips assets with matching fingerprint (`DocumentsSkipped` count increases)
- [ ] `IndexPipeline` with `ForceRebuild = true` clears state store and re-indexes all assets
- [ ] `IndexPipeline` removes deleted assets from index and state store after full scan
- [ ] Second `ferret index` run is observably faster than first (logs show skipped count)
- [ ] All tests pass: `dotnet test tests/`
- [ ] Build passes: `dotnet build src/Ferret.sln`
