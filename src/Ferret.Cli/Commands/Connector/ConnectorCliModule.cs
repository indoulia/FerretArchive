using Ferret.Cli.Cli;
using Ferret.Core.Connectors;
using Ferret.Core.Workspace;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Connector;

/// <summary>CLI module for connector management commands.</summary>
internal sealed class ConnectorCliModule : CliModuleBase
{
    private readonly IReadOnlyList<IConnectorFactory> _factories;

    /// <summary>Initializes a new instance of the <see cref="ConnectorCliModule"/> class.</summary>
    /// <param name="factories">Connector factories that will be available in the registry.</param>
    public ConnectorCliModule(IReadOnlyList<IConnectorFactory> factories) => _factories = factories;

    /// <inheritdoc/>
    public override string Name => "ferret.connector";

    /// <inheritdoc/>
    public override string Description => "Connector management and inspection.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(new CommandMetadata("connector", "Connector management and inspection."), HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("list", "List all registered connectors."),
            typeof(ConnectorListCommandHandler),
            Group: "connector");

        yield return new CommandDefinition(
            new CommandMetadata("info", "Show connector details."),
            typeof(ConnectorInfoCommandHandler),
            Group: "connector")
            .WithArgument("id", "Connector ID (e.g. filesystem)");

        yield return new CommandDefinition(
            new CommandMetadata("enable", "Create or enable a connector instance."),
            typeof(ConnectorEnableCommandHandler),
            Group: "connector",
            Options:
            [
                new OptionDefinition("--name", "Instance name (default: \"default\").", typeof(string)),
                new OptionDefinition("--type", "Connector type ID (e.g. filesystem).", typeof(string)),
                new OptionDefinition("--path", "Root path for the connector.", typeof(string)),
                new OptionDefinition("--include", "Include glob patterns.", typeof(string)),
                new OptionDefinition("--exclude", "Exclude glob patterns.", typeof(string)),
            ]);

        yield return new CommandDefinition(
            new CommandMetadata("disable", "Disable a connector instance."),
            typeof(ConnectorDisableCommandHandler),
            Group: "connector",
            Options:
            [
                new OptionDefinition("--name", "Instance name (default: \"default\").", typeof(string)),
            ]);

        yield return new CommandDefinition(
            new CommandMetadata("configure", "Update a connector instance's configuration."),
            typeof(ConnectorConfigureCommandHandler),
            Group: "connector",
            Options:
            [
                new OptionDefinition("--name", "Instance name (default: \"default\").", typeof(string)),
                new OptionDefinition("--path", "Root path for the connector.", typeof(string)),
                new OptionDefinition("--include", "Include glob patterns.", typeof(string)),
                new OptionDefinition("--exclude", "Exclude glob patterns.", typeof(string)),
                new OptionDefinition("--display-name", "Display name for the instance.", typeof(string)),
            ]);

        yield return new CommandDefinition(
            new CommandMetadata("inspect", "Display full configuration for a connector instance."),
            typeof(ConnectorInspectCommandHandler),
            Group: "connector",
            Options:
            [
                new OptionDefinition("--name", "Instance name (default: \"default\").", typeof(string)),
            ]);

        yield return new CommandDefinition(
            new CommandMetadata("validate", "Validate connector instances against the registry."),
            typeof(ConnectorValidateCommandHandler),
            Group: "connector")
            .WithArgument("type", "Optional connector type to validate (e.g. filesystem).", isRequired: false);
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        foreach (var factory in _factories)
        {
            services.AddSingleton<IConnectorFactory>(factory);
        }

        services.AddSingleton<IConnectorRegistry>(sp =>
            ConnectorPlatform.RegistryBuilder.Build(sp.GetServices<IConnectorFactory>()));

        services.AddSingleton<IConnectorInstanceStore, ConnectorPlatform.ConnectorInstanceStore>();

        // Manager — uses IWorkspaceContext.WorkspaceRoot (registered by IndexCliModule in Program.cs).
        services.AddSingleton<IConnectorManager>(sp =>
            ConnectorPlatform.ConnectorPlatformFactory.CreateConnectorManager(
                sp.GetRequiredService<IConnectorInstanceStore>(),
                sp.GetServices<IConnectorFactory>(),
                sp.GetRequiredService<IWorkspaceContext>().WorkspaceRoot));

        services.AddSingleton<TextConnectorListFormatter>();
        services.AddSingleton<TextConnectorInfoFormatter>();
        services.AddSingleton<ConnectorListCommandHandler>();
        services.AddSingleton<ConnectorInfoCommandHandler>();
        services.AddSingleton<ConnectorEnableCommandHandler>();
        services.AddSingleton<ConnectorDisableCommandHandler>();
        services.AddSingleton<ConnectorConfigureCommandHandler>();
        services.AddSingleton<ConnectorInspectCommandHandler>();
        services.AddSingleton<ConnectorValidateCommandHandler>();
    }
}
