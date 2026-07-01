using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E: index real DOCX + XLSX through the published binary, then prove they are searchable.</summary>
public sealed class OfficeIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteSampleOfficeFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search after indexing Office files returns exit code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_AfterOfficeIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search columnar");

        Assert.Equal(0, exitCode);
    }

    /// <summary>A word from the DOCX body is searchable and points at the source document.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_DocxBodyWord_ReturnsDesignProposal()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search columnar");

        Assert.Contains("design-proposal.docx", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A Jira-export cell value (the stated product-value assertion) is searchable and points at the .xlsx.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_XlsxCellValue_ReturnsBugExport()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search regression");

        Assert.Contains("bug-export.xlsx", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>An XLSX assignee cell value is searchable and points at the .xlsx.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_XlsxAssignee_ReturnsBugExport()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search Rahul");

        Assert.Contains("bug-export.xlsx", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
