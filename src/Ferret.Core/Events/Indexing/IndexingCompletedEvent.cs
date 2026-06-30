using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when an index pipeline run completes successfully.</summary>
public sealed class IndexingCompletedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="IndexingCompletedEvent"/> class.</summary>
    /// <param name="workspaceId">The workspace aggregate identifier.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public IndexingCompletedEvent(string workspaceId, CorrelationId correlationId)
        : base(workspaceId, correlationId)
    {
    }

    /// <summary>Gets the pipeline run outcome.</summary>
    public required IndexResult Result { get; init; }
}
