using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E: index real PDFs through the published binary, then prove the text is searchable.</summary>
public sealed class PdfIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSamplePdfFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search after indexing PDFs returns exit code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_AfterPdfIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search throughput");

        Assert.Equal(0, exitCode);
    }

    /// <summary>A word from a PDF body is searchable and points at the source PDF.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_PdfBodyWord_ReturnsSourcePdf()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search throughput");

        Assert.Contains("architecture-decision.pdf", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A word from the second PDF is searchable and points at that file.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_SecondPdfWord_ReturnsIncidentReport()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search saturated");

        Assert.Contains("incident-report.pdf", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
