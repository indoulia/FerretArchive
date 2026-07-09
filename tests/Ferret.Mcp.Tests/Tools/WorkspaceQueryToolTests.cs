using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Knowledge.Federation;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;
using Ferret.Workspace.Graph;

using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class WorkspaceQueryToolTests
{
    private readonly FakeWorkspaceStateFingerprintProvider _fingerprintProvider = new();
    private readonly FederatedQueryCache _queryCache = new();

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new WorkspaceQueryTool(new FakeWorkspaceRegistry([]), new FakeRepoSearchServiceFactory(), _fingerprintProvider, _queryCache);
        Assert.Equal("workspace_query", sut.Descriptor.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithAReferencedWorkspace_ReturnsHitsFromBothWithCitations()
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
        var registry = new FakeWorkspaceRegistry([a, b]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", "a-hit");
        factory.Register("C:/repo-b", "b-hit");
        var sut = new WorkspaceQueryTool(registry, factory, _fingerprintProvider, _queryCache);

        var result = await sut.ExecuteAsync(Arguments(("workspace", "service-a"), ("query", "anything")), CancellationToken.None);

        Assert.False(result.IsError);
        var text = result.Content[0].Text!;
        Assert.Contains("[service-a] a-hit", text, StringComparison.Ordinal);
        Assert.Contains("[shared-lib] b-hit", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkspaceMissing_ReturnsErrorWithActionableMessage()
    {
        var sut = new WorkspaceQueryTool(new FakeWorkspaceRegistry([]), new FakeRepoSearchServiceFactory(), _fingerprintProvider, _queryCache);

        var result = await sut.ExecuteAsync(Arguments(("workspace", "does-not-exist"), ("query", "anything")), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content[0].Text!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAReferencedRepoIsSkipped_StillSucceeds_AndIncludesADiagnostic()
    {
        // Stabilization Sprint 1: a partial result must be visibly partial, not silently
        // indistinguishable from a complete one — mirrors WorkspacesQueryCommandHandlerTests.
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
        var registry = new FakeWorkspaceRegistry([a, b]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", "a-hit");
        factory.RegisterFailure("C:/repo-b-denied", SearchServiceStatus.IndexNotFound);
        var sut = new WorkspaceQueryTool(registry, factory, _fingerprintProvider, _queryCache);

        var result = await sut.ExecuteAsync(Arguments(("workspace", "service-a"), ("query", "anything")), CancellationToken.None);

        Assert.False(result.IsError);
        var text = result.Content[0].Text!;
        Assert.Contains("[service-a] a-hit", text, StringComparison.Ordinal);
        Assert.Contains(b.WorkspaceId.ToString(), text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoRepoHasAnIndex_ReturnsErrorWithActionableMessage()
    {
        var a = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = "service-a",
            Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = "a", LocalPath = "C:/repo-a" }] },
        };
        var registry = new FakeWorkspaceRegistry([a]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.RegisterFailure("C:/repo-a", SearchServiceStatus.IndexNotFound);
        var sut = new WorkspaceQueryTool(registry, factory, _fingerprintProvider, _queryCache);

        var result = await sut.ExecuteAsync(Arguments(("workspace", "service-a"), ("query", "anything")), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("ferret index", result.Content[0].Text!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_RegistryCorrupt_ReturnsErrorResult()
    {
        var sut = new WorkspaceQueryTool(new ThrowingWorkspaceRegistry(), new FakeRepoSearchServiceFactory(), _fingerprintProvider, _queryCache);

        var result = await sut.ExecuteAsync(Arguments(("workspace", "anything"), ("query", "anything")), CancellationToken.None);

        Assert.True(result.IsError);
    }

    private static McpArguments Arguments(params (string Key, string Value)[] pairs) =>
        McpArguments.From(pairs);

    private sealed class FakeWorkspaceRegistry(IReadOnlyList<WorkspaceRegistryEntry> entries) : IWorkspaceRegistry
    {
        public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default) =>
            Task.FromResult(entries.FirstOrDefault(e => e.WorkspaceId == workspaceId));

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult(entries);

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ThrowingWorkspaceRegistry : IWorkspaceRegistry
    {
        public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default) =>
            throw new WorkspaceRegistryCorruptException("bad.json", "malformed");

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            throw new WorkspaceRegistryCorruptException("bad.json", "malformed");

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
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

        private sealed class FakeSearchService(SearchServiceResult result) : ISearchService
        {
            public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) => Task.FromResult(result);

            public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) => Task.FromResult(result);
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
