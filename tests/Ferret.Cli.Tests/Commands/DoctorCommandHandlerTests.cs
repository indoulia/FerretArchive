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

    [Fact]
    public async Task Doctor_PrintsParserPlatformSection()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        string output = sw.ToString();
        Assert.Contains("Parser Platform", output, StringComparison.Ordinal);
        Assert.Contains("Installed Parsers", output, StringComparison.Ordinal);
        Assert.Contains("Excel (XLSX) Parser", output, StringComparison.Ordinal);
        Assert.Contains("Extension Coverage", output, StringComparison.Ordinal);
        Assert.Contains("Parser Packages", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_Default_SummarizesOpaqueExtensions()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor"]);
        Assert.Contains("run `ferret doctor --verbose` for the full list", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_Verbose_ShowsParserPriorityAndFullOpaqueList()
    {
        using var sw = new StringWriter();
        await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["doctor", "--verbose"]);
        string output = sw.ToString();
        Assert.Contains("Priority:", output, StringComparison.Ordinal);
        Assert.Contains("Media Type:", output, StringComparison.Ordinal);
        Assert.DoesNotContain("run `ferret doctor --verbose`", output, StringComparison.Ordinal);
    }
}
