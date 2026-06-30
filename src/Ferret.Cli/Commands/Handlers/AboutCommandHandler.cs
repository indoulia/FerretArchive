using Ferret.Cli.Cli;
using Ferret.Cli.Infrastructure;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: About command surfaces product identity and tagline — first stop for new users.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class AboutCommandHandler : ICommandHandler
{
    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        context.Services.Output.WriteLine("Ferret");
        context.Services.Output.WriteLine("Dig Deep. Deliver Context.");
        context.Services.Output.WriteLine(VersionCommandHandler.PoweredBy);
        context.Services.Output.WriteLine();
        context.Services.Output.WriteLine($"Version: {FerretPlatform.Version}");
        context.Services.Output.WriteLine($"Runtime: {FerretPlatform.RuntimeInfo}");
        return Task.FromResult(CommandResult.Success);
    }
}
