using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when a module has completed its startup sequence and is active.</summary>
public sealed class ModuleActivated : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="ModuleActivated"/> class.</summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="moduleName">The human-readable module name.</param>
    public ModuleActivated(string moduleId, string moduleName)
        : base(moduleId, CorrelationId.Create("system"))
    {
        ModuleId = moduleId ?? string.Empty;
        ModuleName = moduleName ?? string.Empty;
    }

    /// <summary>Gets the unique identifier of the activated module.</summary>
    public string ModuleId { get; }

    /// <summary>Gets the human-readable name of the activated module.</summary>
    public string ModuleName { get; }
}
