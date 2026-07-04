using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Persistence.Tests;

public sealed class FileDependencyStateStoreTests : IDisposable
{
    private readonly string _rootDirectory;

    public FileDependencyStateStoreTests()
    {
        _rootDirectory = Path.Join(Path.GetTempPath(), $"ferret-dependency-record-test-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task GetRecordAsync_WhenNoRecordStoredForKey_ReturnsNull()
    {
        var store = new FileDependencyStateStore(_rootDirectory);

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
        var writer = new FileDependencyStateStore(_rootDirectory);
        await writer.SetRecordAsync(record);

        var reader = new FileDependencyStateStore(_rootDirectory);
        var result = await reader.GetRecordAsync(record.EngineResponsibility, record.RequestPath);

        Assert.Equal(record, result);
    }

    [Fact]
    public async Task SetThenGet_WithConfigurationDependency_RoundTrips()
    {
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/README.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 1024),
            PlainText = "content",
            ConfigurationDependency = new ConfigurationDependency
            {
                Parser = new ComponentRegistrationIdentity { Id = "text/plain", Version = "1.0" },
                Connector = new ComponentRegistrationIdentity { Id = "filesystem", Version = "1.0" },
            },
        };
        var writer = new FileDependencyStateStore(_rootDirectory);
        await writer.SetRecordAsync(record);

        var reader = new FileDependencyStateStore(_rootDirectory);
        var result = await reader.GetRecordAsync(record.EngineResponsibility, record.RequestPath);

        Assert.Equal(record, result);
    }

    [Fact]
    public async Task SetThenGet_WithDependencyChain_RoundTrips()
    {
        var record = new DependencyRecord
        {
            EngineResponsibility = "AssembleContext",
            RequestPath = "/repo",
            SourceFingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 1024),
            DependencyChain = new DependencyChain
            {
                References =
                [
                    new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query-one" },
                    new DependencyReference { EngineResponsibility = "SearchIndex", RequestPath = "search:/repo query-two" },
                ],
            },
        };
        var writer = new FileDependencyStateStore(_rootDirectory);
        await writer.SetRecordAsync(record);

        var reader = new FileDependencyStateStore(_rootDirectory);
        var result = await reader.GetRecordAsync(record.EngineResponsibility, record.RequestPath);

