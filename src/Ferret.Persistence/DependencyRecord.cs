using Ferret.Core.Connectors;

namespace Ferret.Persistence;

/// <summary>
/// The minimum recorded state needed to later determine whether a previously produced
/// artifact still satisfies an equivalent request. Realizes ARCH-032 §2.1 (dependency
/// shape 1: source content), §2.2 (artifact state, Class A only), and §2.3 (request
/// identity, ARCH-028 §2) in the minimal form Sprint 1 needs: exactly one engine
/// responsibility, one dependency shape, and no ambient scope beyond the request path.
/// </summary>
public sealed record DependencyRecord
{
    /// <summary>Gets the engine responsibility this record was produced for (ARCH-028 §2,
    /// property 1). Sprint 1 has exactly one responsibility in scope: parsing a file at a
    /// given path.</summary>
    public required string EngineResponsibility { get; init; }

    /// <summary>Gets the file path this record was produced for — the complete explicit
    /// parameter set for Sprint 1's one responsibility (ARCH-028 §2, property 2). Sprint 1
    /// has no ambient dependency scope beyond this explicit parameter (ARCH-028 §2, property 3).</summary>
    public required string RequestPath { get; init; }

    /// <summary>Gets the source-content dependency (ARCH-025 §3, dependency shape 1) — the
    /// fingerprint of the file this record's artifact was produced against.</summary>
    public required AssetFingerprint SourceFingerprint { get; init; }

    /// <summary>Gets the produced artifact's own reusable output (ARCH-032 §2.2), so a later
    /// Satisfied resolution can reuse it instead of re-parsing. Null if the artifact was not
    /// made durable — ARCH-032 §2.2 does not mandate that every artifact become durable.</summary>
    public string? PlainText { get; init; }

    /// <summary>Gets the dependency-shape-4 (configuration/registration, ARCH-032 §2.1) identity
    /// active when this record's artifact was produced — S2-5. Null for records that predate S2-5
    /// or that have no such dependency; this property is purely additive and does not participate
    /// in resolution (<see cref="ResolutionCheck"/>) until a later milestone extends comparison to
    /// cover it.</summary>
    public ConfigurationDependency? ConfigurationDependency { get; init; }

    /// <summary>Gets the dependency-shape-2 (derived-artifact, ARCH-025 §3) chain this record's
    /// artifact depends on — S2-6. Defaults to <see cref="Persistence.DependencyChain.Empty"/> for
    /// records that predate S2-6 or that depend on no other artifact; purely additive and does not
    /// participate in resolution until a later milestone extends comparison to cover it.</summary>
    public DependencyChain DependencyChain { get; init; } = DependencyChain.Empty;
}
