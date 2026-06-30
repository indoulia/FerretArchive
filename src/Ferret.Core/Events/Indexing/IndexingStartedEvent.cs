using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an index pipeline run begins. Aggregate: workspace ID.</summary>
public sealed class IndexingStartedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="IndexingStartedEvent"/> class.</summary>
    /// <param name="workspaceId">The workspace aggregate identifier.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public IndexingStartedEvent(string workspaceId, CorrelationId correlationId)
        : base(workspaceId, correlationId)
    {
    }

    /// <summary>Gets a value indicating whether this is a full rebuild run.</summary>
    public bool IsRebuild { get; init; }
}
