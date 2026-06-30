using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when a Document has been written to the keyword index.</summary>
public sealed class DocumentIndexedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="DocumentIndexedEvent"/> class.</summary>
    /// <param name="documentId">The document identifier used as the aggregate ID.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public DocumentIndexedEvent(string documentId, CorrelationId correlationId)
        : base(documentId, correlationId)
    {
    }

    /// <summary>Gets the indexed document identifier.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the source asset identifier.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the MIME type of the indexed content.</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the number of characters in the indexed plain-text field.</summary>
    public required long CharCount { get; init; }
}
