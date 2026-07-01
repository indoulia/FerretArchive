using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret doctor.</summary>
public sealed class DoctorE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public Task InitializeAsync() => _workspace.InitializeAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>doctor exits with code 0 in a valid workspace.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Doctor_ValidWorkspace_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("doctor");

        Assert.Equal(0, exitCode);
    }

    /// <summary>doctor output contains "healthy" in a valid workspace.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Doctor_ValidWorkspace_OutputContainsHealthy()
    {
        var (_, stdout, _) = await _workspace.RunAsync("doctor");

        Assert.Contains("healthy", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>doctor output contains "Ferret Doctor" header.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Doctor_ValidWorkspace_OutputContainsDoctorHeader()
    {
        var (_, stdout, _) = await _workspace.RunAsync("doctor");

        Assert.Contains("Ferret Doctor", stdout, StringComparison.Ordinal);
    }

    /// <summary>doctor emits the Parser Platform report through the published binary.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Doctor_ReportsParserPlatformSection()
    {
        var (_, stdout, _) = await _workspace.RunAsync("doctor");

        Assert.Contains("Parser Platform", stdout, StringComparison.Ordinal);
        Assert.Contains("Excel (XLSX) Parser", stdout, StringComparison.Ordinal);
        Assert.Contains("Parser Packages", stdout, StringComparison.Ordinal);
    }
}
