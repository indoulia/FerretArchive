using Ferret.Cli.Modules;
using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Cli.Tests.Integration;

public sealed class RuntimeLifecycleIntegrationTests
{
    [Fact]
    public async Task Start_ReachesRunning()
    {
        var host = Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        Assert.Equal(RuntimeState.Running, host.State);
        await host.StopAsync(cts.Token);
        if (host is IAsyncDisposable d)
        {
            await d.DisposeAsync();
        }
    }

    [Fact]
    public async Task Stop_AfterStart_ReachesStopped()
    {
        var host = Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        await host.StopAsync(cts.Token);
        Assert.Equal(RuntimeState.Stopped, host.State);
        if (host is IAsyncDisposable d)
        {
            await d.DisposeAsync();
        }
    }

    [Fact]
    public async Task Modules_ContainsDiagnosticsModule()
    {
        var host = Build();
        IModule? module = host.Modules.GetById("ferret.diagnostics");
        Assert.NotNull(module);
        Assert.Equal("Ferret Diagnostics", module!.Metadata.Name);
        if (host is IAsyncDisposable d)
        {
            await d.DisposeAsync();
        }
    }

    private static IRuntimeHost Build() => new RuntimeBuilder()
        .AddModule(new DiagnosticsModule(NullLogger<DiagnosticsModule>.Instance))
        .Build();
}
