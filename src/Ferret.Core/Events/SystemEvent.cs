using Ferret.Core.Primitives;

namespace Ferret.Core.Events;

/// <summary>Base class for platform-level system events, such as startup and shutdown notifications.</summary>
public abstract class SystemEvent
{
    /// <summary>Initializes a new instance of the <see cref="SystemEvent"/> class.</summary>
    /// <param name="component">The platform component that emitted this event.</param>
    /// <param name="correlationId">The correlation identifier for the operation that caused this event.</param>
    protected SystemEvent(string component, CorrelationId correlationId)
    {
        EventId = Guid.NewGuid().ToString("N");
        OccurredOn = DateTimeOffset.UtcNow;
        Component = component;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the unique identifier of this event instance.</summary>
    public string EventId { get; }

    /// <summary>Gets the UTC timestamp at which this event occurred.</summary>
    public DateTimeOffset OccurredOn { get; }

    /// <summary>Gets the platform component that emitted this event.</summary>
    public string Component { get; }

    /// <summary>Gets the correlation identifier for the operation that caused this event.</summary>
    public CorrelationId CorrelationId { get; }
}
