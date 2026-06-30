using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Registry;

/// <summary>
/// Accumulates module descriptors during the RuntimeBuilder configuration phase, wrapping non-DefaultModule entries into BoundModule.
/// <para>Why: Normalises all descriptors to DefaultModule so LifecycleOrchestrator always works with a uniform type.</para>
/// <para>Lifecycle: Created by RuntimeBuilder; consumed once by Build(); discarded after Build() returns.</para>
/// <para>Layer: Ferret.Runtime internal — used only by RuntimeBuilder.</para>
/// <para>Thread Safety: Single Thread Only — configure from one thread before Build().</para>
/// </summary>
internal sealed class ModuleDescriptorStore
{
    private readonly List<DefaultModule> _modules = [];
    private readonly HashSet<string> _ids = [];

    /// <summary>Adds a descriptor. Wraps plain IModuleDescriptor into BoundModule. Throws if the ID is already registered.</summary>
    public void Add(IModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_ids.Add(descriptor.Id))
        {
            throw new InvalidOperationException(
                $"A module with ID '{descriptor.Id}' has already been registered.");
        }

        if (descriptor is DefaultModule dm)
        {
            _modules.Add(dm);
        }
        else
        {
            _modules.Add(new BoundModule(descriptor));
        }
    }

    /// <summary>Returns all registered modules in registration order.</summary>
    public IReadOnlyList<DefaultModule> GetAll() => _modules;
}
