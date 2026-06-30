using Ferret.Cli.Cli;
using Ferret.Cli.Infrastructure;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: First command a new user runs; surfaces version + runtime info for bug reports.
///      Version comes from FerretPlatform so it cannot drift from the git tag.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class VersionCommandHandler : ICommandHandler
{
    internal const string PoweredBy = "Powered by ContextOS";

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine($"Ferret {FerretPlatform.Version}");
        context.Services.Output.WriteLine(PoweredBy);
        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine($"Runtime: {FerretPlatform.RuntimeInfo}");
        return Task.FromResult(CommandResult.Success);
    }
}
