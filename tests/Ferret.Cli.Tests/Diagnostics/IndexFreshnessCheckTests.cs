using Ferret.Cli.Cli;
using Ferret.Cli.Diagnostics;
using Ferret.Cli.Diagnostics.Checks;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class IndexFreshnessCheckTests
{
    [Fact]
    public async Task Pass_WhenIndexFileIsRecent()
    {
        var dir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var dbPath = Path.Join(dir, "keyword-index.db");
        await File.WriteAllTextAsync(dbPath, "x");
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddHours(-1));
        try
        {
            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw);
            var check = new IndexFreshnessCheck(dbPath);
            var result = await check.RunAsync(ctx, CancellationToken.None);
            Assert.True(result.Passed);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Warn_WhenIndexFileIsMissing()
    {
        using var sw = new StringWriter();
        var ctx = FerretContext.CreateTest(sw);
        var missing = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "keyword-index.db");
        var check = new IndexFreshnessCheck(missing);
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.True(result.IsWarning);
        Assert.True(result.Passed);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task Warn_WhenIndexFileIsStale()
    {
        var dir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var dbPath = Path.Join(dir, "keyword-index.db");
        await File.WriteAllTextAsync(dbPath, "x");
        File.SetLastWriteTimeUtc(dbPath, DateTime.UtcNow.AddHours(-25));
        try
        {
            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw);
            var check = new IndexFreshnessCheck(dbPath);
            var result = await check.RunAsync(ctx, CancellationToken.None);
            Assert.True(result.IsWarning);
            Assert.True(result.Passed);
            Assert.NotNull(result.FailureReason);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Name_IsStable()
    {
        var check = new IndexFreshnessCheck("dummy.db");
        Assert.Equal("Index freshness", check.Name);
    }
}
