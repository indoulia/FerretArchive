using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Registry;

/// <summary>
/// Read-only registry of active modules, keyed by module ID.
/// <para>Why: Gives the application layer and module contexts a safe, read-only view of all registered modules without exposing internal lifecycle state.</para>
/// <para>Lifecycle: Created by RuntimeBuilder.Build() from the sorted module list; registered as a DI singleton; lives until RuntimeHost is disposed.</para>
/// <para>Layer: Ferret.Runtime — IRuntimeHost.Modules exposes this via the IModuleRegistry contract.</para>
/// <para>Thread Safety: Thread Safe — immutable after construction; dictionary lookups are read-only.</para>
/// </summary>
internal sealed class ModuleRegistry : IModuleRegistry
{
    private readonly IReadOnlyList<DefaultModule> _ordered;
    private readonly Dictionary<string, DefaultModule> _byId;

    /// <summary>Initializes a new instance of the <see cref="ModuleRegistry"/> class with an ordered list of modules.</summary>
    /// <param name="ordered">The ordered list of modules to register.</param>
    internal ModuleRegistry(IReadOnlyList<DefaultModule> ordered)
    {
        _ordered = ordered;
        _byId = ordered.ToDictionary(m => m.Id);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<IModule> Modules => _ordered;

    /// <inheritdoc/>
    public bool TryGet(string moduleId, out IModule? module)
    {
        bool found = _byId.TryGetValue(moduleId, out DefaultModule? dm);
        module = dm;
        return found;
    }

    /// <inheritdoc/>
    public IModule? GetById(string moduleId)
    {
        return _byId.TryGetValue(moduleId, out DefaultModule? dm) ? dm : null;
    }
}
