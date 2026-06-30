namespace Ferret.Cli.Diagnostics;

/// <summary>Why: Represents the result of a diagnostic check run.</summary>
internal sealed record DiagnosticCheckResult(bool Passed, string? FailureReason = null)
{
    /// <summary>Returns a passing result.</summary>
    internal static DiagnosticCheckResult Pass() => new(true);

    /// <summary>Returns a failing result with a reason.</summary>
    internal static DiagnosticCheckResult Fail(string reason) => new(false, reason);
}
