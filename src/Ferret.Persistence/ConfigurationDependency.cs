namespace Ferret.Persistence;

/// <summary>
/// ARCH-032 §2.1's dependency shape 4 (configuration/registration): "the specific parser,
/// connector, or model/provider registration identity active at production time." Realizes the
/// gap ARCH-026 §3 records as unmet for every component — parser registration/version identity
/// (owned by Parser Platform) and connector configuration identity (owned by Connector Platform)
/// were not previously captured as part of a dependent artifact's dependency set. This type
/// captures a copy of that identity for validity-checking purposes only; it does not own, compute,
/// or replace either platform's own registration/configuration data (ARCH-023's Data Ownership
/// principle — V2 owns no primary data).
/// <para/>
/// Both properties are independently optional: not every artifact depends on both a parser and a
/// connector, and this record is itself optional on <see cref="DependencyRecord"/> so existing
/// (pre-S2-5) records remain valid without it. A third slot for model/provider registration
/// identity is deliberately not added here — ARCH-026 §3 leaves that dependency's <em>ownership</em>
/// unassigned (no ARCH-023-approved component currently owns it), and assigning ownership is a
/// governance decision this persistence-mechanism change is not authorized to make. Adding that
/// slot later, once ownership is assigned, is a purely additive change to this type — exactly the
/// kind of change S2-5 makes today for parser and connector identity.
/// </summary>
public sealed record ConfigurationDependency
{
    /// <summary>Gets the parser registration/version identity active when the artifact was produced, or null if this artifact has no parser dependency.</summary>
    public ComponentRegistrationIdentity? Parser { get; init; }

    /// <summary>Gets the connector configuration identity active when the artifact was produced, or null if this artifact has no connector dependency.</summary>
    public ComponentRegistrationIdentity? Connector { get; init; }
}
