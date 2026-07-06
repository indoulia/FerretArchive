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
    public async Task RemoveAsync_WhenEntryExists_ResolveAsyncThenReturnsNull()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "throwaway" };
        await registry.SaveAsync(entry);

        await registry.RemoveAsync(entry.WorkspaceId);

        Assert.Null(await registry.ResolveAsync(entry.WorkspaceId));
    }

    [Fact]
    public async Task RemoveAsync_WhenEntryExists_ExcludesItFromListAsync()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "throwaway" };
        var kept = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "kept" };
        await registry.SaveAsync(entry);
        await registry.SaveAsync(kept);

        await registry.RemoveAsync(entry.WorkspaceId);

        var remaining = await registry.ListAsync();
        Assert.Single(remaining);
        Assert.Equal("kept", remaining[0].Name);
    }

    [Fact]
    public async Task RemoveAsync_WhenNoEntryStoredForId_DoesNotThrow()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);

        await registry.RemoveAsync(Guid.NewGuid());
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

    [Fact]
    public async Task SaveThenResolve_WithKindAndMembers_RoundTrips()
    {
        var entry = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "customer-platform",
            Kind = "team",
            Members = new WorkspaceMembers
            {
                Repos = [new RepoMember { Remote = "git@github.com:acme/service-a.git", LocalPath = "C:/dev/service-a" }],
                Documents = [new DocumentMember { Path = "C:/dev/notes/auth-decisions", Type = "notes" }],
            },
        };
        var writer = new FileWorkspaceRegistry(_rootDirectory);
        await writer.SaveAsync(entry);

        var reader = new FileWorkspaceRegistry(_rootDirectory);
        var result = await reader.ResolveAsync(entry.WorkspaceId);

        Assert.Equal(entry, result);
    }

    [Fact]
    public async Task SaveThenResolve_WithoutExplicitKindOrMembers_DefaultsToPersonalWithEmptyMembers()
    {
        // "Missing optional fields": a caller that only supplies WorkspaceId/Name (as every
        // WIP-010 test in this file already does) must keep working unmodified — Kind and
        // Members are additive, not required, per ADR-0026's additive-schema-upgrade philosophy.
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        await registry.SaveAsync(entry);

        var result = await registry.ResolveAsync(entry.WorkspaceId);

        Assert.Equal("personal", result?.Kind);
        Assert.Empty(result!.Members.Repos);
        Assert.Empty(result.Members.Documents);
    }

    [Fact]
    public async Task ResolveAsync_WhenManifestJsonOmitsMembersEntirely_ReturnsEntryWithEmptyMembers()
    {
        // Proves the "missing optional field" behavior against a hand-written manifest, not just
        // a manifest this same code wrote — the pre-WIP-011 on-disk shape (no "members" property
        // at all, since it did not exist before this change) must still be readable.
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        var idString = workspaceId.ToString();
        await File.WriteAllTextAsync(
            manifestPath,
            "{\"schemaVersion\":\"1.0\",\"workspaceId\":\"" + idString + "\",\"name\":\"customer-platform\"}");

        var result = await registry.ResolveAsync(workspaceId);

        Assert.NotNull(result);
        Assert.Empty(result!.Members.Repos);
        Assert.Empty(result.Members.Documents);
    }

    [Fact]
    public async Task ResolveAsync_WhenManifestContainsAnUnknownFutureField_IgnoresItAndReadsSuccessfully()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        var idString = workspaceId.ToString();
        var json = "{\"schemaVersion\":\"1.0\",\"workspaceId\":\"" + idString + "\",\"name\":\"customer-platform\","
            + "\"sharing\":{\"ownerId\":\"user_1\",\"visibility\":\"team\"}}";
        await File.WriteAllTextAsync(manifestPath, json);

        var result = await registry.ResolveAsync(workspaceId);

        Assert.NotNull(result);
        Assert.Equal("customer-platform", result!.Name);
    }

    [Fact]
    public async Task ResolveAsync_WhenSchemaVersionIsNotOneThisReaderRecognizes_ThrowsWorkspaceRegistryCorruptException()
    {
        // The "synthetic future-schema-version manifest" acceptance criterion (WIP-011 backlog):
        // proves ARCH-001 S12.4's fail-closed-when-unreachable behavior without any real v1.1/v1.2
        // migration code existing yet — there is no declared migration path to "9.9", so it is not
        // reachable, and the manifest is reported unresolvable per ADR-0026, not silently accepted.
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        var idString = workspaceId.ToString();
        await File.WriteAllTextAsync(
            manifestPath,
            "{\"schemaVersion\":\"9.9\",\"workspaceId\":\"" + idString + "\",\"name\":\"customer-platform\"}");

        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => registry.ResolveAsync(workspaceId));
    }

    [Fact]
    public async Task ResolveAsync_WhenSchemaVersionIsNotRecognized_DoesNotDeleteTheFile()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        var idString = workspaceId.ToString();
        await File.WriteAllTextAsync(
            manifestPath,
            "{\"schemaVersion\":\"9.9\",\"workspaceId\":\"" + idString + "\",\"name\":\"customer-platform\"}");

        await Assert.ThrowsAsync<WorkspaceRegistryCorruptException>(() => registry.ResolveAsync(workspaceId));

        Assert.True(File.Exists(manifestPath), "An unreachable schemaVersion is reported unresolvable, never deleted (ADR-0026).");
    }

    [Fact]
    public async Task SaveAsync_EmbedsCurrentSchemaVersion()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" });

        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        var json = await File.ReadAllTextAsync(manifestPath);
        using var document = System.Text.Json.JsonDocument.Parse(json);

        Assert.Equal("1.0", document.RootElement.GetProperty("schemaVersion").GetString());
    }

    [Fact]
    public async Task SaveAsync_ProducesByteIdenticalOutput_ForEquivalentInput()
    {
        var entry = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "customer-platform",
            Kind = "team",
            Members = new WorkspaceMembers
            {
                Repos = [new RepoMember { Remote = "git@github.com:acme/service-a.git", LocalPath = "C:/dev/service-a" }],
            },
        };
        var firstRoot = Path.Join(_rootDirectory, "first");
        var secondRoot = Path.Join(_rootDirectory, "second");

        await new FileWorkspaceRegistry(firstRoot).SaveAsync(entry);
        await new FileWorkspaceRegistry(secondRoot).SaveAsync(entry with { });

        var firstBytes = await File.ReadAllBytesAsync(Directory.GetFiles(firstRoot, "workspace.json", SearchOption.AllDirectories).Single());
        var secondBytes = await File.ReadAllBytesAsync(Directory.GetFiles(secondRoot, "workspace.json", SearchOption.AllDirectories).Single());

        Assert.Equal(firstBytes, secondBytes);
    }

    [Fact]
    public async Task SaveThenResolve_WithReferences_RoundTrips()
    {
        var entry = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            SchemaVersion = FileWorkspaceRegistry.ReferencesSchemaVersion,
            References = [new WorkspaceReference { WorkspaceId = Guid.NewGuid() }],
        };
        var writer = new FileWorkspaceRegistry(_rootDirectory);
        await writer.SaveAsync(entry);

        var reader = new FileWorkspaceRegistry(_rootDirectory);
        var result = await reader.ResolveAsync(entry.WorkspaceId);

        Assert.Equal(entry, result);
        Assert.Equal("read-only", result!.References[0].Mode);
        Assert.Null(result.References[0].PinnedStateHash);
    }

    [Fact]
    public async Task SaveThenResolve_WithoutReferences_KeepsSchemaVersionOneZero()
    {
        // Backward-compat invariant: an entry with no references must be byte-identical to
        // pre-WIP-SLICE-2 output — adding the References property must not change SchemaVersion
        // for entries that don't use it.
        var entry = new WorkspaceRegistryEntry { WorkspaceId = Guid.NewGuid(), Name = "customer-platform" };
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        await registry.SaveAsync(entry);

        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        var json = await File.ReadAllTextAsync(manifestPath);
        using var document = System.Text.Json.JsonDocument.Parse(json);

        Assert.Equal("1.0", document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.False(document.RootElement.TryGetProperty("references", out _));
    }

    [Fact]
    public async Task ResolveAsync_WhenManifestOmitsReferencesEntirely_ReturnsEntryWithEmptyReferences()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        await registry.SaveAsync(new WorkspaceRegistryEntry { WorkspaceId = workspaceId, Name = "customer-platform" });
        var manifestPath = Directory.GetFiles(_rootDirectory, "workspace.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(
            manifestPath,
            "{\"schemaVersion\":\"1.0\",\"workspaceId\":\"" + workspaceId + "\",\"name\":\"customer-platform\"}");

        var result = await registry.ResolveAsync(workspaceId);

        Assert.NotNull(result);
        Assert.Empty(result!.References);
    }

    [Fact]
    public async Task ResolveAsync_WhenSchemaVersionIsOnePointOne_IsReachable()
    {
        var registry = new FileWorkspaceRegistry(_rootDirectory);
        var workspaceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var manifestPath = Path.Join(EnsureManifestDirectory(workspaceId), "workspace.json");
        var json = "{\"schemaVersion\":\"1.1\",\"workspaceId\":\"" + workspaceId + "\",\"name\":\"service-a\","
            + "\"references\":[{\"workspaceId\":\"" + targetId + "\",\"mode\":\"read-only\"}]}";
        await File.WriteAllTextAsync(manifestPath, json);

        var result = await registry.ResolveAsync(workspaceId);

        Assert.NotNull(result);
        Assert.Single(result!.References);
        Assert.Equal(targetId, result.References[0].WorkspaceId);
    }

    private string EnsureManifestDirectory(Guid workspaceId)
    {
        var dir = Path.Join(_rootDirectory, workspaceId.ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
