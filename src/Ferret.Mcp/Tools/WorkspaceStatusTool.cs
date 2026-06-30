using System.Text.Json;

using Ferret.Core.Indexing;
using Ferret.Core.Workspace;
using Ferret.Mcp.Protocol;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that reports current workspace and index status.</summary>
public sealed class WorkspaceStatusTool : IMcpTool
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IWorkspaceContext _workspaceContext;
    private readonly IIndexEngine _indexEngine;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceStatusTool"/> class.</summary>
    /// <param name="workspaceContext">Workspace context.</param>
    /// <param name="indexEngine">Index engine for stats.</param>
    public WorkspaceStatusTool(IWorkspaceContext workspaceContext, IIndexEngine indexEngine)
    {
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(indexEngine);
        _workspaceContext = workspaceContext;
        _indexEngine = indexEngine;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "workspace_status",
        Description = "Get the current Ferret workspace status including index statistics.",
        InputSchemaJson = """{"type":"object","properties":{}}""",
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var stats = await _indexEngine.GetStatsAsync(ct).ConfigureAwait(false);

        var payload = new
        {
            workspaceId = _workspaceContext.WorkspaceId.Value,
            workspaceRoot = _workspaceContext.WorkspaceRoot.FullPath,
            documentCount = stats.DocumentCount,
            indexSizeBytes = stats.IndexSizeBytes,
            lastIndexedAt = stats.LastIndexedAt,
            totalChars = stats.TotalChars,
        };

        return McpToolResult.Success(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
