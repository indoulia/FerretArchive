using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;

namespace Ferret.Cli.Tests.Diagnostics;

internal sealed class PassCheck : IDiagnosticCheck
{
    public string Name => "Always passes";

    public Task<DiagnosticCheckResult> RunAsync(IFerretContext ctx, CancellationToken ct) =>
        Task.FromResult(DiagnosticCheckResult.Pass());
}
