namespace Ferret.Persistence;

/// <summary>
/// Determines whether two requests are equivalent, per ARCH-028 §3's exact, contract-level
/// relation. Sprint 1's minimal request-identity shape (ARCH-028 §2) has exactly one engine
/// responsibility and no ambient dependency scope beyond the request path, so identity reduces
/// to these two properties. No partial, approximate, or subsuming match is recognized (ARCH-028 §4).
/// </summary>
public static class RequestEquivalence
{
    /// <summary>Returns true only when both requests' engine responsibility and request path match exactly.</summary>
    /// <param name="engineResponsibilityA">The first request's engine responsibility (ARCH-028 §2, property 1).</param>
    /// <param name="requestPathA">The first request's explicit parameter — the file path (ARCH-028 §2, property 2).</param>
    /// <param name="engineResponsibilityB">The second request's engine responsibility (ARCH-028 §2, property 1).</param>
    /// <param name="requestPathB">The second request's explicit parameter — the file path (ARCH-028 §2, property 2).</param>
    /// <returns>True if both requests are equivalent per ARCH-028 §3; otherwise false.</returns>
    public static bool AreEquivalent(string engineResponsibilityA, string requestPathA, string engineResponsibilityB, string requestPathB)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineResponsibilityA);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPathA);
        ArgumentException.ThrowIfNullOrWhiteSpace(engineResponsibilityB);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPathB);

        return engineResponsibilityA == engineResponsibilityB && requestPathA == requestPathB;
    }
}
