namespace Ferret.Core.Events;

/// <summary>Publishes domain events to registered subscribers. In-process only for Sprint 9.</summary>
public interface IEventBus
{
    /// <summary>Publishes a domain event to all registered subscribers.</summary>
    /// <typeparam name="TEvent">The concrete event type.</typeparam>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that completes when all subscribers have been notified.</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent;
}
