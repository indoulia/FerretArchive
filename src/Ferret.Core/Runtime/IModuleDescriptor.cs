using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime;

/// <summary>Describes a module for registration with the runtime builder before the module is activated.</summary>
public interface IModuleDescriptor
{
    /// <summary>Gets the unique module identifier.</summary>
    string Id { get; }

    /// <summary>Gets the human-readable module name.</summary>
    string Name { get; }

    /// <summary>Gets the module version.</summary>
    SemanticVersion Version { get; }

    /// <summary>Gets the capabilities this module declares.</summary>
    IReadOnlyCollection<ModuleCapability> Capabilities { get; }
}
