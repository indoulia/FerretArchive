using Ferret.Core.Events;

namespace Ferret.Runtime.Events;

/// <summary>
/// In-process typed pub/sub event bus for runtime domain events. Handler failures are isolated per ARCH-013.
/// <para>Why: Decouples lifecycle components (LifecycleOrchestrator, ModuleLifecycleService) from each other; they publish events rather than calling each other directly.</para>
/// <para>Lifecycle: Registered as a DI singleton in RuntimeBuilder.Build(); lives until RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime internal — never exposed publicly; runtime components subscribe via injection.</para>
/// <para>Thread Safety: Thread Safe — handler registration and dispatch are protected by a lock.</para>
/// </summary>
internal sealed class RuntimeEventDispatcher
{
    private readonly Dictionary<Type, List<Func<DomainEvent, Task>>> _handlers = [];

    private readonly Lock _lock = new();

    /// <summary>Subscribes <paramref name="handler"/> to events of type <typeparamref name="T"/>. Returns a disposable to unsubscribe.</summary>
    /// <typeparam name="T">The concrete domain event type.</typeparam>
    /// <param name="handler">The handler to invoke when an event of type <typeparamref name="T"/> is published.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription on disposal.</returns>
    public IDisposable Subscribe<T>(Func<T, Task> handler)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        Func<DomainEvent, Task> wrapper = e => handler((T)e);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Func<DomainEvent, Task>>? list))
            {
                list = [];
                _handlers[typeof(T)] = list;
            }

            list.Add(wrapper);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out List<Func<DomainEvent, Task>>? l))
                {
                    l.Remove(wrapper);
                }
            }
        });
    }

    /// <summary>Publishes <paramref name="domainEvent"/> to all registered handlers. Handler exceptions are caught and isolated per ARCH-013.</summary>
    /// <typeparam name="T">The concrete domain event type.</typeparam>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="cancellationToken">A token to cancel the publish operation.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        List<Func<DomainEvent, Task>>? snapshot;
        lock (_lock)
        {
            _handlers.TryGetValue(typeof(T), out snapshot);
            snapshot = snapshot is null ? null : [.. snapshot];
        }

        if (snapshot is null)
        {
            return;
        }

        foreach (Func<DomainEvent, Task> handler in snapshot)
        {
            try
            {
                await handler(domainEvent).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // ARCH-013: OperationCanceledException propagates when token is cancelled
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // ARCH-013: all other handler failures are isolated; remaining handlers still run
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _onDispose;

        private bool _disposed;

        internal Subscription(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _onDispose();
            }
        }
    }
}
