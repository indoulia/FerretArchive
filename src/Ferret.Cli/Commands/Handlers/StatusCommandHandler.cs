using System.Globalization;

using Ferret.Cli.Cli;

namespace Ferret.Cli.Commands.Handlers;

/// <summary>
/// Why: Operator visibility into runtime state, via the on-disk marker <see cref="RuntimeStatusFile"/>
///      that 'ferret start' writes -- this process cannot see another process's in-memory RuntimeState.
/// Thread Safety: Single Thread Only.
/// </summary>
internal sealed class StatusCommandHandler : ICommandHandler
{
    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var path = RuntimeStatusFile.ResolvePath(context.WorkingDirectory);
        var record = RuntimeStatusFile.TryRead(path);

        if (record is not null && RuntimeStatusFile.IsProcessAlive(record.ProcessId))
        {
            context.Services.Output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Ferret is running (PID {record.ProcessId}, started {record.StartedAtUtc:u})."));
            return Task.FromResult(CommandResult.Success);
        }

        if (record is not null)
        {
            // Stale marker from a process that crashed or was killed without cleanup.
            RuntimeStatusFile.Delete(path);
        }

        context.Services.Output.WriteLine("Ferret is not running (start with: ferret start)");
        return Task.FromResult(CommandResult.Failure);
    }
}
