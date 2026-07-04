namespace Ferret.Persistence;

/// <summary>
/// The materialization state of a <see cref="GraphNode"/> (ARCH-037 §1) — "and nothing else":
/// this is the only classification a node carries. It is not a validity, resolution, or
/// correctness judgment (ARCH-037 §5, "No derived semantic state") — it states only whether the
/// node's <see cref="DependencyRecord"/> could be read at materialization time.
/// </summary>
public enum GraphNodeState
{
    /// <summary>The node's <see cref="DependencyRecord"/> was read successfully and is attached to the node.</summary>
    Resolved,

    /// <summary>
    /// The node's record could not be materialized — <see cref="IDependencyStateStore.GetRecordAsync"/>
    /// returned null, whether because the record is genuinely absent or because it was classified as
    /// corrupted or otherwise unreadable (ARCH-032 §5, S2-8). This mechanism carries that same
    /// coarse distinction forward rather than inventing a finer one (ARCH-037 §7).
    /// </summary>
    Unavailable,
}
