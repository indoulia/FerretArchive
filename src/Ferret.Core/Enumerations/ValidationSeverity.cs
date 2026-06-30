namespace Ferret.Core.Enumerations;

/// <summary>Represents the severity of a validation finding.</summary>
public enum ValidationSeverity
{
    /// <summary>Informational message — no action required.</summary>
    Info = 0,

    /// <summary>Warning — the input is valid but may cause issues.</summary>
    Warning = 1,

    /// <summary>Error — the input is invalid and must be corrected.</summary>
    Error = 2,
}
