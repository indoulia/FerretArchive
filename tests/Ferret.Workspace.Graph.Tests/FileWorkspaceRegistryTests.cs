namespace Ferret.Workspace.Graph.Tests;

public sealed class FileWorkspaceRegistryTests : IDisposable
{
    private readonly string _rootDirectory;

    public FileWorkspaceRegistryTests()
    {
        _rootDirectory = Path.Join(Path.GetTempPath(), $"ferret-workspace-registry-test-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task SaveThenResolve_ViaNewInstance_RoundTrips()
    {
        var entry = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "customer-platform",
        };
        var writer = new FileWorkspaceRegistry(_rootDirectory);
        await writer.SaveAsync(entry);

        var reader = new FileWorkspaceRegistry(_rootDirectory);
        var result = await reader.ResolveAsync(entry.WorkspaceId);

        Assert.Equal(entry, result);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoEntryStoredForId_ReturnsNull()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);

        var result = await registry.ResolveAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_Overwrites_PreviousEntry_ForTheSameId()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "old-name" });

        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "new-name" });
        var result = await registry.ResolveAsync(workspaceId);

        Assert.Equal("new-name", result?.Name);
    }

    [Fact]
    public async Task ListAsync_WhenRegistryIsEmpty_ReturnsEmpty()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);

        var result = await registry.ListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsEveryStoredEntry()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var first = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "workspace-a" };
        var second = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "workspace-b" };
        await registry.SaveAsync(first);
        await registry.SaveAsync(second);

        var result = await registry.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "workspace-a");
        Assert.Contains(result, e => e.Name == "workspace-b");
    }

    [Fact]
    public async Task SaveAsync_WritesAtomically_LeavingNoTemporaryFilesBehind()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);

        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" });

        Assert.Single(Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(_rootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SaveAsync_Overwrite_AlsoLeavesNoTemporaryFilesBehind()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "first" });

        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "second" });

        Assert.Empty(Directory.GetFiles(_rootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ResolveAsync_WhenAnOrphanedTempFileExistsAlongsideAValidEntry_StillReturnsTheValidEntry()
    {
        // Simulates a crash between File.Create(tmpPath) and File.Move in a *subsequent* SaveAsync
        // (e.g. updating the workspace's name): the previously-committed workspace.json must remain
        // intact and resolvable, exactly as ADR-0026's atomic-write guarantee requires — a crash
        // mid-write must never destroy the last known-good state.
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await registry.SaveAsync(entry);
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(manifestPath + ".tmp", "{ partial write left behind by a crashed SaveAsync");

        var result = await registry.ResolveAsync(entry.WorkspaceId);

        Assert.Equal(entry, result);
    }

    [Fact]
    public async Task ResolveAsync_WhenManifestContainsMalformedJson_ThrowsWorkspaceRegistryCorruptException()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await registry.SaveAsync(entry);
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(manifestPath, "{ this is not valid json");

        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => registry.ResolveAsync(entry.WorkspaceId));
    }

    [Fact]
    public async Task ResolveAsync_WhenManifestContainsMalformedJson_DoesNotDeleteTheFile()
    {
        // ADR-0026's deliberate divergence from Ferret.Persistence.FileDependencyStateStore's
        // eviction behavior: a workspace registry entry is real user configuration, not a
        // recomputable cache record, so a corrupt manifest is never auto-deleted.
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await registry.SaveAsync(entry);
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(manifestPath, "{ this is not valid json");

        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => registry.ResolveAsync(entry.WorkspaceId));

        Assert.True(File.Exists(manifestPath), "A corrupt manifest must be left in place for manual recovery, never silently deleted.");
    }

    [Fact]
    public async Task ResolveAsync_WhenManifestContainsMalformedJson_ExceptionNamesTheFile()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        await registry.SaveAsync(entry);
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(manifestPath, "{ this is not valid json");

        var exception = await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => registry.ResolveAsync(entry.WorkspaceId));

        Assert.Equal(manifestPath, exception.FilePath);
    }

    [Fact]
    public async Task ListAsync_WhenOneOfSeveralManifestsIsCorrupt_PropagatesTheException()
    {
        // Documented scope decision (not specified by the backlog): WIP-010 does not implement
        // partial/best-effort listing when one of many entries is corrupt. A CLI layer that wants
        // to show the healthy entries anyway is a WIP-012 concern, not this storage primitive's.
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "healthy" });
        var corruptEntry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "corrupt" };
        await registry.SaveAsync(corruptEntry);
        var corruptManifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories)
            .Single(p => p.Contains(corruptEntry.WorkspaceId.ToString("N"), StringComparison.Ordinal));
        await File.WriteAllTextAsync(corruptManifestPath, "{ this is not valid json");

        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => registry.ListAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
