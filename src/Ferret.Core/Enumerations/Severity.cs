namespace Ferret.Core.Enumerations;

/// <summary>Represents the severity level of a finding, issue, or event.</summary>
public enum Severity
{
    /// <summary>No severity — informational only.</summary>
    None = 0,

    /// <summary>Low severity — minor issue with minimal impact.</summary>
    Low = 1,

    /// <summary>Medium severity — notable issue requiring attention.</summary>
    Medium = 2,

    /// <summary>High severity — significant issue requiring prompt action.</summary>
    High = 3,

    /// <summary>Critical severity — blocking issue requiring immediate action.</summary>
    Critical = 4,
}
