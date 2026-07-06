using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.Logging;

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
    public async Task SearchAsync_FirstCall_LogsCacheMiss()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var logger = new RecordingLogger<CachingFederatedKnowledgeStore>();
        var cache = new CachingFederatedKnowledgeStore(new CountingInnerStore(), registry, fingerprints, workspaceId, new FederatedQueryCache(), logger);

        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Contains(logger.Entries, e => e.Message.Contains("miss", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_RepeatedCall_LogsCacheHit()
    {
        var workspaceId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(workspaceId, "service-a"));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(workspaceId, "fp-a");
        var logger = new RecordingLogger<CachingFederatedKnowledgeStore>();
        var cache = new CachingFederatedKnowledgeStore(new CountingInnerStore(), registry, fingerprints, workspaceId, new FederatedQueryCache(), logger);

        await cache.SearchAsync("term", SearchOptions.Default);
        logger.Entries.Clear();
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Contains(logger.Entries, e => e.Message.Contains("hit", StringComparison.OrdinalIgnoreCase));
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
    public async Task SearchAsync_WhenAFloatingReferencesIndexChangeSignalChanges_InvokesInnerStoreAgain()
    {
        // P3-002 regression test: a floating reference's cache validity must still track its
        // searchable content changing, even though the cheap signal (not the full fingerprint) is
        // what now detects it.
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.RegisterIndexChangeSignal(bId, "index-signal-1");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());
        await cache.SearchAsync("term", SearchOptions.Default);

        fingerprints.RegisterIndexChangeSignal(bId, "index-signal-2");
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_WhenAFloatingReferencesIndexChangeSignalCannotBeComputed_NeverCachesAndAlwaysInvokesInnerStore()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.Register(bId, "fp-b"); // the expensive fingerprint IS resolvable...
        fingerprints.RegisterIndexChangeSignal(bId, signal: null); // ...but no index has been built yet.
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);
        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SearchAsync_ForAFloatingReference_UsesTheCheapIndexChangeSignalNotTheFullFingerprint()
    {
        // Direct regression guard for the P3-002 fix itself: proves the cache key no longer pays the
        // full per-file fingerprint cost for a floating reference (the identified regression), even
        // though the observable invalidation behavior above is unchanged.
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var registry = new FakeWorkspaceRegistry();
        registry.Register(SimpleEntry(bId, "shared-lib"));
        registry.Register(EntryWithReference(aId, "service-a", bId, pinnedStateHash: null));
        var fingerprints = new FakeWorkspaceStateFingerprintProvider();
        fingerprints.Register(aId, "fp-a");
        fingerprints.RegisterIndexChangeSignal(bId, "index-signal");
        var inner = new CountingInnerStore();
        var cache = new CachingFederatedKnowledgeStore(inner, registry, fingerprints, aId, new FederatedQueryCache());

        await cache.SearchAsync("term", SearchOptions.Default);

        Assert.True(fingerprints.WasIndexChangeSignalCheckedFor(bId));
        Assert.False(fingerprints.WasFingerprintedFor(bId));
    }

    [Fact]
    public async Task SearchAsync_ForAPinnedReference_StillUsesTheFullFingerprintNotTheCheapSignal()
    {
        // Inverse regression guard: pinning correctness/drift-detection must keep using the real,
        // portable, content-based fingerprint -- P3-002 must never touch this path.
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

        Assert.True(fingerprints.WasFingerprintedFor(bId));
        Assert.False(fingerprints.WasIndexChangeSignalCheckedFor(bId));
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
        private readonly Dictionary<Guid, string?> _indexChangeSignalsByWorkspaceId = [];
        private readonly HashSet<Guid> _fingerprintCalledFor = [];
        private readonly HashSet<Guid> _indexChangeSignalCalledFor = [];

        public void Register(Guid workspaceId, string? fingerprint) => _fingerprintsByWorkspaceId[workspaceId] = fingerprint;

        /// <summary>Registers a floating reference's cheap change signal independently of its (unused,
        /// expensive) fingerprint -- defaults to mirroring <see cref="Register"/> when not called, so
        /// every pre-P3-002 test that only registers a fingerprint keeps working unmodified.</summary>
        public void RegisterIndexChangeSignal(Guid workspaceId, string? signal) => _indexChangeSignalsByWorkspaceId[workspaceId] = signal;

        public bool WasFingerprintedFor(Guid workspaceId) => _fingerprintCalledFor.Contains(workspaceId);

        public bool WasIndexChangeSignalCheckedFor(Guid workspaceId) => _indexChangeSignalCalledFor.Contains(workspaceId);

        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
        {
            _fingerprintCalledFor.Add(entry.WorkspaceId);
            return Task.FromResult(_fingerprintsByWorkspaceId.GetValueOrDefault(entry.WorkspaceId));
        }

        public Task<string?> ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default)
        {
            _indexChangeSignalCalledFor.Add(entry.WorkspaceId);
            return Task.FromResult(_indexChangeSignalsByWorkspaceId.TryGetValue(entry.WorkspaceId, out var signal)
                ? signal
                : _fingerprintsByWorkspaceId.GetValueOrDefault(entry.WorkspaceId));
        }
    }

    private sealed class ThrowingWorkspaceStateFingerprintProvider : IWorkspaceStateFingerprintProvider
    {
        public Task<string?> ComputeFingerprintAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            throw new IOException("simulated locked file");

        public Task<string?> ComputeIndexChangeSignalAsync(WorkspaceRegistryEntry entry, CancellationToken ct = default) =>
            throw new IOException("simulated locked file");
    }
}
