using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret incremental index behaviour.</summary>
[Collection("IncrementalIndex")]
public sealed class IncrementalIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>Second index run on unchanged files reports skipped documents.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IncrementalIndex_SecondRun_ReportsSkipped()
    {
        // First run — indexes everything
        await _workspace.RunAsync("index");

        // Second run — files unchanged; engine should skip them
        var (exitCode, stdout, _) = await _workspace.RunAsync("index");

        Assert.Equal(0, exitCode);
        Assert.Contains("Skipped", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Second index run on unchanged files exits with code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IncrementalIndex_SecondRun_ExitCodeZero()
    {
        await _workspace.RunAsync("index");
        var (exitCode, _, _) = await _workspace.RunAsync("index");

        Assert.Equal(0, exitCode);
    }
}
