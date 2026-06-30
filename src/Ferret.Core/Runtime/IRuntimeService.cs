namespace Ferret.Core.Runtime;

/// <summary>Marker interface for services provided by the runtime and resolvable by modules.</summary>
public interface IRuntimeService
{
    /// <summary>Gets the unique identifier for this runtime service.</summary>
    string ServiceId { get; }
}
