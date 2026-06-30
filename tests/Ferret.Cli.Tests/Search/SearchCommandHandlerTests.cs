using System.Text;
using System.Text.Json;

using Ferret.Cli.Cli;
using Ferret.Cli.Search;
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Runtime;
using Ferret.Core.Search;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Ferret.Cli.Tests.Search;

public sealed class SearchCommandHandlerTests
{
    // ── Exit codes ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Returns_Success_On_Successful_Search()
    {
        var handler = MakeHandler(providerHits: [MakeHit("doc-1")]);
        var result = await handler.HandleAsync(
            new SearchCommandArgs { Query = "authentication" },
            new StubFerretContext());
        Assert.Equal(CommandResult.Success, result);
    }

    [Fact]
    public async Task HandleAsync_Returns_Failure_On_EmptyQuery()
    {
        var handler = MakeHandler();
        var result = await handler.HandleAsync(
            new SearchCommandArgs { Query = string.Empty },
            new StubFerretContext());
        Assert.Equal(CommandResult.Failure, result);
    }

    [Fact]
    public async Task HandleAsync_Returns_Failure_When_Index_Not_Found()
    {
        var handler = MakeHandler(status: SearchServiceStatus.IndexNotFound);
        var result = await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth" },
            new StubFerretContext());
        Assert.Equal(CommandResult.Failure, result);
    }

    // ── Output content ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Writes_Results_To_Output()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(providerHits: [MakeHit("auth.cs")]);
        await handler.HandleAsync(new SearchCommandArgs { Query = "auth" }, ctx);
        Assert.True(ctx.Output.Length > 0);
    }

    [Fact]
    public async Task HandleAsync_NoHighlight_Output_Contains_No_Escape_Sequences()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(providerHits: [MakeHit("auth.cs")]);
        await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth", NoHighlight = true }, ctx);
        Assert.DoesNotContain("\x1B[", ctx.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_Json_Format_Produces_Valid_Json_Output()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(providerHits: [MakeHit("auth.cs")]);
        await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth", Format = SearchOutputFormat.Json }, ctx);
        Assert.True(IsValidJson(ctx.Output));
    }

    [Fact]
    public async Task HandleAsync_Limit_Is_Passed_To_Search_Service()
    {
        int capturedLimit = 0;
        var stub = new CapturingSearchService(onSearch: opts => capturedLimit = opts.MaxResults);
        var handler = new SearchCommandHandler(stub, new SearchRendererSelector(new NullTextStyler()));
        await handler.HandleAsync(
            new SearchCommandArgs { Query = "auth", Limit = 5 }, new StubFerretContext());
        Assert.Equal(5, capturedLimit);
    }

    [Fact]
    public async Task HandleAsync_Error_Written_On_Failure()
    {
        var ctx = new StubFerretContext();
        var handler = MakeHandler(status: SearchServiceStatus.IndexNotFound);
        await handler.HandleAsync(new SearchCommandArgs { Query = "auth" }, ctx);
        Assert.True(ctx.Output.Length > 0 || ctx.ErrorOutput.Length > 0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchCommandHandler MakeHandler(
        IReadOnlyList<FileSearchHit>? providerHits = null,
        SearchServiceStatus status = SearchServiceStatus.Success) =>
        new SearchCommandHandler(
            new StubSearchService(providerHits ?? [], status),
            new SearchRendererSelector(new NullTextStyler()));

    private static FileSearchHit MakeHit(string name) =>
        new()
        {
            DocumentId = DocumentId.Create(name),
            ConnectorInstanceId = new ConnectorInstanceId(string.Empty),
            CanonicalUri = new Uri($"file:///{name}"),
            DisplayName = name,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = new HighlightedText { Spans = [new TextSpan(name, TextSpanKind.Normal)] },
        };

    private static bool IsValidJson(string text)
    {
        try
        {
            JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class StubSearchService : ISearchService
    {
        private readonly IReadOnlyList<FileSearchHit> _hits;
        private readonly SearchServiceStatus _status;

        public StubSearchService(IReadOnlyList<FileSearchHit> hits, SearchServiceStatus status)
        {
            _hits = hits;
            _status = status;
        }

        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
        {
            if (string.IsNullOrWhiteSpace(rawQuery))
            {
                var stubQuery = new SearchQuery
                {
                    OriginalText = rawQuery ?? string.Empty,
                    Root = new KeywordExpression(string.Empty),
                };
                return Task.FromResult(SearchServiceResult.Failure(
                    stubQuery, SearchServiceStatus.InvalidQuery, []));
            }

            var query = new SearchQuery
            {
                OriginalText = rawQuery,
                Root = new KeywordExpression(rawQuery),
            };

            return Task.FromResult(
                _status == SearchServiceStatus.Success
                    ? SearchServiceResult.Success(
                        query,
                        new SearchResult
                        {
                            Hits = _hits,
                            TotalHits = _hits.Count,
                            ReturnedHits = _hits.Count,
                        },
                        MakeInfo())
                    : SearchServiceResult.Failure(query, _status, []));
        }

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            SearchAsync(query.OriginalText, options);

        private static SearchExecutionInfo MakeInfo() =>
            new()
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "stub",
                Duration = TimeSpan.FromMilliseconds(1),
                DocumentsScanned = 0,
                IndexVersion = "stub",
            };
    }

    private sealed class CapturingSearchService : ISearchService
    {
        private readonly Action<SearchOptions> _onSearch;

        public CapturingSearchService(Action<SearchOptions> onSearch) =>
            _onSearch = onSearch;

        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options)
        {
            _onSearch(options);
            var query = new SearchQuery
            {
                OriginalText = rawQuery,
                Root = new KeywordExpression(rawQuery),
            };
            return Task.FromResult(SearchServiceResult.Success(
                query,
                new SearchResult { Hits = [], TotalHits = 0, ReturnedHits = 0 },
                new SearchExecutionInfo
                {
                    SessionId = Guid.NewGuid(),
                    ProviderName = "stub",
                    Duration = TimeSpan.Zero,
                    DocumentsScanned = 0,
                    IndexVersion = "stub",
                }));
        }

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            SearchAsync(query.OriginalText, options);
    }

    private sealed class StubFerretContext : IFerretContext
    {
        private readonly StubOutputFormatter _formatter = new();

        public StubFerretContext() =>
            Services = new StubFerretServices(_formatter);

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
