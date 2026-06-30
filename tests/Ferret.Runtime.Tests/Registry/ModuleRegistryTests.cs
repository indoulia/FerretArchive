using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

namespace Ferret.Runtime.Tests.Registry;

/// <summary>Tests for <see cref="ModuleRegistry"/>.</summary>
public sealed class ModuleRegistryTests
{
    [Fact]
    public void Modules_ReturnsAllRegistered()
    {
        var a = MakeModule("a");
        var b = MakeModule("b");
        var registry = new ModuleRegistry([a, b]);
        Assert.Equal(2, registry.Modules.Count);
    }

    [Fact]
    public void TryGet_ExistingId_ReturnsTrue()
    {
        var a = MakeModule("a");
        var registry = new ModuleRegistry([a]);
        bool found = registry.TryGet("a", out IModule? m);
        Assert.True(found);
        Assert.Same(a, m);
    }

    [Fact]
    public void TryGet_MissingId_ReturnsFalse()
    {
        var registry = new ModuleRegistry([]);
        bool found = registry.TryGet("x", out IModule? m);
        Assert.False(found);
        Assert.Null(m);
    }

    [Fact]
    public void GetById_ExistingId_ReturnsModule()
    {
        var a = MakeModule("a");
        var registry = new ModuleRegistry([a]);
        Assert.Same(a, registry.GetById("a"));
    }

    [Fact]
    public void GetById_MissingId_ReturnsNull()
    {
        var registry = new ModuleRegistry([]);
        Assert.Null(registry.GetById("missing"));
    }

    private static FakeMod MakeModule(string id)
    {
        var meta = ModuleMetadata.Create(
            id,
            id,
            SemanticVersion.Create(1, 0, 0),
            [ModuleCapability.None],
            string.Empty,
            string.Empty);
        return new FakeMod(meta);
    }

    private sealed class FakeMod(ModuleMetadata m) : DefaultModule(m)
    {
    }
}
