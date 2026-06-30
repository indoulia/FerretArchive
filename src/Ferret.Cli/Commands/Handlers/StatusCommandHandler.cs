using Ferret.Cli.Cli;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Operator visibility into runtime state. Sprint 7: named-pipe IPC health query.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class StatusCommandHandler : ICommandHandler
{
    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        // Sprint 6: no IPC. Sprint 7 adds named-pipe health endpoint.
        context.Services.Output.WriteLine("Ferret is not running (start with: ferret start)");
        return Task.FromResult(CommandResult.Failure);
    }
}
