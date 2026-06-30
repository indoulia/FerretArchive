using Ferret.Core.Search;

namespace Ferret.AI.Context;

/// <summary>
/// Removes duplicate search hits by document ID, preserving the first occurrence.
/// Pure function — no DI, no state, safe to call from any thread.
/// </summary>
public static class ContextDeduplicator
{
    /// <summary>
    /// Returns a new list with duplicate <see cref="SearchHit"/> entries removed.
    /// When the same <see cref="Ferret.Core.Primitives.DocumentId"/> appears more than once,
    /// the first occurrence is kept and subsequent occurrences are discarded.
    /// Input order is preserved.
    /// </summary>
    /// <param name="hits">The search hits to deduplicate.</param>
    /// <returns>A new list with at most one entry per document ID.</returns>
    public static IReadOnlyList<SearchHit> Deduplicate(IReadOnlyList<SearchHit> hits)
    {
        ArgumentNullException.ThrowIfNull(hits);

        if (hits.Count == 0)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<SearchHit>(hits.Count);

        foreach (var hit in hits)
        {
            if (seen.Add(hit.DocumentId.Value))
            {
                result.Add(hit);
            }
        }

        return result;
    }
}
