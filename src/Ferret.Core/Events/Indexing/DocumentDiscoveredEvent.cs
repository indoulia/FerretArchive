using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published by <c>IndexPipeline</c> immediately before attempting to parse each discovered asset.
/// Represents the 8th indexing lifecycle event.</summary>
public sealed class DocumentDiscoveredEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="DocumentDiscoveredEvent"/> class.</summary>
    /// <param name="assetId">The string value of the discovered asset identifier.</param>
    /// <param name="correlationId">The correlation identifier for this pipeline run.</param>
    public DocumentDiscoveredEvent(string assetId, CorrelationId correlationId)
        : base(assetId, correlationId)
    {
        AssetId = new AssetId(assetId);
    }

    /// <summary>Gets the identifier of the discovered asset.</summary>
    public AssetId AssetId { get; }
}
