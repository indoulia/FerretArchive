using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;

using Microsoft.Data.Sqlite;

using Xunit;

namespace Ferret.Indexing.Tests;

public sealed class SqliteKeywordIndexEngineTests : IDisposable
{
    private readonly string _dbPath;

    public SqliteKeywordIndexEngineTests()
    {
        _dbPath = Path.Join(Path.GetTempPath(), $"ferret-index-test-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        // Clear connection pools to release file locks before deleting
        SqliteConnection.ClearAllPools();

        TryDelete(_dbPath);
        TryDelete(_dbPath + "-wal");
        TryDelete(_dbPath + "-shm");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup — test isolation still holds since file names are unique per test
        }
    }

    [Fact]
    public void Constructor_Creates_Database_File_On_Disk()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public void Constructor_Creates_Parent_Directory_If_Missing()
    {
        var nested = Path.Join(Path.GetTempPath(), $"ferret-test-{Guid.NewGuid():N}", "sub", "index.db");
        var topDir = Path.GetDirectoryName(Path.GetDirectoryName(nested));
        try
        {
            using (new SqliteKeywordIndexEngine(nested))
            {
                Assert.True(File.Exists(nested));
            }

            SqliteConnection.ClearAllPools();
        }
        finally
        {
            if (topDir != null && Directory.Exists(topDir))
            {
                Directory.Delete(topDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Constructor_Propagates_SqliteException_For_Corrupt_File()
    {
        File.WriteAllBytes(_dbPath, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);
        Assert.Throws<SqliteException>(() => new SqliteKeywordIndexEngine(_dbPath));
    }

    [Fact]
    public async Task WriteAsync_Then_GetStatsAsync_Returns_One_Document()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        var doc = MakeDocument("doc-1", "Hello world");

        await engine.WriteAsync(doc);
        var stats = await engine.GetStatsAsync();

        Assert.Equal(1, stats.DocumentCount);
    }

    [Fact]
    public async Task WriteAsync_Upserts_On_Same_Id()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        var doc1 = MakeDocument("doc-1", "Hello world");
        var doc2 = MakeDocument("doc-1", "Updated content");

        await engine.WriteAsync(doc1);
        await engine.WriteAsync(doc2);
        var stats = await engine.GetStatsAsync();

        Assert.Equal(1, stats.DocumentCount);
    }

    [Fact]
    public async Task WriteAsync_Multiple_Documents_Increments_Count()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);

        await engine.WriteAsync(MakeDocument("doc-1", "First document"));
        await engine.WriteAsync(MakeDocument("doc-2", "Second document"));
        await engine.WriteAsync(MakeDocument("doc-3", "Third document"));
        var stats = await engine.GetStatsAsync();

        Assert.Equal(3, stats.DocumentCount);
    }

    [Fact]
    public async Task GetStatsAsync_Returns_Zero_DocumentCount_On_Empty_Index()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        var stats = await engine.GetStatsAsync();

        Assert.Equal(0, stats.DocumentCount);
    }

    [Fact]
    public async Task GetStatsAsync_Returns_Correct_TotalChars()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        var text = "Hello world";
        await engine.WriteAsync(MakeDocument("doc-1", text));

        var stats = await engine.GetStatsAsync();

        Assert.Equal(text.Length, stats.TotalChars);
    }

    [Fact]
    public async Task ClearAsync_Removes_All_Documents()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        await engine.WriteAsync(MakeDocument("doc-1", "First document"));
        await engine.WriteAsync(MakeDocument("doc-2", "Second document"));

        await engine.ClearAsync();
        var stats = await engine.GetStatsAsync();

        Assert.Equal(0, stats.DocumentCount);
    }

    [Fact]
    public async Task WriteAsync_After_ClearAsync_Inserts_Correctly()
    {
        using var engine = new SqliteKeywordIndexEngine(_dbPath);
        await engine.WriteAsync(MakeDocument("doc-1", "Before clear"));
        await engine.ClearAsync();
        await engine.WriteAsync(MakeDocument("doc-2", "After clear"));

        var stats = await engine.GetStatsAsync();

        Assert.Equal(1, stats.DocumentCount);
    }

    [Fact]
    public async Task Constructor_Reopens_Existing_Database_Without_Data_Loss()
    {
        // Write to engine, dispose, reopen, and verify data is still there
        {
            using var engine = new SqliteKeywordIndexEngine(_dbPath);
            await engine.WriteAsync(MakeDocument("doc-1", "Persisted content"));
        }

        {
            using var engine = new SqliteKeywordIndexEngine(_dbPath);
            var stats = await engine.GetStatsAsync();
            Assert.Equal(1, stats.DocumentCount);
        }
    }

    private static Document MakeDocument(string id, string plainText) => new()
    {
        Id = DocumentId.Create(id),
        SourceAssetId = new AssetId(id),
        ConnectorId = new Ferret.Core.Connectors.ConnectorId("filesystem"),
        InstanceId = new Ferret.Core.Connectors.ConnectorInstanceId("test"),
        MediaType = "text/plain",
        Kind = DocumentKind.Unknown,
        PlainText = plainText,
        ProducedAt = DateTimeOffset.UtcNow,
    };
}
