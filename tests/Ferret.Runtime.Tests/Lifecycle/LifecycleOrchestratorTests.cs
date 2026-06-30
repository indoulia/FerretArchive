using Ferret.Core.Abstractions;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Lifecycle;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;
using Ferret.Runtime.Tests.Fakes;

using Microsoft.Extensions.Logging.Abstractions;

using RuntimeExecutionContext = Ferret.Runtime.Lifecycle.ExecutionContext;
using RuntimeModuleContext = Ferret.Runtime.Lifecycle.ModuleContext;

namespace Ferret.Runtime.Tests.Lifecycle;

/// <summary>Tests for <see cref="LifecycleOrchestrator"/>.</summary>
public sealed class LifecycleOrchestratorTests
{
    [Fact]
    public async Task StartModuleAsync_CallsOnStartingAndOnStarted()
    {
        var module = new FakeModule("m");
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await orchestrator.StartModuleAsync(module, ctx, CancellationToken.None);

        Assert.Equal(1, module.OnStartingCalls);
        Assert.Equal(1, module.OnStartedCalls);
        Assert.Equal(ModuleState.Active, module.State);
    }

    [Fact]
    public async Task StopModuleAsync_CallsOnStoppingAndOnStopped()
    {
        var module = new FakeModule("m");
        module.SetState(ModuleState.Active);
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await orchestrator.StopModuleAsync(module, ctx, CancellationToken.None);

        Assert.Equal(1, module.OnStoppingCalls);
        Assert.Equal(1, module.OnStoppedCalls);
        Assert.Equal(ModuleState.Stopped, module.State);
    }

    [Fact]
    public async Task StartModuleAsync_WhenOnStartingThrows_SetsFaulted()
    {
        var module = new FakeModule("m", startException: new InvalidOperationException("fail"));
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.StartModuleAsync(module, ctx, CancellationToken.None));

        Assert.Equal(ModuleState.Faulted, module.State);
    }

    [Fact]
    public async Task StartModuleAsync_IInitializable_CallsInitialize()
    {
        var module = new FakeInitializableModule("init");
        var ctx = MakeContext(module);
        var orchestrator = new LifecycleOrchestrator(NullLogger<LifecycleOrchestrator>.Instance);

        await orchestrator.StartModuleAsync(module, ctx, CancellationToken.None);

        Assert.True(module.InitializeCalled);
    }

    private static RuntimeModuleContext MakeContext(DefaultModule module)
    {
        var registry = new ModuleRegistry([module]);
        var execCtx = new RuntimeExecutionContext(
            CorrelationId.Create("system"),
            ExecutionId.Create("exec-1"),
            CancellationToken.None);
        return new RuntimeModuleContext(module, execCtx, registry);
    }

    private sealed class FakeInitializableModule : DefaultModule, IInitializable
    {
        public FakeInitializableModule(string id)
            : base(ModuleMetadata.Create(
                id,
                id,
                SemanticVersion.Create(1, 0, 0),
                Array.Empty<ModuleCapability>(),
                string.Empty,
                string.Empty))
        {
        }

        public bool InitializeCalled { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }
    }
}
