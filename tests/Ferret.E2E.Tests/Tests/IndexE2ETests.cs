using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret index.</summary>
public sealed class IndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public Task InitializeAsync() => _workspace.InitializeAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>index on a workspace with 3 cs files exits with code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Index_ThreeCsFiles_ExitCodeZero()
    {
        await _workspace.WriteSampleCsFilesAsync();

        var (exitCode, _, _) = await _workspace.RunAsync("index");

        Assert.Equal(0, exitCode);
    }

    /// <summary>index output contains "Indexed" when files are processed.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Index_ThreeCsFiles_OutputContainsIndexed()
    {
        await _workspace.WriteSampleCsFilesAsync();

        var (_, stdout, _) = await _workspace.RunAsync("index");

        Assert.Contains("Indexed", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>index output contains "Index complete" summary line.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Index_ThreeCsFiles_OutputContainsIndexComplete()
    {
        await _workspace.WriteSampleCsFilesAsync();

        var (_, stdout, _) = await _workspace.RunAsync("index");

        Assert.Contains("Index complete", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
