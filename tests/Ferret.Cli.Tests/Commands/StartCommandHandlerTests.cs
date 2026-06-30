using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Handlers;
using Ferret.Cli.Infrastructure;
using Xunit;

namespace Ferret.Cli.Tests.Commands;

[Collection("StartHandlerTests")]
public sealed class StartCommandHandlerTests : IDisposable
{
    [Fact]
    public async Task Start_CancelsCleanly_ExitsZero()
    {
        Arm(out var cts);
        using (cts)
        {
            Assert.Equal(0, await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(["start"]));
        }
    }

    [Fact]
    public async Task Start_PrintsBanner()
    {
        Arm(out var cts);
        using (cts)
        {
            using var sw = new StringWriter();
            await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["start"]);
            Assert.Contains($"Ferret {FerretPlatform.Version}", sw.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Start_PrintsRuntimeReady()
    {
        Arm(out var cts);
        using (cts)
        {
            using var sw = new StringWriter();
            await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["start"]);
            Assert.Contains("Runtime ready", sw.ToString(), StringComparison.Ordinal);
        }
    }

    public void Dispose() => StartCommandHandler.TestCancellationToken = CancellationToken.None;

    private static void Arm(out CancellationTokenSource cts)
    {
        cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(2000));
        StartCommandHandler.TestCancellationToken = cts.Token;
    }
}
