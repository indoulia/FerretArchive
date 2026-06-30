using Ferret.Core.Runtime;

namespace Ferret.Runtime.Lifecycle;

/// <summary>
/// Default implementation of <see cref="IModuleContext"/>, giving a module access to the registry and its own identity.
/// <para>Why: Gives modules a stable, narrow view of the runtime so they can discover peer modules without accessing RuntimeHost directly.</para>
/// <para>Lifecycle: Created by ModuleLifecycleService per module per lifecycle phase; not reused.</para>
/// <para>Layer: Ferret.Runtime internal — passed to module lifecycle methods as IModuleContext.</para>
/// <para>Thread Safety: Single Thread Only — created and consumed on the lifecycle thread.</para>
/// </summary>
internal sealed class ModuleContext : IModuleContext
{
    /// <summary>Initializes a new instance of the <see cref="ModuleContext"/> class for the specified module.</summary>
    /// <param name="module">The module this context belongs to.</param>
    /// <param name="executionContext">The execution context for the current operation.</param>
    /// <param name="registry">The module registry for discovering peer modules.</param>
    internal ModuleContext(IModule module, IExecutionContext executionContext, IModuleRegistry registry)
    {
        ModuleId = module.Metadata.Id;
        ExecutionContext = executionContext;
        Registry = registry;
    }

    /// <inheritdoc/>
    public string ModuleId { get; }

    /// <inheritdoc/>
    public IExecutionContext ExecutionContext { get; }

    /// <inheritdoc/>
    public IModuleRegistry Registry { get; }
}
