using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;
using Ferret.Indexing;
using Microsoft.Data.Sqlite;

namespace Ferret.Indexing.Tests;

public sealed class SqliteKeywordIndexEngineDeleteTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteKeywordIndexEngine _engine;

    public SqliteKeywordIndexEngineDeleteTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ferret-delete-test-{Guid.NewGuid():N}.db");
        _engine = new SqliteKeywordIndexEngine(_dbPath);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocumentFromIndex()
    {
        var docId = DocumentId.Create("file:///workspace/file1.cs");
        await _engine.WriteAsync(MakeDocument(docId, "public class File1 { }"));
        var statsBefore = await _engine.GetStatsAsync();
        Assert.Equal(1, statsBefore.DocumentCount);

        await _engine.DeleteAsync(docId);

        var statsAfter = await _engine.GetStatsAsync();
        Assert.Equal(0, statsAfter.DocumentCount);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentDocument_DoesNotThrow()
    {
        var docId = DocumentId.Create("file:///workspace/nonexistent.cs");
        await _engine.DeleteAsync(docId); // must not throw
    }

    [Fact]
    public async Task DeleteAsync_DocumentNotReturnedInStatsAfterDelete()
    {
        var docId = DocumentId.Create("file:///workspace/searchable.cs");
        await _engine.WriteAsync(MakeDocument(docId, "public class SearchableClass { }"));
        await _engine.DeleteAsync(docId);

        var stats = await _engine.GetStatsAsync();
        Assert.Equal(0, stats.DocumentCount);
    }

    public void Dispose()
    {
        _engine.Dispose();
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup — test isolation still holds since file names are unique per test
        }
    }

    private static Document MakeDocument(DocumentId id, string plainText) => new()
    {
        Id = id,
        SourceAssetId = new AssetId(id.Value),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        MediaType = "text/plain",
        Kind = DocumentKind.Code,
        PlainText = plainText,
        Title = null,
        ProducedAt = DateTimeOffset.UtcNow,
    };
}
