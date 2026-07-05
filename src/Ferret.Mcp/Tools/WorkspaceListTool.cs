using System.Text.Json;

using Ferret.Mcp.Protocol;
using Ferret.Workspace.Graph;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that enumerates workspace registry membership (WIP-014, parity with the CLI's <c>workspaces list</c>).</summary>
public sealed class WorkspaceListTool : IMcpTool
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IWorkspaceRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceListTool"/> class.</summary>
    /// <param name="registry">The workspace registry to enumerate.</param>
    public WorkspaceListTool(IWorkspaceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "workspace_list",
        Description = "List all Ferret multi-repository workspaces.",
        InputSchemaJson = """{"type":"object","properties":{}}""",
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        IReadOnlyList<WorkspaceRegistryEntry> entries;
        try
        {
            entries = await _registry.ListAsync(ct).ConfigureAwait(false);
        }
        catch (WorkspaceRegistryCorruptException ex)
        {
            return McpToolResult.Error(ex.Message);
        }

        var payload = entries
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .Select(e => new
            {
                workspaceId = e.WorkspaceId,
                name = e.Name,
                kind = e.Kind,
                repoCount = e.Members.Repos.Count,
            });

        return McpToolResult.Success(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
