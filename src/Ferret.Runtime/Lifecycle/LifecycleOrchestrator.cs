using Ferret.Core.Abstractions;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Microsoft.Extensions.Logging;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// Drives the lifecycle method sequence for a single module: Loading → Active (start) or Active → Stopped (stop).
/// <para>Why: Centralises lifecycle sequencing so RuntimeHost and ModuleLifecycleService do not duplicate the start/stop logic.</para>
/// <para>Lifecycle: Registered as a DI singleton; injected into ModuleLifecycleService.</para>
/// <para>Layer: Ferret.Runtime internal — not accessible outside the runtime assembly.</para>
/// <para>Thread Safety: Thread Compatible — each StartModuleAsync/StopModuleAsync call is independent; do not call concurrently on the same module.</para>
/// </summary>
internal sealed class LifecycleOrchestrator
{
    private static readonly Action<ILogger, string, Exception?> LogModuleStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "ModuleStarted"), "Module '{Id}' started.");

    private static readonly Action<ILogger, string, Exception?> LogModuleStartFaulted =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "ModuleStartFaulted"), "Module '{Id}' faulted during startup.");

    private static readonly Action<ILogger, string, Exception?> LogModuleStopped =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, "ModuleStopped"), "Module '{Id}' stopped.");

    private static readonly Action<ILogger, string, Exception?> LogModuleStopFaulted =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4, "ModuleStopFaulted"), "Module '{Id}' faulted during shutdown.");

    private readonly ILogger<LifecycleOrchestrator> _logger;

    /// <summary>Initializes a new instance of the <see cref="LifecycleOrchestrator"/> class.</summary>
    /// <param name="logger">The logger for lifecycle diagnostics.</param>
    public LifecycleOrchestrator(ILogger<LifecycleOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>Starts a module: Loading → (OnStarting → IInitializable → OnStarted) → Active. Throws and sets Faulted on failure.</summary>
    /// <param name="module">The module to start.</param>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous start operation.</returns>
    public async Task StartModuleAsync(
        DefaultModule module,
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        module.SetState(ModuleState.Loading);

        try
        {
            await module.OnStartingAsync(context, cancellationToken).ConfigureAwait(false);

            if (module is IInitializable initializable)
            {
                await initializable.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            await module.OnStartedAsync(context, cancellationToken).ConfigureAwait(false);
            module.SetState(ModuleState.Active);

            LogModuleStarted(_logger, module.Id, null);
        }
        catch (Exception ex) when (SetFaultedAndRethrow(module, ex))
        {
            // Unreachable: SetFaultedAndRethrow always returns false to propagate the exception.
            throw;
        }
    }

    /// <summary>Stops a module: Deactivating → (OnStopping → OnStopped) → Stopped. Best-effort — logs but does not rethrow.</summary>
    /// <param name="module">The module to stop.</param>
    /// <param name="context">The module context for the current operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    public async Task StopModuleAsync(
        DefaultModule module,
        IModuleContext context,
        CancellationToken cancellationToken)
    {
        module.SetState(ModuleState.Deactivating);

        try
        {
            await module.OnStoppingAsync(context, cancellationToken).ConfigureAwait(false);
            await module.OnStoppedAsync(context, cancellationToken).ConfigureAwait(false);
            module.SetState(ModuleState.Stopped);

            LogModuleStopped(_logger, module.Id, null);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Best-effort stop: set Faulted, log, but do not rethrow so remaining modules can shut down.
            module.SetState(ModuleState.Faulted);
            LogModuleStopFaulted(_logger, module.Id, ex);
        }
    }

    private bool SetFaultedAndRethrow(DefaultModule module, Exception ex)
    {
        module.SetState(ModuleState.Faulted);
        LogModuleStartFaulted(_logger, module.Id, ex);
        return false; // Always false — exception propagates.
    }
}
