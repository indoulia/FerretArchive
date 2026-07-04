using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class SpikeDependencyStateStoreTests : IDisposable
{
    private readonly string _filePath;

    public SpikeDependencyStateStoreTests()
    {
        _filePath = Path.Join(Path.GetTempPath(), $"ferret-dependency-record-test-{Guid.NewGuid():N}", "record.json");
    }

    [Fact]
    public async Task GetRecordAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var store = new SpikeDependencyStateStore(_filePath);

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetThenGet_ViaNewInstance_RoundTrips()
    {
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/README.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 1024),
            PlainText = "content",
        };
        var writer = new SpikeDependencyStateStore(_filePath);
        await writer.SetRecordAsync(record);

        var reader = new SpikeDependencyStateStore(_filePath);
        var result = await reader.GetRecordAsync(record.EngineResponsibility, record.RequestPath);

        Assert.Equal(record, result);
    }

    [Fact]
    public async Task GetRecordAsync_WhenIdentityDoesNotMatchStoredRecord_ReturnsNull()
    {
        var store = new SpikeDependencyStateStore(_filePath);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });

        var result = await store.GetRecordAsync("ParseFile", "/repo/b.md");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetRecordAsync_Overwrites_PreviousRecord()
    {
        var store = new SpikeDependencyStateStore(_filePath);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var latest = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
        };

        await store.SetRecordAsync(latest);
        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Equal(latest, result);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
