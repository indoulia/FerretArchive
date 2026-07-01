using Ferret.E2E.Tests.Infrastructure;

namespace Ferret.E2E.Tests.Fixtures;

/// <summary>
/// xUnit class fixture that provisions a temporary Ferret workspace for E2E tests.
/// InitializeAsync: creates temp dir, publishes binary, runs workspace init.
/// DisposeAsync: deletes temp dir.
/// </summary>
public sealed class WorkspaceFixture : IAsyncLifetime
{
    /// <summary>Gets the absolute path to the temporary workspace directory.</summary>
    public string WorkspaceDir { get; } = Path.Join(
        Path.GetTempPath(),
        "ferret-e2e-ws-" + Guid.NewGuid().ToString("N")[..8]);

    /// <summary>Gets the absolute path to the ferret binary after initialization.</summary>
    public string BinaryPath { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(WorkspaceDir);
        BinaryPath = await FerretBinaryLocator.GetOrPublishAsync().ConfigureAwait(false);

        // Initialize the workspace so all tests start from a valid state.
        var (exitCode, _, stderr) = await FerretCliRunner.RunAsync(
            BinaryPath,
            "workspace init",
            WorkspaceDir,
            TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"workspace init failed (exit {exitCode}):\n{stderr}");
        }
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        if (Directory.Exists(WorkspaceDir))
        {
            Directory.Delete(WorkspaceDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <summary>Writes three sample C# source files into the workspace directory.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task WriteSampleCsFilesAsync()
    {
        await File.WriteAllTextAsync(
            Path.Join(WorkspaceDir, "Alpha.cs"),
            "namespace Sample;\npublic class AlphaService { }").ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Join(WorkspaceDir, "Beta.cs"),
            "namespace Sample;\npublic class BetaRepository { }").ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Join(WorkspaceDir, "Gamma.cs"),
            "namespace Sample;\npublic class GammaController { }").ConfigureAwait(false);
    }

    /// <summary>Writes realistic enterprise CSV/TSV exports (Jira / Azure DevOps style) into the workspace.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task WriteEnterpriseCsvFilesAsync()
    {
        const string issuesCsv =
            "Key,Summary,Severity,Status,Assignee,Sprint\n" +
            "PROJ-101,Login fails for SSO users,High,Open,Dana Wells,Sprint 14\n" +
            "PROJ-102,\"Timeout on export, then crash\",Critical,In Progress,Rahul Menon,Sprint 14\n" +
            "PROJ-103,Add audit log retention policy,Medium,Done,Dana Wells,Sprint 13\n";

        await File.WriteAllTextAsync(
            Path.Join(WorkspaceDir, "issues.csv"),
            issuesCsv).ConfigureAwait(false);

        const string workItemsTsv =
            "ID\tTitle\tState\tAssignedTo\tIteration\n" +
            "5001\tAuthentication token refresh\tActive\tPriya Nair\tSprint 14\n" +
            "5002\tCustomer risk register review\tClosed\tOmar Said\tSprint 13\n";

        await File.WriteAllTextAsync(
            Path.Join(WorkspaceDir, "workitems.tsv"),
            workItemsTsv).ConfigureAwait(false);
    }

    /// <summary>Runs a ferret command in the workspace directory.</summary>
    /// <param name="args">The command arguments to pass to the ferret binary.</param>
    /// <param name="timeout">Optional timeout; defaults to 30 seconds.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string args,
        TimeSpan? timeout = null) =>
        FerretCliRunner.RunAsync(
            BinaryPath,
            args,
            WorkspaceDir,
            timeout ?? TimeSpan.FromSeconds(30));
}
