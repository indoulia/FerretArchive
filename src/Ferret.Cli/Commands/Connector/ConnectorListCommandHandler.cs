using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector list'.</summary>
internal sealed class ConnectorListCommandHandler : ICommandHandler
{
    private readonly IConnectorRegistry _registry;
    private readonly TextConnectorListFormatter _formatter;

    /// <summary>Initializes a new instance of the <see cref="ConnectorListCommandHandler"/> class.</summary>
    /// <param name="registry">The connector registry to query.</param>
    /// <param name="formatter">The formatter used to render the list.</param>
    public ConnectorListCommandHandler(IConnectorRegistry registry, TextConnectorListFormatter formatter)
    {
        _registry = registry;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var items = _registry.GetAll()
            .Select(d => new ConnectorListItem(
                Id: d.Id.Value,
                Name: d.Metadata.Name,
                Version: d.Metadata.Version,
                PrimaryCapability: d.Capabilities.Count > 0 ? d.Capabilities[0].Name : "(none)",
                IsConfigured: false))
            .ToList();

        _formatter.Format(new ConnectorListResult(items), context.Services.Output);
        return Task.FromResult(CommandResult.Success);
    }
}
