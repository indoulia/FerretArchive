using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Persistence.Tests;

/// <summary>
/// S3-1: TDD tests for <see cref="DependencyGraphMaterializer"/>, written against ARCH-037 §4
/// (materialization procedure), §5 (structural invariants), §6 (cycle handling), and §7
/// (unavailable-dependency handling) before any implementation exists. Uses the same
/// <see cref="FileDependencyStateStore"/>-against-a-temp-directory fixture style already
/// established by <see cref="ResolutionCheckTests"/>, not a new fake/mock abstraction.
/// </summary>
public sealed class DependencyGraphMaterializerTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    [Fact]
    public async Task MaterializeAsync_RootHasNoRecord_ProducesSingleUnavailableNode_WithNoEdges()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Parse", "missing.txt", store);

        Assert.Equal(GraphNodeState.Unavailable, graph.Root.State);
        Assert.Null(graph.Root.Record);
        Assert.Single(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public async Task MaterializeAsync_RootHasRecordWithEmptyChain_ProducesSingleResolvedNode_WithNoEdges()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "Parse",
            RequestPath = "a.txt",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Parse", "a.txt", store);

        Assert.Equal(GraphNodeState.Resolved, graph.Root.State);
        Assert.NotNull(graph.Root.Record);
        Assert.Single(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public async Task MaterializeAsync_RootReferencesOneResolvableRecord_ProducesTwoNodes_OneNonCycleEdge()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "SearchIndex",
            RequestPath = "search:leaf",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:leaf" }],
            },
        });

        var graph = await DependencyGraphMaterializer.MaterializeAsync("AssembleContext", "/repo", store);

        Assert.Equal(2, graph.Nodes.Count);
        var edge = Assert.Single(graph.Edges);
        Assert.Same(graph.Root, edge.From);
        Assert.Equal("SearchIndex", edge.To.EngineResponsibility);
        Assert.Equal("search:leaf", edge.To.RequestPath);
        Assert.Equal(GraphNodeState.Resolved, edge.To.State);
        Assert.False(edge.ClosesCycle);
    }

    [Fact]
    public async Task MaterializeAsync_FollowsReferencesTransitively_ThroughMultipleLinks()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "SearchIndex",
            RequestPath = "search:leaf",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/mid",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:leaf" }],
            },
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "Parse",
            RequestPath = "/root",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 3),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "/mid" }],
            },
        });

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Parse", "/root", store);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);
        Assert.All(graph.Edges, e => Assert.False(e.ClosesCycle));
        Assert.Contains(graph.Nodes, n => n.EngineResponsibility == "SearchIndex" && n.RequestPath == "search:leaf" && n.State == GraphNodeState.Resolved);
    }

    [Fact]
    public async Task MaterializeAsync_ReferenceToMissingRecord_PreservesEdge_AsUnavailableNode()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:never-stored" }],
            },
        });

        var graph = await DependencyGraphMaterializer.MaterializeAsync("AssembleContext", "/repo", store);

        Assert.Equal(2, graph.Nodes.Count);
        var edge = Assert.Single(graph.Edges);
        Assert.Equal(GraphNodeState.Unavailable, edge.To.State);
        Assert.Null(edge.To.Record);
        Assert.False(edge.ClosesCycle);
    }

    [Fact]
    public async Task MaterializeAsync_ReferenceCycle_ClosesWithoutRecursingForever_AndReusesTheSameNodeObject()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "A",
            RequestPath = "a",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "B", RequestPath = "b" }] },
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "B",
            RequestPath = "b",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "A", RequestPath = "a" }] },
        });

        var graph = await DependencyGraphMaterializer.MaterializeAsync("A", "a", store);

        // No duplicate nodes (ARCH-037 §5): exactly one node per distinct identity (A, B), never
        // a third node minted for the back-reference to A.
        Assert.Equal(2, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);

        var closingEdge = Assert.Single(graph.Edges, e => e.ClosesCycle);
        Assert.Same(graph.Root, closingEdge.To);
    }

    [Fact]
    public async Task MaterializeAsync_DiamondDependency_SecondArrivalAtSharedNode_IsFlaggedAsCycleClosing()
    {
        // ARCH-037 §5: "a cycle is precisely a second edge arriving at a node that already
        // exists" — this mechanism does not distinguish a true back-edge from a shared
        // (diamond-shaped) dependency reached twice; both are the same structural fact. This is
        // a faithful generalization of ResolutionCheck.CompareLinkAsync's own pre-existing
        // visited-set behavior (ARCH-033), not a new design choice introduced here.
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "D",
            RequestPath = "d",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "B",
            RequestPath = "b",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "D", RequestPath = "d" }] },
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "C",
            RequestPath = "c",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "D", RequestPath = "d" }] },
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "Root",
            RequestPath = "root",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            DependencyChain = new DependencyChain
            {
                References =
                [
                    new DependencyReference { EngineResponsibility = "B", RequestPath = "b" },
                    new DependencyReference { EngineResponsibility = "C", RequestPath = "c" },
                ],
            },
        });

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Root", "root", store);

        // Exactly one node for D (ARCH-037 §5's no-duplicate-nodes invariant) even though two
        // independent edges (Root->B->D and Root->C->D) arrive at it: Root->B, Root->C, B->D,
        // C->D — four edges over four distinct nodes.
        Assert.Equal(4, graph.Nodes.Count);
        Assert.Equal(4, graph.Edges.Count);
        Assert.Single(graph.Edges, e => e.ClosesCycle);

        var edgesToD = graph.Edges.Where(e => e.To.EngineResponsibility == "D").ToList();
        Assert.Equal(2, edgesToD.Count);
        Assert.Same(edgesToD[0].To, edgesToD[1].To);
    }

    [Fact]
    public async Task MaterializeAsync_SameRootAndPersistedState_ProducesStructurallyIdenticalGraph_OnRepeatedMaterialization()
    {
        var store = new FileDependencyStateStore(CreateTempDirectory());
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "SearchIndex",
            RequestPath = "search:leaf",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
            DependencyChain = new DependencyChain
            {
                References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:leaf" }],
            },
        });

        var first = await DependencyGraphMaterializer.MaterializeAsync("AssembleContext", "/repo", store);
        var second = await DependencyGraphMaterializer.MaterializeAsync("AssembleContext", "/repo", store);

        Assert.Equal(first.Nodes.Count, second.Nodes.Count);
        Assert.Equal(first.Edges.Count, second.Edges.Count);
        Assert.Equal(
            first.Nodes.Select(n => (n.EngineResponsibility, n.RequestPath, n.State)).OrderBy(t => t.RequestPath),
            second.Nodes.Select(n => (n.EngineResponsibility, n.RequestPath, n.State)).OrderBy(t => t.RequestPath));
        Assert.Equal(
            first.Edges.Select(e => (e.From.RequestPath, e.To.RequestPath, e.ClosesCycle)),
            second.Edges.Select(e => (e.From.RequestPath, e.To.RequestPath, e.ClosesCycle)));

        // Two independent materializations are two independent object graphs (ARCH-037 §2: never
        // cached, never reused across operations) — same content, never the same instance.
        Assert.NotSame(first, second);
        Assert.NotSame(first.Root, second.Root);
    }

    // ---- S3-2: behavioral proof that materialization never writes (ARCH-037 §4) ----
    //
    // Architecture.Tests can prove IDependencyStateStore's *shape* hasn't changed via reflection,
    // but not that DependencyGraphMaterializer never *calls* SetRecordAsync — that requires
    // actually running the materializer, which belongs here, not in the reflection-only suite.
    // Each test below reuses the exact fixture shape of its TDD counterpart above, substituting
    // ReadOnlySpyDependencyStateStore for FileDependencyStateStore: if materialization ever wrote,
    // the spy would throw and the test would fail at that call site.

    [Fact]
    public async Task MaterializeAsync_EmptyChain_NeverCallsSetRecordAsync()
    {
        var store = new ReadOnlySpyDependencyStateStore(
        [
            new DependencyRecord
            {
                EngineResponsibility = "Parse",
                RequestPath = "a.txt",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            },
        ]);

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Parse", "a.txt", store);

        Assert.Equal(GraphNodeState.Resolved, graph.Root.State);
        Assert.Single(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public async Task MaterializeAsync_TransitiveChain_NeverCallsSetRecordAsync()
    {
        var store = new ReadOnlySpyDependencyStateStore(
        [
            new DependencyRecord
            {
                EngineResponsibility = "SearchIndex",
                RequestPath = "search:leaf",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            },
            new DependencyRecord
            {
                EngineResponsibility = "AssembleContext",
                RequestPath = "/mid",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
                DependencyChain = new DependencyChain
                {
                    References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:leaf" }],
                },
            },
            new DependencyRecord
            {
                EngineResponsibility = "Parse",
                RequestPath = "/root",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 3),
                DependencyChain = new DependencyChain
                {
                    References = [new DependencyReference { EngineResponsibility = "AssembleContext", RequestPath = "/mid" }],
                },
            },
        ]);

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Parse", "/root", store);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.Edges.Count);
    }

    [Fact]
    public async Task MaterializeAsync_ReferenceCycle_NeverCallsSetRecordAsync()
    {
        var store = new ReadOnlySpyDependencyStateStore(
        [
            new DependencyRecord
            {
                EngineResponsibility = "A",
                RequestPath = "a",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
                DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "B", RequestPath = "b" }] },
            },
            new DependencyRecord
            {
                EngineResponsibility = "B",
                RequestPath = "b",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
                DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "A", RequestPath = "a" }] },
            },
        ]);

        var graph = await DependencyGraphMaterializer.MaterializeAsync("A", "a", store);

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Single(graph.Edges, e => e.ClosesCycle);
    }

    [Fact]
    public async Task MaterializeAsync_DiamondDependency_NeverCallsSetRecordAsync()
    {
        var store = new ReadOnlySpyDependencyStateStore(
        [
            new DependencyRecord
            {
                EngineResponsibility = "D",
                RequestPath = "d",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            },
            new DependencyRecord
            {
                EngineResponsibility = "B",
                RequestPath = "b",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
                DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "D", RequestPath = "d" }] },
            },
            new DependencyRecord
            {
                EngineResponsibility = "C",
                RequestPath = "c",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
                DependencyChain = new DependencyChain { References = [new DependencyReference { EngineResponsibility = "D", RequestPath = "d" }] },
            },
            new DependencyRecord
            {
                EngineResponsibility = "Root",
                RequestPath = "root",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
                DependencyChain = new DependencyChain
                {
                    References =
                    [
                        new DependencyReference { EngineResponsibility = "B", RequestPath = "b" },
                        new DependencyReference { EngineResponsibility = "C", RequestPath = "c" },
                    ],
                },
            },
        ]);

        var graph = await DependencyGraphMaterializer.MaterializeAsync("Root", "root", store);

        Assert.Equal(4, graph.Nodes.Count);
        Assert.Equal(4, graph.Edges.Count);
    }

    [Fact]
    public async Task MaterializeAsync_UnavailableReference_NeverCallsSetRecordAsync()
    {
        var store = new ReadOnlySpyDependencyStateStore(
        [
            new DependencyRecord
            {
                EngineResponsibility = "AssembleContext",
                RequestPath = "/repo",
                SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
                DependencyChain = new DependencyChain
                {
                    References = [new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:never-stored" }],
                },
            },
        ]);

        var graph = await DependencyGraphMaterializer.MaterializeAsync("AssembleContext", "/repo", store);

        Assert.Equal(2, graph.Nodes.Count);
        var edge = Assert.Single(graph.Edges);
        Assert.Equal(GraphNodeState.Unavailable, edge.To.State);
    }

    // Proves ReadOnlySpyDependencyStateStore is actually armed — that the five
    // "NeverCallsSetRecordAsync" tests above are passing because materialization never writes,
    // not because the spy would silently accept a write. Without this, a passing
    // "NeverCallsSetRecordAsync" test would be exactly as uninformative as a vacuously-passing
    // architecture check (the same failure mode S2-0/S3-0 guarded against by observing their red
    // checks fail before relying on them).
    [Fact]
    public async Task ReadOnlySpyDependencyStateStore_SetRecordAsync_AlwaysThrows()
    {
        var store = new ReadOnlySpyDependencyStateStore([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "Parse",
            RequestPath = "a.txt",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        }));
    }

    private string CreateTempDirectory()
    {
        var path = Path.Join(Path.GetTempPath(), $"ferret-dependency-graph-test-{Guid.NewGuid():N}");
        _tempDirectories.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    /// <summary>
    /// A minimal, seedable <see cref="IDependencyStateStore"/> whose <see cref="SetRecordAsync"/>
    /// throws unconditionally — proves materialization is read-only by observable behavior rather
    /// than by inspecting generated code shape (ARCH-036 §1: conformance is judged by observable
    /// behavior, never by the technology or technique that produced it). Seeded directly through
    /// its constructor, never through <see cref="SetRecordAsync"/>, since that method must never be
    /// exercised as a normal write path in these tests.
    /// </summary>
    private sealed class ReadOnlySpyDependencyStateStore : IDependencyStateStore
    {
        private readonly Dictionary<(string EngineResponsibility, string RequestPath), DependencyRecord> _records;

        public ReadOnlySpyDependencyStateStore(IEnumerable<DependencyRecord> seedRecords)
        {
            _records = seedRecords.ToDictionary(r => (r.EngineResponsibility, r.RequestPath));
        }

        public ValueTask<DependencyRecord?> GetRecordAsync(string engineResponsibility, string requestPath, CancellationToken ct = default) =>
            ValueTask.FromResult(_records.TryGetValue((engineResponsibility, requestPath), out var record) ? record : null);

        public Task SetRecordAsync(DependencyRecord record, CancellationToken ct = default) =>
            throw new InvalidOperationException(
                $"DependencyGraphMaterializer must never call SetRecordAsync (ARCH-037 §4) — attempted to write {record.EngineResponsibility}/{record.RequestPath}.");
    }
}
