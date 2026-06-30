using Ferret.Core.Primitives;
using Ferret.Indexing;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Indexing.Tests;

public sealed class DocumentServiceTests : IAsyncDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _connection;

    public DocumentServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"docservice-test-{Guid.NewGuid()}.db");
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        CreateSchema(_connection);
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS documents (
                id TEXT NOT NULL PRIMARY KEY,
                connector_id TEXT NOT NULL,
                instance_id TEXT NOT NULL,
                media_type TEXT NOT NULL,
                kind INTEGER NOT NULL,
                plain_text TEXT NOT NULL,
                title TEXT,
                produced_at INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static void SeedDocument(SqliteConnection connection, string id, string plainText, string? title = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO documents (id, connector_id, instance_id, media_type, kind, plain_text, title, produced_at)
            VALUES ($id, 'fs', 'fs-1', 'text/plain', 0, $text, $title, $ts)
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$text", plainText);
        cmd.Parameters.AddWithValue("$title", (object?)title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task GetAsync_ExistingDocument_ReturnsDocument()
    {
        SeedDocument(_connection, "doc-001", "hello world", "Hello");
        await _connection.CloseAsync();

        var sut = new DocumentService(_dbPath);
        var doc = await sut.GetAsync(DocumentId.Create("doc-001"), CancellationToken.None);

        Assert.NotNull(doc);
        Assert.Equal("doc-001", doc.Id.Value);
        Assert.Equal("hello world", doc.PlainText);
        Assert.Equal("Hello", doc.Title);
    }

    [Fact]
    public async Task GetAsync_MissingDocument_ReturnsNull()
    {
        await _connection.CloseAsync();

        var sut = new DocumentService(_dbPath);
        var doc = await sut.GetAsync(DocumentId.Create("no-such-doc"), CancellationToken.None);

        Assert.Null(doc);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
