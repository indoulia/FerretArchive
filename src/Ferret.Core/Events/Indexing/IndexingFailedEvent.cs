using Ferret.Core.Primitives;

namespace Ferret.Core.Events.Indexing;

/// <summary>Published when the index pipeline itself fails (not a per-document failure).</summary>
public sealed class IndexingFailedEvent : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="IndexingFailedEvent"/> class.</summary>
    /// <param name="workspaceId">The workspace aggregate identifier.</param>
    /// <param name="correlationId">The correlation identifier for the operation.</param>
    public IndexingFailedEvent(string workspaceId, CorrelationId correlationId)
        : base(workspaceId, correlationId)
    {
    }

    /// <summary>Gets the pipeline-level error message.</summary>
    public required string ErrorMessage { get; init; }
}
