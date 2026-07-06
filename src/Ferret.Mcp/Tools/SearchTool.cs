using System.Globalization;
using System.Text;

using Ferret.Core.Search;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that searches the Ferret workspace index.</summary>
public sealed class SearchTool : IMcpTool
{
    private readonly ISearchService _searchService;

    /// <summary>Initializes a new instance of the <see cref="SearchTool"/> class.</summary>
    /// <param name="searchService">Platform search service.</param>
    public SearchTool(ISearchService searchService)
    {
        ArgumentNullException.ThrowIfNull(searchService);
        _searchService = searchService;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "search",
        Description = "Search the Ferret workspace index for relevant documents and code.",
        InputSchemaJson = """{"type":"object","properties":{"query":{"type":"string","description":"Full-text search query"},"max_results":{"type":"integer","description":"Maximum results to return (default: 10)"}},"required":["query"]}""",
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var query = arguments.GetRequiredString("query");
        var maxResults = arguments.TryGetInt32("max_results", out var n) ? n : 10;

        var options = new SearchOptions { MaxResults = maxResults, HighlightEnabled = true };
        var result = await _searchService.SearchAsync(query, options).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var message = result.Status switch
            {
                SearchServiceStatus.InvalidQuery =>
                    $"Invalid query: {(result.Diagnostics.Count > 0 ? result.Diagnostics[0].Message : "empty or whitespace")}",
                SearchServiceStatus.IndexNotFound => "No search index found. Run 'ferret index' first.",
                SearchServiceStatus.WorkspaceNotFound => "No workspace found. Run 'ferret workspace init' first.",
                SearchServiceStatus.ProviderUnavailable => "No search provider is available for this query.",
                _ => $"Search failed: {result.Status}",
            };
            return McpToolResult.Error(message);
        }

        if (result.Hits.Count == 0)
        {
            return McpToolResult.Success($"No results found for: {query}");
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Found {result.Hits.Count} result(s) for: {query}");
        sb.AppendLine();

        for (var i = 0; i < result.Hits.Count; i++)
        {
            var hit = result.Hits[i];
            var snippetText = string.Concat(hit.Snippet.Spans.Select(s => s.Text));
            sb.AppendLine(CultureInfo.InvariantCulture, $"[{i + 1}] {hit.DisplayName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    Document ID: {hit.DocumentId.Value}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    URI: {hit.CanonicalUri}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    Score: {hit.Score:F3}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    {snippetText}");
            sb.AppendLine();
        }

        return McpToolResult.Success(sb.ToString().TrimEnd());
    }
}
