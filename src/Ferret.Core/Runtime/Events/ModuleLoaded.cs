using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when a module has been loaded into the runtime registry.</summary>
public sealed class ModuleLoaded : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="ModuleLoaded"/> class.</summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="moduleName">The human-readable module name.</param>
    /// <param name="version">The module version string.</param>
    public ModuleLoaded(string moduleId, string moduleName, string version)
        : base(moduleId, CorrelationId.Create("system"))
    {
        ModuleId = moduleId ?? string.Empty;
        ModuleName = moduleName ?? string.Empty;
        Version = version ?? string.Empty;
    }

    /// <summary>Gets the unique identifier of the loaded module.</summary>
    public string ModuleId { get; }

    /// <summary>Gets the human-readable name of the loaded module.</summary>
    public string ModuleName { get; }

    /// <summary>Gets the version of the loaded module.</summary>
    public string Version { get; }
}
