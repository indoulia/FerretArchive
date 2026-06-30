using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Diagnostics.Checks;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class WorkspaceRootCheckTests
{
    [Fact]
    public async Task Pass_WhenDirectoryExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw, workingDirectory: dir);
            var check = new WorkspaceRootCheck(dir);
            var result = await check.RunAsync(ctx, CancellationToken.None);
            Assert.True(result.Passed);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Fail_WhenDirectoryMissing()
    {
        using var sw = new StringWriter();
        var ctx = FerretContext.CreateTest(sw);
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var check = new WorkspaceRootCheck(missing);
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.False(result.Passed);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void Name_IsStable()
    {
        var check = new WorkspaceRootCheck(Path.GetTempPath());
        Assert.Equal("Workspace root exists", check.Name);
    }
}
