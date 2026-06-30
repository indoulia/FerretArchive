using Ferret.Core.Documents;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

using Microsoft.Data.Sqlite;

namespace Ferret.Indexing;

/// <summary>Keyword (FTS5) index engine backed by SQLite. Implements <see cref="IIndexEngine"/>.</summary>
public sealed class SqliteKeywordIndexEngine : IIndexEngine, IDisposable
{
    private const int SchemaVersion = 1;

    // Constant SQL strings — safe from injection: all literals, no user input.
    private const string CreateDocumentsSql = """
        CREATE TABLE IF NOT EXISTS documents (
            id           TEXT NOT NULL PRIMARY KEY,
            connector_id TEXT NOT NULL,
            instance_id  TEXT NOT NULL,
            media_type   TEXT NOT NULL,
            kind         INTEGER NOT NULL,
            plain_text   TEXT NOT NULL,
            title        TEXT,
            produced_at  INTEGER NOT NULL
        );
        """;

    private const string CreateFtsSql = """
        CREATE VIRTUAL TABLE IF NOT EXISTS documents_fts USING fts5 (
            id UNINDEXED,
            plain_text,
            title
        );
        """;

    // PRAGMA user_version only accepts an integer literal, not a parameter — this constant is safe.
    private const string SetVersionSql = "PRAGMA user_version = 1;";

    private const string UpsertDocumentSql = """
        INSERT INTO documents (id, connector_id, instance_id, media_type, kind, plain_text, title, produced_at)
        VALUES ($id, $connector_id, $instance_id, $media_type, $kind, $plain_text, $title, $produced_at)
        ON CONFLICT(id) DO UPDATE SET
            connector_id  = excluded.connector_id,
            instance_id   = excluded.instance_id,
            media_type    = excluded.media_type,
            kind          = excluded.kind,
            plain_text    = excluded.plain_text,
            title         = excluded.title,
            produced_at   = excluded.produced_at;
        """;

    private const string DeleteFtsByIdSql = "DELETE FROM documents_fts WHERE id = $id;";

    private const string InsertFtsSql =
        "INSERT INTO documents_fts (id, plain_text, title) VALUES ($id, $plain_text, $title);";

    private const string StatsSql = """
        SELECT
            COUNT(*) AS document_count,
            COALESCE(SUM(LENGTH(plain_text)), 0) AS total_chars,
            COALESCE(MAX(produced_at), 0) AS last_indexed_ms
        FROM documents;
        """;

    private const string DeleteDocumentByIdSql = "DELETE FROM documents WHERE id = $id;";
    private const string DeleteFtsDocumentByIdSql = "DELETE FROM documents_fts WHERE id = $id;";

    private const string DeleteAllDocumentsSql = "DELETE FROM documents;";
    private const string DeleteAllFtsSql = "DELETE FROM documents_fts;";
    private const string PageCountSql = "PRAGMA page_count;";
    private const string PageSizeSql = "PRAGMA page_size;";
    private const string UserVersionSql = "PRAGMA user_version;";

    private readonly SqliteConnection _connection;

    /// <summary>Initializes a new instance of the <see cref="SqliteKeywordIndexEngine"/> class.</summary>
    /// <param name="dbPath">Full path to the SQLite database file. Parent directories are created if missing.</param>
    /// <exception cref="SqliteException">Thrown if the database file is corrupt or cannot be opened.</exception>
    public SqliteKeywordIndexEngine(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        // Use DELETE journal mode to avoid WAL files that can cause file-lock issues on Windows.
        using var journalCmd = _connection.CreateCommand();
        journalCmd.CommandText = "PRAGMA journal_mode=DELETE;";
        journalCmd.ExecuteNonQuery();

        EnsureSchema();
    }

