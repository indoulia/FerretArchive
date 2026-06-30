using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector disable' — disables a named connector instance.</summary>
internal sealed class ConnectorDisableCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorDisableCommandHandler"/> class.</summary>
    /// <param name="store">The connector instance store.</param>
    public ConnectorDisableCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = (await _store.LoadAllAsync(rootPath, context.CancellationToken).ConfigureAwait(false))
            .ToList();

        var existing = instances.Find(i => i.Id.Value == name);

        if (existing is null)
        {
            context.Services.Output.WriteError($"Connector '{name}' not found.");
            return CommandResult.Failure;
        }

        if (!existing.IsEnabled)
        {
            context.Services.Output.WriteLine($"Connector '{name}' is already disabled.");
            return CommandResult.Success;
        }

        var updated = existing with { IsEnabled = false };
        var updatedList = instances.Select(i => i.Id.Value == name ? updated : i).ToList();
        await _store.SaveAsync(rootPath, updatedList, context.CancellationToken).ConfigureAwait(false);
        context.Services.Output.WriteSuccess($"Connector '{name}' disabled.");
        return CommandResult.Success;
    }
}
