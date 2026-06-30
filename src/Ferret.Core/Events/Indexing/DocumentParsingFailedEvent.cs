using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when a parser fails for a specific asset.</summary>
public sealed class DocumentParsingFailedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="DocumentParsingFailedEvent"/> class.</summary>
    /// <param name="assetId">The asset identifier used as the aggregate ID.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public DocumentParsingFailedEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId)
    {
    }

    /// <summary>Gets the asset that failed to parse.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the MIME type that was dispatched to the parser.</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the error message from the parser failure.</summary>
    public required string ErrorMessage { get; init; }
}
