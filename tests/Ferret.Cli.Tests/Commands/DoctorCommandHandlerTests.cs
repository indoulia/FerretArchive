using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class DoctorCommandHandlerTests
{
    [Fact]
    public async Task Doctor_ExitsWithoutException() =>
        Assert.InRange(await RootCommandFactory.Build([new CoreCliModule()]).InvokeAsync(["doctor"]), 0, 1);

    [Fact]
    public async Task Doctor_PrintsHeader()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        Assert.Contains("Ferret Doctor", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_PrintsChecks()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        string output = sw.ToString();
        Assert.Contains("Configuration loaded", output, StringComparison.Ordinal);
        Assert.Contains("Runtime lifecycle", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_PrintsConclusion()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        string output = sw.ToString();
        Assert.True(
            output.Contains("Ferret is healthy", StringComparison.Ordinal)
            || output.Contains("Ferret has issues", StringComparison.Ordinal),
            $"Expected conclusion line in output: {output}");
    }

    [Fact]
    public async Task Doctor_PrintsWorkspaceRootCheck()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        Assert.Contains("Workspace root exists", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_PrintsFerretConfigDirCheck()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        Assert.Contains(".ferret config directory exists", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_PrintsIndexFreshnessCheck()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        Assert.Contains("Index freshness", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_PrintsAiProviderCheck()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        Assert.Contains("AI provider configured", sw.ToString(), StringComparison.Ordinal);
    }
}
