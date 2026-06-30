using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Microsoft.Data.Sqlite;

namespace Ferret.Indexing;

/// <summary>Reads documents by ID from the keyword-index SQLite database.</summary>
public sealed class DocumentService : IDocumentService
{
    private readonly string _dbPath;

    /// <summary>Initializes a new instance of the <see cref="DocumentService"/> class.</summary>
    /// <param name="dbPath">Full path to the SQLite keyword-index database file.</param>
    public DocumentService(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        _dbPath = dbPath;
    }

    /// <inheritdoc/>
    public async Task<Document?> GetAsync(DocumentId id, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(id);

        var connection = new SqliteConnection($"Data Source={_dbPath}");
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT id, connector_id, instance_id, media_type, kind, plain_text, title, produced_at " +
                "FROM documents WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id.Value);

            var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            try
            {
                if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return null;
                }

                var title = await reader.IsDBNullAsync(6, ct).ConfigureAwait(false)
                    ? null
                    : reader.GetString(6);

                return new Document
                {
                    Id = DocumentId.Create(reader.GetString(0)),
                    ConnectorId = new ConnectorId(reader.GetString(1)),
                    InstanceId = new ConnectorInstanceId(reader.GetString(2)),
                    SourceAssetId = new AssetId(reader.GetString(0)), // id == SourceAssetId.Value per Sprint 9
                    MediaType = reader.GetString(3),
                    Kind = (DocumentKind)reader.GetInt32(4),
                    PlainText = reader.GetString(5),
                    Title = title,
                    ProducedAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)),
                };
            }
            finally
            {
                await reader.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
