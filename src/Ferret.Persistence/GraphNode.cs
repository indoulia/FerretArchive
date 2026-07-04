namespace Ferret.Persistence;

/// <summary>
/// One distinct request identity encountered during a single graph materialization, together
/// with its materialization state and nothing else (ARCH-037 §1). Immutable once constructed
/// (ARCH-037 §5, "Immutable graph") — every property is init-only, matching the shape already
/// used by <see cref="DependencyRecord"/>, <see cref="DependencyReference"/>, and
/// <see cref="DependencyChain"/>. Carries no validity, resolution, conflict, or recommendation
/// vocabulary of any kind (ARCH-037 §5, "No derived semantic state") — <see cref="ResolutionCheck"/>
/// and <see cref="ResolutionOutcome"/> are never referenced here or by any type this record exposes.
/// </summary>
public sealed record GraphNode
{
    /// <summary>Gets the engine responsibility this node's request identity was produced for (ARCH-028 §2, property 1).</summary>
    public required string EngineResponsibility { get; init; }

    /// <summary>Gets the request path this node's request identity was produced for (ARCH-028 §2, property 2).</summary>
    public required string RequestPath { get; init; }

    /// <summary>Gets whether this node's <see cref="DependencyRecord"/> could be materialized (ARCH-037 §1, §7).</summary>
    public required GraphNodeState State { get; init; }

    /// <summary>
    /// Gets the persisted record this node was materialized from, when <see cref="State"/> is
    /// <see cref="GraphNodeState.Resolved"/>; null when <see cref="State"/> is
    /// <see cref="GraphNodeState.Unavailable"/> (ARCH-037 §1, §7).
    /// </summary>
    public DependencyRecord? Record { get; init; }
}
