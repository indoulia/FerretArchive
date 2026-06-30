using System.Text.Json;

using Ferret.Core.Indexing;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Resources;

/// <summary>MCP resource that exposes keyword index statistics.</summary>
public sealed class IndexStatsResource : IMcpResource
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IIndexEngine _indexEngine;

    /// <summary>Initializes a new instance of the <see cref="IndexStatsResource"/> class.</summary>
    /// <param name="indexEngine">Index engine for stats.</param>
    public IndexStatsResource(IIndexEngine indexEngine)
    {
        ArgumentNullException.ThrowIfNull(indexEngine);
        _indexEngine = indexEngine;
    }

    /// <inheritdoc/>
    public McpResourceDescriptor Descriptor { get; } = new()
    {
        ResourceUri = "workspace://index/stats",
        Name = "index_stats",
        Description = "Ferret keyword index statistics.",
    };

    /// <inheritdoc/>
    public async Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(resourceUri);

        var stats = await _indexEngine.GetStatsAsync(ct).ConfigureAwait(false);
        var text = JsonSerializer.Serialize(
            new
            {
                documentCount = stats.DocumentCount,
                totalChars = stats.TotalChars,
                indexSizeBytes = stats.IndexSizeBytes,
                lastIndexedAt = stats.LastIndexedAt,
            },
            JsonOptions);

        return new McpResourceContent { ResourceUri = resourceUri, MimeType = "application/json", Text = text };
    }
}
