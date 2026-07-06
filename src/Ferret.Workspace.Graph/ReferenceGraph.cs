namespace Ferret.Workspace.Graph;

/// <summary>
/// Pure graph algorithm over the <c>IMPORTS</c> edges implied by every workspace's
/// <see cref="WorkspaceRegistryEntry.References"/> (<c>03-Cross-Workspace-References.md</c> §5:
/// "Reference graphs must be a DAG"). Takes a snapshot of all entries rather than the registry
/// itself — the CLI layer (WIP-SLICE-2's <c>add-reference</c> command) owns fetching that snapshot,
/// this type only walks it.
/// </summary>
public static class ReferenceGraph
{
    /// <summary>Determines whether adding a reference from <paramref name="fromWorkspaceId"/> to
    /// <paramref name="toWorkspaceId"/> would create a cycle, given the reference edges already
    /// present in <paramref name="allEntries"/>.</summary>
    /// <param name="allEntries">A snapshot of every workspace currently in the registry.</param>
    /// <param name="fromWorkspaceId">The workspace that would gain the new reference.</param>
    /// <param name="toWorkspaceId">The workspace the new reference would point to.</param>
    /// <returns><see langword="true"/> if the new edge would create a cycle (including a direct self-reference); otherwise <see langword="false"/>.</returns>
    public static bool WouldCreateCycle(IReadOnlyList<WorkspaceRegistryEntry> allEntries, Guid fromWorkspaceId, Guid toWorkspaceId)
    {
        ArgumentNullException.ThrowIfNull(allEntries);

        if (fromWorkspaceId == toWorkspaceId)
        {
            return true;
        }

        // A cycle would exist if the new edge closes a loop back to its own source — i.e. if
        // toWorkspaceId can already reach fromWorkspaceId through existing IMPORTS edges.
        var edgesBySource = allEntries.ToDictionary(e => e.WorkspaceId, e => e.References);

        var visited = new HashSet<Guid> { toWorkspaceId };
        var queue = new Queue<Guid>();
        queue.Enqueue(toWorkspaceId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!edgesBySource.TryGetValue(current, out var references))
            {
                continue;
            }

            foreach (var reference in references)
            {
                if (reference.WorkspaceId == fromWorkspaceId)
                {
                    return true;
                }

                if (visited.Add(reference.WorkspaceId))
                {
                    queue.Enqueue(reference.WorkspaceId);
                }
            }
        }

        return false;
    }
}
