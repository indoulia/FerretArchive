using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret search.</summary>
public sealed class SearchE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search "class" after indexing returns exit code 0.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Search_AfterIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search class");

        Assert.Equal(0, exitCode);
    }

    /// <summary>search "AlphaService" returns output containing Alpha.cs.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Search_AlphaService_ReturnsAlphaCs()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search AlphaService");

        Assert.Contains("Alpha.cs", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>search "BetaRepository" returns output containing Beta.cs.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Search_BetaRepository_ReturnsBetaCs()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search BetaRepository");

        Assert.Contains("Beta.cs", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>search "GammaController" returns output containing Gamma.cs.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task Search_GammaController_ReturnsGammaCs()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search GammaController");

        Assert.Contains("Gamma.cs", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
