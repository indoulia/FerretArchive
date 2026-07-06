using Ferret.Cli.Cli;
using Ferret.Cli.Commands;
using Ferret.Cli.Commands.Handlers;

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

    [Fact]
    public async Task ExecuteAsync_LiveProcessRecorded_ReportsRunningWithPid()
    {
        var dir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var path = RuntimeStatusFile.ResolvePath(dir);

            // The current test process is, by definition, alive for the duration of this test.
            RuntimeStatusFile.Write(path, Environment.ProcessId, DateTimeOffset.UtcNow);

            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw, workingDirectory: dir);
            var result = await new StatusCommandHandler().ExecuteAsync(ctx);

            Assert.Equal(CommandResult.Success, result);
            Assert.Contains("running", sw.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture), sw.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_StaleRecordForDeadProcess_ReportsNotRunning_AndDeletesFile()
    {
        var dir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var path = RuntimeStatusFile.ResolvePath(dir);
            RuntimeStatusFile.Write(path, int.MaxValue - 1, DateTimeOffset.UtcNow.AddHours(-1));

            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw, workingDirectory: dir);
            var result = await new StatusCommandHandler().ExecuteAsync(ctx);

            Assert.Equal(CommandResult.Failure, result);
            Assert.Contains("not running", sw.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(path), "stale status file should be cleaned up");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoRecordedFile_ReportsNotRunning()
    {
        var dir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var sw = new StringWriter();
            var ctx = FerretContext.CreateTest(sw, workingDirectory: dir);
            var result = await new StatusCommandHandler().ExecuteAsync(ctx);

            Assert.Equal(CommandResult.Failure, result);
            Assert.Contains("not running", sw.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
