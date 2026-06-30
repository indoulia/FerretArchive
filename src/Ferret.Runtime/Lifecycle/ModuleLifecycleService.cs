using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Core.Runtime.Events;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Events;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

using Microsoft.Extensions.Hosting;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// IHostedService that starts and stops all modules in dependency order, publishing domain events and updating RuntimeState.
/// <para>Why: Bridges the IHost startup/shutdown lifecycle to the Ferret module lifecycle so RuntimeHost.StartAsync delegates to a single IHostedService.</para>
/// <para>Lifecycle: Registered as an IHostedService in RuntimeBuilder.Build(); started and stopped by IHost.</para>
/// <para>Layer: Ferret.Runtime internal — never accessible outside the runtime assembly.</para>
/// <para>Thread Safety: Single Thread Only — IHost guarantees StartAsync and StopAsync are not called concurrently.</para>
/// </summary>
internal sealed class ModuleLifecycleService : IHostedService
{
    private readonly LifecycleOrchestrator _orchestrator;
    private readonly IReadOnlyList<DefaultModule> _modules;
    private readonly RuntimeStateManager _stateManager;
    private readonly RuntimeEventDispatcher _events;
    private readonly RuntimeOptions _options;
    private readonly ModuleRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="ModuleLifecycleService"/> class.</summary>
    /// <param name="orchestrator">The orchestrator that drives per-module start/stop sequences.</param>
    /// <param name="modules">The ordered list of modules to manage.</param>
    /// <param name="stateManager">The runtime-level state machine.</param>
    /// <param name="events">The domain event dispatcher.</param>
    /// <param name="options">The runtime configuration options.</param>
    /// <param name="registry">The module registry for building per-module contexts.</param>
    public ModuleLifecycleService(
        LifecycleOrchestrator orchestrator,
        IReadOnlyList<DefaultModule> modules,
        RuntimeStateManager stateManager,
        RuntimeEventDispatcher events,
        RuntimeOptions options,
        ModuleRegistry registry)
    {
        _orchestrator = orchestrator;
        _modules = modules;
        _stateManager = stateManager;
        _events = events;
        _options = options;
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (DefaultModule module in _modules)
        {
            var execCtx = new ExecutionContext(
                CorrelationId.Create("system"),
                ExecutionId.Create(Guid.NewGuid().ToString("N")),
                cancellationToken);
            var ctx = new ModuleContext(module, execCtx, _registry);

            await _orchestrator.StartModuleAsync(module, ctx, cancellationToken).ConfigureAwait(false);

            await _events.PublishAsync(
                new ModuleActivated(module.Metadata.Id, module.Metadata.Name),
                cancellationToken).ConfigureAwait(false);
        }

        _stateManager.TryTransition(RuntimeState.Starting, RuntimeState.Running);

        await _events.PublishAsync(
            new RuntimeStarted(_options.RuntimeVersion),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        int activeCount = _modules.Count(m => m.State == ModuleState.Active);

        foreach (DefaultModule module in _modules.Reverse())
        {
            var execCtx = new ExecutionContext(
                CorrelationId.Create("system"),
                ExecutionId.Create(Guid.NewGuid().ToString("N")),
                cancellationToken);
            var ctx = new ModuleContext(module, execCtx, _registry);

            await _orchestrator.StopModuleAsync(module, ctx, cancellationToken).ConfigureAwait(false);

            await _events.PublishAsync(
                new ModuleStopped(module.Metadata.Id, module.Metadata.Name),
                cancellationToken).ConfigureAwait(false);
        }

        _stateManager.TryTransition(RuntimeState.Stopping, RuntimeState.Stopped);

        await _events.PublishAsync(
            new RuntimeStopped(_options.RuntimeVersion, activeCount),
            cancellationToken).ConfigureAwait(false);
    }
}
