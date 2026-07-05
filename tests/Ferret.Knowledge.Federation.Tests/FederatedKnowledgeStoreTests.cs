using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Workspace.Graph;

namespace Ferret.Knowledge.Federation.Tests;

public sealed class FederatedKnowledgeStoreTests : IDisposable
{
    private readonly string _registryRoot;
    private readonly FileWorkspaceRegistry _registry;
    private readonly FakeWorkspaceStateFingerprintProvider _fingerprintProvider = new();

    public FederatedKnowledgeStoreTests()
    {
        _registryRoot = Path.Join(Path.GetTempPath(), $"ferret-federation-test-{Guid.NewGuid():N}");
        _registry = new FileWorkspaceRegistry(_registryRoot);
    }

    [Fact]
    public async Task SearchAsync_WithNoReferences_ReturnsOnlyLocalHits()
    {
        var workspace = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        var store = new FederatedKnowledgeStore(_registry, factory, workspace.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Hits);
        Assert.Equal("a-hit", hit.DisplayName);
        Assert.Equal(workspace.WorkspaceId, hit.SourceWorkspaceId);
    }

    [Fact]
    public async Task SearchAsync_WithAReference_MergesHitsFromBothWorkspaces_WithCorrectSourceTagging()
    {
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [b.WorkspaceId]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.Register("C:/repo-b", FakeHit("b-hit", score: 2.0f));
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Hits.Count);
        Assert.Equal("b-hit", result.Hits[0].DisplayName); // higher score first
        Assert.Equal(b.WorkspaceId, result.Hits[0].SourceWorkspaceId);
        Assert.Equal("a-hit", result.Hits[1].DisplayName);
        Assert.Equal(a.WorkspaceId, result.Hits[1].SourceWorkspaceId);
    }

    [Fact]
    public async Task SearchAsync_WhenReferencedRepoHasNoIndex_StillReturnsLocalHits()
    {
        // "One repository may be unavailable without corrupting the other" — WIP-SLICE-1 acceptance criterion.
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b-missing"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [b.WorkspaceId]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.RegisterFailure("C:/repo-b-missing", SearchServiceStatus.IndexNotFound);
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Hits);
        Assert.Equal("a-hit", hit.DisplayName);
    }

    [Fact]
    public async Task SearchAsync_WhenReferencedRepoHasNoIndex_RecordsADiagnostic()
    {
        // Stabilization Sprint 1: a skipped source must be visible to the caller, not just absent from Hits.
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b-missing"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [b.WorkspaceId]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.RegisterFailure("C:/repo-b-missing", SearchServiceStatus.IndexNotFound);
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.Contains(result.Diagnostics, d => d.Message.Contains(b.WorkspaceId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_WhenOneSourceThrowsAnException_StillReturnsResultsFromTheOtherSource()
    {
        // Stabilization Sprint 1: any per-source I/O failure (not just a status-code failure) must degrade
        // only that source. Reproduces the class of bug found live in dogfooding (permission-denied index file).
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b-denied"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [b.WorkspaceId]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.RegisterThrows("C:/repo-b-denied", new UnauthorizedAccessException("Access to the path is denied."));
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Hits);
        Assert.Equal("a-hit", hit.DisplayName);
    }

    [Fact]
    public async Task SearchAsync_WhenOneSourceThrowsAnException_RecordsADiagnosticNamingTheFailure()
    {
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b-denied"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [b.WorkspaceId]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.RegisterThrows("C:/repo-b-denied", new UnauthorizedAccessException("Access to the path is denied."));
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.Contains(result.Diagnostics, d =>
            d.Message.Contains(b.WorkspaceId.ToString(), StringComparison.Ordinal)
            && d.Message.Contains("Access to the path is denied", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_WhenEverySourceThrows_ReturnsAFailureResult_NotAnException()
    {
        var workspace = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.RegisterThrows("C:/repo-a", new UnauthorizedAccessException("Access to the path is denied."));
        var store = new FederatedKnowledgeStore(_registry, factory, workspace.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("Access to the path is denied", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_WhenEveryRepoFails_ReturnsAFailureResult()
    {
        var workspace = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.RegisterFailure("C:/repo-a", SearchServiceStatus.IndexNotFound);
        var store = new FederatedKnowledgeStore(_registry, factory, workspace.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SearchAsync_WhenWorkspaceDoesNotExist_ReturnsWorkspaceNotFound()
    {
        var factory = new FakeRepoSearchServiceFactory();
        var store = new FederatedKnowledgeStore(_registry, factory, Guid.NewGuid(), _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.WorkspaceNotFound, result.Status);
    }

    [Fact]
    public async Task SearchAsync_WhenReferencedWorkspaceNoLongerExists_DegradesGracefullyToLocalOnly()
    {
        var danglingReferenceId = Guid.NewGuid();
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [danglingReferenceId]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, _fingerprintProvider);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Hits);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains(danglingReferenceId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchAsync_NeverCallsFactoryForARepoOutsideTheWorkspaceAndItsReferences()
    {
        // Zero-duplication guard at the fan-out layer: an unrelated workspace's repo must never be queried.
        await SaveWorkspaceAsync("unrelated", repoPaths: ["C:/repo-unrelated"]);
        var workspace = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        var store = new FederatedKnowledgeStore(_registry, factory, workspace.WorkspaceId, _fingerprintProvider);

        await store.SearchAsync("anything", SearchOptions.Default);

        Assert.DoesNotContain("C:/repo-unrelated", factory.RequestedRepoPaths);
    }

    [Fact]
    public async Task SearchAsync_WithAPinnedReferenceMatchingItsCurrentFingerprint_MergesHitsNormally()
    {
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [(b.WorkspaceId, "current-fingerprint")]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.Register("C:/repo-b", FakeHit("b-hit", score: 2.0f));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(b.WorkspaceId, "current-fingerprint");
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, fingerprints);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Hits.Count);
    }

    [Fact]
    public async Task SearchAsync_WithAPinnedReferenceWhoseCurrentFingerprintHasChanged_ExcludesItAndRecordsAnErrorDiagnostic()
    {
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [(b.WorkspaceId, "pinned-fingerprint")]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.Register("C:/repo-b", FakeHit("b-hit", score: 2.0f));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(b.WorkspaceId, "different-current-fingerprint");
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, fingerprints);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Hits);
        Assert.Equal("a-hit", hit.DisplayName);
        Assert.Contains(result.Diagnostics, d =>
            d.Severity == SearchDiagnosticSeverity.Error
            && d.Message.Contains(b.WorkspaceId.ToString(), StringComparison.Ordinal)
            && d.Message.Contains("out of date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_WithAPinnedReferenceThatCannotBeVerified_FailsClosed_ExcludingItWithAnErrorDiagnostic()
    {
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [(b.WorkspaceId, "pinned-fingerprint")]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.Register("C:/repo-b", FakeHit("b-hit", score: 2.0f));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(b.WorkspaceId, fingerprint: null); // cannot be computed (unreachable)
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, fingerprints);

        var result = await store.SearchAsync("anything", SearchOptions.Default);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Hits);
        Assert.Equal("a-hit", hit.DisplayName);
        Assert.Contains(result.Diagnostics, d => d.Severity == SearchDiagnosticSeverity.Error);
    }

    [Fact]
    public async Task SearchAsync_WithAFloatingReference_NeverCallsTheFingerprintProvider()
    {
        // Performance: only a pinned reference pays the cost of computing a fingerprint (ADR-0027 Consequences).
        var b = await SaveWorkspaceAsync("shared-lib", repoPaths: ["C:/repo-b"]);
        var a = await SaveWorkspaceAsync("service-a", repoPaths: ["C:/repo-a"], references: [(b.WorkspaceId, null)]);
        var factory = new FakeRepoSearchServiceFactory();
        factory.Register("C:/repo-a", FakeHit("a-hit", score: 1.0f));
        factory.Register("C:/repo-b", FakeHit("b-hit", score: 2.0f));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        var store = new FederatedKnowledgeStore(_registry, factory, a.WorkspaceId, fingerprints);

        await store.SearchAsync("anything", SearchOptions.Default);

        Assert.False(fingerprints.WasCalledFor(b.WorkspaceId));
    }

    private static FileSearchHit FakeHit(string displayName, float score) => new()
    {
        DocumentId = DocumentId.Create(displayName),
        ConnectorInstanceId = new ConnectorInstanceId("test"),
        CanonicalUri = new Uri($"filesystem:///{displayName}"),
        DisplayName = displayName,
        Kind = SearchHitKind.File,
        Score = score,
        Snippet = HighlightedText.Empty,
    };

    private Task<WorkspaceRegistryEntry> SaveWorkspaceAsync(string name, string[] repoPaths, Guid[]? references = null) =>
        SaveWorkspaceAsync(name, repoPaths, (references ?? []).Select(id => (id, (string?)null)).ToArray());

    private async Task<WorkspaceRegistryEntry> SaveWorkspaceAsync(
        string name, string[] repoPaths, (Guid WorkspaceId, string? PinnedStateHash)[] references)
    {
        var entry = new WorkspaceRegistryEntry
        {
            WorkspaceId = Guid.NewGuid(),
            Name = name,
            Members = new WorkspaceMembers
            {
                Repos = repoPaths.Select(p => new RepoMember { Remote = p, LocalPath = p }).ToList(),
            },
            References = references
                .Select(r => new WorkspaceReference { WorkspaceId = r.WorkspaceId, PinnedStateHash = r.PinnedStateHash })
                .ToList(),
        };
        await _registry.SaveAsync(entry);
        return entry;
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
        private readonly Dictionary<string, Exception> _exceptionsByRepoPath = [];
        private readonly List<string> _requestedRepoPaths = [];

        public IReadOnlyList<string> RequestedRepoPaths => _requestedRepoPaths;

        public void Register(string repoPath, FileSearchHit hit) =>
            _resultsByRepoPath[repoPath] = SearchServiceResult.Success(
                new SearchQuery { OriginalText = string.Empty, Root = new KeywordExpression(string.Empty) },
                new SearchResult { Hits = [hit], TotalHits = 1, ReturnedHits = 1 },
                new SearchExecutionInfo { SessionId = Guid.NewGuid(), ProviderName = "fake", Duration = TimeSpan.Zero, DocumentsScanned = 1, IndexVersion = "fake" });

        public void RegisterFailure(string repoPath, SearchServiceStatus status) =>
            _resultsByRepoPath[repoPath] = SearchServiceResult.Failure(
                new SearchQuery { OriginalText = string.Empty, Root = new KeywordExpression(string.Empty) },
                status,
                []);

        public void RegisterThrows(string repoPath, Exception exception) =>
            _exceptionsByRepoPath[repoPath] = exception;

        public ISearchService CreateForRepo(string repoPath)
        {
            _requestedRepoPaths.Add(repoPath);
            if (_exceptionsByRepoPath.TryGetValue(repoPath, out var exception))
            {
                return new FakeSearchService(null, exception);
            }

            var result = _resultsByRepoPath.TryGetValue(repoPath, out var registered)
                ? registered
                : SearchServiceResult.Failure(
                    new SearchQuery { OriginalText = string.Empty, Root = new KeywordExpression(string.Empty) },
                    SearchServiceStatus.IndexNotFound,
                    []);
            return new FakeSearchService(result, null);
        }

        private sealed class FakeSearchService : ISearchService
        {
            private readonly SearchServiceResult? _result;
            private readonly Exception? _exception;

            public FakeSearchService(SearchServiceResult? result, Exception? exception)
            {
                _result = result;
                _exception = exception;
            }

            public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
                _exception is not null ? Task.FromException<SearchServiceResult>(_exception) : Task.FromResult(_result!);

            public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
                _exception is not null ? Task.FromException<SearchServiceResult>(_exception) : Task.FromResult(_result!);
        }
    }

    private sealed class FakeWorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
    {
        private readonly Dictionary<Guid, string?> _fingerprintsByWorkspaceId = [];
        private readonly HashSet<Guid> _calledFor = [];

        public void Register(Guid workspaceId, string? fingerprint) => _fingerprintsByWorkspaceId[workspaceId] = fingerprint;

        public bool WasCalledFor(Guid workspaceId) => _calledFor.Contains(workspaceId);

        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
        {
            _calledFor.Add(entry.WorkspaceId);
            return Task.FromResult(_fingerprintsByWorkspaceId.GetValueOrDefault(entry.WorkspaceId));
        }

        // FederatedKnowledgeStore (the uncached, real pipeline under test in this file) never calls this
        // -- only CachingFederatedKnowledgeStore's cache-key construction does (P3-002) -- but the
        // interface requires an implementation.
        public Task<string?> ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            throw new NotSupportedException("FederatedKnowledgeStore never calls ComputeIndexChangeSignalAsync.");
    }
}
