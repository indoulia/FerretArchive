using Ferret.Cli.Cli;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Diagnostics.Checks;

/// <summary>Checks that the <c>.ferret</c> configuration directory exists under the workspace root.</summary>
internal sealed class FerretConfigDirCheck : IDiagnosticCheck
{
    private readonly string _workspaceRoot;

    internal FerretConfigDirCheck(string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(workspaceRoot);
        _workspaceRoot = workspaceRoot;
    }

    /// <inheritdoc/>
    public string Name => ".ferret config directory exists";

    /// <inheritdoc/>
    public Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken)
    {
        var ferretDir = Path.Combine(_workspaceRoot, WorkspaceLayout.RootDirectoryName);
        var result = Directory.Exists(ferretDir)
            ? DiagnosticCheckResult.Pass()
            : DiagnosticCheckResult.Fail($"Directory not found: {ferretDir}");
        return Task.FromResult(result);
    }
}
