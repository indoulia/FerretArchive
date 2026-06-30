using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector configure' — patch-updates a connector instance's configuration.</summary>
internal sealed class ConnectorConfigureCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorConfigureCommandHandler"/> class.</summary>
    /// <param name="store">The connector instance store.</param>
    public ConnectorConfigureCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var path = context.GetOption<string>("path");
        var exclude = context.GetOption<string>("exclude");
        var include = context.GetOption<string>("include");
        var displayName = context.GetOption<string>("display-name");

        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = (await _store.LoadAllAsync(rootPath, context.CancellationToken).ConfigureAwait(false))
            .ToList();

        var existing = instances.Find(i => i.Id.Value == name);

        if (existing is null)
        {
            context.Services.Output.WriteError($"Connector '{name}' not found.");
            return CommandResult.Failure;
        }

        var config = existing.Configuration;
        if (path is not null)
        {
            config = config.With("rootPath", path);
        }

        if (exclude is not null)
        {
            config = config.With("exclude", exclude);
        }

        if (include is not null)
        {
            config = config.With("include", include);
        }

        var updated = existing with
        {
            DisplayName = displayName ?? existing.DisplayName,
            Configuration = config,
        };

        var updatedList = instances.Select(i => i.Id.Value == name ? updated : i).ToList();
        await _store.SaveAsync(rootPath, updatedList, context.CancellationToken).ConfigureAwait(false);
        context.Services.Output.WriteSuccess($"Connector '{name}' updated.");
        return CommandResult.Success;
    }
}
