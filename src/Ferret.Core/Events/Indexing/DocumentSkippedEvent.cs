using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an asset is skipped during indexing.</summary>
public sealed class DocumentSkippedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="DocumentSkippedEvent"/> class.</summary>
    /// <param name="assetId">The asset identifier used as the aggregate ID.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public DocumentSkippedEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId)
    {
    }

    /// <summary>Gets the skipped asset identifier.</summary>
    public required AssetId AssetId { get; init; }

    /// <summary>Gets the reason for skipping.</summary>
    public required string Reason { get; init; }
}
