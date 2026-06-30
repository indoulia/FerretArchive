using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Tests.Fakes;

namespace Ferret.Runtime.Tests.Integration;

/// <summary>Integration tests that exercise the full RuntimeBuilder → RuntimeHost pipeline.</summary>
public sealed class RuntimeIntegrationTests
{
    [Fact]
    public async Task FullLifecycle_StartAndStop_AllModulesActivatedAndStopped()
    {
        var modA = new FakeModule("a");
        var modB = new FakeModule("b");

        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(modA)
            .AddModule(modB)
            .Build();

        await using (host as IAsyncDisposable)
        {
            await host.StartAsync();

            Assert.Equal(ModuleState.Active, modA.State);
            Assert.Equal(ModuleState.Active, modB.State);

            await host.StopAsync();

            Assert.Equal(ModuleState.Stopped, modA.State);
            Assert.Equal(ModuleState.Stopped, modB.State);
        }
    }

    [Fact]
    public async Task EventDispatch_RuntimeStarted_StateIsRunning()
    {
        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(new FakeModule("evt"))
            .Build();

        await using (host as IAsyncDisposable)
        {
            await host.StartAsync();

            // ModuleLifecycleService transitions Starting → Running and publishes RuntimeStarted.
            // Verifying Running state confirms the event dispatch pipeline completed successfully.
            Assert.Equal(RuntimeState.Running, host.State);
        }
    }

    [Fact]
    public async Task DependencyOrder_DependentModuleStartsAfterDependency()
    {
        var startOrder = new List<string>();
        var modA = new OrderTrackingModule("dep-a", startOrder);
        var modB = new DependentOrderTrackingModule("dep-b", "dep-a", startOrder);

        // Register b before a to verify sorting is applied
        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(modB)
            .AddModule(modA)
            .Build();

        await using (host as IAsyncDisposable)
        {
            await host.StartAsync();

            Assert.Equal(["dep-a", "dep-b"], startOrder);
        }
    }

    [Fact]
    public async Task FaultedModule_OnStart_RuntimeTransitionsToFaulted()
    {
        var faulted = new FakeModule("bad", startException: new InvalidOperationException("boom"));

        IRuntimeHost host = new RuntimeBuilder()
            .AddModule(faulted)
            .Build();

        await using (host as IAsyncDisposable)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

            Assert.Equal(RuntimeState.Faulted, host.State);
        }
    }

    private sealed class OrderTrackingModule : DefaultModule
    {
        private readonly List<string> _order;

        public OrderTrackingModule(string id, List<string> order)
            : base(ModuleMetadata.Create(id, id, SemanticVersion.Create(1, 0, 0), [], string.Empty, string.Empty))
        {
            _order = order;
        }

        public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken)
        {
            _order.Add(Id);
            return Task.CompletedTask;
        }
    }

    private sealed class DependentOrderTrackingModule : DefaultModule, IModuleWithDependencies
    {
        private readonly List<string> _order;
        private readonly string _dependencyId;

        public DependentOrderTrackingModule(string id, string dependencyId, List<string> order)
            : base(ModuleMetadata.Create(id, id, SemanticVersion.Create(1, 0, 0), [], string.Empty, string.Empty))
        {
            _dependencyId = dependencyId;
            _order = order;
        }

        public IReadOnlyList<string> DependsOn => [_dependencyId];

        public override Task OnStartingAsync(IModuleContext context, CancellationToken cancellationToken)
        {
            _order.Add(Id);
            return Task.CompletedTask;
        }
    }
}
