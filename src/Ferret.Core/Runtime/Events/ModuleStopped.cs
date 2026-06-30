using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when a module has completed its shutdown sequence.</summary>
public sealed class ModuleStopped : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="ModuleStopped"/> class.</summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="moduleName">The human-readable module name.</param>
    public ModuleStopped(string moduleId, string moduleName)
        : base(moduleId, CorrelationId.Create("system"))
    {
        ModuleId = moduleId ?? string.Empty;
        ModuleName = moduleName ?? string.Empty;
    }

    /// <summary>Gets the unique identifier of the stopped module.</summary>
    public string ModuleId { get; }

    /// <summary>Gets the human-readable name of the stopped module.</summary>
    public string ModuleName { get; }
}
