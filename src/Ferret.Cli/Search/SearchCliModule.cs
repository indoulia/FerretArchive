using Ferret.Cli.Cli;
using Ferret.Core.Search;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Cli.Search;

/// <summary>
/// Registers the <c>ferret search &lt;query&gt;</c> command and all required services.
/// Sprint 10: keyword search only. No post-processors registered.
/// </summary>
internal sealed class SearchCliModule : CliModuleBase
{
    /// <inheritdoc/>
    public override string Name => "ferret.search";

    /// <inheritdoc/>
    public override string Description => "Keyword search over the workspace index.";

    /// <inheritdoc/>
    public override IEnumerable<CommandDefinition> GetCommands()
    {
        yield return new CommandDefinition(
            new CommandMetadata("search", "Search the workspace index for files matching a query."),
            typeof(SearchCommandHandler),
            Options:
            [
                new OptionDefinition("--limit", "Maximum results to return.", typeof(int), DefaultValue: 20),
                new OptionDefinition("--passages", "Return passage-level results instead of files.", typeof(bool)),
                new OptionDefinition("--no-highlight", "Disable ANSI highlighting.", typeof(bool)),
                new OptionDefinition("--format", "Output format: text (default) or json.", typeof(string), DefaultValue: "text"),
            ])
            .WithArgument("query", "Search query (keywords, \"phrase\", prefix*)");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        // Query parsing
        services.AddSingleton<IQueryParser, QueryParser>();

        // Search providers (IEnumerable<ISearchProvider> resolved by SearchService constructor)
        services.AddSingleton<ISearchProvider, Bm25SearchProvider>();

        // Search service (uses IEnumerable<ISearchProvider> and IEnumerable<ISearchPostProcessor>)
        services.AddSingleton<ISearchService, SearchService>();

        // Rendering — AnsiTextStyler is the default; handler overrides to NullTextStyler when --no-highlight
        services.AddSingleton<SearchRendererSelector>(_ => new SearchRendererSelector(new AnsiTextStyler()));

        // Handler
        services.AddSingleton<SearchCommandHandler>();
    }
}
