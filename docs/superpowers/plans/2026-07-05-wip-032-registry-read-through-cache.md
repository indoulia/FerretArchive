# WIP-032 Registry Read-Through Cache Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an in-process, read-through cache in front of `IWorkspaceRegistry.ResolveAsync` so a federated query no longer re-opens and re-parses the same `workspace.json` files on every call within one long-lived process.

**Architecture:** A single new decorator, `CachingWorkspaceRegistry`, implements `IWorkspaceRegistry` and wraps the existing `FileWorkspaceRegistry`. It caches `ResolveAsync` results keyed by `WorkspaceId` in a `ConcurrentDictionary`, and keeps the cache correct by writing through on `SaveAsync` — the registry's one and only mutation path, which every `workspaces` CLI command (`add-repo`, `add-reference`, `remove-reference`, `pin-reference`, etc.) already funnels through for the workspace it modifies. `ListAsync` passes straight through, uncached — it isn't on the federated query hot path. No other type changes. This is exactly the scope validated by `docs/roadmap/Workspace-Intelligence/20-Phase-3-Priority-Assessment.md` §1/§5: cache registry-entry reads, not a graph walk (none exists to cache).

**Tech Stack:** C# / .NET, `System.Collections.Concurrent.ConcurrentDictionary`, xUnit.

## Global Constraints

- Do NOT change ADR-0026, ADR-0027, federation, references, pinning, or the registry persistence format.
- Do NOT introduce a distributed cache, persisted cache, background refresh, or speculative invalidation.
- In-process cache only; must not survive a process restart.
- Must be completely transparent to callers, preserve `IWorkspaceRegistry`/`FederatedKnowledgeStore` interfaces unchanged.
- A corrupt registry entry must keep throwing `WorkspaceRegistryCorruptException` on every call — never cached.
- A missing workspace must keep resolving to `null` correctly, including after it's later created.
- No unrelated cleanup, no refactor of `FileWorkspaceRegistry` or `FederatedKnowledgeStore` beyond DI wiring.

---

### Task 1: `CachingWorkspaceRegistry` decorator + tests

**Files:**
- Create: `src/Ferret.Workspace.Graph/CachingWorkspaceRegistry.cs`
- Create: `tests/Ferret.Workspace.Graph.Tests/CachingWorkspaceRegistryTests.cs`

**Interfaces:**
- Consumes: `IWorkspaceRegistry` (`src/Ferret.Workspace.Graph/IWorkspaceRegistry.cs`) — `Task<WorkspaceRegistryEntry?> ResolveAsync(Guid, CancellationToken)`, `Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken)`, `Task SaveAsync(WorkspaceRegistryEntry, CancellationToken)`. `WorkspaceRegistryEntry` (`src/Ferret.Workspace.Graph/WorkspaceRegistryEntry.cs`) has `required Guid WorkspaceId`. `WorkspaceRegistryCorruptException` (`src/Ferret.Workspace.Graph/WorkspaceRegistryCorruptException.cs`).
- Produces: `public sealed class CachingWorkspaceRegistry : IWorkspaceRegistry` with a public constructor `CachingWorkspaceRegistry(IWorkspaceRegistry inner)` — this is what Task 2 registers in DI.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/Ferret.Workspace.Graph.Tests/CachingWorkspaceRegistryTests.cs
namespace Ferret.Workspace.Graph.Tests;

public sealed class CachingWorkspaceRegistryTests : IDisposable
{
    private readonly string _rootDirectory;

