using Ferret.Core.Runtime;

namespace Ferret.Runtime.Modules;

/// <summary>
/// Internal adapter that wraps a plain <see cref="IModuleDescriptor"/> (or <see cref="IModule"/>) so LifecycleOrchestrator always works with <see cref="DefaultModule"/>.
/// <para>Why: Allows plugin authors to implement IModule directly without extending DefaultModule; the runtime normalises all descriptors to DefaultModule at build time.</para>
/// <para>Lifecycle: Created by ModuleDescriptorStore.Add() for descriptors that do not already extend DefaultModule; owned by ModuleRegistry.</para>
/// <para>Layer: Ferret.Runtime internal — never exposed publicly.</para>
/// <para>Thread Safety: Thread Compatible — same contract as DefaultModule.</para>
/// </summary>
internal sealed class BoundModule : DefaultModule
{
    private readonly IModule? _lifecycleTarget;

    internal BoundModule(IModuleDescriptor descriptor)
        : base(ModuleMetadata.Create(
            descriptor.Id,
            descriptor.Name,
            descriptor.Version,
            descriptor.Capabilities,
            string.Empty,
            string.Empty))
    {
        _lifecycleTarget = descriptor as IModule;
    }

    /// <inheritdoc/>
    public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => _lifecycleTarget?.OnStartingAsync(context, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => _lifecycleTarget?.OnStartedAsync(context, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => _lifecycleTarget?.OnStoppingAsync(context, cancellationToken) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => _lifecycleTarget?.OnStoppedAsync(context, cancellationToken) ?? Task.CompletedTask;
}
