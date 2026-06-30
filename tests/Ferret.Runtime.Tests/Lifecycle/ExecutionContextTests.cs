using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Runtime.Modules;
using Ferret.Runtime.Registry;

using RuntimeExecutionContext = Ferret.Runtime.Lifecycle.ExecutionContext;
using RuntimeModuleContext = Ferret.Runtime.Lifecycle.ModuleContext;

namespace Ferret.Runtime.Tests.Lifecycle;

/// <summary>Tests for <see cref="RuntimeExecutionContext"/> and <see cref="RuntimeModuleContext"/>.</summary>
public sealed class ExecutionContextTests
{
    private static readonly CorrelationId TestCorrelation = CorrelationId.Create("corr-1");
    private static readonly ExecutionId TestExecution = ExecutionId.Create("exec-1");

    [Fact]
    public void ExecutionContext_ImplementsIExecutionContext()
    {
        var ctx = MakeExecutionContext();
        Assert.IsAssignableFrom<IExecutionContext>(ctx);
    }

    [Fact]
    public void ExecutionContext_CorrelationId_RoundTrips()
    {
        var ctx = MakeExecutionContext();
        Assert.Equal(TestCorrelation, ctx.CorrelationId);
    }

    [Fact]
    public void ExecutionContext_ExecutionId_RoundTrips()
    {
        var ctx = MakeExecutionContext();
        Assert.Equal(TestExecution, ctx.ExecutionId);
    }

    [Fact]
    public void ExecutionContext_CancellationToken_RoundTrips()
    {
        using var cts = new CancellationTokenSource();
        var ctx = new RuntimeExecutionContext(TestCorrelation, TestExecution, cts.Token);
        Assert.Equal(cts.Token, ctx.CancellationToken);
    }

    [Fact]
    public void ModuleContext_ImplementsIModuleContext()
    {
        var (ctx, _, _) = MakeModuleContext();
        Assert.IsAssignableFrom<IModuleContext>(ctx);
    }

    [Fact]
    public void ModuleContext_Registry_ReturnsSameInstance()
    {
        var (ctx, _, registry) = MakeModuleContext();
        Assert.Same(registry, ctx.Registry);
    }

    [Fact]
    public void ModuleContext_ExecutionContext_ReturnsSameInstance()
    {
        var (ctx, execCtx, _) = MakeModuleContext();
        Assert.Same(execCtx, ctx.ExecutionContext);
    }

    [Fact]
    public void ModuleContext_ModuleId_MatchesModuleMetadataId()
    {
        var (ctx, _, _) = MakeModuleContext();
        Assert.Equal("test-module", ctx.ModuleId);
    }

    private static RuntimeExecutionContext MakeExecutionContext() =>
        new(TestCorrelation, TestExecution, CancellationToken.None);

    private static (RuntimeModuleContext Ctx, IExecutionContext ExecCtx, IModuleRegistry Registry) MakeModuleContext()
    {
        var meta = ModuleMetadata.Create(
            "test-module",
            "Test Module",
            SemanticVersion.Create(1, 0, 0),
            Array.Empty<ModuleCapability>(),
            string.Empty,
            string.Empty);
        var module = new FakeMod(meta);
        var registry = new ModuleRegistry([module]);
        var execCtx = MakeExecutionContext();
        var ctx = new RuntimeModuleContext(module, execCtx, registry);
        return (ctx, execCtx, registry);
    }

    private sealed class FakeMod(ModuleMetadata m) : DefaultModule(m)
    {
    }
}
