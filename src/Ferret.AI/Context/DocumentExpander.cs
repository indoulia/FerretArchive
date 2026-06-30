using Ferret.Core.Documents;
using Ferret.Core.Search;

using Microsoft.Extensions.Logging;

namespace Ferret.AI.Context;

/// <summary>
/// Resolves <see cref="SearchHit"/> instances to full <see cref="Document"/> objects
/// via <see cref="IDocumentService"/>. Fetches in parallel (max 5 concurrent).
/// Missing documents are logged at Warning level and excluded from the result.
/// </summary>
public sealed class DocumentExpander
{
    private const int MaxConcurrency = 5;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentExpander> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExpander"/> class.
    /// </summary>
    /// <param name="documentService">The document service for fetching documents.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentExpander(IDocumentService documentService, ILogger<DocumentExpander> logger)
    {
        ArgumentNullException.ThrowIfNull(documentService);
        ArgumentNullException.ThrowIfNull(logger);
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the full document for each hit in parallel.
    /// Hits whose documents cannot be found are silently excluded from the result.
    /// </summary>
    /// <param name="hits">Search hits to expand.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full documents for every hit that was found in the document store.</returns>
    public async Task<IReadOnlyList<Document>> ExpandAsync(
        IReadOnlyList<SearchHit> hits, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(hits);

        if (hits.Count == 0)
        {
            return [];
        }

        var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        try
        {
            var tasks = hits.Select(hit => FetchOneAsync(hit, semaphore, ct)).ToArray();
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.Where(d => d is not null).Select(d => d!).ToList();
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    private async Task<Document?> FetchOneAsync(
        SearchHit hit, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var document = await _documentService.GetAsync(hit.DocumentId, ct).ConfigureAwait(false);
            if (document is null)
            {
                Log.DocumentNotFound(_logger, hit.DocumentId.Value);
            }

            return document;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> DocumentNotFoundHandler =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, nameof(DocumentNotFound)),
                "Document not found during context expansion: {DocumentId}");

        public static void DocumentNotFound(ILogger logger, string documentId) =>
            DocumentNotFoundHandler(logger, documentId, null);
    }
}
