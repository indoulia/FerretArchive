using Ferret.Cli.Cli;
using Ferret.Core.Search;
using Ferret.Knowledge.Federation;
using Ferret.Workspace.Graph;

namespace Ferret.Cli.Commands.Workspaces;

/// <summary>Handles 'ferret workspaces query' (WIP-SLICE-1) — the vertical slice's single query
/// surface: fans out across a workspace's own repos and every workspace it references, merging
/// results with source-workspace citations (03-Cross-Workspace-References.md, 04-Knowledge-Graph.md §2).</summary>
internal sealed class WorkspacesQueryCommandHandler : ICommandHandler
{
    private readonly IWorkspaceRegistry _registry;
    private readonly IRepoSearchServiceFactory _repoSearchServiceFactory;
    private readonly IWorkspaceStateFingerprintProvider _fingerprintProvider;

    /// <summary>Initializes a new instance of the <see cref="WorkspacesQueryCommandHandler"/> class.</summary>
    /// <param name="registry">The workspace registry.</param>
    /// <param name="repoSearchServiceFactory">Builds a per-repo search service for the federated fan-out.</param>
    /// <param name="fingerprintProvider">Computes the Workspace State Fingerprint used to verify a pinned reference.</param>
    public WorkspacesQueryCommandHandler(
        IWorkspaceRegistry registry,
        IRepoSearchServiceFactory repoSearchServiceFactory,
        IWorkspaceStateFingerprintProvider fingerprintProvider)
    {
        _registry = registry;
        _repoSearchServiceFactory = repoSearchServiceFactory;
        _fingerprintProvider = fingerprintProvider;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(IFerretContext context)
    {
        var workspaceArg = context.GetOption<string>("workspace");
        var queryText = context.GetOption<string>("query");
        if (string.IsNullOrWhiteSpace(workspaceArg) || string.IsNullOrWhiteSpace(queryText))
        {
            context.Services.Output.WriteError("Usage: ferret workspaces query <id-or-name> <query>.");
            return CommandResult.Failure;
        }

        WorkspaceRegistryEntry? entry;
        try
        {
            entry = await WorkspaceLookup.ResolveAsync(_registry, workspaceArg, context.CancellationToken).ConfigureAwait(false);
        }
        catch (WorkspaceRegistryCorruptException ex)
        {
            context.Services.Output.WriteError(ex.Message);
            return CommandResult.Failure;
        }

        if (entry is null)
        {
            context.Services.Output.WriteError($"Workspace '{workspaceArg}' not found. Run 'ferret workspaces list' to see available workspaces.");
            return CommandResult.Failure;
        }

        var limitOption = context.GetOption<int>("limit");
        var limit = limitOption > 0 ? limitOption : 20;
        var options = new SearchOptions { MaxResults = limit, Mode = SearchExecutionMode.Auto };
        var store = new FederatedKnowledgeStore(_registry, _repoSearchServiceFactory, entry.WorkspaceId, _fingerprintProvider);
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
            context.Services.Output.WriteError(message);
            WriteDiagnostics(context, result.Diagnostics);
            return CommandResult.Failure;
        }

        var namesById = await BuildWorkspaceNameLookupAsync(entry, context.CancellationToken).ConfigureAwait(false);
        WriteHits(context, result.Hits, namesById);
        WriteDiagnostics(context, result.Diagnostics);
        return CommandResult.Success;
    }

    private static void WriteHits(IFerretContext context, IReadOnlyList<SearchHit> hits, IReadOnlyDictionary<Guid, string> namesById)
    {
        if (hits.Count == 0)
        {
            context.Services.Output.WriteLine("No results.");
            return;
        }

        foreach (var hit in hits)
        {
            var source = hit.SourceWorkspaceId is { } id && namesById.TryGetValue(id, out var name) ? name : "unknown";
            context.Services.Output.WriteLine($"[{source}] {hit.DisplayName} (score: {hit.Score:F2})");
            context.Services.Output.WriteLine($"  {hit.CanonicalUri}");
        }
    }

    private static void WriteDiagnostics(IFerretContext context, IReadOnlyList<SearchDiagnostic> diagnostics)
    {
        // Stabilization Sprint 1: a result can be a genuine Success and still be partial (one or more
        // sources skipped) — surface that unconditionally (WriteLine, not WriteVerbose) so a partial
        // answer is never indistinguishable from a complete one by default.
        if (diagnostics.Count == 0)
        {
            return;
        }

        context.Services.Output.WriteLine();
        foreach (var diagnostic in diagnostics)
        {
            context.Services.Output.WriteLine($"! {diagnostic.Message}");
        }
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
