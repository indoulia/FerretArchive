using Ferret.Cli.Cli;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Config;

/// <summary>Contributes config subcommands to the Ferret CLI.</summary>
internal sealed class ConfigCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.config";

    /// <inheritdoc/>
    public override string Description => "Configuration management.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("config", "Manage Ferret configuration."),
            HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("validate", "Validate ferret.json and report errors."),
            typeof(ConfigValidateCommandHandler),
            Group: "config",
            Options:
            [
                new OptionDefinition("--config", "Path to ferret.json.", typeof(string)),
            ]);
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ConfigValidateCommandHandler>();
    }
}
