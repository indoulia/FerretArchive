using Ferret.Cli.Cli;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Checks that the configured workspace root directory exists on disk.</summary>
internal sealed class WorkspaceRootCheck : IDiagnosticCheck
{
    private readonly string _workspaceRoot;

    internal WorkspaceRootCheck(string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(workspaceRoot);
        _workspaceRoot = workspaceRoot;
    }

    /// <inheritdoc/>
    public string Name => "Workspace root exists";

    /// <inheritdoc/>
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        var result = Directory.Exists(_workspaceRoot)
            ? DiagnosticCheckResult.Pass()
            : DiagnosticCheckResult.Fail($"Directory not found: {_workspaceRoot}");
        return Task.FromResult(result);
    }
}