        Assert.Equal(record, result);
    }

    [Fact]
    public async Task GetRecordAsync_OnRecordWrittenBeforeS2_6_WithNoDependencyChainProperty_ReturnsRecordWithEmptyDependencyChain()
    {
        // Simulates a record persisted by the pre-S2-6 envelope shape, which never wrote a
        // "dependencyChain" property at all — proving backward compatibility without needing an
        // old build of the store to actually produce the file.
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(
            recordFile,
            """{"schemaVersion":"1.0","engineResponsibility":"ParseFile","requestPath":"/repo/a.md","sourceFingerprint":{"algorithm":"lightweight","value":"x"}}""");

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.NotNull(result);
        Assert.Equal(DependencyChain.Empty, result.DependencyChain);
    }

    [Fact]
    public async Task GetRecordAsync_OnRecordWrittenBeforeS2_5_WithNoConfigurationDependencyProperty_ReturnsRecordWithNullConfigurationDependency()
    {
        // Simulates a record persisted by the pre-S2-5 envelope shape (ADR-0023/ADR-0024),
        // which never wrote a "configurationDependency" property at all — proving backward
        // compatibility without needing an old build of the store to actually produce the file.
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(
            recordFile,
            """{"schemaVersion":"1.0","engineResponsibility":"ParseFile","requestPath":"/repo/a.md","sourceFingerprint":{"algorithm":"lightweight","value":"x"}}""");

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.NotNull(result);
        Assert.Null(result.ConfigurationDependency);
    }

    [Fact]
    public async Task GetRecordAsync_ForKeyNeverStored_ReturnsNull_EvenWhenOtherKeysExist()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
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
    public async Task SetRecordAsync_Overwrites_PreviousRecord_ForTheSameKey()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
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

    [Fact]
    public async Task SetRecordAsync_TwoDistinctRequestPaths_AreStoredAndRetrievedIndependently()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        var first = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        };
        var second = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/b.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
        };

        await store.SetRecordAsync(first);
        await store.SetRecordAsync(second);

        Assert.Equal(first, await store.GetRecordAsync("ParseFile", "/repo/a.md"));
        Assert.Equal(second, await store.GetRecordAsync("ParseFile", "/repo/b.md"));
    }

    [Fact]
    public async Task SetRecordAsync_SameRequestPath_DifferentEngineResponsibility_AreStoredAndRetrievedIndependently()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        var parse = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        };
        var otherResponsibility = new DependencyRecord
        {
            EngineResponsibility = "SomeOtherResponsibility",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
        };

        await store.SetRecordAsync(parse);
        await store.SetRecordAsync(otherResponsibility);

        Assert.Equal(parse, await store.GetRecordAsync("ParseFile", "/repo/a.md"));
        Assert.Equal(otherResponsibility, await store.GetRecordAsync("SomeOtherResponsibility", "/repo/a.md"));
    }

    [Fact]
    public async Task GetRecordAsync_WhenStoredFileContentIdentityDoesNotMatchQueryKey_ReturnsNull()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });

        // Tamper with the on-disk file directly (without knowing the store's internal key->path
        // mapping) to prove the identity check inside the file is a real, load-bearing safeguard,
        // not dead code — defense in depth against a hash collision or a manually edited file.
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(
            recordFile,
            """{"schemaVersion":"1.0","engineResponsibility":"ParseFile","requestPath":"/repo/DIFFERENT.md","sourceFingerprint":{"algorithm":"lightweight","value":"x"}}""");

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordAsync_WhenStoredFileContainsMalformedJson_ReturnsNull()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(recordFile, "{ this is not valid json");

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordAsync_WhenStoredFileIsLockedByAnotherHandle_ReturnsNull()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();

        using var exclusiveLock = new FileStream(recordFile, FileMode.Open, FileAccess.Read, FileShare.None);
        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordAsync_WhenStoredFileContainsMalformedJson_EvictsTheCorruptedFile()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        await File.WriteAllTextAsync(recordFile, "{ this is not valid json");

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Null(result);
        Assert.False(File.Exists(recordFile), "A record safely classified as corrupted (malformed content) should be evicted, not left behind forever.");
    }

    [Fact]
    public async Task GetRecordAsync_WhenFileIsLockedByAnotherHandle_DoesNotEvictTheFile()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        };
        await store.SetRecordAsync(record);
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();

        using (new FileStream(recordFile, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var lockedResult = await store.GetRecordAsync("ParseFile", "/repo/a.md");
            Assert.Null(lockedResult);
        }

        Assert.True(File.Exists(recordFile), "A transient I/O failure is not a safe corruption classification and must never cause eviction.");
        var afterUnlock = await store.GetRecordAsync("ParseFile", "/repo/a.md");
        Assert.Equal(record, afterUnlock);
    }

    [Fact]
    public async Task GetRecordAsync_WhenOrphanedTempFileExistsWithNoCorrespondingRecord_EvictsTheOrphanedTempFile()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        File.Delete(recordFile);
        var orphanedTempFile = recordFile + ".tmp";
        await File.WriteAllTextAsync(orphanedTempFile, "partial write left behind by a crashed SetRecordAsync");

        var result = await store.GetRecordAsync("ParseFile", "/repo/a.md");

        Assert.Null(result);
        Assert.False(File.Exists(orphanedTempFile), "A temp file is never a valid record by construction, so it is always safe to evict once its key is queried.");
    }

    [Fact]
    public async Task SetRecordAsync_WritesAtomically_LeavingNoTemporaryFilesBehind()
    {
        var store = new FileDependencyStateStore(_rootDirectory);

        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });

        Assert.Single(Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(_rootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SetRecordAsync_Overwrite_AlsoLeavesNoTemporaryFilesBehind()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });

        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 2),
        });

        Assert.Empty(Directory.GetFiles(_rootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SetRecordAsync_WritesCamelCasePropertyNames()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            PlainText = "content",
        });

        var json = await ReadTheOneRecordFileAsync();

        Assert.Contains("\"schemaVersion\"", json, StringComparison.Ordinal);
        Assert.Contains("\"engineResponsibility\"", json, StringComparison.Ordinal);
        Assert.Contains("\"requestPath\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sourceFingerprint\"", json, StringComparison.Ordinal);
        Assert.Contains("\"algorithm\"", json, StringComparison.Ordinal);
        Assert.Contains("\"value\"", json, StringComparison.Ordinal);
        Assert.Contains("\"plainText\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"EngineResponsibility\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"RequestPath\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetRecordAsync_EmbedsCurrentSchemaVersion()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
        });

        var json = await ReadTheOneRecordFileAsync();
        using var document = System.Text.Json.JsonDocument.Parse(json);

        Assert.Equal("1.0", document.RootElement.GetProperty("schemaVersion").GetString());
    }

    [Fact]
    public async Task SetRecordAsync_OmitsPlainTextProperty_WhenPlainTextIsNull()
    {
        var store = new FileDependencyStateStore(_rootDirectory);
        await store.SetRecordAsync(new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/a.md",
            SourceFingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 1),
            PlainText = null,
        });

        var json = await ReadTheOneRecordFileAsync();

        Assert.DoesNotContain("\"plainText\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetRecordAsync_ProducesByteIdenticalOutput_ForEquivalentInput()
    {
        var fingerprint = AssetFingerprint.CreateLightweight(new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero), 1024);
        var firstRoot = Path.Join(_rootDirectory, "first");
        var secondRoot = Path.Join(_rootDirectory, "second");
        var record = new DependencyRecord
        {
            EngineResponsibility = "ParseFile",
            RequestPath = "/repo/README.md",
            SourceFingerprint = fingerprint,
            PlainText = "content",
        };

        await new FileDependencyStateStore(firstRoot).SetRecordAsync(record);
        await new FileDependencyStateStore(secondRoot).SetRecordAsync(record with { });

        var firstBytes = await File.ReadAllBytesAsync(Directory.GetFiles(firstRoot, "*.json").Single());
        var secondBytes = await File.ReadAllBytesAsync(Directory.GetFiles(secondRoot, "*.json").Single());

        Assert.Equal(firstBytes, secondBytes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private async Task<string> ReadTheOneRecordFileAsync()
    {
        var recordFile = Directory.GetFiles(_rootDirectory, "*.json", SearchOption.AllDirectories).Single();
        return await File.ReadAllTextAsync(recordFile);
    }
}
