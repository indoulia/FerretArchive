using Ferret.Cli.Cli;

namespace Ferret.Cli.Tests.Cli;

public sealed class FerretContextTests
{
    [Fact]
    public void CreateTest_ReturnsValidContext()
    {
        var ctx = FerretContext.CreateTest(new StringWriter());
        Assert.NotNull(ctx.Services.Output);
        Assert.Equal(VerbosityLevel.Normal, ctx.Verbosity);
        Assert.Equal(OutputFormat.Text, ctx.OutputFormat);
    }

    [Fact]
    public void CreateTest_VerbosePropagates()
    {
        var ctx = FerretContext.CreateTest(new StringWriter(), VerbosityLevel.Verbose);
        Assert.Equal(VerbosityLevel.Verbose, ctx.Verbosity);
    }

    [Fact]
    public void GetOption_UnknownKey_ReturnsDefault()
    {
        var ctx = FerretContext.CreateTest(new StringWriter());
        Assert.Null(ctx.GetOption<string>("nonexistent"));
    }
}
