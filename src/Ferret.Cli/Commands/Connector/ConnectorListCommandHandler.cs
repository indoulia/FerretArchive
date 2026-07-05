using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector list'.</summary>
internal sealed class ConnectorListCommandHandler : ICommandHandler
{
    private readonly IConnectorRegistry _registry;
    private readonly IConnectorInstanceStore _store;
    private readonly TextConnectorListFormatter _formatter;

    /// <summary>Initializes a new instance of the <see cref="ConnectorListCommandHandler"/> class.</summary>
    /// <param name="registry">The connector registry to query.</param>
    /// <param name="store">The connector instance store, used to determine which types have a configured instance.</param>
    /// <param name="formatter">The formatter used to render the list.</param>
    public ConnectorListCommandHandler(IConnectorRegistry registry, IConnectorInstanceStore store, TextConnectorListFormatter formatter)
    {
        _registry = registry;
        _store = store;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = await _store.LoadAllAsync(rootPath, context.CancellationToken).ConfigureAwait(false);
        var configuredTypes = instances.Select(i => i.ConnectorType).ToHashSet();

        var items = _registry.GetAll()
            .Select(d => new ConnectorListItem(
                Id: d.Id.Value,
                Name: d.Metadata.Name,
                Version: d.Metadata.Version,
                PrimaryCapability: d.Capabilities.Count > 0 ? d.Capabilities[0].Name : "(none)",
                IsConfigured: configuredTypes.Contains(d.Id)))
            .ToList();

        _formatter.Format(new ConnectorListResult(items), context.Services.Output);
        return CommandResult.Success;
    }
}
