namespace Ferret.Persistence;

/// <summary>The three outcomes a resolution comparison can produce (ARCH-027 §3). Exactly one of
/// these is always produced — no other value, and no partial or combined form, is possible.</summary>
public enum ResolutionOutcome
{
    /// <summary>Every recorded dependency matches current state; the candidate may be reused.</summary>
    Satisfied,

    /// <summary>At least one recorded dependency has changed, or no candidate exists.</summary>
    NotSatisfied,

    /// <summary>The persisted state needed to evaluate the candidate cannot be established.</summary>
    Indeterminate,
}
