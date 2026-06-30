namespace Ferret.Core.Runtime;

/// <summary>Represents the lifecycle state of a platform module.</summary>
public enum ModuleState
{
    /// <summary>The module has not been loaded.</summary>
    Unloaded = 0,

    /// <summary>The module is currently loading.</summary>
    Loading = 1,

    /// <summary>The module is loaded and active.</summary>
    Active = 2,

    /// <summary>The module is in the process of deactivating.</summary>
    Deactivating = 3,

    /// <summary>The module has been stopped cleanly.</summary>
    Stopped = 4,

    /// <summary>The module has encountered an unrecoverable error.</summary>
    Faulted = 5,
}
