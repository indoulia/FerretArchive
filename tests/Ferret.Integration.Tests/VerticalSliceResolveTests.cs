using Xunit;

namespace Ferret.Integration.Tests;

/// <summary>
/// Exercises T7's resolve-and-reuse driver across a genuine process boundary, per the vertical
/// slice plan's Global Constraints ("Real process restart required for Milestone 4/5" — an
/// in-process round-trip does not prove ARCH-026 §1's actual bar). Each test launches the write
/// (T6) and the resolve (T7) as separate, sequential OS processes via <see cref="VerticalSliceHostRunner"/>,
/// waiting for each to genuinely exit before the next begins.
/// </summary>
public sealed class VerticalSliceResolveTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _fileName = "sample.txt";
    private readonly string _storePath;

    public VerticalSliceResolveTests()
    {
        _rootPath = Path.Join(Path.GetTempPath(), $"ferret-resolve-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        File.WriteAllText(Path.Join(_rootPath, _fileName), "hello vertical slice");
        _storePath = Path.Join(_rootPath, ".ferret", "temp", "record.json");
    }

    [Fact]
    public async Task FileUnchanged_ProducesSatisfied_AcrossARealProcessRestart()
    {
        await VerticalSliceHostRunner.RunAsync("scan-and-persist", _rootPath, _fileName, _storePath);

        var outcome = await VerticalSliceHostRunner.RunAsync("resolve-and-reuse", _rootPath, _fileName, _storePath);

        Assert.Equal("Satisfied", outcome);
    }

    [Fact]
    public async Task FileModified_ProducesNotSatisfied_AcrossARealProcessRestart()
    {
        await VerticalSliceHostRunner.RunAsync("scan-and-persist", _rootPath, _fileName, _storePath);
        await File.WriteAllTextAsync(Path.Join(_rootPath, _fileName), "hello vertical slice, modified with a different length");

        var outcome = await VerticalSliceHostRunner.RunAsync("resolve-and-reuse", _rootPath, _fileName, _storePath);

        Assert.Equal("NotSatisfied", outcome);
    }

    [Fact]
    public async Task RecordCorrupted_ProducesIndeterminate_AcrossARealProcessRestart()
    {
        await VerticalSliceHostRunner.RunAsync("scan-and-persist", _rootPath, _fileName, _storePath);
        await CorruptTheStoredRecordAsync(_storePath);

        var outcome = await VerticalSliceHostRunner.RunAsync("resolve-and-reuse", _rootPath, _fileName, _storePath);

        Assert.Equal("Indeterminate", outcome);
    }

    /// <summary>
    /// Corrupts the one record file S2-4's key/lookup structure (ADR-0024) placed under
    /// <paramref name="storeRootPath"/>, without assuming or reproducing its hash-derived name.
    /// </summary>
    internal static async Task CorruptTheStoredRecordAsync(string storeRootPath)
    {
        var recordFile = Directory.GetFiles(storeRootPath, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(recordFile, "{ this is not valid json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
