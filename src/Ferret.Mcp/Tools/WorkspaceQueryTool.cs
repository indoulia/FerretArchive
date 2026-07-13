using System.Globalization;
using System.Text;

using Ferret.Core.Search;
using Ferret.Knowledge.Federation;
using Ferret.Mcp.Protocol;
using Ferret.Workspace.Graph;

using Microsoft.Extensions.Logging;

namespace Ferret.Mcp.Tools;

/// <summary>MCP tool that queries a workspace and every workspace it references, merging results
/// (MCP parity for the CLI's <c>workspaces query</c> — the Workspace Intelligence Platform's
/// federated-query capability, previously reachable only from the CLI).</summary>
public sealed class WorkspaceQueryTool : IMcpTool
{
    private readonly IWorkspaceRegistry _registry;
    private readonly IRepoSearchServiceFactory _repoSearchServiceFactory;
    private readonly IWorkspaceStateFingerprintProvider _fingerprintProvider;
    private readonly FederatedQueryCache _queryCache;
    private readonly ILogger<FederatedKnowledgeStore>? _federationLogger;
    private readonly ILogger<CachingFederatedKnowledgeStore>? _cacheLogger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceQueryTool"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="repoSearchServiceFactory">Builds a per-repo search service for the federated fan-out.</param>
    /// <param name="fingerprintProvider">Computes the Workspace State Fingerprint used to verify a pinned reference.</param>
    /// <param name="queryCache">The process-local federated query cache (WIP-030/031).</param>
    /// <param name="federationLogger">Structured logger for the federated fan-out (WIP-040). Defaults to a no-op logger.</param>
    /// <param name="cacheLogger">Structured logger for the query cache (WIP-040). Defaults to a no-op logger.</param>
    public WorkspaceQueryTool(
        IWorkspaceRegistry registry,
        IRepoSearchServiceFactory repoSearchServiceFactory,
        IWorkspaceStateFingerprintProvider fingerprintProvider,
        FederatedQueryCache queryCache,
        ILogger<FederatedKnowledgeStore>? federationLogger = null,
        ILogger<CachingFederatedKnowledgeStore>? cacheLogger = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(repoSearchServiceFactory);
        ArgumentNullException.ThrowIfNull(fingerprintProvider);
        ArgumentNullException.ThrowIfNull(queryCache);
        _registry = registry;
        _repoSearchServiceFactory = repoSearchServiceFactory;
        _fingerprintProvider = fingerprintProvider;
        _queryCache = queryCache;
        _federationLogger = federationLogger;
        _cacheLogger = cacheLogger;
    }

    /// <inheritdoc/>
    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "workspace_query",
        Description = "Query a Ferret multi-repository workspace and every workspace it references, merging results with source-workspace citations.",
        InputSchemaJson = """{"type":"object","properties":{"workspace":{"type":"string","description":"Workspace ID or name"},"query":{"type":"string","description":"Full-text search query"},"max_results":{"type":"integer","description":"Maximum results to return (default: 20)"}},"required":["workspace","query"]}""",
    };

    /// <inheritdoc/>
    public async Task<McpToolResult> ExecuteAsync(McpArguments arguments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var workspaceArg = arguments.GetRequiredString("workspace");
        var queryText = arguments.GetRequiredString("query");
        var maxResults = arguments.TryGetInt32("max_results", out var n) ? n : 20;

        WorkspaceRegistryEntry? entry;
        try
        {
            entry = await WorkspaceLookup.ResolveAsync(_registry, workspaceArg, ct).ConfigureAwait(false);
        }
        catch (WorkspaceRegistryCorruptException ex)
        {
            return McpToolResult.Error(ex.Message);
        }

        if (entry is null)
        {
            return McpToolResult.Error($"Workspace '{workspaceArg}' not found. Use the workspace_list tool to see available workspaces.");
        }

        var options = new SearchOptions { MaxResults = maxResults, Mode = SearchExecutionMode.Auto };
        var innerStore = new FederatedKnowledgeStore(_registry, _repoSearchServiceFactory, entry.WorkspaceId, _fingerprintProvider, _federationLogger);
        var store = new CachingFederatedKnowledgeStore(innerStore, _registry, _fingerprintProvider, entry.WorkspaceId, _queryCache, _cacheLogger);
        var result = await store.SearchAsync(queryText, options).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var message = result.Status switch
            {
                SearchServiceStatus.WorkspaceNotFound =>
                    $"Workspace '{workspaceArg}' has no registry entry.",
                SearchServiceStatus.IndexNotFound =>
                    $"No queryable index found across '{entry.Name}' or its references. Run 'ferret index' in each member repo first.",
                SearchServiceStatus.InvalidQuery =>
                    $"Invalid query: {(result.Diagnostics.Count > 0 ? result.Diagnostics[0].Message : "empty or whitespace")}",
                _ => $"Query failed: {result.Status}",
            };
            return McpToolResult.Error(AppendDiagnostics(message, result.Diagnostics));
        }

        var namesById = await BuildWorkspaceNameLookupAsync(entry, ct).ConfigureAwait(false);
        var text = FormatHits(queryText, result.Hits, namesById);
        return McpToolResult.Success(AppendDiagnostics(text, result.Diagnostics));
    }

    private static string FormatHits(string queryText, IReadOnlyList<SearchHit> hits, IReadOnlyDictionary<Guid, string> namesById)
    {
        if (hits.Count == 0)
        {
            return $"No results found for: {queryText}";
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Found {hits.Count} result(s) for: {queryText}");
        sb.AppendLine();

        for (var i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            var source = hit.SourceId is { } id && namesById.TryGetValue(id, out var name) ? name : "unknown";
            sb.AppendLine(CultureInfo.InvariantCulture, $"[{i + 1}] [{source}] {hit.DisplayName} (score: {hit.Score:F2})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    {hit.CanonicalUri}");
        }

        return sb.ToString().TrimEnd();
    }

    // Stabilization Sprint 1: a result can be a genuine Success and still be partial (one or more
    // sources skipped) — surface that unconditionally, so a partial answer is never indistinguishable
    // from a complete one by default. Mirrors WorkspacesQueryCommandHandler.WriteDiagnostics.
    private static string AppendDiagnostics(string text, IReadOnlyList<SearchDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return text;
        }

        var sb = new StringBuilder(text);
        sb.AppendLine();
        sb.AppendLine();
        foreach (var diagnostic in diagnostics)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"! {diagnostic.Message}");
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildWorkspaceNameLookupAsync(WorkspaceRegistryEntry entry, CancellationToken ct)
    {
        var namesById = new Dictionary<Guid, string> { [entry.WorkspaceId] = entry.Name };
        foreach (var reference in entry.References)
        {
            if (namesById.ContainsKey(reference.WorkspaceId))
            {
                continue;
            }

            var referenced = await _registry.ResolveAsync(reference.WorkspaceId, ct).ConfigureAwait(false);
            if (referenced is not null)
            {
                namesById[referenced.WorkspaceId] = referenced.Name;
            }
        }

        return namesById;
    }
}
