using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Discovers all IDiagnosticCheck instances from registered modules and runs them, then prints
///      the informational Parser Platform report. Adding a new module automatically extends doctor.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class DoctorCommandHandler : ICommandHandler
{
    private readonly IReadOnlyList<IDiagnosticCheck> _checks;
    private readonly ParserPlatformReport _parserReport;

    internal DoctorCommandHandler(IEnumerable<IDiagnosticCheck> checks, ParserPlatformReport parserReport)
    {
        _checks = checks.ToList();
        _parserReport = parserReport;
    }

    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine("Ferret Doctor");
        context.Services.Output.WriteLine();

        bool healthy = await DiagnosticRunner.RunAsync(_checks, context).ConfigureAwait(false);

        _parserReport.Render(context.Services.Output, context.Verbosity == VerbosityLevel.Verbose);

        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine(healthy
            ? "Ferret is healthy."
            : "Ferret has issues. Review the checks above.");

        return healthy ? CommandResult.Success : CommandResult.Failure;
    }
}
