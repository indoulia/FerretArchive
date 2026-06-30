using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class AboutCommandHandlerTests
{
    [Fact]
    public async Task About_ExitsZero() =>
        Assert.Equal(0, await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(["about"]));

    [Fact]
    public async Task About_PrintsProductName()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["about"]);
        Assert.Contains("Ferret", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task About_PrintsTagline()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["about"]);
        Assert.Contains("Dig Deep", sw.ToString(), StringComparison.Ordinal);
    }
}
