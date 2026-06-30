using System.Text;

using Ferret.Cli.Cli;
using Ferret.Cli.Search;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Core.Search;
using Ferret.Core.Workspace;
using Ferret.Search;
using Ferret.Search.Providers.Bm25;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Ferret.Cli.Tests.Search;

/// <summary>
/// End-to-end: real SQLite FTS5 DB → BM25SearchProvider → SearchService → SearchCommandHandler → rendered output.
/// </summary>
public sealed class SearchIntegrationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly SearchCommandHandler _handler;

    /// <summary>Initializes a new instance of the <see cref="SearchIntegrationTests"/> class.</summary>
    public SearchIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ferret-e2e-{Guid.NewGuid():N}");
        var indexDir = Path.Combine(_tempRoot, ".ferret", "indexes", "keyword");
        Directory.CreateDirectory(indexDir);
        var dbPath = Path.Combine(indexDir, "keyword-index.db");

        SeedDatabase(dbPath);

        var workspaceContext = new StubWorkspaceContext(_tempRoot);
        var queryParser = new QueryParser();
        var provider = new Bm25SearchProvider(workspaceContext);
        var service = new SearchService(queryParser, [provider], []);
        var renderer = new SearchRendererSelector(new NullTextStyler());

        _handler = new SearchCommandHandler(service, renderer);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_Keyword_Returns_Success()
    {
        var result = await _handler.HandleAsync(
            new SearchCommandArgs { Query = "authentication" }, new CapturingContext());
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task Search_Keyword_Produces_Non_Empty_Output()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(new SearchCommandArgs { Query = "authentication" }, ctx);
        Assert.True(ctx.Output.Length > 0);
    }

    [Fact]
    public async Task Search_Keyword_Output_Contains_Matching_Document_Name()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(new SearchCommandArgs { Query = "authentication" }, ctx);
        Assert.Contains("AuthToken", ctx.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_Json_Format_Is_Valid_Json()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(
            new SearchCommandArgs { Query = "authentication", Format = SearchOutputFormat.Json }, ctx);
        var doc = System.Text.Json.JsonDocument.Parse(ctx.Output);
        Assert.NotNull(doc);
    }

    [Fact]
    public async Task Search_Empty_Query_Returns_Failure()
    {
        var result = await _handler.HandleAsync(
            new SearchCommandArgs { Query = string.Empty }, new CapturingContext());
        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task Search_Phrase_Returns_Matching_Results()
    {
        var ctx = new CapturingContext();
        var result = await _handler.HandleAsync(
            new SearchCommandArgs { Query = "\"runtime initialization\"" }, ctx);
        Assert.Equal(CommandResult.Success, result);
        Assert.Contains("RuntimeBuilder", ctx.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_Prefix_Returns_Success()
    {
        var result = await _handler.HandleAsync(
            new SearchCommandArgs { Query = "auth*" }, new CapturingContext());
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task Search_Limit_One_Returns_At_Most_One_Hit_In_Json()
    {
        var ctx = new CapturingContext();
        await _handler.HandleAsync(
            new SearchCommandArgs { Query = "content", Limit = 1, Format = SearchOutputFormat.Json }, ctx);
        var doc = System.Text.Json.JsonDocument.Parse(ctx.Output);
        var hitsCount = doc.RootElement.GetProperty("hits").GetArrayLength();
        Assert.True(hitsCount <= 1);
    }

    // ── Seed ──────────────────────────────────────────────────────────────────

    private static void SeedDatabase(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE documents (
                id TEXT PRIMARY KEY,
                connector_id TEXT NOT NULL DEFAULT '',
                instance_id TEXT NOT NULL DEFAULT '',
                media_type TEXT NOT NULL DEFAULT '',
                kind TEXT NOT NULL DEFAULT '',
                plain_text TEXT NOT NULL DEFAULT '',
                title TEXT NOT NULL DEFAULT '',
                produced_at TEXT NOT NULL DEFAULT ''
            );
            CREATE VIRTUAL TABLE documents_fts USING fts5(
                id UNINDEXED,
                plain_text,
                title
            );
            INSERT INTO documents (id, connector_id, instance_id, title, plain_text) VALUES
                ('doc-1', 'fs', 'fs-default', 'AuthToken', 'Token-based authentication content here'),
                ('doc-2', 'fs', 'fs-default', 'SessionManager', 'Session management for authenticated users'),
                ('doc-3', 'fs', 'fs-default', 'RuntimeBuilder', 'Builder content for runtime initialization');
            INSERT INTO documents_fts (id, title, plain_text) VALUES
                ('doc-1', 'AuthToken', 'Token-based authentication content here'),
                ('doc-2', 'SessionManager', 'Session management for authenticated users'),
                ('doc-3', 'RuntimeBuilder', 'Builder content for runtime initialization');
            """;
        cmd.ExecuteNonQuery();
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubWorkspaceContext : IWorkspaceContext
    {
        public StubWorkspaceContext(string root)
        {
            WorkspaceRoot = WorkspacePath.Create(root);
            WorkspaceId = WorkspaceId.Create("e2e-test");
        }

        public WorkspaceId WorkspaceId { get; }

        public WorkspacePath WorkspaceRoot { get; }
    }

    private sealed class CapturingContext : IFerretContext
    {
        private readonly StubOutputFormatter _formatter = new();

        public CapturingContext() => Services = new StubFerretServices(_formatter);

        public string Output => _formatter.Output;

        public string ErrorOutput => _formatter.ErrorOutput;

        public CancellationToken CancellationToken => CancellationToken.None;

        public VerbosityLevel Verbosity => VerbosityLevel.Normal;

        public OutputFormat OutputFormat => OutputFormat.Text;

        public IFerretServices Services { get; }

        public string WorkingDirectory => string.Empty;

        public T? GetOption<T>(string name) => default;
    }

    private sealed class StubFerretServices : IFerretServices
    {
        public StubFerretServices(IOutputFormatter output) => Output = output;

        public IServiceProvider Services => throw new NotSupportedException();

        public IConfiguration Configuration => throw new NotSupportedException();

        public ILoggerFactory LoggerFactory => throw new NotSupportedException();

        public IOutputFormatter Output { get; }

        public IRuntimeHost? Runtime => null;
    }

    private sealed class StubOutputFormatter : IOutputFormatter
    {
        private readonly StringBuilder _out = new();
        private readonly StringBuilder _err = new();

        public string Output => _out.ToString();

        public string ErrorOutput => _err.ToString();

        public void WriteLine(string text = "") => _out.AppendLine(text);

        public void WriteSuccess(string message) => _out.AppendLine(message);

        public void WriteError(string message) => _err.AppendLine(message);

        public void WriteVerbose(string message)
        {
        }
    }
}
