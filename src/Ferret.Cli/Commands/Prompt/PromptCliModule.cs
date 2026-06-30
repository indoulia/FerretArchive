using Ferret.Cli.Cli;
using Ferret.Prompts;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Prompt;

/// <summary>Registers the <c>ferret prompt</c> command group and subcommands.</summary>
internal sealed class PromptCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.prompt";

    /// <inheritdoc/>
    public override string Description => "Prompt template registry commands.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("prompt", "Prompt template registry commands."),
            HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("list", "List all registered prompt templates."),
            typeof(PromptListCommandHandler),
            Group: "prompt");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        PromptsModule.ConfigureServices(services);
        services.AddSingleton<PromptListCommandHandler>();
    }
}
