using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IIndexEngine. Tracks writes and clears for assertion.</summary>
internal sealed class FakeIndexEngine : IIndexEngine
{
    private readonly List<Document> _written = [];
    private readonly List<DocumentId> _deleted = [];

    /// <summary>Gets all documents written via WriteAsync.</summary>
    internal IReadOnlyList<Document> WrittenDocuments => _written;

    /// <summary>Gets all document IDs passed to DeleteAsync.</summary>
    internal IReadOnlyList<DocumentId> DeletedDocumentIds => _deleted;

    /// <summary>Gets the number of times ClearAsync was called.</summary>
    internal int ClearCount { get; private set; }

    /// <inheritdoc/>
    public Task WriteAsync(Document document, CancellationToken ct = default)
    {
        _written.Add(document);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IndexStats> GetStatsAsync(CancellationToken ct = default) =>
        Task.FromResult(new IndexStats
        {
            DocumentCount = _written.Count,
            TotalChars = _written.Sum(d => (long)d.PlainText.Length),
            LastIndexedAt = DateTimeOffset.UtcNow,
            IndexSizeBytes = 0,
        });

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default)
    {
        ClearCount++;
        _written.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(DocumentId documentId, CancellationToken ct = default)
    {
        _deleted.Add(documentId);
        _written.RemoveAll(d => d.Id.Equals(documentId));
        return Task.CompletedTask;
    }
}
