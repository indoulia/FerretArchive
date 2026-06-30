using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector info &lt;id&gt;'.</summary>
internal sealed class ConnectorInfoCommandHandler : ICommandHandler
{
    private readonly IConnectorRegistry _registry;
    private readonly TextConnectorInfoFormatter _formatter;

    /// <summary>Initializes a new instance of the <see cref="ConnectorInfoCommandHandler"/> class.</summary>
    /// <param name="registry">The connector registry to query.</param>
    /// <param name="formatter">The formatter used to render connector details.</param>
    public ConnectorInfoCommandHandler(IConnectorRegistry registry, TextConnectorInfoFormatter formatter)
    {
        _registry = registry;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var id = context.GetOption<string>("id");
        if (string.IsNullOrWhiteSpace(id))
        {
            context.Services.Output.WriteLine("Error: connector ID is required.");
            context.Services.Output.WriteLine("       Run 'ferret connector list' to see available connectors.");
            return Task.FromResult(CommandResult.Failure);
        }

        var descriptor = _registry.GetById(new ConnectorId(id));
        if (descriptor is null)
        {
            context.Services.Output.WriteLine($"Error: connector '{id}' is not registered.");
            context.Services.Output.WriteLine("       Run 'ferret connector list' to see available connectors.");
            return Task.FromResult(CommandResult.Failure);
        }

        _formatter.Format(new ConnectorInfoView(descriptor, IsConfigured: false), context.Services.Output);
        return Task.FromResult(CommandResult.Success);
    }
}
