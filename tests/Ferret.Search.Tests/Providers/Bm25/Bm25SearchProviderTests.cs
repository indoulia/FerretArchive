using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Search.Providers.Bm25;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Ferret.Search.Tests.Providers.Bm25;

public sealed class Bm25SearchProviderTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _dbPath;
    private readonly Bm25SearchProvider _provider;

    public Bm25SearchProviderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ferret-test-{Guid.NewGuid():N}");
        var indexDir = Path.Combine(_tempRoot, ".ferret", "indexes", "keyword");
        Directory.CreateDirectory(indexDir);
        _dbPath = Path.Combine(indexDir, "keyword-index.db");

        SeedDatabase(_dbPath);
        _provider = new Bm25SearchProvider(new StubWorkspaceContext(_tempRoot));
    }

    // ── Descriptor / Capabilities ─────────────────────────────────────────────

    [Fact]
    public void Descriptor_Id_Is_Bm25Fts5()
    {
        Assert.Equal("bm25-fts5", _provider.Descriptor.Id);
    }

    [Fact]
    public void Capabilities_SupportsKeyword_Is_True()
    {
        Assert.True(_provider.Capabilities.SupportsKeyword);
    }

    [Fact]
    public void Capabilities_SupportsSemantic_Is_False()
    {
        Assert.False(_provider.Capabilities.SupportsSemantic);
    }

    // ── SearchAsync: success cases ────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_Single_Keyword_Returns_Matching_Hits()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var result = await _provider.SearchAsync(query, DefaultOptions());
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Hits);
        Assert.All(result.Hits, h => Assert.False(string.IsNullOrEmpty(h.DisplayName)));
    }

    [Fact]
    public async Task SearchAsync_Score_Is_Positive()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var result = await _provider.SearchAsync(query, DefaultOptions());
        Assert.All(result.Hits, h => Assert.True(h.Score > 0f));
    }

    [Fact]
    public async Task SearchAsync_Snippet_Has_At_Least_One_Span()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var result = await _provider.SearchAsync(query, DefaultOptions());
        Assert.All(result.Hits, h => Assert.NotEmpty(h.Snippet.Spans));
    }

    [Fact]
    public async Task SearchAsync_Snippet_Contains_Match_Span_For_Query_Term()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var result = await _provider.SearchAsync(query, DefaultOptions());
        var firstHit = result.Hits[0];
        Assert.Contains(firstHit.Snippet.Spans, s => s.Kind == TextSpanKind.Match);
    }

    [Fact]
    public async Task SearchAsync_Returns_FileSearchHit_Type()
    {
        var query = MakeQuery(new KeywordExpression("authentication"));
        var result = await _provider.SearchAsync(query, DefaultOptions());
        Assert.All(result.Hits, h => Assert.IsType<FileSearchHit>(h));
    }

    // ── SearchAsync: missing index ─────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_Returns_IndexNotFound_When_Database_Missing()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        var query = MakeQuery(new KeywordExpression("auth"));
        var result = await _provider.SearchAsync(query, DefaultOptions());
        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.IndexNotFound, result.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchQuery MakeQuery(SearchExpression root) =>
        new() { OriginalText = "test", Root = root };

    private static SearchOptions DefaultOptions() =>
        new() { MaxResults = 20, Mode = SearchExecutionMode.Keyword };

    private static void SeedDatabase(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();
        using var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA journal_mode=DELETE;";
        pragmaCmd.ExecuteNonQuery();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE documents (
                id           TEXT NOT NULL PRIMARY KEY,
                connector_id TEXT NOT NULL DEFAULT '',
                instance_id  TEXT NOT NULL DEFAULT '',
                media_type   TEXT NOT NULL DEFAULT '',
                kind         INTEGER NOT NULL DEFAULT 0,
                plain_text   TEXT NOT NULL DEFAULT '',
                title        TEXT,
                produced_at  INTEGER NOT NULL DEFAULT 0
            );
            CREATE VIRTUAL TABLE documents_fts USING fts5(
                id UNINDEXED,
                plain_text,
                title
            );
            """;
        cmd.ExecuteNonQuery();

        cmd.CommandText = """
            INSERT INTO documents (id, connector_id, instance_id, media_type, kind, plain_text, title, produced_at)
            VALUES
                ('file:///src/auth/token.cs',   'filesystem', 'fs-1', 'text/plain', 1, 'Token-based authentication content here', 'AuthenticationToken', 0),
                ('file:///src/auth/session.cs',  'filesystem', 'fs-1', 'text/plain', 1, 'Session management for authenticated users', 'SessionManager', 0),
                ('file:///src/runtime/builder.cs','filesystem', 'fs-1', 'text/plain', 1, 'Builder content for runtime initialization', 'RuntimeBuilder', 0);
            INSERT INTO documents_fts (id, plain_text, title)
            SELECT id, plain_text, title FROM documents;
            """;
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private sealed class StubWorkspaceContext : IWorkspaceContext
    {
        public StubWorkspaceContext(string rootPath) =>
            WorkspaceRoot = WorkspacePath.Create(rootPath);

        public WorkspaceId WorkspaceId => WorkspaceId.Create("test-workspace");

        public WorkspacePath WorkspaceRoot { get; }
    }
}
