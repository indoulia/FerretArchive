using System.Diagnostics;

using Ferret.Core.Search;

namespace Ferret.Search;

/// <summary>
/// Orchestrates search across registered <see cref="ISearchProvider"/> implementations.
/// Parses raw strings, selects a capable provider, applies post-processors, and returns <see cref="SearchServiceResult"/>.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly IQueryParser _queryParser;
    private readonly IEnumerable<ISearchProvider> _providers;
    private readonly IEnumerable<ISearchPostProcessor> _postProcessors;

    /// <summary>Initializes a new instance of the <see cref="SearchService"/> class.</summary>
    /// <param name="queryParser">The query parser for raw input.</param>
    /// <param name="providers">The registered search providers.</param>
    /// <param name="postProcessors">The registered post-processors.</param>
    public SearchService(
        IQueryParser queryParser,
        IEnumerable<ISearchProvider> providers,
        IEnumerable<ISearchPostProcessor> postProcessors)
    {
        _queryParser = queryParser;
        _providers = providers;
        _postProcessors = postProcessors;
    }

    /// <inheritdoc/>
    public async Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var parseResult = _queryParser.Parse(rawQuery);

        if (!parseResult.IsSuccess)
        {
            var stubQuery = new SearchQuery
            {
                OriginalText = rawQuery,
                Root = new KeywordExpression(string.Empty),
            };
            return SearchServiceResult.Failure(stubQuery, SearchServiceStatus.InvalidQuery, parseResult.Diagnostics);
        }

        return await SearchAsync(parseResult.Query!, options).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        var provider = _providers.FirstOrDefault(p => IsCapable(p, options));

        if (provider is null)
        {
            return SearchServiceResult.Failure(query, SearchServiceStatus.ProviderUnavailable, []);
        }

        var stopwatch = Stopwatch.StartNew();
        var providerResult = await provider.SearchAsync(query, options, options.Token).ConfigureAwait(false);
        stopwatch.Stop();

        if (!providerResult.IsSuccess)
        {
            return SearchServiceResult.Failure(query, providerResult.Status, []);
        }

        var hits = await ApplyPostProcessorsAsync(providerResult.Hits, query, options).ConfigureAwait(false);

        var searchResult = new SearchResult
        {
            Hits = hits,
            TotalHits = providerResult.DocumentsScanned,
            ReturnedHits = hits.Count,
        };

        var executionInfo = new SearchExecutionInfo
        {
            SessionId = Guid.NewGuid(),
            ProviderName = provider.Descriptor.Id,
            Duration = stopwatch.Elapsed,
            DocumentsScanned = providerResult.DocumentsScanned,
            IndexVersion = providerResult.IndexVersion,
        };

        return SearchServiceResult.Success(query, searchResult, executionInfo, provider.Descriptor);
    }

    private static bool IsCapable(ISearchProvider provider, SearchOptions options) =>
        options.Mode switch
        {
            SearchExecutionMode.Keyword => provider.Capabilities.SupportsKeyword,
            SearchExecutionMode.Semantic => provider.Capabilities.SupportsSemantic,
            SearchExecutionMode.Hybrid => provider.Capabilities.SupportsHybrid,
            SearchExecutionMode.Auto => provider.Capabilities.SupportsKeyword || provider.Capabilities.SupportsSemantic,
            _ => false,
        };

    private async Task<IReadOnlyList<SearchHit>> ApplyPostProcessorsAsync(
        IReadOnlyList<SearchHit> hits, SearchQuery query, SearchOptions options)
    {
        var current = hits;

        foreach (var postProcessor in _postProcessors)
        {
            current = await postProcessor.ProcessAsync(current, query, options).ConfigureAwait(false);
        }

        return current;
    }
}
