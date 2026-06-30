using Ferret.Cli.Cli;
using Ferret.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Models;

/// <summary>Registers the <c>ferret models</c> command group and subcommands.</summary>
internal sealed class ModelsCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.models";

    /// <inheritdoc/>
    public override string Description => "AI model registry commands.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("models", "AI model registry commands."),
            HandlerType: null);

        yield return new CommandDefinition(
            new CommandMetadata("list", "List all registered AI models."),
            typeof(ModelsListCommandHandler),
            Group: "models");

        yield return new CommandDefinition(
            new CommandMetadata("info", "Show detail for a specific AI model."),
            typeof(ModelsInfoCommandHandler),
            Group: "models")
            .WithArgument("model-id", "The model identifier (e.g. ollama/llama3.2).");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        ModelPlatformModule.ConfigureServices(services);
        services.AddSingleton<ModelsListCommandHandler>();
        services.AddSingleton<ModelsInfoCommandHandler>();
    }
}
