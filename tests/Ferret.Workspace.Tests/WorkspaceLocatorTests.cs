using Ferret.Core.Workspace;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceLocatorTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly WorkspaceLocator _locator = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task LocateAsync_WhenDotFerretAtRoot_ReturnsRoot()
    {
        CreateWorkspaceAt(_dir.Path);
        var result = await _locator.LocateAsync(WorkspacePath.Create(_dir.Path));
        Assert.NotNull(result);
        Assert.Equal(_dir.Path, result.FullPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LocateAsync_WhenCalledFromSubdirectory_FindsAncestorRoot()
    {
        CreateWorkspaceAt(_dir.Path);
        var subDir = Path.Join(_dir.Path, "src", "core");
        Directory.CreateDirectory(subDir);
        var result = await _locator.LocateAsync(WorkspacePath.Create(subDir));
        Assert.NotNull(result);
        Assert.Equal(_dir.Path, result.FullPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LocateAsync_WhenNoWorkspaceExists_ReturnsNull()
    {
        var result = await _locator.LocateAsync(WorkspacePath.Create(_dir.Path));
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenDotFerretWithManifest_ReturnsTrue()
    {
        CreateWorkspaceAt(_dir.Path);
        Assert.True(await _locator.ExistsAsync(WorkspacePath.Create(_dir.Path)));
    }

    [Fact]
    public async Task ExistsAsync_WhenNoDotFerret_ReturnsFalse()
    {
        Assert.False(await _locator.ExistsAsync(WorkspacePath.Create(_dir.Path)));
    }

    [Fact]
    public async Task ExistsAsync_WhenDotFerretExistsButNoManifest_ReturnsFalse()
    {
        Directory.CreateDirectory(Path.Join(_dir.Path, WorkspaceLayout.RootDirectoryName));
        Assert.False(await _locator.ExistsAsync(WorkspacePath.Create(_dir.Path)));
    }

    private static void CreateWorkspaceAt(string root)
    {
        var ferretDir = Path.Join(root, WorkspaceLayout.RootDirectoryName);
        Directory.CreateDirectory(ferretDir);
        File.WriteAllText(Path.Join(ferretDir, WorkspaceLayout.ManifestFileName), "{}");
    }
}
