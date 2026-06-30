using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

namespace Ferret.Runtime.Tests.Registry;

public sealed class ModuleDependencyGraphTests
{
    [Fact]
    public void Sort_NoDependencies_PreservesOrder()
    {
        var a = Make("a");
        var b = Make("b");
        var sorted = ModuleDependencyGraph.Sort([a, b]);
        Assert.Equal(["a", "b"], sorted.Select(m => m.Id));
    }

    [Fact]
    public void Sort_ChainDependency_StartsDependencyFirst()
    {
        var b = Make("b", "a");
        var a = Make("a");
        var sorted = ModuleDependencyGraph.Sort([b, a]);
        var ids = sorted.Select(m => m.Id).ToList();
        Assert.True(ids.IndexOf("a") < ids.IndexOf("b"));
    }

    [Fact]
    public void Sort_DiamondDependency_StartsRootFirst()
    {
        // a ← b, a ← c, b ← d, c ← d
        var d = Make("d", "b", "c");
        var b = Make("b", "a");
        var c = Make("c", "a");
        var a = Make("a");
        var sorted = ModuleDependencyGraph.Sort([d, b, c, a]);
        var ids = sorted.Select(m => m.Id).ToList();
        Assert.True(ids.IndexOf("a") < ids.IndexOf("b"));
        Assert.True(ids.IndexOf("a") < ids.IndexOf("c"));
        Assert.True(ids.IndexOf("b") < ids.IndexOf("d"));
        Assert.True(ids.IndexOf("c") < ids.IndexOf("d"));
    }

    [Fact]
    public void Sort_CycleDetected_ThrowsInvalidOperation()
    {
        var a = Make("a", "b");
        var b = Make("b", "a");
        var ex = Assert.Throws<InvalidOperationException>(() => ModuleDependencyGraph.Sort([a, b]));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sort_MissingDependency_ThrowsInvalidOperation()
    {
        var a = Make("a", "missing");
        var ex = Assert.Throws<InvalidOperationException>(() => ModuleDependencyGraph.Sort([a]));
        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sort_EmptyList_ReturnsEmpty()
    {
        Assert.Empty(ModuleDependencyGraph.Sort([]));
    }

    private static DefaultModule Make(string id, params string[] deps)
    {
        var meta = ModuleMetadata.Create(
            id,
            id,
            SemanticVersion.Create(1, 0, 0),
            Array.Empty<ModuleCapability>(),
            string.Empty,
            string.Empty);

        return deps.Length == 0
            ? new NoDepsModule(meta)
            : new DepsModule(meta, deps);
    }

    private sealed class NoDepsModule(ModuleMetadata m) : DefaultModule(m)
    {
    }

    private sealed class DepsModule(ModuleMetadata m, string[] deps) : DefaultModule(m), IModuleWithDependencies
    {
        public IReadOnlyList<string> DependsOn => deps;
    }
}
