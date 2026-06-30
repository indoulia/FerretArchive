using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector inspect' — displays full configuration for a connector instance.</summary>
internal sealed class ConnectorInspectCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;

    /// <summary>Initializes a new instance of the <see cref="ConnectorInspectCommandHandler"/> class.</summary>
    /// <param name="store">The connector instance store.</param>
    public ConnectorInspectCommandHandler(IConnectorInstanceStore store) => _store = store;

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var name = context.GetOption<string>("name") ?? "default";
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);
        var instances = await _store.LoadAllAsync(rootPath, context.CancellationToken).ConfigureAwait(false);

        var existing = instances.FirstOrDefault(i => i.Id.Value == name);

        if (existing is null)
        {
            context.Services.Output.WriteError($"Connector '{name}' not found.");
            return CommandResult.Failure;
        }

        context.Services.Output.WriteLine($"Id:            {existing.Id.Value}");
        context.Services.Output.WriteLine($"ConnectorType: {existing.ConnectorType.Value}");
        context.Services.Output.WriteLine($"DisplayName:   {existing.DisplayName}");
        context.Services.Output.WriteLine($"IsEnabled:     {existing.IsEnabled}");
        context.Services.Output.WriteLine($"SchemaVersion: {existing.SchemaVersion}");

        if (existing.Tags.Count > 0)
        {
            context.Services.Output.WriteLine($"Tags:          {string.Join(", ", existing.Tags)}");
        }

        var config = existing.Configuration.AsReadOnlyDictionary();
        if (config.Count > 0)
        {
            context.Services.Output.WriteLine("Configuration:");
            foreach (var (key, value) in config)
            {
                context.Services.Output.WriteLine($"  {key}: {value}");
            }
        }

        return CommandResult.Success;
    }
}
