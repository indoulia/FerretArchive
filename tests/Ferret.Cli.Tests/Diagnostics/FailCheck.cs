using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;

namespace Ferret.Cli.Tests.Diagnostics;

internal sealed class FailCheck : IDiagnosticCheck
{
    public string Name => "Always fails";

    public Task<DiagnosticCheckResult> RunAsync(IFerretContext ctx, CancellationToken ct) =>
        Task.FromResult(DiagnosticCheckResult.Fail("intentional"));
}
