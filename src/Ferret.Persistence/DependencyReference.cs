namespace Ferret.Persistence;

/// <summary>
/// A reference to another artifact's own dependency record, identified by the same two request-
/// identity components <see cref="IDependencyStateStore.GetRecordAsync"/> already keys on
/// (ARCH-028 §2, properties 1–2). Realizes ARCH-025 §3's dependency shape 2 (derived-artifact
/// dependency) exactly as ARCH-032 §7.3 and §7.10 require: **by reference, never by embedding a
/// copy** of the referenced artifact's own dependency state. A caller holding a
/// <see cref="DependencyReference"/> looks the referenced record up fresh, through the same
/// <see cref="IDependencyStateStore"/> abstraction, when (and only when) it needs to — this type
/// carries no cached copy of what that lookup would return, so it can never itself go stale or
/// diverge from the referenced artifact's own, separately-owned record.
/// </summary>
public sealed record DependencyReference
{
    /// <summary>Gets the engine responsibility of the referenced artifact (ARCH-028 §2, property 1).</summary>
    public required string EngineResponsibility { get; init; }

    /// <summary>Gets the request path of the referenced artifact (ARCH-028 §2, property 2).</summary>
    public required string RequestPath { get; init; }
}
