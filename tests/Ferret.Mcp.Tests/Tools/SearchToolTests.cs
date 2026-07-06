using Ferret.Core.Connectors;
using Ferret.Core.Primitives;
using Ferret.Core.Search;
using Ferret.Mcp.Protocol;
using Ferret.Mcp.Tools;

using Xunit;

namespace Ferret.Mcp.Tests.Tools;

public sealed class SearchToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithResults_ReturnsFormattedHits()
    {
        var service = new FakeSearchService([MakeHit("doc-1", "Main.cs", "some relevant code")]);
        var sut = new SearchTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("query", "relevant")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("Main.cs", result.Content[0].Text, StringComparison.Ordinal);
        Assert.Contains("relevant code", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_WithResults_IncludesDocumentIdUsableByReadDocumentTool()
    {
        // read_document's own contract is "Document ID from a search result" -- but the only
        // identifier this tool printed was CanonicalUri, which read_document cannot resolve
        // (it looks up by DocumentId, not by the display URI). Every result must expose the
        // literal DocumentId.Value so an AI caller can copy it straight into read_document.
        var service = new FakeSearchService([MakeHit("filesystem:///docs/Overview.md", "Overview.md", "intro text")]);
        var sut = new SearchTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("query", "intro")), CancellationToken.None);

        Assert.Contains("Document ID: filesystem:///docs/Overview.md", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_NoResults_ReturnsNoResultsMessage()
    {
        var service = new FakeSearchService([]);
        var sut = new SearchTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("query", "nothing")), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("No results", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidQuery_ReturnsError_NotFalseNoResults()
    {
        // A failed SearchServiceResult exposes Hits as an empty list (by design, for
        // successful-but-empty callers), but SearchTool must distinguish "ran fine, found
        // nothing" from "didn't run at all" -- otherwise a rejected query (e.g. the FTS5
        // hyphen bug) is misreported as "No results found", hiding the real failure from
        // an AI caller that has no other way to know the query never actually executed.
        var service = new FailingSearchService(SearchServiceStatus.InvalidQuery);
        var sut = new SearchTool(service);

        var result = await sut.ExecuteAsync(McpArguments.From(("query", "nem-3795")), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.DoesNotContain("No results", result.Content[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_MissingQueryArgument_Throws()
    {
        var sut = new SearchTool(new FakeSearchService([]));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ExecuteAsync(McpArguments.Empty, CancellationToken.None));
    }

    [Fact]
    public void Descriptor_HasCorrectName()
    {
        var sut = new SearchTool(new FakeSearchService([]));
        Assert.Equal("search", sut.Descriptor.Name);
    }

    private static FileSearchHit MakeHit(string docId, string displayName, string snippet) => new()
    {
        DocumentId = DocumentId.Create(docId),
        ConnectorInstanceId = new ConnectorInstanceId("fs-1"),
        CanonicalUri = new Uri($"file:///src/{displayName}"),
        DisplayName = displayName,
        Kind = SearchHitKind.File,
        Score = 0.9f,
        Snippet = HighlightedText.Plain(snippet),
    };

    private sealed class FakeSearchService(IReadOnlyList<FileSearchHit> hits) : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
            Task.FromResult(SearchServiceResult.Success(
                new SearchQuery { OriginalText = rawQuery, Root = new KeywordExpression(rawQuery) },
                new SearchResult { Hits = hits, TotalHits = hits.Count, ReturnedHits = hits.Count },
                new SearchExecutionInfo
                {
                    SessionId = Guid.Empty,
                    ProviderName = "fake",
                    Duration = TimeSpan.Zero,
                    DocumentsScanned = 0,
                    IndexVersion = "0",
                }));

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            SearchAsync(query.OriginalText, options);
    }

    private sealed class FailingSearchService(SearchServiceStatus status) : ISearchService
    {
        public Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options) =>
            Task.FromResult(SearchServiceResult.Failure(
                new SearchQuery { OriginalText = rawQuery, Root = new KeywordExpression(string.Empty) },
                status,
                []));

        public Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options) =>
            SearchAsync(query.OriginalText, options);
    }
}
