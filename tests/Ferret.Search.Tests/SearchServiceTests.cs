using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Xunit;

namespace Ferret.Search.Tests;

public sealed class SearchServiceTests
{
    // ── String overload: parse failure path ──────────────────────────────────

    [Fact]
    public async Task SearchAsync_String_EmptyQuery_Returns_InvalidQuery_Status()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);
        var result = await service.SearchAsync(string.Empty, DefaultOptions());
        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.InvalidQuery, result.Status);
    }

    [Fact]
    public async Task SearchAsync_String_WhitespaceQuery_Returns_InvalidQuery_Status()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);
        var result = await service.SearchAsync("   ", DefaultOptions());
        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.InvalidQuery, result.Status);
    }

    // ── No provider ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_NoProviders_Returns_ProviderUnavailable_Status()
    {
        var service = MakeService([]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.ProviderUnavailable, result.Status);
    }

    [Fact]
    public async Task SearchAsync_AllProvidersRefuse_Returns_ProviderUnavailable_Status()
    {
        var service = MakeService([new NeverCapableProvider()]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.False(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.ProviderUnavailable, result.Status);
    }

    // ── Success path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_ReturnsSuccess_When_Provider_Succeeds()
    {
        var hit = MakeHit("doc-1");
        var service = MakeService([new AlwaysSucceedProvider([hit])]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.True(result.IsSuccess);
        Assert.Equal(SearchServiceStatus.Success, result.Status);
    }

    [Fact]
    public async Task SearchAsync_Hits_Match_Provider_Output()
    {
        var hit = MakeHit("doc-42");
        var service = MakeService([new AlwaysSucceedProvider([hit])]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.Single(result.Hits);
        Assert.Equal("doc-42", result.Hits[0].DocumentId.ToString());
    }

    [Fact]
    public async Task SearchAsync_ExecutionInfo_Is_Populated_On_Success()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.NotNull(result.ExecutionInfo);
        Assert.False(string.IsNullOrEmpty(result.ExecutionInfo!.ProviderName));
        Assert.NotEqual(Guid.Empty, result.ExecutionInfo.SessionId);
    }

    [Fact]
    public async Task SearchAsync_Duration_Is_Non_Negative()
    {
        var service = MakeService([new AlwaysSucceedProvider()]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.True(result.ExecutionInfo!.Duration >= TimeSpan.Zero);
    }

    // ── Post-processor ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_PostProcessor_Can_Filter_Hits()
    {
        SearchHit[] hits = [MakeHit("keep"), MakeHit("drop")];
        var service = MakeService([new AlwaysSucceedProvider(hits)], [new RemoveHitProcessor("drop")]);
        var result = await service.SearchAsync(MakeQuery("auth"), DefaultOptions());
        Assert.Single(result.Hits);
        Assert.Equal("keep", result.Hits[0].DocumentId.ToString());
    }

    [Fact]
    public async Task SearchAsync_String_Overload_Success_Populates_Hits()
    {
        var hit = MakeHit("doc-1");
        var service = MakeService([new AlwaysSucceedProvider([hit])]);
        var result = await service.SearchAsync("authentication", DefaultOptions());
        Assert.True(result.IsSuccess);
        Assert.Single(result.Hits);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchService MakeService(
        IReadOnlyList<ISearchProvider> providers,
        IReadOnlyList<ISearchPostProcessor>? postProcessors = null) =>
        new SearchService(new QueryParser(), providers, postProcessors ?? []);

    private static SearchQuery MakeQuery(string keyword) =>
        new() { OriginalText = keyword, Root = new KeywordExpression(keyword) };

    private static SearchOptions DefaultOptions() =>
        new() { MaxResults = 20, Mode = SearchExecutionMode.Auto };

    private static FileSearchHit MakeHit(string id) =>
        new()
        {
            DocumentId = DocumentId.Create(id),
            ConnectorInstanceId = new ConnectorInstanceId(string.Empty),
            CanonicalUri = new Uri($"file:///{id}"),
            DisplayName = id,
            Kind = SearchHitKind.File,
            Score = 1.0f,
            Snippet = new HighlightedText { Spans = [new TextSpan(id, TextSpanKind.Normal)] },
        };

    // ── Stub providers / processors ───────────────────────────────────────────

    private sealed class AlwaysSucceedProvider : ISearchProvider
    {
        private readonly IReadOnlyList<SearchHit> _hits;

        public AlwaysSucceedProvider(IReadOnlyList<SearchHit>? hits = null) =>
            _hits = hits ?? [];

        public SearchProviderDescriptor Descriptor { get; } = new()
        {
            Id = "stub-success",
            DisplayName = "Stub Success Provider",
            Version = "1.0.0",
            Capabilities = new SearchCapabilities
            {
                SupportsKeyword = true,
                SupportsPhrase = true,
                SupportsPrefix = true,
            },
        };

        public SearchCapabilities Capabilities => Descriptor.Capabilities;

        public Task<SearchProviderResult> SearchAsync(
            SearchQuery query, SearchOptions options, CancellationToken ct) =>
            Task.FromResult(SearchProviderResult.Success(_hits, documentsScanned: _hits.Count, indexVersion: "stub"));
    }

    private sealed class NeverCapableProvider : ISearchProvider
    {
        public SearchProviderDescriptor Descriptor { get; } = new()
        {
            Id = "stub-never",
            DisplayName = "Stub Never Provider",
            Version = "1.0.0",
            Capabilities = new SearchCapabilities
            {
                SupportsKeyword = false,
                SupportsPhrase = false,
                SupportsPrefix = false,
            },
        };

        public SearchCapabilities Capabilities => Descriptor.Capabilities;

        public Task<SearchProviderResult> SearchAsync(
            SearchQuery query, SearchOptions options, CancellationToken ct) =>
            throw new InvalidOperationException("Should not be called.");
    }

    private sealed class RemoveHitProcessor : ISearchPostProcessor
    {
        private readonly string _documentIdToRemove;

        public RemoveHitProcessor(string documentIdToRemove) =>
            _documentIdToRemove = documentIdToRemove;

        public Task<IReadOnlyList<SearchHit>> ProcessAsync(
            IReadOnlyList<SearchHit> hits, SearchQuery query, SearchOptions options)
        {
            IReadOnlyList<SearchHit> filtered =
                [..hits.Where(h => h.DocumentId.ToString() != _documentIdToRemove)];
            return Task.FromResult(filtered);
        }
    }
}
