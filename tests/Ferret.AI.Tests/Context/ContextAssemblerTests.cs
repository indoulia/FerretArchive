using Ferret.AI.Context;
using Ferret.Core.Connectors;
using Ferret.Core.Context;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ferret.AI.Tests.Context;

public sealed class ContextAssemblerTests
{
    private static FileSearchHit MakeHit(string docId, float score) =>
        new()
        {
            DocumentId = DocumentId.Create(docId),
            ConnectorInstanceId = new ConnectorInstanceId("test"),
            CanonicalUri = new Uri($"filesystem:///{docId}"),
            DisplayName = docId,
            Kind = SearchHitKind.File,
            Score = score,
            Snippet = new HighlightedText { Spans = [] },
        };

    private static Document MakeDocument(string docId, string text) =>
        new()
        {
            Id = DocumentId.Create(docId),
            SourceAssetId = new AssetId(docId),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            MediaType = "text/plain",
            Kind = DocumentKind.Prose,
            PlainText = text,
            ProducedAt = DateTimeOffset.UtcNow,
        };

    private static ContextAssembler BuildAssembler(
        IReadOnlyList<SearchHit> hits,
        Dictionary<string, Document> docs)
    {
        var searchService = new StubSearchService(hits);
        var docService = new StubDocumentService(docs);
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);
        return new ContextAssembler(searchService, expander, NullLogger<ContextAssembler>.Instance);
    }

    [Fact]
    public async Task AssembleAsync_TwoDocuments_ReturnsBothInPackage()
    {
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.7f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('x', 200)),
            ["b"] = MakeDocument("b", new string('y', 200)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal("test", pkg.Query);
        Assert.Equal(2, pkg.DocumentsIncluded);
        Assert.Equal(2, pkg.Documents.Count);
    }

    [Fact]
    public async Task AssembleAsync_TokenBudget_StopsWhenBudgetExceeded()
    {
        // 52 chars = 13 tokens per doc (4 chars/token). Budget 15 → first fits (13), second would be 26 total > 15.
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.7f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('x', 52)),
            ["b"] = MakeDocument("b", new string('x', 52)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test", MaxTokens = 15 };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(1, pkg.DocumentsIncluded);
    }

    [Fact]
    public async Task AssembleAsync_FirstDocumentAloneExceedsBudget_TruncatesInsteadOfIgnoringBudget()
    {
        // 10,000 chars ~= 2,500 tokens (4 chars/token) -- far larger than a 100-token budget.
        // The always-include-at-least-one-document rule must not mean "MaxTokens is optional
        // when the top hit is big": the returned content has to actually respect the budget.
        var hits = new[] { MakeHit("a", 0.9f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('x', 10_000)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test", MaxTokens = 100 };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(1, pkg.DocumentsIncluded);
        Assert.True(
            pkg.Documents[0].Content.Length < 10_000,
            "content should be truncated to fit the budget, not returned in full");
        Assert.True(
            pkg.TotalTokenEstimate <= 100,
            $"token estimate {pkg.TotalTokenEstimate} should respect the 100-token budget");
    }

    [Fact]
    public async Task AssembleAsync_FirstDocumentAloneExceedsBudget_SmallerBudgetProducesSmallerOutput()
    {
        // Regression guard for the exact live symptom: two different max_tokens values on the
        // same oversized top hit must not produce byte-identical output.
        var hits = new[] { MakeHit("a", 0.9f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('x', 10_000)),
        };

        var pkg100 = await BuildAssembler(hits, docs)
            .AssembleAsync(new ContextRequest { Query = "test", MaxTokens = 100 }, CancellationToken.None);
        var pkg50 = await BuildAssembler(hits, docs)
            .AssembleAsync(new ContextRequest { Query = "test", MaxTokens = 50 }, CancellationToken.None);

        Assert.True(pkg50.Documents[0].Content.Length < pkg100.Documents[0].Content.Length);
    }

    [Fact]
    public async Task AssembleAsync_MaxDocuments_LimitsCount()
    {
        var hits = new[] { MakeHit("a", 0.9f), MakeHit("b", 0.8f), MakeHit("c", 0.7f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('a', 200)),
            ["b"] = MakeDocument("b", new string('b', 200)),
            ["c"] = MakeDocument("c", new string('c', 200)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test", MaxDocuments = 2 };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(2, pkg.DocumentsIncluded);
    }

    [Fact]
    public async Task AssembleAsync_DuplicateHits_DeduplicatedBeforeExpansion()
    {
        var hits = new[]
        {
            MakeHit("a", 0.9f),
            MakeHit("a", 0.5f), // duplicate
        };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('a', 200)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "test" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(1, pkg.DocumentsIncluded);
    }

    [Fact]
    public async Task AssembleAsync_NoSearchResults_ReturnsEmptyPackage()
    {
        var assembler = BuildAssembler([], new Dictionary<string, Document>());
        var request = new ContextRequest { Query = "nothing" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(0, pkg.DocumentsIncluded);
        Assert.Equal("nothing", pkg.Query);
        Assert.False(pkg.SearchFailed);
    }

    [Fact]
    public async Task AssembleAsync_SearchFails_IsDistinguishableFromLegitimateEmptyResult()
    {
        // Issue #21: a failed search (bad query, missing index, missing workspace) must not be
        // indistinguishable from "the query legitimately matched nothing" -- both used to produce
        // an identical DocumentsIncluded == 0 package with zero signal of what happened.
        var searchService = new FailingSearchService(SearchServiceStatus.IndexNotFound);
        var docService = new StubDocumentService(new Dictionary<string, Document>());
        var expander = new DocumentExpander(docService, NullLogger<DocumentExpander>.Instance);
        var assembler = new ContextAssembler(searchService, expander, NullLogger<ContextAssembler>.Instance);
        var request = new ContextRequest { Query = "test" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal(0, pkg.DocumentsIncluded);
        Assert.True(pkg.SearchFailed);
        Assert.NotEmpty(pkg.Diagnostics);
        Assert.Contains("Search failed", pkg.ToPromptString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssembleAsync_DocumentsOrderedByDescendingScore()
    {
        var hits = new[] { MakeHit("b", 0.7f), MakeHit("a", 0.9f) };
        var docs = new Dictionary<string, Document>
        {
            ["a"] = MakeDocument("a", new string('a', 200)),
            ["b"] = MakeDocument("b", new string('b', 200)),
        };
        var assembler = BuildAssembler(hits, docs);
        var request = new ContextRequest { Query = "order" };

        var pkg = await assembler.AssembleAsync(request, CancellationToken.None);

        Assert.Equal("a", pkg.Documents[0].DocumentId.Value);
        Assert.Equal("b", pkg.Documents[1].DocumentId.Value);
    }

    private sealed class StubSearchService(IReadOnlyList<SearchHit> hits) : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
            Task.FromResult(BuildResult(rawQuery, hits));

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            Task.FromResult(BuildResult(query.OriginalText, hits));

        private static SearchServiceResult BuildResult(string query, IReadOnlyList<SearchHit> hits)
        {
            var parsedQuery = new SearchQuery
            {
                OriginalText = query,
                Root = new KeywordExpression(query),
            };
            var result = new SearchResult
            {
                Hits = hits,
                TotalHits = hits.Count,
                ReturnedHits = hits.Count,
            };
            var execInfo = new SearchExecutionInfo
            {
                SessionId = Guid.NewGuid(),
                ProviderName = "stub",
                Duration = TimeSpan.Zero,
                DocumentsScanned = hits.Count,
                IndexVersion = "0",
            };
            return SearchServiceResult.Success(parsedQuery, result, execInfo, new SearchProviderDescriptor
            {
                Id = "stub",
                DisplayName = "Stub",
                Version = "0",
                Capabilities = new SearchCapabilities
                {
                    SupportsKeyword = true,
                    SupportsPhrase = true,
                    SupportsPrefix = true,
                },
            });
        }
    }

    private sealed class FailingSearchService(SearchServiceStatus status) : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
            Task.FromResult(SearchServiceResult.Failure(
                new SearchQuery { OriginalText = rawQuery, Root = new KeywordExpression(rawQuery) },
                status,
                [new SearchDiagnostic(SearchDiagnosticSeverity.Error, "No search index found.")]));

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            SearchAsync(query.OriginalText, options);
    }

    private sealed class StubDocumentService(Dictionary<string, Document> store) : IDocumentService
    {
        public Task<Document?> GetAsync(DocumentId id, CancellationToken ct)
        {
            store.TryGetValue(id.Value, out var doc);
            return Task.FromResult(doc);
        }
    }
}
