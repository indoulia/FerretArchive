using Ferret.Cli.Cli;

namespace Ferret.Cli.Diagnostics;

/// <summary>Why: Stub interface for Task 4 diagnostic checks. Task 4 adds the runner and concrete checks.</summary>
internal interface IDiagnosticCheck
{
    /// <summary>Gets the check name.</summary>
    string Name { get; }

    /// <summary>Runs the check and returns a result.</summary>
    /// <param name="context">The per-invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task resolving to the check result.</returns>
    Task<DiagnosticCheckResult> RunAsync(IFerretContext context, CancellationToken cancellationToken);
}
