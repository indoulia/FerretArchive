using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an asset has been successfully parsed into a Document.</summary>
public sealed class DocumentParsedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="DocumentParsedEvent"/> class.</summary>
    /// <param name="assetId">The asset identifier used as the aggregate ID.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public DocumentParsedEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId)
    {
    }

    /// <summary>Gets the source asset identifier.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the produced document identifier.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the MIME type of the parsed content.</summary>
    public required string MediaType { get; init; }
}
