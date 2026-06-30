#pragma warning disable SA1201 // A enum should not follow a record
namespace Ferret.Core.Search;

/// <summary>A diagnostic message produced during query parsing or search execution.</summary>
/// <param name="Severity">The severity of this diagnostic.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
/// <param name="Position">The zero-based character position in the raw query where the issue occurred, if applicable.</param>
public sealed record SearchDiagnostic(SearchDiagnosticSeverity Severity, string Message, int? Position = null);

/// <summary>Severity levels for search diagnostics.</summary>
public enum SearchDiagnosticSeverity
{
    /// <summary>Informational note — no action required.</summary>
    Info = 0,

    /// <summary>Non-fatal issue — search proceeded with a best-effort interpretation.</summary>
    Warning = 1,

    /// <summary>Fatal error — search could not proceed.</summary>
    Error = 2,
}
#pragma warning restore SA1201 // A enum should not follow a record
