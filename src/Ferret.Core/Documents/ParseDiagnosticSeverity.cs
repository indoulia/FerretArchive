namespace Ferret.Core.Documents;

/// <summary>Severity levels for parse diagnostics.</summary>
public enum ParseDiagnosticSeverity
{
    /// <summary>Informational note — does not affect result usability.</summary>
    Info = 0,

    /// <summary>Non-fatal issue (e.g. encoding fallback, malformed section) — result is still usable.</summary>
    Warning = 1,

    /// <summary>Fatal parse error — result is not usable.</summary>
    Error = 2,
}
