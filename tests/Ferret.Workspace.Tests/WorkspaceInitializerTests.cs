using Ferret.Core.Workspace;

using Ferret.Workspace.Persistence;

namespace Ferret.Workspace.Tests;

public sealed class WorkspaceInitializerTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    private WorkspacePath RootPath => WorkspacePath.Create(_dir.Path);

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task InitialiseAsync_CreatesDotFerretDirectory()
    {
        await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        Assert.True(Directory.Exists(Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName)));
    }

    [Fact]
    public async Task InitialiseAsync_CreatesAllContextOsDirectories()
    {
        await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var ferretDir = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName);
        foreach (var sub in WorkspaceLayout.AllDirectories)
        {
            var fullPath = Path.Combine(ferretDir, sub.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(Directory.Exists(fullPath), $"Missing directory: {sub}");
        }
    }

    [Fact]
    public async Task InitialiseAsync_WritesManifestFile()
    {
        await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var manifestPath = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.ManifestFileName);
        Assert.True(File.Exists(manifestPath));
    }

    [Fact]
    public async Task InitialiseAsync_WritesStateFile()
    {
        await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var statePath = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.StateFileName);
        Assert.True(File.Exists(statePath));
    }

    [Fact]
    public async Task InitialiseAsync_WritesAllConfigFiles()
    {
        await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var configDir = Path.Combine(_dir.Path, WorkspaceLayout.RootDirectoryName, WorkspaceLayout.ConfigDirectoryName);
        foreach (var fileName in WorkspaceLayout.ConfigFileNames)
        {
            Assert.True(File.Exists(Path.Combine(configDir, fileName)), $"Missing config: {fileName}");
        }
    }

    [Fact]
    public async Task InitialiseAsync_ManifestContainsContextOsVersion()
    {
        await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        var manifest = await JsonWorkspaceStore.ReadManifestAsync(RootPath, CancellationToken.None);
        Assert.NotNull(manifest);
        Assert.Equal("1.0", manifest.ContextOsVersion);
        Assert.Equal("repository", manifest.WorkspaceType);
    }

    [Fact]
    public async Task InitialiseAsync_ReturnsContextWithCorrectRootPath()
    {
        var context = await WorkspaceInitializer.InitialiseAsync(RootPath, null, CancellationToken.None);
        Assert.Equal(RootPath, context.RootPath);
        Assert.NotEmpty(context.Metadata.Name);
    }
}
