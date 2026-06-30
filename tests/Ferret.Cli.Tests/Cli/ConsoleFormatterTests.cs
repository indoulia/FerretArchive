using Ferret.Cli.Cli;

namespace Ferret.Cli.Tests.Cli;

public sealed class ConsoleFormatterTests
{
    [Fact]
    public void WriteSuccess_PrependsTick()
    {
        var writer = new StringWriter();
        new ConsoleFormatter(writer).WriteSuccess("All good");
        Assert.Contains("✓ All good", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WriteError_PrependsX()
    {
        var writer = new StringWriter();
        new ConsoleFormatter(writer).WriteError("Something wrong");
        Assert.Contains("✗ Something wrong", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WriteVerbose_WhenNormal_WritesNothing()
    {
        var writer = new StringWriter();
        new ConsoleFormatter(writer, VerbosityLevel.Normal).WriteVerbose("secret");
        Assert.Empty(writer.ToString().Trim());
    }

    [Fact]
    public void WriteVerbose_WhenVerbose_WritesMessage()
    {
        var writer = new StringWriter();
        new ConsoleFormatter(writer, VerbosityLevel.Verbose).WriteVerbose("secret");
        Assert.Contains("secret", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WriteLine_WritesMessage()
    {
        var writer = new StringWriter();
        new ConsoleFormatter(writer).WriteLine("hello");
        Assert.Contains("hello", writer.ToString(), StringComparison.Ordinal);
    }
}
