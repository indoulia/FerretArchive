using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Tests.Modules;

public sealed class DefaultModuleTests
{
    [Fact]
    public void State_Initial_IsUnloaded()
    {
        var m = new ConcreteModule();
        Assert.Equal(ModuleState.Unloaded, m.State);
    }

    [Fact]
    public void SetState_ChangesState()
    {
        var m = new ConcreteModule();
        m.SetState(ModuleState.Active);
        Assert.Equal(ModuleState.Active, m.State);
    }

    [Fact]
    public void MetadataProperties_DelegateToMetadata()
    {
        var m = new ConcreteModule();
        Assert.Equal("test", m.Id);
        Assert.Equal("Test", m.Name);
        Assert.Equal(SemanticVersion.Create(1, 0, 0), m.Version);
        Assert.Empty(m.Capabilities);
    }

    [Fact]
    public async Task OnStartingAsync_Override_FiresAndReturnsCompleted()
    {
        var m = new ConcreteModule();
        await m.OnStartingAsync(null!, CancellationToken.None);
        Assert.Equal(1, m.StartingCalls);
    }

    private sealed class ConcreteModule : DefaultModule
    {
        public ConcreteModule()
            : base(ModuleMetadata.Create(
                "test",
                "Test",
                SemanticVersion.Create(1, 0, 0),
                Array.Empty<ModuleCapability>(),
                string.Empty,
                string.Empty))
        {
        }

        public int StartingCalls { get; private set; }

        public int StartedCalls { get; private set; }

        public override Task OnStartingAsync(IModuleContext ctx, CancellationToken ct)
        {
            StartingCalls++;
            return Task.CompletedTask;
        }

        public override Task OnStartedAsync(IModuleContext ctx, CancellationToken ct)
        {
            StartedCalls++;
            return Task.CompletedTask;
        }
    }
}