    /// <inheritdoc/>
    public async Task WriteAsync(Document document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();

        var tx = await _connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (tx.ConfigureAwait(false))
        {
            try
            {
                var upsert = _connection.CreateCommand();
                await using (upsert.ConfigureAwait(false))
                {
                    upsert.Transaction = (SqliteTransaction)tx;
                    upsert.CommandText = UpsertDocumentSql;
                    upsert.Parameters.AddWithValue("$id", document.Id.Value);
                    upsert.Parameters.AddWithValue("$connector_id", document.ConnectorId.Value);
                    upsert.Parameters.AddWithValue("$instance_id", document.InstanceId.Value);
                    upsert.Parameters.AddWithValue("$media_type", document.MediaType);
                    upsert.Parameters.AddWithValue("$kind", (int)document.Kind);
                    upsert.Parameters.AddWithValue("$plain_text", document.PlainText);
                    upsert.Parameters.AddWithValue("$title", document.Title ?? (object)DBNull.Value);
                    upsert.Parameters.AddWithValue("$produced_at", document.ProducedAt.ToUnixTimeMilliseconds());
                    await upsert.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                var delFts = _connection.CreateCommand();
                await using (delFts.ConfigureAwait(false))
                {
                    delFts.Transaction = (SqliteTransaction)tx;
                    delFts.CommandText = DeleteFtsByIdSql;
                    delFts.Parameters.AddWithValue("$id", document.Id.Value);
                    await delFts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                var insFts = _connection.CreateCommand();
                await using (insFts.ConfigureAwait(false))
                {
                    insFts.Transaction = (SqliteTransaction)tx;
                    insFts.CommandText = InsertFtsSql;
                    insFts.Parameters.AddWithValue("$id", document.Id.Value);
                    insFts.Parameters.AddWithValue("$plain_text", document.PlainText);
                    insFts.Parameters.AddWithValue("$title", document.Title ?? string.Empty);
                    await insFts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IndexStats> GetStatsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        long count;
        long chars;
        long lastMs;

        var statsCmd = _connection.CreateCommand();
        await using (statsCmd.ConfigureAwait(false))
        {
            statsCmd.CommandText = StatsSql;
            var reader = await statsCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                await reader.ReadAsync(ct).ConfigureAwait(false);
                count = reader.GetInt64(0);
                chars = reader.GetInt64(1);
                lastMs = reader.GetInt64(2);
            }
        }

        var lastIndexedAt = lastMs == 0
            ? DateTimeOffset.MinValue
            : DateTimeOffset.FromUnixTimeMilliseconds(lastMs);

        long pageCount;
        var pageCountCmd = _connection.CreateCommand();
        await using (pageCountCmd.ConfigureAwait(false))
        {
            pageCountCmd.CommandText = PageCountSql;
            pageCount = Convert.ToInt64(
                await pageCountCmd.ExecuteScalarAsync(ct).ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        long pageSize;
        var pageSizeCmd = _connection.CreateCommand();
        await using (pageSizeCmd.ConfigureAwait(false))
        {
            pageSizeCmd.CommandText = PageSizeSql;
            pageSize = Convert.ToInt64(
                await pageSizeCmd.ExecuteScalarAsync(ct).ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return new IndexStats
        {
            DocumentCount = count,
            TotalChars = chars,
            LastIndexedAt = lastIndexedAt,
            IndexSizeBytes = pageCount * pageSize,
        };
    }

    /// <inheritdoc/>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var tx = await _connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (tx.ConfigureAwait(false))
        {
            try
            {
                var delDocs = _connection.CreateCommand();
                await using (delDocs.ConfigureAwait(false))
                {
                    delDocs.Transaction = (SqliteTransaction)tx;
                    delDocs.CommandText = DeleteAllDocumentsSql;
                    await delDocs.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                var delFts = _connection.CreateCommand();
                await using (delFts.ConfigureAwait(false))
                {
                    delFts.Transaction = (SqliteTransaction)tx;
                    delFts.CommandText = DeleteAllFtsSql;
                    await delFts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(DocumentId documentId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ct.ThrowIfCancellationRequested();

        var tx = await _connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (tx.ConfigureAwait(false))
        {
            try
            {
                var delFts = _connection.CreateCommand();
                await using (delFts.ConfigureAwait(false))
                {
                    delFts.Transaction = (SqliteTransaction)tx;
                    delFts.CommandText = DeleteFtsDocumentByIdSql;
                    delFts.Parameters.AddWithValue("$id", documentId.Value);
                    await delFts.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                var delDoc = _connection.CreateCommand();
                await using (delDoc.ConfigureAwait(false))
                {
                    delDoc.Transaction = (SqliteTransaction)tx;
                    delDoc.CommandText = DeleteDocumentByIdSql;
                    delDoc.Parameters.AddWithValue("$id", documentId.Value);
                    await delDoc.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection.Dispose();
    }

    private void EnsureSchema()
    {
        using var verCmd = _connection.CreateCommand();
        verCmd.CommandText = UserVersionSql;
        var version = Convert.ToInt32(verCmd.ExecuteScalar()!, System.Globalization.CultureInfo.InvariantCulture);

        if (version > SchemaVersion)
        {
            throw new InvalidOperationException(
                $"Database schema version {version} is newer than supported version {SchemaVersion}. Upgrade Ferret.Indexing.");
        }

        if (version == SchemaVersion)
        {
            return;
        }

        // version == 0 → create schema
        using var tx = _connection.BeginTransaction();
        try
        {
            using var createDocs = _connection.CreateCommand();
            createDocs.Transaction = tx;
            createDocs.CommandText = CreateDocumentsSql;
            createDocs.ExecuteNonQuery();

            using var createFts = _connection.CreateCommand();
            createFts.Transaction = tx;
            createFts.CommandText = CreateFtsSql;
            createFts.ExecuteNonQuery();

            using var setVersion = _connection.CreateCommand();
            setVersion.Transaction = tx;
            setVersion.CommandText = SetVersionSql;
            setVersion.ExecuteNonQuery();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
