using Ferret.Core.Runtime;
using Ferret.Runtime.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ferret.Runtime.Bootstrap;

/// <summary>
/// Coordinates module startup, shutdown, event dispatch, and health aggregation for the Ferret platform.
/// <para>Why: Owns the runtime lifecycle and composes all collaborators behind the IRuntimeHost contract.</para>
/// <para>Lifecycle: Built by RuntimeBuilder.Build(); owned by the application entry point; disposed at application shutdown.</para>
/// <para>Layer: Ferret.Runtime — consumed by the application layer only; never referenced by Core.</para>
/// <para>Thread Safety: Thread Compatible — StartAsync/StopAsync must not be called concurrently.</para>
/// </summary>
internal sealed class RuntimeHost : IRuntimeHost, IAsyncDisposable
{
    private readonly IHost _host;
    private readonly RuntimeStateManager _stateManager;

    internal RuntimeHost(IHost host)
    {
        _host = host;
        _stateManager = host.Services.GetRequiredService<RuntimeStateManager>();
    }

    /// <inheritdoc/>
    public RuntimeState State => _stateManager.Current;

    /// <inheritdoc/>
    public IModuleRegistry Modules =>
        _host.Services.GetRequiredService<ModuleRegistry>();

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_stateManager.TryTransition(RuntimeState.Stopped, RuntimeState.Starting))
        {
            throw new InvalidOperationException(
                $"Cannot start runtime: current state is '{State}'. Runtime must be Stopped before starting.");
        }

        try
        {
            await _host.StartAsync(cancellationToken).ConfigureAwait(false);

            // ModuleLifecycleService.StartAsync transitions Starting → Running
        }
        catch
        {
            _stateManager.ForceSet(RuntimeState.Faulted);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_stateManager.TryTransition(RuntimeState.Running, RuntimeState.Stopping))
        {
            throw new InvalidOperationException(
                $"Cannot stop runtime: current state is '{State}'. Runtime must be Running before stopping.");
        }

        try
        {
            await _host.StopAsync(cancellationToken).ConfigureAwait(false);

            // ModuleLifecycleService.StopAsync transitions Stopping → Stopped
        }
        catch
        {
            _stateManager.ForceSet(RuntimeState.Faulted);
            throw;
        }
    }

    /// <summary>Stops the runtime if running, then disposes the underlying host.</summary>
    public async ValueTask DisposeAsync()
    {
        if (State is RuntimeState.Running)
        {
            try
            {
                await StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // best-effort: suppress shutdown errors during disposal
            }
        }

        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _host.Dispose();
        }
    }
}
