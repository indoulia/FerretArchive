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

    [Fact]
    public async Task RemoveAsync_EvictsTheCachedEntry_SoResolveAsyncReflectsRemovalWithoutARestart()
    {
        var workspaceId = Guid.NewGuid();
        var file = new FileWorkspaceRegistry(_rootDirectory);
        var cache = new CachingWorkspaceRegistry(file);
        await cache.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "throwaway" });
        await cache.ResolveAsync(workspaceId); // warms the cache

        await cache.RemoveAsync(workspaceId);

        Assert.Null(await cache.ResolveAsync(workspaceId));
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToInnerRegistry()
    {
        var workspaceId = Guid.NewGuid();
        var file = new FileWorkspaceRegistry(_rootDirectory);
        await file.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "throwaway" });
        var cache = new CachingWorkspaceRegistry(file);

        await cache.RemoveAsync(workspaceId);

        Assert.Null(await file.ResolveAsync(workspaceId));
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
