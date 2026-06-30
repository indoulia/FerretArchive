using Ferret.Core.Runtime.Events;
using Ferret.Runtime.Events;

namespace Ferret.Runtime.Tests.Events;

/// <summary>Tests for <see cref="RuntimeEventDispatcher"/>.</summary>
public sealed class RuntimeEventDispatcherTests
{
    [Fact]
    public async Task PublishAsync_NoHandlers_DoesNotThrow()
    {
        var dispatcher = new RuntimeEventDispatcher();
        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);
    }

    [Fact]
    public async Task Subscribe_HandlerReceivesPublishedEvent()
    {
        var dispatcher = new RuntimeEventDispatcher();
        RuntimeStarted? received = null;
        dispatcher.Subscribe<RuntimeStarted>(e =>
        {
            received = e;
            return Task.CompletedTask;
        });

        await dispatcher.PublishAsync(new RuntimeStarted("2.0.0"), CancellationToken.None);

        Assert.NotNull(received);
        Assert.Equal("2.0.0", received.RuntimeVersion);
    }

    [Fact]
    public async Task Subscribe_MultipleHandlers_AllInvoked()
    {
        var dispatcher = new RuntimeEventDispatcher();
        int count = 0;
        dispatcher.Subscribe<RuntimeStarted>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });
        dispatcher.Subscribe<RuntimeStarted>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });

        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task PublishAsync_HandlerThrows_OtherHandlersStillRun()
    {
        // ARCH-013: handler failures are isolated
        var dispatcher = new RuntimeEventDispatcher();
        int count = 0;
        dispatcher.Subscribe<RuntimeStarted>(_ => throw new InvalidOperationException("handler fail"));
        dispatcher.Subscribe<RuntimeStarted>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });

        // Must not throw to caller
        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Subscribe_DifferentEventTypes_HandlerNotCalledForWrongType()
    {
        var dispatcher = new RuntimeEventDispatcher();
        bool called = false;
        dispatcher.Subscribe<RuntimeStopped>(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.False(called);
    }

    [Fact]
    public async Task Unsubscribe_RemovesHandler()
    {
        var dispatcher = new RuntimeEventDispatcher();
        int count = 0;
        IDisposable sub = dispatcher.Subscribe<RuntimeStarted>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });
        sub.Dispose();

        await dispatcher.PublishAsync(new RuntimeStarted("1.0.0"), CancellationToken.None);

        Assert.Equal(0, count);
    }
}
