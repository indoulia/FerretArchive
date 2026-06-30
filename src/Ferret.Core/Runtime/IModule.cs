namespace Ferret.Core.Runtime;

/// <summary>Represents a platform module managed by the runtime host.</summary>
public interface IModule : ILifecycleParticipant
{
    /// <summary>Gets the metadata describing this module.</summary>
    ModuleMetadata Metadata { get; }

    /// <summary>Gets the current lifecycle state of this module.</summary>
    ModuleState State { get; }
}
