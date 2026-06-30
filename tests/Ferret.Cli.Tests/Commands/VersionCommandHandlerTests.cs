using Ferret.Cli.Commands;
using Ferret.Cli.Infrastructure;

namespace Ferret.Cli.Tests.Commands;

public sealed class VersionCommandHandlerTests
{
    [Fact]
    public async Task Version_ExitsZero() =>
        Assert.Equal(0, await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(["version"]));

    [Fact]
    public async Task Version_PrintsAssemblyVersion()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["version"]);
        Assert.Contains($"Ferret {FerretPlatform.Version}", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Version_PrintsPoweredBy()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["version"]);
        Assert.Contains("Powered by ContextOS", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Version_PrintsRuntimeInfo()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["version"]);
        Assert.Contains(".NET", sw.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
