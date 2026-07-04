namespace Ferret.Persistence;

/// <summary>
/// The materialized structure produced by recursively following <see cref="DependencyReference"/>s
/// outward from one root request identity: a set of <see cref="GraphNode"/>s connected by
/// <see cref="GraphEdge"/>s (ARCH-037 §1, §4). A deterministically materialized, immutable,
/// in-memory projection of dependency records already persisted in the repository — never itself
/// persisted, cached, or treated as a new source of truth (ARCH-037 §2). Its identity is its root
/// request identity and nothing else (ARCH-037 §3); it is never allocated an identifier of its own.
/// Produced only by <see cref="DependencyGraphMaterializer"/> — this type carries no construction
/// logic of its own, only the materialized result.
/// </summary>
public sealed record DependencyGraph
{
    /// <summary>Gets the node materialized for this graph's root request identity (ARCH-037 §3).</summary>
    public required GraphNode Root { get; init; }

    /// <summary>Gets every distinct <see cref="GraphNode"/> encountered during materialization — exactly one per distinct request identity reached (ARCH-037 §5, "No duplicate nodes").</summary>
    public required IReadOnlyList<GraphNode> Nodes { get; init; }

    /// <summary>Gets every <see cref="GraphEdge"/> materialized, including cycle-closing edges — never omitted (ARCH-037 §6, §7).</summary>
    public required IReadOnlyList<GraphEdge> Edges { get; init; }
}
