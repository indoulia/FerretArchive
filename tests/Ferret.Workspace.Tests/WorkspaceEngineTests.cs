using Ferret.Core.Workspace;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceEngineTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceEngine _engine = new();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task InitialiseAsync_OnFreshDirectory_Succeeds()
    {
        var result = await _engine.InitialiseAsync(RootPath);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Context);
    }

    [Fact]
    public async Task InitialiseAsync_WhenAlreadyInitialised_ReturnsFailure()
    {
        await _engine.InitialiseAsync(RootPath);
        var second = await _engine.InitialiseAsync(RootPath);
        Assert.False(second.Succeeded);
        Assert.Contains("already exists", second.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_AfterInit_ReturnsContextWithSameId()
    {
        var initResult = await _engine.InitialiseAsync(RootPath);
        var context = await _engine.LoadAsync(RootPath);
        Assert.Equal(initResult.Context!.Id, context.Id);
    }

    [Fact]
    public async Task LoadAsync_WhenNoManifest_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _engine.LoadAsync(RootPath));
    }
}
