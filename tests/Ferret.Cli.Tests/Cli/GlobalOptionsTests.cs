using System.CommandLine;

using Ferret.Cli.Cli;
using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Cli;

public sealed class GlobalOptionsTests
{
    [Fact]
    public void LogLevel_Option_LongName_Is_LogLevel()
    {
        Assert.Contains("--log-level", GlobalOptions.LogLevel.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void LogLevel_Option_IsNotHidden()
    {
        Assert.False(GlobalOptions.LogLevel.Hidden);
    }

    [Fact]
    public void AddAll_Adds_LogLevel_To_Root()
    {
        var root = new RootCommand();
        GlobalOptions.AddAll(root);
        Assert.Contains(root.Options, o => o.Name == "--log-level");
    }

    [Fact]
    public async Task LogLevel_Debug_DoesNot_Throw()
    {
        using var sw = new StringWriter();

        // Should not throw; verifies the factory wiring path runs without error.
        var exit = await RootCommandFactory
            .Build([new CoreCliModule()], sw)
            .InvokeAsync(["--log-level", "debug", "version"]);
        Assert.Equal(0, exit);
    }

    [Fact]
    public void LogLevel_Option_DefaultValue_Is_Information()
    {
        // Critical 1: default must be Information, not null/NullLoggerFactory.
        // Verify by parsing with no --log-level argument.
        var root = new RootCommand();
        root.Add(GlobalOptions.LogLevel);
        var result = root.Parse([]);
        Assert.Equal("Information", result.GetValue(GlobalOptions.LogLevel));
    }

    [Fact]
    public async Task LogLevel_Omitted_DoesNot_Throw()
    {
        using var sw = new StringWriter();

        // Critical 1: omitting --log-level must use Information, not throw.
        var exit = await RootCommandFactory
            .Build([new CoreCliModule()], sw)
            .InvokeAsync(["version"]);
        Assert.Equal(0, exit);
    }

    [Fact]
    public async Task LogLevel_UnknownValue_FallsThrough_To_Information()
    {
        using var sw = new StringWriter();

        // Critical 2: unknown value must not crash (falls through to Information).
        var exit = await RootCommandFactory
            .Build([new CoreCliModule()], sw)
            .InvokeAsync(["--log-level", "verbosely_loud", "version"]);
        Assert.Equal(0, exit);
    }
}
