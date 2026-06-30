using Ferret.Core.Context;
using Ferret.Core.Search;

using Microsoft.Extensions.Logging;

namespace Ferret.AI.Context;

/// <summary>
/// Implements the context assembly pipeline:
///   1. Search — call ISearchService with the query.
///   2. Deduplicate — remove repeated DocumentIds (first occurrence wins).
///   3. Expand — fetch full Document for each unique hit.
///   4. Filter — remove empty, too-small, and content-duplicate documents.
///   5. Sort — order by descending score.
///   6. Budget — add documents until MaxTokens or MaxDocuments is reached.
///   7. Package — wrap results in a ContextPackage.
/// </summary>
public sealed class ContextAssembler : IContextAssembler
{
    private readonly ISearchService _searchService;
    private readonly DocumentExpander _expander;
    private readonly ILogger<ContextAssembler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextAssembler"/> class.
    /// </summary>
    /// <param name="searchService">The search service for querying the index.</param>
    /// <param name="expander">The document expander for fetching full documents.</param>
    /// <param name="logger">The logger instance.</param>
    public ContextAssembler(
        ISearchService searchService,
        DocumentExpander expander,
        ILogger<ContextAssembler> logger)
    {
        ArgumentNullException.ThrowIfNull(searchService);
        ArgumentNullException.ThrowIfNull(expander);
        ArgumentNullException.ThrowIfNull(logger);
        _searchService = searchService;
        _expander = expander;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ContextPackage> AssembleAsync(ContextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Step 1: Search
        var options = new SearchOptions { MaxResults = request.MaxDocuments * 2 };
        var searchResult = await _searchService.SearchAsync(request.Query, options).ConfigureAwait(false);

        var allHits = searchResult.IsSuccess ? searchResult.Hits : (IReadOnlyList<SearchHit>)[];
        var documentsConsidered = allHits.Count;

        Log.HitsFound(_logger, allHits.Count, request.Query);

        // Step 2: Deduplicate
        var uniqueHits = ContextDeduplicator.Deduplicate(allHits);

        // Step 3: Expand
        var documents = await _expander.ExpandAsync(uniqueHits, ct).ConfigureAwait(false);

        // Step 4: Filter — remove empty, too-small, and content-duplicate documents
        var filtered = ContentFilter.Filter(documents);
        Log.FilterPassed(_logger, filtered.Count, documents.Count);

        // Build lookup dictionaries from hits (keyed by DocumentId value)
        var scoreByDocId = uniqueHits
            .ToDictionary(h => h.DocumentId.Value, h => h.Score, StringComparer.Ordinal);
        var hitByDocId = uniqueHits
            .ToDictionary(h => h.DocumentId.Value, h => h, StringComparer.Ordinal);

        // Step 5: Sort filtered documents by descending score
        var sorted = filtered
            .Select(doc => (doc, score: scoreByDocId.TryGetValue(doc.Id.Value, out var s) ? s : 0f))
            .OrderByDescending(x => x.score)
            .ToList();

        // Step 6: Apply token budget and document count limit
        var included = new List<ContextDocument>(request.MaxDocuments);
        var totalTokens = 0;

        foreach (var (doc, score) in sorted)
        {
            if (included.Count >= request.MaxDocuments)
            {
                break;
            }

            var content = doc.PlainText;
            var tokenEstimate = TokenEstimator.Estimate(content);

            if (totalTokens + tokenEstimate > request.MaxTokens && included.Count > 0)
            {
                break;
            }

            hitByDocId.TryGetValue(doc.Id.Value, out var srcHit);
            included.Add(new ContextDocument
            {
                DocumentId = doc.Id,
                CanonicalUri = srcHit?.CanonicalUri ?? new Uri($"file:///{doc.Id.Value}"),
                DisplayName = srcHit?.DisplayName ?? doc.Id.Value,
                Title = doc.Title,
                Content = content,
                Score = score,
                TokenEstimate = tokenEstimate,
                Source = ContextDocumentSource.FullDocument,
            });

            totalTokens += tokenEstimate;
        }

        // Step 7: Package
        return new ContextPackage
        {
            Query = request.Query,
            Documents = included,
            TotalTokenEstimate = totalTokens,
            DocumentsConsidered = documentsConsidered,
            DocumentsIncluded = included.Count,
            AssembledAt = DateTimeOffset.UtcNow,
        };
    }

    private static class Log
    {
        private static readonly Action<ILogger, int, string, Exception?> HitsFoundHandler =
            LoggerMessage.Define<int, string>(
                LogLevel.Debug,
                new EventId(1, nameof(HitsFound)),
                "Context assembly: {HitCount} hits for query '{Query}'");

        private static readonly Action<ILogger, int, int, Exception?> FilterPassedHandler =
            LoggerMessage.Define<int, int>(
                LogLevel.Debug,
                new EventId(2, nameof(FilterPassed)),
                "Context assembly: {FilteredCount}/{ExpandedCount} documents passed content filter");

        public static void HitsFound(ILogger logger, int hitCount, string query) =>
            HitsFoundHandler(logger, hitCount, query, null);

        public static void FilterPassed(ILogger logger, int filteredCount, int expandedCount) =>
            FilterPassedHandler(logger, filteredCount, expandedCount, null);
    }
}
