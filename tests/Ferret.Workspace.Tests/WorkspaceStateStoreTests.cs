using Ferret.Core.Workspace;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceStateStoreTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceStateStore _store = new();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task ReadStatistics_WhenNoFile_ReturnsDefaults()
    {
        CreateFerretDir();
        var stats = await _store.ReadStatisticsAsync(RootPath);

        Assert.Equal(0, stats.TotalFiles);
        Assert.Equal(0, stats.IndexedFiles);
        Assert.Equal(DateTimeOffset.MinValue, stats.LastIndexed);
    }

    [Fact]
    public async Task WriteStatistics_ThenRead_RoundTrips()
    {
        CreateFerretDir();
        var expected = WorkspaceStatistics.Create(
            100,
            80,
            new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero),
            "1.0");

        await _store.WriteStatisticsAsync(RootPath, expected);
        var restored = await _store.ReadStatisticsAsync(RootPath);

        Assert.Equal(100, restored.TotalFiles);
        Assert.Equal(80, restored.IndexedFiles);
        Assert.Equal(expected.LastIndexed, restored.LastIndexed);
    }

    [Fact]
    public async Task WriteStatistics_PreservesExistingStateFields()
    {
        CreateFerretDir();
        await JsonWorkspaceStore.WriteStateAsync(
            RootPath,
            new WorkspaceStateDto { KnowledgeVersion = 7 },
            CancellationToken.None);

        var stats = WorkspaceStatistics.Create(10, 10, DateTimeOffset.MinValue, "1.0");
        await _store.WriteStatisticsAsync(RootPath, stats);

        var dto = await JsonWorkspaceStore.ReadStateAsync(RootPath, CancellationToken.None);
        Assert.Equal(7, dto!.KnowledgeVersion);
    }

    private void CreateFerretDir() =>
        Directory.CreateDirectory(Path.Join(_dir.Path, WorkspaceLayout.RootDirectoryName));
}
