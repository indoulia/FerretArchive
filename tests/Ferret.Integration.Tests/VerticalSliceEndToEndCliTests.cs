using Xunit;

namespace Ferret.Integration.Tests;

/// <summary>
/// T9: the full composed sequence (ARCH-035 §1) — request → retrieval → comparison → decision →
/// reuse/recompute → surface — exercised end-to-end through T8's real CLI path (the
/// <c>vslice-resolve</c> command, not <see cref="VerticalSliceDriver"/> directly), across a
/// genuine process boundary, per the vertical slice plan's Overall Success Criteria. Re-runs the
/// same three scenarios T7 proved at the driver level, now through the composed CLI path.
/// A non-zero exit or thrown host error already fails these tests via <see cref="VerticalSliceHostRunner"/>,
/// so success is implied by output being returned at all.
/// </summary>
public sealed class VerticalSliceEndToEndCliTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _fileName = "sample.txt";
    private readonly string _storePath;

    public VerticalSliceEndToEndCliTests()
    {
        _rootPath = Path.Join(Path.GetTempPath(), $"ferret-e2e-cli-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        File.WriteAllText(Path.Join(_rootPath, _fileName), "hello end to end");
        _storePath = Path.Join(_rootPath, ".ferret", "temp", "record.json");
    }

    [Fact]
    public async Task FileUnchanged_SecondCliInvocation_ReusesAndProducesIdenticalOutput_AcrossARealProcessRestart()
    {
        var first = await VerticalSliceHostRunner.RunAsync("cli-resolve", _rootPath, _fileName, _storePath);
        var second = await VerticalSliceHostRunner.RunAsync("cli-resolve", _rootPath, _fileName, _storePath);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task FileModified_SecondCliInvocation_RecomputesWithNewContent_AcrossARealProcessRestart()
    {
        var first = await VerticalSliceHostRunner.RunAsync("cli-resolve", _rootPath, _fileName, _storePath);
        await File.WriteAllTextAsync(Path.Join(_rootPath, _fileName), "hello end to end, modified with a different length");

        var second = await VerticalSliceHostRunner.RunAsync("cli-resolve", _rootPath, _fileName, _storePath);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task RecordCorrupted_SecondCliInvocation_RecomputesWithSameContent_AcrossARealProcessRestart()
    {
        var first = await VerticalSliceHostRunner.RunAsync("cli-resolve", _rootPath, _fileName, _storePath);
        await VerticalSliceResolveTests.CorruptTheStoredRecordAsync(_storePath);

        var second = await VerticalSliceHostRunner.RunAsync("cli-resolve", _rootPath, _fileName, _storePath);

        Assert.Equal(first, second);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
