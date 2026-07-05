using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Knowledge.Federation;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;
using Ferret.Workspace;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>
/// Concrete <see cref="IRepoSearchServiceFactory"/> for the CLI composition root. Builds an
/// isolated <see cref="SearchService"/> rooted at a single repo's own <c>.ferret/</c> index, reusing
/// the same <see cref="Bm25SearchProvider"/>/<see cref="SearchService"/> pipeline <c>ferret search</c>
/// already uses — <see cref="Ferret.Knowledge.Federation"/> stays decoupled from this concrete
/// provider (01-Architecture.md §2); only this composition-root type knows about it.
/// </summary>
internal sealed class RepoSearchServiceFactory : IRepoSearchServiceFactory
{
    private readonly IQueryParser _queryParser;

    /// <summary>Initializes a new instance of the <see cref="RepoSearchServiceFactory"/> class.</summary>
    /// <param name="queryParser">The query parser shared across every per-repo search service — stateless, safe to reuse.</param>
    public RepoSearchServiceFactory(IQueryParser queryParser) => _queryParser = queryParser;

    /// <inheritdoc/>
    public ISearchService CreateForRepo(string repoPath)
    {
        var context = new DefaultWorkspaceContext(WorkspaceId.Create(repoPath), WorkspacePath.Create(repoPath));
        var provider = new Bm25SearchProvider(context);
        return new SearchService(_queryParser, [provider], []);
    }
}
