using Ferret.Cli.Cli;
using Ferret.Manual;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Manual;

/// <summary>Registers the <c>ferret manual</c> command.</summary>
internal sealed class ManualCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.manual";

    /// <inheritdoc/>
    public override string Description => "Open The Ferret Manual in your browser";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("manual", "Open The Ferret Manual in your browser"),
            typeof(ManualCliCommandHandler),
            Options:
            [
                new OptionDefinition("--port", "Port for the manual server", typeof(int), DefaultValue: 7070),
            ]);
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ManualCommandHandler>();
        services.AddSingleton<ManualCliCommandHandler>();
    }
}
