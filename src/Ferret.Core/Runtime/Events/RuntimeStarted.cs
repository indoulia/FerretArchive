using Ferret.Core.Events;
using Ferret.Core.Primitives;

namespace Ferret.Core.Runtime.Events;

/// <summary>Raised when the runtime host has fully started and all modules are active.</summary>
public sealed class RuntimeStarted : DomainEvent
{
    /// <summary>Initializes a new instance of the <see cref="RuntimeStarted"/> class.</summary>
    /// <param name="runtimeVersion">The version of the runtime that started.</param>
    public RuntimeStarted(string runtimeVersion)
        : base("runtime", CorrelationId.Create("system"))
    {
        RuntimeVersion = runtimeVersion ?? string.Empty;
    }

    /// <summary>Gets the version of the runtime host that started.</summary>
    public string RuntimeVersion { get; }
}
