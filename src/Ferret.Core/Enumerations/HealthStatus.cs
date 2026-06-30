namespace Ferret.Core.Enumerations;

/// <summary>Represents the health state of a platform component or subsystem.</summary>
public enum HealthStatus
{
    /// <summary>Health state has not been determined.</summary>
    Unknown = 0,

    /// <summary>The component is operating normally.</summary>
    Healthy = 1,

    /// <summary>The component is operational but degraded.</summary>
    Degraded = 2,

    /// <summary>The component has failed and is not operational.</summary>
    Unhealthy = 3,
}
