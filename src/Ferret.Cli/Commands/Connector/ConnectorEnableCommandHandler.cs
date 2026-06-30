using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector enable' — creates or enables a connector instance.</summary>
internal sealed class ConnectorEnableCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorEnableCommandHandler"/> class.</summary>
    /// <param name="store">The connector instance store.</param>
    public ConnectorEnableCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var type = context.GetOption<string>("type") ?? string.Empty;
        var path = context.GetOption<string>("path");
        var include = context.GetOption<string>("include");
        var exclude = context.GetOption<string>("exclude");

        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = (await _store.LoadAllAsync(rootPath, context.CancellationToken).ConfigureAwait(false))
            .ToList();

        var existing = instances.Find(i => i.Id.Value == name);

        if (existing is not null)
        {
            if (existing.IsEnabled)
            {
                context.Services.Output.WriteLine($"Connector '{name}' is already enabled.");
                return CommandResult.Success;
            }

            var updated = existing with { IsEnabled = true };
            var updatedList = instances.Select(i => i.Id.Value == name ? updated : i).ToList();
            await _store.SaveAsync(rootPath, updatedList, context.CancellationToken).ConfigureAwait(false);
            context.Services.Output.WriteSuccess($"Connector '{name}' re-enabled.");
            return CommandResult.Success;
        }

        var config = ConnectorConfiguration.Empty;
        if (path is not null)
        {
            config = config.With("rootPath", path);
        }

        if (include is not null)
        {
            config = config.With("include", include);
        }

        if (exclude is not null)
        {
            config = config.With("exclude", exclude);
        }

        var newInstance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId(name),
            ConnectorType = new ConnectorId(type),
            DisplayName = name,
            IsEnabled = true,
            Configuration = config,
        };

        instances.Add(newInstance);
        await _store.SaveAsync(rootPath, instances, context.CancellationToken).ConfigureAwait(false);
        context.Services.Output.WriteSuccess($"Connector '{name}' enabled.");
        return CommandResult.Success;
    }
}
