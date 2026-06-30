using Ferret.Core.Primitives;

namespace Ferret.Core.Events;

/// <summary>Base class for integration events that cross module or service boundaries.</summary>
public abstract class IntegrationEvent
{
    /// <summary>Initializes a new instance of the <see cref="IntegrationEvent"/> class.</summary>
    /// <param name="source">The module or component that emitted this event.</param>
    /// <param name="correlationId">The correlation identifier for the operation that caused this event.</param>
    protected IntegrationEvent(string source, CorrelationId correlationId)
    {
        EventId = Guid.NewGuid().ToString("N");
        OccurredOn = DateTimeOffset.UtcNow;
        Source = source;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public string EventId { get; }

    /// <summary>Gets the UTC timestamp at which this event occurred.</summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>Gets the module or component that emitted this event.</summary>
    public string Source { get; }

    /// <summary>Gets the correlation identifier for the operation that caused this event.</summary>
    public CorrelationId CorrelationId { get; }
}
