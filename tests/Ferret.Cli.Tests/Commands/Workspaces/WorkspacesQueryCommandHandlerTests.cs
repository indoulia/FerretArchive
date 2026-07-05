using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Workspaces;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Knowledge.Federation;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Tests.Commands.Workspaces;

public sealed class WorkspacesQueryCommandHandlerTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeOutput _output;
    private readonly FakeContext _context;
    private readonly FakeWorkspaceStateFingerprintProvider _fingerprintProvider = new();
    private readonly FederatedQueryCache _queryCache = new();

    public WorkspacesQueryCommandHandlerTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-workspaces-query-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
        _output = new FakeOutput();
        _context = new FakeContext(new FakeServices(_output));
    }

    [Fact]
    public async Task Query_WithAReferencedWorkspace_PrintsHitsFromBothWithCitations()
    {
        var b = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "shared-lib",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "b", LocalPath = "C:/repo-b" }] },
        };
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "a", LocalPath = "C:/repo-a" }] },
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(a);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", "a-hit");
        factory.Register("C:/repo-b", "b-hit");
        var handler = new WorkspacesQueryCommandHandler(_registry, factory, _fingerprintProvider, _queryCache);
        _context.With("workspace", "service-a").With("query", "anything");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(_output.Lines, l => l.Contains("[service-a] a-hit", StringComparison.Ordinal));
        Assert.Contains(_output.Lines, l => l.Contains("[shared-lib] b-hit", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Query_WhenWorkspaceMissing_FailsWithActionableMessage()
    {
        var handler = new WorkspacesQueryCommandHandler(_registry, new FakeRepoSearchServiceFactory(), _fingerprintProvider, _queryCache);
        _context.With("workspace", "does-not-exist").With("query", "anything");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Query_WhenAReferencedRepoIsSkipped_StillSucceeds_AndPrintsADiagnostic()
    {
        // Stabilization Sprint 1: a partial result must be visibly partial, not silently indistinguishable
        // from a complete one.
        var b = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "shared-lib",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "b", LocalPath = "C:/repo-b-denied" }] },
        };
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "a", LocalPath = "C:/repo-a" }] },
            References = [new WorkspaceReference { WorkspaceId = b.WorkspaceId }],
        };
        await _registry.SaveAsync(b);
        await _registry.SaveAsync(a);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", "a-hit");
        factory.RegisterFailure("C:/repo-b-denied", SearchServiceStatus.IndexNotFound);
        var handler = new WorkspacesQueryCommandHandler(_registry, factory, _fingerprintProvider, _queryCache);
        _context.With("workspace", "service-a").With("query", "anything");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Success, result);
        Assert.Contains(_output.Lines, l => l.Contains("[service-a] a-hit", StringComparison.Ordinal));
        Assert.Contains(_output.Lines, l => l.Contains(b.WorkspaceId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Query_WhenNoRepoHasAnIndex_FailsWithActionableMessage()
    {
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "a", LocalPath = "C:/repo-a" }] },
        };
        await _registry.SaveAsync(a);
        var factory = new FakeRepoSearchServiceFactory();
        factory.RegisterFailure("C:/repo-a", SearchServiceStatus.IndexNotFound);
        var handler = new WorkspacesQueryCommandHandler(_registry, factory, _fingerprintProvider, _queryCache);
        _context.With("workspace", "service-a").With("query", "anything");

        var result = await handler.ExecuteAsync(_context);

        Assert.Equal(CommandResult.Failure, result);
        Assert.Contains(_output.Lines, l => l.Contains("ferret index", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_registryRoot))
        {
            Directory.Delete(_registryRoot, recursive: true);
        }
    }

    private sealed class FakeRepoSearchServiceFactory : IRepoSearchServiceFactory
    {
        private readonly Dictionary<string, SearchServiceResult> _resultsByRepoPath = [];

        public void Register(string repoPath, string hitDisplayName) =>
            _resultsByRepoPath[repoPath] = SearchServiceResult.Success(
                Query(),
                new SearchResult
                {
                    Hits = [Hit(hitDisplayName)],
                    TotalHits = 1,
                    ReturnedHits = 1,
                },
                new SearchExecutionInfo { SessionId = Guid.NewGuid(), ProviderName = "fake", Duration = TimeSpan.Zero, DocumentsScanned = 1, IndexVersion = "fake" });

        public void RegisterFailure(string repoPath, SearchServiceStatus status) =>
            _resultsByRepoPath[repoPath] = SearchServiceResult.Failure(Query(), status, []);

        public ISearchService CreateForRepo(string repoPath)
        {
            var result = _resultsByRepoPath.TryGetValue(repoPath, out var registered)
                ? registered
                : SearchServiceResult.Failure(Query(), SearchServiceStatus.IndexNotFound, []);
            return new FakeSearchService(result);
        }

        private static SearchQuery Query() => new() { OriginalText = string.Empty, Root = new KeywordExpression(string.Empty) };

        private static FileSearchHit Hit(string displayName) => new()
        {
            DocumentId = DocumentId.Create(displayName),
            ConnectorInstanceId = new ConnectorInstanceId("test"),
            CanonicalUri = new Uri($"filesystem:///{displayName}"),
            DisplayName = displayName,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = HighlightedText.Empty,
        };

        private sealed class FakeSearchService : ISearchService
        {
            private readonly SearchServiceResult _result;

            public FakeSearchService(SearchServiceResult result) => _result = result;

            public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) => Task.FromResult(_result);

            public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) => Task.FromResult(_result);
        }
    }

    private sealed class FakeWorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
    {
        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);
    }
}
