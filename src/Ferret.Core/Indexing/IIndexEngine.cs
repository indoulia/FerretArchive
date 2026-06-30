using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.Core.Indexing;

/// <summary>Abstraction over the keyword (FTS5) index storage backend.</summary>
public interface IIndexEngine
{
    /// <summary>Writes a document to the index, inserting or replacing the existing entry.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the document has been written.</returns>
    Task WriteAsync(Document document, CancellationToken ct = default);

    /// <summary>Returns current statistics for this index.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to an <see cref="IndexStats"/> snapshot.</returns>
    Task<IndexStats> GetStatsAsync(CancellationToken ct = default);

    /// <summary>Deletes all documents from the index. Called by <c>IIndexPipeline</c> when
    /// <c>IndexPipelineOptions.ForceRebuild</c> is true. Storage engines never own orchestration.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when all documents have been deleted.</returns>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>Removes a single document from the index. No-ops if the document does not exist.</summary>
    /// <param name="documentId">The identifier of the document to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the document has been removed.</returns>
    Task DeleteAsync(DocumentId documentId, CancellationToken ct = default);
}
