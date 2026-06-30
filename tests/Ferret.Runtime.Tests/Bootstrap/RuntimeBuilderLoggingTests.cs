using Ferret.Core.Runtime;
using Ferret.Runtime.Bootstrap;
using Microsoft.Extensions.Logging;

namespace Ferret.Runtime.Tests.Bootstrap;

public sealed class RuntimeBuilderLoggingTests
{
    [Fact]
    public void ConfigureLogging_ReturnsSameBuilder()
    {
        var builder = new RuntimeBuilder();
        var returned = builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Debug));
        Assert.Same(builder, returned);
    }

    [Fact]
    public async Task Build_WithConfigureLogging_StartsAndStopsWithoutError()
    {
        IRuntimeHost host = new RuntimeBuilder()
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning))
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await host.StartAsync(cts.Token);
        await host.StopAsync(cts.Token);
        if (host is IAsyncDisposable d)
        {
            await d.DisposeAsync();
        }
    }
}
