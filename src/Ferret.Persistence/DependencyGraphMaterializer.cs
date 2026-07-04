namespace Ferret.Persistence;

/// <summary>
/// Performs ARCH-037 §4's materialization procedure: builds a <see cref="DependencyGraph"/> by
/// recursively following <see cref="DependencyReference"/>s outward from a root request identity,
/// reading exclusively through <see cref="IDependencyStateStore.GetRecordAsync"/> — no new store
/// method, no new query shape (ARCH-037 §4). This is a direct generalization of
/// <see cref="ResolutionCheck"/>'s existing private <c>CompareLinksAsync</c>/<c>CompareLinkAsync</c>
/// traversal (ARCH-033), decoupled from producing a <see cref="ResolutionOutcome"/>: it produces
/// structure, never a validity or resolution judgment. <see cref="ResolutionCheck"/> and
/// <see cref="IDependencyStateStore"/> are consumed exactly as they already stand and are not
/// modified by this class.
/// </summary>
public static class DependencyGraphMaterializer
{
    /// <summary>
    /// Materializes the <see cref="DependencyGraph"/> rooted at the given request identity.
    /// </summary>
    /// <param name="engineResponsibility">The root's engine responsibility (ARCH-028 §2, property 1).</param>
    /// <param name="requestPath">The root's request path (ARCH-028 §2, property 2).</param>
    /// <param name="store">The store to fetch each identity's record from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The materialized graph.</returns>
    public static async Task<DependencyGraph> MaterializeAsync(
        string engineResponsibility, string requestPath, IDependencyStateStore store, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineResponsibility);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPath);
        ArgumentNullException.ThrowIfNull(store);

        var visited = new Dictionary<(string EngineResponsibility, string RequestPath), GraphNode>();
        var edges = new List<GraphEdge>();

        var root = await MaterializeNodeAsync(engineResponsibility, requestPath, store, visited, edges, ct).ConfigureAwait(false);

        return new DependencyGraph
        {
            Root = root,
            Nodes = [.. visited.Values],
            Edges = edges,
        };
    }

    private static async Task<GraphNode> MaterializeNodeAsync(
        string engineResponsibility,
        string requestPath,
        IDependencyStateStore store,
        Dictionary<(string EngineResponsibility, string RequestPath), GraphNode> visited,
        List<GraphEdge> edges,
        CancellationToken ct)
    {
        var key = (engineResponsibility, requestPath);

        // ARCH-037 §5's "No duplicate nodes" invariant: a key already materialized in this
        // construction — whether an ancestor still in progress or an already-completed sibling
        // subtree — returns the same node object rather than minting a second one. This is what
        // makes cycle detection (§6) a property of node identity rather than a separate pass.
        if (visited.TryGetValue(key, out var existing))
        {
            return existing;
        }

        // S2-8: FileDependencyStateStore itself classifies and fail-closes on an unreadable
        // record — it never throws, only returns null — so this traversal stays purely in terms
        // of the IDependencyStateStore abstraction, exactly as ResolutionCheck's traversal does.
        var record = await store.GetRecordAsync(engineResponsibility, requestPath, ct).ConfigureAwait(false);

        var node = new GraphNode
        {
            EngineResponsibility = engineResponsibility,
            RequestPath = requestPath,
            State = record is null ? GraphNodeState.Unavailable : GraphNodeState.Resolved,
            Record = record,
        };

        visited.Add(key, node);

        if (record is null)
        {
            // ARCH-037 §7: an unavailable node has no outgoing edges — there is no dependency
            // chain to read from a record that could not be materialized.
            return node;
        }

        foreach (var reference in record.DependencyChain.References)
        {
            var referenceKey = (reference.EngineResponsibility, reference.RequestPath);
            var closesCycle = visited.ContainsKey(referenceKey);

            var target = await MaterializeNodeAsync(reference.EngineResponsibility, reference.RequestPath, store, visited, edges, ct)
                .ConfigureAwait(false);

            // ARCH-037 §6, §7: every reference is represented as an edge, whether it closes a
            // cycle or resolves to an Unavailable node — never silently omitted.
            edges.Add(new GraphEdge { From = node, To = target, ClosesCycle = closesCycle });
        }

        return node;
    }
}
