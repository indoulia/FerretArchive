namespace Ferret.Cli.Diagnostics;

/// <summary>Severity of a diagnostic check outcome.</summary>
internal enum CheckSeverity
{
    /// <summary>The check passed.</summary>
    Pass,

    /// <summary>The check found an advisory issue that does not make the workspace unhealthy.</summary>
    Warn,

    /// <summary>The check failed; the workspace has a problem.</summary>
    Fail,
}

/// <summary>Why: Represents the result of a diagnostic check run.</summary>
internal sealed record DiagnosticCheckResult(CheckSeverity Severity, string? FailureReason = null)
{
    /// <summary>Gets a value indicating whether the check did not fail (passed or warned).</summary>
    internal bool Passed => Severity != CheckSeverity.Fail;

    /// <summary>Gets a value indicating whether the check produced an advisory warning.</summary>
    internal bool IsWarning => Severity == CheckSeverity.Warn;

    /// <summary>Returns a passing result.</summary>
    internal static DiagnosticCheckResult Pass() => new(CheckSeverity.Pass);

    /// <summary>Returns an advisory warning result with a reason.</summary>
    /// <param name="reason">The advisory reason.</param>
    internal static DiagnosticCheckResult Warn(string reason) => new(CheckSeverity.Warn, reason);

    /// <summary>Returns a failing result with a reason.</summary>
    /// <param name="reason">The failure reason.</param>
    internal static DiagnosticCheckResult Fail(string reason) => new(CheckSeverity.Fail, reason);
}
