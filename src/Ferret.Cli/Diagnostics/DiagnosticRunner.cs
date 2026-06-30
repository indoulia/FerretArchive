using Ferret.Cli.Cli;

namespace Ferret.Cli.Diagnostics;

/// <summary>
/// Why: Runs an ordered check list, reports via IOutputFormatter, returns overall pass/fail.
///      Isolated from DoctorCommandHandler so the check list is injectable in tests.
/// Thread Safety: Single Thread Only.
/// </summary>
internal static class DiagnosticRunner
{
    /// <summary>Runs a list of diagnostic checks and reports results.</summary>
    /// <param name="checks">The checks to run in order.</param>
    /// <param name="context">The per-invocation context.</param>
    /// <returns>True if all checks passed; false otherwise.</returns>
    internal static async Task<bool> RunAsync(IReadOnlyList<IDiagnosticCheck> checks, IFerretContext context)
    {
        bool allPassed = true;
        foreach (IDiagnosticCheck check in checks)
        {
            DiagnosticCheckResult result;
            try
            {
                result = await check.RunAsync(context, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                result = DiagnosticCheckResult.Fail(ex.Message);
            }

            if (result.Passed)
            {
                context.Services.Output.WriteSuccess(check.Name);
            }
            else
            {
                string detail = result.FailureReason is not null ? $": {result.FailureReason}" : string.Empty;
                context.Services.Output.WriteError($"{check.Name}{detail}");
                allPassed = false;
            }
        }

        return allPassed;
    }
}
