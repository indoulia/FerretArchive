using Ferret.Cli.Commands;

namespace Ferret.Cli.Tests.Commands;

public sealed class StatusCommandHandlerTests
{
    [Fact]
    public async Task Status_ReportsNotRunning_ExitsOne()
    {
        using var sw = new StringWriter();
        int code = await RootCommandFactory.Build([new CoreCliModule()], sw).InvokeAsync(["status"]);
        Assert.Equal(1, code);
        Assert.Contains("not running", sw.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
