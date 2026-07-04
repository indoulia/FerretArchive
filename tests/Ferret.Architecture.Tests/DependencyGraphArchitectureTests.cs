using System.Reflection;

using Ferret.Persistence;

using Xunit;

namespace Ferret.Architecture.Tests;

/// <summary>
/// Dependency Graph Architecture Regression Tests: encodes the ARCH-037 invariants named in the
/// Sprint 3 Architecture Review (docs/superpowers/plans/2026-07-04-v2-sprint-3-architecture-review.md
/// §4.1, §4.3) as permanent, reflection-only checks — same style as
/// <see cref="PersistenceArchitectureTests"/> and <see cref="ConnectorArchitectureTests"/>; no IL
/// inspection, no second test framework.
///
/// This file's tests have moved through three roles as the mechanism was built, without any test
/// being weakened or replaced along the way:
/// <list type="bullet">
/// <item><b>S3-0 (construction tests):</b> <c>DependencyGraph</c>, <c>GraphNode</c>, and
/// <c>GraphEdge</c> did not exist yet in <c>Ferret.Persistence</c> (ARCH-037 §1). These tests
/// failed by design — that failure was the observable proof the suite actually detects the
/// mechanism's absence, rather than passing vacuously once nothing existed to check. The
/// `IDependencyStateStore`/`ResolutionCheck` contract-protection tests were green from the start,
/// guarding Sprint 2's contracts while S3-1 was built.</item>
/// <item><b>S3-1 (acceptance criteria):</b> once the graph types and materialization procedure
/// existed, the same tests became the acceptance gate S3-1 had to turn green without any test
/// being edited.</item>
/// <item><b>S3-2 onward (permanent architectural invariants):</b> the same tests, now green,
/// stand as regression protection against future erosion (e.g., a later change adding a validity
/// field to <c>GraphNode</c>). S3-2 adds two new invariants (sealed types; a closed
/// <c>GraphNodeState</c> enum) and strengthens one existing check to also cover
/// <c>DependencyGraph</c> itself — strictly additive, per the standing rule that an approved
/// invariant may be broadened but never weakened or replaced without an explicit architecture
/// decision superseding it.</item>
/// </list>
/// </summary>
public sealed class DependencyGraphArchitectureTests
{
    private static readonly Assembly PersistenceAssembly = typeof(IDependencyStateStore).Assembly;

    private static readonly string[] DisallowedStorageTechnologyNamespacePrefixes =
    {
        "System.IO",
        "System.Data",
        "Microsoft.Data",
        "System.Text.Json",
    };

    private static readonly string[] ExpectedDependencyStateStoreMethodNames = { "GetRecordAsync", "SetRecordAsync" };

    private static readonly string[] ExpectedResolutionCheckMethodNames =
    {
        "Combine", "Compare", "CompareChainAsync", "CompareConfiguration",
    };

    private static readonly string[] ExpectedDependencyStateStoreImplementations =
    {
        "FileDependencyStateStore", "SpikeDependencyStateStore",
    };

    private static readonly string[] ExpectedGraphNodeStateValues = { "Resolved", "Unavailable" };

    // ---- RED today: existence gate for the S3-1 mechanism (ARCH-037 §1) ----

    /// <summary>DependencyGraph must exist in Ferret.Persistence once S3-1 lands (ARCH-037 §1).</summary>
    [Fact]
    public void DependencyGraph_Type_Must_Exist()
    {
        RequireGraphType("DependencyGraph");
    }

    /// <summary>GraphNode must exist in Ferret.Persistence once S3-1 lands (ARCH-037 §1).</summary>
    [Fact]
    public void GraphNode_Type_Must_Exist()
    {
        RequireGraphType("GraphNode");
    }

    /// <summary>GraphEdge must exist in Ferret.Persistence once S3-1 lands (ARCH-037 §1).</summary>
    [Fact]
    public void GraphEdge_Type_Must_Exist()
    {
        RequireGraphType("GraphEdge");
    }

