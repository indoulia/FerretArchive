using Ferret.Core.Primitives;

namespace Ferret.Core.Events;

/// <summary>Base class for domain events that occur within an aggregate boundary.</summary>
public abstract class DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="DomainEvent"/> class.</summary>
    /// <param name="aggregateId">The identifier of the aggregate that raised this event.</param>
    /// <param name="correlationId">The correlation identifier for the operation that caused this event.</param>
    protected DomainEvent(string aggregateId, CorrelationId correlationId)
    {
        EventId = Guid.NewGuid().ToString("N");
        OccurredOn = DateTimeOffset.UtcNow;
        AggregateId = aggregateId;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public string EventId { get; }

    /// <summary>Gets the UTC timestamp at which this event occurred.</summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>Gets the identifier of the aggregate that raised this event.</summary>
    public string AggregateId { get; }

    /// <summary>Gets the correlation identifier for the operation that caused this event.</summary>
    public CorrelationId CorrelationId { get; }
}
