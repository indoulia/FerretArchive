namespace Ferret.Core.Events;

/// <summary>No-op <see cref="IEventBus"/> used when no subscribers are registered.
/// Registered as the default in the CLI composition root; production code replaces
/// this with a real bus when subscribers exist.</summary>
public sealed class NullEventBus : IEventBus
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static NullEventBus Instance { get; } = new();

    /// <inheritdoc/>
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : DomainEvent
        => Task.CompletedTask;
}
