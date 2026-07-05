using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Workspace.Graph;

namespace Ferret.Knowledge.Federation.Tests;

public sealed class CachingFederatedKnowledgeStoreTests
{
    [Fact]
    public async Task SearchAsync_CalledTwiceWithTheIdenticalQuery_OnlyInvokesInnerStoreOnce()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, workspaceId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_CachedResult_ReturnsTheExactHitsAndDiagnosticsFromTheOriginalCall()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, workspaceId, new FederatedQueryCache());

        var first = await cache.SearchAsync("term", SearchOptions.Default);
        var second = await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(first.Hits[0].DisplayName, second.Hits[0].DisplayName);
        Assert.Equal(first.Diagnostics[0].Message, second.Diagnostics[0].Message);
        Assert.Equal("hit-1", second.Hits[0].DisplayName); // proves it's call 1's data, not a second live call
    }

    [Fact]
    public async Task SearchAsync_CalledWithADifferentQueryText_InvokesInnerStoreAgain()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, workspaceId, new FederatedQueryCache());

        await cache.SearchAsync("term-one", SearchOptions.Default);
        await cache.SearchAsync("term-two", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_CalledWithDifferentMaxResults_InvokesInnerStoreAgain()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, workspaceId, new FederatedQueryCache());

        await cache.SearchAsync("term", new SearchOptions { MaxResults = 10 });
        await cache.SearchAsync("term", new SearchOptions { MaxResults = 20 });

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenTheQueriedWorkspacesOwnContentChanges_InvokesInnerStoreAgain()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, workspaceId, new FederatedQueryCache());
        await cache.SearchAsync("term", SearchOptions.Default);

        fingerprints.Register(workspaceId, "fp-a-changed");
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenAPinnedReferencesCurrentFingerprintDrifts_InvokesInnerStoreAgain()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: "pinned-hash"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.Register(bId, "pinned-hash");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());
        await cache.SearchAsync("term", SearchOptions.Default);

        fingerprints.Register(bId, "different-current-fingerprint");
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_AfterAddReferenceViaRegistrySave_InvokesInnerStoreAgain()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(SimpleEntry(aId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.Register(bId, "fp-b");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());
        await cache.SearchAsync("term", SearchOptions.Default);

        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_AfterRemoveReferenceViaRegistrySave_InvokesInnerStoreAgain()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.Register(bId, "fp-b");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());
        await cache.SearchAsync("term", SearchOptions.Default);

        registry.Register(SimpleEntry(aId, "service-a"));
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenAReferencedWorkspaceDisappearsFromTheRegistry_InvokesInnerStoreAgain()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.Register(bId, "fp-b");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());
        await cache.SearchAsync("term", SearchOptions.Default);

        registry.Remove(bId);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenAReferencedWorkspaceRegistryEntryIsCorrupt_NeverCachesAndAlwaysInvokesInnerStore()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.RegisterCorrupt(bId);
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(3, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenAParticipatingWorkspacesFingerprintCannotBeComputed_NeverCachesAndAlwaysInvokesInnerStore()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.Register(bId, fingerprint: null); // unreachable local checkout
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(3, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenComputingTheFingerprintThrows_NeverCachesAndAlwaysInvokesInnerStore()
    {
        // Real dogfooding evidence: a locked or permission-denied file under a member repo makes
        // WorkspaceStateFingerprintProvider throw (e.g. IOException). That must degrade to "can't
        // verify this workspace's state, don't cache" -- never crash the query itself.
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new ThrowingWorkspaceStateFingerprintProvider();
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, workspaceId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenTheQueriedWorkspaceItselfCannotBeFound_NeverCachesAndAlwaysInvokesInnerStore()
    {
        var unknownId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, unknownId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    private static WorkspaceRegistryEntry SimpleEntry(Guid workspaceId, string name) => new()
    {
        WorkspaceId = workspaceId,
        Name = name,
        Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = $"C:/{name}", LocalPath = $"C:/{name}" }] },
    };

    private static WorkspaceRegistryEntry EntryWithReference(Guid workspaceId, string name, Guid referencedId, string? pinnedStateHash) => new()
    {
        WorkspaceId = workspaceId,
        Name = name,
        SchemaVersion = "1.1",
        Members = new WorkspaceMembers { Repos = [new RepoMember { Remote = $"C:/{name}", LocalPath = $"C:/{name}" }] },
        References = [new WorkspaceReference { WorkspaceId = referencedId, PinnedStateHash = pinnedStateHash }],
    };

    private sealed class CountingInnerStore : IFederatedKnowledgeStore
    {
        public int CallCount { get; private set; }

        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) => Respond();

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) => Respond();

        private Task<SearchServiceResult> Respond()
        {
            CallCount++;
            var callNumber = CallCount;
            var hit = new FileSearchHit
            {
                DocumentId = DocumentId.Create($"hit-{callNumber}"),
                ConnectorInstanceId = new ConnectorInstanceId("test"),
                CanonicalUri = new Uri($"filesystem:///hit-{callNumber}"),
                DisplayName = $"hit-{callNumber}",
                Kind = SearchHitKind.File,
                Score = 1.0f,
                Snippet = HighlightedText.Empty,
            };
            var result = SearchServiceResult.Success(
                new SearchQuery { OriginalText = string.Empty, Root = new KeywordExpression(string.Empty) },
                new SearchResult { Hits = [hit], TotalHits = 1, ReturnedHits = 1 },
                new SearchExecutionInfo { SessionId = Guid.NewGuid(), ProviderName = "fake", Duration = TimeSpan.Zero, DocumentsScanned = 1, IndexVersion = "fake" })
                with
            { Diagnostics = [new SearchDiagnostic(SearchDiagnosticSeverity.Warning, $"call-{callNumber}")] };
            return Task.FromResult(result);
        }
    }

    private sealed class FakeWorkspaceRegistry : IWorkspaceRegistry
    {
        private readonly Dictionary<Guid, WorkspaceRegistryEntry> _entries = [];
        private readonly HashSet<Guid> _corrupt = [];

        public void Register(WorkspaceRegistryEntry entry) => _entries[entry.WorkspaceId] = entry;

        public void Remove(Guid workspaceId) => _entries.Remove(workspaceId);

        public void RegisterCorrupt(Guid workspaceId) => _corrupt.Add(workspaceId);

        public Task<WorkspaceRegistryEntry?> ResolveAsync(Guid workspaceId, CancellationToken ct = default)
        {
            if (_corrupt.Contains(workspaceId))
            {
                throw new WorkspaceRegistryCorruptException($"{workspaceId}.json", "simulated corruption");
            }

            return Task.FromResult(_entries.TryGetValue(workspaceId, out var entry) ? entry : null);
        }

        public Task<IReadOnlyList<WorkspaceRegistryEntry>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WorkspaceRegistryEntry>>(_entries.Values.ToList());

        public Task SaveAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
        {
            _entries[entry.WorkspaceId] = entry;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
    {
        private readonly Dictionary<Guid, string?> _fingerprintsByWorkspaceId = [];

        public void Register(Guid workspaceId, string? fingerprint) => _fingerprintsByWorkspaceId[workspaceId] = fingerprint;

        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            Task.FromResult(_fingerprintsByWorkspaceId.GetValueOrDefault(entry.WorkspaceId));
    }

    private sealed class ThrowingWorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
    {
        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            throw new IOException("simulated locked file");
    }
}
