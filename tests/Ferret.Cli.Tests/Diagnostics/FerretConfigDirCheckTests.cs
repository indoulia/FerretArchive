using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Diagnostics.Checks;
using Ferret.Core.Workspace;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class FerretConfigDirCheckTests
{
    [Fact]
    public async Task Pass_WhenFerretDirExists()
    {
        var root = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var ferretDir = Path.Join(root, WorkspaceLayout.RootDirectoryName);
        Directory.CreateDirectory(ferretDir);
        try
        {
            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw);
            var check = new FerretConfigDirCheck(root);
            var result = await check.RunAsync(ctx, CancellationToken.None);
            Assert.True(result.Passed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Fail_WhenFerretDirMissing()
    {
        var root = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw);
            var check = new FerretConfigDirCheck(root);
            var result = await check.RunAsync(ctx, CancellationToken.None);
            Assert.False(result.Passed);
            Assert.NotNull(result.FailureReason);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Name_IsStable()
    {
        var check = new FerretConfigDirCheck(Path.GetTempPath());
        Assert.Equal(".ferret config directory exists", check.Name);
    }
}
