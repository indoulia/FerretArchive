using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

namespace Ferret.Runtime.Tests.Registry;

public sealed class ModuleDescriptorStoreTests
{
    [Fact]
    public void GetAll_Empty_ReturnsEmpty()
    {
        var store = new ModuleDescriptorStore();
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Add_PlainDescriptor_WrapsInBoundModule()
    {
        var store = new ModuleDescriptorStore();
        store.Add(new PlainDescriptor("a"));
        var all = store.GetAll();
        Assert.Single(all);
        Assert.IsType<BoundModule>(all[0]);
    }

    [Fact]
    public void Add_DefaultModuleSubclass_KeptAsIs()
    {
        var store = new ModuleDescriptorStore();
        var fakeMod = new FakeMod();
        store.Add(fakeMod);
        var all = store.GetAll();
        Assert.Single(all);
        Assert.IsType<FakeMod>(all[0]);
        Assert.Same(fakeMod, all[0]);
    }

    [Fact]
    public void Add_DuplicateId_ThrowsInvalidOperation()
    {
        var store = new ModuleDescriptorStore();
        store.Add(new PlainDescriptor("dup"));
        var ex = Assert.Throws<InvalidOperationException>(() => store.Add(new PlainDescriptor("dup")));
        Assert.Contains("dup", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Add_NullDescriptor_ThrowsArgumentNull()
    {
        var store = new ModuleDescriptorStore();
        Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    }

    [Fact]
    public void Add_MultipleDistinctDescriptors_AllAdded()
    {
        var store = new ModuleDescriptorStore();
        store.Add(new PlainDescriptor("a"));
        store.Add(new PlainDescriptor("b"));
        store.Add(new FakeMod());

        var all = store.GetAll();
        Assert.Equal(3, all.Count);
        Assert.IsType<BoundModule>(all[0]);
        Assert.IsType<BoundModule>(all[1]);
        Assert.IsType<FakeMod>(all[2]);
    }

    [Fact]
    public void Add_PreservesRegistrationOrder()
    {
        var store = new ModuleDescriptorStore();
        var desc1 = new PlainDescriptor("first");
        var desc2 = new PlainDescriptor("second");
        var desc3 = new FakeMod();

        store.Add(desc1);
        store.Add(desc2);
        store.Add(desc3);

        var all = store.GetAll();
        Assert.Equal("first", all[0].Id);
        Assert.Equal("second", all[1].Id);
        Assert.Equal("fake", all[2].Id);
    }

    private sealed class PlainDescriptor : IModuleDescriptor
    {
        public PlainDescriptor(string id) => Id = id;

        public string Id { get; }

        public string Name => Id;

        public SemanticVersion Version => SemanticVersion.Create(1, 0, 0);

        public IReadOnlyCollection<ModuleCapability> Capabilities => new[] { ModuleCapability.None };
    }

    private sealed class FakeMod : DefaultModule
    {
        public FakeMod()
            : base(ModuleMetadata.Create(
                "fake",
                "Fake",
                SemanticVersion.Create(1, 0, 0),
                new[] { ModuleCapability.None },
                string.Empty,
                string.Empty))
        {
        }
    }
}
