using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Ferret.Indexing.Tests.EndToEnd;
using Ferret.Indexing.Tests.Helpers;

using Xunit;

namespace Ferret.Indexing.Tests.Performance;

/// <summary>
/// Smoke-level benchmark: verifies pipeline throughput stays above a minimum bar.
/// Not a microbenchmark — runs the full real pipeline to catch regressions.
/// </summary>
public sealed class IndexPipelineBenchmarkTests
{
    /// <summary>
    /// 100 small text files must be fully indexed in under 10 seconds on any CI agent.
    /// Typical on modern hardware: under 2 seconds.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact(Skip = "Flaky: wall-clock timing budget depends on CI runner speed (seen 16.5s on a slow agent). Re-enable with a runner-independent metric or higher budget.")]
    public async Task IndexPipeline_Processes_100_Files_In_Under_10_Seconds()
    {
        using var tempDir = new TempDirectory();

        for (var i = 0; i < 100; i++)
        {
            await File.WriteAllTextAsync(
                Path.Join(tempDir.Path, $"file-{i:D3}.txt"),
                $"benchmark content line {i} ferret test data for FTS5 full-text indexing pipeline throughput");
        }

        // Place db inside .ferret/ — auto-skipped by FilesystemConnector during discovery.
        var dbPath = Path.Join(tempDir.Path, ".ferret", "bench-index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var engine = new SqliteKeywordIndexEngine(dbPath);

        var pipeline = EndToEndIndexPipelineTests.BuildRealPipeline(tempDir.Path, engine);
        var workspaceId = WorkspaceId.Create("bench-100");

        var startTick = Environment.TickCount64;
        var result = await pipeline.RunAsync(
            workspaceId,
            IndexPipelineOptions.Default,
            CancellationToken.None);
        var elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64 - startTick);

        Assert.True(
            elapsed < TimeSpan.FromSeconds(10),
            $"Expected < 10s but took {elapsed.TotalSeconds:F2}s");
        Assert.True(
            result.DocumentsIndexed >= 90,
            $"Expected >= 90 indexed but got {result.DocumentsIndexed}");
        Assert.Equal(0, result.Failures);
    }
}
