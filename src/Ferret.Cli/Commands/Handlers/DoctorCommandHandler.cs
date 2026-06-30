using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Discovers all IDiagnosticCheck instances from registered modules and runs them.
///      Adding a new module automatically extends doctor — this handler never changes.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class DoctorCommandHandler : ICommandHandler
{
    private readonly IReadOnlyList<IDiagnosticCheck> _checks;

    internal DoctorCommandHandler(IEnumerable<IDiagnosticCheck> checks)
    {
        _checks = checks.ToList();
    }

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine("Ferret Doctor");
        context.Services.Output.WriteLine();

        bool healthy = await DiagnosticRunner.RunAsync(_checks, context).ConfigureAwait(false);

        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine(healthy
            ? "Ferret is healthy."
            : "Ferret has issues. Review the checks above.");

        return healthy ? CommandResult.Success : CommandResult.Failure;
    }
}
