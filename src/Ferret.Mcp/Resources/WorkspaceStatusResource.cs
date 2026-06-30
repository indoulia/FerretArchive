using System.Text.Json;

using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Resources;

/// <summary>MCP resource that exposes workspace and index status.</summary>
public sealed class WorkspaceStatusResource : IMcpResource
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IWorkspaceContext _workspaceContext;
    private readonly IIndexEngine _indexEngine;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceStatusResource"/> class.</summary>
    /// <param name="workspaceContext">Workspace context.</param>
    /// <param name="indexEngine">Index engine for stats.</param>
    public WorkspaceStatusResource(IWorkspaceContext workspaceContext, IIndexEngine indexEngine)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(indexEngine);
        _workspaceContext = workspaceContext;
        _indexEngine = indexEngine;
    }

    /// <inheritdoc/>
    public McpResourceDescriptor Descriptor { get; } = new()
    {
        ResourceUri = "workspace://status",
        Name = "workspace_status",
        Description = "Current Ferret workspace status and index statistics.",
    };

    /// <inheritdoc/>
    public async Task<McpResourceContent> ReadAsync(string resourceUri, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(resourceUri);

        var stats = await _indexEngine.GetStatsAsync(ct).ConfigureAwait(false);
        var text = JsonSerializer.Serialize(
            new
            {
                workspaceId = _workspaceContext.WorkspaceId.Value,
                workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath,
                documentCount = stats.DocumentCount,
                indexSizeBytes = stats.IndexSizeBytes,
                lastIndexedAt = stats.LastIndexedAt,
                totalChars = stats.TotalChars,
            },
            JsonOptions);

        return new McpResourceContent { ResourceUri = resourceUri, MimeType = "application/json", Text = text };
    }
}
