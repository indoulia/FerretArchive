using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E tests for ferret configuration validation.</summary>
public sealed class ConfigValidateE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public Task InitializeAsync() => _workspace.InitializeAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>connector validate filesystem on a valid workspace exits with code 0.</summary>
    [Fact]
    public async Task ConnectorValidate_Filesystem_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("connector validate filesystem");

        Assert.Equal(0, exitCode);
    }

    /// <summary>connector validate filesystem outputs connector name or "valid".</summary>
    [Fact]
    public async Task ConnectorValidate_Filesystem_OutputContainsValidOrConnectorName()
    {
        var (_, stdout, _) = await _workspace.RunAsync("connector validate filesystem");

        var hasExpected =
            stdout.Contains("valid", StringComparison.OrdinalIgnoreCase) ||
            stdout.Contains("filesystem", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasExpected, $"Expected 'valid' or 'filesystem' in output. Got:\n{stdout}");
    }
}
