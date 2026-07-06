using Ferret.Core.Search;

namespace Ferret.Knowledge.Federation;

/// <summary>
/// Builds an <see cref="ISearchService"/> rooted at a single repo's local checkout path. Kept as an
/// abstraction here (rather than <see cref="FederatedKnowledgeStore"/> depending on the concrete
/// search provider directly) so this module's only dependencies stay <c>Ferret.Core</c> and
/// <c>Ferret.Workspace.Graph</c>, per 01-Architecture.md §2 — the concrete implementation (backed by
/// the BM25/SQLite provider) is wired at the CLI composition root, which already depends on both
/// <c>Ferret.Search</c> and this module.
/// </summary>
public interface IRepoSearchServiceFactory
{
    /// <summary>Creates a search service scoped to a single repo's own index.</summary>
    /// <param name="repoPath">The repo's local checkout path (<see cref="Ferret.Workspace.Graph.RepoMember.LocalPath"/>).</param>
    /// <returns>An <see cref="ISearchService"/> that queries only that repo's index.</returns>
    ISearchService CreateForRepo(string repoPath);
}
