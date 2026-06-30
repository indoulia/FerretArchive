using Ferret.Core.Primitives;
using Ferret.Core.Runtime;

namespace Ferret.Runtime.Modules;

/// <summary>
/// Optional convenience base class for Ferret modules. Plugin authors may implement <see cref="IModule"/> and <see cref="IModuleDescriptor"/> directly without inheriting this class.
/// <para>Why: Provides a default no-op state machine and lifecycle stubs so simple modules do not repeat boilerplate. It is not required — composition over inheritance is preferred.</para>
/// <para>Lifecycle: Subclasses are instantiated by the plugin author; passed to IRuntimeBuilder.AddModule(); owned by ModuleRegistry after Build().</para>
/// <para>Layer: Ferret.Runtime — subclasses live in plugin assemblies or in Ferret.Runtime itself for built-in modules.</para>
/// <para>Thread Safety: Thread Compatible — SetState is called only by LifecycleOrchestrator on one thread at a time; State reads are volatile.</para>
/// </summary>
public abstract class DefaultModule : IModule, IModuleDescriptor
{
    private int _state = (int)ModuleState.Unloaded;

    /// <summary>Initializes a new instance of the <see cref="DefaultModule"/> class with the specified metadata.</summary>
    /// <param name="metadata">The metadata describing this module. Must not be null.</param>
    protected DefaultModule(ModuleMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <inheritdoc/>
    public ModuleMetadata Metadata { get; }

    /// <inheritdoc/>
    public ModuleState State => (ModuleState)Volatile.Read(ref _state);

    // IModuleDescriptor members — delegate to Metadata for consistency.

    /// <inheritdoc/>
    public string Id => Metadata.Id;

    /// <inheritdoc/>
    public string Name => Metadata.Name;

    /// <inheritdoc/>
    public SemanticVersion Version => Metadata.Version;

    /// <inheritdoc/>
    public IReadOnlyCollection<ModuleCapability> Capabilities => Metadata.Capabilities;

    /// <inheritdoc/>
    public virtual Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task OnStartedAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task OnStoppingAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual Task OnStoppedAsync(IModuleContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>Sets the module state. Called exclusively by LifecycleOrchestrator.</summary>
    internal void SetState(ModuleState state)
        => Volatile.Write(ref _state, (int)state);
}
