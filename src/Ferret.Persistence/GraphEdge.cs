namespace Ferret.Persistence;

/// <summary>
/// One <see cref="DependencyReference"/>, materialized as a directed edge from the
/// <see cref="GraphNode"/> that recorded it to the <see cref="GraphNode"/> representing the
/// referenced request identity, carrying exactly one structural flag: whether following this
/// edge closed a cycle (ARCH-037 §1, §6). Immutable once constructed (ARCH-037 §5) and carries no
/// derived semantic state — no validity, resolution, conflict, or recommendation vocabulary of
/// any kind is exposed here.
/// </summary>
public sealed record GraphEdge
{
    /// <summary>Gets the node this edge originates from — the node whose dependency chain recorded the reference.</summary>
    public required GraphNode From { get; init; }

    /// <summary>Gets the node this edge points to — the node representing the referenced request identity.</summary>
    public required GraphNode To { get; init; }

    /// <summary>
    /// Gets a value indicating whether following this edge arrived at a node already
    /// materialized earlier in the same construction (ARCH-037 §5, §6) — a structural fact only,
    /// asserting nothing about whether that recurrence is a true back-edge or a shared
    /// (diamond-shaped) dependency; both are the same fact under this mechanism's definition.
    /// </summary>
    public required bool ClosesCycle { get; init; }
}
