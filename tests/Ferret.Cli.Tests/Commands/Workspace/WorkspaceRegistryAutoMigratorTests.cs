using Ferret.Cli.Commands.Workspace;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Commands.Workspace;

internal sealed class RecordingWorkspaceRegistry : IWorkspaceRegistry
{
    internal List<WorkspaceRegistryEntry> Entries { get; } = [];

    internal List<WorkspaceRegistryEntry> SavedEntries { get; } = [];

    internal Exception? ListException { get; set; }

    internal Exception? SaveException { get; set; }

    public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default) =>
        Task.FromResult(Entries.FirstOrDefault(e => e.WorkspaceId == workspaceId));

    public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default)
    {
        if (ListException is not null)
        {
            throw ListException;
        }

        return Task.FromResult<IReadOnlyList<WorkspaceRegistryEntry>>(Entries);
    }

    public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        SavedEntries.Add(entry);
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

public sealed class WorkspaceRegistryAutoMigratorTests : IDisposable
{
    private readonly string _repoPath;

    public WorkspaceRegistryAutoMigratorTests()
    {
        _repoPath = Path.Join(Path.GetTempPath(), $"ferret-auto-migrate-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_repoPath);
    }

    [Fact]
    public async Task EnsureMigratedAsync_NoExistingEntry_CreatesPersonalWorkspaceEntry()
    {
        WriteGitConfig("""
            [remote "origin"]
                url = git@github.com:acme/service-a.git
            """);
        var registry = new RecordingWorkspaceRegistry();
        var migrator = new WorkspaceRegistryAutoMigrator(registry, NullLogger<WorkspaceRegistryAutoMigrator>.Instance);

        await migrator.EnsureMigratedAsync(_repoPath);

        var saved = Assert.Single(registry.SavedEntries);
        Assert.Equal("personal", saved.Kind);
        var member = Assert.Single(saved.Members.Repos);
        Assert.Equal("github.com/acme/service-a", member.Remote);
        Assert.Empty(saved.References);
    }

    [Fact]
    public async Task EnsureMigratedAsync_EntryAlreadyExistsForIdentity_DoesNotCreateDuplicate()
    {
        WriteGitConfig("""
            [remote "origin"]
                url = git@github.com:acme/service-a.git
            """);
        var registry = new RecordingWorkspaceRegistry();
        registry.Entries.Add(new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "already-migrated",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "github.com/acme/service-a" }] },
        });
        var migrator = new WorkspaceRegistryAutoMigrator(registry, NullLogger<WorkspaceRegistryAutoMigrator>.Instance);

        await migrator.EnsureMigratedAsync(_repoPath);

        Assert.Empty(registry.SavedEntries);
    }

    [Fact]
    public async Task EnsureMigratedAsync_NotAGitRepository_DoesNotThrowAndDoesNotCreateEntry()
    {
        var registry = new RecordingWorkspaceRegistry();
        var migrator = new WorkspaceRegistryAutoMigrator(registry, NullLogger<WorkspaceRegistryAutoMigrator>.Instance);

        await migrator.EnsureMigratedAsync(_repoPath);

        Assert.Empty(registry.SavedEntries);
    }

    [Fact]
    public async Task EnsureMigratedAsync_RegistryListThrows_DoesNotThrow()
    {
        WriteGitConfig("""
            [remote "origin"]
                url = git@github.com:acme/service-a.git
            """);
        var registry = new RecordingWorkspaceRegistry { ListException = new WorkspaceRegistryCorruptException("bad.json", "malformed") };
        var migrator = new WorkspaceRegistryAutoMigrator(registry, NullLogger<WorkspaceRegistryAutoMigrator>.Instance);

        await migrator.EnsureMigratedAsync(_repoPath);
    }

    [Fact]
    public async Task EnsureMigratedAsync_RegistrySaveThrows_DoesNotThrow()
    {
        WriteGitConfig("""
            [remote "origin"]
                url = git@github.com:acme/service-a.git
            """);
        var registry = new RecordingWorkspaceRegistry { SaveException = new IOException("disk full") };
        var migrator = new WorkspaceRegistryAutoMigrator(registry, NullLogger<WorkspaceRegistryAutoMigrator>.Instance);

        await migrator.EnsureMigratedAsync(_repoPath);
    }

    private void WriteGitConfig(string content)
    {
        var gitDir = Path.Join(_repoPath, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Join(gitDir, "config"), content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoPath))
        {
            Directory.Delete(_repoPath, recursive: true);
        }
    }
}
