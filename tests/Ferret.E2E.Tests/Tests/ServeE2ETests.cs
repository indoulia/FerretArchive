using Ferret.E2E.Tests.Fixtures;
using Ferret.E2E.Tests.Infrastructure;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E smoke test for ferret serve (MCP stdio).</summary>
public sealed class ServeE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);

        // Index must exist before serving — serve depends on the keyword DB.
        await _workspace.WriteSampleCsFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>serve starts within 2 s and produces no error output on stderr.</summary>
    [Fact]
    public async Task Serve_StartsWithoutErrorOutput()
    {
        // Run with a 2-second timeout — the process will be killed after 2 s.
        var (_, _, stderr) = await FerretCliRunner.RunAsync(
            _workspace.BinaryPath,
            "serve",
            _workspace.WorkspaceDir,
            TimeSpan.FromSeconds(2));

        // Stderr must be empty — any exception or startup error written there is a test failure.
        Assert.True(
            string.IsNullOrWhiteSpace(stderr),
            $"ferret serve produced unexpected stderr:\n{stderr}");
    }
}
