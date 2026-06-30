using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Handles 'ferret connector validate' — validates all connector instances against the registry.</summary>
internal sealed class ConnectorValidateCommandHandler : ICommandHandler
{
    private readonly IConnectorInstanceStore _store;
    private readonly IConnectorRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="ConnectorValidateCommandHandler"/> class.</summary>
    /// <param name="store">The connector instance store.</param>
    /// <param name="registry">The connector registry used to check registrations.</param>
    public ConnectorValidateCommandHandler(IConnectorInstanceStore store, IConnectorRegistry registry)
    {
        _store = store;
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var rootPath = WorkspacePath.Create(context.WorkingDirectory);

        var type = context.GetOption<string>("type");
        if (!string.IsNullOrWhiteSpace(type) && !_registry.IsRegistered(new ConnectorId(type)))
        {
            context.Services.Output.WriteError($"Connector type '{type}' is not registered.");
            return CommandResult.Failure;
        }

        var result = await ValidateAsync(rootPath, context.CancellationToken).ConfigureAwait(false);

        if (result.IsValid)
        {
            context.Services.Output.WriteSuccess("All connector instances are valid.");
        }
        else
        {
            context.Services.Output.WriteError("Connector validation failed:");
            foreach (var issue in result.Issues)
            {
                var prefix = issue.InstanceId is not null ? $"[{issue.InstanceId}] " : string.Empty;
                context.Services.Output.WriteLine($"  {prefix}{issue.Message}");
            }
        }

        return result.IsValid ? CommandResult.Success : CommandResult.Failure;
    }

    /// <summary>Validates all connector instances for the given workspace path.</summary>
    /// <param name="rootPath">The workspace root path.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ValidationResult"/> representing the aggregate validation outcome.</returns>
    public async Task<ValidationResult> ValidateAsync(WorkspacePath rootPath, CancellationToken ct = default)
    {
        var instances = await _store.LoadAllAsync(rootPath, ct).ConfigureAwait(false);

        var results = instances
            .Select(i => _registry.IsRegistered(i.ConnectorType)
                ? ValidationResult.Ok()
                : ValidationResult.WithError(
                    $"Connector type '{i.ConnectorType.Value}' is not registered.",
                    instanceId: i.Id.Value))
            .ToList();

        return ValidationResult.Combine(results);
    }
}
