using Ferret.Cli.Cli;
using Ferret.Core.Search;

namespace Ferret.Cli.Search;

/// <summary>
/// Handles <c>ferret search &lt;query&gt;</c>.
/// Calls <see cref="ISearchService"/>, builds <see cref="SearchViewModel"/>, and renders via <see cref="SearchRendererSelector"/>.
/// </summary>
internal sealed class SearchCommandHandler : ICommandHandler
{
    private readonly ISearchService _searchService;
    private readonly SearchRendererSelector _renderer;

    /// <summary>Initializes a new instance of the <see cref="SearchCommandHandler"/> class.</summary>
    /// <param name="searchService">The search service to query.</param>
    /// <param name="renderer">The renderer selector used to format search output.</param>
    public SearchCommandHandler(ISearchService searchService, SearchRendererSelector renderer)
    {
        _searchService = searchService;
        _renderer = renderer;
    }

    /// <summary>Executes the search with the given arguments and writes results to context output.</summary>
    /// <param name="args">The parsed command arguments.</param>
    /// <param name="context">The per-invocation context.</param>
    /// <returns>A task resolving to the command result.</returns>
    public async Task<CommandResult> HandleAsync(SearchCommandArgs args, IFerretContext context)
    {
        var options = new SearchOptions
        {
            MaxResults = args.Limit,
            Mode = SearchExecutionMode.Auto,
            IncludePassages = args.Passages,
        };

        var result = await _searchService.SearchAsync(args.Query, options).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var message = result.Status switch
            {
                SearchServiceStatus.InvalidQuery =>
                    $"Invalid query: {(result.Diagnostics.Count > 0 ? result.Diagnostics[0].Message : "empty or whitespace")}",
                SearchServiceStatus.IndexNotFound =>
                    "No search index found. Run 'ferret index' first.",
                SearchServiceStatus.WorkspaceNotFound =>
                    "No workspace found. Run 'ferret workspace init' first.",
                SearchServiceStatus.ProviderUnavailable =>
                    "No search provider is available for this query.",
                _ => $"Search failed: {result.Status}",
            };

            context.Services.Output.WriteError(message);
            return CommandResult.Failure;
        }

        var selector = args.NoHighlight
            ? new SearchRendererSelector(new NullTextStyler())
            : _renderer;

        var viewModel = new SearchViewModel
        {
            OriginalQuery = args.Query,

            // Sprint 10: BM25 provider only produces FileSearchHit; other hit types expand in future sprints.
            Hits = result.Hits.OfType<FileSearchHit>().ToList(),
            ExecutionInfo = result.ExecutionInfo!,
        };

        var output = selector.Render(viewModel, args.Format);
        context.Services.Output.WriteLine(output);

        return CommandResult.Success;
    }

    /// <inheritdoc/>
    public Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var args = new SearchCommandArgs
        {
            Query = context.GetOption<string>("query") ?? string.Empty,
            Limit = int.TryParse(context.GetOption<string>("limit"), out var lim) ? lim : 20,
            Passages = context.GetOption<bool?>("passages") ?? false,
            NoHighlight = context.GetOption<bool?>("no-highlight") ?? false,
            Format = Enum.TryParse<SearchOutputFormat>(
                context.GetOption<string>("format"), ignoreCase: true, out var fmt)
                ? fmt
                : SearchOutputFormat.Text,
        };
        return HandleAsync(args, context);
    }
}
