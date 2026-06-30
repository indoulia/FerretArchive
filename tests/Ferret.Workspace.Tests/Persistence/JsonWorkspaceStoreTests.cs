using Ferret.Core.Workspace;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests.Persistence;

public sealed class JsonWorkspaceStoreTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task ReadManifest_WhenFileNotExists_ReturnsNull()
    {
        CreateFerretDir();
        var result = await JsonWorkspaceStore.ReadManifestAsync(RootPath, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteManifest_ThenRead_RoundTrips()
    {
        CreateFerretDir();
        var manifest = new WorkspaceManifest
        {
            Id = "ws-001",
            Name = "test-project",
            ContextOsVersion = "1.0",
            WorkspaceType = "repository",
            CreatedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
        };

        await JsonWorkspaceStore.WriteManifestAsync(RootPath, manifest, CancellationToken.None);
        var restored = await JsonWorkspaceStore.ReadManifestAsync(RootPath, CancellationToken.None);

        Assert.NotNull(restored);
        Assert.Equal("ws-001", restored.Id);
        Assert.Equal("1.0", restored.ContextOsVersion);
        Assert.Equal("repository", restored.WorkspaceType);
    }

    [Fact]
    public async Task ReadState_WhenFileNotExists_ReturnsNull()
    {
        CreateFerretDir();
        var result = await JsonWorkspaceStore.ReadStateAsync(RootPath, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteState_ThenRead_RoundTrips_NestedStatistics()
    {
        CreateFerretDir();
        var dto = new WorkspaceStateDto
        {
            KnowledgeVersion = 1,
            GraphVersion = 2,
            Statistics = new StatisticsDto { TotalFiles = 5, IndexedFiles = 3 },
        };

        await JsonWorkspaceStore.WriteStateAsync(RootPath, dto, CancellationToken.None);
        var restored = await JsonWorkspaceStore.ReadStateAsync(RootPath, CancellationToken.None);

        Assert.NotNull(restored);
        Assert.Equal(1, restored.KnowledgeVersion);
        Assert.Equal(5, restored.Statistics.TotalFiles);
    }

    private void CreateFerretDir() =>
        Directory.CreateDirectory(System.IO.Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName));
}
