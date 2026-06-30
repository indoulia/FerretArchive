namespace Ferret.Core.Runtime;

/// <summary>Represents the lifecycle state of the Ferret runtime host.</summary>
public enum RuntimeState
{
    /// <summary>The runtime is stopped and no modules are active.</summary>
    Stopped = 0,

    /// <summary>The runtime is in the process of starting up.</summary>
    Starting = 1,

    /// <summary>The runtime is fully started and all modules are active.</summary>
    Running = 2,

    /// <summary>The runtime is in the process of stopping.</summary>
    Stopping = 3,

    /// <summary>The runtime has encountered an unrecoverable error.</summary>
    Faulted = 4,
}
