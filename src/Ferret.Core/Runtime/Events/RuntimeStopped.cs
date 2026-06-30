using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when the runtime host has fully stopped.</summary>
public sealed class RuntimeStopped : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="RuntimeStopped"/> class.</summary>
    /// <param name="runtimeVersion">The version of the runtime that stopped.</param>
    /// <param name="modulesActive">The number of modules that were active at the time of shutdown.</param>
    public RuntimeStopped(string runtimeVersion, int modulesActive)
        : base("runtime", CorrelationId.Create("system"))
    {
        RuntimeVersion = runtimeVersion ?? string.Empty;
        ModulesActive = modulesActive;
    }

    /// <summary>Gets the version of the runtime host that stopped.</summary>
    public string RuntimeVersion { get; }

    /// <summary>Gets the number of modules that were active at the time of shutdown.</summary>
    public int ModulesActive { get; }
}
