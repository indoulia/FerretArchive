namespace Ferret.Core.Connectors;

/// <summary>Indicates the severity of a validation issue.</summary>
public enum ValidationSeverity
{
    /// <summary>Advisory — does not block operation.</summary>
    Warning,

    /// <summary>Blocking — marks the overall result as invalid.</summary>
    Error,
}
