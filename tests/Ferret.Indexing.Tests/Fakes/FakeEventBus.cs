using Ferret.Core.Events;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IEventBus.</summary>
internal sealed class FakeEventBus : IEventBus
{
    private readonly List<DomainEvent> _published = [];

    /// <summary>Gets all events published via PublishAsync.</summary>
    internal IReadOnlyList<DomainEvent> Published => _published;

    /// <inheritdoc/>
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent
    {
        _published.Add(domainEvent);
        return Task.CompletedTask;
    }
}
