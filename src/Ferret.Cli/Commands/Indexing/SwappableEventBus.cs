using Ferret.Core.Events;

namespace Ferret.Cli.Commands.Indexing;

/// <summary>
/// IEventBus whose inner bus can be replaced between pipeline invocations.
/// Registered as a singleton so IndexCommandHandler can inject a verbose sink at runtime.
/// Note: not safe for concurrent invocations from the same DI scope — CLI tools run once per process.
/// </summary>
internal sealed class SwappableEventBus : IEventBus
{
    private IEventBus _inner;

    internal SwappableEventBus(IEventBus inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <summary>Gets or sets the active inner bus.</summary>
    internal IEventBus Inner
    {
        get => _inner;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _inner = value;
        }
    }

    /// <inheritdoc/>
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent => _inner.PublishAsync(domainEvent, ct);
}
