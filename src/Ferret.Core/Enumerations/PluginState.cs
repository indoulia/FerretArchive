namespace Ferret.Core.Enumerations;

/// <summary>Represents the lifecycle state of a plugin within the platform.</summary>
public enum PluginState
{
    /// <summary>The plugin has not been loaded.</summary>
    Unloaded = 0,

    /// <summary>The plugin is in the process of loading.</summary>
    Loading = 1,

    /// <summary>The plugin is loaded and active.</summary>
    Active = 2,

    /// <summary>The plugin encountered an error and is in a faulted state.</summary>
    Faulted = 3,

    /// <summary>The plugin is in the process of unloading.</summary>
    Unloading = 4,
}
