using Ferret.E2E.Tests.Infrastructure;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret workspace init.</summary>
[Collection("WorkspaceInit")]
public sealed class WorkspaceInitE2ETests : IAsyncLifetime
{
    private readonly string _tempDir = Path.Join(
        Path.GetTempPath(),
        "ferret-e2e-init-" + Guid.NewGuid().ToString("N")[..8]);

    private string _binaryPath = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempDir);
        _binaryPath = await FerretBinaryLocator.GetOrPublishAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <summary>workspace init in a clean directory creates .ferret/workspace.json.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WorkspaceInit_CreatesWorkspaceJson()
    {
        var (exitCode, _, _) = await FerretCliRunner.RunAsync(
            _binaryPath,
            "workspace init",
            _tempDir,
            TimeSpan.FromSeconds(30));

        Assert.Equal(0, exitCode);
        Assert.True(
            File.Exists(Path.Join(_tempDir, ".ferret", "workspace.json")),
            ".ferret/workspace.json must exist after workspace init");
    }

    /// <summary>workspace init in a clean directory creates .ferret/state.json.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WorkspaceInit_CreatesStateJson()
    {
        var (exitCode, _, _) = await FerretCliRunner.RunAsync(
            _binaryPath,
            "workspace init",
            _tempDir,
            TimeSpan.FromSeconds(30));

        Assert.Equal(0, exitCode);
        Assert.True(
            File.Exists(Path.Join(_tempDir, ".ferret", "state.json")),
            ".ferret/state.json must exist after workspace init");
    }

    /// <summary>workspace init when already initialised returns non-zero exit code.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WorkspaceInit_WhenAlreadyInitialised_ReturnsNonZeroExitCode()
    {
        // First init
        await FerretCliRunner.RunAsync(
            _binaryPath, "workspace init", _tempDir, TimeSpan.FromSeconds(30));

        // Second init — must fail
        var (exitCode, _, _) = await FerretCliRunner.RunAsync(
            _binaryPath,
            "workspace init",
            _tempDir,
            TimeSpan.FromSeconds(30));

        Assert.NotEqual(0, exitCode);
    }
}
