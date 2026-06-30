using System.Globalization;

using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class DiagnosticRunnerTests
{
    [Fact]
    public async Task RunAsync_AllPass_ReturnsTrue()
    {
        var ctx = FerretContext.CreateTest(new StringWriter());
        var result = await DiagnosticRunner.RunAsync([new PassCheck()], ctx);
        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_OneFails_ReturnsFalse()
    {
        var ctx = FerretContext.CreateTest(new StringWriter());
        var result = await DiagnosticRunner.RunAsync([new PassCheck(), new FailCheck()], ctx);
        Assert.False(result);
    }

    [Fact]
    public async Task RunAsync_PassingCheck_PrintsSuccessLine()
    {
        var sw = new StringWriter();
        await DiagnosticRunner.RunAsync([new PassCheck()], FerretContext.CreateTest(sw));
        Assert.Contains("✓ Always passes", sw.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_FailingCheck_PrintsErrorLine()
    {
        var sw = new StringWriter();
        await DiagnosticRunner.RunAsync([new FailCheck()], FerretContext.CreateTest(sw));
        Assert.Contains("✗ Always fails", sw.ToString(), StringComparison.Ordinal);
    }
}
