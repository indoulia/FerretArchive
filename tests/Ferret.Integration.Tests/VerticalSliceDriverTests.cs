using Ferret.Core.Connectors;
using Ferret.Persistence;
using Ferret.VerticalSlice;

using Xunit;

namespace Ferret.Integration.Tests;

public sealed class VerticalSliceDriverTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _fileName = "sample.txt";
    private readonly string _storePath;

    public VerticalSliceDriverTests()
    {
        _rootPath = Path.Join(Path.GetTempPath(), $"ferret-vslice-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        File.WriteAllText(Path.Join(_rootPath, _fileName), "hello vertical slice");
        _storePath = Path.Join(_rootPath, ".ferret", "temp", "record.json");
    }

    [Fact]
    public async Task ScanAndPersistAsync_PersistsRecord_WithFingerprintMatchingTheFile()
    {
        var store = new SpikeDependencyStateStore(_storePath);
        var filePath = Path.Join(_rootPath, _fileName);
        var fileInfo = new FileInfo(filePath);
        var expectedFingerprint = AssetFingerprint.CreateLightweight(fileInfo.LastWriteTimeUtc, fileInfo.Length);

        var record = await VerticalSliceDriver.ScanAndPersistAsync(_rootPath, _fileName, store);

        Assert.Equal(expectedFingerprint, record.SourceFingerprint);
        Assert.Equal("hello vertical slice", record.PlainText);

        var reloadedStore = new SpikeDependencyStateStore(_storePath);
        var reloaded = await reloadedStore.GetRecordAsync(record.EngineResponsibility, record.RequestPath);
        Assert.Equal(record, reloaded);
    }

    [Fact]
    public async Task ScanAndPersistAsync_PersistsRecord_WithParserAndConnectorConfigurationDependency()
    {
        var store = new SpikeDependencyStateStore(_storePath);

        var record = await VerticalSliceDriver.ScanAndPersistAsync(_rootPath, _fileName, store);

        Assert.NotNull(record.ConfigurationDependency);
        Assert.Equal("text/plain", record.ConfigurationDependency.Parser?.Id);
        Assert.Equal("1.0", record.ConfigurationDependency.Parser?.Version);
        Assert.Equal("filesystem", record.ConfigurationDependency.Connector?.Id);
        Assert.Equal("1.0", record.ConfigurationDependency.Connector?.Version);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
