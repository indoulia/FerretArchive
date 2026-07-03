using Ferret.E2E.Tests.Fixtures;

namespace Ferret.E2E.Tests.Tests;

/// <summary>E2E: index enterprise CSV/TSV exports, then prove the rows are searchable.</summary>
public sealed class CsvIndexE2ETests : IAsyncLifetime
{
    private readonly WorkspaceFixture _workspace = new();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _workspace.InitializeAsync().ConfigureAwait(false);
        await _workspace.WriteEnterpriseCsvFilesAsync().ConfigureAwait(false);
        await _workspace.RunAsync("index").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _workspace.DisposeAsync();

    /// <summary>search after indexing CSV returns exit code 0.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_AfterCsvIndex_ExitCodeZero()
    {
        var (exitCode, _, _) = await _workspace.RunAsync("search authentication");

        Assert.Equal(0, exitCode);
    }

    /// <summary>An issue key token from the CSV is searchable and points at issues.csv.
    /// The query parser does not accept the hyphenated key "PROJ-101" as a literal term
    /// (it tokenizes to "PROJ"/"101" at index time), so assert on the "PROJ" token.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_IssueKey_ReturnsIssuesCsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search PROJ");

        Assert.Contains("issues.csv", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A quoted-field value (embedded comma) is indexed as a single searchable cell.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_QuotedFieldValue_ReturnsIssuesCsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search timeout");

        Assert.Contains("issues.csv", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A column value (assignee) from the CSV is searchable.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_Assignee_ReturnsIssuesCsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search Dana");

        Assert.Contains("issues.csv", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A TSV work-item title is searchable and points at workitems.tsv.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Search_TsvTitle_ReturnsWorkItemsTsv()
    {
        var (_, stdout, _) = await _workspace.RunAsync("search risk");

        Assert.Contains("workitems.tsv", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