    // ---- RED today: structural invariants the S3-1 types must satisfy once they exist ----

    /// <summary>
    /// DependencyGraph, GraphNode, and GraphEdge must carry no resolution or validity judgment —
    /// ARCH-037 §5's "No derived semantic state" invariant. Originally scoped to GraphNode/GraphEdge
    /// only during S3-0/S3-1; S3-2 strengthens this to also scan DependencyGraph itself, since §5's
    /// invariant applies to all three types and DependencyGraph was previously unchecked. This is a
    /// strict broadening (one more type added to the scan) — the original GraphNode/GraphEdge checks
    /// are unchanged.
    /// </summary>
    [Fact]
    public void DependencyGraph_GraphNode_And_GraphEdge_Must_Not_Carry_Resolution_Or_Validity_Vocabulary()
    {
        foreach (var typeName in new[] { "DependencyGraph", "GraphNode", "GraphEdge" })
        {
            var type = RequireGraphType(typeName);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var referencedTypes = FlattenTypeArguments(property.PropertyType).ToList();
                Assert.DoesNotContain(typeof(ResolutionOutcome), referencedTypes);
            }
        }
    }

    /// <summary>
    /// DependencyGraph, GraphNode, and GraphEdge must be immutable once constructed — ARCH-037
    /// §5's "Immutable graph" invariant. Get-only and init-only properties are permitted (the same
    /// shape already used by <see cref="DependencyRecord"/>, <see cref="DependencyReference"/>, and
    /// <see cref="DependencyChain"/>); a publicly mutable setter is not.
    /// </summary>
    [Fact]
    public void Graph_Types_Must_Be_Immutable()
    {
        foreach (var typeName in new[] { "DependencyGraph", "GraphNode", "GraphEdge" })
        {
            var type = RequireGraphType(typeName);
            var violations = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => HasPubliclyMutableSetter(p))
                .Select(p => p.Name)
                .ToList();

            Assert.True(
                violations.Count == 0,
                $"{typeName} must be immutable (get-only or init-only). Mutable properties: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// The graph mechanism must remain independent of storage technology — ARCH-037 §4 realizes
    /// materialization purely against <see cref="IDependencyStateStore"/>, naming no file format,
    /// database, or serialization type anywhere in its own vocabulary. This is the graph-mechanism
    /// analogue of <see cref="PersistenceArchitectureTests.IDependencyStateStore_Methods_Must_Not_Reference_Storage_Technology_Types"/>.
    /// </summary>
    [Fact]
    public void Graph_Types_Must_Not_Reference_Storage_Technology_Types()
    {
        foreach (var typeName in new[] { "DependencyGraph", "GraphNode", "GraphEdge" })
        {
            var type = RequireGraphType(typeName);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var referenced in FlattenTypeArguments(property.PropertyType))
                {
                    var ns = referenced.Namespace ?? string.Empty;
                    var isDisallowed = DisallowedStorageTechnologyNamespacePrefixes
                        .Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal));

                    Assert.False(
                        isDisallowed,
                        $"{typeName}.{property.Name} references storage-technology type {referenced.FullName}.");
                }
            }
        }
    }

    // ---- S3-2: new permanent invariants, guarding future erosion ----

    /// <summary>
    /// DependencyGraph, GraphNode, and GraphEdge must stay sealed — prevents a future subclass
    /// from quietly adding a field (e.g. a derived GraphNode carrying a validity flag), which would
    /// reopen ARCH-037 §5's "No derived semantic state" invariant through inheritance instead of a
    /// directly-added property. Same rationale as
    /// <see cref="ConnectorArchitectureTests.IConnector_Implementations_Must_Be_Sealed"/> elsewhere
    /// in this repository.
    /// </summary>
    [Fact]
    public void Graph_Types_Must_Be_Sealed()
    {
        foreach (var typeName in new[] { "DependencyGraph", "GraphNode", "GraphEdge" })
        {
            var type = RequireGraphType(typeName);
            Assert.True(type.IsSealed, $"{typeName} must remain sealed.");
        }
    }

    /// <summary>
    /// GraphNodeState must remain exactly { Resolved, Unavailable } — ARCH-037 §1's "and nothing
    /// else." A third value (e.g. "Conflicted" or "PartiallyResolved") would be derived semantic
    /// state smuggled in through an enum member instead of a property, which
    /// <see cref="DependencyGraph_GraphNode_And_GraphEdge_Must_Not_Carry_Resolution_Or_Validity_Vocabulary"/>
    /// cannot detect on its own since it only inspects property types, not enum membership.
    /// </summary>
    [Fact]
    public void GraphNodeState_Must_Have_Exactly_Two_Values()
    {
        var enumType = RequireGraphType("GraphNodeState");
        var values = Enum.GetNames(enumType).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(ExpectedGraphNodeStateValues, values);
    }

    // ---- GREEN today: existing contracts must not move while S3-1 is built ----

    /// <summary>
    /// IDependencyStateStore must remain exactly GetRecordAsync/SetRecordAsync — ARCH-037 §4:
    /// "no new interface, storage call, or query shape is introduced." A graph mechanism that adds
    /// a third method here would be persistence reaching into the graph's territory, or vice versa.
    /// </summary>
    [Fact]
    public void IDependencyStateStore_Must_Remain_Unchanged()
    {
        var methodNames = typeof(IDependencyStateStore).GetMethods()
            .Select(m => m.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedDependencyStateStoreMethodNames, methodNames);
    }

    /// <summary>
    /// ResolutionCheck's public surface must remain exactly Compare/CompareConfiguration/Combine/
    /// CompareChainAsync — ARCH-037 §9: "ResolutionCheck.CompareChainAsync's existing private
    /// traversal... is not required to change as a result of this document." S3-0 through S3-2
    /// build the graph mechanism beside resolution, not into it.
    /// </summary>
    [Fact]
    public void ResolutionCheck_Public_Contract_Must_Remain_Unchanged()
    {
        var methodNames = typeof(ResolutionCheck)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var expected = ExpectedResolutionCheckMethodNames
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, methodNames);
    }

    /// <summary>
    /// IDependencyStateStore must have exactly the two known, already-approved implementations —
    /// no graph-shaped persistence or caching backend may register itself under this interface
    /// (ARCH-037 §2, §9: a graph is "not persisted, cached, or treated as a new source of truth").
    /// </summary>
    [Fact]
    public void IDependencyStateStore_Must_Have_No_Implementations_Beyond_Known_Stores()
    {
        var implementors = PersistenceAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IDependencyStateStore).IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedDependencyStateStoreImplementations, implementors);
    }

    private static Type RequireGraphType(string simpleName)
    {
        var type = PersistenceAssembly.GetType($"Ferret.Persistence.{simpleName}");
        Assert.True(
            type is not null,
            $"Ferret.Persistence.{simpleName} does not exist yet (ARCH-037 §1) — this is the S3-1 target this baseline establishes.");
        return type!;
    }

    private static bool HasPubliclyMutableSetter(PropertyInfo property)
    {
        var setMethod = property.SetMethod;
        var hasPublicSetter = setMethod?.IsPublic ?? false;
        var isInitOnly = setMethod?.ReturnParameter?.GetRequiredCustomModifiers()
            .Any(m => m.Name == "IsExternalInit") ?? false;
        return hasPublicSetter && !isInitOnly;
    }

    private static IEnumerable<Type> FlattenTypeArguments(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        yield return underlying;

        if (!underlying.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in underlying.GetGenericArguments())
        {
            foreach (var nested in FlattenTypeArguments(argument))
            {
                yield return nested;
            }
        }
    }
}
