using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Core.Workspace;

using Microsoft.Data.Sqlite;

namespace Ferret.Search.Providers.Bm25;

/// <summary>
/// BM25 keyword search provider backed by SQLite FTS5.
/// Reads from the keyword index at <c>.ferret/indexes/keyword/keyword-index.db</c>.
/// </summary>
public sealed class Bm25SearchProvider : ISearchProvider
{
    private readonly IWorkspaceContext _workspace;

    /// <summary>Initializes a new instance of the <see cref="Bm25SearchProvider"/> class.</summary>
    /// <param name="workspace">The workspace context providing the index path.</param>
    public Bm25SearchProvider(IWorkspaceContext workspace)
    {
        _workspace = workspace;
    }

    /// <inheritdoc/>
    public SearchProviderDescriptor Descriptor { get; } = new()
    {
        Id = "bm25-fts5",
        DisplayName = "BM25 FTS5 Keyword Search",
        Version = "1.0.0",
        Capabilities = new SearchCapabilities
        {
            SupportsKeyword = true,
            SupportsPhrase = true,
            SupportsPrefix = true,
        },
    };

    /// <inheritdoc/>
    public SearchCapabilities Capabilities => Descriptor.Capabilities;

    /// <inheritdoc/>
    public async Task<SearchProviderResult> SearchAsync(
        SearchQuery query, SearchOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        var dbPath = GetDatabasePath();

        if (!File.Exists(dbPath))
        {
            return SearchProviderResult.Failure(SearchServiceStatus.IndexNotFound);
        }

        try
        {
            return await ExecuteAsync(dbPath, query, options, ct).ConfigureAwait(false);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            return SearchProviderResult.Failure(SearchServiceStatus.InvalidQuery);
        }
    }

    private static async Task<SearchProviderResult> ExecuteAsync(
        string dbPath, SearchQuery query, SearchOptions options, CancellationToken ct)
    {
        var ftsQuery = QueryTranslator.Translate(query.Root);

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString();

        var connection = new SqliteConnection(cs);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);

            var cmd = connection.CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
                cmd.CommandText = """
                    SELECT
                        d.id,
                        d.connector_id,
                        d.instance_id,
                        d.title,
                        snippet(documents_fts, 1, char(2), char(3), '...', 15) AS snippet,
                        documents_fts.rank
                    FROM documents_fts
                    JOIN documents d ON d.id = documents_fts.id
                    WHERE documents_fts MATCH @query
                    ORDER BY documents_fts.rank
                    LIMIT @limit
                    """;
                cmd.Parameters.AddWithValue("@query", ftsQuery);
                cmd.Parameters.AddWithValue("@limit", options.MaxResults);

                var hits = new List<SearchHit>();

                var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        hits.Add(BuildHit(reader));
                    }
                }

                return SearchProviderResult.Success(hits, documentsScanned: hits.Count, indexVersion: "fts5");
            }
        }
    }

    private static FileSearchHit BuildHit(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var connectorId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        var instanceId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        var title = reader.IsDBNull(3) ? id : reader.GetString(3);
        var snippetText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
        var rank = reader.GetDouble(5);

        return new FileSearchHit
        {
            DocumentId = DocumentId.Create(id),
            ConnectorInstanceId = new ConnectorInstanceId(instanceId),
            CanonicalUri = new Uri(id.StartsWith("file://", StringComparison.Ordinal) ? id : $"file:///{id}"),
            DisplayName = string.IsNullOrEmpty(title) ? id : title,
            Kind = SearchHitKind.File,
            Score = (float)-rank,
            Snippet = HighlightParser.Parse(snippetText),
        };
    }

    private string GetDatabasePath() =>
        Path.Combine(_workspace.WorkspaceRoot.FullPath, ".ferret", "indexes", "keyword", "keyword-index.db");
}
