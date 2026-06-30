using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;

namespace Ferret.Runtime.Tests.Modules;

public sealed class BoundModuleTests
{
    [Fact]
    public void BoundModule_WithPlainDescriptor_HasExpectedId()
    {
        var bound = new BoundModule(new PlainDescriptor());
        Assert.Equal("plain", bound.Id);
        Assert.Equal(ModuleState.Unloaded, bound.State);
    }

    [Fact]
    public async Task BoundModule_WithIModule_DelegatesOnStarting()
    {
        var inner = new FullModule();
        var bound = new BoundModule(inner);
        await bound.OnStartingAsync(null!, CancellationToken.None);
        Assert.Equal(1, inner.StartingCalls);
    }

    [Fact]
    public async Task BoundModule_WithPlainDescriptor_OnStarting_IsNoOp()
    {
        var bound = new BoundModule(new PlainDescriptor());
        await bound.OnStartingAsync(null!, CancellationToken.None); // no exception
    }

    private sealed class PlainDescriptor : IModuleDescriptor
    {
        public string Id => "plain";

        public string Name => "Plain";

        public SemanticVersion Version => SemanticVersion.Create(1, 0, 0);

        public IReadOnlyCollection<ModuleCapability> Capabilities => Array.Empty<ModuleCapability>();
    }

    private sealed class FullModule : IModule, IModuleDescriptor
    {
        public string Id => "full";

        public string Name => "Full";

        public SemanticVersion Version => SemanticVersion.Create(1, 0, 0);

        public IReadOnlyCollection<ModuleCapability> Capabilities => Array.Empty<ModuleCapability>();

        public ModuleMetadata Metadata => ModuleMetadata.Create(Id, Name, Version, Capabilities, string.Empty, string.Empty);

        public ModuleState State => ModuleState.Unloaded;

        public int StartingCalls { get; private set; }

        public Task OnStartingAsync(IModuleContext ctx, CancellationToken ct)
        {
            StartingCalls++;
            return Task.CompletedTask;
        }

        public Task OnStartedAsync(IModuleContext ctx, CancellationToken ct) => Task.CompletedTask;

        public Task OnStoppingAsync(IModuleContext ctx, CancellationToken ct) => Task.CompletedTask;

        public Task OnStoppedAsync(IModuleContext ctx, CancellationToken ct) => Task.CompletedTask;
    }
}
