using Ferret.Cli.Cli;
using Ferret.Mcp.Protocol;

namespace Ferret.Cli.Commands.Serve;

/// <summary>Runs the Ferret MCP stdio server until cancelled.</summary>
internal sealed class ServeCommandHandler : ICommandHandler
{
    private readonly IMcpRuntime _runtime;

    /// <summary>Initializes a new instance of the <see cref="ServeCommandHandler"/> class.</summary>
    /// <param name="runtime">The MCP runtime to run.</param>
    public ServeCommandHandler(IMcpRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        try
        {
            await _runtime.RunAsync(context.CancellationToken).ConfigureAwait(false);
            return CommandResult.Success;
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Cancelled;
        }
    }
}
