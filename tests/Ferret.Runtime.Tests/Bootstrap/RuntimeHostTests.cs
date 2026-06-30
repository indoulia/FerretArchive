using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Bootstrap;

/// <summary>Tests for <see cref="RuntimeHost"/> accessed via <see cref="RuntimeBuilder"/>.</summary>
public sealed class RuntimeHostTests : IAsyncDisposable
{
    private readonly IRuntimeHost _host;

    public RuntimeHostTests()
    {
        _host = new RuntimeBuilder()
            .AddModule(new FakeModule("test-module"))
            .Build();
    }

    [Fact]
    public void State_BeforeStart_IsStopped()
    {
        Assert.Equal(RuntimeState.Stopped, _host.State);
    }

    [Fact]
    public async Task StartAsync_TransitionsToRunning()
    {
        await _host.StartAsync();

        Assert.Equal(RuntimeState.Running, _host.State);
    }

    [Fact]
    public async Task StopAsync_AfterStart_TransitionsToStopped()
    {
        await _host.StartAsync();
        await _host.StopAsync();

        Assert.Equal(RuntimeState.Stopped, _host.State);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperation()
    {
        await _host.StartAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _host.StartAsync());
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _host.StopAsync());
    }

    [Fact]
    public void Modules_ReturnsRegistry_NotNull()
    {
        Assert.NotNull(_host.Modules);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}