    public CachingWorkspaceRegistryTests()
    {
        _rootDirectory = Path.Join(Path.GetTempPath(), $"ferret-caching-registry-test-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task ResolveAsync_CalledTwiceForSameId_OnlyReadsInnerRegistryOnce()
    {
        var workspaceId = Guid.NewGuid();
        var file = new FileWorkspaceRegistry(_rootDirectory);
        await file.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var counting = new CountingWorkspaceRegistry(file);
        var cache = new CachingWorkspaceRegistry(counting);

        await cache.ResolveAsync(workspaceId);
        await cache.ResolveAsync(workspaceId);

        Assert.Equal(1, counting.ResolveCount);
    }

    [Fact]
    public async Task ResolveAsync_AfterSaveAsyncOnTheCache_ReturnsUpdatedEntryWithoutReadingInnerRegistryAgain()
    {
        var workspaceId = Guid.NewGuid();
        var file = new FileWorkspaceRegistry(_rootDirectory);
        var counting = new CountingWorkspaceRegistry(file);
        var cache = new CachingWorkspaceRegistry(counting);
        await cache.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "old-name" });
        await cache.ResolveAsync(workspaceId);

        await cache.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "new-name" });
        var result = await cache.ResolveAsync(workspaceId);

        Assert.Equal("new-name", result?.Name);
        Assert.Equal(1, counting.ResolveCount);
    }

    [Fact]
    public async Task ResolveAsync_AfterAddReferenceViaSaveAsync_ReturnsEntryWithTheNewReference()
    {
        var workspaceId = Guid.NewGuid();
        var referencedId = Guid.NewGuid();
        var cache = new CachingWorkspaceRegistry(new FileWorkspaceRegistry(_rootDirectory));
        await cache.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "service-a" });
        var current = await cache.ResolveAsync(workspaceId);
        Assert.Empty(current!.References);

        await cache.SaveAsync(current with
        {
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = referencedId }],
        });
        var updated = await cache.ResolveAsync(workspaceId);

        Assert.Single(updated!.References);
        Assert.Equal(referencedId, updated.References[0].WorkspaceId);
    }

    [Fact]
    public async Task ResolveAsync_AfterRemoveReferenceViaSaveAsync_ReturnsEntryWithoutTheRemovedReference()
    {
        var workspaceId = Guid.NewGuid();
        var referencedId = Guid.NewGuid();
        var cache = new CachingWorkspaceRegistry(new FileWorkspaceRegistry(_rootDirectory));
        await cache.SaveAsync(new WorkspaceRegistryEntry
        {
            WorkspaceId = workspaceId,
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = referencedId }],
        });
        var current = await cache.ResolveAsync(workspaceId);
        Assert.Single(current!.References);

        await cache.SaveAsync(current with { References = [] });
        var updated = await cache.ResolveAsync(workspaceId);

        Assert.Empty(updated!.References);
    }

    [Fact]
    public async Task ResolveAsync_WhenInnerRegistryThrowsCorruptException_PropagatesEveryTimeAndNeverCaches()
    {
        var workspaceId = Guid.NewGuid();
        var file = new FileWorkspaceRegistry(_rootDirectory);
        await file.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(manifestPath, "{ this is not valid json");
        var counting = new CountingWorkspaceRegistry(file);
        var cache = new CachingWorkspaceRegistry(counting);

        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => cache.ResolveAsync(workspaceId));
        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => cache.ResolveAsync(workspaceId));

        Assert.Equal(2, counting.ResolveCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenWorkspaceDoesNotExist_ReturnsNullEachTimeAndCachesTheMiss()
    {
        var workspaceId = Guid.NewGuid();
        var counting = new CountingWorkspaceRegistry(new FileWorkspaceRegistry(_rootDirectory));
        var cache = new CachingWorkspaceRegistry(counting);

        var first = await cache.ResolveAsync(workspaceId);
        var second = await cache.ResolveAsync(workspaceId);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, counting.ResolveCount);
    }

    [Fact]
    public async Task ResolveAsync_ViaAFreshInstance_DoesNotReuseAPreviousInstancesCache()
    {
        var workspaceId = Guid.NewGuid();
        var firstCounting = new CountingWorkspaceRegistry(new FileWorkspaceRegistry(_rootDirectory));
        var firstProcess = new CachingWorkspaceRegistry(firstCounting);
        await firstProcess.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        await firstProcess.ResolveAsync(workspaceId);

        var secondCounting = new CountingWorkspaceRegistry(new FileWorkspaceRegistry(_rootDirectory));
        var secondProcess = new CachingWorkspaceRegistry(secondCounting);
        var result = await secondProcess.ResolveAsync(workspaceId);

        Assert.Equal("customer-platform", result?.Name);
        Assert.Equal(1, secondCounting.ResolveCount);
    }

    [Fact]
    public async Task ListAsync_PassesThroughToInnerRegistryUncached()
    {
        var cache = new CachingWorkspaceRegistry(new FileWorkspaceRegistry(_rootDirectory));
        await cache.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "workspace-a" });

        var result = await cache.ListAsync();

        Assert.Single(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private sealed class CountingWorkspaceRegistry : IWorkspaceRegistry
    {
        private readonly IWorkspaceRegistry _inner;

        public CountingWorkspaceRegistry(IWorkspaceRegistry inner) => _inner = inner;

        public int ResolveCount { get; private set; }

        public async Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default)
        {
            ResolveCount++;
            return await _inner.ResolveAsync(workspaceId, ct).ConfigureAwait(false);
        }

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            _inner.ListAsync(ct);

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            _inner.SaveAsync(entry, ct);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Ferret.Workspace.Graph.Tests/Ferret.Workspace.Graph.Tests.csproj --filter CachingWorkspaceRegistryTests`
Expected: FAIL to compile — `CachingWorkspaceRegistry` does not exist yet.

- [ ] **Step 3: Write the implementation**

```csharp
// src/Ferret.Workspace.Graph/CachingWorkspaceRegistry.cs
using System.Collections.Concurrent;

namespace Ferret.Workspace.Graph;

/// <summary>
/// In-process, read-through cache over another <see cref="IWorkspaceRegistry"/> (WIP-032). The
/// federated query path (<see cref="Ferret.Knowledge.Federation.FederatedKnowledgeStore"/>'s source
/// resolution) calls <see cref="ResolveAsync"/> once per member repo and once per direct reference
/// on every query — a file-open + JSON-parse each, even when nothing has changed since the last
/// query in this process (<c>20-Phase-3-Priority-Assessment.md</c> §1/§2). This decorator caches the
/// resolved entry (or its absence) per <see cref="Guid"/> and keeps it correct by writing through on
/// <see cref="SaveAsync"/> — the registry's only mutation path, which every <c>workspaces</c> CLI
/// command (<c>add-repo</c>, <c>add-reference</c>, <c>pin-reference</c>, etc.) already funnels
/// through for the one workspace entry it modifies.
/// </summary>
/// <remarks>
/// Never a source of truth: an exception from the wrapped registry (e.g.
/// <see cref="WorkspaceRegistryCorruptException"/>) is never cached — it propagates on every call,
/// so a corrupt manifest keeps failing exactly as it did before this cache existed.
/// <see cref="ListAsync"/> passes straight through, uncached; it is not on the federated query hot
/// path and its own directory scan already dominates its cost. In-memory only — a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> instance field, lost on process exit, never
/// persisted or shared across processes.
/// </remarks>
public sealed class CachingWorkspaceRegistry : IWorkspaceRegistry
{
    private readonly IWorkspaceRegistry _inner;
    private readonly ConcurrentDictionary<Guid, WorkspaceRegistryEntry?> _cache = new();

    /// <summary>Initializes a new instance of the <see cref="CachingWorkspaceRegistry"/> class.</summary>
    /// <param name="inner">The registry read through to on a cache miss and written through to on every save.</param>
    public CachingWorkspaceRegistry(IWorkspaceRegistry inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc/>
    public async Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(workspaceId, out var cached))
        {
            return cached;
        }

        var entry = await _inner.ResolveAsync(workspaceId, ct).ConfigureAwait(false);
        _cache[workspaceId] = entry;
        return entry;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
        _inner.ListAsync(ct);

    /// <inheritdoc/>
    public async Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _inner.SaveAsync(entry, ct).ConfigureAwait(false);
        _cache[entry.WorkspaceId] = entry;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Ferret.Workspace.Graph.Tests/Ferret.Workspace.Graph.Tests.csproj --filter CachingWorkspaceRegistryTests`
Expected: PASS, 8 tests.

- [ ] **Step 5: Run the full `Ferret.Workspace.Graph.Tests` suite to confirm no regression**

Run: `dotnet test tests/Ferret.Workspace.Graph.Tests/Ferret.Workspace.Graph.Tests.csproj`
Expected: PASS, all pre-existing tests plus the 8 new ones.

- [ ] **Step 6: Commit**

```bash
git add src/Ferret.Workspace.Graph/CachingWorkspaceRegistry.cs tests/Ferret.Workspace.Graph.Tests/CachingWorkspaceRegistryTests.cs
git commit -m "feat(workspace-graph): add CachingWorkspaceRegistry read-through cache (WIP-032)"
```

---

### Task 2: Wire the cache into the CLI composition root

**Files:**
- Modify: `src/Ferret.Cli/Commands/Workspaces/WorkspacesCliModule.cs:104-111`

**Interfaces:**
- Consumes: `CachingWorkspaceRegistry` (Task 1) — `public CachingWorkspaceRegistry(IWorkspaceRegistry inner)`.
- Produces: nothing new — `IWorkspaceRegistry` is still resolved the same way by every existing consumer (`FederatedKnowledgeStore`, `WorkspacesQueryCommandHandler`, all other `Workspaces*CommandHandler` types), now backed by the cache transparently.

- [ ] **Step 1: Update the registration**

In `src/Ferret.Cli/Commands/Workspaces/WorkspacesCliModule.cs`, replace:

```csharp
        services.AddSingleton<IWorkspaceRegistry>(_ =>
        {
            var root = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ferret", "workspaces");
            return new FileWorkspaceRegistry(root);
        });
```

with:

```csharp
        services.AddSingleton<IWorkspaceRegistry>(_ =>
        {
            var root = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ferret", "workspaces");
            return new CachingWorkspaceRegistry(new FileWorkspaceRegistry(root));
        });
```

Registration stays `AddSingleton` — required for the cache to live for the process lifetime, same reasoning already documented on `WorkspaceStateFingerprintProvider`'s own singleton registration two lines below.

- [ ] **Step 2: Build and run the full CLI test project**

Run: `dotnet build src/Ferret.sln` then `dotnet test tests/Ferret.Cli.Tests/Ferret.Cli.Tests.csproj`
Expected: 0 warnings, 0 errors, all pre-existing tests pass unmodified (this is a DI wiring change only — no CLI test asserts the concrete `IWorkspaceRegistry` type).

- [ ] **Step 3: Commit**

```bash
git add src/Ferret.Cli/Commands/Workspaces/WorkspacesCliModule.cs
git commit -m "feat(workspace-graph): wire CachingWorkspaceRegistry into the CLI composition root (WIP-032)"
```

---

### Task 3: Dogfood, benchmark, and write up the deliverable doc

**Files:**
- Create: `docs/roadmap/Workspace-Intelligence/22-WIP-032-Registry-Read-Through-Cache.md`

**Interfaces:**
- Consumes: the real `ferret` CLI (`workspaces create/add-repo/add-reference/pin-reference/query`), and the same manual-measurement style already used in `21-P3-001-Fingerprint-Optimization.md` §4 (direct instantiation + timing, not a permanent perf test — timing assertions in the automated suite are unreliable/flaky and are not required by this task).

- [ ] **Step 1: Dogfood against a real repo pair**

Using two real, separate local repos (reuse the same `ferret-platform` / `indoulia-foundation` pair from `17-Dogfooding-Sprint-1.md` / `21-P3-001-Fingerprint-Optimization.md` if still registered; otherwise create two throwaway workspaces from real directories under `C:\POC`), run:

```
ferret workspaces create --name wip032-a
ferret workspaces add-repo wip032-a <path-to-repo-a>
ferret workspaces create --name wip032-b
ferret workspaces add-repo wip032-b <path-to-repo-b>
ferret workspaces add-reference wip032-a wip032-b
ferret workspaces query wip032-a "<some real term>"
ferret workspaces query wip032-a "<some real term>"
ferret workspaces query wip032-a "<some real term>"
```

Note: each CLI invocation is its own process, so this proves *correctness* (identical results/diagnostics each time) and exercises every code path (create, add-repo, add-reference, query), but does **not** by itself demonstrate the cache's performance win — that requires one long-lived process (Step 2). Then exercise invalidation in the same session:

```
ferret workspaces remove-reference wip032-a wip032-b
ferret workspaces query wip032-a "<some real term>"
```

Confirm the reference-removed query's diagnostics/results no longer include `wip032-b`'s content, and clean up afterward (`ferret workspaces remove-repo`, or just leave the throwaway entries — note whichever was done in the doc).

- [ ] **Step 2: Benchmark repeated resolution in one process**

Write a short throwaway console probe (do not commit it) that, in a single process, constructs a `FileWorkspaceRegistry` pointed at the real `~/.ferret/workspaces` root, wraps it in `CachingWorkspaceRegistry`, and times N repeated `ResolveAsync` calls for the same workspace ID (and its reference) before vs. after the wrap — mirroring the measurement style in `21-P3-001-Fingerprint-Optimization.md` §4. Record cold (first call) vs. warm (cached) timings.

- [ ] **Step 3: Write the deliverable doc**

Create `docs/roadmap/Workspace-Intelligence/22-WIP-032-Registry-Read-Through-Cache.md` following the structure of `21-P3-001-Fingerprint-Optimization.md`, covering exactly the 7 sections the mission requires:

1. Implementation Summary — `CachingWorkspaceRegistry` added, wraps `FileWorkspaceRegistry` in `WorkspacesCliModule`.
2. Cache Design — key (`WorkspaceId`), invalidation (write-through on `SaveAsync`), why no architecture change was needed (single mutation path, singleton DI lifetime already established by `WorkspaceStateFingerprintProvider`'s precedent).
3. Test Summary — the 8 tests from Task 1, mapped to the mission's acceptance criteria.
4. Benchmark — actual numbers from Step 2.
5. Dogfooding — actual workflow and output from Step 1, including the remove-reference invalidation check.
6. What Implementation Taught Us — max 5 bullets, implementation evidence only (e.g. confirm/deny whether registry I/O was actually measurable against BM25 fan-out cost).
7. Architecture Validation — state explicitly: architecture upheld (yes/no), any assumption invalidated, new ADR required (expected: no), technical debt introduced (expected: none).

- [ ] **Step 4: Commit**

```bash
git add docs/roadmap/Workspace-Intelligence/22-WIP-032-Registry-Read-Through-Cache.md
git commit -m "docs(workspace-intelligence): record WIP-032 registry read-through cache results"
```

---

## Self-Review Notes

- **Spec coverage:** All 7 required test scenarios are covered by Task 1's 8 tests (repeated lookup, update invalidation, add-reference invalidation, remove-reference invalidation, corrupt-throws, missing-workspace, no-process-survival) plus a `ListAsync` pass-through sanity check. DI wiring (Task 2) is the only production code path change. Dogfooding + benchmark + write-up satisfy the Deliverables list end-to-end (Task 3).
- **Placeholder scan:** none — every step has complete, runnable code or exact commands.
- **Type consistency:** `CachingWorkspaceRegistry(IWorkspaceRegistry inner)` constructor signature is identical between Task 1 (definition) and Task 2 (registration site).
