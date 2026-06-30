namespace Ferret.Core.Documents;

/// <summary>A diagnostic message produced during parsing. Severity determines whether
/// the result is usable (Warning) or not (Error).</summary>
/// <param name="Severity">The diagnostic severity.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
public sealed record ParseDiagnostic(ParseDiagnosticSeverity Severity, string Message);
