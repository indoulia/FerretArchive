using Ferret.AI;
using Ferret.Cli.Cli;
using Ferret.Core.Search;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Commands.Context;

/// <summary>CLI module for the <c>ferret context &lt;query&gt;</c> command.
/// Registers <see cref="Ferret.Core.Context.IContextAssembler"/> and its dependencies.</summary>
internal sealed class ContextCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.context";

    /// <inheritdoc/>
    public override string Description => "Assemble context from the workspace for a query.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("context", "Assemble token-budgeted context from the workspace for a query."),
            typeof(ContextAssembleCommandHandler),
            Options:
            [
                new OptionDefinition("--max-tokens", "Token budget for assembled context (default: 8000).", typeof(int)),
                new OptionDefinition("--max-documents", "Maximum documents to include (default: 10).", typeof(int)),
            ])
            .WithArgument("query", "Search query to assemble context for");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Search services (required by ContextAssembler via ISearchService)
        services.AddSingleton<IQueryParser, QueryParser>();
        services.AddSingleton<ISearchProvider, Bm25SearchProvider>();
        services.AddSingleton<ISearchService, SearchService>();

        // Context assembly services
        AiModule.ConfigureServices(services);

        services.AddSingleton<ContextAssembleCommandHandler>();
    }
}
