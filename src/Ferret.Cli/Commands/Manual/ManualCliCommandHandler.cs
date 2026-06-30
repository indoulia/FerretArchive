using Ferret.Cli.Cli;
using Ferret.Manual;

namespace Ferret.Cli.Commands.Manual;

/// <summary>Bridges <c>ferret manual</c> CLI invocation to <see cref="ManualCommandHandler"/>.</summary>
internal sealed class ManualCliCommandHandler : ICommandHandler
{
    private readonly ManualCommandHandler _handler;

    public ManualCliCommandHandler(ManualCommandHandler handler)
    {
        _handler = handler;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var port = context.GetOption<int>("port");
        var exitCode = await _handler.HandleAsync(port, context.CancellationToken).ConfigureAwait(false);
        return exitCode == 0 ? CommandResult.Success : CommandResult.Failure;
    }
}
